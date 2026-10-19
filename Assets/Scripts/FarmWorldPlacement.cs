using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class FarmWorld25D
{
    Transform decorRoot,ghost;
    MeshRenderer ghostMarker;
    int ghostKey=int.MinValue;
    readonly List<Light> decorLights=new();
    readonly Collider[] decorHits=new Collider[16];
    // Point suivi par la caméra à la place du personnage, pendant une pose à la manette ou au clavier.
    public Vector2? CameraFocus { get; set; }
    // Recul de la caméra extérieure, pour la vue d'ensemble pendant une pose à la souris.
    public float? CameraDistanceOverride { get; set; }

    void RebuildDecor()
    {
        if(decorRoot!=null){decorRoot.gameObject.SetActive(false);FarmArt.Release(decorRoot.gameObject);}
        decorLights.Clear();
        decorRoot=new GameObject("Décorations de la ferme").transform;decorRoot.SetParent(root,false);
        // L'ordre des enfants suit State.Decor : DecorBlocked s'en sert pour ignorer l'objet déplacé.
        foreach(var item in state.Decor)
        {
            var recipe=FarmCrafting.Recipes[item.Kind];var g=new GameObject(recipe.Name).transform;g.SetParent(decorRoot,false);
            g.localPosition=new Vector3(item.X,GroundHeight(item.X,item.Z),item.Z);DecorArt(item.Kind,g);
            var sprite=art.GetSprite(recipe.Icon);float width=recipe.DecorHeight*sprite.rect.width/sprite.rect.height;
            ContactShadow("Ancrage "+recipe.Name,g.position+Vector3.up*.014f,new Vector2(width*.8f,.3f)).transform.SetParent(g,true);
            var box=g.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,.5f,0);box.size=new Vector3(.6f,1,.6f);
            if(recipe.Icon!=FarmArt.Kind.Pumpkin)continue;
            var light=new GameObject("Lueur de la lanterne").AddComponent<Light>();light.transform.SetParent(g,false);light.transform.localPosition=new Vector3(0,.45f,-.25f);
            light.type=LightType.Point;light.range=3.2f;light.color=new Color(1,.60f,.26f);light.shadows=LightShadows.None;decorLights.Add(light);
        }
        if(sun!=null)UpdateLighting();
    }
    void DecorArt(int kind,Transform g)
    {
        var recipe=FarmCrafting.Recipes[kind];
        Card("Illustration "+recipe.Name,art.GetSprite(recipe.Icon),Vector3.zero,recipe.DecorHeight,g);
    }
    public bool DecorBlocked(int x,int z,int ignore,out bool player)
    {
        Physics.SyncTransforms();player=false;bool blocked=false;
        int count=Physics.OverlapBoxNonAlloc(new Vector3(x,.55f,z),new Vector3(.3f,.4f,.3f),decorHits,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
        Transform ignored=ignore>=0&&decorRoot!=null&&ignore<decorRoot.childCount?decorRoot.GetChild(ignore):null;
        for(int i=0;i<count;i++)
        {
            var hit=decorHits[i].transform;if(ignored!=null&&hit.IsChildOf(ignored))continue;
            if(hit.IsChildOf(Player))player=true;blocked=true;
        }
        return blocked;
    }
    public void ShowFurnitureGhost(FarmFurniture item,bool valid)=>
        Ghost(1000+item.Kind*4+item.Rotation,interior,g=>FurnitureArt(item,g),new Vector3(item.X+item.Width*.5f,0,item.Z+item.Depth*.5f),new Vector2(item.Width,item.Depth)*.96f,valid);
    public void ShowDecorGhost(int kind,int x,int z,bool valid)=>
        Ghost(2000+kind,root,g=>DecorArt(kind,g),new Vector3(x,GroundHeight(x,z),z),Vector2.one*.9f,valid);
    public void ShowMarkerGhost(Vector3 position,bool valid)=>Ghost(-1,root,null,position,Vector2.one*.9f,valid);
    void Ghost(int key,Transform parent,Action<Transform> build,Vector3 localPosition,Vector2 footprint,bool valid)
    {
        if(ghost==null||ghostKey!=key)
        {
            HideGhost();ghost=new GameObject("Aperçu de pose").transform;ghost.SetParent(parent,false);ghostKey=key;
            build?.Invoke(ghost);
            ghostMarker=GroundCard("Emprise",art.Marker,new Vector3(0,.035f,0),footprint,ghost);
            foreach(var r in ghost.GetComponentsInChildren<MeshRenderer>())r.shadowCastingMode=ShadowCastingMode.Off;
        }
        ghost.localPosition=localPosition;
        var color=valid?new Color(.62f,1,.62f):new Color(1,.48f,.42f);
        foreach(var r in ghost.GetComponentsInChildren<MeshRenderer>())SetTint(r,color);
    }
    public void HideGhost()
    {
        if(ghost==null)return;
        ghost.gameObject.SetActive(false);FarmArt.Release(ghost.gameObject);ghost=null;ghostMarker=null;ghostKey=int.MinValue;
    }
}
