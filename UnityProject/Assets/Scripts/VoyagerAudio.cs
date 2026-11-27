using System.Collections.Generic;
using UnityEngine;

// Original synthesized score and effects. Bounded voices, no downloaded music or runtime services.
public sealed class VoyagerAudio:MonoBehaviour {
 public static VoyagerAudio Current {get;private set;}
 VoyagerSettings settings;
 readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
 readonly List<AudioSource> voices=new List<AudioSource>();
 AudioSource ambience,combat,engine,wind,tires;
 int voice;
 float footClock,battleMix;
 bool active;
 public void Initialize(VoyagerSettings options){
  Current=this;settings=options;
  clips["engine"]=Noise("Plasma engine",1,76,.48f);
  clips["wind"]=Noise("Planetary wind",2,38,.84f);
  clips["tires"]=Noise("Regolith tire contact",1,115,.74f);
  foreach(string cue in new[]{"fire","impact","shield","step","ui","success","chapter","launch","alien"})
   clips[cue]=Effect(cue);
  clips["music"]=Score(false);clips["combat"]=Score(true);
  ambience=Loop(clips["music"]);combat=Loop(clips["combat"]);engine=Loop(clips["engine"]);wind=Loop(clips["wind"]);tires=Loop(clips["tires"]);
  for(int i=0;i<16;i++){var go=new GameObject("Audio / pooled spatial voice "+i);go.transform.SetParent(transform,false);var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=1;source.rolloffMode=AudioRolloffMode.Logarithmic;source.minDistance=6;source.maxDistance=1200;voices.Add(source);}
 }
 AudioSource Loop(AudioClip clip){var source=gameObject.AddComponent<AudioSource>();source.clip=clip;source.loop=true;source.volume=0;source.playOnAwake=false;source.Play();return source;}
 AudioClip Noise(string name,int seconds,float hz,float noise){
  int rate=22050,n=rate*seconds;float[] data=new float[n];var rng=new System.Random(227);float smooth=0;
  for(int i=0;i<n;i++){float t=i/(float)rate;smooth=Mathf.Lerp(smooth,(float)rng.NextDouble()*2-1,.12f);data[i]=(Mathf.Sin(t*hz*Mathf.PI*2)*.12f+smooth*noise)*.55f;}
  var clip=AudioClip.Create(name,n,1,rate,false);clip.SetData(data,0);return clip;
 }
 AudioClip Effect(string cue){
  float duration=cue=="chapter"?2.2f:cue=="launch"?2.4f:cue=="success"?1:cue=="impact"?.55f:cue=="step"?.13f:.22f;
  int rate=22050,n=Mathf.RoundToInt(duration*rate);float[] data=new float[n];var rng=new System.Random(1151);float phase=0,smooth=0;
  for(int i=0;i<n;i++){
   float t=i/(float)rate,u=t/duration,envelope=Mathf.Pow(1-u,cue=="chapter"?1:2)*Mathf.Min(1,t*80);
   float hz=cue=="fire"?Mathf.Lerp(950,95,u):cue=="shield"?Mathf.Lerp(430,140,u):cue=="step"?78:cue=="impact"?Mathf.Lerp(95,28,u):cue=="launch"?Mathf.Lerp(45,90,u):cue=="alien"?Mathf.Lerp(110,220,Mathf.Sin(u*7)):cue=="ui"?660:cue=="success"?new[]{330f,415.3f,493.9f,659.3f}[Mathf.Min(3,(int)(u*4))]:220;
   phase+=Mathf.PI*2*hz/rate;smooth=Mathf.Lerp(smooth,(float)rng.NextDouble()*2-1,.22f);
   float noise=(cue=="impact"||cue=="launch"||cue=="step")?.65f:.08f;
   float melody=cue=="chapter"?(Mathf.Sin(t*220*Mathf.PI*2)+Mathf.Sin(t*329.63f*Mathf.PI*2)*.5f+Mathf.Sin(t*440*Mathf.PI*2)*.3f)*.32f:Mathf.Sin(phase)*.32f;
   data[i]=Mathf.Clamp((melody+smooth*noise)*envelope,-.85f,.85f);
  }
  var clip=AudioClip.Create("Wayfarer / "+cue,n,1,rate,false);clip.SetData(data,0);return clip;
 }
 AudioClip Score(bool fighting){
  int rate=22050,seconds=16,n=rate*seconds;float[] data=new float[n*2];
  float[][] chords={new[]{110f,164.8138f,220f,261.6256f},new[]{87.3071f,130.8128f,174.6141f,220f},new[]{130.8128f,195.9977f,261.6256f,329.6276f},new[]{97.9989f,146.8324f,195.9977f,246.9417f}};
  for(int i=0;i<n;i++){float t=i/(float)rate,local=t%4;int chord=(int)(t/4);float env=Mathf.Sin(local/4*Mathf.PI);float left=0,right=0;
   foreach(float hz in chords[chord]){left+=Mathf.Sin(t*hz*Mathf.PI*2)*.031f;right+=Mathf.Sin(t*(hz+.13f)*Mathf.PI*2)*.031f;}
   if(fighting){float beat=t%.5f,kick=Mathf.Sin(beat*(65-35*beat)*Mathf.PI*2)*Mathf.Exp(-beat*20)*.09f;left+=kick;right+=kick;int note=(int)(t*4)%4;left+=Mathf.Sin(t*chords[chord][note]*2*Mathf.PI*2)*Mathf.Exp(-(t%.25f)*15)*.045f;right+=left*.18f;}
   data[i*2]=left*env;data[i*2+1]=right*env;
  }
  var clip=AudioClip.Create(fighting?"Wayfarer / frontier conflict":"Wayfarer / horizons",n,2,rate,false);clip.SetData(data,0);return clip;
 }
 public void Tick(float dt,bool running,bool boots,bool sprint,float walkSpeed,bool rover,float shipSpeed,float thrust,float atmosphere,bool fighting,Vector3 listener){
  if(settings==null)return;active=running;AudioListener.volume=settings.master;
  battleMix=Mathf.MoveTowards(battleMix,fighting?1:0,dt*.4f);
  ambience.volume=settings.music*(1-battleMix*.8f)*(running?.45f:.22f);
  combat.volume=settings.music*battleMix*(running?.65f:.10f);
  engine.volume=running&&!boots&&!rover?settings.effects*Mathf.Clamp01(thrust)*.26f:0;engine.pitch=.7f+Mathf.Clamp01(thrust)*.7f;
  wind.volume=running?settings.effects*atmosphere*(boots?.12f:.20f)*Mathf.Clamp01(.2f+shipSpeed/40):0;
  tires.volume=running&&rover&&!boots?settings.effects*Mathf.Clamp01(shipSpeed/10)*.23f:0;tires.pitch=.7f+shipSpeed*.04f;
  footClock-=dt;if(running&&boots&&walkSpeed>.8f&&footClock<=0){Cue("step",listener);footClock=sprint?.27f:.43f;}
 }
 public void Silence(){active=false;if(engine!=null)engine.volume=0;if(wind!=null)wind.volume=0;if(tires!=null)tires.volume=0;if(combat!=null)combat.volume=0;if(ambience!=null)ambience.volume=settings.music*.2f;}
 public void Cue(string cue,Vector3 world){
  if(settings==null||voices.Count==0||!clips.TryGetValue(cue,out var clip))return;
  var source=voices[voice++%voices.Count];source.Stop();source.transform.position=world;
  source.spatialBlend=cue=="chapter"||cue=="success"||cue=="ui"?0:1;
  source.volume=settings.effects*(cue=="step"?.32f:cue=="fire"?.42f:.7f);source.pitch=1;source.clip=clip;source.Play();
 }
 public void Shift(Vector3 shift){foreach(var source in voices)source.transform.position-=shift;}
 void OnDestroy(){if(Current==this)Current=null;foreach(var clip in clips.Values)if(clip!=null)Destroy(clip);}
}
