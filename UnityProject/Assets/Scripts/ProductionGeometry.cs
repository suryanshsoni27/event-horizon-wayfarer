using System.Collections.Generic;
using UnityEngine;

// Small original meshes shared by ships and architecture. Bevels catch real lighting.
public static class ProductionGeometry {
 static Mesh roundedBox,pressureHull;
 public static Mesh BeveledBox {
  get {
   if(roundedBox!=null)return roundedBox;
   var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
   const float edge=.425f,radius=.075f;float[] grid={-.5f,-edge,edge,.5f};
   foreach(Vector3 n in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back}){
    Vector3 a=new Vector3(n.y,n.z,n.x),b=Vector3.Cross(n,a);int start=vertices.Count;
    for(int y=0;y<4;y++)for(int x=0;x<4;x++){
     Vector3 p=n*.5f+a*grid[x]+b*grid[y];Vector3 core=new Vector3(Mathf.Clamp(p.x,-edge,edge),Mathf.Clamp(p.y,-edge,edge),Mathf.Clamp(p.z,-edge,edge));
     Vector3 normal=(p-core).normalized;vertices.Add(core+normal*radius);normals.Add(normal);uv.Add(new Vector2(grid[x]+.5f,grid[y]+.5f));
     if(x<3&&y<3){int i=start+y*4+x;triangles.AddRange(new[]{i,i+1,i+4,i+1,i+5,i+4});}
    }
   }
   roundedBox=new Mesh{name="Original / shared bevelled armor block"};roundedBox.SetVertices(vertices);roundedBox.SetNormals(normals);roundedBox.SetUVs(0,uv);roundedBox.SetTriangles(triangles,0);roundedBox.RecalculateBounds();return roundedBox;
  }
 }
 public static Mesh PressureHull {
  get {
   if(pressureHull!=null)return pressureHull;
   var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
   float[] z={-.5f,-.42f,-.24f,.10f,.34f,.48f,.5f},width={.22f,.36f,.5f,.40f,.22f,.055f,.012f},height={.19f,.30f,.42f,.30f,.15f,.045f,.012f};const int sides=16;
   for(int r=0;r<z.Length;r++)for(int k=0;k<sides;k++){
    float angle=k*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(angle)*width[r],Mathf.Sin(angle)*height[r],z[r]));uv.Add(new Vector2(k/(float)sides,(z[r]+.5f)));
    if(r<z.Length-1){int a=r*sides+k,b=r*sides+(k+1)%sides,c=(r+1)*sides+k,d=(r+1)*sides+(k+1)%sides;triangles.AddRange(new[]{a,b,c,b,d,c});}
   }
   int tail=vertices.Count;vertices.Add(new Vector3(0,0,-.5f));uv.Add(Vector2.zero);int nose=vertices.Count;vertices.Add(new Vector3(0,0,.5f));uv.Add(Vector2.one);
   for(int k=0;k<sides;k++){triangles.AddRange(new[]{tail,(k+1)%sides,k,nose,(z.Length-1)*sides+k,(z.Length-1)*sides+(k+1)%sides});}
   pressureHull=new Mesh{name="Original / closed sixteen-sided pressure shell"};pressureHull.SetVertices(vertices);pressureHull.SetUVs(0,uv);pressureHull.SetTriangles(triangles,0);pressureHull.RecalculateNormals();pressureHull.RecalculateBounds();return pressureHull;
  }
 }
}
