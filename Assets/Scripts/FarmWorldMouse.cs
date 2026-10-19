using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum FarmInteraction { None, House, Shop, Wardrobe, DecorChest, Exit, Workbench }

public sealed partial class FarmWorld25D
{
    MeshRenderer shopIllustration,wardrobeIllustration,chestIllustration,exitIllustration;
    readonly List<LineRenderer> landBorders=new();
    readonly Collider[] navigationHits=new Collider[64];
    Vector2Int? mouseAim;
    public Vector2 PlayerPoint=>new(Player.position.x,Player.position.z);
    public Rect WalkingBounds=>Indoors?new Rect(HomeOffsetX+.3f,.3f,8.4f,6.4f):new Rect(.3f,.3f,14.4f,10.4f);
    public void SetMouseAim(Vector2Int? plot)=>mouseAim=plot;
    public Vector2Int AimedPlot=>mouseAim??Target();
    public void FacePoint(Vector2 point){var delta=point-PlayerPoint;if(delta.sqrMagnitude>.001f)Facing=delta.normalized;}
    public Vector2 CameraInput(Vector2 worldDirection)
    {
        var right=Camera.transform.right;var forward=Camera.transform.forward;
        var r=new Vector2(right.x,right.z).normalized;var f=new Vector2(forward.x,forward.z).normalized;
        return new Vector2(Vector2.Dot(worldDirection,r),Vector2.Dot(worldDirection,f));
    }
    public void RotateCamera(float degrees){if(!Indoors){yaw=Mathf.Clamp(yaw+degrees,-45,45);UpdateCamera(1,true);UpdateCards();}}
    public bool PointerGround(Vector2 screen,out Vector2 point)
    {
        var ray=Camera.ScreenPointToRay(screen);var plane=new Plane(Vector3.up,Vector3.zero);
        point=default;if(!plane.Raycast(ray,out float distance))return false;
        var p=ray.GetPoint(distance);point=new Vector2(p.x,p.z);
        return Indoors?p.x>=HomeOffsetX&&p.x<=HomeOffsetX+9&&p.z>=0&&p.z<=7:p.x>=-.5f&&p.x<=14.7f&&p.z>=-.5f&&p.z<=10.7f;
    }
    bool HitPaper(MeshRenderer renderer,Ray ray,out float distance)
    {
        distance=0;if(renderer==null||!renderer.gameObject.activeInHierarchy)return false;
        if(!new Plane(renderer.transform.forward,renderer.transform.position).Raycast(ray,out distance))return false;
        Vector3 local=renderer.transform.InverseTransformPoint(ray.GetPoint(distance));var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
        var bounds=mesh.bounds;if(local.x<bounds.min.x||local.x>bounds.max.x||local.y<bounds.min.y||local.y>bounds.max.y)return false;
        var uv=mesh.uv;float u=Mathf.InverseLerp(bounds.min.x,bounds.max.x,local.x),v=Mathf.InverseLerp(bounds.min.y,bounds.max.y,local.y);
        var texture=renderer.sharedMaterial.GetTexture("_BaseMap") as Texture2D;
        return texture!=null&&texture.isReadable&&texture.GetPixelBilinear(Mathf.Lerp(uv[0].x,uv[1].x,u),Mathf.Lerp(uv[0].y,uv[3].y,v)).a>.18f;
    }
    public FarmInteraction PickInteraction(Vector2 screen)
    {
        var ray=Camera.ScreenPointToRay(screen);float closest=float.PositiveInfinity;FarmInteraction result=FarmInteraction.None;
        void Pick(MeshRenderer renderer,FarmInteraction interaction)
        {if(HitPaper(renderer,ray,out float distance)&&distance<closest){result=interaction;closest=distance;}}
        if(Indoors){Pick(wardrobeIllustration,FarmInteraction.Wardrobe);Pick(chestIllustration,FarmInteraction.DecorChest);Pick(exitIllustration,FarmInteraction.Exit);}
        else{Pick(house,FarmInteraction.House);Pick(shopIllustration,FarmInteraction.Shop);Pick(workbenchIllustration,FarmInteraction.Workbench);}
        return result;
    }
    public Vector2 ApproachPoint(FarmInteraction interaction)=>interaction switch {
        FarmInteraction.House=>new Vector2(2,5.9f),FarmInteraction.Shop=>new Vector2(11.8f,5.9f),
        // À côté de l'établi, pour ne pas le masquer pendant la fabrication.
        FarmInteraction.Workbench=>WorkbenchPoint+new Vector2(.95f,-.25f),
        FarmInteraction.Wardrobe=>new Vector2(HomeOffsetX+.5f,4.95f),FarmInteraction.DecorChest=>new Vector2(HomeOffsetX+8.5f,4.95f),
        _=>new Vector2(HomeOffsetX+4.5f,.65f)
    };
    public bool NearInteraction(FarmInteraction interaction)=>interaction switch {
        FarmInteraction.House=>NearHouse,FarmInteraction.Shop=>NearShop,FarmInteraction.Wardrobe=>NearWardrobe,FarmInteraction.DecorChest=>NearDecorChest,FarmInteraction.Exit=>NearExit,FarmInteraction.Workbench=>NearWorkbench,_=>false
    };
    public bool CanStand(Vector2 p)
    {
        var b=WalkingBounds;if(p.x<b.xMin||p.x>b.xMax||p.y<b.yMin||p.y>b.yMax)return false;
        int count=Physics.OverlapCapsuleNonAlloc(new Vector3(p.x,.34f,p.y),new Vector3(p.x,.92f,p.y),.27f,navigationHits,~0,QueryTriggerInteraction.Ignore);
        if(count==navigationHits.Length)return false;
        for(int i=0;i<count;i++)if(!navigationHits[i].transform.IsChildOf(Player))return false;
        return true;
    }
    public bool CanTravel(Vector2 from,Vector2 to)
    {
        var direction=new Vector3(to.x-from.x,0,to.y-from.y);float distance=direction.magnitude;if(distance<.001f)return true;
        var hits=Physics.CapsuleCastAll(new Vector3(from.x,.34f,from.y),new Vector3(from.x,.92f,from.y),.25f,direction/distance,distance,~0,QueryTriggerInteraction.Ignore);
        foreach(var hit in hits)if(!hit.transform.IsChildOf(Player))return false;
        return true;
    }
    public FarmNavigation Navigation(){Physics.SyncTransforms();return new FarmNavigation(WalkingBounds,CanStand,CanTravel);}
    void BuildLandBorders()
    {
        var material=Paper(Texture2D.whiteTexture);
        for(int i=0;i<FarmLand.Lots.Length;i++)
        {
            var lot=FarmLand.Lots[i];var g=new GameObject("Limites — "+lot.Name);g.transform.SetParent(root,false);
            var line=g.AddComponent<LineRenderer>();line.sharedMaterial=material;line.loop=true;line.useWorldSpace=true;line.positionCount=4;
            line.startWidth=line.endWidth=.035f;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            line.SetPositions(new[]{new Vector3(lot.X-.48f,.04f,lot.Z-.48f),new Vector3(lot.X+lot.Width-.52f,.04f,lot.Z-.48f),new Vector3(lot.X+lot.Width-.52f,.04f,lot.Z+lot.Depth-.52f),new Vector3(lot.X-.48f,.04f,lot.Z+lot.Depth-.52f)});
            landBorders.Add(line);
        }
        state.LandChanged+=RefreshLandBorders;RefreshLandBorders();
    }
    void RefreshLandBorders()
    {
        for(int i=0;i<landBorders.Count;i++)
        {
            var properties=new MaterialPropertyBlock();properties.SetColor("_BaseColor",state.OwnsLand(i)?new Color(.55f,.70f,.39f):new Color(.94f,.70f,.31f));landBorders[i].SetPropertyBlock(properties);
        }
    }
}
