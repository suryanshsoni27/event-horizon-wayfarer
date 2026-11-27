using System;
using UnityEngine;

// Stable planet-relative cells: moving the stream window does not reroll the vegetation.
// A single combined mesh carries root-to-tip wind weights; no per-blade objects or colliders.
public sealed class SurfaceFlora : MonoBehaviour {
    PlanetExpedition ground;SurfaceArt art;PlanetExpedition.Planet planet;Transform patch;Mesh mesh;Vector3 last;float timer;
    public int Plants{get;private set;}public int Grass{get;private set;}public int Succulents{get;private set;}public int IceClusters{get;private set;}
    public int Vertices{get{return mesh==null?0:mesh.vertexCount;}}public PlanetExpedition.Planet BoundPlanet{get{return patch!=null?planet:null;}}
    public SurfaceEcology.Biome Biome{get;private set;}
    public void Initialize(PlanetExpedition expedition,SurfaceArt materials){ground=expedition;art=materials;}
    void Update(){if(ground==null)return;timer-=Time.deltaTime;if(timer>0)return;timer=.7f;Vector3 focus=ground.OnFoot?ground.Focus:Camera.main.transform.position;var p=ground.Nearest(focus);if(p==null||ground.Alt(focus)>90){Clear();return;}if(p!=planet||(focus-last).sqrMagnitude>144)Rebuild(p,focus);}
    void Clear(){if(patch!=null)Destroy(patch.gameObject);if(mesh!=null)Destroy(mesh);mesh=null;patch=null;planet=null;Plants=Grass=Succulents=IceClusters=0;}
    static Color C(float r,float g,float b,float wind=0){return new Color(r,g,b,wind);}
    public static void Leaf(SurfaceArt.Geometry g,Vector3 at,Vector3 along,Vector3 up,float width,float length,Color color){
        Vector3 side=Vector3.Cross(along,up).normalized;int first=g.vertices.Count;const int steps=5;
        for(int k=0;k<=steps;k++) {
            float t=k/(float)steps,w=Mathf.Sin(t*Mathf.PI)*width;
            Vector3 center=at+along*length*t+up*(Mathf.Sin(t*Mathf.PI)*length*.23f-length*t*t*.12f);
            Vector3 normal=Vector3.Cross(side,along+up*((Mathf.Cos(t*Mathf.PI)*.23f*Mathf.PI)-.24f*t)).normalized;
            Color c=color;c.a=t;
            g.vertices.Add(center-side*w);g.vertices.Add(center+up*w*.15f);g.vertices.Add(center+side*w);
            g.uv.Add(new Vector2(0,t));g.uv.Add(new Vector2(.5f,t));g.uv.Add(new Vector2(1,t));
            g.colors.Add(c);g.colors.Add(c);g.colors.Add(c);g.normals.Add(normal);g.normals.Add(normal);g.normals.Add(normal);
            if(k<steps)for(int j=0;j<2;j++){int i=first+k*3+j;g.triangles.AddRange(new[]{i,i+1,i+3,i+1,i+4,i+3});}
        }
    }
    public void Refresh(){last=new Vector3(float.PositiveInfinity,0,0);timer=0;}
    void Rebuild(PlanetExpedition.Planet p,Vector3 focus){
        Clear();planet=p;last=focus;Biome=SurfaceEcology.Climate(p.key);Vector3 dir=(focus-p.body.P).normalized;
        var g=new SurfaceArt.Geometry();Vector3[] axes={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
        for(int face=0;face<6;face++) {
            Vector3 axis=axes[face];float facing=Vector3.Dot(dir,axis);if(facing<.55f)continue;
            Vector3 a=new Vector3(axis.y,axis.z,axis.x),b=Vector3.Cross(axis,a),cube=dir/facing;
            float tile=2.8f/(float)p.body.radius;int cx=Mathf.FloorToInt((Vector3.Dot(cube,a)+1)/tile),cy=Mathf.FloorToInt((Vector3.Dot(cube,b)+1)/tile);
            for(int x=cx-22;x<=cx+22;x++)for(int y=cy-22;y<=cy+22;y++) {
                var rng=new System.Random(unchecked(x*73856093^y*19349663^face*83492791^(int)p.body.seed*1107));
                float u=(x+(float)rng.NextDouble())*tile-1,v=(y+(float)rng.NextDouble())*tile-1;if(Mathf.Abs(u)>1||Mathf.Abs(v)>1)continue;
                Vector3 normal=PlanetTerrainLOD.Direction(face,u,v);float h=PlanetExpedition.Height(normal,p.body.seed,p.body.radius);
                if(h<.7f)continue;Vector3 pos=normal*((float)p.body.radius+h-.025f),world=p.body.P+pos;
                if((world-focus).sqrMagnitude>58*58||ground.Alt(world)<-.16f)continue;
                Vector3 t=PlanetExpedition.SurfaceTangent(normal,a),side=Vector3.Cross(normal,t);Quaternion rot=Quaternion.FromToRotation(Vector3.up,normal);
                float size=.55f+(float)rng.NextDouble()*.65f;int choice=rng.Next(24);Color c;
                if(Biome==SurfaceEcology.Biome.Temperate) {
                    c=C(.12f+size*.04f,.23f+size*.05f,.085f);
                    if(choice<3) { // Curved compound fronds with individual tapered leaflets.
                        float height=choice==0?1.5f:.9f;
                        g.Horn(pos,pos+normal*height,.025f,C(.16f,.20f,.08f),t*.12f,6,4);
                        for(int j=0;j<6;j++) {
                            float angle=j*2.39996f;Vector3 outward=t*Mathf.Cos(angle)+side*Mathf.Sin(angle);
                            Vector3 basePoint=pos+normal*(.17f+j*.12f);
                            for(int k=0;k<4;k++)for(int sign=-1;sign<=1;sign+=2)Leaf(g,basePoint+outward*(k*.13f),outward*.45f+Vector3.Cross(normal,outward)*sign*.85f,normal,.07f,size*(.32f-k*.035f),c);
                        }
                    } else { // Clumped, fine grasses rather than isolated broad triangular spikes.
                        for(int j=0;j<5;j++){float angle=j*2.4f;Vector3 outward=t*Mathf.Cos(angle)+side*Mathf.Sin(angle);Leaf(g,pos+outward*.07f,normal*.80f+outward*.38f,-outward,.018f,size*(.34f+(j%3)*.09f),c);}Grass++;
                    }
                } else if(Biome==SurfaceEcology.Biome.Arid) {
                    if(choice<2) { // Waxy succulent rosettes, sparse enough to expose desert soil.
                        c=C(.27f,.29f,.17f);
                        for(int j=0;j<9;j++){float angle=j*2.39996f;Vector3 outwards=t*Mathf.Cos(angle)+side*Mathf.Sin(angle);Leaf(g,pos+normal*.06f,outwards*.65f+normal*.75f,normal,.09f,size*.75f,c);}Succulents++;
                        g.Ellipsoid(pos+normal*.10f,new Vector3(.15f,.1f,.15f),C(.23f,.22f,.13f),12,8,rot);
                    } else if(choice<5) {
                        c=C(.37f,.30f,.16f);for(int j=0;j<5;j++){Vector3 outwards=Quaternion.AngleAxis(j*72,normal)*t;Leaf(g,pos,normal*.8f+outwards*.4f,-outwards,.012f,size*.35f,c);}
                    } else continue;
                } else {
                    if(choice<2) { // Rime shards are rigid; wind weights remain zero.
                        for(int j=0;j<4;j++){Vector3 q=pos+t*(j%2)*.19f+side*(j/2)*.19f;g.Horn(q,q+normal*(size*.55f+j*.12f)+t*.12f,.13f,C(.48f,.62f,.67f),Vector3.zero,5,2);}IceClusters++;
                    } else if(choice<5)g.Ellipsoid(pos,new Vector3(size*.28f,.05f,size*.23f),C(.29f,.33f,.26f),12,6,rot);
                    else continue;
                }
                Plants++;
            }
        }
        mesh=g.Mesh("Streamed "+Biome+" ecology / rooted curved foliage and rigid minerals");
        patch=art.MeshObject("PLANET ECOLOGY / "+Biome,p.root.transform,mesh,art.vegetation).transform;
    }
    void OnDestroy(){Clear();}
}
