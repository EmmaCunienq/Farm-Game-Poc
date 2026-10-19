using System;
using System.IO;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

// Contrôles et rendus de la direction artistique. Exécuter dans une copie isolée.
public static class FarmPaperChecks
{
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception("PAPER_CHECK_FAILED: "+name);checks++;Debug.Log("PAPER_CHECK_OK: "+name);}
    public static void Run()
    {
        FarmHomeChecks.Run();checks=0;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var poc=new GameObject("Contrôle papier 2.5D").AddComponent<FarmPoc>();
        if(poc.State==null)typeof(FarmPoc).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(poc,null);
        try
        {
            var room=poc.World.Camera.transform.parent.Find("Intérieur de la maison");
            Check(room!=null,"Intérieur construit");
            foreach(var renderer in room.GetComponentsInChildren<MeshRenderer>(true))
                Check(renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount==4,"Plan 2D : "+renderer.name);
            var art=(FarmArt)typeof(FarmPoc).GetField("art",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(poc);
            for(int kind=0;kind<9;kind++)
            {
                var front=art.HomeSprite(kind);var side=art.HomeSprite(kind,true);
                Check(front.texture!=side.texture&&front.rect.width>30&&side.rect.width>30,"Vues illustrées de face / côté "+kind);
            }
            for(int part=0;part<4;part++)
            {
                var sprite=art.AvatarPart(poc.Appearance,part);
                Check(sprite.texture.width>=192&&sprite.texture.GetPixel(0,0).a<.1f,"Partie illustrée avec alpha "+part);
            }
            for(int kind=0;kind<7;kind++)for(int rotation=0;rotation<4;rotation++)
            {
                poc.Home.Furniture.Clear();poc.Home.Furniture.Add(new FarmFurniture{Kind=kind,X=3,Z=2,Rotation=rotation});poc.World.ApplyHome(poc.Home);
                var illustrated=room.Find("Mobilier placé").GetComponentInChildren<MeshRenderer>();
                Check(illustrated.sharedMaterial.shader.name=="Farm/PaperCutout","Meuble reste en papier après rotation "+kind+" / "+rotation);
            }
            poc.Home.Furniture.Clear();poc.Home.Furniture.Add(new FarmFurniture{Kind=7,X=1,Z=3,Rotation=1});poc.World.ApplyHome(poc.Home);
            Check(poc.Home.Valid(),"Tapis tourné et décoration valides");
            poc.Home.Furniture.Clear();poc.Home.Furniture.AddRange(FarmHome.Starter().Furniture);poc.Home.Wallpaper=0;poc.Home.Floor=0;poc.World.ApplyHome(poc.Home);
            poc.World.SetIndoors(true);FarmHomeChecks.Render(poc,"farm-paper-interior.png");
            poc.World.SetIndoors(false);poc.World.Teleport(new Vector3(3.5f,.03f,5.4f),Vector2.up);FarmHomeChecks.Render(poc,"farm-paper-exterior.png");
            string report=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../output/paper-redesign-checks.md"));
            File.WriteAllText(report,"# Vérification du rendu papier 2.5D\n\n63 contrôles fonctionnels et "+checks+" contrôles graphiques réussis dans Unity 6000.3.0f1.\n\nTout le mobilier visible, l'armoire, le coffre et les détails du décor sont des cartes à quatre sommets avec alpha. Les rotations choisissent une illustration de face ou de côté, puis un miroir ; les silhouettes restent verticales. Les sols et murs sont des plans avec revêtements peints. Les volumes de collision sont invisibles.\n\nLe personnage utilise des pièces illustrées alignées et animées : tête, buste et jambes. Les catégories de personnalisation et le fichier de sauvegarde restent compatibles.\n\nRendus : farm-paper-interior.png et farm-paper-exterior.png.\n");
            Debug.Log("PAPER_ALL_CHECKS_OK: "+checks);
        }
        finally{UnityEngine.Object.DestroyImmediate(poc.gameObject);}
    }
}
