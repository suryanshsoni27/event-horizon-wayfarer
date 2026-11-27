Shader "Orbit/Schwarzschild" {
 Properties { _Steps("Ray budget",Float)=220 _StepScale("Affine step scale",Float)=0.045 _Rs("Schwarzschild radius",Float)=1 _RayEnabled("Trace nearby hole",Float)=1 _HoleCenter("Hole center",Vector)=(0,0,0,0) _CaptureMatterDepth("Illustrative capture foreground",Float)=0 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Background-100" "RenderType"="Opaque"}
 Pass { ZWrite On ZTest Always Cull Off HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.5
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 positionOS:POSITION;};struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;};
 struct O {half4 color:SV_Target;float depth:SV_Depth;};
 float _Steps,_StepScale,_Rs,_RayEnabled,_CaptureMatterDepth;float4 _HoleCenter;
 V vert(A i){V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float h3(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
 float n3(float3 p){float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(h3(i),h3(i+float3(1,0,0)),f.x),lerp(h3(i+float3(0,1,0)),h3(i+float3(1,1,0)),f.x),f.y),lerp(lerp(h3(i+float3(0,0,1)),h3(i+float3(1,0,1)),f.x),lerp(h3(i+float3(0,1,1)),h3(i+1),f.x),f.y),f.z);}
 float fbm(float3 p){return n3(p)*.57+n3(p*2.03+7)*.28+n3(p*4.11+19)*.15;}
 float3 galaxy(float3 d,float3 center,float scale){float forward=dot(d,center);if(forward<.9)return 0;float3 right=normalize(cross(center,float3(0,1,0))),up=cross(right,center);float2 p=float2(dot(d,right),dot(d,up)*2.6)/(forward*scale);float r=length(p),angle=atan2(p.y,p.x);float core=exp(-r*19),arms=pow(.5+.5*sin(angle*3-log(max(.02,r))*5),5)*exp(-r*3)*smoothstep(.035,.18,r);return float3(.45,.53,.68)*arms*.25+float3(.8,.64,.4)*core*.65;}
 float3 sky(float3 d){d=normalize(d);float2 uv=float2(atan2(d.z,d.x)/6.2831853+.5,asin(clamp(d.y,-1,1))/3.14159265+.5);float clouds=fbm(d*28+13),lane=dot(d,normalize(float3(.12,1,.23)))+(fbm(d*5)-.5)*.12,band=exp(-pow(lane/.14,2));float dust=smoothstep(.31,.69,clouds);float cut=1-smoothstep(.48,.67,fbm(d*48+91));float3 c=float3(.003,.005,.012)+band*lerp(float3(.007,.012,.024),float3(.13,.11,.16),dust)*(.3+.7*cut);c+=pow(band,3)*dust*cut*float3(.05,.033,.025);
  [unroll]for(int l=0;l<3;l++){float2 grid=uv*float2(680,340)*(1+l*.61);float2 cell=floor(grid),f=frac(grid);float h=hash(cell+17*l);float2 center=float2(hash(cell+9),hash(cell+31));float r=length(f-center);float star=exp(-r*r/(l==0?.0018:.0006))*step(.986-band*.015,h);c+=star*lerp(float3(.5,.7,1),float3(1,.75,.4),hash(cell+72))*(.5+hash(cell+62)*2);}
  c+=galaxy(d,normalize(float3(-.7,.4,.6)),.11);c+=galaxy(d,normalize(float3(.75,.21,-.6)),.055);return c;}
 // Spatial Schwarzschild null geodesic in affine parameter:
 // r'' vector = -(3/2) Rs L² r / |r|^5, L = r cross r'.
 // Equivalent to u''(phi)+u = (3/2) Rs u². No Newtonian light-force approximation.
 float3 acceleration(float3 p,float L2){float r2=dot(p,p);return -1.5*_Rs*L2*p/(r2*r2*sqrt(r2));}
 float3 disk(float3 p,float3 direction){
  float r=length(p.xz),inner=3*_Rs,outer=8*_Rs;
  float scaledRadius=r/_Rs;float phi=atan2(p.z,p.x),flow=phi-_Time.y*.7*pow(inner/r,1.5);
  float turbulence=.52+.16*sin(flow*7+scaledRadius*5+sin(flow*3)*2)+.13*sin(flow*19-scaledRadius*11+sin(scaledRadius*4))+.08*sin(flow*43+scaledRadius*23);
  float temp=pow(inner/r,.75)*pow(max(.001,1-sqrt(inner/r)),.25);
  float emissivity=smoothstep(inner,inner+.45*_Rs,r)*(1-smoothstep(outer-1.5*_Rs,outer,r))*max(.12,turbulence)*(.4+1.9*pow(inner/r,1.9));
  float3 tangent=float3(-p.z,0,p.x)/r;
  float orbitalSpeed=sqrt(_Rs/(2*(r-_Rs)));
  float shift=sqrt(1-_Rs/r)*sqrt(1-orbitalSpeed*orbitalSpeed)/(1+orbitalSpeed*dot(tangent,normalize(direction)));
  float3 emission=lerp(float3(1,.12,.018),float3(1,.78,.42),saturate(temp*2.5));
  return emission*emissivity*pow(clamp(shift,.35,2),3)*2.1;
 }
 O frag(V i){
  O o;float3 start=_WorldSpaceCameraPos-_HoleCenter.xyz,p=start,d=normalize(i.world-_WorldSpaceCameraPos);float r0=max(length(p),_Rs*1.01);float3 radial=p/r0;
  if(_RayEnabled<.5){o.color=half4(sky(d),1);
 #if UNITY_REVERSED_Z
 o.depth=0;
 #else
 o.depth=1;
 #endif
 return o;}
 float radialV=dot(d,radial);
  // Static-observer tetrad at finite camera radius, with conserved energy E=1.
  float3 v=radialV*radial+(d-radialV*radial)/sqrt(1-_Rs/r0);
  float3 L=cross(p,v);float L2=dot(L,L);float3 color=0;float3 hit=0;bool surface=false,finished=false,capturedRay=false;
  [loop]for(int j=0;j<320;j++){
   if(j>=(int)_Steps)break;
   float r=length(p);if(r<_Rs*1.002){capturedRay=true;finished=true;surface=true;hit=normalize(start)*2.3*_Rs;break;}
   if(r>r0+12*_Rs&&dot(p,v)>0){color=sky(v);finished=true;break;}
   float h=clamp(r*_StepScale,.025*_Rs,2.4*_Rs);float3 old=p;
   float3 a=acceleration(p,L2);p+=v*h+.5*a*h*h;v+=.5*(a+acceleration(p,L2))*h;
   if(old.y*p.y<0){float t=old.y/(old.y-p.y);float3 q=lerp(old,p,t);float rr=length(q.xz);
    if(rr>=3*_Rs&&rr<=8*_Rs){color=disk(q,v);hit=q;surface=true;finished=true;break;}
   }
  }
  // Finite ray budget is a quality limitation; unresolved near-critical rays are dark.
  if(!finished){capturedRay=true;surface=true;hit=normalize(start)*2.3*_Rs;}
  o.color=half4(color,1);
  #if UNITY_REVERSED_Z
  o.depth=0;
  #else
  o.depth=1;
  #endif
  if(_CaptureMatterDepth>.5&&capturedRay){float b=dot(start,d),disc=b*b-dot(start,start)+_Rs*_Rs;if(disc>0){float nearHit=-b-sqrt(disc);surface=nearHit>0;hit=start+d*nearHit;}else surface=false;}
  if(surface){float4 clip=TransformWorldToHClip(hit+_HoleCenter.xyz);o.depth=clip.z/clip.w;}
  return o;
 }
 ENDHLSL }
 }
}
