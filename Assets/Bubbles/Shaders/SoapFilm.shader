Shader "Bubbles/Soap Film"
{
    Properties
    {
        _HDRI("HDR panorama",2D)="gray" {}
        _EnvironmentYaw("Panorama direction (turns)",Float)=0
        _Exposure("HDR exposure",Float)=0
        _ViewTanHalfFov("View width",Float)=.85
        _IndexOfRefraction("Membrane refractive index",Range(1,1.6))=1.333
        _RefractionStrength("Visible refraction",Range(0,1.5))=.85
        _Dispersion("Colour dispersion",Range(0,.04))=.012
        _FilmThickness("Film thickness (nm)",Range(100,900))=410
        _FilmVariation("Drainage and flow (nm)",Range(0,300))=165
        _Iridescence("Interference strength",Range(0,2))=1.1
        _PatternStrength("Flowing interference pattern",Range(0,1))=.65
        _Reflection("HDR reflections",Range(0,3))=1.1
        _TransmissionScale("Transmission visibility adjustment",Range(0,1))=.86
        _Seed("Flow seed",Float)=0
        _BubbleTime("Simulation time",Float)=0
        _ShapeTime("Shape animation time",Float)=0
        _Wobble("Touch oscillation",Float)=0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _EnvironmentYaw,_Exposure,_ViewTanHalfFov;
            float _IndexOfRefraction,_RefractionStrength,_Dispersion;
            float _FilmThickness,_FilmVariation,_Iridescence,_Reflection,_PatternStrength;
            float _Seed,_BubbleTime,_ShapeTime,_Wobble,_TransmissionScale;
            CBUFFER_END
            #include "EnvironmentSampling.hlsl"
            struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float3 local:TEXCOORD2;float4 screen:TEXCOORD3;};
            V Vert(A i)
            {
                V o;float3 p=i.positionOS.xyz;
                float ripple=sin(p.y*9+_ShapeTime*1.1+_Seed)*sin(p.x*7-_ShapeTime*.8+_Seed)*.008;
                p+=i.normalOS*(ripple+sin(p.y*10+p.x*7+_ShapeTime*20)*_Wobble*.014);
                o.positionWS=TransformObjectToWorld(p);o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.local=i.normalOS;o.screen=ComputeScreenPos(o.positionCS);return o;
            }
            float3 LensRay(float3 incident,float3 normal,float index)
            {
                return normalize(lerp(incident,refract(incident,normal,1/max(index,1.001)),_RefractionStrength));
            }
            half4 Frag(V i):SV_Target
            {
                float3 n=normalize(i.normalWS),v=GetWorldSpaceNormalizeViewDir(i.positionWS);
                float nv=saturate(dot(n,v)),edge=pow(1-nv,2.5);
                float3 incident=EnvironmentRay(i.screen.xy/i.screen.w);
                float3 transmission;
                transmission.r=EnvironmentColour(LensRay(incident,n,_IndexOfRefraction-_Dispersion)).r;
                transmission.g=EnvironmentColour(LensRay(incident,n,_IndexOfRefraction)).g;
                transmission.b=EnvironmentColour(LensRay(incident,n,_IndexOfRefraction+_Dispersion)).b;
                float3 reflected=EnvironmentColour(reflect(-v,n));
                float flow=sin(i.local.y*7+i.local.x*2+sin(i.local.z*5+_BubbleTime*.16+_Seed)*1.2-_BubbleTime*.22+_Seed);
                flow+=.38*sin(i.local.y*16-i.local.x*5+_BubbleTime*.13+_Seed*2);
                float swirl=sin(i.local.x*9+i.local.y*6+sin(i.local.y*8-_BubbleTime*.19+_Seed)*1.7+_Seed);
                float bands=i.local.y*2.1+flow*.48+swirl*.22;
                float thickness=max(80,_FilmThickness+_FilmVariation*lerp(i.local.y*.65+flow*.38,bands,_PatternStrength));
                float cosFilm=sqrt(1-(1-nv*nv)/(_IndexOfRefraction*_IndexOfRefraction));
                float3 phase=12.5663706*_IndexOfRefraction*thickness*cosFilm/float3(650,510,440);
                float r0=pow((_IndexOfRefraction-1)/(_IndexOfRefraction+1),2);
                float r=r0+(1-r0)*pow(1-nv,5);
                // 内部反射を含めた、吸収のない空気と膜の二つの界面。
                // 各界面の反射率をSchlick近似し、膜の透過率を1-Rとする。
                float membraneReflectance=saturate(2*r/(1+r));
                float3 interference=(2*r*(1-cos(phase)))/(1+r*r-2*r*cos(phase)+.0001);
                float3 film=max(0,lerp(interference.xxx,interference,_Iridescence));
                // 正規化した分光色で、膜全体に流れる干渉模様を表す。
                float3 spectral=.5+.5*cos(phase);
                spectral/=max(max(spectral.r,spectral.g),max(spectral.b,.001));
                float reflectionWeight=membraneReflectance;
                float3 colour=transmission*(1-reflectionWeight)+reflected*reflectionWeight*_Reflection;
                float pattern=_PatternStrength*(.50+.18*edge);
                colour=lerp(colour,colour*(.40+spectral*.85)+spectral*.18,pattern);
                colour+=film*(.10+edge*.38);
                colour+=pow(1-nv,6)*.18;
                // 明るい空でも、環境色を映した薄い縁が見えるようにする。
                colour=lerp(colour,reflected*.25+film*.13,pow(1-nv,4)*.86);
                // フレネル反射の角度特性を維持し、視認性を少し補う。
                float alpha=1-(1-membraneReflectance)*_TransmissionScale;
                return half4(colour*alpha,alpha);
            }
            ENDHLSL
        }
    }
}
