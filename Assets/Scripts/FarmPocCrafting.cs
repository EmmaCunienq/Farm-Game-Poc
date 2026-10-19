using UnityEngine;

public sealed partial class FarmPoc
{
    int craftCursor,craftRecipe;
    float craftTimer=-1,craftDoneUntil;
    GUIStyle craftDark,craftDarkSmall,craftOk,craftMissing;
    public int CraftCursor=>craftCursor;
    public bool Crafting=>craftTimer>=0;
    static readonly int[] ObjectItems=BuildObjectItems();
    static int[] BuildObjectItems(){var items=new int[FarmCrafting.Recipes.Length];for(int i=0;i<items.Length;i++)items[i]=6+i;return items;}

    // Identifiants d'inventaire : 0-2 graines, 3-5 récoltes, 6+ objets fabriqués.
    int ItemCount(int id)=>id<3?State.Seeds[id]:id<6?State.Products[id-3]:State.Crafted[id-6];
    Sprite ItemSprite(int id)=>id<6?art.CropSprite(id%3):art.GetSprite(FarmCrafting.Recipes[id-6].Icon);
    string ItemName(int id)=>id<6?FarmSimulation.Crops[id%3].Name:FarmCrafting.Recipes[id-6].Name;
    string ItemCategory(int id)=>id<3?"Graines":id<6?"Récolte":FarmCrafting.Recipes[id-6].Category;
    static int Columns(int count)=>count>6?4:3;

