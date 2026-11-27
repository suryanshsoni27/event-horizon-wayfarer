using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Original, reusable surface art. All meshes/materials belong to this game instance.
public sealed partial class SurfaceArt : IDisposable {
    public sealed class CreatureView {
        public Transform root,torso,jaw;public HumanoidMotion humanoid;
        public Transform Muzzle{get{return humanoid==null?null:humanoid.muzzle;}}
        public void Aim(Vector3 target){if(humanoid!=null)humanoid.target=target;}public int kind;public SurfaceEcology.Species species;
        public readonly List<Transform> tail=new List<Transform>(),pincers=new List<Transform>();
        public readonly List<Transform> hips=new List<Transform>(),knees=new List<Transform>(),feet=new List<Transform>();
        public readonly List<Vector3> homes=new List<Vector3>(),plants=new List<Vector3>();public readonly List<bool> planted=new List<bool>();
        public readonly List<Renderer> renderers=new List<Renderer>();public System.Func<Vector3,Vector3> Contact;
        readonly MaterialPropertyBlock block=new MaterialPropertyBlock();float phase,lastTime,strike,collapse;public float KneeFlex{get;private set;}
        public void Strike(){strike=1;if(humanoid!=null)humanoid.Kick();}public void Shift(Vector3 d){if(humanoid!=null)humanoid.Shift(d);for(int i=0;i<plants.Count;i++)plants[i]-=d;}
        public void Animate(float time,float speed,float windup,float hurt,bool dead){
            float dt=Mathf.Clamp(time-lastTime,0,.1f);lastTime=time;strike=Mathf.MoveTowards(strike,0,dt*4);collapse=Mathf.MoveTowards(collapse,dead?1:0,dt*2.2f);
            if(humanoid!=null){humanoid.Pose(this,time,dt,speed,windup,strike,collapse,hurt,dead);KneeFlex=humanoid.KneeFlex;ApplyDamage(time,hurt,dead);return;}
            if(!dead&&windup<=0)phase+=Mathf.Max(0,speed)*dt/(kind==2?2.7f:1.65f);
            float breathe=Mathf.Sin(time*2.3f)*.018f;torso.localPosition=new Vector3(0,breathe+windup*.11f-collapse*.50f,strike*.35f);
            torso.localRotation=Quaternion.Euler(-windup*17+strike*25,Mathf.Sin(time)*1.5f,collapse*83);
            if(jaw!=null)jaw.localRotation=Quaternion.Euler(windup*24+strike*38+Mathf.Sin(time*3)*2,0,0);
            for(int j=0;j<tail.Count;j++)tail[j].localRotation=Quaternion.Euler((species!=null&&species.biome==SurfaceEcology.Biome.Arid?32:5)+windup*26-strike*42,Mathf.Sin(time*1.8f-j*.6f)*9*(1-collapse),0);
            for(int j=0;j<pincers.Count;j++)pincers[j].localRotation=Quaternion.Euler(-windup*18+strike*25,(j==0?-1:1)*(18-windup*22+strike*32),0);
            KneeFlex=0;
            for(int i=0;i<hips.Count;i++){
                float offset=((i/2+i%2)%2)*.5f,t=Mathf.Repeat(phase+offset,1),swing=Mathf.Clamp01((t-.61f)/.39f);bool stance=t<.61f&&speed>.2f&&windup<=0&&!dead;
                float step=(stance?Mathf.Lerp(.27f,-.27f,t/.61f):Mathf.Lerp(-.27f,.27f,Mathf.SmoothStep(0,1,swing)))*Mathf.Clamp01(speed/.9f);
                Vector3 target=homes[i]+Vector3.forward*step;target.y+=Mathf.Sin(swing*Mathf.PI)*.25f*Mathf.Clamp01(speed);target.x*=1+windup*.14f;
                if(dead)target=Vector3.Lerp(target,new Vector3(target.x*.7f,-.55f,target.z*.65f),collapse);
                Vector3 world=root.TransformPoint(target);if(Contact!=null&&!dead){Vector3 grounded=Contact(world);world=grounded+root.up*Mathf.Sin(swing*Mathf.PI)*.26f*Mathf.Clamp01(speed);if(stance||speed<.2f){if(!planted[i]){plants[i]=grounded;planted[i]=true;}if(Vector3.Distance(plants[i],grounded)>1.1f*root.lossyScale.y)plants[i]=grounded;world=plants[i];}else planted[i]=false;}
                Vector3 pole=root.TransformDirection(new Vector3(homes[i].x>0?1:-1,.15f,.12f));JointMotion.Solve(root,hips[i],knees[i],feet[i],world,pole,.72f,.83f);KneeFlex=Mathf.Max(KneeFlex,Quaternion.Angle(hips[i].rotation,knees[i].rotation));
            }
            ApplyDamage(time,hurt,dead);
        }
        void ApplyDamage(float time,float hurt,bool dead){if(dead&&!wasDead)deathTime=time;wasDead=dead;block.SetFloat("_Hurt",hurt*3);block.SetFloat("_Dissolve",dead?Mathf.Clamp01((time-deathTime-3)/1.5f):0);foreach(var renderer in renderers)if(renderer!=null)renderer.SetPropertyBlock(block);
        }
        float deathTime;bool wasDead;
    }

