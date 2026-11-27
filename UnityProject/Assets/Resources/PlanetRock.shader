Shader "Orbit/PlanetRock" {
 Properties{_BaseMap("Rock albedo",2D)="white"{} _BumpMap("Rock normal",2D)="bump"{} _Climate("Climate",Float)=0}
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float _Climate;
 CBUFFER_END
 ENDHLSL
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass{Name "ForwardLit" Tags{"LightMode"="UniversalForward"} HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
 #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
 struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 local:TEXCOORD1;half3 n:TEXCOORD2;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.world=TransformObjectToWorld(a.p.xyz);o.local=a.p.xyz;o.n=TransformObjectToWorldNormal(a.n);return o;}
 half4 frag(V i):SV_Target{
  float3 n=normalize(i.n),w=pow(abs(n),4);w/=max(.0001,w.x+w.y+w.z);float3 p=i.local*.32;
  float3 photo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.yz).rgb*w.x+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz).rgb*w.y+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy).rgb*w.z;
  float2 a=SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,p.yz).xy*2-1,b=SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,p.xz).xy*2-1,c=SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,p.xy).xy*2-1;
  float3 detail=float3(0,a.x,a.y)*w.x+float3(b.x,0,b.y)*w.y+float3(c.x,c.y,0)*w.z;detail-=n*dot(n,detail);
  float arid=step(.5,_Climate)*(1-step(1.5,_Climate)),ice=step(1.5,_Climate),gray=dot(photo,float3(.3,.59,.11));
  float snow=smoothstep(.25,.7,dot(n,normalize(i.local)))*ice;
  float3 albedo=lerp(photo*.64,gray*float3(.74,.49,.28),arid);albedo=lerp(albedo,gray*float3(.55,.65,.72),ice);albedo=lerp(albedo,float3(.62,.68,.71)*(.9+gray*.2),snow);
  n=normalize(n+detail*.19*(1-snow*.8)*(1-smoothstep(35,120,distance(_WorldSpaceCameraPos,i.world))));
  SurfaceData s=(SurfaceData)0;s.albedo=albedo;s.smoothness=.17+ice*.12+snow*.06;s.occlusion=1;s.normalTS=half3(0,0,1);s.alpha=1;
  InputData d=(InputData)0;d.positionWS=i.world;d.positionCS=i.p;d.normalWS=n;d.viewDirectionWS=SafeNormalize(_WorldSpaceCameraPos-i.world);d.shadowCoord=TransformWorldToShadowCoord(i.world);d.bakedGI=SampleSH(n);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);d.shadowMask=half4(1,1,1,1);d.vertexLighting=VertexLighting(i.world,n);
  return UniversalFragmentPBR(d,s);
 }
 ENDHLSL}
 Pass{Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask 0 HLSLPROGRAM
 #pragma vertex dv
 #pragma fragment df
 float4 dv(float4 p:POSITION):SV_POSITION{return TransformObjectToHClip(p.xyz);}half4 df():SV_Target{return 0;}
 ENDHLSL}
 Pass{Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} ZWrite On ColorMask 0 HLSLPROGRAM
 #pragma vertex sv
 #pragma fragment sf
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
 float3 _LightDirection;struct A{float4 p:POSITION;float3 n:NORMAL;};float4 sv(A a):SV_POSITION{return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(TransformObjectToWorld(a.p.xyz),TransformObjectToWorldNormal(a.n),_LightDirection)));}half4 sf():SV_Target{return 0;}
 ENDHLSL}
 }
}
