using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// An original HDR reflection environment and explicit camera settings, independent of scene reloads.
public sealed class ProductionVisuals : MonoBehaviour {
 public static ProductionVisuals Current{get;private set;}Cubemap environment;Bloom bloom;Vignette vignette;
 public bool ReflectionsReady{get{return environment!=null&&RenderSettings.customReflectionTexture==environment;}}
 public void Initialize(){
  Current=this;environment=new Cubemap(64,TextureFormat.RGBAHalf,true);environment.name="Wayfarer / original orbital reflection environment";environment.wrapMode=TextureWrapMode.Clamp;environment.filterMode=FilterMode.Trilinear;
  for(int face=0;face<6;face++){
   var colors=new Color[64*64];for(int y=0;y<64;y++)for(int x=0;x<64;x++){
    float u=(x+.5f)/32-1,v=(y+.5f)/32-1;Vector3 d=CubeDirection(face,u,v);
    Color sky=Color.Lerp(new Color(.025f,.035f,.06f),new Color(.22f,.31f,.42f),Mathf.SmoothStep(0,1,d.y*.5f+.5f));
    float key=Mathf.Pow(Mathf.Max(0,Vector3.Dot(d,new Vector3(-.35f,.65f,-.68f).normalized)),48);
    float rim=Mathf.Pow(Mathf.Max(0,Vector3.Dot(d,new Vector3(.7f,.25f,.65f).normalized)),24);
    float ribbon=Mathf.Exp(-Mathf.Pow((d.y-d.x*.25f-.15f)*9,2))*.18f;
    colors[y*64+x]=sky+new Color(1,.84f,.62f)*(key*3.5f)+new Color(.33f,.65f,1)*(rim*1.8f)+new Color(.32f,.4f,.62f)*ribbon;
   }environment.SetPixels(colors,(CubemapFace)face);
  }environment.Apply(true,false);RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=environment;RenderSettings.reflectionIntensity=1;
  RenderSettings.ambientMode=AmbientMode.Custom;var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(new Color(.15f,.19f,.25f));probe.AddDirectionalLight(Vector3.up,new Color(.46f,.58f,.74f),.35f);probe.AddDirectionalLight(Vector3.down,new Color(.18f,.12f,.08f),.15f);RenderSettings.ambientProbe=probe;
  foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional)light.cullingMask&=~(1<<28);
  foreach(var volume in FindObjectsByType<Volume>(FindObjectsSortMode.None))if(volume.gameObject.name=="Cinematic color / local procedural art"&&volume.isGlobal&&volume.profile!=null){volume.profile.TryGet(out bloom);volume.profile.TryGet(out vignette);if(bloom!=null)break;}
 }
 static Vector3 CubeDirection(int face,float u,float v){switch(face){case 0:return new Vector3(1,-v,-u).normalized;case 1:return new Vector3(-1,-v,u).normalized;case 2:return new Vector3(u,1,v).normalized;case 3:return new Vector3(u,-1,-v).normalized;case 4:return new Vector3(u,-v,1).normalized;default:return new Vector3(-u,-v,-1).normalized;}}
 public void Apply(Camera camera,VoyagerSettings settings){
  if(settings==null)return;var data=camera.GetUniversalAdditionalCameraData();data.antialiasing=settings.antialiasing==2?AntialiasingMode.SubpixelMorphologicalAntiAliasing:settings.antialiasing==1?AntialiasingMode.FastApproximateAntialiasing:AntialiasingMode.None;data.antialiasingQuality=AntialiasingQuality.High;data.requiresDepthTexture=true;
  if(bloom!=null)bloom.intensity.Override(settings.bloom);if(vignette!=null)vignette.intensity.Override(settings.motion?.08f:0);
  var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;if(pipeline!=null)pipeline.msaaSampleCount=1;
  var key=GameObject.Find("Warm key");if(key!=null){var light=key.GetComponent<Light>();light.shadows=settings.shadowQuality==0?LightShadows.None:settings.shadowQuality==1?LightShadows.Hard:LightShadows.Soft;}
 }
 void OnDestroy(){if(RenderSettings.customReflectionTexture==environment)RenderSettings.customReflectionTexture=null;if(environment!=null)Destroy(environment);if(Current==this)Current=null;}
}
