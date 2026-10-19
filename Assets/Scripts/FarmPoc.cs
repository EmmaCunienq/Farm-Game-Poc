using UnityEngine;

public sealed partial class FarmPoc : MonoBehaviour
{
    enum View { Play, Wheel, Inventory, Shop, Sleep, Style, Decoration, Pause, LoadConfirm, QuitConfirm, Land, Craft }
    public FarmSimulation State { get; private set; }
    public FarmWorld25D World { get; private set; }
    public FarmTool ActiveTool { get; private set; }=FarmTool.Hoe;
    public string CurrentScreen=>view.ToString();
    public int WheelCursor=>wheelCursor;
    public int InventoryCursor=>cursor;
    public int InventoryTab=>tab;
    FarmArt art;
    FarmInput controls;
    View view;
    Vector2 lastWheelMouse;
    bool wheelMouseReady;
    int wheelCursor,originalTool,cursor,tab,shopTab,quantity=1;
    string toast;
    float toastUntil;
    GUIStyle panel,title,text,small,button,selected,center;
    readonly Color paper=new(.92f,.86f,.71f);
    readonly string[] toolNames={"Bêche","Graines","Arrosoir","Panier","Maison","Armoire"};
    readonly string[] toolDescriptions={
        "Préparer ou renouveler la terre.",
        "Planter les graines choisies dans l'inventaire.",
        "Accélérer la pousse une fois par jour.",
        "Récolter ; vendre le produit choisi près du marchand.",
        "Décorer la ferme dehors, ou la pièce depuis l'intérieur.",
        "Choisir ses traits et ses vêtements dans l'armoire."
    };
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch(){if(FindFirstObjectByType<FarmPoc>()==null)new GameObject("Farm POC").AddComponent<FarmPoc>();}
    void Awake()
    {
        State=new FarmSimulation();controls=new FarmInput();art=new FarmArt();InitializeSave();
        World=new FarmWorld25D(art,State,transform);
        World.ApplyHome(Home);World.ApplyAppearance(Appearance);RestorePlayerAtLaunch();
        State.NewDay+=OnNewDay;
        Say(startupMessage);
    }
    void OnNewDay(int day)=>Say("Jour "+day+" : une nouvelle journée commence.");
    void Update()
    {
        var frame=controls.Read(Time.unscaledTime);ProcessInput(frame);
        if(IsPaused){CancelMouseNavigation();return;}
        State.AdvanceRealSeconds(Time.unscaledDeltaTime);UpdateCrafting(Time.unscaledDeltaTime);UpdateBuild(frame,Time.unscaledDeltaTime);
        UpdateMouseAim(frame);var movement=MouseMovement(frame.Move,Time.deltaTime);
        World.Update(Time.deltaTime,movement,view==View.Play,frame.CameraStick,frame.Orbit,ActiveTool);
        if(view==View.Play&&Time.unscaledTime>=nextAutosave){SaveGame(false);nextAutosave=Time.unscaledTime+60;}
    }
    public void ProcessInput(FarmInputFrame f)
    {
        if(f.Pause){if(IsPaused)Resume();else OpenPause();return;}
        if(IsPaused){ProcessPause(f);return;}
        if(f.Cancel&&view==View.Decoration&&buildPlacing){BackToCatalog();return;}
        if(f.Cancel){if(view==View.Play){OpenPause();return;}CloseView();return;}
        if(f.Inventory&&(view==View.Play||view==View.Inventory)){if(view==View.Inventory)view=View.Play;else OpenInventory();return;}
        switch(view)
        {
            case View.Play:
                if(f.DirectTool>=0&&f.DirectTool<6){CancelMouseNavigation();ActiveTool=(FarmTool)f.DirectTool;}
                if(f.Wheel){originalTool=(int)ActiveTool;wheelCursor=originalTool;wheelMouseReady=false;view=View.Wheel;return;}
                if(f.Sleep){view=View.Sleep;return;}
                if(f.Shop){OpenShop();return;}
                if(f.Build){OpenDecoration();return;}
                if(f.Confirm){CancelMouseNavigation();UseTool();}
                break;
            case View.Wheel:
                if(f.WheelDirection.sqrMagnitude>.16f)wheelCursor=FarmToolWheel.Select(f.WheelDirection,wheelCursor);
                else if(f.Navigate!=Vector2Int.zero)wheelCursor=(wheelCursor+(f.Navigate.x!=0?f.Navigate.x:-f.Navigate.y)+6)%6;
                if(f.Confirm||f.Wheel){ActiveTool=(FarmTool)wheelCursor;view=View.Play;}
                break;
            case View.Inventory:
                if(f.Tab!=0){tab=(tab+f.Tab+4)%4;cursor=0;}
                MoveCursor(f.Navigate,Items().Length);ChangeQuantity(f.Quantity);
                if(f.Confirm)InventoryAction();
                break;
            case View.Shop:
                if(f.Tab!=0){shopTab=(shopTab+f.Tab+3)%3;cursor=0;quantity=1;}
                MoveCursor(f.Navigate,ShopItems().Length);ChangeQuantity(f.Quantity);
                if(f.Confirm)ShopAction();
                break;
            case View.Sleep: if(f.Confirm)Sleep();break;
            case View.Style:
                if(f.Navigate.y!=0)appearanceCategory=(appearanceCategory-f.Navigate.y+8)%8;
                if(f.Navigate.x!=0)CycleAppearance(f.Navigate.x);
                if(f.Tab!=0)appearanceCategory=(appearanceCategory+f.Tab+8)%8;
                if(f.Confirm)ApplyAppearance();
                break;
            case View.Decoration:ProcessDecoration(f);break;
            case View.Land:
                if(f.Navigate!=Vector2Int.zero)landCursor=(landCursor+(f.Navigate.y!=0?-f.Navigate.y:f.Navigate.x)+FarmLand.Lots.Length)%FarmLand.Lots.Length;
                if(f.Confirm)LandAction();break;
            case View.Craft:ProcessCrafting(f);break;
        }
    }
    void UseTool()
    {
        if(World.NearHouse){World.SetIndoors(true);Say("Bienvenue chez toi ! Le coffre décore la pièce et l'armoire change ton personnage.");SaveGame(false);return;}
        if(World.NearExit){World.SetIndoors(false);SaveGame(false);return;}
        if(World.NearWardrobe){OpenAppearance();return;}
        if(World.NearDecorChest){OpenDecoration();return;}
        // Près des champs, l'outil reste prioritaire sur l'établi.
        var aimed=World.Target();if(World.NearWorkbench&&!State.CanCultivate(aimed.x,aimed.y)){OpenCrafting();return;}
        if(ActiveTool==FarmTool.House||ActiveTool==FarmTool.Wardrobe)
        {
            if(!World.Indoors&&ActiveTool==FarmTool.House){OpenDecoration();return;}
            if(!World.Indoors){Say("Entre dans la maison par la porte pour accéder à l'armoire.");return;}
            if(ActiveTool==FarmTool.House)OpenDecoration();else OpenAppearance();return;
        }
        if(World.Indoors){Say("Utilise le coffre pour décorer, l'armoire pour te changer, ou le seuil pour sortir.");return;}
        UseFarmToolAt(World.Target());
    }
    void UseFarmToolAt(Vector2Int p)
    {
        string message;
        switch(ActiveTool)
        {
            case FarmTool.Hoe:State.Till(p.x,p.y,out message);break;
            case FarmTool.Seeds:State.Plant(p.x,p.y,out message);break;
            case FarmTool.Water:State.Water(p.x,p.y,out message);break;
            default:
                if(FarmSimulation.Inside(p.x,p.y)&&State.Plots[p.x,p.y].Stage==PlantStage.Mature)State.Harvest(p.x,p.y,out message);
                else if(World.NearShop)State.Sell(State.SelectedProduct,quantity,out message);
                else message="Vise une culture mûre ; pour vendre, choisis le produit dans l'inventaire et rejoins le marchand.";
                break;
        }
        World.TriggerAction();Say(message);
    }
    void OpenInventory(){view=View.Inventory;tab=0;cursor=State.SelectedSeed;quantity=1;}
    void OpenShop()
    {
        if(!World.NearShop){Say("Approche-toi du marchand, en haut à droite, pour acheter ou vendre.");return;}
        view=View.Shop;shopTab=0;cursor=State.SelectedSeed;quantity=1;
    }
    void MoveCursor(Vector2Int nav,int count)
    {
        if(count==0){cursor=0;return;}
        if(nav.x!=0)cursor=(cursor+nav.x+count)%count;
        else if(nav.y!=0)cursor=(cursor-nav.y*Columns(count)+count)%count;
        cursor=Mathf.Clamp(cursor,0,count-1);
    }
    void ChangeQuantity(int change)=>quantity=Mathf.Clamp(quantity+change,1,99);
    int[] Items()=>tab==0?new[]{0,1,2}:tab==1?new[]{3,4,5}:tab==2?ObjectItems:new[]{0,1,2,3,4,5,6,7,8,9,10,11};
    int[] ShopItems()=>shopTab==0?new[]{0,1,2}:shopTab==1?new[]{3,4,5}:ObjectItems;
    void InventoryAction()
    {
        int id=Items()[cursor],kind=id%3;
        if(id>=6)
        {
            if(World.NearShop){State.SellCrafted(id-6,quantity,out var sold);Say(sold);}
            else if(!World.Indoors&&FarmCrafting.Recipes[id-6].Placeable)OpenFarmPlacement(id-6);
            else Say(ItemName(id)+" : rejoins le marchand pour le vendre.");
            return;
        }
        if(id<3){State.SelectedSeed=kind;ActiveTool=FarmTool.Seeds;view=View.Play;Say("Graines choisies : "+FarmSimulation.Crops[kind].Name+". Vise une case bêchée pour planter.");}
        else
        {
            State.SelectedProduct=kind;
            if(World.NearShop){State.Sell(kind,quantity,out var message);Say(message);}
            else {ActiveTool=FarmTool.Basket;view=View.Play;Say(FarmSimulation.Crops[kind].Name+" sélectionné(e) pour la vente. Rejoins le marchand.");}
        }
    }
    void ShopAction()
    {
        if(!World.NearShop){view=View.Play;Say("Le marchand est trop loin.");return;}
        string message;
        if(shopTab==0)State.BuySeeds(cursor,quantity,out message);
        else if(shopTab==1){State.SelectedProduct=cursor;State.Sell(cursor,quantity,out message);}
        else State.SellCrafted(cursor,quantity,out message);
        Say(message);
    }
    void Sleep(){State.SkipToNextMorning();World.UpdateLighting();view=View.Play;SaveGame(false);Say("Jour "+State.Day+" — 08:00. Les cultures ont continué à pousser.");}
    void Say(string message){toast=message;toastUntil=Time.unscaledTime+4.5f;}
    void Styles()
    {
        if(panel!=null)return;
        var dark=art.Panel(new Color(.14f,.20f,.18f,.97f),new Color(.07f,.11f,.10f));
        var parchment=art.Panel(new Color(.89f,.82f,.67f),new Color(.34f,.29f,.22f));
        var clay=art.Panel(new Color(.66f,.34f,.23f),new Color(.33f,.20f,.15f));
        panel=new GUIStyle(GUI.skin.box){border=new RectOffset(8,8,8,8)};panel.normal.background=dark;
        title=new GUIStyle(GUI.skin.label){fontSize=21,fontStyle=FontStyle.Bold};title.normal.textColor=paper;
        text=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true};text.normal.textColor=paper;
        small=new GUIStyle(text){fontSize=12};small.normal.textColor=new Color(.75f,.80f,.67f);
        center=new GUIStyle(text){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
        button=new GUIStyle(GUI.skin.button){fontSize=14,fontStyle=FontStyle.Bold,border=new RectOffset(8,8,8,8)};
        foreach(var state in new[]{button.normal,button.hover,button.active,button.focused}){state.background=parchment;state.textColor=new Color(.20f,.23f,.18f);}
        selected=new GUIStyle(button);foreach(var state in new[]{selected.normal,selected.hover,selected.active,selected.focused}){state.background=clay;state.textColor=paper;}
    }
    void OnGUI()
    {
        if(art==null||State==null)return;Styles();GUIUtility.keyboardControl=0;
        Vector2 screenPointer=new Vector2(Event.current.mousePosition.x,Screen.height-Event.current.mousePosition.y);
        var old=GUI.matrix;var oldColor=GUI.color;
        float scale=Mathf.Min(Screen.width/1200f,Screen.height/760f);if(scale<=0)return;
        float w=Screen.width/scale,h=Screen.height/scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        DrawHud(w,h);DrawMouseHud(w,h);
        if(view!=View.Play)
        {
            // Pendant une pose, le voile s'efface pour laisser voir le décor.
            GUI.color=new Color(0,0,0,.48f*(view==View.Decoration?1-Mathf.SmoothStep(0,1,buildSlide):1));GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=Color.white;
            if(view==View.Wheel)DrawWheel(w,h);
            if(view==View.Inventory||view==View.Shop)DrawInventory(w,h);
            if(view==View.Sleep)DrawSleep(w,h);
            if(view==View.Style)DrawAppearance(w,h);
            if(view==View.Decoration)DrawDecoration(w,h);
            if(view==View.Land)DrawLand(w,h);
            if(view==View.Craft)DrawCrafting(w,h);
            if(IsPaused)DrawPause(w,h);
        }
        if(Time.unscaledTime<toastUntil)
        {
            var r=new Rect(w/2-280,20,560,50);GUI.Box(r,GUIContent.none,panel);
            GUI.Label(new Rect(r.x+14,r.y+8,r.width-28,36),toast,small);
        }
        GUI.color=oldColor;GUI.matrix=old;
        if(Event.current.type==EventType.MouseDown&&(Event.current.button==0||Event.current.button==1)&&GUIUtility.hotControl==0)
        {if(ProcessPointerClick(screenPointer,Event.current.button==1))Event.current.Use();}
    }
    void DrawHud(float w,float h)
    {
        GUI.Box(new Rect(20,20,292,129),GUIContent.none,panel);
        GUI.Label(new Rect(36,27,268,30),"LE PETIT MATIN",title);
        GUI.Label(new Rect(36,62,260,26),"JOUR "+State.Day+"  /  "+State.ClockText+(State.Hour>=6&&State.Hour<19?"  ·  JOUR":"  ·  NUIT"),text);
        GUI.Label(new Rect(36,91,260,23),State.Money+" pièces  ·  "+State.TotalProducts+" récoltes  ·  "+State.TotalCrafted+" objets",small);
        GUI.Label(new Rect(36,116,260,23),"Graines : "+FarmSimulation.Crops[State.SelectedSeed].Name+" × "+State.Seeds[State.SelectedSeed],small);
        GUI.Box(new Rect(w-290,20,270,121),GUIContent.none,panel);
        GUI.Label(new Rect(w-274,31,242,104),controls.UsingGamepad?
            "Stick gauche : se déplacer\nA / Croix : agir ou valider\nX / Carré : roue d'outils\nY / Triangle : inventaire\nRB / R1 : marchand   ·   Start : pause\nLT / L2 + stick droit : caméra":
            "Clic gauche : se déplacer / interagir\nClic droit : utiliser l'outil choisi\nRoue : choisir un outil\nParcelles : agrandir la ferme\nMolette maintenue : tourner la caméra\nClic droit dans un menu : fermer",small);
        bool enabled=GUI.enabled;GUI.enabled=!IsPaused;
        if(GUI.Button(new Rect(w-130,151,110,32),"Pause",button))OpenPause();
        float start=w/2-360;
        GUI.Box(new Rect(start,h-103,720,83),GUIContent.none,panel);
        GUI.Label(new Rect(start+16,h-95,215,28),"OUTIL · "+toolNames[(int)ActiveTool],text);
        string detail=ActiveTool==FarmTool.Seeds?FarmSimulation.Crops[State.SelectedSeed].Name+" · "+State.Seeds[State.SelectedSeed]+" graines":
            ActiveTool==FarmTool.Basket?"Vente : "+FarmSimulation.Crops[State.SelectedProduct].Name+" × "+quantity:"";
        GUI.Label(new Rect(start+16,h-65,215,30),detail,small);
        GUI.enabled=view==View.Play;
        if(GUI.Button(new Rect(start+237,h-89,112,52),"Roue\nTab / X",button)){originalTool=(int)ActiveTool;wheelCursor=originalTool;wheelMouseReady=false;view=View.Wheel;}
        if(GUI.Button(new Rect(start+355,h-89,112,52),"Inventaire\nI / Y",button))OpenInventory();
        if(GUI.Button(new Rect(start+473,h-89,112,52),"Construire\nG",button))OpenDecoration();
        if(GUI.Button(new Rect(start+591,h-89,113,52),"Lendemain\nN",button))view=View.Sleep;
        GUI.enabled=enabled;
        if(view==View.Play)
        {
            var p=World.AimedPlot;
            string hint=World.InteractionHint;
            if(hint.Length>0){GUI.Box(new Rect(20,204,292,62),GUIContent.none,panel);GUI.Label(new Rect(32,212,268,50),hint,small);}
            else if(!World.Indoors&&FarmSimulation.Inside(p.x,p.y)&&State.CanCultivate(p.x,p.y))
            {
                var plot=State.Plots[p.x,p.y];
                string label=plot.Crop>=0?FarmSimulation.Crops[plot.Crop].Name+" · "+(plot.Stage==PlantStage.Mature?"prêt(e)":"pousse "+Mathf.RoundToInt((float)(plot.GrowthHours/FarmSimulation.Crops[plot.Crop].GrowthHours)*100)+" %"):
                    plot.Tilled?"Terre préparée · "+plot.UsesLeft+" récoltes restantes":"Terre naturelle · bêcher pour planter";
                GUI.Box(new Rect(20,204,292,62),GUIContent.none,panel);GUI.Label(new Rect(32,212,268,50),label,small);
            }
        }
    }
    void DrawWheel(float w,float h)
    {
        Vector2 c=new Vector2(w/2,h/2);float radius=178;
        if(!controls.UsingGamepad)
        {
            var mouse=Event.current.mousePosition;var local=mouse-c;var d=new Vector2(local.x,-local.y);
            if(wheelMouseReady&&(mouse-lastWheelMouse).sqrMagnitude>4&&d.magnitude>82&&d.magnitude<265)wheelCursor=FarmToolWheel.Select(d,wheelCursor);
            lastWheelMouse=mouse;wheelMouseReady=true;
        }
        GUI.DrawTexture(new Rect(c.x-126,c.y-126,252,252),art.Marker.texture);
        GUI.Box(new Rect(c.x-122,c.y-70,244,140),GUIContent.none,panel);
        if(GUI.Button(new Rect(c.x+220,c.y-250,95,32),"Fermer",button)){CloseView();return;}
        GUI.Label(new Rect(c.x-110,c.y-58,220,29),toolNames[wheelCursor],center);
        GUI.Label(new Rect(c.x-110,c.y-20,220,68),toolDescriptions[wheelCursor],small);
        for(int i=0;i<6;i++)
        {
            float angle=i*Mathf.PI/3;var p=c+new Vector2(Mathf.Sin(angle),-Mathf.Cos(angle))*radius;
            if(GUI.Button(new Rect(p.x-76,p.y-30,152,60),(i+1)+" · "+toolNames[i],i==wheelCursor?selected:button))
            {wheelCursor=i;ActiveTool=(FarmTool)i;view=View.Play;}
        }
        GUI.Label(new Rect(c.x-250,c.y+267,500,40),controls.UsingGamepad?"Stick : choisir   ·   A : équiper   ·   B : annuler":"Souris / flèches : choisir   ·   E : équiper   ·   Échap : annuler",center);
    }
    void DrawInventory(float w,float h)
    {
        bool shop=view==View.Shop;var r=new Rect(w/2-400,h/2-245,800,490);
        GUI.Box(r,GUIContent.none,panel);GUI.Label(new Rect(r.x+24,r.y+18,650,33),shop?"CHEZ LE MARCHAND":"MON INVENTAIRE",title);
        if(GUI.Button(new Rect(r.xMax-62,r.y+18,38,31),"×",button)){view=View.Play;return;}
        string[] tabs=shop?new[]{"Acheter des graines","Vendre les récoltes","Vendre les objets"}:new[]{"Graines","Récoltes","Objets","Tout"};
        int active=shop?shopTab:tab;float tabWidth=(752-(tabs.Length-1)*10f)/tabs.Length;
        for(int i=0;i<tabs.Length;i++)
            if(GUI.Button(new Rect(r.x+24+i*(tabWidth+10),r.y+68,tabWidth,38),tabs[i],active==i?selected:button))
            {if(shop)shopTab=i;else tab=i;cursor=0;quantity=1;}
        int[] list=shop?ShopItems():Items();
        int columns=Columns(list.Length);bool compact=list.Length>6;
        float cellWidth=(752-(columns-1)*10f)/columns,cellHeight=compact?68:98;
        cursor=Mathf.Clamp(cursor,0,list.Length-1);
        for(int i=0;i<list.Length;i++)
        {
            int item=list[i];var cell=new Rect(r.x+24+(i%columns)*(cellWidth+10),r.y+124+(i/columns)*(cellHeight+10),cellWidth,cellHeight);
            if(GUI.Button(cell,"",i==cursor?selected:button))cursor=i;
            float icon=cellHeight*.66f;SpriteIcon(new Rect(cell.x+10,cell.y+(cellHeight-icon)/2,icon,icon),ItemSprite(item));
            var style=new GUIStyle(compact?small:text);style.normal.textColor=i==cursor?paper:new Color(.20f,.23f,.18f);
            GUI.Label(new Rect(cell.x+icon+18,cell.y+(compact?6:12),cell.width-icon-24,cellHeight-12),ItemCategory(item)+"\n"+ItemName(item)+"\n× "+ItemCount(item),style);
        }
        int id=list[cursor],selectedKind=id%3;var def=FarmSimulation.Crops[selectedKind];
        string info=id<3?"Pousse : "+def.GrowthHours+" h   ·   "+def.Yield+" produit(s) par récolte   ·   Graine : "+def.SeedPrice+" pièces":
            id<6?"Vente : "+def.SalePrice+" pièces par produit   ·   Total possédé : "+State.Products[selectedKind]:
            FarmCrafting.Recipes[id-6].Description+"   ·   Vente : "+FarmCrafting.Recipes[id-6].SalePrice+" pièces";
        GUI.Label(new Rect(r.x+24,r.y+350,752,29),info,small);
        GUI.Label(new Rect(r.x+24,r.y+395,100,28),"Quantité",text);
        if(GUI.Button(new Rect(r.x+128,r.y+386,38,38),"−",button))ChangeQuantity(-1);
        GUI.Label(new Rect(r.x+172,r.y+391,55,29),quantity.ToString(),center);
        if(GUI.Button(new Rect(r.x+232,r.y+386,38,38),"+",button))ChangeQuantity(1);
        string action=shop?(shopTab==0?"Acheter "+quantity:"Vendre "+quantity):id<3?"Équiper les graines":World.NearShop?"Vendre "+quantity:id<6?"Choisir pour la vente":!World.Indoors&&FarmCrafting.Recipes[id-6].Placeable?"Placer dans la ferme":"À vendre chez le marchand";
        if(GUI.Button(new Rect(r.x+330,r.y+382,446,48),action+"  ·  A / E",selected)){if(shop)ShopAction();else InventoryAction();}
        GUI.Label(new Rect(r.x+24,r.y+448,752,26),"Stick / flèches : naviguer   ·   LB/RB ou Page↑/↓ : onglets   ·   LT/RT ou −/+ : quantité",small);
    }
    void DrawSleep(float w,float h)
    {
        var r=new Rect(w/2-250,h/2-135,500,270);GUI.Box(r,GUIContent.none,panel);
        GUI.Label(new Rect(r.x+24,r.y+22,450,36),"PASSER AU LENDEMAIN",title);
        GUI.Label(new Rect(r.x+24,r.y+76,452,67),"Les cultures continueront de pousser.\nTu te réveilleras au jour "+(State.Day+1)+", à 08:00.",text);
        if(GUI.Button(new Rect(r.x+24,r.y+166,215,45),"Dormir · A / E",selected))Sleep();
        if(GUI.Button(new Rect(r.x+261,r.y+166,215,45),"Annuler · B / Échap",button))view=View.Play;
        GUI.Label(new Rect(r.x+24,r.y+231,450,24),"Une journée complète dure dix minutes réelles.",small);
    }
    void OnDestroy()
    {
        if(IsPaused)Time.timeScale=previousTimeScale;
        if(State!=null)State.NewDay-=OnNewDay;
        if(World!=null)World.Dispose();if(art!=null)art.Dispose();
    }
}
