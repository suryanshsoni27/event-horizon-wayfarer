using System.Collections.Generic;
using UnityEngine;
public static class GalaxyBackdrop {
 public static void Build(){} // Streaming creates actual spatial chunks, rather than a fixed backdrop.
 public static GameObject Chunk(Vector3 center,int seed,int theme){
  var root=new GameObject("Deterministic galaxy sector "+seed);root.transform.position=center;
  var random=new System.Random(seed);var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
  Color tint=theme==0?new Color(.2f,.55f,1):theme==1?new Color(1,.35f,.12f):theme==2?new Color(.7f,.25f,1):new Color(.2f,1,.7f);
  for(int i=0;i<180;i++){
   Vector3 p=new Vector3((float)random.NextDouble()*80-40,(float)random.NextDouble()*80-40,(float)random.NextDouble()*80-40);
   float size=.05f+(float)random.NextDouble()*.13f;Color c=Color.Lerp(tint,Color.white,(float)random.NextDouble());c.a=.9f;
   for(int axis=0;axis<3;axis++){Vector3 right=axis==0?Vector3.right:axis==1?Vector3.up:Vector3.forward,up=axis==0?Vector3.up:axis==1?Vector3.forward:Vector3.right;int n=vertices.Count;
    vertices.Add(p-right*size-up*size);vertices.Add(p+right*size-up*size);vertices.Add(p+right*size+up*size);vertices.Add(p-right*size+up*size);uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});for(int j=0;j<4;j++)colors.Add(c);triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
  }
  // Sparse spiral clouds mark sector themes, built from soft, actual world-space patches.
  for(int i=0;i<45;i++){float a=i*.7f,r=5+i*.48f;Vector3 p=new Vector3(Mathf.Cos(a)*r,(float)random.NextDouble()*7-3.5f,Mathf.Sin(a)*r);float size=5+(float)random.NextDouble()*6;int n=vertices.Count;
   vertices.Add(p+new Vector3(-size,0,-size));vertices.Add(p+new Vector3(size,0,-size));vertices.Add(p+new Vector3(size,0,size));vertices.Add(p+new Vector3(-size,0,size));uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});Color c=tint;c.a=.012f;for(int j=0;j<4;j++)colors.Add(c);triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
  var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=new Material(Shader.Find("Orbit/GalaxyParticles"));return root;
 }
}
