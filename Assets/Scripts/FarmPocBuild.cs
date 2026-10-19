using System;
using UnityEngine;

// Menu de construction partagé par la maison (mobilier) et la ferme (décorations fabriquées) :
// un catalogue central, puis un panneau rangé sur le côté pendant que l'objet se pose dans le décor.
public sealed partial class FarmPoc
{
    int decorTab,decorKind,decorRotation,decorX=3,decorZ=3,movingFurniture=-1,farmKind=1;
    bool decorSelecting,buildFarm,buildPlacing;
    float buildSlide;
    string decorMessage="",placeReason="";
    bool placeValid;
    Vector3 placeWorld;
    Vector2Int placedCell=new(-1,-1);
    public bool BuildPlacing=>buildPlacing;
    public bool BuildingFarm=>buildFarm;
    static readonly int[] PlaceableRecipes=Array.FindAll(BuildIndices(),i=>FarmCrafting.Recipes[i].Placeable);
    static int[] BuildIndices(){var all=new int[FarmCrafting.Recipes.Length];for(int i=0;i<all.Length;i++)all[i]=i;return all;}
    static Rect BuildDockRect(float w,float h)=>new(20,236,310,h-360);
    // Vue d'ensemble décalée et reculée : le panneau rangé à gauche ne masque ni la maison ni l'établi.
    static readonly Vector2 FarmOverview=new(5.8f,4.5f);
    const float FarmOverviewDistance=20.5f;
    Sprite FurnitureIcon(int kind,bool side=false)=>kind==7?art.HomeSprite(9):art.HomeSprite(kind,side);

