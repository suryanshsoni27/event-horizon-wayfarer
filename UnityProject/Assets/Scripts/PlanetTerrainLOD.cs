using System;
using System.Collections.Generic;
using UnityEngine;

// Bounded cube-sphere LOD. Fine edges follow the actual coarse edge, including cube-face joins.
public sealed class PlanetTerrainLOD : MonoBehaviour {
 sealed class Patch {
  public int face,level,revision;public float u,v,size,width,skirt;public GameObject go;public Mesh mesh;public Patch[] children;
  public Vector3 center,direction;public Vector3[] source,sourceNormals,draw,drawNormals;
 }
 [Serializable] public struct Foundation {public Vector3 normal;public float level,radius;}
 PlanetExpedition.Body body;Material material;Patch[] roots;int nodes,revision;Camera cameraRef;
 readonly List<Patch> leaves=new List<Patch>(),mergeable=new List<Patch>();
 readonly Vector3[] surfaceDetails=new Vector3[24];int detailCount;
 readonly Foundation[] foundations=new Foundation[24];int foundationCount;
 public int maxLevel=10;const int N=16,Grid=(N+1)*(N+1);const int NodeBudget=600;
 public int MaxDepth{get;private set;}public int NodeCount{get{return nodes;}}public int LeafCount{get;private set;}public int Generated{get;private set;}
 static readonly Vector3[] normals={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
 public static Vector3 Direction(int face,float u,float v){Vector3 n=normals[face],a=new Vector3(n.y,n.z,n.x),b=Vector3.Cross(n,a);return (n+a*u+b*v).normalized;}
 public void Initialize(PlanetExpedition.Body descriptor,Material surface){body=descriptor;material=surface;cameraRef=Camera.main;roots=new Patch[6];for(int i=0;i<6;i++)roots[i]=Create(i,0,-1,-1,2);}
 public void SetSurfaceDetails(List<Vector3> points){detailCount=Mathf.Min(surfaceDetails.Length,points.Count);for(int i=0;i<detailCount;i++)surfaceDetails[i]=points[i];}
 public bool SetFoundations(List<Foundation> pads){
  int count=Mathf.Min(foundations.Length,pads.Count);bool changed=count!=foundationCount;
  for(int i=0;i<count;i++)if(i>=foundationCount||foundations[i].normal!=pads[i].normal||Mathf.Abs(foundations[i].level-pads[i].level)>.01f||foundations[i].radius!=pads[i].radius)changed=true;
  if(!changed)return false;foundationCount=count;for(int i=0;i<count;i++)foundations[i]=pads[i];revision++;return true;
 }
 float Height(Vector3 dir,bool micro){
  float radius=(float)body.radius,height=PlanetExpedition.Height(dir,body.seed,body.radius,micro);
  for(int i=0;i<foundationCount;i++){
   var pad=foundations[i];float dot=Vector3.Dot(dir,pad.normal);if(dot<.99f)continue;
   float distance=(dir-pad.normal).magnitude*radius,outer=pad.radius+12;if(distance>=outer)continue;
   float plane=pad.level/Mathf.Max(.9f,dot)-radius-.24f;
   float blend=Mathf.SmoothStep(0,1,(outer-distance)/12);height=Mathf.Lerp(height,Mathf.Max(height,plane),blend);
  }return height;
 }
 public Vector3 SampleSurface(Vector3 direction){direction.Normalize();return direction*((float)body.radius+Height(direction,true));}
 Vector3 Surface(Vector3 dir){return dir*((float)body.radius+Height(dir,true));}
 Vector3 MacroSurface(Vector3 dir){return dir*((float)body.radius+Height(dir,false));}
 Vector3 Normal(Vector3 dir){Vector3 a=Vector3.Cross(dir,Mathf.Abs(dir.y)>.9f?Vector3.right:Vector3.up).normalized,b=Vector3.Cross(dir,a);float e=1.5f/(float)body.radius;return Vector3.Cross(MacroSurface((dir+a*e).normalized)-MacroSurface((dir-a*e).normalized),MacroSurface((dir+b*e).normalized)-MacroSurface((dir-b*e).normalized)).normalized;}
 void Geometry(Patch p,bool initial){
  var vertices=new Vector3[Grid+4*(N+1)];var vertexNormals=new Vector3[vertices.Length];float maxHeight=0;
  for(int y=0;y<=N;y++)for(int x=0;x<=N;x++){
   Vector3 dir=Direction(p.face,p.u+p.size*x/N,p.v+p.size*y/N),point=Surface(dir);maxHeight=Mathf.Max(maxHeight,point.magnitude-(float)body.radius);int k=y*(N+1)+x;vertices[k]=point;vertexNormals[k]=Normal(dir);
  }
  p.skirt=maxHeight+(float)body.radius*.01f+2;
  for(int edge=0;edge<4;edge++)for(int k=0;k<=N;k++){int index=EdgeIndex(edge,k),to=Grid+edge*(N+1)+k;Vector3 at=vertices[index];vertices[to]=at-at.normalized*p.skirt;vertexNormals[to]=vertexNormals[index];}
  p.source=vertices;p.sourceNormals=vertexNormals;p.draw=(Vector3[])vertices.Clone();p.drawNormals=(Vector3[])vertexNormals.Clone();p.revision=revision;
  p.mesh.vertices=vertices;p.mesh.normals=vertexNormals;
  if(initial){var triangles=new List<int>();for(int y=0;y<N;y++)for(int x=0;x<N;x++){int i=y*(N+1)+x;triangles.AddRange(new[]{i,i+1,i+N+1,i+1,i+N+2,i+N+1});}
   for(int edge=0;edge<4;edge++)for(int k=0;k<N;k++){int a=EdgeIndex(edge,k),b=EdgeIndex(edge,k+1),bottom=Grid+edge*(N+1)+k;triangles.AddRange(new[]{a,b,bottom,b,bottom+1,bottom,a,bottom,b,b,bottom,bottom+1});}p.mesh.SetTriangles(triangles,0);
  }p.mesh.RecalculateBounds();
 }
 static int EdgeIndex(int edge,int k){return edge==0?k:edge==1?N*(N+1)+k:edge==2?k*(N+1):k*(N+1)+N;}
 Patch Create(int face,int level,float u,float v,float size){
  var p=new Patch{face=face,level=level,u=u,v=v,size=size,width=size*(float)body.radius};p.direction=Direction(face,u+size*.5f,v+size*.5f);p.center=Surface(p.direction);
  p.go=new GameObject("Terrain L"+level+" / "+face);p.go.transform.SetParent(transform,false);p.mesh=new Mesh{name="Stitched cube-sphere / native terrain and colony foundations"};Geometry(p,true);
  p.go.AddComponent<MeshFilter>().sharedMesh=p.mesh;p.go.AddComponent<MeshRenderer>().sharedMaterial=material;nodes++;Generated++;return p;
 }
 void Dispose(Patch p){if(p.children!=null)foreach(var child in p.children)Dispose(child);if(p.mesh!=null)Destroy(p.mesh);if(p.go!=null)Destroy(p.go);nodes--;}
 float Score(Patch p,Vector3 focus){float distance=Vector3.Distance(focus,p.center);for(int i=0;i<detailCount;i++)if(Vector3.Distance(focus,surfaceDetails[i])<1800)distance=Mathf.Min(distance,Vector3.Distance(p.center,surfaceDetails[i])+3);return p.width/Mathf.Max(.1f,distance);}
 void Merge(Patch p){foreach(var child in p.children)Dispose(child);p.children=null;if(p.revision!=revision)Geometry(p,false);p.go.GetComponent<Renderer>().enabled=true;}
 bool MergeFar(Patch p,Vector3 focus){if(p.children==null)return false;bool changed=false;foreach(var child in p.children)changed|=MergeFar(child,focus);if(p.level>=maxLevel||Score(p,focus)<.14f){Merge(p);changed=true;}return changed;}
 void Collect(Patch p){if(p.children==null){leaves.Add(p);return;}bool simple=true;foreach(var child in p.children){Collect(child);if(child.children!=null)simple=false;}if(simple)mergeable.Add(p);}
 void CollectLeaves(){leaves.Clear();mergeable.Clear();foreach(var root in roots)Collect(root);}
 void Split(Patch p){p.children=new Patch[4];float half=p.size*.5f;for(int k=0;k<4;k++)p.children[k]=Create(p.face,p.level+1,p.u+(k%2)*half,p.v+(k/2)*half,half);p.go.GetComponent<Renderer>().enabled=false;}
 Patch FindLeaf(Vector3 direction){int face=0;float highest=-1;for(int i=0;i<6;i++){float d=Vector3.Dot(direction,normals[i]);if(d>highest){highest=d;face=i;}}Vector3 n=normals[face],a=new Vector3(n.y,n.z,n.x),b=Vector3.Cross(n,a);float u=Vector3.Dot(direction,a)/highest,v=Vector3.Dot(direction,b)/highest;var patch=roots[face];while(patch.children!=null){float half=patch.size*.5f;int index=(u>=patch.u+half?1:0)+(v>=patch.v+half?2:0);patch=patch.children[index];}return patch;}
 void EdgeCoordinates(Patch p,int edge,int k,out float u,out float v){u=edge==2?p.u:edge==3?p.u+p.size:p.u+p.size*k/N;v=edge==0?p.v:edge==1?p.v+p.size:p.v+p.size*k/N;}
 Patch Neighbor(Patch p,int edge,float u,float v){const float step=.000001f;if(edge==0)v-=step;else if(edge==1)v+=step;else if(edge==2)u-=step;else u+=step;return FindLeaf(Direction(p.face,u,v));}
 Vector3 EdgeSample(Patch p,Vector3 direction,bool normal){
  Vector3 n=normals[p.face],a=new Vector3(n.y,n.z,n.x),b=Vector3.Cross(n,a);float d=Vector3.Dot(direction,n),u=(Vector3.Dot(direction,a)/d-p.u)/p.size*N,v=(Vector3.Dot(direction,b)/d-p.v)/p.size*N;
  u=Mathf.Clamp(u,0,N);v=Mathf.Clamp(v,0,N);int x=Mathf.Min(N-1,(int)u),y=Mathf.Min(N-1,(int)v);var points=normal?p.drawNormals:p.draw;int k=y*(N+1)+x;
  return Vector3.Lerp(Vector3.Lerp(points[k],points[k+1],u-x),Vector3.Lerp(points[k+N+1],points[k+N+2],u-x),v-y);
 }
 void Stitch(){
  leaves.Sort((a,b)=>a.level.CompareTo(b.level));
  foreach(var p in leaves){Array.Copy(p.source,p.draw,p.source.Length);Array.Copy(p.sourceNormals,p.drawNormals,p.sourceNormals.Length);
   for(int edge=0;edge<4;edge++)for(int k=0;k<=N;k++){
    float u,v;EdgeCoordinates(p,edge,k,out u,out v);var neighbor=Neighbor(p,edge,u,v);if(neighbor==p||neighbor.level>=p.level)continue;int index=EdgeIndex(edge,k);Vector3 direction=Direction(p.face,u,v);
    p.draw[index]=EdgeSample(neighbor,direction,false);p.drawNormals[index]=EdgeSample(neighbor,direction,true).normalized;
   }
   for(int edge=0;edge<4;edge++)for(int k=0;k<=N;k++){int index=EdgeIndex(edge,k),bottom=Grid+edge*(N+1)+k;p.draw[bottom]=p.draw[index]-p.draw[index].normalized*p.skirt;p.drawNormals[bottom]=p.drawNormals[index];}
   p.mesh.vertices=p.draw;p.mesh.normals=p.drawNormals;p.mesh.RecalculateBounds();
  }
 }
 bool RefreshFoundations(){int budget=4;bool changed=false;foreach(var p in leaves){if(p.revision==revision)continue;bool overlaps=false;for(int i=0;i<foundationCount;i++)if(Vector3.Distance(p.center,foundations[i].normal*foundations[i].level)<p.width*1.2f+foundations[i].radius+14){overlaps=true;break;}if(!overlaps){p.revision=revision;continue;}if(budget--<=0)continue;Geometry(p,false);p.center=Surface(p.direction);changed=true;}return changed;}
 public void RefineAt(Vector3 worldFocus,int frames=1){
  if(roots==null)return;Vector3 focus=worldFocus-transform.position;bool changed=false;
  for(int i=0;i<frames;i++){
   foreach(var root in roots)changed|=MergeFar(root,focus);
   for(int split=0;split<2;split++){
    CollectLeaves();Patch best=null;float score=.18f;foreach(var p in leaves){float s=Score(p,focus);if(p.level<maxLevel&&s>score&&Vector3.Dot(focus.normalized,p.direction)>-.18f){best=p;score=s;}}if(best==null)break;
    if(nodes+4>NodeBudget){Patch loser=null;float low=score/1.3f;foreach(var p in mergeable){if(Array.IndexOf(p.children,best)>=0)continue;float s=Score(p,focus);if(s<low){low=s;loser=p;}}if(loser==null)break;Merge(loser);}Split(best);changed=true;
   }
  }
  CollectLeaves();changed|=RefreshFoundations();if(changed)Stitch();MaxDepth=0;LeafCount=leaves.Count;foreach(var p in leaves)MaxDepth=Mathf.Max(MaxDepth,p.level);
 }
 void LateUpdate(){if(cameraRef!=null)RefineAt(cameraRef.transform.position);}
 public float MaximumSeamError(out int samples){float maximum=0;int count=0;CollectLeaves();foreach(var p in leaves)for(int edge=0;edge<4;edge++)for(int k=1;k<N;k++){
  float u,v;EdgeCoordinates(p,edge,k,out u,out v);var neighbor=Neighbor(p,edge,u,v);if(neighbor==p||neighbor.level>p.level)continue;Vector3 at=EdgeSample(neighbor,Direction(p.face,u,v),false);maximum=Mathf.Max(maximum,Vector3.Distance(p.draw[EdgeIndex(edge,k)],at));count++;
 }samples=count;return maximum;}
 public bool ClosedSkirts(){bool valid=true;Action<Patch> visit=null;visit=p=>{for(int i=Grid;i<p.draw.Length;i++)valid&=p.draw[i].magnitude<(float)body.radius*.99f+.01f;if(p.children!=null)foreach(var child in p.children)visit(child);};foreach(var root in roots)visit(root);return valid;}
 public float ContactMeshError(Vector3 worldFocus,out int samples){int count=0;float error=0;Vector3 focus=worldFocus-transform.position;CollectLeaves();foreach(var p in leaves){var vertices=p.draw;for(int y=0;y<N;y++)for(int x=0;x<N;x++){int k=y*(N+1)+x;Vector3 point=(vertices[k]+vertices[k+1]+vertices[k+N+1])/3;if(Vector3.Distance(point,focus)>12)continue;error=Mathf.Max(error,Mathf.Abs(point.magnitude-(float)body.radius-Height(point.normalized,true)));count++;}}samples=count;return error;}
 public float RenderedRadius(Vector3 direction){direction.Normalize();float hit=0;CollectLeaves();foreach(var p in leaves){if(Vector3.Dot(p.direction,direction)<.9f)continue;var vertices=p.draw;for(int y=0;y<N;y++)for(int x=0;x<N;x++){int a=y*(N+1)+x;hit=Mathf.Max(hit,RayTriangle(direction,vertices[a],vertices[a+1],vertices[a+N+1]));hit=Mathf.Max(hit,RayTriangle(direction,vertices[a+1],vertices[a+N+2],vertices[a+N+1]));}}return hit;}
 static float RayTriangle(Vector3 direction,Vector3 a,Vector3 b,Vector3 c){Vector3 edge1=b-a,edge2=c-a,h=Vector3.Cross(direction,edge2);float determinant=Vector3.Dot(edge1,h);if(Mathf.Abs(determinant)<.000001f)return 0;float inv=1/determinant;Vector3 offset=-a;float u=Vector3.Dot(offset,h)*inv;if(u<-.00001f||u>1.00001f)return 0;Vector3 q=Vector3.Cross(offset,edge1);float v=Vector3.Dot(direction,q)*inv;if(v<-.00001f||u+v>1.00001f)return 0;return Mathf.Max(0,Vector3.Dot(edge2,q)*inv);}
 void OnDestroy(){if(roots!=null)foreach(var root in roots)Dispose(root);}
}
