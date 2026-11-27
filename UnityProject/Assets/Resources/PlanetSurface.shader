Shader "Orbit/PlanetSurface" {
 Properties { _Radius("Radius",Float)=1200 _Seed("Seed",Float)=4 _Land("Land",Color)=(.2,.3,.16,1) _Ocean("Ocean",Color)=(.01,.07,.10,1) _GrassTex("Grass surface",2D)="white"{} _RockTex("Rock surface",2D)="white"{} _GrassNormal("Grass detail normals",2D)="bump"{} _RockNormal("Rock detail normals",2D)="bump"{} }
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass {HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float3 local:TEXCOORD2;};float _Radius,_Seed;float4 _Land,_Ocean;TEXTURE2D(_GrassTex);SAMPLER(sampler_GrassTex);TEXTURE2D(_RockTex);SAMPLER(sampler_RockTex);TEXTURE2D(_GrassNormal);SAMPLER(sampler_GrassNormal);TEXTURE2D(_RockNormal);SAMPLER(sampler_RockNormal);
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.world=TransformObjectToWorld(a.p.xyz);o.normal=TransformObjectToWorldNormal(a.n);o.local=a.p.xyz;return o;}
 float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
 float noise(float3 p){float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+1),f.x),f.y),f.z);}
 float3 triColor(TEXTURE2D_PARAM(tex,samp),float3 p,float3 w){return SAMPLE_TEXTURE2D(tex,samp,p.yz).rgb*w.x+SAMPLE_TEXTURE2D(tex,samp,p.xz).rgb*w.y+SAMPLE_TEXTURE2D(tex,samp,p.xy).rgb*w.z;}
 float3 triDetail(TEXTURE2D_PARAM(tex,samp),float3 p,float3 w){float2 x=SAMPLE_TEXTURE2D(tex,samp,p.yz).xy*2-1,y=SAMPLE_TEXTURE2D(tex,samp,p.xz).xy*2-1,z=SAMPLE_TEXTURE2D(tex,samp,p.xy).xy*2-1;return float3(0,x.x,x.y)*w.x+float3(y.x,0,y.y)*w.y+float3(z.x,z.y,0)*w.z;}
 half4 frag(V i):SV_Target{
  float3 radial=normalize(i.local),n=normalize(i.normal),view=normalize(_WorldSpaceCameraPos-i.world);float height=length(i.local)-_Radius,scale=max(1,_Radius/22),h=height/scale;Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
  float broad=noise(i.local*.012+_Seed),grain=noise(i.local*.18),fine=noise(i.local*1.7);float slope=1-saturate(dot(n,radial));
  float water=1-smoothstep(.08,.7,height),snow=smoothstep(65,105,height*1200/max(100,_Radius)+abs(radial.y)*18);
  if(_Radius<100){water=1-smoothstep(-.24,-.03,height);snow=smoothstep(.62,1.16,height+abs(radial.y)*.35);}
  float3 weights=pow(abs(n),4);weights/=max(.0001,weights.x+weights.y+weights.z);float3 grassPhoto=triColor(TEXTURE2D_ARGS(_GrassTex,sampler_GrassTex),i.local,weights),rockPhoto=triColor(TEXTURE2D_ARGS(_RockTex,sampler_RockTex),i.local*.25,weights);float3 sand=float3(.36,.29,.18),stone=rockPhoto*lerp(.92,1.08,broad),grass=grassPhoto*(_Land.rgb*3)*lerp(.8,1.25,broad);float3 land=lerp(sand,grass,smoothstep(1.2,8,height));land=lerp(land,stone,saturate(slope*4.5+smoothstep(20,80,height)*.6));land=lerp(land,float3(.72,.78,.8),snow*(1-saturate(slope*3)));land*=.78+.22*grain+.08*fine;
  float rockAmount=saturate(slope*4.5+smoothstep(20,80,height)*.6);float3 detailNormal=lerp(triDetail(TEXTURE2D_ARGS(_GrassNormal,sampler_GrassNormal),i.local,weights),triDetail(TEXTURE2D_ARGS(_RockNormal,sampler_RockNormal),i.local*.25,weights),rockAmount);detailNormal-=n*dot(n,detailNormal);n=normalize(n+detailNormal*.25*(1-water)*(1-snow*.7));
  float3 wave=float3(sin(i.local.x*.10+_Time.y)*cos(i.local.z*.09),0,cos(i.local.z*.12-_Time.y*.7)*sin(i.local.x*.07));n=normalize(n+wave*.055*water);
  float diffuse=saturate(dot(n,sun.direction))*sun.shadowAttenuation;float night=saturate(dot(radial,sun.direction)*3+.2);float3 color=lerp(land,_Ocean.rgb*(.85+grain*.15),water);color*=float3(.025,.035,.05)+sun.color*(.10+diffuse*1.05);
  float spec=pow(saturate(dot(n,normalize(view+sun.direction))),water>.5?180:24);color+=sun.color*spec*sun.shadowAttenuation*(water*.95+.015);
  float fresnel=pow(1-saturate(dot(n,view)),5);color+=water*fresnel*float3(.07,.13,.19)*night;
  uint lights=GetAdditionalLightsCount();for(uint index=0;index<lights;index++){Light lamp=GetAdditionalLight(index,i.world);color+=lerp(land,_Ocean.rgb,water)*lamp.color*saturate(dot(n,lamp.direction))*lamp.distanceAttenuation*lamp.shadowAttenuation;}
  float distance=length(_WorldSpaceCameraPos-i.world);float haze=(1-exp(-distance/max(180,_Radius*.9)))*.3*night;color=lerp(color,float3(.16,.25,.32)*(.3+night),haze);
  return half4(color,1);
 }
 ENDHLSL}
 Pass {Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask 0 HLSLPROGRAM
 #pragma vertex depthVert
 #pragma fragment depthFrag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float4 depthVert(float4 p:POSITION):SV_POSITION{return TransformObjectToHClip(p.xyz);} half4 depthFrag():SV_Target{return 0;}
 ENDHLSL}
 Pass {Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ColorMask 0 HLSLPROGRAM
 #pragma vertex shadowVert
 #pragma fragment shadowFrag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
 float3 _LightDirection;struct ShadowA{float4 p:POSITION;float3 n:NORMAL;};float4 shadowVert(ShadowA a):SV_POSITION{return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(TransformObjectToWorld(a.p.xyz),TransformObjectToWorldNormal(a.n),_LightDirection)));} half4 shadowFrag():SV_Target{return 0;}
 ENDHLSL}
 }
}
