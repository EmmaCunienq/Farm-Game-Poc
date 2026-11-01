using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Construit le prototype jouable. La ferme se crée toute seule au lancement (FarmPoc.Launch),
// il suffit donc d'une scène vide enregistrée et inscrite dans la liste des scènes du build.
public static class FarmBuild
{
    public const string ScenePath="Assets/Scenes/Ferme.unity";
    public const string OutputFolder="Build du prototype";

    [MenuItem("Farm POC/Construire le prototype macOS")]
    public static void BuildMac()=>Build(BuildTarget.StandaloneOSX,"Farm Game.app");
    [MenuItem("Farm POC/Construire le prototype Windows")]
    public static void BuildWindows()=>Build(BuildTarget.StandaloneWindows64,"Farm Game/Farm Game.exe");

    static void Build(BuildTarget target,string file)
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        EnsureScene();
        var options=new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.Combine(OutputFolder,file),target=target,options=BuildOptions.None};
        var summary=BuildPipeline.BuildPlayer(options).summary;
        string message=summary.result==BuildResult.Succeeded
            ?"Build réussi : "+options.locationPathName+" ("+(summary.totalSize/1048576)+" Mo)."
            :"Build échoué : "+summary.result+", "+summary.totalErrors+" erreur(s). Détails dans la console.";
        Debug.Log(message);
        if(Application.isBatchMode)EditorApplication.Exit(summary.result==BuildResult.Succeeded?0:1);
        else if(summary.result==BuildResult.Succeeded)EditorUtility.RevealInFinder(options.locationPathName);
    }
    static void EnsureScene()
    {
        if(!File.Exists(ScenePath))
        {
            // Une scène additive est refusée tant qu'une scène sans titre est ouverte : on remplace puis on restaure.
            var setup=EditorSceneManager.GetSceneManagerSetup();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene,ScenePath);
            if(setup.Length>0&&System.Array.TrueForAll(setup,s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
        // Permet aussi de lancer le build depuis la fenêtre Build Profiles de Unity.
        var scenes=EditorBuildSettings.scenes;
        if(scenes.Length!=1||scenes[0].path!=ScenePath)EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
    }
}
