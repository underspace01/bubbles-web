Shader "Bubbles/Water Droplet"
{
    Properties { _Edge("Water edge",Color)=(.05,.16,.22,1) _Shine("Water highlight",Color)=(.94,.98,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Edge,_Shine;
            CBUFFER_END
            struct A {float4 positionOS:POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;};
            V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.color=i.color;o.uv=i.uv;return o;}
            half4 Frag(V i):SV_Target
            {
                float2 p=(i.uv-.5)*2;float d=length(p/float2(.73-.16*p.y,1));float shape=1-smoothstep(.85,1,d);
                float edge=smoothstep(.45,.95,d);float glint=exp(-dot((p-float2(-.20,.25))*float2(6,4),(p-float2(-.20,.25))*float2(6,4)));
                float3 colour=lerp(i.color.rgb,_Edge.rgb,edge*.85);colour=lerp(colour,_Shine.rgb,glint*.98);
                return half4(colour,shape*i.color.a*(.35+edge*.58+glint*.4));
            }
            ENDHLSL
        }
    }
}
