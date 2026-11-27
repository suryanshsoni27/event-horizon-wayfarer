Shader "Orbit/PlanetSurface" {
 Properties {
  _Radius("Radius",Float)=1200 _Seed("Seed",Float)=4 _Climate("Temperate, arid, ice",Float)=0
  _Land("Land",Color)=(.2,.3,.16,1) _Ocean("Ocean",Color)=(.01,.07,.10,1)
  _GrassTex("Grass surface",2D)="white"{} _RockTex("Rock surface",2D)="white"{} _GrassNormal("Grass detail normals",2D)="bump"{} _RockNormal("Rock detail normals",2D)="bump"{}
 }
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float _Radius,_Seed,_Climate;float4 _Land,_Ocean;
 CBUFFER_END
 ENDHLSL
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass {Name "ForwardLit" Tags{"LightMode"="UniversalForward"} HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
 #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_GrassTex);SAMPLER(sampler_GrassTex);TEXTURE2D(_RockTex);SAMPLER(sampler_RockTex);TEXTURE2D(_GrassNormal);SAMPLER(sampler_GrassNormal);TEXTURE2D(_RockNormal);SAMPLER(sampler_RockNormal);
 struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float3 local:TEXCOORD2;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.world=TransformObjectToWorld(a.p.xyz);o.normal=TransformObjectToWorldNormal(a.n);o.local=a.p.xyz;return o;}
 float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
 float noise(float3 p){float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+1),f.x),f.y),f.z);}
 float3 triColor(TEXTURE2D_PARAM(tex,samp),float3 p,float3 w){return SAMPLE_TEXTURE2D(tex,samp,p.yz).rgb*w.x+SAMPLE_TEXTURE2D(tex,samp,p.xz).rgb*w.y+SAMPLE_TEXTURE2D(tex,samp,p.xy).rgb*w.z;}
 float3 triDetail(TEXTURE2D_PARAM(tex,samp),float3 p,float3 w){float2 x=SAMPLE_TEXTURE2D(tex,samp,p.yz).xy*2-1,y=SAMPLE_TEXTURE2D(tex,samp,p.xz).xy*2-1,z=SAMPLE_TEXTURE2D(tex,samp,p.xy).xy*2-1;return float3(0,x.x,x.y)*w.x+float3(y.x,0,y.y)*w.y+float3(z.x,z.y,0)*w.z;}
 half4 frag(V i):SV_Target {
  float3 radial=normalize(i.local),n=normalize(i.normal),view=SafeNormalize(_WorldSpaceCameraPos-i.world);float height=length(i.local)-_Radius,scaledHeight=height*1200/max(100,_Radius);
  float broad=noise(i.local*.012+_Seed),grain=noise(i.local*.16),slope=1-saturate(dot(n,radial));
  float water=1-smoothstep(.08,.7,height),snow=smoothstep(140,195,scaledHeight+abs(radial.y)*20);
  float arid=step(.5,_Climate)*(1-step(1.5,_Climate)),ice=step(1.5,_Climate);snow=lerp(snow,0,arid);snow=lerp(snow,saturate(.68+scaledHeight*.004),ice);
  if(_Radius<100){water=1-smoothstep(-.24,-.03,height);snow=smoothstep(.8,1.4,height+abs(radial.y)*.25);}
  float3 weights=pow(abs(n),4);weights/=max(.0001,weights.x+weights.y+weights.z);
  float3 grassPhoto=triColor(TEXTURE2D_ARGS(_GrassTex,sampler_GrassTex),i.local*.28,weights),rockPhoto=triColor(TEXTURE2D_ARGS(_RockTex,sampler_RockTex),i.local*.12,weights);
  float rockAmount=saturate(slope*3.4+smoothstep(80,160,scaledHeight)*.3);
  float3 sand=lerp(float3(.27,.22,.135),float3(.33,.145,.065),arid),stone=rockPhoto*lerp(.85,1.05,broad);
  stone=lerp(stone,stone*float3(1.1,.7,.45),arid*.65);
  float3 grass=grassPhoto*(_Land.rgb*3.1)*lerp(.82,1.12,broad);float3 land=lerp(sand,grass,smoothstep(.5,4,scaledHeight)*(1-arid));land=lerp(land,stone,rockAmount);
  land=lerp(land,float3(.63,.71,.77),snow*(1-saturate(slope*2.1)));land*=.94+.06*grain;
  float detailFade=1-smoothstep(30,130,length(_WorldSpaceCameraPos-i.world));
  float3 detail=lerp(triDetail(TEXTURE2D_ARGS(_GrassNormal,sampler_GrassNormal),i.local*.28,weights),triDetail(TEXTURE2D_ARGS(_RockNormal,sampler_RockNormal),i.local*.12,weights),rockAmount);detail-=n*dot(n,detail);n=normalize(n+detail*.15*detailFade*(1-water)*(1-snow*.7));
  float3 wave=float3(sin(i.local.x*.07+_Time.y)*cos(i.local.z*.08),0,cos(i.local.z*.08-_Time.y*.7)*sin(i.local.x*.06));n=normalize(n+wave*.025*water);
  SurfaceData surface=(SurfaceData)0;surface.albedo=lerp(land,_Ocean.rgb*(.8+grain*.2),water);surface.metallic=0;surface.smoothness=lerp(.14+snow*.13,.94,water);surface.occlusion=1;surface.normalTS=half3(0,0,1);surface.alpha=1;
  InputData input=(InputData)0;input.positionWS=i.world;input.positionCS=i.p;input.normalWS=n;input.viewDirectionWS=view;input.shadowCoord=TransformWorldToShadowCoord(i.world);input.bakedGI=SampleSH(n);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);input.shadowMask=half4(1,1,1,1);input.vertexLighting=VertexLighting(i.world,n);
  half4 color=UniversalFragmentPBR(input,surface);float day=saturate(dot(radial,GetMainLight().direction)*3+.2),distance=length(_WorldSpaceCameraPos-i.world);float haze=(1-exp(-distance/max(250,_Radius*1.1)))*.20*day;color.rgb=lerp(color.rgb,float3(.12,.21,.29)*(.3+day),haze);return color;
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
