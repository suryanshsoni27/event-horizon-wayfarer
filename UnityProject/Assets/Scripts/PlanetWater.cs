using UnityEngine;
using UnityEngine.Rendering;

// Shallow seas sit above the existing solid basin floor. No save positions or native ABI change.
public sealed class PlanetWater : MonoBehaviour {
    public const float SeaLevel=.85f;
    public Mesh Surface{get;private set;}public Material Material{get;private set;}
    public static PlanetWater Attach(PlanetExpedition.Planet planet) {
        var o=new GameObject("COASTAL WATER / moving reflections and depth foam");o.transform.SetParent(planet.root.transform,false);
        var w=o.AddComponent<PlanetWater>();w.Build((float)planet.body.radius);return w;
    }
    void Build(float radius) {
        // This resolution bounds sphere chord error below 0.2 m on a 6 km planet.
        const int n=192;var verts=new Vector3[6*(n+1)*(n+1)];var normals=new Vector3[verts.Length];var triangles=new int[6*n*n*6];int vi=0,ti=0;
        for(int face=0;face<6;face++) {
            int first=vi;
            for(int y=0;y<=n;y++)for(int x=0;x<=n;x++){Vector3 d=PlanetTerrainLOD.Direction(face,x*2f/n-1,y*2f/n-1);verts[vi]=d*(radius+SeaLevel);normals[vi++]=d;if(x<n&&y<n){int a=first+y*(n+1)+x;triangles[ti++]=a;triangles[ti++]=a+1;triangles[ti++]=a+n+1;triangles[ti++]=a+1;triangles[ti++]=a+n+2;triangles[ti++]=a+n+1;}}
        }
        Surface=new Mesh{name="Continuous coastal sphere / 0.2 m chord budget",indexFormat=IndexFormat.UInt32};Surface.vertices=verts;Surface.normals=normals;Surface.triangles=triangles;Surface.RecalculateBounds();
        Material=new Material(Shader.Find("Orbit/CoastalWater"));gameObject.AddComponent<MeshFilter>().sharedMesh=Surface;var r=gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=Material;r.shadowCastingMode=ShadowCastingMode.Off;
    }
    void OnDestroy(){if(Surface!=null)Destroy(Surface);if(Material!=null)Destroy(Material);}
}
