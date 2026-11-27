Shader "Orbit/AlienFlora" {
 Properties{_LeafMap("Original leaf albedo",2D)="white"{}}
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float3 Wind(float3 p,float weight){float3 radial=normalize(p),tangent=cross(radial,float3(.7,.2,.4));return p+tangent*sin(_Time.y*1.6+dot(p,float3(.12,.06,.18)))*.055*weight;}
 ENDHLSL
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Cull Off
 Pass {Name "ForwardLit" Tags{"LightMode"="UniversalForward"} HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.5
 #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_LeafMap);SAMPLER(sampler_LeafMap);
 struct A{float4 p:POSITION;float3 n:NORMAL;float4 c:COLOR;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;half3 n:TEXCOORD1;half4 c:COLOR;float2 uv:TEXCOORD2;};
 V vert(A a){V o;float3 p=Wind(a.p.xyz,a.c.a);o.p=TransformObjectToHClip(p);o.world=TransformObjectToWorld(p);o.n=TransformObjectToWorldNormal(a.n);o.c=a.c;o.uv=a.uv;return o;}
 half4 frag(V i,bool front:SV_IsFrontFace):SV_Target{
  float3 n=normalize(i.n)*(front?1:-1),view=SafeNormalize(_WorldSpaceCameraPos-i.world);
  SurfaceData s=(SurfaceData)0;s.albedo=i.c.rgb*lerp(float3(1,1,1),SAMPLE_TEXTURE2D(_LeafMap,sampler_LeafMap,i.uv).rgb*2.8,saturate(i.c.a*8));s.smoothness=.28;s.occlusion=lerp(.68,1,i.c.a);s.normalTS=half3(0,0,1);s.alpha=1;
  InputData d=(InputData)0;d.positionWS=i.world;d.positionCS=i.p;d.normalWS=n;d.viewDirectionWS=view;d.shadowCoord=TransformWorldToShadowCoord(i.world);d.bakedGI=SampleSH(n);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);d.shadowMask=half4(1,1,1,1);d.vertexLighting=VertexLighting(i.world,n);
  half4 color=UniversalFragmentPBR(d,s);Light sun=GetMainLight(d.shadowCoord);float translucency=pow(saturate(dot(-view,sun.direction)),3)*.16*i.c.a*sun.shadowAttenuation;color.rgb+=i.c.rgb*sun.color*translucency;return color;
 }
 ENDHLSL}
 Pass{Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask 0 HLSLPROGRAM
 #pragma vertex dv
 #pragma fragment df
 struct A{float4 p:POSITION;float4 c:COLOR;};float4 dv(A a):SV_POSITION{return TransformObjectToHClip(Wind(a.p.xyz,a.c.a));}half4 df():SV_Target{return 0;}
 ENDHLSL}
 Pass{Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} ZWrite On ColorMask 0 HLSLPROGRAM
 #pragma vertex sv
 #pragma fragment sf
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
 float3 _LightDirection;struct A{float4 p:POSITION;float3 n:NORMAL;float4 c:COLOR;};
 float4 sv(A a):SV_POSITION{return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(TransformObjectToWorld(Wind(a.p.xyz,a.c.a)),TransformObjectToWorldNormal(a.n),_LightDirection)));}half4 sf():SV_Target{return 0;}
 ENDHLSL}
 }
}
