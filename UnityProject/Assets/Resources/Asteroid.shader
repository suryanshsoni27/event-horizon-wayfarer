Shader "Orbit/Asteroid" {
 Properties {_Rock("Rock color",2D)="white"{} _RockNormal("Rock normal",2D)="bump"{}}
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}Pass {HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.5
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_Rock);SAMPLER(sampler_Rock);TEXTURE2D(_RockNormal);SAMPLER(sampler_RockNormal);
 struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 local:TEXCOORD0;float3 n:TEXCOORD1;float3 world:TEXCOORD2;};
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.local=i.p.xyz;o.n=TransformObjectToWorldNormal(i.n);o.world=TransformObjectToWorld(i.p.xyz);return o;}
 half4 frag(V i):SV_Target{float3 n=normalize(i.n),w=pow(abs(n),4);w/=max(.001,w.x+w.y+w.z);float3 p=i.local*2.6;
 float3 tex=SAMPLE_TEXTURE2D(_Rock,sampler_Rock,p.yz).rgb*w.x+SAMPLE_TEXTURE2D(_Rock,sampler_Rock,p.xz).rgb*w.y+SAMPLE_TEXTURE2D(_Rock,sampler_Rock,p.xy).rgb*w.z;
 float3 bump=SAMPLE_TEXTURE2D(_RockNormal,sampler_RockNormal,p.xz).xyz*2-1;n=normalize(n+float3(bump.x,0,bump.y)*.18);Light light=GetMainLight(TransformWorldToShadowCoord(i.world));float diffuse=saturate(dot(n,light.direction));float3 v=normalize(_WorldSpaceCameraPos-i.world);float spec=pow(saturate(dot(n,normalize(v+light.direction))),22)*.08;float cracks=.75+.25*sin(p.x*27+sin(p.y*29))*sin(p.z*31);float3 c=tex*float3(.85,.83,.79)*cracks*(float3(.20,.22,.26)+light.color*diffuse*light.shadowAttenuation)+spec*light.color;return half4(c,1);}
 ENDHLSL}
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 }
}
