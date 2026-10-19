using System;
using System.IO;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

// À lancer dans une copie isolée : -executeMethod FarmCraftChecks.Run.
// Ces contrôles utilisent leur propre fichier, jamais la sauvegarde de la joueuse.
public static class FarmCraftChecks
{
    static int checks;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string name){if(!ok)throw new Exception("CRAFT_CHECK_FAILED: "+name);checks++;Debug.Log("CRAFT_CHECK_OK: "+name);}
    static FarmInputFrame Frame()=>new(){DirectTool=-1};
    static void Confirm(FarmPoc poc){var f=Frame();f.Confirm=true;poc.ProcessInput(f);}
    static void Cancel(FarmPoc poc){var f=Frame();f.Cancel=true;poc.ProcessInput(f);}
    static void Advance(FarmPoc poc,float seconds)=>typeof(FarmPoc).GetMethod("UpdateCrafting",Private).Invoke(poc,new object[]{seconds});
    static void AtWorkbench(FarmPoc poc)
    {
        var p=poc.World.ApproachPoint(FarmInteraction.Workbench);
        poc.World.SetIndoors(false,false);poc.World.Teleport(new Vector3(p.x,FarmWorld25D.GroundHeight(p.x,p.y)+.03f,p.y),Vector2.left);
    }
    public static void Run()
    {
        checks=0;Models();FarmProjectSetup.EnsurePipeline();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var poc=new GameObject("Contrôles établi").AddComponent<FarmPoc>();
        if(poc.State==null)typeof(FarmPoc).GetMethod("Awake",Private).Invoke(poc,null);
        var testPath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../tmp/craft-save-"+Guid.NewGuid().ToString("N")+".json"));
        typeof(FarmPoc).GetField("storage",Private).SetValue(poc,new FarmSave(testPath));
        try
        {
            Workbench(poc);
            AtWorkbench(poc);poc.World.SetCraftTime(.05f);FarmHomeChecks.Render(poc,"farm-craft-workbench.png");poc.World.SetCraftTime(-1);
            string report=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../output/craft-checks.md"));
            File.WriteAllText(report,"# Contrôles de l'établi\n\nUnity 6000.3.0f1 : "+checks+" contrôles réussis.\n\nRecettes, ingrédients manquants, consommation en fin d'animation, interruption sans perte, vente chez le marchand, sauvegarde et anciennes sauvegardes, ouverture au clavier et à la souris.\n\nCapture URP : farm-craft-workbench.png (copeaux de l'animation visibles). L'interface OnGUI est exclue des captures du décor.\n");
            Debug.Log("CRAFT_ALL_CHECKS_OK: "+checks);
        }
        finally{UnityEngine.Object.DestroyImmediate(poc.gameObject);Time.timeScale=1;}
    }
    static void Models()
    {
        var state=new FarmSimulation(false);int lantern=1;
        Check(!state.CanCraft(lantern)&&!state.Craft(lantern,out var message)&&message.Contains("citrouille"),"Ingrédients manquants indiqués");
        state.Products[1]=3;state.Products[2]=1;
        Check(state.CanCraft(lantern)&&state.Craft(lantern,out _),"Fabrication possible avec les ingrédients");
        Check(state.Products[1]==2&&state.Products[2]==0&&state.Crafted[lantern]==1,"Ingrédients consommés et objet ajouté");
        int money=state.Money;Check(state.Craft(0,out _)&&state.Money==money-FarmCrafting.Recipes[0].Coins,"Pièces de la recette débitées");
        money=state.Money;
        Check(state.SellCrafted(lantern,1,out _)&&state.Money==money+FarmCrafting.Recipes[lantern].SalePrice&&state.Crafted[lantern]==0,"Objet vendu chez le marchand");
        Check(!state.SellCrafted(lantern,1,out _),"Vente impossible sans objet");
        state.Crafted[5]=4;
        var save=new FarmSaveData{Home=FarmHome.Starter(),Appearance=new FarmAppearance(),PlayerPosition=new Vector3(6,.03f,2),Facing=Vector2.up};state.Export(save);
        Check(save.Valid()&&JsonUtility.ToJson(save).Contains("Crafted"),"Objets fabriqués sauvegardés");
        var restored=new FarmSimulation(false);restored.Restore(JsonUtility.FromJson<FarmSaveData>(JsonUtility.ToJson(save)));
        Check(restored.Crafted[5]==4,"Objets fabriqués restaurés");
        save.Crafted=null;Check(save.Valid(),"Ancienne sauvegarde sans objets acceptée");
        restored.Restore(save);Check(restored.TotalCrafted==0,"Ancienne sauvegarde : inventaire d'objets vide");
        save.Crafted=new[]{1,2};Check(!save.Valid(),"Liste d'objets incohérente refusée");
        save.Crafted=new int[FarmCrafting.Recipes.Length];save.Crafted[0]=-1;Check(!save.Valid(),"Quantité négative refusée");
    }
    static void Workbench(FarmPoc poc)
    {
        Array.Clear(poc.State.Products,0,3);
        AtWorkbench(poc);Check(poc.World.NearWorkbench,"Établi détecté à proximité");
        Confirm(poc);Check(poc.CurrentScreen=="Craft","Action devant l'établi ouvre le catalogue");
        var f=Frame();f.Navigate=Vector2Int.down;poc.ProcessInput(f);Check(poc.CraftCursor==1,"Navigation dans le catalogue");
        Confirm(poc);Check(!poc.Crafting,"Fabrication refusée sans ingrédients");
        poc.State.Products[1]=1;poc.State.Products[2]=1;
        Confirm(poc);Check(poc.Crafting,"Animation de fabrication lancée");
        Advance(poc,1);Check(poc.State.Products[2]==1&&poc.State.Crafted[1]==0,"Ingrédients conservés pendant l'animation");
        Cancel(poc);Check(poc.CurrentScreen=="Play"&&!poc.Crafting&&poc.State.Products[2]==1,"Interruption sans perte");
        Confirm(poc);Confirm(poc);Advance(poc,1);
        f=Frame();f.Navigate=Vector2Int.up;poc.ProcessInput(f);Check(poc.CraftCursor==1,"Catalogue verrouillé pendant la fabrication");
        Advance(poc,FarmCrafting.CraftSeconds);
        Check(!poc.Crafting&&poc.State.Crafted[1]==1&&poc.State.Products[1]==0&&poc.State.Products[2]==0,"Objet fabriqué à la fin de l'animation");
        Cancel(poc);
        poc.World.Teleport(FarmWorld25D.Position(6,2),Vector2.up);
        Check(poc.StartMouseInteraction(FarmInteraction.Workbench),"Clic sur l'établi engage l'approche");
        int frames=0;
        while(poc.MouseWalking&&frames++<600){var movement=poc.MouseMovement(Vector2.zero,.05f);poc.World.Update(.05f,movement,poc.CurrentScreen=="Play",Vector2.zero,false,poc.ActiveTool);}
        Check(poc.CurrentScreen=="Craft","Établi atteint et ouvert à la souris");
        Cancel(poc);
        poc.World.Teleport(new Vector3(2f,.03f,4.3f),Vector2.right);Confirm(poc);
        Check(poc.CurrentScreen=="Play"&&poc.State.Plots[3,4].Tilled,"Près des champs, la bêche reste prioritaire");
    }
}
