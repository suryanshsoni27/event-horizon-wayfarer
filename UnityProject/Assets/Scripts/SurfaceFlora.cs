using System;
using UnityEngine;

// One streamed mesh per nearby patch: no individual grass GameObjects or colliders.
public sealed class SurfaceFlora : MonoBehaviour {
 PlanetExpedition ground;SurfaceArt art;PlanetExpedition.Planet planet;Transform patch;Mesh mesh;Vector3 last;float timer;
 public int Plants{get;private set;} public int Vertices{get{return mesh==null?0:mesh.vertexCount;}}
 public void Initialize(PlanetExpedition expedition,SurfaceArt materials){ground=expedition;art=materials;}
 void Update(){if(ground==null)return;timer-=Time.deltaTime;if(timer>0)return;timer=.7f;Vector3 focus=ground.OnFoot?ground.Focus:Camera.main.transform.position;var p=ground.Nearest(focus);if(p==null||ground.Alt(focus)>90){Clear();return;}if(p!=planet||(focus-last).sqrMagnitude>144)Rebuild(p,focus);}
 void Clear(){if(patch!=null)Destroy(patch.gameObject);if(mesh!=null)Destroy(mesh);mesh=null;patch=null;planet=null;Plants=0;}
 void Rebuild(PlanetExpedition.Planet p,Vector3 focus){Clear();planet=p;last=focus;Vector3 up=(focus-p.body.P).normalized,forward=PlanetExpedition.SurfaceTangent(up,Vector3.forward),right=Vector3.Cross(up,forward);Vector3 anchor=up*((float)p.body.radius+PlanetExpedition.Height(up,p.body.seed,p.body.radius));var g=new SurfaceArt.Geometry();int seed=unchecked((int)p.body.seed);var rng=new System.Random(seed^Mathf.RoundToInt(up.x*400)^Mathf.RoundToInt(up.z*400)*397);bool purple=(seed&1)==0;
  for(int i=0;i<1050;i++){float angle=(float)rng.NextDouble()*Mathf.PI*2,r=Mathf.Sqrt((float)rng.NextDouble())*57;Vector3 local=(anchor+right*Mathf.Cos(angle)*r+forward*Mathf.Sin(angle)*r).normalized;float h=PlanetExpedition.Height(local,p.body.seed,p.body.radius);Vector3 world=p.body.P+local*((float)p.body.radius+h);if(ground.Alt(world)<-.3f)continue;Vector3 pos=world-p.body.P,normal=local;Vector3 t=PlanetExpedition.SurfaceTangent(normal,right);float size=.4f+(float)rng.NextDouble()*.8f;
   if(i<60){Color stem=purple?new Color(.11f,.24f,.26f,0):new Color(.19f,.27f,.15f,0);float height=1.1f+size;g.Horn(pos,pos+normal*height,.055f,stem,t*.2f,6,3);for(int j=0;j<7;j++){float a=j*2.4f;Vector3 radial=t*Mathf.Cos(a)+Vector3.Cross(normal,t)*Mathf.Sin(a);Color c=purple?new Color(.32f,.12f,.45f,1):new Color(.08f,.4f,.26f,1);g.Blade(pos+normal*(height*.25f+j*.13f),Vector3.Cross(radial,normal),radial*.85f+normal*.35f,.24f,size*.85f,c);} }
   else if(i<105){float height=size*.55f;g.Horn(pos,pos+normal*height,.075f,new Color(.12f,.26f,.28f,0),Vector3.zero,6,3);g.Ellipsoid(pos+normal*height,new Vector3(size*.65f,.13f,size*.65f),purple?new Color(.18f,.55f,.61f,1):new Color(.53f,.22f,.5f,1),10,5,Quaternion.FromToRotation(Vector3.up,normal));}
   else {Color c=purple?new Color(.19f,.31f,.27f,1):new Color(.22f,.37f,.17f,1);for(int j=0;j<3;j++){Vector3 a=Quaternion.AngleAxis(j*60,normal)*t;g.Blade(pos,a,normal,.06f,.3f+size*.35f,c);}}Plants++;
  }
  mesh=g.Mesh("Streamed alien undergrowth / ferns / bioluminescent fungi");var go=art.MeshObject("ALIEN FLORA / combined wind mesh",p.root.transform,mesh,art.vegetation);patch=go.transform;
 }
 void OnDestroy(){Clear();}
}
