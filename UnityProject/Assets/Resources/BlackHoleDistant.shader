Shader "Orbit/BlackHoleDistant" {
 Properties {_Inclination("Disk inclination",Float)=.3}
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-30" "RenderType"="Transparent"}Pass {Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};float _Inclination;
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target{
  float2 p=(i.uv-.5)*18;float r=length(p);if(r>8.8)discard;
  // Angular LOD only: nearby views use the numerical Schwarzschild solver.
  float tilt=max(.17,abs(_Inclination)),diskR=length(float2(p.x,p.y/tilt));float phi=atan2(p.y/tilt,p.x)-_Time.y*.13;
  float grain=.68+.20*sin(phi*17+diskR*8+sin(phi*5))+.1*sin(phi*37-diskR*19);
  float annulus=smoothstep(2.8,3.3,diskR)*(1-smoothstep(6.9,8.3,diskR))*grain;
  float lensR=length(float2(p.x,p.y*1.05)),arc=exp(-pow((lensR-3.05)/.25,2))*smoothstep(.35,1.3,p.y)*(.3+.7*(1-tilt));
  float photon=exp(-pow((r-2.62)/.065,2));float front=step(p.y,0);annulus*=r<2.58?front:1;
  float energy=annulus+arc*.65+photon*.8;float3 heat=lerp(float3(1.8,.25,.04),float3(3.2,1.7,.65),saturate(1-(diskR-3)/5));
  if(r<2.56&&annulus<.02)return half4(0,0,0,1);float alpha=saturate(energy*1.2);return half4(heat*energy,alpha);
 }
 ENDHLSL}}
}
