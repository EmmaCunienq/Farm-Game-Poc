using System.IO;
using UnityEngine;

public sealed partial class FarmPoc
{
    public FarmHome Home { get; private set; }=FarmHome.Starter();
    public FarmAppearance Appearance { get; private set; }=new();
    public bool IsPaused=>view==View.Pause||view==View.LoadConfirm||view==View.QuitConfirm;
    public string SaveFilePath=>storage.Path;
    FarmSave storage;
    FarmSaveData startupSave;
    string startupMessage="Clic gauche : se déplacer ou interagir. Clic droit : utiliser l'outil. Parcelles : agrandir ta ferme.";
    float nextAutosave,previousTimeScale=1;
    bool allowAutosave=true,quitSaved;
    View beforePause;
    int pauseCursor,appearanceCategory;
    FarmAppearance appearanceDraft;
    int decorTab,decorKind,decorRotation,decorX=3,decorZ=3,movingFurniture=-1;
    bool decorSelecting;
    string decorMessage="Choisis un meuble, puis une case et valide pour le poser.";
    static readonly string[] furnitureLabels={"Lit","Tab.","Cha.","Can.","Bib.","Pl.","Lam.","Tap."};

    void InitializeSave()
    {
        storage=new FarmSave();nextAutosave=Time.unscaledTime+60;
        if(Application.isPlaying&&storage.Exists)
        {
            if(storage.Read(out startupSave,out var message))
            {State.Restore(startupSave);Home=startupSave.Home;Appearance=startupSave.Appearance;ActiveTool=(FarmTool)startupSave.ActiveTool;startupMessage=message;}
            else{allowAutosave=false;startupMessage=message+" La sauvegarde automatique est suspendue ; consulte le menu pause.";}
        }
    }
    void RestorePlayerAtLaunch(){if(startupSave!=null)RestorePosition(startupSave);}
    void RestorePosition(FarmSaveData data)
    {
        World.SetIndoors(data.Indoors,false);
        var pos=data.PlayerPosition;pos.y=data.Indoors?.03f:FarmWorld25D.GroundHeight(pos.x,pos.z)+.03f;
        // Une ancienne position au contact d'un meuble revient sur le seuil libre.
        if(data.Indoors)foreach(var item in Home.Furniture)
            if(item.Kind!=7&&pos.x>FarmWorld25D.HomeOffsetX+item.X-.24f&&pos.x<FarmWorld25D.HomeOffsetX+item.X+item.Width+.24f&&pos.z>item.Z-.24f&&pos.z<item.Z+item.Depth+.24f){pos=new Vector3(FarmWorld25D.HomeOffsetX+4.5f,.03f,1.5f);break;}
        World.Teleport(pos,data.Facing);World.Update(0,Vector2.zero,false,Vector2.zero,false,ActiveTool);
    }
    public bool SaveGame(bool manual=true)
    {
        if(!manual&&!allowAutosave)return false;
        var data=new FarmSaveData{Home=Home,Appearance=Appearance,ActiveTool=(int)ActiveTool,Indoors=World.Indoors,PlayerPosition=World.Player.position,Facing=World.Facing};
        State.Export(data);
        bool ok=storage.Write(data,out var message);
        if(ok){allowAutosave=true;nextAutosave=Time.unscaledTime+60;}
        if(manual||!ok)Say(message);
        return ok;
    }
    public bool LoadGame(bool backup=false)
    {
        if(!storage.Read(out var data,out var message,backup)){Say(message);return false;}
        CancelMouseNavigation();
        if(IsPaused)Resume();CloseView(false);
        State.Restore(data);Home=data.Home;Appearance=data.Appearance;ActiveTool=(FarmTool)data.ActiveTool;
        World.ApplyHome(Home);World.ApplyAppearance(Appearance);RestorePosition(data);
        allowAutosave=true;nextAutosave=Time.unscaledTime+60;view=View.Play;Say(message);return true;
    }
    void OnApplicationQuit(){if(Application.isPlaying&&!quitSaved&&World!=null)SaveGame(false);}
    void CloseView(bool saveDecoration=true)
    {
        if(view==View.Wheel)ActiveTool=(FarmTool)originalTool;
        if(view==View.Style)World.ApplyAppearance(Appearance);
        if(view==View.Craft)StopCrafting();
        if(view==View.Decoration){movingFurniture=-1;if(saveDecoration)SaveGame(false);}
        view=View.Play;
    }
    void OpenPause()
    {
        if(IsPaused)return;CancelMouseNavigation();beforePause=view;view=View.Pause;pauseCursor=0;
        previousTimeScale=Time.timeScale;Time.timeScale=0;
    }
    void Resume(){Time.timeScale=previousTimeScale;view=beforePause;nextAutosave=Time.unscaledTime+60;}
    void ProcessPause(FarmInputFrame f)
    {
        if(f.Cancel){if(view==View.Pause)Resume();else view=View.Pause;return;}
        if(view==View.LoadConfirm||view==View.QuitConfirm){if(f.Confirm){if(view==View.LoadConfirm)LoadGame(loadBackup);else SaveAndQuit();}return;}
        if(f.Navigate!=Vector2Int.zero)pauseCursor=(pauseCursor+(f.Navigate.y!=0?-f.Navigate.y:f.Navigate.x)+6)%6;
        if(f.Confirm)PauseAction(pauseCursor);
    }
    bool loadBackup;
    void PauseAction(int action)
    {
        switch(action)
        {
            case 0:Resume();break;
            case 1:SaveGame();break;
            case 2:loadBackup=false;view=View.LoadConfirm;break;
            case 3:loadBackup=true;view=View.LoadConfirm;break;
            case 4:Resume();CloseView();view=View.Sleep;break;
            case 5:view=View.QuitConfirm;break;
        }
    }
    void SaveAndQuit()
    {
        if(!SaveGame())return;quitSaved=true;Resume();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit();
#endif
    }
    void DrawPause(float w,float h)
    {
        var r=new Rect(w/2-300,h/2-270,600,540);GUI.Box(r,GUIContent.none,panel);
        GUI.Label(new Rect(r.x+24,r.y+20,550,32),"PAUSE",title);
        if(view==View.LoadConfirm||view==View.QuitConfirm)
        {
            bool load=view==View.LoadConfirm;
            GUI.Label(new Rect(r.x+24,r.y+95,550,125),load?"Recharger "+(loadBackup?"la copie de secours":"la dernière sauvegarde")+" ?\nLes changements depuis cette sauvegarde seront perdus.":"Sauvegarder et quitter ?\nTa ferme, ta maison et ton personnage seront conservés.",text);
            if(GUI.Button(new Rect(r.x+24,r.y+260,550,48),"Confirmer · E / A",selected)){if(load)LoadGame(loadBackup);else SaveAndQuit();}
            if(GUI.Button(new Rect(r.x+24,r.y+325,550,48),"Retour · Échap / B",button))view=View.Pause;
            return;
        }
        GUI.Label(new Rect(r.x+24,r.y+61,550,28),"Le temps, les cultures et le personnage sont arrêtés.",small);
        string[] actions={"Reprendre","Sauvegarder","Charger la sauvegarde","Restaurer la copie de secours","Passer au lendemain","Sauvegarder et quitter"};
        for(int i=0;i<actions.Length;i++)
        {
            bool enabled=GUI.enabled;GUI.enabled=i==2?storage.Exists:i==3?File.Exists(storage.Path+".bak"):enabled;
            if(GUI.Button(new Rect(r.x+24,r.y+105+i*53,550,44),actions[i],pauseCursor==i?selected:button))PauseAction(i);
            GUI.enabled=enabled;
        }
        GUI.Label(new Rect(r.x+24,r.y+435,550,60),allowAutosave?"Sauvegarde automatique chaque minute et à la sortie de la maison.\nFichier : farm-save.json":"Sauvegarde automatique suspendue : le fichier n'a pas pu être chargé.\nRestaure la copie de secours ou sauvegarde cette partie manuellement.",small);
        GUI.Label(new Rect(r.x+24,r.y+503,550,25),"Flèches / stick : choisir · E / A : valider · Échap / Start : reprendre",small);
    }
    void OpenAppearance(){appearanceDraft=Appearance.Copy();appearanceCategory=0;view=View.Style;}
    void CycleAppearance(int direction)
    {
        int n=FarmAppearance.Options[appearanceCategory].Length;
        appearanceDraft.Set(appearanceCategory,(appearanceDraft.Get(appearanceCategory)+direction+n)%n);World.ApplyAppearance(appearanceDraft);
    }
    void ApplyAppearance(){Appearance=appearanceDraft.Copy();World.ApplyAppearance(Appearance);view=View.Play;SaveGame(false);Say("Ton nouveau personnage est prêt !");}
    void DrawAppearance(float w,float h)
    {
        var r=new Rect(w/2-440,h/2-270,880,540);GUI.Box(r,GUIContent.none,panel);
        GUI.Label(new Rect(r.x+24,r.y+20,760,34),"L'ARMOIRE · MON PERSONNAGE",title);
        if(GUI.Button(new Rect(r.xMax-62,r.y+20,38,30),"×",button)){CloseView();return;}
        for(int i=0;i<8;i++)
        {
            float y=r.y+74+i*43;
            if(GUI.Button(new Rect(r.x+24,y,210,35),FarmAppearance.Categories[i],i==appearanceCategory?selected:button))appearanceCategory=i;
            if(GUI.Button(new Rect(r.x+246,y,36,35),"‹",button)){appearanceCategory=i;CycleAppearance(-1);}
            GUI.Label(new Rect(r.x+288,y+6,237,28),FarmAppearance.Options[i][appearanceDraft.Get(i)],center);
            if(GUI.Button(new Rect(r.x+532,y,36,35),"›",button)){appearanceCategory=i;CycleAppearance(1);}
        }
        GUI.Box(new Rect(r.x+600,r.y+75,250,337),GUIContent.none,panel);
        GUI.Label(new Rect(r.x+617,r.y+86,218,30),"APERÇU",center);
        // Les quatre mêmes cartes composent l'aperçu et le personnage du monde.
        DrawAvatarPreview(new Vector2(r.x+725,r.y+390),166);
        if(GUI.Button(new Rect(r.x+24,r.y+436,540,45),"Valider le personnage · E / A",selected))ApplyAppearance();
        if(GUI.Button(new Rect(r.x+600,r.y+436,250,45),"Annuler · Échap / B",button))CloseView();
        GUI.Label(new Rect(r.x+24,r.y+499,830,26),"Haut / bas : catégorie · Gauche / droite : variante · Les changements sont appliqués après validation.",small);
    }
    void DrawAvatarSprite(Rect rect,Sprite sprite)=>GUI.DrawTextureWithTexCoords(rect,sprite.texture,new Rect(sprite.rect.x/sprite.texture.width,sprite.rect.y/sprite.texture.height,sprite.rect.width/sprite.texture.width,sprite.rect.height/sprite.texture.height));
    void DrawAvatarPreview(Vector2 feet,float scale)
    {
        void Part(int part,float center,float bottom,float height)
        {
            var sprite=art.AvatarPart(appearanceDraft,part);float h=height*scale,w=h*sprite.rect.width/sprite.rect.height;
            DrawAvatarSprite(new Rect(feet.x+center*scale-w/2,feet.y-bottom*scale-h,w,h),sprite);
        }
        Part(2,-FarmArt.AvatarLegSpacing,0,FarmArt.AvatarLegHeight);Part(3,FarmArt.AvatarLegSpacing,0,FarmArt.AvatarLegHeight);
        Part(1,0,FarmArt.AvatarTorsoBase,FarmArt.AvatarTorsoHeight);Part(0,0,FarmArt.AvatarHeadBase,FarmArt.AvatarHeadHeight);
    }

