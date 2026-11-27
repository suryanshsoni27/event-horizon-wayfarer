Shader "Orbit/XenoChitin" {
 Properties { _Hurt("Damage flash", Float)=0 _Dissolve("Corpse dissolve",Float)=0 }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" } Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.5
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile _ _ADDITIONAL_LIGHTS
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 struct A {float4 p:POSITION;float3 n:NORMAL;float4 c:COLOR;};
 struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float3 local:TEXCOORD2;float4 c:COLOR;};float _Hurt,_Dissolve;
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.world=TransformObjectToWorld(a.p.xyz);o.n=TransformObjectToWorldNormal(a.n);o.local=a.p.xyz;o.c=a.c;return o;}
 half4 frag(V i):SV_Target {
  float dither=frac(sin(dot(floor(i.world*43),float3(12.9898,78.233,37.719)))*43758.5453);clip(dither-_Dissolve);float3 n=normalize(i.n),v=normalize(_WorldSpaceCameraPos-i.world);float ridges=.5+.5*sin(i.local.y*31+sin(i.local.z*17)*2+sin(i.local.x*23));float pores=.5+.5*sin(i.local.x*97)*sin(i.local.y*103)*sin(i.local.z*89);
  float glow=saturate(i.c.a);float3 albedo=i.c.rgb*(.75+.2*ridges+.05*pores);Light l=GetMainLight(TransformWorldToShadowCoord(i.world));
  float diff=saturate(dot(n,l.direction))*l.shadowAttenuation;float spec=pow(saturate(dot(n,normalize(v+l.direction))),64)*.55;
  float3 color=albedo*(float3(.07,.1,.13)+l.color*(.12+diff*.95))+l.color*spec*l.shadowAttenuation;
  color+=pow(1-saturate(dot(n,v)),3)*albedo*.45;uint count=GetAdditionalLightsCount();for(uint k=0;k<count;k++){Light a=GetAdditionalLight(k,i.world);color+=albedo*a.color*saturate(dot(n,a.direction))*a.distanceAttenuation;}
  color+=i.c.rgb*glow*(2.5+.3*sin(_Time.y*3+i.local.y*4));color=lerp(color,float3(2,.3,.1),saturate(_Hurt));return half4(color,1);
 }
 ENDHLSL }
 Pass {Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} ZWrite On ColorMask 0 HLSLPROGRAM
 #pragma vertex sv
 #pragma fragment sf
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
 float3 _LightDirection;struct A{float4 p:POSITION;float3 n:NORMAL;};float4 sv(A a):SV_POSITION{return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(TransformObjectToWorld(a.p.xyz),TransformObjectToWorldNormal(a.n),_LightDirection)));}half4 sf():SV_Target{return 0;}
 ENDHLSL }
 }
}
