using System;
using System.Collections.Generic;
using UnityEngine;

// Six complete cube-sphere trees. Parents remain visible until all children exist.
public sealed class PlanetTerrainLOD:MonoBehaviour {
 sealed class Patch {public int face,level;public float u,v,size;public GameObject go;public Mesh mesh;public Patch[] children;public Vector3 center,direction;public float width;}
 PlanetExpedition.Body body;Material material;Patch[] roots;int nodes,budget;public int MaxDepth{get;private set;}public int NodeCount{get{return nodes;}}public int LeafCount{get;private set;}public int Generated{get;private set;}
 public int maxLevel=10;const int N=16;Camera cameraRef;
 static readonly Vector3[] normals={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
 public static Vector3 Direction(int face,float u,float v){Vector3 n=normals[face],a=new Vector3(n.y,n.z,n.x),b=Vector3.Cross(n,a);return (n+a*u+b*v).normalized;}
 public void Initialize(PlanetExpedition.Body descriptor,Material surface){body=descriptor;material=surface;cameraRef=Camera.main;roots=new Patch[6];for(int i=0;i<6;i++)roots[i]=Create(i,0,-1,-1,2);}
 Vector3 Surface(Vector3 dir){return dir*((float)body.radius+PlanetExpedition.Height(dir,body.seed,body.radius));}
 Vector3 Normal(Vector3 dir){Vector3 a=Vector3.Cross(dir,Mathf.Abs(dir.y)>.9f?Vector3.right:Vector3.up).normalized,b=Vector3.Cross(dir,a);float e=4f/(float)body.radius;return Vector3.Cross(Surface((dir+a*e).normalized)-Surface((dir-a*e).normalized),Surface((dir+b*e).normalized)-Surface((dir-b*e).normalized)).normalized;}
 Patch Create(int face,int level,float u,float v,float size){var p=new Patch{face=face,level=level,u=u,v=v,size=size};p.direction=Direction(face,u+size*.5f,v+size*.5f);p.center=Surface(p.direction);p.width=size*(float)body.radius;p.go=new GameObject("Terrain L"+level+" / "+face);p.go.transform.SetParent(transform,false);var vertices=new List<Vector3>();var norms=new List<Vector3>();var triangles=new List<int>();float maxHeight=0;
  for(int y=0;y<=N;y++)for(int x=0;x<=N;x++){Vector3 dir=Direction(face,u+size*x/N,v+size*y/N);Vector3 point=Surface(dir);maxHeight=Mathf.Max(maxHeight,point.magnitude-(float)body.radius);vertices.Add(point);norms.Add(Normal(dir));if(x<N&&y<N){int i=y*(N+1)+x;triangles.AddRange(new[]{i,i+1,i+N+1,i+1,i+N+2,i+N+1});}}
  float skirt=maxHeight+(float)body.radius*.01f+2;for(int edge=0;edge<4;edge++){int start=vertices.Count;for(int k=0;k<=N;k++){int idx=edge==0?k:edge==1?N*(N+1)+k:edge==2?k*(N+1):k*(N+1)+N;Vector3 a=vertices[idx];vertices.Add(a-a.normalized*skirt);norms.Add(norms[idx]);if(k<N){int next=edge<2?idx+1:idx+N+1;triangles.AddRange(new[]{idx,next,start+k,next,start+k+1,start+k,idx,start+k,next,next,start+k,start+k+1});}}}
  p.mesh=new Mesh{name="Cube-sphere patch / shared analytic height"};p.mesh.SetVertices(vertices);p.mesh.SetNormals(norms);p.mesh.SetTriangles(triangles,0);p.mesh.RecalculateBounds();p.go.AddComponent<MeshFilter>().sharedMesh=p.mesh;p.go.AddComponent<MeshRenderer>().sharedMaterial=material;nodes++;Generated++;return p;
 }
 void Dispose(Patch p){if(p.children!=null)foreach(var c in p.children)Dispose(c);if(p.mesh!=null)Destroy(p.mesh);if(p.go!=null)Destroy(p.go);nodes--;}
 readonly List<Patch> leaves=new List<Patch>(),mergeable=new List<Patch>();
 readonly Vector3[] surfaceDetails=new Vector3[24];int detailCount;
 public void SetSurfaceDetails(List<Vector3> localPoints){detailCount=Mathf.Min(surfaceDetails.Length,localPoints.Count);for(int i=0;i<detailCount;i++)surfaceDetails[i]=localPoints[i];}
 float Score(Patch p,Vector3 focus){float distance=Vector3.Distance(focus,p.center);for(int i=0;i<detailCount;i++)if(Vector3.Distance(focus,surfaceDetails[i])<1800)distance=Mathf.Min(distance,Vector3.Distance(p.center,surfaceDetails[i])+3);return p.width/Mathf.Max(.1f,distance);}
 void Merge(Patch p){foreach(var c in p.children)Dispose(c);p.children=null;p.go.GetComponent<Renderer>().enabled=true;}
 void MergeFar(Patch p,Vector3 focus){if(p.children==null)return;foreach(var c in p.children)MergeFar(c,focus);if(p.level>=maxLevel||Score(p,focus)<.14f)Merge(p);}
 void Collect(Patch p){if(p.children==null){leaves.Add(p);return;}bool simple=true;foreach(var c in p.children){Collect(c);if(c.children!=null)simple=false;}if(simple)mergeable.Add(p);}
 void Split(Patch p){p.children=new Patch[4];float h=p.size*.5f;for(int k=0;k<4;k++)p.children[k]=Create(p.face,p.level+1,p.u+(k%2)*h,p.v+(k/2)*h,h);p.go.GetComponent<Renderer>().enabled=false;}
 public void RefineAt(Vector3 worldFocus,int frames=1){if(roots==null)return;Vector3 focus=worldFocus-transform.position;for(int i=0;i<frames;i++){foreach(var p in roots)MergeFar(p,focus);for(int split=0;split<2;split++){leaves.Clear();mergeable.Clear();foreach(var p in roots)Collect(p);Patch best=null;float score=.18f;foreach(var p in leaves){float s=Score(p,focus);if(p.level<maxLevel&&s>score&&Vector3.Dot(focus.normalized,p.direction)>-.18f){best=p;score=s;}}if(best==null)break;
   if(nodes+4>600){Patch loser=null;float low=score/1.3f;foreach(var p in mergeable){if(Array.IndexOf(p.children,best)>=0)continue;float s=Score(p,focus);if(s<low){low=s;loser=p;}}if(loser==null)break;Merge(loser);}Split(best);}
  }leaves.Clear();mergeable.Clear();foreach(var p in roots)Collect(p);MaxDepth=0;LeafCount=leaves.Count;foreach(var p in leaves)MaxDepth=Mathf.Max(MaxDepth,p.level);
 }
 void LateUpdate(){if(cameraRef!=null)RefineAt(cameraRef.transform.position);}
 public bool ClosedSkirts(){bool valid=true;Action<Patch> visit=null;visit=p=>{var vertices=p.mesh.vertices;for(int i=(N+1)*(N+1);i<vertices.Length;i++)valid&=vertices[i].magnitude<(float)body.radius*.99f+.01f;if(p.children!=null)foreach(var c in p.children)visit(c);};foreach(var p in roots)visit(p);return valid;}
 public float ContactMeshError(Vector3 worldFocus,out int samples){int count=0;float error=0;Vector3 focus=worldFocus-transform.position;Action<Patch> visit=null;visit=p=>{if(p.children!=null){foreach(var c in p.children)visit(c);return;}var v=p.mesh.vertices;for(int y=0;y<N;y++)for(int x=0;x<N;x++){int a=y*(N+1)+x;Vector3 point=(v[a]+v[a+1]+v[a+N+1])/3;if(Vector3.Distance(point,focus)>12)continue;error=Mathf.Max(error,Mathf.Abs(point.magnitude-(float)body.radius-PlanetExpedition.Height(point,body.seed,body.radius)));count++;}};foreach(var p in roots)visit(p);samples=count;return error;}
 void OnDestroy(){if(roots!=null)foreach(var p in roots)if(p.mesh!=null)Dispose(p);}
}
