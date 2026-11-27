using UnityEngine;

public sealed class VoyagerVfx:MonoBehaviour {
 public static VoyagerVfx Current{get;private set;}
 readonly ParticleSystem[] impacts=new ParticleSystem[12];
 readonly ParticleSystem.Particle[] buffer=new ParticleSystem.Particle[256];
 ParticleSystem dust;
 Material material;
 int next;
 public void Initialize(){
  Current=this;material=new Material(Shader.Find("Orbit/VoyagerParticle"));
  for(int i=0;i<impacts.Length;i++)impacts[i]=System("Pooled impact "+i,96);
  dust=System("Surface dust / tires and boots",180);
  var main=dust.main;main.startLifetime=.9f;main.startSize=new ParticleSystem.MinMaxCurve(.15f,.55f);main.startSpeed=new ParticleSystem.MinMaxCurve(.8f,2.2f);main.startColor=new Color(.40f,.32f,.22f,.25f);
 }
 ParticleSystem System(string name,int limit){
  var root=new GameObject(name);root.transform.SetParent(transform,false);var ps=root.AddComponent<ParticleSystem>();
  var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=limit;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=new ParticleSystem.MinMaxCurve(.15f,.6f);main.startSize=new ParticleSystem.MinMaxCurve(.045f,.22f);main.startSpeed=new ParticleSystem.MinMaxCurve(2,11);
  var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.15f;
  var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.65f,.75f,.9f),1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});color.color=gradient;
  var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.sortMode=ParticleSystemSortMode.Distance;ps.Play();return ps;
 }
 public void Impact(Vector3 point,Color color){var ps=impacts[next++%impacts.Length];ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.transform.position=point;var main=ps.main;main.startColor=color;ps.Play();ps.Emit(30);}
 public void Surface(Vector3 point,Vector3 up,bool moving,float intensity){
  if(dust==null)return;dust.transform.position=point;dust.transform.rotation=Quaternion.FromToRotation(Vector3.up,up);
  if(moving)dust.Emit(Mathf.Clamp(Mathf.RoundToInt(intensity*2),1,5));
 }
 public void Shift(Vector3 shift){ShiftSystem(dust,shift);foreach(var ps in impacts)ShiftSystem(ps,shift);}
 void ShiftSystem(ParticleSystem ps,Vector3 shift){if(ps==null)return;int count=ps.GetParticles(buffer);for(int i=0;i<count;i++)buffer[i].position-=shift;ps.SetParticles(buffer,count);ps.transform.position-=shift;}
 void OnDestroy(){if(Current==this)Current=null;if(material!=null)Destroy(material);}
}

public sealed partial class DefenseGame {
 VoyagerVfx voyagerVfx;
 static Mesh coronaMesh;
 float dustClock;
 void CreateStellarCorona(Transform sun){
  if(voyager==null||sun==null||sun.Find("Stellar corona")!=null)return;
  if(coronaMesh==null){coronaMesh=new Mesh{name="Shared solar corona billboard"};coronaMesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};coronaMesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};coronaMesh.triangles=new[]{0,1,2,0,2,3};coronaMesh.bounds=new Bounds(Vector3.zero,Vector3.one*8);}
  var root=new GameObject("Stellar corona");root.transform.SetParent(sun,false);root.AddComponent<MeshFilter>().sharedMesh=coronaMesh;
  var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=new Material(Shader.Find("Orbit/StellarCorona"));renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
 }
 void VoyagerVisualTick(float dt){
  if(voyagerVfx==null){voyagerVfx=gameObject.AddComponent<VoyagerVfx>();voyagerVfx.Initialize();}
  dustClock-=dt;if(dustClock>0||paused)return;dustClock=.06f;
  Vector3 at=GroundActive?expedition.Focus:Pos(player);var p=expedition.Nearest(at);if(p==null)return;
  Vector3 up=(at-p.body.P).normalized;float speed=GroundActive?expedition.Speed:Vel(player).magnitude;
  bool moving=(GroundActive||RoverReady)&&expedition.Alt(at)<2&&speed>.9f;
  voyagerVfx.Surface(at-up*(GroundActive?1.05f:.8f),up,moving,speed*.25f);
 }
}
