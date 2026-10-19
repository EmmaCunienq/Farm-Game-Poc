using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Exécuter dans une copie du projet : -executeMethod FarmHomeChecks.Run.
// Ces contrôles utilisent leur propre fichier, jamais la sauvegarde de la joueuse.
public static class FarmHomeChecks
{
    static int assertions;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool value,string name){if(!value)throw new Exception("HOME_CHECK_FAILED: "+name);assertions++;Debug.Log("HOME_CHECK_OK: "+name);}
    static void Invoke(FarmPoc poc,string method)=>typeof(FarmPoc).GetMethod(method,Private).Invoke(poc,null);
    static void Set(FarmPoc poc,string field,object value)=>typeof(FarmPoc).GetField(field,Private).SetValue(poc,value);
    static FarmInputFrame Frame()=>new(){DirectTool=-1};
    static void Confirm(FarmPoc poc){var f=Frame();f.Confirm=true;poc.ProcessInput(f);}
    static void Cancel(FarmPoc poc){var f=Frame();f.Cancel=true;poc.ProcessInput(f);}
    public static void Run()
    {
        assertions=0;Models();FarmProjectSetup.EnsurePipeline();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var poc=new GameObject("Contrôles maison").AddComponent<FarmPoc>();
        if(poc.State==null)Invoke(poc,"Awake");
        var testPath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../tmp/home-save-"+Guid.NewGuid().ToString("N")+".json"));
        Set(poc,"storage",new FarmSave(testPath));
        try
        {
            Interactions(poc);Persistence(poc,testPath);InputDevices(poc);Render(poc,"farm-home-interior.png");
            poc.World.SetIndoors(false);Render(poc,"farm-home-exterior.png");
            Check(!ShaderUtil.ShaderHasError(Shader.Find("Farm/PaperCutout")),"Shader papier sans erreur");
            string report=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../output/home-feature-checks.md"));
            File.WriteAllText(report,"# Contrôles maison, personnage et sauvegarde\n\nUnity 6000.3.0f1 : "+assertions+" contrôles réussis.\n\nPlacement, rotation, chevauchement, entrée/sortie, armoire, annulation, pause, sérialisation JSON, restauration complète, fichier endommagé et copie de secours. Clavier et manette simulés via Input System. Compilation avec Input System et Input Manager.\n\nCaptures URP : farm-home-interior.png et farm-home-exterior.png. L'interface OnGUI est exclue des captures du décor. Les périphériques physiques restent à vérifier dans l'onglet Game.\n");
            Debug.Log("HOME_ALL_CHECKS_OK: "+assertions);
        }
        finally{UnityEngine.Object.DestroyImmediate(poc.gameObject);Time.timeScale=1;}
    }
    static void Models()
    {
        var home=FarmHome.Starter();Check(home.Valid(),"Maison initiale valide");
        Check(!home.Place(new FarmFurniture{Kind=3,X=8,Z=2},-1,out _),"Meuble hors pièce refusé");
        Check(!home.Place(new FarmFurniture{Kind=2,X=4,Z=0},-1,out _),"Entrée réservée");
        Check(!home.Place(new FarmFurniture{Kind=2,X=0,Z=5},-1,out _),"Armoire accessible");
        Check(!home.Place(new FarmFurniture{Kind=2,X=8,Z=6},-1,out _),"Coffre accessible");
        Check(!home.Place(new FarmFurniture{Kind=2,X=1,Z=4},-1,out _),"Chevauchement des meubles refusé");
        Check(home.Place(new FarmFurniture{Kind=7,X=1,Z=3},-1,out _),"Tapis sous un meuble autorisé");
        Check(home.Place(new FarmFurniture{Kind=3,X=3,Z=5},-1,out _),"Ajout d'un canapé");
        int last=home.Furniture.Count-1;
        Check(home.Place(new FarmFurniture{Kind=3,X=3,Z=2,Rotation=1},last,out _),"Déplacement avec rotation sans collision avec soi-même");
        Check(home.Furniture[last].Width==1&&home.Furniture[last].Depth==3,"Rotation permute l'empreinte");
        var previous=home.Furniture[last];
        Check(!home.Place(new FarmFurniture{Kind=3,X=1,Z=3},last,out _)&&home.Furniture[last]==previous,"Placement invalide conserve l'ancien meuble");
        Check(home.Valid(),"Pièce décorée valide");
        var appearance=new FarmAppearance();Check(appearance.Valid(),"Apparence initiale valide");
        var copy=appearance.Copy();copy.Hair=2;Check(appearance.Hair==0&&copy.Hair==2,"Brouillon de personnage indépendant");
        appearance.Skin=9;Check(!appearance.Valid(),"Choix de personnage invalide refusé");
    }
    static void Interactions(FarmPoc poc)
    {
        Confirm(poc);Check(!poc.World.Indoors,"Action loin de la porte ne fait pas entrer");
        poc.World.Teleport(new Vector3(2,.03f,5.7f),Vector2.up);Check(poc.World.NearHouse,"Porte accessible devant le collider de la maison");
        Confirm(poc);Check(poc.World.Indoors,"Entrée avec E / A quel que soit l'outil");
        Check(!poc.World.NearShop,"Marchand inaccessible depuis l'intérieur");
        Physics.SyncTransforms();
        Check(Physics.Raycast(new Vector3(84.5f,2,2),Vector3.down,out var hit,3)&&hit.collider.gameObject.name=="Sol","Sol intérieur indépendant du terrain extérieur");
        poc.World.Teleport(new Vector3(80.5f,.03f,4.9f),Vector2.up);Confirm(poc);
        Check(poc.CurrentScreen=="Style","Armoire ouvre les catégories du personnage");
        var f=Frame();f.Navigate=Vector2Int.right;poc.ProcessInput(f);Cancel(poc);
        Check(poc.Appearance.Skin==0&&poc.CurrentScreen=="Play","Annulation de la personnalisation");
        Confirm(poc);poc.ProcessInput(f);Confirm(poc);Check(poc.Appearance.Skin==1,"Validation applique les traits");
        var art=(FarmArt)typeof(FarmPoc).GetField("art",Private).GetValue(poc);
        var before=art.AvatarPart(poc.Appearance,0);var draft=poc.Appearance.Copy();draft.Hair=2;
        Check(before!=art.AvatarPart(draft,0),"Coiffure modifie la carte du visage");draft.Top=2;
        Check(art.AvatarPart(draft,1)!=art.AvatarPart(poc.Appearance,1),"Vêtement modifie la carte du buste");
        poc.World.Teleport(new Vector3(88.5f,.03f,4.9f),Vector2.up);Confirm(poc);Check(poc.CurrentScreen=="Decoration","Coffre ouvre la décoration");
        Set(poc,"decorKind",3);Set(poc,"decorX",3);Set(poc,"decorZ",5);int count=poc.Home.Furniture.Count;Confirm(poc);
        Check(poc.Home.Furniture.Count==count+1,"Pose d'un meuble depuis le menu");
        Set(poc,"decorSelecting",true);Confirm(poc);Set(poc,"decorX",3);Set(poc,"decorZ",2);Set(poc,"decorRotation",1);Confirm(poc);
        Check(poc.Home.Furniture[count].Rotation==1&&poc.Home.Furniture[count].X==3,"Déplacement et rotation dans le menu");
        var del=Frame();del.Delete=true;poc.ProcessInput(del);Check(poc.Home.Furniture.Count==count,"Retrait d'un meuble");
        var tab=Frame();tab.Tab=1;poc.ProcessInput(tab);poc.ProcessInput(f);Check(poc.Home.Wallpaper==1,"Papier peint changé au clavier");
        poc.ProcessInput(tab);poc.ProcessInput(f);Check(poc.Home.Floor==1,"Sol changé au clavier");Cancel(poc);
        Cancel(poc);Check(poc.IsPaused&&Time.timeScale==0,"Échap ouvre la pause et arrête le temps Unity");
        double hour=poc.State.Hour;Vector3 pos=poc.World.Player.position;Invoke(poc,"Update");
        Check(poc.State.Hour==hour&&poc.World.Player.position==pos,"Pause bloque l'horloge, les cultures et le déplacement");
        var inventory=Frame();inventory.Inventory=true;poc.ProcessInput(inventory);Check(poc.IsPaused,"Inventaire ne contourne pas la pause");Cancel(poc);
        Check(!poc.IsPaused&&Time.timeScale==1,"Reprise restaure le temps");
        poc.World.Teleport(new Vector3(84.5f,.03f,.65f),Vector2.down);Confirm(poc);Check(!poc.World.Indoors,"Sortie par le seuil");
        var open=Frame();open.Inventory=true;poc.ProcessInput(open);Cancel(poc);Check(poc.CurrentScreen=="Play","Échap ferme d'abord les menus");
        poc.World.SetIndoors(true);
    }
    static void Persistence(FarmPoc poc,string path)
    {
        poc.State.Till(4,3,out _);poc.State.Plant(4,3,out _);poc.State.Water(4,3,out _);poc.State.Harvest(4,6,out _);poc.State.Sell(0,1,out _);
        poc.State.AdvanceRealSeconds(35);poc.State.SelectedSeed=2;poc.State.SelectedProduct=1;
        Check(poc.SaveGame(),"Écriture réelle du fichier JSON");
        Check(File.Exists(path)&&File.ReadAllText(path).Contains("Wallpaper"),"Fichier contient la maison");
        int coins=poc.State.Money,seeds=poc.State.Seeds[0],products=poc.State.Products[0],day=poc.State.Day;double hour=poc.State.Hour,growth=poc.State.Plots[4,3].GrowthHours;
        int wallpaper=poc.Home.Wallpaper,floor=poc.Home.Floor,skin=poc.Appearance.Skin;Vector3 pos=poc.World.Player.position;
        Check(new FarmSave(path).Read(out var saved,out _)&&saved.Valid(),"Relecture et validation du fichier complet");
        var another=new FarmSimulation(false);another.Restore(saved);Check(another.Money==coins&&another.Plots[4,3].GrowthHours==growth,"Restauration dans une nouvelle simulation");
        poc.State.SkipToNextMorning();poc.State.BuySeeds(0,1,out _);poc.Home.Wallpaper=2;poc.Home.Floor=2;poc.Appearance.Skin=3;poc.World.SetIndoors(false);
        Invoke(poc,"OpenDecoration");string fileBeforeLoad=File.ReadAllText(path);
        Check(poc.LoadGame(),"Chargement depuis le menu de jeu");
        Check(File.ReadAllText(path)==fileBeforeLoad,"Chargement depuis la décoration n'écrase pas la sauvegarde avec les changements annulés");
        Check(poc.State.Day==day&&Math.Abs(poc.State.Hour-hour)<1e-9&&poc.State.Money==coins,"Jour, heure et argent restaurés");
        Check(poc.State.Seeds[0]==seeds&&poc.State.Products[0]==products&&poc.State.SelectedSeed==2&&poc.State.SelectedProduct==1,"Stocks et sélections restaurés");
        Check(poc.State.Plots[4,3].GrowthHours==growth&&poc.State.Plots[4,3].WateredDay==day,"Culture et arrosage restaurés");
        Check(poc.Home.Wallpaper==wallpaper&&poc.Home.Floor==floor&&poc.Appearance.Skin==skin,"Maison et personnage restaurés");
        Check(poc.World.Indoors&&Vector3.Distance(pos,poc.World.Player.position)<.1f,"Pièce et position restaurées");
        Check(poc.SaveGame()&&File.Exists(path+".bak"),"Remplacement atomique conserve une copie de secours");
        string validJson=File.ReadAllText(path);saved.Home.Wallpaper=99;
        Check(!new FarmSave(path).Write(saved,out _)&&File.ReadAllText(path)==validJson,"État invalide n'écrase pas le fichier");
        File.WriteAllText(path,"{ fichier endommagé");hour=poc.State.Hour;
        Check(!poc.LoadGame()&&poc.State.Hour==hour,"Fichier endommagé conserve la partie en cours");
        Check(poc.LoadGame(true),"Copie de secours récupère la partie");
        Check(poc.SaveGame(),"Sauvegarde possible après récupération");
        new FarmSave(path).Read(out saved,out _);saved.Version=999;Check(!saved.Valid(),"Version inconnue refusée");saved.Version=1;saved.Plots[0].Crop=999;Check(!saved.Valid(),"Culture incorrecte refusée avant restauration");
    }
    static void InputDevices(FarmPoc poc)
    {
        InputSystem.settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE",true);
        var oldMode=InputSystem.settings.updateMode;InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;
        var pad=InputSystem.AddDevice<Gamepad>();var keyboard=InputSystem.AddDevice<Keyboard>();pad.MakeCurrent();var reader=new FarmInput();float time=0;
        void Pad(GamepadState state){InputSystem.QueueStateEvent(pad,state);InputSystem.Update();poc.ProcessInput(reader.Read(time+=.4f));}
        try
        {
            Pad(new GamepadState());Pad(new GamepadState().WithButton(GamepadButton.Start));Check(poc.IsPaused,"Start / Options ouvre la pause");
            Pad(new GamepadState());Pad(new GamepadState().WithButton(GamepadButton.Start));Check(!poc.IsPaused,"Start / Options reprend");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));InputSystem.Update();poc.ProcessInput(reader.Read(time+=.4f));Check(poc.IsPaused,"Touche Échap ouvre la pause");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();reader.Read(time+=.4f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));InputSystem.Update();poc.ProcessInput(reader.Read(time+=.4f));Check(!poc.IsPaused,"Touche Échap reprend");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();reader.Read(time+=.4f);
            Invoke(poc,"OpenDecoration");Pad(new GamepadState().WithButton(GamepadButton.North));
            Check((int)typeof(FarmPoc).GetField("decorRotation",Private).GetValue(poc)==2,"Y tourne le meuble sans ouvrir l'inventaire");
            Pad(new GamepadState());Pad(new GamepadState().WithButton(GamepadButton.RightShoulder));
            Check((int)typeof(FarmPoc).GetField("decorTab",Private).GetValue(poc)==1,"RB change d'onglet décoration");
            Pad(new GamepadState());Pad(new GamepadState().WithButton(GamepadButton.East));Check(poc.CurrentScreen=="Play","B termine la décoration");
        }
        finally{InputSystem.RemoveDevice(pad);InputSystem.RemoveDevice(keyboard);InputSystem.settings.updateMode=oldMode;InputSystem.settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE",false);}
    }
    public static void Render(FarmPoc poc,string filename)
    {
        poc.World.Update(0,Vector2.zero,false,Vector2.zero,false,poc.ActiveTool);Physics.SyncTransforms();
        var camera=poc.World.Camera;camera.aspect=1280f/800;
        var rt=new RenderTexture(1280,800,24);rt.Create();
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
        var previous=RenderTexture.active;RenderTexture.active=rt;
        var texture=new Texture2D(1280,800,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,1280,800),0,0);texture.Apply();
        string target=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../output/"+filename));Directory.CreateDirectory(Path.GetDirectoryName(target));File.WriteAllBytes(target,texture.EncodeToPNG());
        RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
}
