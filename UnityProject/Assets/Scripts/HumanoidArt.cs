using System.Collections.Generic;
using UnityEngine;

// Original two-legged expedition opponents. Meshes and textured suit materials are shared per role.
public sealed partial class SurfaceArt {
    readonly Dictionary<int,Material> humanMaterials=new Dictionary<int,Material>();
    Material HumanMaterial(SurfaceEcology.Species s,int part) {
        int key=s.Id*10+part;if(humanMaterials.TryGetValue(key,out var m))return m;
        Color c=part==0?Color.Lerp(s.shell,Color.black,.42f):part==1?s.plate:part==2?s.shell:new Color(.023f,.034f,.039f);
        m=Own(ProductionMaterials.Create(c,part==1?.56f:part==3?.8f:.03f,part==3?.85f:part==1?.42f:.27f,0,0,part==0));humanMaterials[key]=m;return m;
    }
    CreatureView Humanoid(Transform parent,SurfaceEcology.Species s) {
        var v=new CreatureView{kind=s.role,species=s};v.root=JointMotion.Joint(s.name,parent,Vector3.zero);
        var model=SkinnedCharacterArt.Create(v.root,false,s.role==1,s.helmet,Color.Lerp(s.shell,Color.black,.60f),s.plate,s.helmet?Color.white:Color.Lerp(Color.white,s.shell,.36f),s.role);
        var performance=CharacterPerformance.Create(model,false);
        var h=new HumanoidMotion{performance=performance,body=model.Model,head=model.HeadSocket};v.humanoid=h;v.torso=model.Model;
        for(int i=0;i<2;i++){var l=performance.Legs[i];h.legs[i]=new HumanoidMotion.Limb{upper=l.upper,lower=l.lower,end=l.end};var a=performance.Arms[i];h.arms[i]=new HumanoidMotion.Limb{upper=a.upper,lower=a.lower,end=a.end};}
        h.weapon=Rifle(v.root);performance.BindWeapon(h.weapon);h.muzzle=performance.Muzzle;
        performance.Pose(.01f,Vector3.zero,false,true,true,false,v.root.forward);
        v.renderers.AddRange(v.root.GetComponentsInChildren<Renderer>());return v;
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
    public sealed class Limb {public Transform upper,lower,end;}
    public readonly Limb[] legs=new Limb[2],arms=new Limb[2];
    public CharacterPerformance performance;public Transform body,head,weapon,muzzle;public Vector3 target,velocity;
    public float KneeFlex{get{return performance.KneeFlex;}}
    public void Kick(){performance.Kick();}
    public void Reload(){performance.Reload();}
    public void Hit(bool headshot=false){performance.Hit(headshot);}
    public void Shift(Vector3 d){performance.Shift(d);target-=d;}
    public void Pose(SurfaceArt.CreatureView v,float time,float dt,float speed,float windup,float strike,float collapse,float hurt,bool dead) {
        if(dead)performance.Die();
        Vector3 aim=target.sqrMagnitude>.1f?(target-head.position).normalized:v.root.forward;
        performance.Pose(dt,velocity,false,true,true,windup>0||strike>0,aim,0,false,v.Contact);
    }
}
