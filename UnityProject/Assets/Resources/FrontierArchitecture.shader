Shader "Orbit/FrontierArchitecture" {
 Properties {_BaseColor("Coating",Color)=(.2,.3,.4,1) _Accent("Lighting",Color)=(.1,.8,1,1) _Metallic("Metal",Range(0,1))=.65 _Glass("Window facade",Float)=0 _Glow("Emission",Float)=0}
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass {HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.5
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor,_Accent;float _Metallic,_Glass,_Glow;
 CBUFFER_END
 struct A {float4 p:POSITION;float3 n:NORMAL;};struct V {float4 p:SV_POSITION;float3 local:TEXCOORD0;float3 world:TEXCOORD1;float3 n:TEXCOORD2;};
 V vert(A i){V o;o.local=i.p.xyz;o.world=TransformObjectToWorld(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);o.p=TransformWorldToHClip(o.world);return o;}
 half4 frag(V i):SV_Target {
  float3 n=normalize(i.n),v=normalize(_WorldSpaceCameraPos-i.world);Light l=GetMainLight(TransformWorldToShadowCoord(i.world));float3 h=normalize(l.direction+v);
  float3 q=i.local;float2 panel=abs(n.y)>.65?q.xz:abs(n.x)>.65?q.zy:q.xy;float2 grid=frac(panel*float2(.48,.62));
  float seam=smoothstep(.015,.045,min(min(grid.x,1-grid.x),min(grid.y,1-grid.y)));
  float wear=frac(sin(dot(floor(q*65),float3(12.98,78.23,37.71)))*43758.54);float rib=(.91+.09*seam)*(.96+.04*wear);float diffuse=saturate(dot(n,l.direction));float fresnel=pow(1-saturate(dot(v,n)),4);
  float spec=pow(saturate(dot(n,h)),lerp(42,160,_Glass))*(.25+_Metallic*.7)*l.shadowAttenuation;
  float windows=step(.15,grid.x)*step(grid.x,.82)*step(.25,grid.y)*step(grid.y,.66)*step(abs(n.y),.5);
  float hash=frac(sin(dot(floor(panel*float2(.48,.62)),float2(12.9898,78.233)))*43758.5453);windows*=step(.30,hash);
  float3 ambient=lerp(float3(.20,.25,.32),float3(.30,.37,.46),saturate(n.y*.5+.5));
  float3 color=_BaseColor.rgb*rib*(ambient+l.color*diffuse*l.shadowAttenuation*.85)+l.color*spec*lerp(float3(.04,.04,.04),_BaseColor.rgb,_Metallic)*2+float3(.10,.19,.27)*fresnel*_Metallic;
  #if defined(_ADDITIONAL_LIGHTS)
  uint extra=GetAdditionalLightsCount();for(uint index=0;index<min(extra,4u);index++){Light a=GetAdditionalLight(index,i.world);color+=_BaseColor.rgb*a.color*saturate(dot(n,a.direction))*a.distanceAttenuation*.5;}
  #endif
  color+=_Accent.rgb*(_Glow+windows*_Glass*1.05);return half4(color,1);
 }
 ENDHLSL}
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 }
}
