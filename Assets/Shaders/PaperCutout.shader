Shader "Farm/PaperCutout"
{
    Properties
    {
        _BaseMap("Illustration",2D)="white"{}
        _BaseColor("Teinte",Color)=(1,1,1,1)
        _Cutoff("Découpe alpha",Range(0,1))=.12
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        half _Cutoff;
        CBUFFER_END
        struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
        struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionWS:TEXCOORD1; half3 normalWS:TEXCOORD2; half fogFactor:TEXCOORD3; };
        Varyings Vert(Attributes input)
        {
            Varyings o;o.positionWS=TransformObjectToWorld(input.positionOS.xyz);
            o.positionCS=TransformWorldToHClip(o.positionWS);o.uv=input.uv;
            o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.fogFactor=ComputeFogFactor(o.positionCS.z);return o;
        }
        half4 Illustration(float2 uv){half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv)*_BaseColor;clip(c.a-_Cutoff);return c;}
        ENDHLSL
        Pass
        {
            Name "PaperLit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            half4 Frag(Varyings input):SV_Target
            {
                half4 c=Illustration(input.uv);
                // Normale adoucie : garde l'illustration lisible tout en recevant la lumière 3D.
                half3 n=normalize(input.normalWS+half3(0,.7,0));
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 illumination=SampleSH(n)+sun.color*(.25+.75*saturate(dot(n,sun.direction)))*sun.distanceAttenuation*sun.shadowAttenuation;
                #if defined(_ADDITIONAL_LIGHTS)
                uint count=GetAdditionalLightsCount();
                for(uint i=0;i<count;i++){Light light=GetAdditionalLight(i,input.positionWS);illumination+=light.color*(.2+.8*saturate(dot(n,light.direction)))*light.distanceAttenuation*light.shadowAttenuation;}
                #endif
                half3 color=c.rgb*max(illumination,half3(.12,.12,.17));
                color=MixFog(color,input.fogFactor);
                return half4(color,1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection;
            float3 _LightPosition;
            Varyings ShadowVert(Attributes input)
            {
                Varyings o;
                float3 position=TransformObjectToWorld(input.positionOS.xyz);
                float3 normal=TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 direction=normalize(_LightPosition-position);
                #else
                float3 direction=_LightDirection;
                #endif
                o.positionCS=TransformWorldToHClip(ApplyShadowBias(position,normal,direction));
                #if UNITY_REVERSED_Z
                o.positionCS.z=min(o.positionCS.z,o.positionCS.w*UNITY_NEAR_CLIP_VALUE);
                #else
                o.positionCS.z=max(o.positionCS.z,o.positionCS.w*UNITY_NEAR_CLIP_VALUE);
                #endif
                o.uv=input.uv;o.positionWS=position;o.normalWS=normal;o.fogFactor=0;return o;
            }
            half4 ShadowFrag(Varyings input):SV_Target { Illustration(input.uv);return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            half4 DepthFrag(Varyings input):SV_Target{Illustration(input.uv);return input.positionCS.z;}
            ENDHLSL
        }
    }
}
