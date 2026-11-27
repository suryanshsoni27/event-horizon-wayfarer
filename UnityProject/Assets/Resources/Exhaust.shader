Shader "Orbit/Exhaust" {SubShader{Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent"} Pass{Blend SrcAlpha One ZWrite Off Cull Off HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
half4 frag(V i):SV_Target{float f=pow(saturate(1-i.uv.y),1.7);float pulse=.85+.15*sin(i.uv.y*31-_Time.y*38+i.uv.x*9);float3 c=lerp(float3(.8,1,2),float3(.05,.18,1.3),i.uv.y);return half4(c*1.7,f*pulse*.7);}
ENDHLSL}}}
