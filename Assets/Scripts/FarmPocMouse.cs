using System.Collections.Generic;
using UnityEngine;

public sealed partial class FarmPoc
{
    List<Vector2> mousePath;
    int mouseWaypoint,landCursor;
    bool mouseFarmAction,mouseScene;
    FarmInteraction mouseInteraction;
    FarmTool mouseTool;
    Vector2Int mousePlot;
    Vector2 mousePrevious;
    float mouseBlockedTime;
    public bool MouseWalking=>mousePath!=null;
    public void CancelMouseNavigation()
    {mousePath=null;mouseWaypoint=0;mouseFarmAction=false;mouseInteraction=FarmInteraction.None;mouseBlockedTime=0;}
    static float InterfaceScale=>Mathf.Min(Screen.width/1200f,Screen.height/760f);
    Rect LandLabelRect(int id,float w,float h)
    {
        if(World.Indoors||State.OwnsLand(id))return Rect.zero;
        var lot=FarmLand.Lots[id];var p=World.Camera.WorldToScreenPoint(new Vector3(lot.X+(lot.Width-1)*.5f,.05f,lot.Z+(lot.Depth-1)*.5f));
        if(p.z<=0)return Rect.zero;float scale=InterfaceScale;
        var rect=new Rect(p.x/scale-99,(Screen.height-p.y)/scale-15,198,30);
        if(rect.x<12||rect.xMax>w-12||rect.y<270||rect.yMax>h-110)return Rect.zero;
        return rect;
    }
    public bool PointerOverHud(Vector2 screen)
    {
        float scale=InterfaceScale;if(scale<=0)return true;float w=Screen.width/scale,h=Screen.height/scale;
        var p=new Vector2(screen.x/scale,(Screen.height-screen.y)/scale);
        if(new Rect(20,20,292,247).Contains(p)||new Rect(w-290,20,270,207).Contains(p)||
            new Rect(w/2-360,h-103,720,83).Contains(p))return true;
        if(Time.unscaledTime<toastUntil&&new Rect(w/2-280,20,560,50).Contains(p))return true;
        for(int i=0;i<FarmLand.Lots.Length;i++)if(LandLabelRect(i,w,h).Contains(p))return true;
        return false;
    }
    public bool ProcessPointerClick(Vector2 screen,bool secondary)
    {
        controls.MarkMouseUsed();
        if(view==View.Decoration&&buildPlacing)return ProcessBuildClick(screen,secondary);
        if(secondary&&view!=View.Play)
        {
            if(IsPaused){if(view==View.Pause)Resume();else view=View.Pause;}else CloseView();return true;
        }
        if(view!=View.Play||PointerOverHud(screen))return false;
        CancelMouseNavigation();var interaction=World.PickInteraction(screen);
        if(interaction!=FarmInteraction.None)return StartMouseInteraction(interaction);
        if(!World.PointerGround(screen,out var point))return false;
        int x=Mathf.RoundToInt(point.x),z=Mathf.RoundToInt(point.y);
        if(!World.Indoors)
        {
            int land=FarmLand.At(x,z);if(land>=0&&!State.OwnsLand(land)){OpenLand(land);return true;}
        }
        if(!secondary)return StartMouseMove(point);
        if(ActiveTool==FarmTool.House||ActiveTool==FarmTool.Wardrobe)
        {
            if(!World.Indoors&&ActiveTool==FarmTool.House){OpenDecoration();return true;}
            if(!World.Indoors){Say("Clique sur la maison pour entrer, puis utilise l'armoire.");return true;}
            if(ActiveTool==FarmTool.House)OpenDecoration();else OpenAppearance();return true;
        }
        return StartMouseFarmAction(new Vector2Int(x,z));
    }
    public bool StartMouseMove(Vector2 destination)
    {
        CancelMouseNavigation();
        if(!World.Navigation().FindPath(World.PlayerPoint,destination,out var path)){Say("Cet endroit est inaccessible. Choisis un point sur le sol libre.");return false;}
        BeginMousePath(path);return true;
    }
    public bool StartMouseInteraction(FarmInteraction interaction)
    {
        CancelMouseNavigation();
        if(interaction==FarmInteraction.None)return false;
        bool indoor=interaction==FarmInteraction.Wardrobe||interaction==FarmInteraction.DecorChest||interaction==FarmInteraction.Exit;
        if(indoor!=World.Indoors)return false;
        if(World.NearInteraction(interaction)){ExecuteMouseInteraction(interaction);return true;}
        if(!World.Navigation().FindPath(World.PlayerPoint,World.ApproachPoint(interaction),out var path)){Say("Le passage est bloqué. Libère un chemin pour rejoindre cet endroit.");return false;}
        BeginMousePath(path);mouseInteraction=interaction;return true;
    }
    public bool StartMouseFarmAction(Vector2Int plot)
    {
        CancelMouseNavigation();
        if(World.Indoors){Say("Clique sur le coffre pour décorer ou sur l'armoire pour te changer.");return false;}
        int lot=FarmLand.At(plot.x,plot.y);if(lot>=0&&!State.OwnsLand(lot)){OpenLand(lot);return true;}
        if(!State.CanCultivate(plot.x,plot.y)){Say("Choisis une case de tes champs pour utiliser cet outil.");return false;}
        var point=new Vector2(plot.x,plot.y);
        if(Vector2.Distance(World.PlayerPoint,point)<=1.15f){World.FacePoint(point);UseFarmToolAt(plot);return true;}
        var navigation=World.Navigation();var approaches=new List<Vector2>();
        for(int i=0;i<8;i++){float angle=i*Mathf.PI/4;approaches.Add(point+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*.85f);}
        approaches.Sort((a,b)=>(a-World.PlayerPoint).sqrMagnitude.CompareTo((b-World.PlayerPoint).sqrMagnitude));
        foreach(var approach in approaches)if(navigation.FindPath(World.PlayerPoint,approach,out var path))
        {BeginMousePath(path);mouseFarmAction=true;mousePlot=plot;mouseTool=ActiveTool;return true;}
        Say("Impossible d'atteindre cette case depuis ici.");return false;
    }
    void BeginMousePath(List<Vector2> path)
    {mousePath=path;mouseWaypoint=0;mouseScene=World.Indoors;mousePrevious=World.PlayerPoint;mouseBlockedTime=0;}
    void ExecuteMouseInteraction(FarmInteraction interaction)
    {
        CancelMouseNavigation();
        switch(interaction)
        {
            case FarmInteraction.House:World.SetIndoors(true);SaveGame(false);Say("Bienvenue ! Clique sur le coffre ou l'armoire pour aménager ta maison.");break;
            case FarmInteraction.Exit:World.SetIndoors(false);SaveGame(false);break;
            case FarmInteraction.Shop:OpenShop();break;
            case FarmInteraction.Wardrobe:OpenAppearance();break;
            case FarmInteraction.DecorChest:OpenDecoration();break;
            case FarmInteraction.Workbench:OpenCrafting();break;
        }
    }
    public Vector2 MouseMovement(Vector2 manual,float delta)
    {
        if(view!=View.Play||manual.sqrMagnitude>.01f||mouseScene!=World.Indoors){CancelMouseNavigation();return manual;}
        if(mousePath==null)return manual;
        if(mouseInteraction!=FarmInteraction.None&&World.NearInteraction(mouseInteraction)){ExecuteMouseInteraction(mouseInteraction);return Vector2.zero;}
        if(mouseFarmAction&&Vector2.Distance(World.PlayerPoint,new Vector2(mousePlot.x,mousePlot.y))<=1.15f)
        {
            var plot=mousePlot;var tool=mouseTool;CancelMouseNavigation();ActiveTool=tool;World.FacePoint(new Vector2(plot.x,plot.y));UseFarmToolAt(plot);return Vector2.zero;
        }
        while(mouseWaypoint<mousePath.Count&&Vector2.Distance(World.PlayerPoint,mousePath[mouseWaypoint])<.07f)mouseWaypoint++;
        if(mouseWaypoint>=mousePath.Count){CancelMouseNavigation();return Vector2.zero;}
        if(Vector2.Distance(mousePrevious,World.PlayerPoint)<.003f)mouseBlockedTime+=delta;else mouseBlockedTime=0;
        mousePrevious=World.PlayerPoint;
        if(mouseBlockedTime>2){CancelMouseNavigation();Say("Le passage est bloqué. Clique pour choisir un autre chemin.");return Vector2.zero;}
        var direction=mousePath[mouseWaypoint]-World.PlayerPoint;
        return World.CameraInput(direction.normalized*Mathf.Min(1,direction.magnitude/Mathf.Max(.001f,3.2f*delta)));
    }
    void UpdateMouseAim(FarmInputFrame frame)
    {
        Vector2Int? target=null;
        if(view==View.Play&&controls.UsingMouse&&frame.PointerAvailable&&frame.Move.sqrMagnitude<.01f&&!PointerOverHud(frame.PointerPosition)&&World.PointerGround(frame.PointerPosition,out var p))
            target=new Vector2Int(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y));
        World.SetMouseAim(target);
    }
    void OpenLand(int selectedLot=0){CancelMouseNavigation();view=View.Land;landCursor=Mathf.Clamp(selectedLot,0,FarmLand.Lots.Length-1);}
    void LandAction()
    {
        if(State.OwnsLand(landCursor))
        {
            var lot=FarmLand.Lots[landCursor];view=View.Play;if(World.Indoors)World.SetIndoors(false);
            StartMouseMove(new Vector2(lot.X+(lot.Width-1)*.5f,lot.Z+(lot.Depth-1)*.5f));return;
        }
        bool bought=State.BuyLand(landCursor,out var message);if(bought)SaveGame(false);Say(message);
    }
    void DrawLand(float w,float h)
    {
        var r=new Rect(w/2-410,h/2-250,820,500);GUI.Box(r,GUIContent.none,panel);
        GUI.Label(new Rect(r.x+24,r.y+20,700,34),"AGRANDIR LA FERME",title);
        if(GUI.Button(new Rect(r.xMax-62,r.y+20,38,30),"×",button)){CloseView();return;}
        GUI.Label(new Rect(r.x+24,r.y+65,770,28),State.Money+" pièces · Les achats sont conservés dans ta sauvegarde.",text);
        for(int i=0;i<FarmLand.Lots.Length;i++)
        {
            var lot=FarmLand.Lots[i];float y=r.y+106+i*94;GUI.Box(new Rect(r.x+24,y,772,84),GUIContent.none,panel);
            GUI.Label(new Rect(r.x+40,y+9,510,28),lot.Name+" · "+lot.Cells+" cases",text);
            GUI.Label(new Rect(r.x+40,y+41,510,30),State.OwnsLand(i)?"À toi · Terre prête à être labourée":"Prix : "+lot.Price+" pièces · Labourage disponible après achat",small);
            bool enabled=GUI.enabled;GUI.enabled=State.OwnsLand(i)||State.Money>=lot.Price;
            if(GUI.Button(new Rect(r.x+574,y+18,206,46),State.OwnsLand(i)?"Aller à la parcelle":"Acheter · "+lot.Price+" pièces",landCursor==i?selected:button)){landCursor=i;LandAction();}
            GUI.enabled=enabled;
        }
        GUI.Label(new Rect(r.x+24,r.y+400,772,42),"Après achat : équipe la bêche dans la roue et fais un clic droit sur les nouvelles cases.\nLe personnage s'approche avant d'agir.",small);
        if(GUI.Button(new Rect(r.x+24,r.y+451,772,33),"Retour à la ferme",button))CloseView();
    }
    void DrawMouseHud(float w,float h)
    {
        bool enabled=GUI.enabled;GUI.enabled=view==View.Play;
        if(GUI.Button(new Rect(w-290,151,150,32),"Parcelles",button))OpenLand();
        GUI.enabled=view==View.Play&&!World.Indoors;
        if(GUI.Button(new Rect(w-290,195,130,32),"Caméra ‹",button))World.RotateCamera(-15);
        if(GUI.Button(new Rect(w-150,195,130,32),"Caméra ›",button))World.RotateCamera(15);
        GUI.enabled=view==View.Play;
        FarmInteraction context=World.Indoors?(World.NearWardrobe?FarmInteraction.Wardrobe:World.NearDecorChest?FarmInteraction.DecorChest:World.NearExit?FarmInteraction.Exit:FarmInteraction.None):World.NearHouse?FarmInteraction.House:World.NearWorkbench?FarmInteraction.Workbench:FarmInteraction.Shop;
        string label=context switch{FarmInteraction.House=>"Entrer dans la maison",FarmInteraction.Shop=>"Rejoindre le marchand",FarmInteraction.Wardrobe=>"Ouvrir l'armoire",FarmInteraction.DecorChest=>"Décorer la maison",FarmInteraction.Exit=>"Sortir de la maison",FarmInteraction.Workbench=>"Utiliser l'établi",_=>""};
        if(label.Length>0&&GUI.Button(new Rect(20,158,292,36),label,button))StartMouseInteraction(context);
        if(view==View.Play)for(int i=0;i<FarmLand.Lots.Length;i++)
        {
            var rect=LandLabelRect(i,w,h);if(rect.width<=0)continue;
            if(GUI.Button(rect,FarmLand.Lots[i].Name+" · "+FarmLand.Lots[i].Price,button))OpenLand(i);
        }
        GUI.enabled=enabled;
    }
}
