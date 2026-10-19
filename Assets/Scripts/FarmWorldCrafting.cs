using System.Collections.Generic;
using UnityEngine;

public sealed partial class FarmWorld25D
{
    public static readonly Vector2 WorkbenchPoint=new(1.1f,4.3f);
    MeshRenderer workbenchIllustration;
    Vector3 workbenchPosition,workbenchScale;
    readonly List<MeshRenderer> craftChips=new();
    float craftTime=-1;
    public bool NearWorkbench=>!Indoors&&Vector2.Distance(PlayerPoint,WorkbenchPoint)<1.1f;
    // Temps écoulé depuis le début de la fabrication, ou -1 hors fabrication.
    public void SetCraftTime(float seconds)=>craftTime=seconds;

    void BuildWorkbench()
    {
        var p=new Vector3(WorkbenchPoint.x,GroundHeight(WorkbenchPoint.x,WorkbenchPoint.y),WorkbenchPoint.y);
        // Provisoire : la table illustrée de la maison sert d'établi en attendant un dessin dédié.
        workbenchIllustration=Card("Établi — fabrication",art.HomeSprite(1),p,1.35f);
        workbenchPosition=workbenchIllustration.transform.localPosition;workbenchScale=workbenchIllustration.transform.localScale;
        ContactShadow("Ancrage établi",p+Vector3.up*.014f,new Vector2(1.45f,.45f));
        Prop(FarmArt.Kind.Crate,WorkbenchPoint+new Vector2(-.95f,-.05f),.55f);
        var g=new GameObject("Volume établi");g.transform.SetParent(root,false);g.transform.position=p+new Vector3(0,.55f,0);
        g.AddComponent<BoxCollider>().size=new Vector3(1.3f,1.1f,.6f);
        for(int i=0;i<6;i++)
        {
            var chip=Card("Copeau",art.Seed,p,.09f);chip.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            chip.gameObject.SetActive(false);craftChips.Add(chip);
        }
    }
    void UpdateWorkbench()
    {
        if(workbenchIllustration==null)return;
        bool active=craftTime>=0&&!Indoors;
        // Coups d'outil réguliers : l'établi s'écrase puis rebondit, des copeaux jaillissent.
        float strokes=active?craftTime*2.5f:0,sinceHit=(strokes-Mathf.Floor(strokes))/2.5f;
        float impact=active?Mathf.Exp(-sinceHit*18):0;
        workbenchIllustration.transform.localPosition=workbenchPosition+Vector3.up*impact*.03f;
        workbenchIllustration.transform.localScale=Vector3.Scale(workbenchScale,new Vector3(1+impact*.06f,1-impact*.07f,1));
        if(active&&impact>.5f)actionUntil=Time.time+.08f;
        for(int i=0;i<craftChips.Count;i++)
        {
            var chip=craftChips[i];bool flying=active&&sinceHit<.38f;chip.gameObject.SetActive(flying);if(!flying)continue;
            float angle=i*1.05f+Mathf.Floor(strokes)*2.1f;
            var spread=new Vector3(Mathf.Cos(angle)*.55f,0,Mathf.Sin(angle)*.2f-.15f)*(1+i%3*.3f);
            chip.transform.localPosition=workbenchPosition+new Vector3(0,1f,-.1f)+spread*sinceHit*2.4f+Vector3.up*(2.6f*sinceHit-7f*sinceHit*sinceHit);
        }
    }
}
