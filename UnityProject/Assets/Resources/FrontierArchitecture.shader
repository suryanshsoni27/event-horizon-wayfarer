Shader "Orbit/FrontierArchitecture" {
 Properties {
  _BaseColor("Coating",Color)=(.2,.3,.4,1) _Accent("Lighting",Color)=(.1,.8,1,1)
  _Metallic("Metal",Range(0,1))=.65 _Smoothness("Smoothness",Range(0,1))=.5 _ClearCoat("Canopy clear coat",Range(0,1))=0
  _Glass("Window facade",Float)=0 _Glow("Emission",Float)=0 _Fabric("Pressure fabric",Float)=0
  _DetailMap("Coating wear",2D)="white"{} _NormalMap("Surface normal",2D)="bump"{} _ArmMap("Occlusion roughness metal",2D)="white"{}
  _DetailScale("Detail scale",Float)=.65 _NormalStrength("Normal strength",Float)=.2 _Wear("Wear",Range(0,1))=.4
 }
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor,_Accent;float _Metallic,_Smoothness,_ClearCoat,_Glass,_Glow,_Fabric,_DetailScale,_NormalStrength,_Wear;
 CBUFFER_END
 ENDHLSL
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass {Name "ForwardLit" Tags{"LightMode"="UniversalForward"} HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.5
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
 #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
 #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
 #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
 #define _CLEARCOAT 1
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_DetailMap);SAMPLER(sampler_DetailMap);TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);TEXTURE2D(_ArmMap);SAMPLER(sampler_ArmMap);
 struct A {float4 p:POSITION;float3 n:NORMAL;};
 struct V {float4 p:SV_POSITION;float3 local:TEXCOORD0;float3 world:TEXCOORD1;half3 n:TEXCOORD2;half3 localN:TEXCOORD3;};
 V vert(A i){V o;o.local=i.p.xyz;o.world=TransformObjectToWorld(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);o.localN=i.n;o.p=TransformWorldToHClip(o.world);return o;}
 float3 TriSample(TEXTURE2D_PARAM(tex,samp),float3 p,float3 w){return SAMPLE_TEXTURE2D(tex,samp,p.yz).rgb*w.x+SAMPLE_TEXTURE2D(tex,samp,p.zx).rgb*w.y+SAMPLE_TEXTURE2D(tex,samp,p.xy).rgb*w.z;}
 half4 frag(V i):SV_Target {
  float3 localNormal=normalize(i.localN),n=normalize(i.n),q=i.local;float3 weights=pow(abs(localNormal),6);weights/=max(.0001,weights.x+weights.y+weights.z);
  float3 photo=TriSample(TEXTURE2D_ARGS(_DetailMap,sampler_DetailMap),q*_DetailScale,weights);
  float3 arm=TriSample(TEXTURE2D_ARGS(_ArmMap,sampler_ArmMap),q*_DetailScale,weights);
  float3 detail=TriSample(TEXTURE2D_ARGS(_NormalMap,sampler_NormalMap),q*_DetailScale,weights)*2-1;
  float distance=length(_WorldSpaceCameraPos-i.world),detailFade=1-smoothstep(35,130,distance);
  float3 perturb=abs(localNormal.y)>.65?float3(detail.x,0,detail.y):abs(localNormal.x)>.65?float3(0,detail.x,detail.y):float3(detail.x,detail.y,0);
  perturb=TransformObjectToWorldDir(perturb);n=normalize(n+(perturb-n*dot(n,perturb))*_NormalStrength*detailFade);
  float2 panel=abs(localNormal.y)>.65?q.xz:abs(localNormal.x)>.65?q.zy:q.xy;
  float2 grid=frac(panel*float2(.65,.7)),pixel=max(fwidth(panel).x,fwidth(panel).y);
  float edge=min(min(grid.x,1-grid.x),min(grid.y,1-grid.y));float seam=smoothstep(.006,.015+pixel,edge);
  float luminosity=dot(photo,float3(.299,.587,.114));float coating=lerp(1,clamp(.75+luminosity*.7,.65,1.15),_Wear*(1-_Fabric));
  float3 albedo=_BaseColor.rgb*coating*lerp(.86,1,seam*(1-_Fabric)+_Fabric);
  float2 facade=frac(panel*float2(.36,.3));float2 cell=floor(panel*float2(.36,.3));
  float hash=frac(sin(dot(cell,float2(12.9898,78.233)))*43758.5453);
  float windows=step(.22,facade.x)*step(facade.x,.78)*step(.37,facade.y)*step(facade.y,.64)*step(abs(localNormal.y),.5)*step(.42,hash);
  windows*=1-smoothstep(.10,.32,pixel);
  SurfaceData surface=(SurfaceData)0;surface.albedo=albedo;surface.metallic=_Metallic;surface.smoothness=clamp(lerp(_Smoothness,1-arm.g,_Wear*.45),.08,.96);
  surface.occlusion=lerp(1,arm.r,.45)*lerp(.92,1,seam);surface.normalTS=half3(0,0,1);surface.alpha=1;
  surface.clearCoatMask=_ClearCoat;surface.clearCoatSmoothness=.96;surface.emission=_Accent.rgb*(_Glow+windows*_Glass*.65);
  InputData input=(InputData)0;input.positionWS=i.world;input.positionCS=i.p;input.normalWS=n;input.viewDirectionWS=SafeNormalize(_WorldSpaceCameraPos-i.world);input.shadowCoord=TransformWorldToShadowCoord(i.world);
  input.bakedGI=SampleSH(n);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);input.shadowMask=half4(1,1,1,1);input.vertexLighting=VertexLighting(i.world,n);
  return UniversalFragmentPBR(input,surface);
 }
 ENDHLSL}
 Pass {Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask 0 HLSLPROGRAM
 #pragma vertex depthVert
 #pragma fragment depthFrag
 float4 depthVert(float4 p:POSITION):SV_POSITION{return TransformObjectToHClip(p.xyz);} half4 depthFrag():SV_Target{return 0;}
 ENDHLSL}
 Pass {Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} ZWrite On ColorMask 0 HLSLPROGRAM
 #pragma vertex shadowVert
 #pragma fragment shadowFrag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
 float3 _LightDirection;struct ShadowA{float4 p:POSITION;float3 n:NORMAL;};float4 shadowVert(ShadowA a):SV_POSITION{return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(TransformObjectToWorld(a.p.xyz),TransformObjectToWorldNormal(a.n),_LightDirection)));} half4 shadowFrag():SV_Target{return 0;}
 ENDHLSL}
 }
}
