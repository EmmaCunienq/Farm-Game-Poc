using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class FarmProjectSetup
{
    public const string PipelinePath="Assets/Resources/Rendering/FarmPipeline.asset";
    [InitializeOnLoadMethod]
    static void Schedule()=>EditorApplication.delayCall+=EnsurePipeline;
    [MenuItem("Farm POC/Configurer le rendu 2.5D URP")]
    public static void EnsurePipeline()
    {
        if(EditorApplication.isCompiling){EditorApplication.delayCall+=EnsurePipeline;return;}
        System.IO.Directory.CreateDirectory("Assets/Resources/Rendering");
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if(pipeline==null)
        {
            var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer.name="FarmRenderer";
            AssetDatabase.CreateAsset(renderer,"Assets/Resources/Rendering/FarmRenderer.asset");
            pipeline=UniversalRenderPipelineAsset.Create(renderer);
            pipeline.name="FarmPipeline";pipeline.msaaSampleCount=4;pipeline.shadowDistance=55;pipeline.shadowCascadeCount=2;
            var data=new SerializedObject(pipeline);
            data.FindProperty("m_MainLightShadowsSupported").boolValue=true;
            data.FindProperty("m_AdditionalLightsRenderingMode").intValue=1;
            data.FindProperty("m_AdditionalLightShadowsSupported").boolValue=true;
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(pipeline,PipelinePath);AssetDatabase.SaveAssets();
        }
        if(GraphicsSettings.defaultRenderPipeline!=pipeline)GraphicsSettings.defaultRenderPipeline=pipeline;
        if(QualitySettings.renderPipeline!=pipeline)QualitySettings.renderPipeline=pipeline;
        PlayerSettings.colorSpace=ColorSpace.Linear;
        // Rend les shaders des objets créés en code disponibles dans une build PC.
        var graphics=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if(graphics.Length>0)
        {
            var settings=new SerializedObject(graphics[0]);var shaders=settings.FindProperty("m_AlwaysIncludedShaders");
            foreach(string name in new[]{"Farm/PaperCutout","Farm/ContactShadow","Universal Render Pipeline/Lit"})
            {
                var shader=Shader.Find(name);if(shader==null)continue;bool found=false;
                for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;
                if(!found){int n=shaders.arraySize;shaders.InsertArrayElementAtIndex(n);shaders.GetArrayElementAtIndex(n).objectReferenceValue=shader;}
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssets();
    }
}
