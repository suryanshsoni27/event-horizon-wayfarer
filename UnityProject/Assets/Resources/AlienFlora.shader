Shader "Orbit/AlienFlora" {
 SubShader { Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Cull Off Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.5
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 struct A{float4 p:POSITION;float3 n:NORMAL;float4 c:COLOR;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float4 c:COLOR;};
 V vert(A a){V o;float3 p=a.p.xyz;float wind=sin(_Time.y*1.7+p.x*.6+p.z*.3)*.08*a.c.a;p+=float3(wind,0,wind*.4);o.p=TransformObjectToHClip(p);o.world=TransformObjectToWorld(p);o.n=TransformObjectToWorldNormal(a.n);o.c=a.c;return o;}
 half4 frag(V i):SV_Target{Light l=GetMainLight(TransformWorldToShadowCoord(i.world));float diffuse=abs(dot(normalize(i.n),l.direction));float3 c=i.c.rgb*(float3(.08,.12,.16)+l.color*(.15+diffuse*.8*l.shadowAttenuation));float glow=saturate((max(i.c.r,i.c.b)-i.c.g)*3);c+=i.c.rgb*glow*.4;return half4(c,1);}
 ENDHLSL }
 Pass{Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} ZWrite On ColorMask 0 HLSLPROGRAM
 #pragma vertex sv
 #pragma fragment sf
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float4 sv(float4 p:POSITION):SV_POSITION{return TransformObjectToHClip(p.xyz);}half4 sf():SV_Target{return 0;}
 ENDHLSL}
 }
}
