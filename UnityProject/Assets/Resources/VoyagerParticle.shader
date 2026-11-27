Shader "Orbit/VoyagerParticle" {
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"} Pass {
 Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 p:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
 struct V {float4 p:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.color=i.color;o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target{float r=length(i.uv*2-1);float alpha=pow(saturate(1-r*r),2)*i.color.a;return half4(i.color.rgb,alpha);}
 ENDHLSL } }
}
