Shader "Orbit/FrontierStar" {Properties{_Color("Star spectrum",Color)=(1,.45,.08,1)} SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}Pass{HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
float4 _Color;struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 q:TEXCOORD0;float3 n:TEXCOORD1;float3 w:TEXCOORD2;};V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.q=i.p.xyz;o.n=TransformObjectToWorldNormal(i.n);o.w=TransformObjectToWorld(i.p.xyz);return o;}
float noise(float3 p){return frac(sin(dot(floor(p),float3(12.9898,78.233,41.7)))*43758.5453);}
half4 frag(V i):SV_Target{float3 p=normalize(i.q)*36;float n=noise(p+_Time.y*.05)*.4+noise(p*2)*.25+sin(p.y*2+sin(p.x*3+_Time.y*.2))*.15;float rim=pow(saturate(dot(normalize(i.n),normalize(_WorldSpaceCameraPos-i.w))),.3);return half4(_Color.rgb*(1.1+n)*lerp(.55,1.6,rim)+float3(1,.35,.03)*.3,1);}
ENDHLSL}}}
