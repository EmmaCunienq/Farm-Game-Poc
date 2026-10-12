using UnityEngine;

public sealed class FarmPoc : MonoBehaviour
{
    enum View { Play, Wheel, Inventory, Shop, Sleep, Style }
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
    int wheelCursor,originalTool,cursor,tab,shopTab,quantity=1,styleCursor,houseStyle,outfitStyle;
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
        "Choisir une teinte pour la maison.",
        "Choisir une teinte pour la tenue."
    };
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch(){if(FindFirstObjectByType<FarmPoc>()==null)new GameObject("Farm POC").AddComponent<FarmPoc>();}
    void Awake()
    {
        State=new FarmSimulation();controls=new FarmInput();art=new FarmArt();
        World=new FarmWorld25D(art,State,transform);
        State.NewDay+=OnNewDay;
        Say("Trois cultures sont prêtes : carottes, radis et citrouille. Ouvre la roue d'outils !");
    }
    void OnNewDay(int day)=>Say("Jour "+day+" : une nouvelle journée commence.");
    void Update()
    {
        State.AdvanceRealSeconds(Time.unscaledDeltaTime);
        var frame=controls.Read(Time.unscaledTime);ProcessInput(frame);
        World.Update(Time.deltaTime,frame.Move,view==View.Play,frame.CameraStick,frame.Orbit,ActiveTool);
    }
    public void ProcessInput(FarmInputFrame f)
    {
        if(f.Cancel){if(view==View.Wheel)ActiveTool=(FarmTool)originalTool;view=View.Play;return;}
        if(f.Inventory){if(view==View.Inventory)view=View.Play;else OpenInventory();return;}
        switch(view)
        {
            case View.Play:
                if(f.DirectTool>=0&&f.DirectTool<6)ActiveTool=(FarmTool)f.DirectTool;
                if(f.Wheel){originalTool=(int)ActiveTool;wheelCursor=originalTool;wheelMouseReady=false;view=View.Wheel;return;}
                if(f.Sleep){view=View.Sleep;return;}
                if(f.Shop){OpenShop();return;}
                if(f.Confirm)UseTool();
                break;
            case View.Wheel:
                if(f.WheelDirection.sqrMagnitude>.16f)wheelCursor=FarmToolWheel.Select(f.WheelDirection,wheelCursor);
                else if(f.Navigate!=Vector2Int.zero)wheelCursor=(wheelCursor+(f.Navigate.x!=0?f.Navigate.x:-f.Navigate.y)+6)%6;
                if(f.Confirm||f.Wheel){ActiveTool=(FarmTool)wheelCursor;view=View.Play;}
                break;
            case View.Inventory:
                if(f.Tab!=0){tab=(tab+f.Tab+3)%3;cursor=0;}
                MoveCursor(f.Navigate,Items().Length);ChangeQuantity(f.Quantity);
                if(f.Confirm)InventoryAction();
                break;
            case View.Shop:
                if(f.Tab!=0){shopTab=(shopTab+f.Tab+2)%2;cursor=0;quantity=1;}
                MoveCursor(f.Navigate,3);ChangeQuantity(f.Quantity);
                if(f.Confirm)ShopAction();
                break;
            case View.Sleep: if(f.Confirm)Sleep();break;
            case View.Style:
                if(f.Navigate!=Vector2Int.zero)styleCursor=(styleCursor+(f.Navigate.x!=0?f.Navigate.x:-f.Navigate.y)+3)%3;
                if(f.Confirm)ApplyStyle();
                break;
        }
    }
    void UseTool()
    {
        if(ActiveTool==FarmTool.House||ActiveTool==FarmTool.Wardrobe)
        { styleCursor=ActiveTool==FarmTool.House?houseStyle:outfitStyle;view=View.Style;return; }
        var p=World.Target();string message;
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
        else if(nav.y!=0)cursor=(cursor-nav.y*3+count)%count;
        cursor=Mathf.Clamp(cursor,0,count-1);
    }
    void ChangeQuantity(int change)=>quantity=Mathf.Clamp(quantity+change,1,99);
    int[] Items()=>tab==0?new[]{0,1,2}:tab==1?new[]{3,4,5}:new[]{0,1,2,3,4,5};
    void InventoryAction()
    {
        int id=Items()[cursor],kind=id%3;
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
        else {State.SelectedProduct=cursor;State.Sell(cursor,quantity,out message);}
        Say(message);
    }
    void Sleep(){State.SkipToNextMorning();World.UpdateLighting();view=View.Play;Say("Jour "+State.Day+" — 08:00. Les cultures ont continué à pousser.");}
    void ApplyStyle()
    {
        Color[] shades={Color.white,new Color(1,.84f,.73f),new Color(.83f,.94f,.87f)};
        if(ActiveTool==FarmTool.House){houseStyle=styleCursor;World.SetHouseTint(shades[styleCursor]);}
        else{outfitStyle=styleCursor;World.SetOutfitTint(shades[styleCursor]);}
        view=View.Play;
    }
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
        var old=GUI.matrix;var oldColor=GUI.color;
        float scale=Mathf.Min(Screen.width/1200f,Screen.height/760f);if(scale<=0)return;
        float w=Screen.width/scale,h=Screen.height/scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        DrawHud(w,h);
        if(view!=View.Play)
        {
            GUI.color=new Color(0,0,0,.48f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=Color.white;
            if(view==View.Wheel)DrawWheel(w,h);
            if(view==View.Inventory||view==View.Shop)DrawInventory(w,h);
            if(view==View.Sleep)DrawSleep(w,h);
            if(view==View.Style)DrawStyle(w,h);
        }
        if(Time.unscaledTime<toastUntil)
        {
            var r=new Rect(w/2-390,h-167,780,44);GUI.Box(r,GUIContent.none,panel);
            GUI.Label(new Rect(r.x+14,r.y+10,r.width-28,30),toast,small);
        }
        GUI.color=oldColor;GUI.matrix=old;
    }
    void DrawHud(float w,float h)
    {
        GUI.Box(new Rect(20,20,292,129),GUIContent.none,panel);
        GUI.Label(new Rect(36,27,268,30),"LE PETIT MATIN",title);
        GUI.Label(new Rect(36,62,260,26),"JOUR "+State.Day+"  /  "+State.ClockText+(State.Hour>=6&&State.Hour<19?"  ·  JOUR":"  ·  NUIT"),text);
        GUI.Label(new Rect(36,91,260,23),State.Money+" pièces   ·   "+State.TotalProducts+" récoltes",small);
        GUI.Label(new Rect(36,116,260,23),"Graines : "+FarmSimulation.Crops[State.SelectedSeed].Name+" × "+State.Seeds[State.SelectedSeed],small);
        GUI.Box(new Rect(w-290,20,270,121),GUIContent.none,panel);
        GUI.Label(new Rect(w-274,31,242,104),controls.UsingGamepad?
            "Stick gauche : se déplacer\nA / Croix : agir ou valider\nX / Carré : roue d'outils\nY / Triangle : inventaire\nRB / R1 : marchand   ·   Start : dormir\nLT / L2 + stick droit : caméra":
            "ZQSD / WASD / flèches : se déplacer\nE / Espace : agir   ·   Tab : roue\nI : inventaire   ·   B : marchand\nN : dormir   ·   Échap : fermer\nAlt + C / V : tourner la caméra",small);
        float start=w/2-360;
        GUI.Box(new Rect(start,h-103,720,83),GUIContent.none,panel);
        GUI.Label(new Rect(start+16,h-95,215,28),"OUTIL · "+toolNames[(int)ActiveTool],text);
        string detail=ActiveTool==FarmTool.Seeds?FarmSimulation.Crops[State.SelectedSeed].Name+" · "+State.Seeds[State.SelectedSeed]+" graines":
            ActiveTool==FarmTool.Basket?"Vente : "+FarmSimulation.Crops[State.SelectedProduct].Name+" × "+quantity:"";
        GUI.Label(new Rect(start+16,h-65,215,30),detail,small);
        if(GUI.Button(new Rect(start+237,h-89,148,52),"Roue\nTab / X",button)){originalTool=(int)ActiveTool;wheelCursor=originalTool;wheelMouseReady=false;view=View.Wheel;}
        if(GUI.Button(new Rect(start+394,h-89,148,52),"Inventaire\nI / Y",button))OpenInventory();
        if(GUI.Button(new Rect(start+551,h-89,153,52),"Lendemain\nN / Start",button))view=View.Sleep;
        if(view==View.Play)
        {
            var p=World.Target();
            if(FarmSimulation.Inside(p.x,p.y)&&FarmSimulation.Cultivable(p.x,p.y))
            {
                var plot=State.Plots[p.x,p.y];
                string label=plot.Crop>=0?FarmSimulation.Crops[plot.Crop].Name+" · "+(plot.Stage==PlantStage.Mature?"prêt(e)":"pousse "+Mathf.RoundToInt((float)(plot.GrowthHours/FarmSimulation.Crops[plot.Crop].GrowthHours)*100)+" %"):
                    plot.Tilled?"Terre préparée · "+plot.UsesLeft+" récoltes restantes":"Terre naturelle · bêcher pour planter";
                GUI.Box(new Rect(w/2-210,h-205,420,29),GUIContent.none,panel);GUI.Label(new Rect(w/2-195,h-202,390,23),label,small);
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
        string[] tabs=shop?new[]{"Acheter des graines","Vendre les récoltes"}:new[]{"Graines","Récoltes","Tout"};
        int active=shop?shopTab:tab;
        for(int i=0;i<tabs.Length;i++)
            if(GUI.Button(new Rect(r.x+24+i*245,r.y+68,235,38),tabs[i],active==i?selected:button))
            {if(shop)shopTab=i;else tab=i;cursor=0;quantity=1;}
        int[] list=shop?(shopTab==0?new[]{0,1,2}:new[]{3,4,5}):Items();
        cursor=Mathf.Clamp(cursor,0,list.Length-1);
        for(int i=0;i<list.Length;i++)
        {
            int item=list[i],kind=item%3;var cell=new Rect(r.x+24+(i%3)*245,r.y+124+(i/3)*108,235,98);
            int count=item<3?State.Seeds[kind]:State.Products[kind];
            if(GUI.Button(cell,"",i==cursor?selected:button))cursor=i;
            CropIcon(new Rect(cell.x+10,cell.y+12,64,67),kind);
            var style=i==cursor?text:new GUIStyle(text);if(i!=cursor)style.normal.textColor=new Color(.20f,.23f,.18f);
            GUI.Label(new Rect(cell.x+82,cell.y+12,145,69),(item<3?"Graines":"Récolte")+"\n"+FarmSimulation.Crops[kind].Name+"\n× "+count,style);
        }
        int id=list[cursor],selectedKind=id%3;var def=FarmSimulation.Crops[selectedKind];
        string info=id<3?"Pousse : "+def.GrowthHours+" h   ·   "+def.Yield+" produit(s) par récolte   ·   Graine : "+def.SeedPrice+" pièces":
            "Vente : "+def.SalePrice+" pièces par produit   ·   Total possédé : "+State.Products[selectedKind];
        GUI.Label(new Rect(r.x+24,r.y+350,752,29),info,small);
        GUI.Label(new Rect(r.x+24,r.y+395,100,28),"Quantité",text);
        if(GUI.Button(new Rect(r.x+128,r.y+386,38,38),"−",button))ChangeQuantity(-1);
        GUI.Label(new Rect(r.x+172,r.y+391,55,29),quantity.ToString(),center);
        if(GUI.Button(new Rect(r.x+232,r.y+386,38,38),"+",button))ChangeQuantity(1);
        string action=shop?(shopTab==0?"Acheter "+quantity:"Vendre "+quantity):id<3?"Équiper les graines":World.NearShop?"Vendre "+quantity:"Choisir pour la vente";
        if(GUI.Button(new Rect(r.x+330,r.y+382,446,48),action+"  ·  A / E",selected)){if(shop)ShopAction();else InventoryAction();}
        GUI.Label(new Rect(r.x+24,r.y+448,752,26),"Stick / flèches : naviguer   ·   LB/RB ou Page↑/↓ : onglets   ·   LT/RT ou −/+ : quantité",small);
    }
    void CropIcon(Rect target,int kind)
    {
        var sprite=art.CropSprite(kind);var uv=new Rect(sprite.rect.x/sprite.texture.width,sprite.rect.y/sprite.texture.height,sprite.rect.width/sprite.texture.width,sprite.rect.height/sprite.texture.height);
        float ratio=sprite.rect.width/sprite.rect.height;float width=Mathf.Min(target.width,target.height*ratio),height=width/ratio;
        var rect=new Rect(target.center.x-width/2,target.center.y-height/2,width,height);GUI.DrawTextureWithTexCoords(rect,sprite.texture,uv,true);
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
    void DrawStyle(float w,float h)
    {
        var r=new Rect(w/2-285,h/2-135,570,270);GUI.Box(r,GUIContent.none,panel);
        GUI.Label(new Rect(r.x+24,r.y+22,520,36),ActiveTool==FarmTool.House?"LA MAISON":"L'ARMOIRE",title);
        string[] styles={"Naturelle","Chaleureuse","Sauge"};
        for(int i=0;i<3;i++)if(GUI.Button(new Rect(r.x+24+i*177,r.y+100,167,55),styles[i],styleCursor==i?selected:button))styleCursor=i;
        if(GUI.Button(new Rect(r.x+24,r.y+178,522,42),"Appliquer · A / E",selected))ApplyStyle();
    }
    void OnDestroy()
    {
        if(State!=null)State.NewDay-=OnNewDay;
        if(World!=null)World.Dispose();if(art!=null)art.Dispose();
    }
}
