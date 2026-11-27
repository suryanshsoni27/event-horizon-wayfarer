using System.Collections.Generic;
using UnityEngine;

// Original two-legged expedition opponents. Meshes and textured suit materials are shared per role.
public sealed partial class SurfaceArt {
    readonly Dictionary<int,Mesh> humanMeshes=new Dictionary<int,Mesh>();
    readonly Dictionary<int,Material> humanMaterials=new Dictionary<int,Material>();
    Material HumanMaterial(SurfaceEcology.Species s,int part) {
        int key=s.Id*10+part;if(humanMaterials.TryGetValue(key,out var m))return m;
        Color c=part==0?Color.Lerp(s.shell,Color.black,.42f):part==1?s.plate:part==2?s.shell:new Color(.023f,.034f,.039f);
        m=Own(ProductionMaterials.Create(c,part==1?.56f:part==3?.8f:.03f,part==3?.85f:part==1?.42f:.27f,0,0,part==0));humanMaterials[key]=m;return m;
    }
    Mesh HumanMesh(SurfaceEcology.Species s,int part) {
        int key=s.Id*10+part;if(humanMeshes.TryGetValue(key,out var mesh))return mesh;
        var g=new Geometry();Color white=Color.white;
        if(part==0) {
            g.Ellipsoid(new Vector3(0,.18f,0),new Vector3(.255f,.34f,.15f),white,28,24);
            g.Ellipsoid(new Vector3(0,-.13f,0),new Vector3(.25f,.15f,.155f),white,24,16);
            g.Tube(new Vector3(0,.43f,0),new Vector3(0,.69f,0),.09f,.085f,Vector3.zero,white,20,10);
        } else if(part==1) {
            // Two curved armor shells, beveled equipment and a rigid backpack frame.
            g.Ellipsoid(new Vector3(0,.30f,.055f),new Vector3(.30f,.225f,.165f),white,32,24);
            g.Ellipsoid(new Vector3(0,.045f,.075f),new Vector3(.225f,.16f,.12f),white,28,16);
            g.Ellipsoid(new Vector3(0,.26f,-.19f),new Vector3(.235f,.31f,.105f),white,24,18);
            for(int side=-1;side<=1;side+=2) {
                g.Tube(new Vector3(side*.19f,.03f,.12f),new Vector3(side*.21f,.49f,.08f),.037f,.035f,Vector3.forward*.025f,white,12,8);
                g.Ellipsoid(new Vector3(side*.30f,.47f,0),new Vector3(.135f,.09f,.19f),white,24,14);
            }
        } else if(part==2) {
            // Broad forehead, cheek planes, human nose and jaw; subtle alien brow ridges and ears.
            g.Ellipsoid(Vector3.zero,new Vector3(.133f,.197f,.129f),white,40,32);
            g.Ellipsoid(new Vector3(0,-.115f,.035f),new Vector3(.088f,.066f,.085f),white,28,22);
            g.Ellipsoid(new Vector3(0,-.012f,.146f),new Vector3(.025f,.057f,.028f),white,24,20);
            g.Ellipsoid(new Vector3(0,-.040f,.167f),new Vector3(.022f,.016f,.020f),white,24,16);
            for(int side=-1;side<=1;side+=2) {
                g.Ellipsoid(new Vector3(side*.11f,-.036f,.09f),new Vector3(.026f,.035f,.028f),white,20,16);
                g.Tube(new Vector3(side*.022f,.055f,.13f),new Vector3(side*.108f,.063f,.108f),.018f,.013f,Vector3.up*.009f,white,16,10);
                g.Ellipsoid(new Vector3(side*.153f,.013f,-.004f),new Vector3(.020f,.057f,.028f),white,24,18,Quaternion.Euler(0,0,-side*20));
            }
        } else if(part==3) {
            // Pressure helmet with rear seals; visor is separate so its silhouette remains legible.
            g.Ellipsoid(Vector3.zero,new Vector3(.205f,.235f,.19f),white,36,28);
            g.Tube(new Vector3(0,-.24f,0),new Vector3(0,-.17f,0),.18f,.17f,Vector3.zero,white,28,6);
        } else {
            float length=part==4?.48f:part==5?.50f:part==6?.34f:.35f;
            float width=part<6?.105f:.075f;
            g.Tube(Vector3.zero,Vector3.down*length,width,width*.68f,Vector3.forward*.018f,white,24,18);
            g.Ellipsoid(new Vector3(0,-length*.45f,.025f),new Vector3(width*1.12f,length*.36f,width*.87f),white,24,18);
        }
        mesh=Own(g.Mesh(s.name+" / tailored anatomical mesh "+part));humanMeshes[key]=mesh;return mesh;
    }
    CreatureView Humanoid(Transform parent,SurfaceEcology.Species s) {
        var v=new CreatureView{kind=s.role,species=s};v.root=JointMotion.Joint(s.name,parent,Vector3.zero);
        v.torso=JointMotion.Joint("Breathing armored torso",v.root,Vector3.zero);var h=new HumanoidMotion{body=v.torso};v.humanoid=h;
        MeshObject("Pressure fabric / flexible torso",v.torso,HumanMesh(s,0),HumanMaterial(s,0));
        MeshObject("Curved ceramic plating / backpack",v.torso,HumanMesh(s,1),HumanMaterial(s,1));
        for(int i=-1;i<=1;i+=2) {
            Piece("Belt utility pouch",v.torso,PrimitiveType.Cube,new Vector3(i*.20f,-.14f,.16f),new Vector3(.14f,.13f,.09f),HumanMaterial(s,0));
            Piece("Suit communications indicator",v.torso,PrimitiveType.Cube,new Vector3(i*.15f,.34f,.217f),new Vector3(.02f,.05f,.015f),BeamMaterial(s.organ));
            Piece("Backpack oxygen canister",v.torso,PrimitiveType.Capsule,new Vector3(i*.145f,.20f,-.27f),new Vector3(.11f,.21f,.10f),HumanMaterial(s,1));
        }
        var head=JointMotion.Joint(s.helmet?"Sealed expedition helmet":"Aurelian / human-like face",v.torso,new Vector3(0,.81f,0));h.head=head;
        if(s.helmet) {
            MeshObject("Pressure helmet shell",head,HumanMesh(s,3),HumanMaterial(s,1));
            Piece("Smoked panoramic faceplate",head,PrimitiveType.Sphere,new Vector3(0,.01f,.105f),new Vector3(.37f,.275f,.24f),HumanMaterial(s,3));
            Piece("Helmet lamp housing",head,PrimitiveType.Cube,new Vector3(.16f,.13f,.09f),new Vector3(.08f,.07f,.12f),HumanMaterial(s,1));
            Piece("Helmet lamp",head,PrimitiveType.Cube,new Vector3(.16f,.13f,.154f),new Vector3(.05f,.035f,.012f),cyan);
        } else {
            MeshObject("Sculpted face / nose cheeks jaw and ears",head,HumanMesh(s,2),HumanMaterial(s,2));
            for(int side=-1;side<=1;side+=2) {
                Piece("Eye socket",head,PrimitiveType.Sphere,new Vector3(side*.063f,.026f,.125f),new Vector3(.061f,.028f,.032f),dark);
                Piece("Eye sclera",head,PrimitiveType.Sphere,new Vector3(side*.063f,.026f,.141f),new Vector3(.037f,.014f,.014f),ceramic);
                Piece("Aurelian iris",head,PrimitiveType.Sphere,new Vector3(side*.063f,.026f,.15f),new Vector3(.015f,.014f,.009f),HumanMaterial(s,3));
                Piece("Temple interface",head,PrimitiveType.Cube,new Vector3(side*.148f,.02f,-.06f),new Vector3(.035f,.08f,.09f),HumanMaterial(s,1));
            }
            Piece("Closed mouth / lip line",head,PrimitiveType.Sphere,new Vector3(0,-.096f,.148f),new Vector3(.076f,.009f,.012f),HumanMaterial(s,0));
            Piece("Swept cranial crest",head,PrimitiveType.Sphere,new Vector3(0,.142f,-.044f),new Vector3(.252f,.135f,.234f),HumanMaterial(s,0));
        }
        for(int i=0;i<2;i++) {
            int side=i==0?-1:1;var leg=h.legs[i]=HumanoidLimb(v.root,s,"Leg",new Vector3(side*.15f,-.14f,0),.48f,.50f,4,5);
            Piece("Articulated knee guard",leg.lower,PrimitiveType.Sphere,new Vector3(0,-.015f,.075f),new Vector3(.20f,.19f,.08f),HumanMaterial(s,1));
            Piece("Treaded expedition boot",leg.end,PrimitiveType.Cube,new Vector3(0,-.027f,.07f),new Vector3(.22f,.10f,.35f),HumanMaterial(s,3));
            Piece("Boot toe plate",leg.end,PrimitiveType.Cube,new Vector3(0,.022f,.17f),new Vector3(.21f,.06f,.14f),HumanMaterial(s,1));
            leg.home=new Vector3(side*.16f,-1.08f,.02f);
            var arm=h.arms[i]=HumanoidLimb(v.torso,s,"Arm",new Vector3(side*.32f,.45f,0),.34f,.35f,6,7);
            Piece("Tactical glove",arm.end,PrimitiveType.Sphere,Vector3.zero,new Vector3(.12f,.15f,.11f),HumanMaterial(s,0));
        }
        h.weapon=Rifle(v.torso);h.weapon.localPosition=new Vector3(.16f,.23f,.27f);if(s.role==1)h.weapon.localScale=new Vector3(1,1,1.25f);
        h.muzzle=JointMotion.Joint("Native ballistic muzzle",h.weapon,new Vector3(0,.025f,.81f));
        v.renderers.AddRange(v.root.GetComponentsInChildren<Renderer>());return v;
    }
    HumanoidMotion.Limb HumanoidLimb(Transform parent,SurfaceEcology.Species s,string name,Vector3 anchor,float a,float b,int upper,int lower) {
        var l=new HumanoidMotion.Limb{a=a,b=b};l.upper=JointMotion.Joint(name+" / proximal",parent,anchor);l.lower=JointMotion.Joint(name+" / distal",l.upper,Vector3.down*a);l.end=JointMotion.Joint(name+" / endpoint",l.lower,Vector3.down*b);
        MeshObject("Tailored limb",l.upper,HumanMesh(s,upper),HumanMaterial(s,0));MeshObject("Articulated armor shell",l.lower,HumanMesh(s,lower),HumanMaterial(s,1));return l;
    }
    public Transform TechnologySite(Transform parent,string type,SurfaceEcology.Species s) {
        if(type=="RELAY")return Site(parent,type,s.biome);
        var t=JointMotion.Joint(type=="NEST"?"SECURITY COMMAND / core terminal":type=="RECORDER"?"SETTLEMENT / encrypted security log":"ARCHIVE / research terminal",parent,Vector3.zero);
        Piece("Anchored platform",t,PrimitiveType.Cylinder,Vector3.up*.07f,new Vector3(type=="NEST"?5:2,.12f,type=="NEST"?5:2),metal);
        Piece("Terminal armored body",t,PrimitiveType.Cube,new Vector3(0,.72f,0),new Vector3(1.05f,1.4f,.68f),HumanMaterial(s,1));
        Piece("Inclined console",t,PrimitiveType.Cube,new Vector3(0,1.37f,.20f),new Vector3(.98f,.12f,.55f),dark,Quaternion.Euler(-15,0,0));
        Piece("Encrypted data display",t,PrimitiveType.Cube,new Vector3(0,1.46f,.19f),new Vector3(.80f,.025f,.32f),cyan,Quaternion.Euler(-15,0,0));
        for(int i=0;i<3;i++)Piece("Server cartridge",t,PrimitiveType.Cube,new Vector3(-.28f+i*.28f,.72f,.38f),new Vector3(.18f,.72f,.08f),metal);
        if(type=="NEST")for(int sign=-1;sign<=1;sign+=2)Piece("Command server tower",t,PrimitiveType.Cube,new Vector3(sign*1.55f,1.35f,0),new Vector3(.8f,2.7f,.9f),HumanMaterial(s,1));
        return t;
    }
}