    void OpenDecoration()
    {
        CancelMouseNavigation();view=View.Decoration;buildFarm=!World.Indoors;buildPlacing=false;buildSlide=0;
        decorTab=0;movingFurniture=-1;decorSelecting=false;
        if(buildFarm){var t=World.Target();decorX=Mathf.Clamp(t.x,0,FarmSimulation.Width-1);decorZ=Mathf.Clamp(t.y,0,FarmSimulation.Height-1);}
        else{decorX=Mathf.Clamp(decorX,0,FarmHome.Width-1);decorZ=Mathf.Clamp(decorZ,0,FarmHome.Depth-1);}
        decorMessage=buildFarm?"Choisis une décoration fabriquée à l'établi.":"Choisis un meuble à poser dans la pièce.";
    }
    public void OpenFarmPlacement(int recipe)
    {
        if(World.Indoors||recipe<0||recipe>=FarmCrafting.Recipes.Length||!FarmCrafting.Recipes[recipe].Placeable)return;
        OpenDecoration();farmKind=recipe;StartPlacing(false);
    }
    void StartPlacing(bool picking)
    {
        if(buildFarm&&!picking&&State.Crafted[farmKind]<1){decorMessage="Il ne te reste aucun(e) "+FarmCrafting.Recipes[farmKind].Name.ToLower()+". Fabrique-en à l'établi.";return;}
        buildPlacing=true;decorSelecting=picking;movingFurniture=-1;placedCell=new(-1,-1);
        if(!picking)MoveToFreeCell();
        // La ligne d'état explique la case visée ; ce message ne garde que le résultat des actions.
        decorMessage=picking?(buildFarm?"Vise une décoration pour la reprendre.":"Vise un meuble pour le reprendre."):"";
    }
    void BackToCatalog()
    {
        buildPlacing=false;decorSelecting=false;movingFurniture=-1;World.HideGhost();World.CameraFocus=null;World.CameraDistanceOverride=null;
        decorMessage=buildFarm?"Choisis une décoration fabriquée à l'établi.":"Choisis un meuble à poser dans la pièce.";
    }
    FarmFurniture Candidate()=>new(){Kind=decorKind,X=decorX,Z=decorZ,Rotation=decorRotation};
    static int MoveGrid(int index,Vector2Int nav,int count,int columns)
    {
        if(nav.x!=0)return (index+nav.x+count)%count;
        if(nav.y!=0)return ((index-nav.y*columns)%count+count)%count;
        return index;
    }
    void ProcessDecoration(FarmInputFrame f)
    {
        if(f.Inventory||f.Rotate)decorRotation=(decorRotation+1)%4;
        if(!buildPlacing)
        {
            if(!buildFarm&&f.Tab!=0)decorTab=(decorTab+f.Tab+3)%3;
            if(buildFarm)
            {
                int index=Mathf.Max(0,Array.IndexOf(PlaceableRecipes,farmKind));
                farmKind=PlaceableRecipes[MoveGrid((index+f.Quantity+PlaceableRecipes.Length)%PlaceableRecipes.Length,f.Navigate,PlaceableRecipes.Length,4)];
            }
            else if(decorTab==0)decorKind=MoveGrid((decorKind+f.Quantity+FarmHome.Catalog.Length)%FarmHome.Catalog.Length,f.Navigate,FarmHome.Catalog.Length,4);
            else
            {
                int change=f.Navigate.x!=0?f.Navigate.x:-f.Navigate.y;
                if(change==0)change=f.Quantity;
                if(change!=0){if(decorTab==1)Home.Wallpaper=(Home.Wallpaper+change+3)%3;else Home.Floor=(Home.Floor+change+3)%3;World.ApplyHome(Home);}
                return;
            }
            if(f.Wheel)StartPlacing(true);
            else if(f.Confirm)StartPlacing(false);
            return;
        }
        if(f.Wheel){decorSelecting=!decorSelecting;movingFurniture=-1;}
        if(buildFarm){decorX=Mathf.Clamp(decorX+f.Navigate.x,0,FarmSimulation.Width-1);decorZ=Mathf.Clamp(decorZ+f.Navigate.y,0,FarmSimulation.Height-1);}
        else
        {
            decorX=Mathf.Clamp(decorX+f.Navigate.x,0,FarmHome.Width-1);decorZ=Mathf.Clamp(decorZ+f.Navigate.y,0,FarmHome.Depth-1);
            if(f.Quantity!=0&&!decorSelecting){movingFurniture=-1;decorKind=(decorKind+f.Quantity+FarmHome.Catalog.Length)%FarmHome.Catalog.Length;}
        }
        if(f.Delete)RemoveAtCursor();
        if(f.Confirm)ConfirmPlacement();
    }
    void ConfirmPlacement()
    {
        if(decorSelecting){if(buildFarm)PickDecor();else PickFurniture();return;}
        if(buildFarm)PlaceDecor();else PlaceFurniture();
    }
    void RemoveAtCursor(){if(buildFarm)RemoveDecor();else RemoveFurniture();}
    void PickFurniture()
    {
        int index=Home.At(decorX,decorZ);
        if(index<0){decorMessage="Vise une case contenant un meuble.";return;}
        var item=Home.Furniture[index];movingFurniture=index;decorKind=item.Kind;decorRotation=item.Rotation;decorX=item.X;decorZ=item.Z;decorSelecting=false;
        decorMessage="Choisis la nouvelle place. Le meuble reste en place jusqu'à validation.";
    }
    void PlaceFurniture()
    {
        var item=Candidate();
        if(World.OccupiedByPlayer(item)){decorMessage="Le personnage se trouve ici. Choisis une autre place.";return;}
        if(Home.Furniture.Count>=100&&movingFurniture<0){decorMessage="La pièce contient déjà 100 décorations.";return;}
        if(Home.Place(item,movingFurniture,out decorMessage)){movingFurniture=-1;World.ApplyHome(Home);placedCell=new(decorX,decorZ);}
    }
    void RemoveFurniture()
    {
        int index=movingFurniture>=0?movingFurniture:Home.At(decorX,decorZ);
        if(index<0){decorMessage="Vise un meuble à retirer.";return;}
        Home.Furniture.RemoveAt(index);movingFurniture=-1;World.ApplyHome(Home);decorMessage="Meuble rangé dans le catalogue.";
    }
    void PickDecor()
    {
        int index=State.DecorAt(decorX,decorZ);
        if(index<0){decorMessage="Vise une case contenant une décoration.";return;}
        movingFurniture=index;farmKind=State.Decor[index].Kind;decorSelecting=false;
        decorMessage="Choisis la nouvelle place. La décoration reste en place jusqu'à validation.";
    }
    bool DecorCandidateValid(out string message)
    {
        if(!State.CanPlaceDecor(farmKind,decorX,decorZ,movingFurniture,out message))return false;
        if(World.DecorBlocked(decorX,decorZ,movingFurniture,out bool player)){message=player?"Le personnage est ici.":"Un obstacle gêne ici.";return false;}
        return true;
    }
    void PlaceDecor()
    {
        if(!DecorCandidateValid(out decorMessage))return;
        bool moving=movingFurniture>=0;
        if(!State.PlaceDecor(farmKind,decorX,decorZ,movingFurniture,out decorMessage))return;
        movingFurniture=-1;placedCell=new(decorX,decorZ);
        if(!moving&&State.Crafted[farmKind]<1){string placed=decorMessage;BackToCatalog();decorMessage=placed+" Il n'en reste plus dans l'inventaire.";Say(decorMessage);}
    }
    void RemoveDecor()
    {
        int index=movingFurniture>=0?movingFurniture:State.DecorAt(decorX,decorZ);
        State.RemoveDecor(index,out decorMessage);movingFurniture=-1;
    }
    void SetCellFromPoint(Vector2 p)
    {
        if(buildFarm){decorX=Mathf.Clamp(Mathf.RoundToInt(p.x),0,FarmSimulation.Width-1);decorZ=Mathf.Clamp(Mathf.RoundToInt(p.y),0,FarmSimulation.Height-1);return;}
        float x=p.x-FarmWorld25D.HomeOffsetX;
        if(decorSelecting){decorX=Mathf.Clamp(Mathf.FloorToInt(x),0,FarmHome.Width-1);decorZ=Mathf.Clamp(Mathf.FloorToInt(p.y),0,FarmHome.Depth-1);return;}
        // Le curseur vise le centre du meuble, pas son coin.
        var c=Candidate();
        decorX=Mathf.Clamp(Mathf.RoundToInt(x-c.Width*.5f),0,FarmHome.Width-c.Width);decorZ=Mathf.Clamp(Mathf.RoundToInt(p.y-c.Depth*.5f),0,FarmHome.Depth-c.Depth);
    }
    public bool PointerOverBuild(Vector2 screen)
    {
        float scale=InterfaceScale;if(scale<=0)return true;float w=Screen.width/scale,h=Screen.height/scale;
        var p=new Vector2(screen.x/scale,(Screen.height-screen.y)/scale);
        return BuildDockRect(w,h).Contains(p)||new Rect(w/2-360,h-103,720,83).Contains(p);
    }
    bool ProcessBuildClick(Vector2 screen,bool secondary)
    {
        if(secondary){BackToCatalog();return true;}
        if(PointerOverBuild(screen)||!World.PointerGround(screen,out var point))return false;
        SetCellFromPoint(point);ConfirmPlacement();return true;
    }
    void UpdateBuild(FarmInputFrame frame,float delta)
    {
        bool placing=view==View.Decoration&&buildPlacing;
        buildSlide=Mathf.MoveTowards(buildSlide,placing?1:0,delta/.22f);
        if(!placing){World.HideGhost();World.CameraFocus=null;World.CameraDistanceOverride=null;return;}
        if(controls.UsingMouse&&frame.PointerAvailable&&!PointerOverBuild(frame.PointerPosition)&&World.PointerGround(frame.PointerPosition,out var point))SetCellFromPoint(point);
        // À la souris, une vue d'ensemble fixe montre toute la ferme ; au clavier et à la manette, la caméra suit la case visée.
        World.CameraFocus=!buildFarm?null:controls.UsingMouse?FarmOverview:new Vector2(decorX,decorZ);
        World.CameraDistanceOverride=buildFarm&&controls.UsingMouse?FarmOverviewDistance:null;
        UpdateGhost();
    }
    bool CandidateValid(out string message)
    {
        if(buildFarm)return DecorCandidateValid(out message);
        var item=Candidate();
        if(!Home.CanPlace(item,movingFurniture,out message))return false;
        if(World.OccupiedByPlayer(item)){message="Le personnage se trouve ici.";return false;}
        return true;
    }
    // L'aperçu démarre sur la place libre la plus proche plutôt que sur le personnage ou hors de la pièce.
    void MoveToFreeCell()
    {
        int startX=decorX,startZ=decorZ,width=buildFarm?FarmSimulation.Width:FarmHome.Width,depth=buildFarm?FarmSimulation.Height:FarmHome.Depth;
        int bestX=Mathf.Clamp(startX,0,width-1),bestZ=Mathf.Clamp(startZ,0,depth-1),best=int.MaxValue;
        for(int z=0;z<depth;z++)for(int x=0;x<width;x++)
        {
            int distance=(x-startX)*(x-startX)+(z-startZ)*(z-startZ);if(distance>=best)continue;
            decorX=x;decorZ=z;if(CandidateValid(out _)){best=distance;bestX=x;bestZ=z;}
        }
        decorX=bestX;decorZ=bestZ;
    }
    void UpdateGhost()
    {
        placeReason="";placeValid=false;
        if(decorSelecting)
        {
            bool found=buildFarm?State.DecorAt(decorX,decorZ)>=0:Home.At(decorX,decorZ)>=0;
            placeWorld=buildFarm?new Vector3(decorX,FarmWorld25D.GroundHeight(decorX,decorZ),decorZ):new Vector3(FarmWorld25D.HomeOffsetX+decorX+.5f,0,decorZ+.5f);
            World.ShowMarkerGhost(placeWorld,found);placeValid=found;placeReason=found?"":"Aucun objet ici";
            return;
        }
        // Juste après une pose, l'aperçu s'efface pour laisser voir l'objet posé.
        if(placedCell.x==decorX&&placedCell.y==decorZ){World.HideGhost();return;}
        placedCell=new(-1,-1);
        placeValid=CandidateValid(out placeReason);
        if(buildFarm)
        {
            placeWorld=new Vector3(decorX,FarmWorld25D.GroundHeight(decorX,decorZ),decorZ);
            World.ShowDecorGhost(farmKind,decorX,decorZ,placeValid);return;
        }
        var item=Candidate();placeWorld=new Vector3(FarmWorld25D.HomeOffsetX+item.X+item.Width*.5f,0,item.Z+item.Depth*.5f);
        World.ShowFurnitureGhost(item,placeValid);
    }

