using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// A manually evaluated animation graph keeps packaged gameplay, photo mode and capture in sync.
// Humanoid retargeting drives skinning. Contact IK and weapon constraints run after the clips.
public sealed class CharacterPerformance : MonoBehaviour {
    public sealed class Chain {
        public Transform upper, lower, end;
        public Vector3 plant; public bool planted;
    }
    public readonly Chain[] Legs = new Chain[2], Arms = new Chain[2];
    public SkinnedCharacterArt Art { get; private set; }
    public Transform Weapon { get; private set; }
    public Transform Muzzle { get; private set; }
    public Transform Magazine { get; private set; }
    public float GaitPhase { get; private set; }
    public float KneeFlex { get; private set; }
    public float MaxPlantSlip { get; private set; }
    public float GripError { get; private set; }
    public float ReloadProgress { get; private set; }
    public float ActionWeight { get; private set; }
    public float LocomotionBlend { get; private set; }
    public string State { get; private set; } = "IDLE";
    public int Shots { get; private set; }
    public int Reloads { get; private set; }
    public bool MagazineInHand { get; private set; }
    public bool IsDead { get; private set; }
    public bool GraphValid { get { return graph.IsValid(); } }

    PlayableGraph graph;
    AnimationMixerPlayable locomotion;
    AnimationLayerMixerPlayable layers;
    AnimationClipPlayable[] gait;
    AnimationClipPlayable upperAction, fullAction;
    AnimationClip[] gaitClips;
    string upperName = "", fullName = "";
    float upperTime, fullTime, speedSmooth, runSmooth, recoil, hit, reloadClock, reloadDuration, actionClock, actionDuration, fullWeight, upperWeight, elapsed, pelvisOffset;
    bool crew, wasGrounded=true, wasReloading, wasInteracting, seated;
    Vector3 magazineHome; Quaternion magazineRotation;
    Quaternion weaponInHand=Quaternion.identity;
    Vector3 previousForward;
    Transform hips, spine, chest, neck, head;
    Vector3 Forward { get { return crew ? -transform.forward : transform.forward; } }
    Vector3 Right { get { return Vector3.Cross(transform.up,Forward).normalized; } }
    float ActorScale { get { return Art.Scale * transform.lossyScale.y; } }
    public static CharacterPerformance Create(SkinnedCharacterArt art, bool crew) {
        var p = art.gameObject.AddComponent<CharacterPerformance>();p.Art=art;p.crew=crew;p.Initialize();return p;
    }
    Transform Bone(HumanBodyBones b) { return Art.Animator.GetBoneTransform(b); }
    void Initialize() {
        var library = PerformanceLibrary.Current;
        hips=Bone(HumanBodyBones.Hips); spine=Bone(HumanBodyBones.Spine); chest=Bone(HumanBodyBones.Chest);neck=Bone(HumanBodyBones.Neck);head=Bone(HumanBodyBones.Head);
        Legs[0]=ChainOf(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot);
        Legs[1]=ChainOf(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot);
        Arms[0]=ChainOf(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand);
        Arms[1]=ChainOf(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand);
        graph=PlayableGraph.Create("Wayfarer authored performances");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        gaitClips=new[]{library.Clip("Idle_Loop"),library.Clip("Walk_Loop"),library.Clip("Jog_Fwd_Loop"),library.Clip("Sprint_Loop")};
        gait=new AnimationClipPlayable[gaitClips.Length];locomotion=AnimationMixerPlayable.Create(graph,gait.Length);
        for(int i=0;i<gait.Length;i++){gait[i]=AnimationClipPlayable.Create(graph,gaitClips[i]);gait[i].SetSpeed(0);graph.Connect(gait[i],0,locomotion,i);}
        layers=AnimationLayerMixerPlayable.Create(graph,3);graph.Connect(locomotion,0,layers,0);layers.SetInputWeight(0,1);
        layers.SetLayerMaskFromAvatarMask(1,library.upperBody);
        var output=AnimationPlayableOutput.Create(graph,"Retargeted Humanoid skin",Art.Animator);output.SetSourcePlayable(layers);
        graph.Play();previousForward=Forward;EvaluateGait(0,0);graph.Evaluate(0);
    }
    Chain ChainOf(HumanBodyBones a,HumanBodyBones b,HumanBodyBones c){return new Chain{upper=Bone(a),lower=Bone(b),end=Bone(c)};}
    public void BindWeapon(Transform weapon) {
        Weapon=weapon; Weapon.SetParent(transform,true);
        Muzzle=JointMotion.Joint("Animated ballistic muzzle",weapon,new Vector3(0,.025f,.81f));
        var existing=weapon.Find("Exchangeable power magazine");
        Magazine=existing!=null?existing:JointMotion.Joint("Exchangeable power magazine",weapon,new Vector3(0,-.15f,.12f));
        if(existing==null){var mat=weapon.GetComponentInChildren<Renderer>().sharedMaterial;JointMotion.Piece("Magazine / beveled cell",Magazine,PrimitiveType.Cube,Vector3.down*.07f,new Vector3(.085f,.19f,.11f),mat);}
        magazineHome=Magazine.localPosition;magazineRotation=Magazine.localRotation;
        foreach(var t in weapon.GetComponentsInChildren<Transform>())t.gameObject.layer=gameObject.layer;
    }
    public void Kick(){recoil=1;Shots++;}
    public void Reload(float duration=1.45f){if(IsDead)return;reloadClock=0;reloadDuration=Mathf.Max(.5f,duration);Reloads++;SetUpper("Pistol_Reload");}
    public void Hit(bool headshot=false){if(IsDead)return;hit=1;actionClock=0;actionDuration=.42f;SetFull(headshot?"Hit_Head":"Hit_Chest");}
    public void Die(){if(IsDead)return;IsDead=true;SetFull("Death01");fullTime=0;foreach(var l in Legs)l.planted=false;RestoreMagazine();}
    public void ResetPose(){IsDead=false;speedSmooth=runSmooth=GaitPhase=MaxPlantSlip=pelvisOffset=0;actionDuration=reloadDuration=upperWeight=fullWeight=0;foreach(var l in Legs)l.planted=false;RestoreMagazine();}
    public void Shift(Vector3 d){foreach(var l in Legs)if(l.planted)l.plant-=d;}
    void SetUpper(string name){if(upperName==name&&upperTime<.1f)return;if(upperAction.IsValid()){graph.Disconnect(layers,1);graph.DestroyPlayable(upperAction);}upperAction=AnimationClipPlayable.Create(graph,PerformanceLibrary.Current.Clip(name));upperAction.SetSpeed(0);graph.Connect(upperAction,0,layers,1);upperName=name;upperTime=0;}
    void SetFull(string name){if(fullAction.IsValid()){graph.Disconnect(layers,2);graph.DestroyPlayable(fullAction);}fullAction=AnimationClipPlayable.Create(graph,PerformanceLibrary.Current.Clip(name));fullAction.SetSpeed(0);graph.Connect(fullAction,0,layers,2);fullName=name;fullTime=0;}
    static float Smooth(float a,float b,float rate,float dt){return Mathf.Lerp(a,b,1-Mathf.Exp(-rate*dt));}
    void EvaluateGait(float speed,float dt) {
        float move=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.65f,speed));
        float jog=Mathf.InverseLerp(2.05f,3.25f,speed),sprint=Mathf.InverseLerp(3.25f,4.8f,speed);
        float[] w={1-move,move*(1-jog),move*jog*(1-sprint),move*jog*sprint};
        LocomotionBlend=move;
        for(int i=0;i<gait.Length;i++){locomotion.SetInputWeight(i,w[i]);gait[i].SetTime(i==0?elapsed:gaitClips[i].length*Mathf.Repeat(GaitPhase,1));}
    }
    public void Pose(float dt,Vector3 velocity,bool running,bool grounded,bool armed,bool firing,Vector3 aim,float reload=0,bool interact=false,Func<Vector3,Vector3> contact=null) {
        armed=armed&&Weapon!=null; // Companions without a carbine retain the authored natural arm swing.
        if(!graph.IsValid())return;dt=Mathf.Clamp(dt,0,.1f);elapsed+=dt;if(seated&&Weapon!=null&&armed)Weapon.gameObject.SetActive(true);seated=false;
        float speed=Vector3.ProjectOnPlane(velocity,transform.up).magnitude;
        speedSmooth=Smooth(speedSmooth,speed,10,dt);runSmooth=Smooth(runSmooth,running?1:0,8,dt);
        if(!IsDead&&grounded)GaitPhase+=speedSmooth*dt/Mathf.Lerp(1.8f,2.8f,Mathf.Clamp01(speedSmooth/5)*runSmooth);
        if(reload>0&&!wasReloading)Reload();wasReloading=reload>0;
        if(interact&&!wasInteracting){SetUpper("Interact");upperTime=0;}wasInteracting=interact;
        if(!grounded&&wasGrounded&&!IsDead){SetFull("Jump_Loop");actionClock=0;actionDuration=1000;}
        if(grounded&&!wasGrounded&&!IsDead){SetFull("Jump_Land");actionClock=0;actionDuration=.55f;}wasGrounded=grounded;
        State=IsDead?"DEATH":reloadDuration>0?"RELOADING":interact?"INTERACTING":hit>.1f?"HIT":!grounded?"AIRBORNE":speedSmooth<.25f?"IDLE":running?"RUNNING":"WALKING";
        EvaluateGait(speedSmooth,dt);
        recoil=Mathf.MoveTowards(recoil,0,dt*8);hit=Mathf.MoveTowards(hit,0,dt*3);
        float upperTarget=0;
        if(reloadDuration>0){reloadClock+=dt;ReloadProgress=Mathf.Clamp01(reloadClock/reloadDuration);upperTarget=Mathf.SmoothStep(0,1,ReloadProgress/.12f)*Mathf.SmoothStep(0,1,(1-ReloadProgress)/.13f);upperTime=ReloadProgress*PerformanceLibrary.Current.Clip("Pistol_Reload").length;if(reloadClock>=reloadDuration){reloadDuration=0;ReloadProgress=1;RestoreMagazine();}}
        else if(interact){upperTarget=1;upperTime+=dt;}
        else if(armed){if(upperName!="Pistol_Aim_Neutral")SetUpper("Pistol_Aim_Neutral");upperTarget=firing?.80f:.35f;upperTime=0;}
        else upperTime+=dt;
        upperWeight=Smooth(upperWeight,upperTarget,18,dt);layers.SetInputWeight(1,upperWeight);
        if(upperAction.IsValid())upperAction.SetTime(upperTime);
        actionClock+=dt;fullTime+=dt;
        float fullTarget=IsDead?1:actionClock<actionDuration?Mathf.Min(1,actionClock/.065f)*Mathf.Min(1,(actionDuration-actionClock)/.12f):0;
        fullWeight=Smooth(fullWeight,fullTarget,24,dt);layers.SetInputWeight(2,fullWeight);ActionWeight=fullWeight;
        if(fullAction.IsValid()){var clip=PerformanceLibrary.Current.Clip(fullName);fullAction.SetTime(IsDead?Mathf.Min(fullTime,clip.length-.001f):fullName=="Jump_Loop"?Mathf.Repeat(fullTime,clip.length):Mathf.Min(fullTime,clip.length-.001f));}
        // Evaluate once, then constrain. No Animator Update/LateUpdate writes over the solved pose.
        Art.Model.localPosition=Vector3.down*(crew?.86f:1.08f);graph.Evaluate(0);
        if(IsDead){if(Weapon!=null){Weapon.position=Arms[1].end.position;Weapon.rotation=Arms[1].end.rotation*weaponInHand;}return;}
        Vector3 forward=Forward,up=transform.up,right=Right;
        float turn=Vector3.SignedAngle(previousForward,forward,up);previousForward=forward;
        float bank=Mathf.Clamp(turn/Mathf.Max(.01f,dt)*.025f,-8,8)*Mathf.Clamp01(speedSmooth/2);
        spine.rotation=Quaternion.AngleAxis(-bank,forward)*spine.rotation;
        if(aim.sqrMagnitude>.01f){float pitch=-Mathf.Asin(Mathf.Clamp(Vector3.Dot(aim.normalized,up),-.65f,.65f))*Mathf.Rad2Deg;spine.rotation=Quaternion.AngleAxis(pitch*.20f,right)*spine.rotation;chest.rotation=Quaternion.AngleAxis(pitch*.30f,right)*chest.rotation;head.rotation=Quaternion.AngleAxis(pitch*.35f,right)*head.rotation;}
        if(grounded&&contact!=null&&fullWeight<.5f)ContactFeet(contact,dt,speedSmooth);else foreach(var l in Legs)l.planted=false;
        KneeFlex=0;foreach(var l in Legs)KneeFlex=Mathf.Max(KneeFlex,180-Vector3.Angle(l.upper.position-l.lower.position,l.end.position-l.lower.position));
        if(Weapon!=null&&armed)WeaponPose(dt,aim,firing,interact);else if(MagazineInHand)RestoreMagazine();
    }
    void ContactFeet(Func<Vector3,Vector3> contact,float dt,float speed) {
        float scale=ActorScale;Vector3 up=transform.up;float offset=0;
        for(int i=0;i<2;i++){var l=Legs[i];Vector3 goal=contact(l.end.position)+up*.085f*scale;float height=Vector3.Dot(l.end.position-goal,up);if(height<.07f*scale||speed<.2f){if(!l.planted){l.plant=goal;l.planted=true;}if(Vector3.Distance(l.plant,l.upper.position)>scale*.97f||Vector3.Distance(l.plant,goal)>scale*.42f)l.plant=goal;offset=Mathf.Min(offset,Vector3.Dot(l.end.position-l.plant,up));}else l.planted=false;}
        pelvisOffset=Smooth(pelvisOffset,Mathf.Clamp(offset,-.16f*scale,.12f*scale),20,dt);hips.position-=up*pelvisOffset;
        for(int i=0;i<2;i++) {
            var l=Legs[i];Vector3 goal=contact(l.end.position)+up*.085f*scale;float h=Mathf.Max(0,Vector3.Dot(l.end.position-goal,up));
            if(l.planted)goal=l.plant;else goal+=up*h;
            Solve(l,goal,Forward);
            if(l.planted){MaxPlantSlip=Mathf.Max(MaxPlantSlip,Vector3.Distance(l.end.position,l.plant));Vector3 side=Right*.085f,ahead=Forward*.085f;Vector3 n=Vector3.Cross(contact(goal+ahead)-contact(goal-ahead),contact(goal+side)-contact(goal-side)).normalized;if(Vector3.Dot(n,up)<0)n=-n;n=Vector3.RotateTowards(up,n,30*Mathf.Deg2Rad,0);l.end.rotation=Quaternion.FromToRotation(up,n)*l.end.rotation;}
        }
    }
    void WeaponPose(float dt,Vector3 aim,bool firing,bool interact) {
        Vector3 up=transform.up,right=Right,forward=Forward;float scale=ActorScale;
        if(aim.sqrMagnitude<.1f)aim=forward;aim.Normalize();
        float carry=runSmooth*(firing?0:1);float ready=(!firing&&reloadDuration<=0)?17:0;
        Quaternion rotation=Quaternion.LookRotation(aim,up)*Quaternion.Euler(ready+carry*22-recoil*7,carry*-10,reloadDuration>0?-18*Mathf.Sin(ReloadProgress*Mathf.PI):0);
        Weapon.rotation=rotation;Weapon.localScale=Vector3.one*(crew?.78f:.88f);
        Weapon.position=Art.ChestSocket.position+right*.10f*scale-up*(.08f+carry*.07f)*scale+forward*.02f*scale-aim*recoil*.06f;
        Vector3 rightGrip=Weapon.TransformPoint(new Vector3(0,-.14f,-.10f));
        Vector3 leftGrip=Weapon.TransformPoint(new Vector3(-.035f,-.04f,.43f));
        // Original rifle reload: reach pouch, extract cell, insert, charge. Timings match both hands and the visible magazine.
        if(reloadDuration>0) {
            float t=ReloadProgress;Vector3 port=Weapon.TransformPoint(magazineHome+Vector3.down*.12f),pouch=hips.position+right*-.18f*scale-up*.13f*scale+forward*.07f*scale;
            if(t<.18f)leftGrip=Vector3.Lerp(leftGrip,port,Mathf.SmoothStep(0,1,t/.18f));
            else if(t<.38f)leftGrip=Vector3.Lerp(port,pouch,Mathf.SmoothStep(0,1,(t-.18f)/.20f));
            else if(t<.55f)leftGrip=pouch;
            else if(t<.76f)leftGrip=Vector3.Lerp(pouch,port,Mathf.SmoothStep(0,1,(t-.55f)/.21f));
            else if(t<.88f)leftGrip=Vector3.Lerp(port,Weapon.TransformPoint(new Vector3(-.06f,.10f,-.09f)),Mathf.SmoothStep(0,1,(t-.76f)/.12f));
            else leftGrip=Vector3.Lerp(Weapon.TransformPoint(new Vector3(-.06f,.10f,-.09f)),leftGrip,Mathf.SmoothStep(0,1,(t-.88f)/.12f));
            MagazineInHand=t>=.18f&&t<.76f;
        }
        if(interact){leftGrip=chest.position+forward*.48f*scale-up*.03f;}
        Solve(Arms[1],rightGrip,right-up*.4f-forward*.2f);Solve(Arms[0],leftGrip,-right-up*.4f-forward*.2f);
        // Finger curls come from the authored aim/reload layer; orient the palms to the two handles.
        Arms[1].end.rotation=Weapon.rotation*Quaternion.Euler(0,90,75);Arms[0].end.rotation=Weapon.rotation*Quaternion.Euler(0,-90,-70);
        weaponInHand=Quaternion.Inverse(Arms[1].end.rotation)*Weapon.rotation;
        GripError=Mathf.Max(Vector3.Distance(Arms[1].end.position,rightGrip),Vector3.Distance(Arms[0].end.position,leftGrip));
        if(Magazine!=null){if(MagazineInHand){Magazine.position=Arms[0].end.position;Magazine.rotation=Weapon.rotation;}else RestoreMagazine();}
    }
    void RestoreMagazine(){MagazineInHand=false;if(Magazine!=null&&Weapon!=null){Magazine.SetParent(Weapon,false);Magazine.localPosition=magazineHome;Magazine.localRotation=magazineRotation;}}
    public void SeatPose(bool driver) {
        if(!seated||fullName!=(driver?"Driving_Loop":"Sitting_Idle_Loop")){SetFull(driver?"Driving_Loop":"Sitting_Idle_Loop");seated=true;}State=driver?"DRIVING":"PASSENGER";fullTime+=.01f;
        layers.SetInputWeight(2,1);fullAction.SetTime(Mathf.Repeat(fullTime,PerformanceLibrary.Current.Clip(fullName).length));layers.SetInputWeight(1,0);graph.Evaluate(0);
        if(Weapon!=null)Weapon.gameObject.SetActive(false);
        foreach(var l in Legs)l.planted=false;
    }
    // Solve from the authored joint directions, preserving each exporter's bind axes and twist.
    public static void Solve(Chain l,Vector3 target,Vector3 pole) {
        Vector3 origin=l.upper.position;float a=Vector3.Distance(origin,l.lower.position),b=Vector3.Distance(l.lower.position,l.end.position);
        Vector3 delta=target-origin;float d=Mathf.Clamp(delta.magnitude,.01f,a+b-.0001f);Vector3 direction=delta.sqrMagnitude>.00001f?delta.normalized:Vector3.down;
        Vector3 bend=Vector3.ProjectOnPlane(pole,direction).normalized;if(bend.sqrMagnitude<.01f)bend=Vector3.ProjectOnPlane(l.lower.position-origin,direction).normalized;
        float along=(a*a+d*d-b*b)/(2*d),height=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));Vector3 knee=origin+direction*along+bend*height;
        l.upper.rotation=Quaternion.FromToRotation(l.lower.position-origin,knee-origin)*l.upper.rotation;
        l.lower.rotation=Quaternion.FromToRotation(l.end.position-l.lower.position,target-l.lower.position)*l.lower.rotation;
    }
    void OnDestroy(){if(graph.IsValid())graph.Destroy();}
}
