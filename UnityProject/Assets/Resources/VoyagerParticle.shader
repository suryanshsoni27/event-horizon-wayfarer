Shader "Orbit/VoyagerParticle" {
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"} Pass {
 Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 struct A{float4 p:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
 struct V{float4 p:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float eye:TEXCOORD1;};
 V vert(A i){V o;float3 world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(world);o.eye=-TransformWorldToView(world).z;o.color=i.color;o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target{float r=length(i.uv*2-1);float scene=LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(i.p)),_ZBufferParams);float fade=saturate((scene-i.eye)/.65);float alpha=pow(saturate(1-r*r),2)*i.color.a*fade*saturate(i.eye/.4);return half4(i.color.rgb,alpha);}
 ENDHLSL}}}