    void DrawDecoration(float w,float h)
    {
        CraftStyles();
        float s=Mathf.SmoothStep(0,1,buildSlide);var dock=BuildDockRect(w,h);
        float height=buildFarm?64+Mathf.Ceil(PlaceableRecipes.Length/4f)*160+110:540;var catalog=new Rect(w/2-430,h/2-height/2,860,height);
        var r=new Rect(Mathf.Lerp(catalog.x,dock.x,s),Mathf.Lerp(catalog.y,dock.y,s),Mathf.Lerp(catalog.width,dock.width,s),Mathf.Lerp(catalog.height,dock.height,s));
        GUI.Box(r,GUIContent.none,panel);
        // Pendant le glissement, seul le cadre bouge : le contenu apparaît une fois le panneau en place.
        if(buildPlacing?s<.98f:s>.02f){GUI.Label(new Rect(r.x+18,r.y+14,r.width-36,30),buildPlacing?PlacingName():"Catalogue",title);return;}
        if(buildPlacing){DrawPlacingPanel(r);DrawPlacementTip(w,h);}else DrawBuildCatalog(r);
    }
    void DrawBuildCatalog(Rect r)
    {
        GUI.Label(new Rect(r.x+24,r.y+18,700,34),buildFarm?"MA FERME · DÉCORATIONS":"MA MAISON · DÉCORATION",title);
        if(GUI.Button(new Rect(r.xMax-62,r.y+18,38,30),"×",button)){CloseView();return;}
        float y=r.y+64;
        if(!buildFarm)
        {
            string[] tabs={"Meubles & objets","Papier peint","Sol"};float tabWidth=(r.width-68)/3;
            for(int i=0;i<3;i++)if(GUI.Button(new Rect(r.x+24+i*(tabWidth+10),y,tabWidth,37),tabs[i],decorTab==i?selected:button))decorTab=i;
            y+=50;
            if(decorTab!=0)
            {
                var options=decorTab==1?FarmHome.Wallpapers:FarmHome.Floors;int choice=decorTab==1?Home.Wallpaper:Home.Floor;
                for(int i=0;i<options.Length;i++)if(GUI.Button(new Rect(r.x+45+i*260,y+40,245,95),options[i],choice==i?selected:button))
                {if(decorTab==1)Home.Wallpaper=i;else Home.Floor=i;World.ApplyHome(Home);}
                GUI.Label(new Rect(r.x+45,y+170,770,90),"Le revêtement est appliqué à toute la pièce.\nFerme le menu pour voir le résultat et te promener chez toi.",text);
                if(GUI.Button(new Rect(r.xMax-228,r.yMax-50,204,33),"Terminer · Échap / B",button))CloseView();
                return;
            }
        }
        int count=buildFarm?PlaceableRecipes.Length:FarmHome.Catalog.Length;float cw=(r.width-78)/4,ch=150;
        for(int i=0;i<count;i++)
        {
            var cell=new Rect(r.x+24+(i%4)*(cw+10),y+(i/4)*(ch+10),cw,ch);
            int kind=buildFarm?PlaceableRecipes[i]:i;bool chosen=buildFarm?farmKind==kind:decorKind==kind;
            bool available=!buildFarm||State.Crafted[kind]>0;var old=GUI.color;if(!available)GUI.color=new Color(1,1,1,.55f);
            if(GUI.Button(cell,"",chosen?selected:button))
            {if(buildFarm)farmKind=kind;else{decorKind=kind;decorRotation=0;}StartPlacing(false);}
            SpriteIcon(new Rect(cell.x+12,cell.y+8,cell.width-24,ch-62),buildFarm?art.GetSprite(FarmCrafting.Recipes[kind].Icon):FurnitureIcon(kind));
            string name=buildFarm?FarmCrafting.Recipes[kind].Name:FarmHome.Catalog[kind].Name;
            string detail=buildFarm?"Disponible(s) : "+State.Crafted[kind]:FarmHome.Catalog[kind].Width+" × "+FarmHome.Catalog[kind].Depth+" cases";
            GUI.Label(new Rect(cell.x+8,cell.yMax-52,cell.width-16,24),name,chosen?center:craftDarkCenter);
            GUI.Label(new Rect(cell.x+8,cell.yMax-28,cell.width-16,22),detail,chosen?craftPaperSmall:craftDarkSmall);
            GUI.color=old;
        }
        float bottom=r.yMax-100;
        if(GUI.Button(new Rect(r.x+24,bottom,(r.width-58)/2,40),"Déplacer ou retirer · Tab / X",button))StartPlacing(true);
        if(GUI.Button(new Rect(r.x+34+(r.width-58)/2,bottom,(r.width-58)/2,40),"Choisir et poser · E / A",selected))StartPlacing(false);
        GUI.Label(new Rect(r.x+24,bottom+50,r.width-260,44),buildFarm&&State.TotalCrafted==0&&State.Decor.Count==0?"Fabrique une caisse, une lanterne ou un épouvantail à l'établi pour décorer ta ferme.":decorMessage,small);
        if(GUI.Button(new Rect(r.xMax-228,bottom+54,204,33),"Terminer · Échap / B",button))CloseView();
    }
    void DrawPlacingPanel(Rect r)
    {
        float x=r.x+16,width=r.width-32,y=r.y+14;
        GUI.Label(new Rect(x,y,width,30),PlacingName(),title);y+=34;
        if(!decorSelecting)
        {
            SpriteIcon(new Rect(x,y,72,72),buildFarm?art.GetSprite(FarmCrafting.Recipes[farmKind].Icon):FurnitureIcon(decorKind,decorRotation%2!=0));
            string info=movingFurniture>=0?"Déplacement en cours":buildFarm?"Disponible(s) : "+State.Crafted[farmKind]:"Emprise : "+Candidate().Width+" × "+Candidate().Depth+"\nRotation : "+decorRotation*90+"°";
            GUI.Label(new Rect(x+84,y+4,width-84,64),info,small);
        }
        else GUI.Label(new Rect(x,y+4,width,64),"L'anneau vert indique un objet que tu peux reprendre.",small);
        y+=74;
        // État de la case visée en direct, puis le résultat de la dernière action.
        string status=placedCell.x>=0?"Posé ! Vise une autre case pour continuer.":placeValid?(decorSelecting?"Objet trouvé : valide pour le reprendre.":"Emplacement libre : clic ou E pour poser."):placeReason;
        GUI.Label(new Rect(x,y,width,36),status,placedCell.x>=0||placeValid?craftOkLeft:craftMissingLeft);
        GUI.Label(new Rect(x,y+36,width,24),decorMessage,small);y+=62;
        if(GUI.Button(new Rect(x,y,width,36),decorSelecting?"Reprendre l'objet visé · clic / E":"Poser ici · clic / E",selected))ConfirmPlacement();y+=42;
        if(!buildFarm){if(GUI.Button(new Rect(x,y,width,36),"Tourner · R / Y",button))decorRotation=(decorRotation+1)%4;y+=42;}
        if(GUI.Button(new Rect(x,y,width,36),decorSelecting?"Revenir à la pose · Tab / X":"Déplacer un objet posé · Tab / X",decorSelecting?selected:button)){decorSelecting=!decorSelecting;movingFurniture=-1;}y+=42;
        if(GUI.Button(new Rect(x,y,width,36),"Retirer l'objet visé · Suppr",button))RemoveAtCursor();y+=42;
        if(GUI.Button(new Rect(x,y,width,36),"Catalogue · Échap / clic droit",button))BackToCatalog();
    }
    string PlacingName()=>decorSelecting?(buildFarm?"Déplacer une décoration":"Déplacer un meuble"):buildFarm?FarmCrafting.Recipes[farmKind].Name:FarmHome.Catalog[decorKind].Name;
    // Bulle sous l'aperçu : la raison d'un refus s'affiche là où la joueuse regarde.
    void DrawPlacementTip(float w,float h)
    {
        if(placeValid||placeReason.Length==0||placedCell.x>=0)return;
        var p=World.Camera.WorldToScreenPoint(placeWorld);float scale=InterfaceScale;if(p.z<=0||scale<=0)return;
        float width=Mathf.Min(420,craftMissingLeft.CalcSize(new GUIContent(placeReason)).x+24);
        var rect=new Rect(Mathf.Clamp(p.x/scale-width/2,340,w-width-12),Mathf.Clamp((Screen.height-p.y)/scale+18,12,h-140),width,28);
        GUI.Box(rect,GUIContent.none,panel);GUI.Label(new Rect(rect.x+10,rect.y+4,rect.width-20,22),placeReason,craftMissingLeft);
    }
}
