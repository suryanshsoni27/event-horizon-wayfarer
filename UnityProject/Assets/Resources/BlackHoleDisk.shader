Shader "Orbit/BlackHoleDisk" {SubShader{Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent"} Pass{Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
half4 frag(V i):SV_Target{float2 p=(i.uv-.5)*18;float r=length(p);if(r<3||r>8)discard;float a=atan2(p.y,p.x)-_Time.y*.08;float grain=.65+.2*sin(a*13+r*5+sin(a*3)*2)+.1*sin(a*29-r*11);float energy=smoothstep(3,3.6,r)*(1-smoothstep(6,8,r))*grain;return half4(lerp(float3(.7,.19,.035),float3(1,.78,.4),pow(3/r,1.4))*energy*1.8,energy*.9);}
ENDHLSL}}}
