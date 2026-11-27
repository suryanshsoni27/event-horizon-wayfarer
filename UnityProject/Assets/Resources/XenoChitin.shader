Shader "Orbit/XenoChitin" {
 Properties { _AlbedoMap("Original scute albedo",2D)="white"{} _Hurt("Damage flash",Float)=0 _Dissolve("Corpse dissolve",Float)=0 }
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float _Hurt,_Dissolve;
 CBUFFER_END
 void CorpseClip(float3 local) {float d=frac(sin(dot(floor(local*43),float3(12.9898,78.233,37.719)))*43758.5453);clip(d-_Dissolve);}
 ENDHLSL
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass {Name "ForwardLit" Tags{"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.5
 #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
 #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_AlbedoMap);SAMPLER(sampler_AlbedoMap);
 struct A {float4 p:POSITION;float3 n:NORMAL;float4 c:COLOR;float2 uv:TEXCOORD0;};
 struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;half3 n:TEXCOORD1;float3 local:TEXCOORD2;half4 c:COLOR;float2 uv:TEXCOORD3;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.world=TransformObjectToWorld(a.p.xyz);o.n=TransformObjectToWorldNormal(a.n);o.local=a.p.xyz;o.c=a.c;o.uv=a.uv;return o;}
 half4 frag(V i):SV_Target {
  CorpseClip(i.local);float3 n=normalize(i.n),v=SafeNormalize(_WorldSpaceCameraPos-i.world);
  float grooves=.5+.5*sin(i.local.z*38+sin(i.local.x*17)*1.3);
  float pores=sin(i.local.x*113)*sin(i.local.y*97)*sin(i.local.z*103);
  float fade=1-smoothstep(12,45,distance(_WorldSpaceCameraPos,i.world));
  float3 grain=float3(cos(i.local.x*113),sin(i.local.y*97),cos(i.local.z*103));grain-=n*dot(n,grain);n=normalize(n+grain*.026*fade);
  float3 tex=SAMPLE_TEXTURE2D(_AlbedoMap,sampler_AlbedoMap,i.uv).rgb;
  float height=dot(tex,float3(.3,.59,.11));
  float3 dpdx=ddx(i.world),dpdy=ddy(i.world),r1=cross(dpdy,n),r2=cross(n,dpdx);
  float det=dot(dpdx,r1);float3 gradient=(r1*ddx(height)+r2*ddy(height))*sign(det)/max(abs(det),.0001);
  n=normalize(n-clamp(gradient,-2,2)*.045*fade);
  float organ=saturate(i.c.a),crease=smoothstep(.16,.36,grooves);
  SurfaceData surface=(SurfaceData)0;
  surface.albedo=i.c.rgb*lerp(float3(1,1,1),saturate(tex*3.1),.82)*( .94+.025*pores);surface.albedo=lerp(surface.albedo,float3(.48,.095,.045),saturate(_Hurt)*.25);
  surface.metallic=.025;surface.smoothness=lerp(.27+height*.20,.42,organ);surface.occlusion=.82+.18*saturate(height*3);
  surface.normalTS=half3(0,0,1);surface.alpha=1;surface.emission=i.c.rgb*organ*(.55+.08*sin(_Time.y*1.8+i.local.z*2));
  InputData input=(InputData)0;input.positionWS=i.world;input.positionCS=i.p;input.normalWS=n;input.viewDirectionWS=v;
  input.shadowCoord=TransformWorldToShadowCoord(i.world);input.bakedGI=SampleSH(n);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);input.shadowMask=half4(1,1,1,1);input.vertexLighting=VertexLighting(i.world,n);
  half4 color=UniversalFragmentPBR(input,surface);
  Light sun=GetMainLight(input.shadowCoord);float scatter=pow(saturate(dot(-v,sun.direction)),4)*saturate(1-dot(n,v));
  color.rgb+=i.c.rgb*sun.color*scatter*.055*sun.shadowAttenuation;
  return color;
 }
 ENDHLSL }
 Pass {Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask 0 HLSLPROGRAM
 #pragma vertex dv
 #pragma fragment df
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 local:TEXCOORD0;};
 V dv(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.local=a.p.xyz;return o;}half4 df(V i):SV_Target{CorpseClip(i.local);return 0;}
 ENDHLSL}
 Pass {Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} ZWrite On ColorMask 0 HLSLPROGRAM
 #pragma vertex sv
 #pragma fragment sf
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
 float3 _LightDirection;struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 local:TEXCOORD0;};
 V sv(A a){V o;o.p=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(TransformObjectToWorld(a.p.xyz),TransformObjectToWorldNormal(a.n),_LightDirection)));o.local=a.p.xyz;return o;}
 half4 sf(V i):SV_Target{CorpseClip(i.local);return 0;}
 ENDHLSL}
 }
}
