Shader "Bubbles/Quiet Background"
{
    Properties
    {
        _HDRI("HDR panorama",2D)="gray" {}
        _EnvironmentYaw("Panorama direction (turns)",Float)=0
        _Exposure("HDR exposure",Float)=0
        _ViewTanHalfFov("View width",Float)=.85
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Opaque" }
        Pass
        {
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _EnvironmentYaw,_Exposure,_ViewTanHalfFov;
            CBUFFER_END
            #include "EnvironmentSampling.hlsl"
            struct V { float4 positionCS:SV_POSITION; float4 screen:TEXCOORD0; };
            V Vert(float4 p:POSITION){V o;o.positionCS=TransformObjectToHClip(p.xyz);o.screen=ComputeScreenPos(o.positionCS);return o;}
            half4 Frag(V i):SV_Target{return half4(EnvironmentColour(EnvironmentRay(i.screen.xy/i.screen.w)),1);}
            ENDHLSL
        }
    }
}
