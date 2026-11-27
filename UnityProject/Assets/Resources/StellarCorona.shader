Shader "Orbit/StellarCorona" {
 Properties {_Radius("Corona radius",Float)=5400 _Color("Solar spectrum",Color)=(1,.35,.04,.5)}
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"} Pass {
 Blend SrcAlpha One ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
 float _Radius;float4 _Color;
 V vert(A i){V o;float3 center=TransformObjectToWorld(float3(0,0,0));float3 right=UNITY_MATRIX_I_V._m00_m10_m20;float3 up=UNITY_MATRIX_I_V._m01_m11_m21;float3 world=center+(right*i.p.x+up*i.p.y)*_Radius*2;o.p=TransformWorldToHClip(world);o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target{float2 p=i.uv*2-1;float r=length(p);float angle=atan2(p.y,p.x);float rays=.7+.17*sin(angle*23+_Time.y*.07)+.10*sin(angle*47-_Time.y*.11);float halo=exp(-max(0,r-.32)*8)*smoothstep(.27,.34,r)*(1-smoothstep(.85,1,r));return half4(_Color.rgb*2,halo*rays*_Color.a);}
 ENDHLSL } }
}