public sealed class HumanoidMotion {
    public sealed class Limb {public Transform upper,lower,end;public Vector3 home,plant;public bool planted;public float a,b;}
    public readonly Limb[] legs=new Limb[2],arms=new Limb[2];public Transform body,head,weapon,muzzle;public Vector3 target,velocity;
    public float KneeFlex{get;private set;}float phase,kick;
    public void Kick(){kick=1;}
    public void Shift(Vector3 d){foreach(var l in legs)if(l!=null)l.plant-=d;target-=d;}
    public void Pose(SurfaceArt.CreatureView v,float time,float dt,float speed,float windup,float strike,float collapse,float hurt,bool dead) {
        var root=v.root;float move=Mathf.Clamp01(speed/1.3f);kick=Mathf.MoveTowards(kick,0,dt*8);if(!dead)phase+=speed*dt/1.7f;
        float breath=Mathf.Sin(time*2.4f)*.008f;body.localPosition=new Vector3(0,breath+Mathf.Cos(phase*12.566f)*move*.018f-collapse*.53f,0);body.localRotation=Quaternion.Euler(windup*-3+hurt*25,0,collapse*82);
        float pitch=target.sqrMagnitude>.1f?-Mathf.Asin(Mathf.Clamp(Vector3.Dot((target-head.position).normalized,root.up),-.7f,.7f))*Mathf.Rad2Deg:0;
        head.localRotation=Quaternion.Euler(pitch*.6f,Mathf.Sin(time*.5f)*2,0);
        weapon.localPosition=new Vector3(.16f,.23f,.27f-kick*.075f);weapon.localRotation=Quaternion.Euler(pitch-kick*8,0,0);
        KneeFlex=0;
        for(int i=0;i<2;i++) {
            var l=legs[i];float t=Mathf.Repeat(phase+i*.5f,1),swing=Mathf.Clamp01((t-.58f)/.42f);bool stance=t<.58f&&speed>.18f&&!dead;
            Vector3 direction=root.InverseTransformDirection(Vector3.ProjectOnPlane(velocity,root.up)).normalized;if(direction.sqrMagnitude<.01f)direction=Vector3.forward;
            Vector3 goal=l.home+direction*(stance?Mathf.Lerp(.23f,-.23f,t/.58f):Mathf.Lerp(-.23f,.23f,Mathf.SmoothStep(0,1,swing)))*move;
            Vector3 world=root.TransformPoint(goal);if(v.Contact!=null&&!dead){world=v.Contact(world);if(stance||speed<.18f){if(!l.planted||Vector3.Distance(l.plant,world)>.65f){l.plant=world;l.planted=true;}world=l.plant;}else{l.planted=false;world+=root.up*Mathf.Sin(swing*Mathf.PI)*.19f*move;}}
            if(dead)world=root.TransformPoint(Vector3.Lerp(goal,new Vector3(goal.x*1.6f,-.65f,.2f*(i==0?1:-1)),collapse));
            JointMotion.Solve(root,l.upper,l.lower,l.end,world,root.forward,l.a,l.b);KneeFlex=Mathf.Max(KneeFlex,Quaternion.Angle(l.upper.rotation,l.lower.rotation));
            var arm=arms[i];Vector3 grip=i==0?new Vector3(-.055f,-.025f,.43f):new Vector3(0,-.14f,-.10f);Vector3 hand=weapon.TransformPoint(grip);
            if(windup<=0&&speed<.15f&&Mathf.Sin(time*.7f)>.95f&&i==0)hand+=root.up*.06f;
            JointMotion.Solve(root,arm.upper,arm.lower,arm.end,hand,root.TransformDirection(new Vector3(i==0?-1:1,-.5f,-.25f)),arm.a,arm.b);
        }
    }
}
