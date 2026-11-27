Shader "Orbit/HelmetPhoto" {
 Properties { _MainTex("Photo",2D)="white"{} _Crop("Crop origin and size",Vector)=(0,0,1,1) _Turns("Quarter turns",Float)=0 }
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" "RenderType"="Opaque"}
 Pass {Cull Off ZWrite On HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float4 _Crop;float _Turns;
 V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;return o;}
 half4 frag(V i):SV_Target{
  float2 oval=(i.uv-.5)*2;clip(1-dot(oval,oval));
  float2 uv=_Crop.xy+i.uv*_Crop.zw;
  if(_Turns>.5&&_Turns<1.5)uv=float2(1-uv.y,uv.x);
  else if(_Turns>1.5&&_Turns<2.5)uv=1-uv;
  else if(_Turns>2.5)uv=float2(uv.y,1-uv.x);
  half4 p=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);
  return half4(lerp(half3(.04,.065,.09),p.rgb,p.a),1);
 }
 ENDHLSL }
 }
}
