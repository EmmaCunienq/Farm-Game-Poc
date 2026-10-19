using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// À lancer dans une copie isolée : -executeMethod FarmMouseChecks.Run.
public static class FarmMouseChecks
{
    static int checks;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string name){if(!ok)throw new Exception("MOUSE_CHECK_FAILED: "+name);checks++;Debug.Log("MOUSE_CHECK_OK: "+name);}
    static FarmInputFrame Frame()=>new(){DirectTool=-1};
    static void Cancel(FarmPoc poc){var f=Frame();f.Cancel=true;poc.ProcessInput(f);}
    static void Equip(FarmPoc poc,FarmTool tool){var f=Frame();f.DirectTool=(int)tool;poc.ProcessInput(f);}
    static FarmSaveData Snapshot(FarmSimulation state)
    {
        var save=new FarmSaveData{Home=FarmHome.Starter(),Appearance=new FarmAppearance(),PlayerPosition=new Vector3(6,.03f,2),Facing=Vector2.up};state.Export(save);return save;
    }
    static void Drive(FarmPoc poc)
    {
        int frames=0;
        while(poc.MouseWalking&&frames++<600)
        {
            var movement=poc.MouseMovement(Vector2.zero,.05f);poc.World.Update(.05f,movement,poc.CurrentScreen=="Play",Vector2.zero,false,poc.ActiveTool);
        }
        Check(!poc.MouseWalking,"Ordre souris terminé sans blocage");
    }
    public static void Run()
    {
        checks=0;Land();Navigation();Input();FarmProjectSetup.EnsurePipeline();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var poc=new GameObject("Contrôles souris et parcelles").AddComponent<FarmPoc>();
        if(poc.State==null)typeof(FarmPoc).GetMethod("Awake",Private).Invoke(poc,null);
        string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../tmp/mouse-check-"+Guid.NewGuid().ToString("N")+".json"));
        typeof(FarmPoc).GetField("storage",Private).SetValue(poc,new FarmSave(path));
        try
        {
            World(poc);Check(poc.SaveGame(),"Sauvegarde du terrain acheté et labouré");
            Check(new FarmSave(path).Read(out var saved,out _)&&saved.UnlockedLand==1&&saved.Plots[3].Tilled,"Propriété et nouvelle terre dans le fichier JSON");
            FarmHomeChecks.Render(poc,"farm-mouse-land.png");
            string report=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../output/mouse-land-checks.md"));
            File.WriteAllText(report,"# Parcelles et commandes souris\n\nUnity 6000.3.0f1 : "+checks+" contrôles réussis.\n\nAchats, fonds insuffisants, double achat refusé, limites, labourage et culture des extensions, sauvegarde et lecture d'un ancien fichier sans propriété de terrain. Chemins A* avec obstacles, destinations interdites, approche des cultures, action unique, annulation au clavier/dans les menus, entrée/sortie, armoire, coffre et marchand à la souris. Pointeur Input System simulé.\n\nLes contrôles en mode Play et les captures UI complètent cette vérification.\n");
            Debug.Log("MOUSE_ALL_CHECKS_OK: "+checks);
        }
        finally{UnityEngine.Object.DestroyImmediate(poc.gameObject);Time.timeScale=1;}
    }
    static void Land()
    {
        var state=new FarmSimulation(false);int coins=state.Money;
        Check(state.CanCultivate(4,3),"Champ de départ utilisable sans achat");
        for(int i=0;i<FarmLand.Lots.Length;i++)
        {
            var lot=FarmLand.Lots[i];Check(!state.CanCultivate(lot.X,lot.Z)&&!state.Till(lot.X,lot.Z,out _),"Lot verrouillé non labourable "+i);
            Check(FarmSimulation.Inside(lot.X,lot.Z)&&FarmSimulation.Inside(lot.X+lot.Width-1,lot.Z+lot.Depth-1),"Lot dans les limites "+i);
        }
        Check(!state.BuyLand(-1,out _)&&!state.BuyLand(99,out _)&&state.Money==coins,"Identifiant invalide ne débite rien");
        Check(state.BuyLand(0,out _)&&state.Money==coins-30&&state.OwnsLand(0),"Achat du sud débite exactement 30 pièces");
        Check(!state.Plots[3,0].Tilled,"Acheter ne laboure pas automatiquement");
        coins=state.Money;Check(!state.BuyLand(0,out _)&&state.Money==coins,"Double achat refusé sans débit");
        Check(!state.BuyLand(1,out _)&&!state.OwnsLand(1)&&state.Money==coins,"Fonds insuffisants conservent le terrain verrouillé");
        Check(state.Till(3,0,out _)&&state.Plant(3,0,out _)&&state.Water(3,0,out _),"Labourer, planter et arroser la nouvelle parcelle");
        state.AdvanceRealSeconds(900);Check(state.Harvest(3,0,out _),"Récolter sur la nouvelle parcelle");
        state.Products[2]=10;state.Sell(2,10,out _);
        Check(state.BuyLand(1,out _)&&state.BuyLand(2,out _)&&state.UnlockedLand==FarmLand.AllUnlocked,"Les trois extensions peuvent être achetées");
        foreach(var lot in FarmLand.Lots)
        {
            bool usable=true;for(int z=lot.Z;z<lot.Z+lot.Depth;z++)for(int x=lot.X;x<lot.X+lot.Width;x++)usable&=state.CanCultivate(x,z)&&state.Till(x,z,out _);
            Check(usable,"Toutes les cases du lot deviennent labourables : "+lot.Name);
        }
        Check(!state.CanCultivate(2,0)&&!state.CanCultivate(10,6)&&!state.Till(-1,0,out _),"Les achats n'autorisent pas les cases voisines hors parcelle");
        var save=Snapshot(state);Check(save.Valid(),"Sauvegarde avec les trois propriétés valide");
        var restored=new FarmSimulation(false);restored.Restore(JsonUtility.FromJson<FarmSaveData>(JsonUtility.ToJson(save)));
        Check(restored.UnlockedLand==7&&restored.Plots[10,4].Tilled&&restored.Money==state.Money,"Propriété, terre et argent restaurés");
        var old=Snapshot(new FarmSimulation());string json=JsonUtility.ToJson(old).Replace("\"UnlockedLand\":0,","");
        Check(!json.Contains("UnlockedLand")&&JsonUtility.FromJson<FarmSaveData>(json).Valid(),"Ancienne sauvegarde sans nouveaux champs compatible");
        old.UnlockedLand=8;Check(!old.Valid(),"Propriété inconnue refusée");old.UnlockedLand=0;old.Plots[3].Tilled=true;
        Check(!old.Valid(),"Sauvegarde avec labourage d'un terrain non acheté refusée");
    }
    static void Navigation()
    {
        var bounds=new Rect(0,0,6,6);
        bool Free(Vector2 p)=>!(p.x>=2&&p.x<=3.6f&&p.y<=4.4f);
        var navigation=new FarmNavigation(bounds,Free);
        Check(navigation.FindPath(new Vector2(1,.5f),new Vector2(5,.5f),out var path)&&path.Any(p=>p.y>4.4f),"Contourner un obstacle sans traverser son volume");
        var from=new Vector2(1,.5f);bool allClear=true;
        foreach(var to in path){for(int i=1;i<=100;i++)allClear&=Free(Vector2.Lerp(from,to,i/100f));from=to;}
        Check(allClear,"Les segments lissés restent hors obstacle");
        Check(!navigation.FindPath(Vector2.one,new Vector2(2.5f,2),out _),"Destination dans un obstacle refusée");
        Check(!navigation.FindPath(Vector2.one,new Vector2(7,7),out _),"Destination hors carte refusée");
        var closed=new FarmNavigation(bounds,p=>p.x<2||p.x>3.6f);
        Check(!closed.FindPath(new Vector2(1,1),new Vector2(5,1),out _),"Barrière complète déclarée inaccessible");
    }
    static void Input()
    {
        InputSystem.settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE",true);var old=InputSystem.settings.updateMode;
        InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;var mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();
        try
        {
            InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(600,400)});InputSystem.Update();var reader=new FarmInput();var frame=reader.Read(1);
            Check(frame.PointerAvailable&&frame.PointerPosition==new Vector2(600,400),"Position souris exposée par Input System");
            InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(700,400),delta=new Vector2(100,0),buttons=4});InputSystem.Update();frame=reader.Read(2);
            Check(frame.Orbit&&frame.CameraStick.x>0&&reader.UsingMouse,"Bouton central permet de tourner la caméra à la souris");
        }
        finally{InputSystem.RemoveDevice(mouse);InputSystem.settings.updateMode=old;InputSystem.settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE",false);}
    }
    static void World(FarmPoc poc)
    {
        Check(poc.StartMouseMove(new Vector2(13.5f,4.5f)),"Ordre de déplacement distant accepté");Drive(poc);
        Check(Vector2.Distance(poc.World.PlayerPoint,new Vector2(13.5f,4.5f))<.15f,"Arrivée au point cliqué en contournant la mare");
        Check(!poc.StartMouseMove(new Vector2(11.8f,3.2f))&&!poc.MouseWalking,"Clic dans la mare n'engage pas le déplacement");
        Check(poc.StartMouseMove(new Vector2(6,2)),"Nouveau trajet accepté");
        Check(poc.MouseMovement(Vector2.left,.05f)==Vector2.left&&!poc.MouseWalking,"Déplacement manuel annule le trajet souris");
        Equip(poc,FarmTool.Hoe);Check(poc.StartMouseFarmAction(new Vector2Int(3,2)),"Approche automatique de la case à labourer");Drive(poc);
        Check(poc.State.Plots[3,2].Tilled,"L'action touche exactement la case cliquée");
        Equip(poc,FarmTool.Seeds);int seeds=poc.State.Seeds[0];poc.StartMouseFarmAction(new Vector2Int(3,2));Drive(poc);
        Check(poc.State.Seeds[0]==seeds-1&&poc.State.Plots[3,2].Crop==0,"Un seul clic plante une seule graine");
        poc.MouseMovement(Vector2.zero,.2f);Check(poc.State.Seeds[0]==seeds-1,"L'action n'est pas répétée après l'arrivée");
        Equip(poc,FarmTool.Hoe);poc.StartMouseFarmAction(new Vector2Int(9,6));var menu=Frame();menu.Inventory=true;poc.ProcessInput(menu);poc.MouseMovement(Vector2.zero,.05f);
        Check(!poc.MouseWalking&&!poc.State.Plots[9,6].Tilled,"Ouvrir un menu annule l'action en attente");Cancel(poc);
        Check(poc.StartMouseFarmAction(new Vector2Int(3,0))&&poc.CurrentScreen=="Land","Action sur terrain verrouillé ouvre son achat");
        var buy=Frame();buy.Confirm=true;poc.ProcessInput(buy);Check(poc.State.OwnsLand(0)&&poc.State.Money==15,"Menu débloque le lot contre de l'argent");Cancel(poc);
        poc.StartMouseFarmAction(new Vector2Int(3,0));Drive(poc);Check(poc.State.Plots[3,0].Tilled,"Souris laboure une case achetée en bord de carte");
        Check(!poc.ProcessPointerClick(new Vector2(36,Screen.height-40),false)&&!poc.MouseWalking,"Clic sur le HUD ne traverse pas l'interface");
        Check(poc.StartMouseInteraction(FarmInteraction.House),"Clic maison engage l'approche");Drive(poc);Check(poc.World.Indoors,"Entrée automatique dans la maison");
        poc.StartMouseInteraction(FarmInteraction.Wardrobe);Drive(poc);Check(poc.CurrentScreen=="Style","Armoire atteinte en contournant le lit");
        poc.ProcessPointerClick(Vector2.zero,true);Check(poc.CurrentScreen=="Play","Clic droit ferme l'armoire sans clavier");
        poc.StartMouseInteraction(FarmInteraction.DecorChest);Drive(poc);Check(poc.CurrentScreen=="Decoration","Coffre atteint à la souris");poc.ProcessPointerClick(Vector2.zero,true);
        poc.StartMouseInteraction(FarmInteraction.Exit);Drive(poc);Check(!poc.World.Indoors,"Sortie automatique par le seuil");
        poc.StartMouseInteraction(FarmInteraction.Shop);Drive(poc);Check(poc.CurrentScreen=="Shop","Marchand atteint et ouvert à la souris");Cancel(poc);
        var pause=Frame();pause.Pause=true;poc.ProcessInput(pause);poc.ProcessPointerClick(Vector2.zero,true);
        Check(!poc.IsPaused&&Time.timeScale==1,"Clic droit reprend le jeu depuis la pause");
    }
}