    void OpenDecoration(){view=View.Decoration;decorTab=0;movingFurniture=-1;decorSelecting=false;decorMessage="Choisis un meuble, puis une case et valide pour le poser.";}
    FarmFurniture Candidate()=>new(){Kind=decorKind,X=decorX,Z=decorZ,Rotation=decorRotation};
    void ProcessDecoration(FarmInputFrame f)
    {
        if(f.Tab!=0){decorTab=(decorTab+f.Tab+3)%3;movingFurniture=-1;}
        if(decorTab==0)
        {
            decorX=Mathf.Clamp(decorX+f.Navigate.x,0,FarmHome.Width-1);decorZ=Mathf.Clamp(decorZ+f.Navigate.y,0,FarmHome.Depth-1);
            if(f.Quantity!=0){movingFurniture=-1;decorSelecting=false;decorKind=(decorKind+f.Quantity+FarmHome.Catalog.Length)%FarmHome.Catalog.Length;}
            if(f.Wheel){decorSelecting=!decorSelecting;movingFurniture=-1;}
            if(f.Inventory||f.Rotate)decorRotation=(decorRotation+1)%4;
            if(f.Delete)RemoveFurniture();
            if(f.Confirm){if(decorSelecting)PickFurniture();else PlaceFurniture();}
        }
        else
        {
            int change=f.Navigate.x!=0?f.Navigate.x:-f.Navigate.y;
            if(change==0)change=f.Quantity;
            if(change!=0){if(decorTab==1)Home.Wallpaper=(Home.Wallpaper+change+3)%3;else Home.Floor=(Home.Floor+change+3)%3;World.ApplyHome(Home);}
        }
    }
    void PickFurniture()
    {
        int index=Home.At(decorX,decorZ);
        if(index<0){decorMessage="Choisis une case contenant un meuble.";return;}
        var item=Home.Furniture[index];movingFurniture=index;decorKind=item.Kind;decorRotation=item.Rotation;decorX=item.X;decorZ=item.Z;decorSelecting=false;
        decorMessage="Choisis la nouvelle place. Le meuble reste en place jusqu'à validation.";
    }
    void PlaceFurniture()
    {
        var item=Candidate();
        if(World.OccupiedByPlayer(item)){decorMessage="Le personnage se trouve ici. Ferme le menu et déplace-toi.";return;}
        if(Home.Furniture.Count>=100&&movingFurniture<0){decorMessage="La pièce contient déjà 100 décorations.";return;}
        if(Home.Place(item,movingFurniture,out decorMessage)){movingFurniture=-1;World.ApplyHome(Home);}
    }
    void RemoveFurniture()
    {
        int index=movingFurniture>=0?movingFurniture:Home.At(decorX,decorZ);
        if(index<0){decorMessage="Sélectionne un meuble à retirer.";return;}
        Home.Furniture.RemoveAt(index);movingFurniture=-1;World.ApplyHome(Home);decorMessage="Meuble rangé dans le catalogue.";
    }
    void DrawDecoration(float w,float h)
    {
        var r=new Rect(w/2-470,h/2-285,940,570);GUI.Box(r,GUIContent.none,panel);
        GUI.Label(new Rect(r.x+24,r.y+18,790,34),"MA MAISON · DÉCORATION",title);
        if(GUI.Button(new Rect(r.xMax-62,r.y+18,38,30),"×",button)){CloseView();return;}
        string[] tabs={"Meubles & objets","Papier peint","Sol"};
        for(int i=0;i<3;i++)if(GUI.Button(new Rect(r.x+24+i*298,r.y+64,288,37),tabs[i],decorTab==i?selected:button)){decorTab=i;movingFurniture=-1;}
        if(decorTab==0)
        {
            const float cell=47;float gx=r.x+28,gy=r.y+120;
            var candidate=Candidate();bool valid=Home.CanPlace(candidate,movingFurniture,out _)&&!World.OccupiedByPlayer(candidate);
            for(int z=0;z<FarmHome.Depth;z++)for(int x=0;x<FarmHome.Width;x++)
            {
                var rect=new Rect(gx+x*cell,gy+(FarmHome.Depth-1-z)*cell,cell-3,cell-3);
                int at=Home.At(x,z);string label=FarmHome.Reserved(x,z)?"·":at<0?"":furnitureLabels[Home.Furniture[at].Kind];
                Color color=GUI.color;
                if(!decorSelecting&&candidate.Contains(x,z))GUI.color=valid?new Color(.67f,1,.69f):new Color(1,.55f,.45f);
                if(GUI.Button(rect,label,x==decorX&&z==decorZ?selected:button)){decorX=x;decorZ=z;}
                GUI.color=color;
            }
            GUI.Label(new Rect(gx,gy+338,440,23),"Entrée en bas · Armoire à gauche · Coffre à droite",small);
            for(int i=0;i<FarmHome.Catalog.Length;i++)
            {
                var def=FarmHome.Catalog[i];
                if(GUI.Button(new Rect(r.x+480+(i%2)*212,r.y+120+(i/2)*52,202,44),def.Name+" · "+def.Width+"×"+def.Depth,i==decorKind&&!decorSelecting?selected:button))
                {decorKind=i;movingFurniture=-1;decorSelecting=false;decorRotation=0;}
            }
            if(GUI.Button(new Rect(r.x+480,r.y+342,414,38),"Tourner · R / Y  ("+(decorRotation*90)+"°)",button))decorRotation=(decorRotation+1)%4;
            if(GUI.Button(new Rect(r.x+480,r.y+390,202,38),decorSelecting?"Choisir un meuble":"Sélectionner · Tab / X",decorSelecting?selected:button)){decorSelecting=!decorSelecting;movingFurniture=-1;}
            if(GUI.Button(new Rect(r.x+692,r.y+390,202,38),"Retirer · Suppr / R3",button))RemoveFurniture();
            if(GUI.Button(new Rect(r.x+480,r.y+438,414,42),decorSelecting?"Déplacer le meuble · E / A":movingFurniture>=0?"Valider le déplacement · E / A":"Poser le meuble · E / A",selected)){if(decorSelecting)PickFurniture();else PlaceFurniture();}
            GUI.Label(new Rect(r.x+24,r.y+488,870,32),decorMessage,small);
        }
        else
        {
            var options=decorTab==1?FarmHome.Wallpapers:FarmHome.Floors;int choice=decorTab==1?Home.Wallpaper:Home.Floor;
            for(int i=0;i<options.Length;i++)if(GUI.Button(new Rect(r.x+45+i*285,r.y+150,270,95),options[i],choice==i?selected:button))
            {if(decorTab==1)Home.Wallpaper=i;else Home.Floor=i;World.ApplyHome(Home);}
            GUI.Label(new Rect(r.x+45,r.y+290,840,90),"Le revêtement est appliqué à toute la pièce.\nFerme le menu pour voir le résultat et te promener chez toi.",text);
        }
        if(GUI.Button(new Rect(r.x+690,r.y+522,204,33),"Terminer · Échap / B",button))CloseView();
        GUI.Label(new Rect(r.x+24,r.y+530,660,25),"Flèches / stick : case · −/+ ou LT/RT : meuble · Page↑/↓ ou LB/RB : onglet",small);
    }
}
