Shader "Orbit/Clouds" {Properties{_Seed("Seed",Float)=4 _Coverage("Cloud coverage",Float)=1} SubShader{Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20"} Pass{Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 dir:TEXCOORD0;float3 world:TEXCOORD1;};float _Seed,_Coverage;
V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.dir=a.p.xyz;o.world=TransformObjectToWorld(a.p.xyz);return o;}
float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}float noise(float3 p){float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+1),f.x),f.y),f.z);}
half4 frag(V i):SV_Target{float3 n=normalize(i.dir),p=n*13+float3(_Time.y*.006,0,_Seed);float cloud=noise(p)*.55+noise(p*2.1)*.28+noise(p*4.3)*.12+noise(p*9)*.05;float alpha=smoothstep(.55,.73,cloud)*.65*_Coverage;float3 cameraLocal=TransformWorldToObject(_WorldSpaceCameraPos);float worldScale=length(UNITY_MATRIX_M[0].xyz);alpha*=smoothstep(2,25,abs(length(cameraLocal)-.5)*worldScale);Light sun=GetMainLight();float light=saturate(dot(n,sun.direction)*1.5+.15);float3 color=lerp(float3(.05,.07,.11),float3(.93,.92,.86),light);return half4(color,alpha);}
ENDHLSL}}}
