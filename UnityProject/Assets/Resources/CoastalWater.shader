Shader "Orbit/CoastalWater" {
 Properties { _Shallow("Shallow water",Color)=(.045,.21,.18,1) _Deep("Basin water",Color)=(.012,.060,.082,1) }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-20"}
 Pass {Name "CoastalWater" Tags{"LightMode"="UniversalForward"} ZWrite On Cull Back
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _Shallow,_Deep;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 local:TEXCOORD1;float3 normal:TEXCOORD2;};
 V vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.local=a.p.xyz;o.normal=TransformObjectToWorldNormal(a.n);return o;}
 half4 frag(V i):SV_Target {
   float3 radial=normalize(i.normal),view=SafeNormalize(_WorldSpaceCameraPos-i.world);float3 p=i.local;
   float t=_Time.y;
   float3 ripple=float3(sin(dot(p,float3(.72,.19,.43))+t*1.35)+.4*sin(dot(p,float3(1.87,.7,-1.11))-t*1.8),cos(dot(p,float3(.37,.81,-.21))+t),cos(dot(p,float3(-.23,.29,.69))-t*.91));
   ripple-=radial*dot(radial,ripple);float3 n=normalize(radial+ripple*.025);
   float2 uv=GetNormalizedScreenSpaceUV(i.p);float raw=SampleSceneDepth(uv);float bottom=LinearEyeDepth(raw,_ZBufferParams),top=-TransformWorldToView(i.world).z;
   float depth=max(0,(bottom-top)*max(.08,abs(dot(radial,view))));
   float foamNoise=sin(dot(p,float3(1.7,.6,1.13))+t*1.1)*sin(dot(p,float3(-.91,1.43,.72))-t*.7);
   float foam=(1-smoothstep(.05,.63,depth))*(.28+.12*foamNoise)*(.65+.35*sin(depth*17-t*2.4));
   float fresnel=pow(1-saturate(dot(n,view)),5);float3 tint=lerp(_Shallow.rgb,_Deep.rgb,saturate(depth/1.3));
   SurfaceData surface=(SurfaceData)0;surface.albedo=lerp(tint,float3(.55,.64,.59),foam);surface.metallic=0;surface.smoothness=lerp(.88,.36,foam);surface.normalTS=half3(0,0,1);surface.occlusion=1;surface.alpha=1;
   InputData input=(InputData)0;input.positionWS=i.world;input.positionCS=i.p;input.normalWS=n;input.viewDirectionWS=view;input.shadowCoord=TransformWorldToShadowCoord(i.world);input.bakedGI=SampleSH(n);input.normalizedScreenSpaceUV=uv;input.shadowMask=half4(1,1,1,1);
   half4 lit=UniversalFragmentPBR(input,surface);float sky=saturate(dot(reflect(-view,n),float3(0,1,0))*.5+.5);lit.rgb+=lerp(float3(.035,.065,.08),float3(.12,.20,.25),sky)*fresnel*.7;return lit;
 }
 ENDHLSL
 }
 }
}
