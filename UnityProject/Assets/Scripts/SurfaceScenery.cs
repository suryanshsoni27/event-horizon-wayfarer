using System;
using System.Collections.Generic;
using UnityEngine;

// Stable local scenery, anchored to the live planet. Two batches, bounded geometry and native contacts.
[DefaultExecutionOrder(-70)]
public sealed class SurfaceScenery : MonoBehaviour {
    public static SurfaceScenery Current{get;private set;}
    PlanetExpedition ground;SurfaceArt art;PlanetExpedition.Planet planet;Transform root;Mesh structure,leaves;Material bark,mineral;
    Vector3 last;float timer;
    public readonly List<Vector3> Contacts=new List<Vector3>();public readonly List<float> Radii=new List<float>();
    public int Landmarks{get;private set;}public int Vertices{get{return (structure==null?0:structure.vertexCount)+(leaves==null?0:leaves.vertexCount);}}
    public PlanetExpedition.Planet BoundPlanet{get{return root!=null&&planet!=null&&planet.root!=null&&root.parent==planet.root.transform?planet:null;}}
    public void Initialize(PlanetExpedition expedition,SurfaceArt materials){Current=this;ground=expedition;art=materials;bark=new Material(Shader.Find("Orbit/XenoChitin"));bark.SetTexture("_AlbedoMap",Resources.Load<Texture2D>("Surfaces/alien_bark_v1"));mineral=new Material(Shader.Find("Orbit/PlanetRock"));mineral.SetTexture("_BaseMap",Resources.Load<Texture2D>("Surfaces/rock_diff"));mineral.SetTexture("_BumpMap",Resources.Load<Texture2D>("Surfaces/rock_normal"));}
    public void Refresh(){last=new Vector3(float.PositiveInfinity,0,0);timer=0;}
    void Clear(){if(root!=null)Destroy(root.gameObject);if(structure!=null)Destroy(structure);if(leaves!=null)Destroy(leaves);root=null;structure=leaves=null;planet=null;Contacts.Clear();Radii.Clear();Landmarks=0;}
    void Update(){if(ground==null)return;timer-=Time.deltaTime;if(timer>0)return;timer=.8f;Vector3 focus=ground.OnFoot?ground.Focus:Camera.main.transform.position;var p=ground.Nearest(focus);if(p==null||ground.Alt(focus)>150){if(root!=null)Clear();return;}if(p!=planet||root==null||(focus-last).sqrMagnitude>22*22)Rebuild(p,focus);}
    public float Altitude(PlanetExpedition.Planet p,Vector3 world,float alt){if(p!=BoundPlanet||alt>16)return alt;Vector3 local=world-p.body.P;for(int i=0;i<Contacts.Count;i++)alt=Mathf.Min(alt,Vector3.Distance(local,Contacts[i])-Radii[i]);return alt;}
    public Vector3 Clearance(PlanetExpedition.Planet p,Vector3 world,float radius){if(p!=BoundPlanet)return world;for(int i=0;i<Contacts.Count;i++){Vector3 d=world-p.body.P-Contacts[i];float r=Radii[i]+radius;if(d.sqrMagnitude<r*r)world=p.body.P+Contacts[i]+(d.sqrMagnitude<.00001f?Vector3.up:d.normalized)*r;}return world;}
    struct Candidate {public Vector3 normal,pos;public int seed;public float distance;}
    void Rebuild(PlanetExpedition.Planet p,Vector3 focus) {
        Clear();planet=p;last=focus;var biome=SurfaceEcology.Climate(p.key);Vector3 dir=(focus-p.body.P).normalized;
        var candidates=new List<Candidate>();Vector3[] axes={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
        for(int face=0;face<6;face++) {
            Vector3 axis=axes[face];float facing=Vector3.Dot(dir,axis);if(facing<.55f)continue;
            Vector3 a=new Vector3(axis.y,axis.z,axis.x),cube=dir/facing;float tile=23/(float)p.body.radius;
            int cx=Mathf.FloorToInt((Vector3.Dot(cube,a)+1)/tile),cy=Mathf.FloorToInt((Vector3.Dot(cube,Vector3.Cross(axis,a))+1)/tile);
            for(int x=cx-6;x<=cx+6;x++)for(int y=cy-6;y<=cy+6;y++) {
                int seed=unchecked(x*73856093^y*19349663^face*83492791^(int)p.body.seed*5371);var rng=new System.Random(seed);
                if(rng.NextDouble()>.42)continue;float u=(x+(float)rng.NextDouble())*tile-1,v=(y+(float)rng.NextDouble())*tile-1;if(Mathf.Abs(u)>1||Mathf.Abs(v)>1)continue;
                Vector3 n=PlanetTerrainLOD.Direction(face,u,v);float h=PlanetExpedition.Height(n,p.body.seed,p.body.radius);if(h<2.5f)continue;
                Vector3 pos=n*((float)p.body.radius+h-.18f);float dist=(p.body.P+pos-focus).sqrMagnitude;if(dist>125*125||ground.Alt(p.body.P+pos)<-.4f)continue;
                candidates.Add(new Candidate{normal=n,pos=pos,seed=seed,distance=dist});
            }
        }
        candidates.Sort((a,b)=>a.distance.CompareTo(b.distance));var wood=new SurfaceArt.Geometry();var foliage=new SurfaceArt.Geometry();
        foreach(var c in candidates) {
            if(Landmarks>=28)break;var rng=new System.Random(c.seed);Vector3 n=c.normal,t=PlanetExpedition.SurfaceTangent(n,Vector3.forward),side=Vector3.Cross(n,t),at=c.pos;
            float size=.75f+(float)rng.NextDouble()*.65f,height=(biome==SurfaceEcology.Biome.Temperate?10:7)*size;
            Color color=biome==SurfaceEcology.Biome.Temperate?new Color(.16f,.18f,.14f,0):biome==SurfaceEcology.Biome.Arid?new Color(.42f,.26f,.15f,0):new Color(.40f,.56f,.61f,0);
            float thick=biome==SurfaceEcology.Biome.Temperate?.55f:biome==SurfaceEcology.Biome.Arid?1.9f:1.1f;
            wood.Tube(at-n*.4f,at+n*height,thick*size,(biome==SurfaceEcology.Biome.Arid?.85f:.1f)*size,t*size*.7f,color,biome==SurfaceEcology.Biome.Frozen?7:20,22);
            // Rounded native contact envelopes occupy only the solid lower trunk or pillar.
            for(int j=0;j<3;j++){Contacts.Add(at+n*(.45f+j*.72f)*size+t*Mathf.Sin((.45f+j*.72f)/height*Mathf.PI)*size*.7f);Radii.Add(thick*size*(1-j*.06f));}
            for(int branch=0;branch<(biome==SurfaceEcology.Biome.Temperate?7:biome==SurfaceEcology.Biome.Arid?0:4);branch++) {
                float angle=branch*2.39996f+(float)rng.NextDouble()*.3f;Vector3 outwards=t*Mathf.Cos(angle)+side*Mathf.Sin(angle);
                float y=height*(.45f+branch*.065f);Vector3 start=at+n*y+t*size*.55f,end=start+outwards*size*(biome==SurfaceEcology.Biome.Temperate?4.1f:1.9f)+n*size*2.0f;
                wood.Tube(start,end,.22f*size,.025f*size,n*size*.6f,color,biome==SurfaceEcology.Biome.Frozen?6:16,14);
                if(biome==SurfaceEcology.Biome.Temperate)for(int twig=0;twig<4;twig++) {
                    Vector3 tip=Vector3.Lerp(start,end,.44f+twig*.18f),across=Vector3.Cross(n,outwards);
                    for(int sign=-1;sign<=1;sign+=2) {
                        Vector3 leafDir=(outwards*.65f+across*sign*.65f+n*.12f).normalized;
                        SurfaceFlora.Leaf(foliage,tip,leafDir,n,.45f*size,2.0f*size,new Color(.10f+twig*.018f,.24f,.20f));
                        SurfaceFlora.Leaf(foliage,tip+leafDir*size*.55f,leafDir*.8f+outwards*.2f,n,.30f*size,1.35f*size,new Color(.24f,.27f,.12f));
                    }
                }
                else if(biome==SurfaceEcology.Biome.Frozen)wood.Horn(end-n*size,end+n*size*.6f,.32f*size,new Color(.50f,.65f,.7f),t*.25f,6,5);
            }
            for(int j=0;j<(biome==SurfaceEcology.Biome.Arid?0:5);j++) {
                Vector3 outwards=Quaternion.AngleAxis(j*72,n)*t,end=at+outwards*size*(biome==SurfaceEcology.Biome.Temperate?2.3f:1.7f);
                Vector3 radial=end.normalized;end=radial*((float)p.body.radius+PlanetExpedition.Height(radial,p.body.seed,p.body.radius));
                wood.Tube(at+n*size*.8f,end,.27f*size,.035f*size,n*size*.2f,color,14,10);
            }
            Landmarks++;
        }
        root=new GameObject("STREAMED LANDSCAPES / "+biome).transform;root.SetParent(p.root.transform,false);
        structure=wood.Mesh("Continuous branching "+biome+" landmarks");art.MeshObject("Rooted canopy / weathered pillars",root,structure,biome==SurfaceEcology.Biome.Temperate?bark:mineral);mineral.SetFloat("_Climate",(int)biome);
        if(foliage.vertices.Count>0){leaves=foliage.Mesh("Veined alien canopy / rooted wind");art.MeshObject("Shaped canopy leaves",root,leaves,art.vegetation);}
    }
    void OnDestroy(){Clear();if(bark!=null)Destroy(bark);if(mineral!=null)Destroy(mineral);if(Current==this)Current=null;}
}
