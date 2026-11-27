using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// A compact military research settlement anchored to the expedition's saved landing area.
// Buildings remain when boarding; their planet owns the views and native collision envelopes.
public sealed class SurfaceSettlement : MonoBehaviour {
    public static SurfaceSettlement Current{get;private set;}
    public readonly List<Vector3> Contacts=new List<Vector3>();public readonly List<float> Radii=new List<float>();
    public PlanetExpedition.Planet BoundPlanet{get{return root!=null&&planet!=null&&planet.root!=null?planet:null;}}
    public int Buildings{get;private set;}public Transform Root{get{return root;}}
    readonly List<Mesh> batches=new List<Mesh>();readonly List<Material> materials=new List<Material>();
    public int RenderBatches{get{return batches.Count;}}
    PlanetExpedition ground;SurfaceArt art;PlanetExpedition.Planet planet;Transform root;
    public void Initialize(PlanetExpedition g,SurfaceArt a){Current=this;ground=g;art=a;}
    public void Clear(){if(root!=null)Destroy(root.gameObject);foreach(var mesh in batches)if(mesh!=null)Destroy(mesh);batches.Clear();foreach(var mat in materials)if(mat!=null)Destroy(mat);materials.Clear();root=null;planet=null;Contacts.Clear();Radii.Clear();Buildings=0;}
    Vector3 Floor(Vector3 local,float radius) {
        Vector3 up=local.normalized,t=PlanetExpedition.SurfaceTangent(up,Vector3.forward),side=Vector3.Cross(up,t);float h=PlanetExpedition.Height(up,planet.body.seed,planet.body.radius);
        for(int i=0;i<8;i++){float angle=i*Mathf.PI/4;Vector3 n=(local+(t*Mathf.Cos(angle)+side*Mathf.Sin(angle))*radius).normalized;h=Mathf.Max(h,PlanetExpedition.Height(n,planet.body.seed,planet.body.radius));}
        return up*((float)planet.body.radius+h+.15f);
    }
    public void Bind(PlanetExpedition.Planet p,Vector3 anchor,Vector3 heading) {
        if(BoundPlanet==p)return;Clear();if(SurfaceEcology.Civilization(p.key)==SurfaceEcology.Society.Wildlife)return;
        planet=p;root=new GameObject(SurfaceEcology.SocietyName(p.key)+" / MILITARY RESEARCH OUTPOST").transform;root.SetParent(p.root.transform,false);
        Vector3 up=anchor.normalized,forward=PlanetExpedition.SurfaceTangent(up,heading),side=Vector3.Cross(up,forward);
        bool aliens=SurfaceEcology.Civilization(p.key)==SurfaceEcology.Society.AurelianConcord;
        Material facade=ProductionMaterials.Create(aliens?new Color(.40f,.43f,.36f):new Color(.54f,.59f,.60f),.38f,.32f),glass=ProductionMaterials.Create(new Color(.08f,.18f,.23f),.50f,.80f,.3f,.05f);
        materials.Add(facade);materials.Add(glass);
        // Keep the approach, mission terminals and the ship's arrival area open.
        Vector2[] points={new Vector2(-39,23),new Vector2(47,44),new Vector2(-51,68),new Vector2(23,91),new Vector2(-12,105)};
        for(int i=0;i<points.Length;i++) {
            float radius=i==4?4.6f:i==2?4.1f:3.4f,height=i==4?18:i==2?11:7;
            Vector3 pos=Floor(anchor+side*points[i].x+forward*points[i].y,radius);
            var b=JointMotion.Joint((i==4?"Command tower":i==2?"Research observatory":"Pressure habitat")+" / solid native contacts",root,pos);b.localRotation=Quaternion.LookRotation(PlanetExpedition.SurfaceTangent(pos.normalized,forward),pos.normalized);
            art.Piece("Terrain pier foundation",b,PrimitiveType.Cylinder,Vector3.down*2,new Vector3(radius*2+.6f,2.2f,radius*2+.6f),art.dark);
            art.Piece("Reinforced cylindrical facade",b,PrimitiveType.Cylinder,Vector3.up*height*.5f,new Vector3(radius*2,height*.5f,radius*2),facade);
            art.Piece("Ceramic roof overhang",b,PrimitiveType.Cylinder,Vector3.up*(height+.12f),new Vector3(radius*2+.7f,.18f,radius*2+.7f),art.ceramic);
            for(int floor=0;floor<(i==4?5:2);floor++)for(int window=0;window<12;window++) {
                float angle=window*30*Mathf.Deg2Rad;Vector3 n=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));
                var windowView=art.Piece("Recessed observation window",b,PrimitiveType.Cube,n*(radius+.04f)+Vector3.up*(2+floor*2.8f),new Vector3(radius*.27f,1.05f,.07f),glass);windowView.localRotation=Quaternion.Euler(0,window*30,0);
                var trim=art.Piece("Facade light strip",b,PrimitiveType.Cube,n*(radius+.09f)+Vector3.up*(1.36f+floor*2.8f),new Vector3(radius*.25f,.035f,.04f),aliens?art.cyan:art.amber);trim.localRotation=windowView.localRotation;
            }
            for(int level=0;level<(i==4?6:3);level++)art.Piece("Ceramic facade expansion band",b,PrimitiveType.Cylinder,Vector3.up*(.35f+level*(height-.6f)/(i==4?5:2)),new Vector3(radius*2+.12f,.07f,radius*2+.12f),art.ceramic);
            for(int rib=0;rib<8;rib++){float a=rib*Mathf.PI/4;art.Piece("Vertical structural rib",b,PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*(radius+.07f),height*.5f,Mathf.Cos(a)*(radius+.07f)),new Vector3(.09f,height,.16f),art.metal,Quaternion.Euler(0,rib*45,0));}
            for(int vent=0;vent<4;vent++)art.Piece("Rooftop heat exchanger",b,PrimitiveType.Cube,new Vector3(-1.4f+vent*.85f,height+.3f,-.6f),new Vector3(.6f,.45f,1.5f),art.metal);
            art.Piece("Airlock outer frame",b,PrimitiveType.Cube,new Vector3(0,1.3f,radius+.12f),new Vector3(1.75f,2.6f,.3f),art.ceramic);
            art.Piece("Sealed security airlock",b,PrimitiveType.Cube,new Vector3(0,1.28f,radius+.29f),new Vector3(1.35f,2.35f,.08f),art.dark);
            for(int k=0;k<3;k++) {
                float y=Mathf.Lerp(Mathf.Min(radius,height*.5f),Mathf.Max(height-radius,height*.5f),k*.5f);
                Contacts.Add(pos+pos.normalized*y);Radii.Add(radius*.99f);
            }
            Buildings++;
            for(int antenna=0;antenna<3;antenna++)art.Piece("Rooftop communications array",b,PrimitiveType.Cylinder,new Vector3((antenna-1)*1.1f,height+1,0),new Vector3(.07f,1,.07f),art.ceramic);
            if(i==2)art.Piece("Observatory radome",b,PrimitiveType.Sphere,Vector3.up*(height+.5f),new Vector3(5.8f,3.4f,5.8f),art.ceramic);
        }
        for(int i=0;i<5;i++) {
            Vector3 pos=Floor(anchor+side*(58+i*4)+forward*12,2);var solar=JointMotion.Joint("Tracking photovoltaic array",root,pos);solar.localRotation=Quaternion.FromToRotation(Vector3.up,pos.normalized);
            art.Piece("Array pedestal",solar,PrimitiveType.Cylinder,Vector3.up,new Vector3(.22f,1,.22f),art.metal);
            art.Piece("Solar panel",solar,PrimitiveType.Cube,new Vector3(0,2.1f,0),new Vector3(3.2f,.08f,5.2f),art.dark,Quaternion.Euler(0,20,-23));
            for(int strip=0;strip<6;strip++)art.Piece("Photovoltaic cell bus",solar,PrimitiveType.Cube,new Vector3(-1.2f+strip*.48f,2.18f,0),new Vector3(.018f,.015f,4.7f),art.cyan,Quaternion.Euler(0,20,-23));
        }
        // A launch gantry and supply rocket make the settlement's technology level visible.
        Vector3 launch=Floor(anchor-side*65+forward*28,6);var pad=JointMotion.Joint("Orbital launch facility",root,launch);pad.localRotation=Quaternion.FromToRotation(Vector3.up,launch.normalized);
        art.Piece("Launch pad",pad,PrimitiveType.Cylinder,Vector3.zero,new Vector3(12,.18f,12),art.metal);
        art.Piece("Orbital supply rocket",pad,PrimitiveType.Capsule,Vector3.up*7,new Vector3(2.1f,7,2.1f),art.ceramic);
        art.Piece("Rocket service gantry",pad,PrimitiveType.Cube,new Vector3(-3,6,0),new Vector3(1.1f,12,1.2f),art.metal);
        for(int i=0;i<5;i++)art.Piece("Gantry cross brace",pad,PrimitiveType.Cube,new Vector3(-2,2+i*2.1f,0),new Vector3(2.6f,.18f,1.3f),art.ceramic);
        for(int i=0;i<3;i++){Contacts.Add(launch+launch.normalized*(1.1f+i*4.5f));Radii.Add(1.2f);Contacts.Add(launch+pad.TransformVector(new Vector3(-3,1+i*4.5f,0)));Radii.Add(.9f);}
        CompileBatches();SurfaceScatter.Current?.Refresh();
    }
    void CompileBatches() {
        var groups=new Dictionary<Material,List<CombineInstance>>();var filters=root.GetComponentsInChildren<MeshFilter>();
        foreach(var filter in filters){var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null)continue;var material=renderer.sharedMaterial;if(!groups.TryGetValue(material,out var group)){group=new List<CombineInstance>();groups.Add(material,group);}group.Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix});Destroy(renderer);Destroy(filter);}
        foreach(var group in groups){var mesh=new Mesh{name="Military settlement / material batch",indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(group.Value.ToArray(),true,true);mesh.RecalculateBounds();batches.Add(mesh);art.MeshObject("Batched settlement architecture",root,mesh,group.Key);}
    }
    public float Altitude(PlanetExpedition.Planet p,Vector3 world,float alt) {
        if(p!=BoundPlanet||alt>30)return alt;Vector3 local=world-p.body.P;
        for(int i=0;i<Contacts.Count;i++)alt=Mathf.Min(alt,(local-Contacts[i]).magnitude-Radii[i]);return alt;
    }
    public Vector3 Clearance(PlanetExpedition.Planet p,Vector3 world,float radius) {
        if(p!=BoundPlanet)return world;for(int i=0;i<Contacts.Count;i++){Vector3 c=p.body.P+Contacts[i],d=world-c;float r=radius+Radii[i];if(d.sqrMagnitude<r*r)world=c+(d.sqrMagnitude>.00001f?d.normalized:world.normalized)*r;}return world;
    }
    void Update(){if(planet!=null&&(planet.root==null||!ground.planets.Contains(planet)))Clear();}
    void OnDestroy(){Clear();if(Current==this)Current=null;}
}
