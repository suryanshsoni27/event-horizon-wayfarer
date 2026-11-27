using System.Collections.Generic;
using UnityEngine;

// Six folding wheel arms, steering knuckles, axle rotation and terrain-following travel.
public sealed class RoverView : MonoBehaviour {
 public readonly Transform[] mounts=new Transform[6],steering=new Transform[6],tires=new Transform[6];
 public readonly Vector3[] anchors=new Vector3[6];
 Transform deck,wheel;readonly List<Material> owned=new List<Material>();float spin;
 Material M(Color c,float metallic=.2f){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",.32f);owned.Add(m);return m;}
 Transform Part(Transform parent,string name,PrimitiveType type,Vector3 p,Vector3 size,Material m){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;Destroy(g.GetComponent<Collider>());return g.transform;}
 Transform Joint(string name,Transform parent,Vector3 p){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
 public void Build(){Material rubber=M(new Color(.022f,.029f,.034f)),metal=M(new Color(.4f,.46f,.47f),.75f),ceramic=M(new Color(.72f,.73f,.65f)),orange=M(new Color(.84f,.26f,.04f)),seat=M(new Color(.06f,.09f,.10f));
  deck=Joint("Deployable crew deck / four riding seats",transform,Vector3.zero);
  Part(deck,"Pressure deck",PrimitiveType.Cube,new Vector3(0,.42f,-.12f),new Vector3(2.05f,.14f,2.9f),metal);
  for(int row=0;row<2;row++)for(int side=-1;side<=1;side+=2){Vector3 p=new Vector3(side*.53f,.74f,row==0?.57f:-.82f);Part(deck,"Crew seat cushion",PrimitiveType.Cube,p,new Vector3(.5f,.13f,.52f),seat);Part(deck,"Seat back",PrimitiveType.Cube,p+new Vector3(0,.25f,-.25f),new Vector3(.51f,.52f,.12f),seat);Part(deck,"Seat safety frame",PrimitiveType.Cube,p+new Vector3(0,.05f,-.32f),new Vector3(.55f,.70f,.08f),orange);}
  for(int side=-1;side<=1;side+=2){Part(deck,"Roof frame",PrimitiveType.Cylinder,new Vector3(side*.98f,1.28f,-1.32f),new Vector3(.065f,.82f,.065f),metal);Part(deck,"Forward roll bar",PrimitiveType.Cylinder,new Vector3(side*.98f,1.28f,1.13f),new Vector3(.065f,.82f,.065f),metal);var rail=Part(deck,"Overhead roll rail",PrimitiveType.Cylinder,new Vector3(side*.98f,2.06f,-.1f),new Vector3(.065f,1.26f,.065f),ceramic);rail.localRotation=Quaternion.Euler(90,0,0);Part(deck,"Side step",PrimitiveType.Cube,new Vector3(side*1.26f,.08f,-.25f),new Vector3(.45f,.09f,2.2f),metal);}
  Part(deck,"Instrument binnacle",PrimitiveType.Cube,new Vector3(0,.99f,1.03f),new Vector3(1.5f,.14f,.15f),metal);
  wheel=Joint("Driver steering wheel",deck,new Vector3(-.53f,1.21f,1.02f));var rim=wheel.gameObject.AddComponent<LineRenderer>();rim.useWorldSpace=false;rim.loop=true;rim.positionCount=32;rim.widthMultiplier=.035f;rim.sharedMaterial=seat;for(int i=0;i<32;i++){float a=i*Mathf.PI/16;rim.SetPosition(i,new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.25f);}Part(wheel,"Steering spoke",PrimitiveType.Cube,Vector3.zero,new Vector3(.45f,.035f,.04f),metal);
  for(int i=0;i<6;i++){int side=i%2==0?-1:1;float z=1.35f-(i/2)*1.35f;anchors[i]=new Vector3(side*1.80f,-.43f,z);mounts[i]=Joint("Folding wheel arm "+i,transform,anchors[i]);Part(mounts[i],"Suspension piston",PrimitiveType.Cube,new Vector3(-side*.40f,.24f,0),new Vector3(.82f,.12f,.12f),metal);Part(mounts[i],"Suspension travel collar",PrimitiveType.Cylinder,Vector3.up*.1f,new Vector3(.12f,.22f,.12f),orange);
   steering[i]=Joint("Steering knuckle",mounts[i],Vector3.zero);tires[i]=Joint("Rotating tire / visible tread",steering[i],Vector3.zero);var tire=Part(tires[i],"All terrain tire",PrimitiveType.Cylinder,Vector3.zero,new Vector3(1.04f,.19f,1.04f),rubber);tire.localRotation=Quaternion.Euler(0,0,90);var hub=Part(tires[i],"Wheel hub",PrimitiveType.Cylinder,new Vector3(side*.20f,0,0),new Vector3(.54f,.025f,.54f),metal);hub.localRotation=Quaternion.Euler(0,0,90);
   for(int t=0;t<20;t++){float a=t*Mathf.PI/10;var tread=Part(tires[i],"Raised chevron tread",PrimitiveType.Cube,new Vector3(0,Mathf.Cos(a)*.51f,Mathf.Sin(a)*.51f),new Vector3(.42f,.055f,.13f),rubber);tread.localRotation=Quaternion.Euler(a*Mathf.Rad2Deg,0,t%2==0?12:-12);}
  }
  gameObject.SetActive(false);
 }
 public void Pose(float fraction,float steer,float speed,float dt,PlanetExpedition terrain,bool active){gameObject.SetActive(fraction>.001f);if(fraction<=.001f)return;float t=Mathf.SmoothStep(0,1,fraction);deck.localScale=new Vector3(t,Mathf.Max(.05f,t),t);if(active)spin+=speed*dt/.52f*Mathf.Rad2Deg;
  wheel.localRotation=Quaternion.Euler(0,0,-steer*65);for(int i=0;i<6;i++){Vector3 p=Vector3.Lerp(new Vector3(anchors[i].x*.36f,-.05f,anchors[i].z*.58f),anchors[i],t);Vector3 world=transform.TransformPoint(p);float travel=Mathf.Clamp(.52f-terrain.Alt(world),-.20f,.20f)*t;p.y+=travel;mounts[i].localPosition=p;mounts[i].localScale=Vector3.one*Mathf.Lerp(.26f,1,t);steering[i].localRotation=Quaternion.Euler(0,steer*(i<2?32:i>=4?-12:0),0);tires[i].localRotation=Quaternion.Euler(spin,0,0);}
 }
 public void ResetView(){spin=0;gameObject.SetActive(false);}
 void OnDestroy(){foreach(var m in owned)if(m!=null)Destroy(m);}
}