    public void OpenCrafting()
    {
        CancelMouseNavigation();
        if(!World.NearWorkbench){Say("Approche-toi de l'établi, à gauche des champs, pour fabriquer des objets.");return;}
        view=View.Craft;craftDoneUntil=0;
    }
    void ProcessCrafting(FarmInputFrame f)
    {
        if(Crafting)return;
        int n=FarmCrafting.Recipes.Length,change=f.Navigate.y!=0?-f.Navigate.y:f.Navigate.x;
        if(change!=0){craftCursor=(craftCursor+change+n)%n;craftDoneUntil=0;}
        if(f.Confirm)BeginCraft();
    }
    void BeginCraft()
    {
        if(Crafting)return;
        if(!State.CanCraft(craftCursor)){Say("Il manque "+State.MissingFor(craftCursor)+" pour fabriquer cet objet.");return;}
        craftRecipe=craftCursor;craftTimer=0;craftDoneUntil=0;
        World.FacePoint(FarmWorld25D.WorkbenchPoint);World.SetCraftTime(0);
    }
    void UpdateCrafting(float delta)
    {
        if(!Crafting)return;
        craftTimer+=delta;World.SetCraftTime(craftTimer);
        if(craftTimer<FarmCrafting.CraftSeconds)return;
        // Les ingrédients ne sont consommés qu'une fois l'animation terminée.
        craftTimer=-1;World.SetCraftTime(-1);
        if(State.Craft(craftRecipe,out var message)){craftDoneUntil=Time.unscaledTime+1.4f;SaveGame(false);}
        Say(message);
    }
    void StopCrafting()
    {
        if(Crafting)Say("Fabrication interrompue : aucun ingrédient n'a été utilisé.");
        craftTimer=-1;craftDoneUntil=0;World.SetCraftTime(-1);
    }
    void SpriteIcon(Rect target,Sprite sprite)
    {
        var uv=new Rect(sprite.rect.x/sprite.texture.width,sprite.rect.y/sprite.texture.height,sprite.rect.width/sprite.texture.width,sprite.rect.height/sprite.texture.height);
        float ratio=sprite.rect.width/sprite.rect.height;float width=Mathf.Min(target.width,target.height*ratio),height=width/ratio;
        var rect=new Rect(target.center.x-width/2,target.center.y-height/2,width,height);GUI.DrawTextureWithTexCoords(rect,sprite.texture,uv,true);
    }
    void CraftStyles()
    {
        if(craftDark!=null)return;
        craftDark=new GUIStyle(text);craftDark.normal.textColor=new Color(.20f,.23f,.18f);
        craftDarkSmall=new GUIStyle(small);craftDarkSmall.normal.textColor=new Color(.33f,.30f,.22f);
        craftOk=new GUIStyle(text){alignment=TextAnchor.MiddleRight,fontStyle=FontStyle.Bold};craftOk.normal.textColor=new Color(.62f,.86f,.50f);
        craftMissing=new GUIStyle(craftOk);craftMissing.normal.textColor=new Color(.96f,.55f,.42f);
    }
    void DrawCrafting(float w,float h)
    {
        CraftStyles();
        var r=new Rect(w/2-450,h/2-270,900,540);GUI.Box(r,GUIContent.none,panel);
        GUI.Label(new Rect(r.x+24,r.y+18,700,34),"L'ÉTABLI · FABRICATION",title);
        if(GUI.Button(new Rect(r.xMax-62,r.y+18,38,31),"×",button)){CloseView();return;}
        bool enabled=GUI.enabled;
        for(int i=0;i<FarmCrafting.Recipes.Length;i++)
        {
            var recipe=FarmCrafting.Recipes[i];var cell=new Rect(r.x+24,r.y+66+i*64,330,56);
            GUI.enabled=enabled&&!Crafting;
            if(GUI.Button(cell,"",i==craftCursor?selected:button)){craftCursor=i;craftDoneUntil=0;}
            GUI.enabled=enabled;
            SpriteIcon(new Rect(cell.x+8,cell.y+6,44,44),art.GetSprite(recipe.Icon));
            GUI.Label(new Rect(cell.x+62,cell.y+5,260,24),recipe.Name,i==craftCursor?text:craftDark);
            GUI.Label(new Rect(cell.x+62,cell.y+29,260,22),recipe.Category+" · "+(State.CanCraft(i)?"prêt à fabriquer":"ingrédients manquants")+" · × "+State.Crafted[i],i==craftCursor?small:craftDarkSmall);
        }
        int shown=Crafting?craftRecipe:craftCursor;var current=FarmCrafting.Recipes[shown];
        var detail=new Rect(r.x+372,r.y+66,504,376);GUI.Box(detail,GUIContent.none,panel);
        var iconRect=new Rect(detail.x+16,detail.y+14,112,112);
        GUI.Label(new Rect(detail.x+144,detail.y+14,344,30),current.Name,title);
        GUI.Label(new Rect(detail.x+144,detail.y+48,344,22),current.Category+" · Tu en possèdes "+State.Crafted[shown],small);
        GUI.Label(new Rect(detail.x+144,detail.y+72,344,56),current.Description,text);
        GUI.Label(new Rect(detail.x+16,detail.y+142,472,24),"IL TE FAUT",center);
        // Lignes d'ingrédients : chaque espèce requise, puis les pièces.
        var rows=new System.Collections.Generic.List<(Sprite icon,string name,int have,int need)>();
        for(int k=0;k<current.Crops.Length;k++)if(current.Crops[k]>0)rows.Add((art.CropSprite(k),FarmSimulation.Crops[k].Name,State.Products[k],current.Crops[k]));
        if(current.Coins>0)rows.Add((art.Seed,"Pièces",State.Money,current.Coins));
        float progress=Crafting?Mathf.Clamp01(craftTimer/FarmCrafting.CraftSeconds):0;
        for(int k=0;k<rows.Count;k++)
        {
            var row=new Rect(detail.x+16,detail.y+172+k*44,472,38);GUI.Box(row,GUIContent.none,panel);
            var rowIcon=new Rect(row.x+8,row.y+3,32,32);
            if(!Crafting)SpriteIcon(rowIcon,rows[k].icon);
            GUI.Label(new Rect(row.x+50,row.y+7,250,26),rows[k].name,text);
            GUI.Label(new Rect(row.x+300,row.y+7,160,26),rows[k].have+" / "+rows[k].need,rows[k].have>=rows[k].need?craftOk:craftMissing);
            if(Crafting)FlyingIngredient(rowIcon,iconRect,rows[k].icon,progress,k);
        }
        DrawCraftIcon(iconRect,art.GetSprite(current.Icon),progress);
        GUI.Label(new Rect(detail.x+16,detail.y+346,472,24),"Valeur chez le marchand : "+current.SalePrice+" pièces",small);
        if(Crafting)
        {
            var bar=new Rect(detail.x+16,detail.y+320,472,18);var old=GUI.color;
            GUI.color=new Color(.07f,.11f,.10f);GUI.DrawTexture(bar,Texture2D.whiteTexture);
            GUI.color=new Color(.85f,.62f,.30f);GUI.DrawTexture(new Rect(bar.x+2,bar.y+2,(bar.width-4)*progress,bar.height-4),Texture2D.whiteTexture);
            GUI.color=old;
        }
        string action=Crafting?"Fabrication en cours…":State.CanCraft(craftCursor)?"Fabriquer · E / A":"Il manque des ingrédients";
        GUI.enabled=enabled&&!Crafting&&State.CanCraft(craftCursor);
        if(GUI.Button(new Rect(r.x+372,r.y+456,504,48),action,selected))BeginCraft();
        GUI.enabled=enabled;
        GUI.Label(new Rect(r.x+24,r.y+460,330,44),"Haut / bas : choisir un objet\nE / A : fabriquer · Échap / B : fermer",small);
        GUI.Label(new Rect(r.x+24,r.y+510,852,24),"Les récoltes et les pièces sont utilisées à la fin de la fabrication. Les objets se vendent chez le marchand.",small);
    }
    void FlyingIngredient(Rect from,Rect to,Sprite sprite,float progress,int index)
    {
        // Chaque ingrédient rejoint l'objet en tournoyant, l'un après l'autre.
        float p=Mathf.Clamp01((progress-index*.12f)/.5f);if(p>=1)return;
        float eased=p*p*(3-2*p);var size=Mathf.Lerp(from.width,from.width*.45f,eased);
        var center=Vector2.Lerp(from.center,to.center,eased)+Vector2.up*(-Mathf.Sin(eased*Mathf.PI)*60);
        var matrix=GUI.matrix;var old=GUI.color;GUI.color=new Color(1,1,1,1-p*p);
        GUIUtility.RotateAroundPivot(eased*300,center);SpriteIcon(new Rect(center.x-size/2,center.y-size/2,size,size),sprite);
        GUI.matrix=matrix;GUI.color=old;
    }
    void DrawCraftIcon(Rect rect,Sprite sprite,float progress)
    {
        var old=GUI.color;var matrix=GUI.matrix;float scale=1;
        if(Crafting)
        {
            // L'objet se forme au rythme des coups d'outil.
            float strokes=craftTimer*2.5f,impact=Mathf.Exp(-(strokes-Mathf.Floor(strokes))/2.5f*18);
            scale=Mathf.Lerp(.55f,1,progress)*(1-impact*.08f);GUI.color=new Color(1,1,1,.25f+.75f*progress);
            GUIUtility.RotateAroundPivot(Mathf.Sin(craftTimer*15)*4*(1-progress),rect.center);
        }
        else if(Time.unscaledTime<craftDoneUntil)
        {
            float t=1-(craftDoneUntil-Time.unscaledTime)/1.4f;scale=1+.35f*Mathf.Exp(-t*6)*Mathf.Cos(t*18);
            float ring=Mathf.Lerp(.6f,2.1f,t);GUI.color=new Color(1,1,1,1-t);
            GUI.DrawTexture(new Rect(rect.center.x-rect.width*ring/2,rect.center.y-rect.height*ring/2,rect.width*ring,rect.height*ring),art.Marker.texture);
            GUI.color=old;GUI.Label(new Rect(rect.x,rect.y-26*t,rect.width,26),"+1 !",center);
        }
        SpriteIcon(new Rect(rect.center.x-rect.width*scale/2,rect.center.y-rect.height*scale/2,rect.width*scale,rect.height*scale),sprite);
        GUI.matrix=matrix;GUI.color=old;
    }
}
