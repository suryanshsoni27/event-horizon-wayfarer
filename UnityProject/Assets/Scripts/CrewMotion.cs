using UnityEngine;

// Gameplay adapter for the shared skinned performance graph.
public sealed class CrewMotion : MonoBehaviour {
    public bool Armed=true,Interacting,Firing;
    public float Reload;public Vector3 Aim;
    public CharacterPerformance Performance {get;private set;}
    public Transform Grip{get{return Performance.Arms[1].end;}}
    public float KneeFlex{get{return Performance.KneeFlex;}}
    public float GaitPhase{get{return Performance.GaitPhase;}}
    public float MaxPlantSlip{get{return Performance.MaxPlantSlip;}}
    public string State{get{return Performance.State;}}
    public static CrewMotion Create(Transform root){
        var old=root.GetComponent<CrewMotion>();if(old!=null)return old;
        var m=root.gameObject.AddComponent<CrewMotion>();m.Build();return m;
    }
    void Build(){
        foreach(var r in GetComponentsInChildren<Renderer>(true))if(r.name!="Runtime-only helmet photo")r.enabled=false;
        var art=SkinnedCharacterArt.Create(transform,true,name.EndsWith("2"),true,new Color(.15f,.21f,.25f),new Color(.67f,.72f,.73f),Color.white,0);
        Performance=CharacterPerformance.Create(art,true);MountPortrait();
    }
    // Keep the private photo object at the root for the existing roster API, but follow the head.
    void MountPortrait(){var face=transform.Find("Runtime-only helmet photo");if(face!=null){face.localPosition=transform.InverseTransformPoint(Performance.Art.HeadSocket.TransformPoint(new Vector3(0,.06f,-.254f)));face.localRotation=Quaternion.Inverse(transform.rotation)*Performance.Art.HeadSocket.rotation;face.localScale=new Vector3(.27f,.21f,1);}}
    public void Kick(){Performance.Kick();}
    public void ResetPose(){Performance.ResetPose();}
    public void Shift(Vector3 d){Performance.Shift(d);}
    public void SeatPose(bool driver){Performance.SeatPose(driver);MountPortrait();}
    public void Pose(float dt,Vector3 velocity,bool running,PlanetExpedition terrain,bool grounded){
        Performance.Pose(dt,velocity,running,grounded,Armed,Firing,Aim,Reload,Interacting,p=>p-transform.up*(terrain.Alt(p)-.015f));MountPortrait();
    }
}