    public sealed partial class Geometry {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<int> triangles = new List<int>();
        public readonly List<Color> colors = new List<Color>();
        public readonly List<Vector3> normals = new List<Vector3>();
        public void Ellipsoid(Vector3 center, Vector3 size, Color color, int sides = 22, int rings = 14, Quaternion rotation = default(Quaternion)) {
            if (rotation == default(Quaternion)) rotation = Quaternion.identity;
            int start = vertices.Count;
            for (int y = 0; y <= rings; y++) for (int x = 0; x <= sides; x++) {
                float phi = y * Mathf.PI / rings, theta = x * Mathf.PI * 2 / sides;
                Vector3 d = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                float rib = 1 + .025f * Mathf.Sin(theta * 6) * Mathf.Sin(phi * 3);
                vertices.Add(center + rotation * Vector3.Scale(d * rib, size)); colors.Add(color); uv.Add(new Vector2(x/(float)sides,y/(float)rings));
                normals.Add(rotation * new Vector3(d.x/Mathf.Max(.001f,size.x),d.y/Mathf.Max(.001f,size.y),d.z/Mathf.Max(.001f,size.z)).normalized);
                if (y < rings && x < sides) {
                    int i = start + y * (sides + 1) + x;
                    triangles.AddRange(new[] { i, i + 1, i + sides + 1, i + 1, i + sides + 2, i + sides + 1 });
                }
            }
        }
        public void Horn(Vector3 start, Vector3 end, float radius, Color color, Vector3 bend, int sides = 12, int rings = 7) {
            int first = vertices.Count; Vector3 forward = (end - start).normalized;
            Vector3 right = Vector3.Cross(forward, Mathf.Abs(forward.y) > .9f ? Vector3.right : Vector3.up).normalized, up = Vector3.Cross(forward, right);
            for (int r = 0; r <= rings; r++) for (int a = 0; a <= sides; a++) {
                float t = r / (float)rings, angle = a * Mathf.PI * 2 / sides;
                vertices.Add(Vector3.Lerp(start, end, t) + bend * Mathf.Sin(t * Mathf.PI) + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius * Mathf.Pow(1 - t, .7f));
                colors.Add(color);uv.Add(new Vector2(a/(float)sides,t));
                Vector3 tangent=end-start+bend*(Mathf.PI*Mathf.Cos(t*Mathf.PI));
                Vector3 outward=right*Mathf.Cos(angle)+up*Mathf.Sin(angle);
                normals.Add((outward+forward*(radius/Mathf.Max(.01f,tangent.magnitude))*.7f).normalized);
                if (r < rings && a < sides) { int i = first + r * (sides + 1) + a; triangles.AddRange(new[] { i, i + 1, i + sides + 1, i + 1, i + sides + 2, i + sides + 1 }); }
            }
        }
        public void Blade(Vector3 center, Vector3 right, Vector3 up, float width, float length, Color color) {
            int i = vertices.Count;
            vertices.AddRange(new[] { center - right * width, center + right * width, center + up * length + right * width * .2f, center + up * length - right * width * .2f });
            colors.AddRange(new[] { color, color, color, color });uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
            Vector3 n=Vector3.Cross(up,right).normalized;normals.AddRange(new[]{n,n,n,n});
            triangles.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
        }
        public Mesh Mesh(string name) {
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetColors(colors); if(uv.Count==vertices.Count)mesh.SetUVs(0,uv); mesh.SetTriangles(triangles, 0); if(normals.Count==vertices.Count)mesh.SetNormals(normals);else mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }

    readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
    readonly Dictionary<int,Mesh> crystalMeshes=new Dictionary<int,Mesh>(),nestMeshes=new Dictionary<int,Mesh>();
    readonly Dictionary<Color,Material> beamMaterials = new Dictionary<Color,Material>();
    public Material BeamMaterial(Color color) {if(beamMaterials.TryGetValue(color,out var value))return value;value=Own(new Material(Shader.Find("Orbit/Glow")));value.SetColor("_Color",color);beamMaterials[color]=value;return value;}
    public readonly Material chitin, vegetation, metal, ceramic, dark, cyan, amber, violet;
    public SurfaceArt() {
        chitin = Own(new Material(Shader.Find("Orbit/XenoChitin")));
        vegetation = Own(new Material(Shader.Find("Orbit/AlienFlora")));
        chitin.SetTexture("_AlbedoMap",Resources.Load<Texture2D>("Surfaces/xeno_scutes_v1"));
        vegetation.SetTexture("_LeafMap",Resources.Load<Texture2D>("Surfaces/alien_leaf_v1"));
        metal = Lit(new Color(.15f, .22f, .27f), .7f, .45f);
        ceramic = Lit(new Color(.65f, .69f, .65f), .35f, .4f);
        dark = Lit(new Color(.025f, .04f, .048f), .2f, .4f);
        cyan = Lit(new Color(.07f, .65f, .72f), .25f, .6f, true);
        amber = Lit(new Color(1, .24f, .045f), .3f, .55f, true);
        violet = Lit(new Color(.48f, .1f, .85f), .2f, .65f, true);
    }
    T Own<T>(T value) where T : UnityEngine.Object { owned.Add(value); return value; }
    Material Lit(Color color, float metallic, float smooth, bool glow = false) {
        var material = Own(new Material(Shader.Find("Universal Render Pipeline/Lit")));
        material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smooth);
        if (glow) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 3); }
        return material;
    }
    public GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material) {
        var obj = new GameObject(name); obj.transform.SetParent(parent, false);
        obj.layer=parent==null?0:parent.gameObject.layer;obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material; return obj;
    }
    public Transform Piece(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 size, Material material, Quaternion rotation = default(Quaternion)) {
        var obj = GameObject.CreatePrimitive(type); obj.name = name; UnityEngine.Object.Destroy(obj.GetComponent<Collider>());
        obj.transform.SetParent(parent, false);obj.layer=parent==null?0:parent.gameObject.layer; obj.transform.localPosition = position; obj.transform.localScale = size;
        obj.transform.localRotation = rotation == default(Quaternion) ? Quaternion.identity : rotation;
        if(type==PrimitiveType.Cube)obj.GetComponent<MeshFilter>().sharedMesh=ProductionGeometry.BeveledBox;
        obj.GetComponent<Renderer>().sharedMaterial = material; return obj.transform;
    }
    public Mesh Crystal(SurfaceEcology.Biome biome=SurfaceEcology.Biome.Temperate) {
        int key=(int)biome;if(crystalMeshes.TryGetValue(key,out var cached))return cached;
        var g = new Geometry(); Color c = biome==SurfaceEcology.Biome.Arid?new Color(.43f,.32f,.16f,.12f):biome==SurfaceEcology.Biome.Frozen?new Color(.40f,.62f,.70f,.12f):new Color(.22f,.38f,.29f,.12f);
        for (int i = 0; i < 7; i++) { float a = i * 2.39996f; Vector3 p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (i == 0 ? 0 : .8f); g.Horn(p, p + new Vector3(p.x * .3f, 1.2f + (i % 3) * .45f, p.z * .3f), .24f, c, Vector3.zero, 5, 2); }
        cached=Own(g.Mesh(biome+" mineral specimen / faceted cluster"));crystalMeshes[key]=cached;return cached;
    }
    public Transform Site(Transform parent, string type,SurfaceEcology.Biome biome=SurfaceEcology.Biome.Temperate) {
        var root = new GameObject(type).transform; root.SetParent(parent, false);
        if (type == "SPECIMEN") { MeshObject("Resonant alien mineral", root, Crystal(biome), chitin); Piece("Mineral pedestal", root, PrimitiveType.Sphere, Vector3.down * .15f, new Vector3(2.1f, .55f, 2), dark); }
        else if (type == "RECORDER") {
            Piece("Broken pressure hull", root, PrimitiveType.Cube, new Vector3(0, .9f, 0), new Vector3(3.2f, 1.5f, 4), metal, Quaternion.Euler(12, 25, -16));
            Piece("Fractured ceramic wing", root, PrimitiveType.Cube, new Vector3(2.7f, .4f, -.3f), new Vector3(4.2f, .16f, 2), ceramic, Quaternion.Euler(-9, -22, 12));
            Piece("Exposed engine casing", root, PrimitiveType.Cylinder, new Vector3(-1.4f, .5f, -2.1f), new Vector3(1, 1.5f, 1), dark, Quaternion.Euler(70, 12, 0));
            Piece("Encrypted flight recorder", root, PrimitiveType.Cube, new Vector3(0, .55f, 2.8f), new Vector3(.65f, .8f, .55f), amber);
            for (int i = 0; i < 4; i++) Piece("Wreckage spar", root, PrimitiveType.Cube, new Vector3(-2 + i * 1.4f, .18f, 3.6f + i % 2), new Vector3(.2f, .16f, 2.5f), metal, Quaternion.Euler(0, i * 31, 5));
        } else if (type == "RELAY") {
            Piece("Relay foundation", root, PrimitiveType.Cylinder, Vector3.up * .15f, new Vector3(3.2f, .16f, 3.2f), metal);
            Piece("Communications mast", root, PrimitiveType.Cylinder, Vector3.up * 2.2f, new Vector3(.35f, 2.2f, .35f), ceramic);
            Piece("Antenna dish", root, PrimitiveType.Sphere, new Vector3(0, 4.1f, 0), new Vector3(2.5f, .25f, 2.5f), metal, Quaternion.Euler(25, 0, 0));
            Piece("Uplink transceiver", root, PrimitiveType.Cube, new Vector3(0, 1, 1.1f), new Vector3(1.1f, 1.4f, .35f), dark);
            Piece("Uplink status", root, PrimitiveType.Cube, new Vector3(0, 1.2f, 1.31f), new Vector3(.7f, .35f, .025f), cyan);
            for (int i = -1; i <= 1; i += 2) Piece("Solar fin", root, PrimitiveType.Cube, new Vector3(i * 2, 1.9f, 0), new Vector3(2.8f, .08f, 1.2f), dark, Quaternion.Euler(0, 0, i * -20));
        } else {
            if(nestMeshes.TryGetValue((int)biome,out var nestMesh)){MeshObject("Chitin spires and brood eggs",root,nestMesh,chitin);return root;}
            var nest = new Geometry();var guardian=SurfaceEcology.For(biome==SurfaceEcology.Biome.Arid?"S0P1":biome==SurfaceEcology.Biome.Frozen?"S0P2":"HOME",2);Color brood=guardian.organ;brood.a=.16f;
            for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6; Vector3 p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (3.5f + (i % 3) * .3f); nest.Horn(p, p * .8f + Vector3.up * (2.4f + (i % 4) * .6f), .46f, guardian.plate, -p * .15f); }
            for (int i = 0; i < 8; i++) { float a = i * 2.4f; nest.Ellipsoid(new Vector3(Mathf.Cos(a) * 2, .45f, Mathf.Sin(a) * 2), new Vector3(.65f, .9f, .65f), brood); }
            nestMesh=Own(nest.Mesh(biome+" guardian nest"));nestMeshes[(int)biome]=nestMesh;MeshObject("Chitin spires and brood eggs", root, nestMesh, chitin);
        }
        return root;
    }
    public Transform Rifle(Transform crew) {
        var root = new GameObject("PULSE CARBINE / animated muzzle").transform; root.SetParent(crew, false);root.gameObject.layer=crew.gameObject.layer;
        var receiver=Piece("Machined upper receiver",root,PrimitiveType.Cube,new Vector3(0,.025f,.055f),new Vector3(.12f,.13f,.48f),metal);
        receiver.GetComponent<MeshFilter>().sharedMesh=ProductionGeometry.PressureHull;
        Piece("Lower action housing",root,PrimitiveType.Cube,new Vector3(0,-.048f,.02f),new Vector3(.105f,.065f,.31f),dark);
        Piece("Shoulder stock frame",root,PrimitiveType.Cube,new Vector3(0,.005f,-.28f),new Vector3(.075f,.07f,.24f),metal);
        Piece("Rubber shoulder pad",root,PrimitiveType.Cube,new Vector3(0,-.014f,-.40f),new Vector3(.11f,.18f,.05f),dark);
        for(int sign=-1;sign<=1;sign+=2) {
            Piece("Vented handguard",root,PrimitiveType.Cube,new Vector3(sign*.055f,.014f,.39f),new Vector3(.025f,.105f,.35f),ceramic,Quaternion.Euler(0,0,-sign*12));
            for(int i=0;i<5;i++)Piece("Handguard cooling cutout",root,PrimitiveType.Cube,new Vector3(sign*.071f,.034f,.27f+i*.055f),new Vector3(.006f,.035f,.026f),dark);
        }
        Piece("Free floating barrel",root,PrimitiveType.Cylinder,new Vector3(0,.025f,.62f),new Vector3(.052f,.15f,.052f),dark,Quaternion.Euler(90,0,0));
        Piece("Muzzle brake",root,PrimitiveType.Cylinder,new Vector3(0,.025f,.78f),new Vector3(.074f,.04f,.074f),metal,Quaternion.Euler(90,0,0));
        Piece("Recessed barrel bore",root,PrimitiveType.Cylinder,new Vector3(0,.025f,.823f),new Vector3(.035f,.004f,.035f),dark,Quaternion.Euler(90,0,0));
        Piece("Top optic rail",root,PrimitiveType.Cube,new Vector3(0,.105f,.07f),new Vector3(.045f,.035f,.34f),dark);
        Piece("Reflex optic frame",root,PrimitiveType.Cube,new Vector3(0,.153f,.08f),new Vector3(.09f,.07f,.06f),metal);
        Piece("Reflex optic lens",root,PrimitiveType.Cube,new Vector3(0,.159f,.113f),new Vector3(.059f,.044f,.005f),cyan);
        Piece("Power indicator",root,PrimitiveType.Cube,new Vector3(.064f,.038f,-.045f),new Vector3(.008f,.018f,.075f),cyan);
        Piece("Charging bolt",root,PrimitiveType.Cube,new Vector3(.075f,.065f,-.06f),new Vector3(.035f,.03f,.07f),metal);
        Piece("Pistol grip",root,PrimitiveType.Cube,new Vector3(0,-.15f,-.10f),new Vector3(.075f,.18f,.095f),dark,Quaternion.Euler(-15,0,0));
        Piece("Trigger guard",root,PrimitiveType.Cube,new Vector3(0,-.105f,-.012f),new Vector3(.028f,.018f,.115f),metal);
        var cell=JointMotion.Joint("Exchangeable power magazine",root,new Vector3(0,-.115f,.12f));
        Piece("Power cell shell",cell,PrimitiveType.Cube,Vector3.down*.055f,new Vector3(.075f,.17f,.10f),dark,Quaternion.Euler(9,0,0));
        Piece("Power cell lock band",cell,PrimitiveType.Cube,Vector3.down*.025f,new Vector3(.078f,.025f,.104f),ceramic);
        return root;
    }
    public void Dispose() { foreach (var obj in owned) if (obj != null) UnityEngine.Object.Destroy(obj); owned.Clear(); }
}
