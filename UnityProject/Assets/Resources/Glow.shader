Shader "Orbit/Glow" { Properties { _Color("Color",Color)=(1,1,1,1) } SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" } Pass { Blend SrcAlpha One ZWrite Off Cull Off HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A { float4 positionOS:POSITION; }; struct V { float4 positionCS:SV_POSITION; }; float4 _Color;
V vert(A i){ V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); return o; }
half4 frag(V i):SV_Target{return _Color;}
ENDHLSL } } }
