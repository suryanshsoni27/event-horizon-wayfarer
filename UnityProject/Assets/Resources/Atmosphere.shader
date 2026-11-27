Shader "Orbit/Atmosphere" {
 Properties {_PlanetCenter("Center",Vector)=(0,0,0,0) _GroundRadius("Ground",Float)=22 _AtmosRadius("Atmosphere",Float)=30 _Tint("Scattering",Color)=(.15,.4,1,1)}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10"} Pass {Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;};float4 _PlanetCenter,_Tint;float _GroundRadius,_AtmosRadius;
 V vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);return o;}
 half4 frag(V i,bool front:SV_IsFrontFace):SV_Target {
  float3 camera=_WorldSpaceCameraPos-_PlanetCenter.xyz;bool inside=length(camera)<_AtmosRadius;if(inside==front)discard;
  float3 direction=normalize(i.world-_WorldSpaceCameraPos);float b=dot(camera,direction),c=dot(camera,camera)-_AtmosRadius*_AtmosRadius,disc=b*b-c;if(disc<0)discard;
  float entry=max(0,-b-sqrt(disc)),exit=-b+sqrt(disc);
  float2 uv=i.p.xy/_ScaledScreenParams.xy;float depth=SampleSceneDepth(uv);float eye=LinearEyeDepth(depth,_ZBufferParams);float3 forward=-UNITY_MATRIX_V[2].xyz;float scene=eye/max(.001,dot(direction,forward));exit=min(exit,scene);if(exit<=entry)discard;
  float groundDisc=b*b-dot(camera,camera)+_GroundRadius*_GroundRadius;if(groundDisc>0){float groundHit=-b-sqrt(groundDisc);if(groundHit>entry)exit=min(exit,groundHit);}if(exit<=entry)discard;
  float stepSize=(exit-entry)/12,optical=0,lit=0;Light sun=GetMainLight();
  [unroll]for(int k=0;k<12;k++){float3 p=camera+direction*(entry+(k+.5)*stepSize);float altitude=max(0,length(p)-_GroundRadius);float density=exp(-altitude/max(.1,(_AtmosRadius-_GroundRadius)*.24));float illumination=saturate(dot(normalize(p),sun.direction)*3+.3);optical+=density*stepSize;lit+=density*illumination*stepSize;}
  float phase=.75*(1+pow(dot(direction,sun.direction),2));float alpha=1-exp(-optical*(4.2/max(8,_AtmosRadius-_GroundRadius)));float lightRatio=lit/max(.001,optical);float3 scatter=_Tint.rgb*(.025+lightRatio*phase*.85);scatter+=float3(1,.36,.1)*pow(saturate(dot(direction,sun.direction)),24)*lightRatio*.3;
  return half4(scatter,alpha);
 }
 ENDHLSL}}}
