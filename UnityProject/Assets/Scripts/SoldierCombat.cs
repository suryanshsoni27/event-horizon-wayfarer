using UnityEngine;

public sealed partial class SurfaceOperations {
    public bool HumanRayHit(Foe f,Vector3 origin,Vector3 direction,float limit,out float distance,out bool head) {
        distance=limit;head=false;Vector3 p=World(f),up=Up(p),right=f.view==null?Vector3.Cross(up,direction).normalized:f.view.root.right;bool hit=false;
        Vector3[] centers={p+up*.81f,p+up*.19f,p-up*.22f,p-up*.70f-right*.15f,p-up*.70f+right*.15f};float[] radii={.23f,.34f,.27f,.22f,.22f};
        for(int i=0;i<centers.Length;i++){Vector3 d=centers[i]-origin;float projected=Vector3.Dot(d,direction),offset=d.sqrMagnitude-projected*projected,rr=radii[i]*radii[i];if(offset>rr)continue;float enter=projected-Mathf.Sqrt(Mathf.Max(0,rr-offset));if(enter<=0||enter>=distance)continue;distance=enter;head=i==0;hit=true;}return hit;
    }
    public Vector3 SoldierMuzzle(Foe f) {return f.view!=null&&f.view.Muzzle!=null?f.view.Muzzle.position:World(f)+Up(World(f))*.35f;}
    public bool SoldierSight(Foe f,Vector3 target) {
        Vector3 muzzle=SoldierMuzzle(f),ray=target-muzzle;float length=ray.magnitude;
        return length>.2f&&RayLimit(muzzle,ray/length,length)>=length-.55f;
    }
    Vector3 SoldierStep(Foe f,Vector3 p,Vector3 target,Vector3 up,Vector3 delta,float range,bool awake,float dt,int index) {
        var s=Species(f);Vector3 desired=Vector3.zero;if(!awake||f.stun>0){f.attacking=false;f.burst=0;return desired;}
        Vector3 aim=target+up*.12f;bool visible=SoldierSight(f,aim);
        if(f.burst>0) {
            f.burstClock-=dt;
            if(!visible){f.burst=0;f.cooldown=.8f;BlockedShots++;}
            else if(f.burstClock<=0){RifleShot(f,aim);f.burst--;f.burstClock=.16f;if(f.burst==0){f.cooldown=s.cooldown;Reloads++;}}
        } else if(f.attacking) {
            if(!visible||range>s.range+3){f.attacking=false;f.windup=0;f.cooldown=.4f;BlockedShots++;}
            else {f.windup-=dt;if(f.windup<=0){f.attacking=false;f.burst=s.role==1?1:3;f.burstClock=0;}}
        } else if(range<s.range&&f.cooldown<=0&&visible){f.attacking=true;f.windup=s.windup;}
        if(!f.attacking&&f.burst==0) {
            float preferred=s.role==1?26:17;
            if(range>preferred||!visible)desired=delta.normalized*s.speed;
            else if(range<8)desired=-delta.normalized*s.speed;
            if(range<s.range&&range>7)desired+=Vector3.Cross(up,delta.normalized)*((index%2==0?1:-1)*s.speed*.65f*Mathf.Sin(Current.age*.8f+index));
            desired=Vector3.ClampMagnitude(desired,s.speed);
        }
        return desired;
    }
    void RifleShot(Foe f,Vector3 target) {
        if(acids.Count>=24)return;var s=Species(f);Vector3 p=SoldierMuzzle(f),up=Up(p),ray=target-p;
        if(!SoldierSight(f,target)){BlockedShots++;return;}
        float speed=s.role==1?82:63,flight=ray.magnitude/speed;
        // Ballistic compensation for the same central gravity used by the native stepper.
        float gravity=(float)(Planet.body.mu/(p-Planet.body.P).sqrMagnitude);
        Vector3 velocity=(ray+up*(gravity*.5f*flight*flight)).normalized*speed;
        var t=art.Piece(s.name+" / physical rifle bolt",root,PrimitiveType.Capsule,Vector3.zero,new Vector3(.065f,.22f,.065f),art.BeamMaterial(s.organ));
        t.position=p;t.rotation=Quaternion.FromToRotation(Vector3.up,velocity.normalized);
        acids.Add(new Acid{view=t,damage=s.damage,state=new OrbitDemo.State{x=p.x,y=p.y,z=p.z,vx=velocity.x,vy=velocity.y,vz=velocity.z}});
        f.view?.Strike();RifleShots++;ProjectilesFired++;Beam(p,p+velocity.normalized*.65f,new Color(1,.61f,.22f),.085f,.065f);Sound(laserClip,.36f);
    }
}
