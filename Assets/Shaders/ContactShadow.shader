Shader "Farm/ContactShadow"
{
    Properties { _BaseMap("Ombre douce",2D)="white"{} _Strength("Intensité",Range(0,1))=.8 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            half _Strength;
            CBUFFER_END
            struct Input { float4 positionOS:POSITION;float2 uv:TEXCOORD0; };
            struct Output { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0; };
            Output Vert(Input i){Output o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
            half4 Frag(Output i):SV_Target { return half4(.035,.055,.035,SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a*_Strength); }
            ENDHLSL
        }
    }
}
