using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Original, reusable surface art. All meshes/materials belong to this game instance.
public sealed class SurfaceArt : IDisposable {
    public sealed class CreatureView {
        public Transform root,torso,jaw;public int kind;
        public readonly List<Transform> hips=new List<Transform>(),knees=new List<Transform>(),feet=new List<Transform>();
        public readonly List<Vector3> homes=new List<Vector3>(),plants=new List<Vector3>();public readonly List<bool> planted=new List<bool>();
        public readonly List<Renderer> renderers=new List<Renderer>();public System.Func<Vector3,Vector3> Contact;
        readonly MaterialPropertyBlock block=new MaterialPropertyBlock();float phase,lastTime,strike,collapse;public float KneeFlex{get;private set;}
        public void Strike(){strike=1;}public void Shift(Vector3 d){for(int i=0;i<plants.Count;i++)plants[i]-=d;}
        public void Animate(float time,float speed,float windup,float hurt,bool dead){
            float dt=Mathf.Clamp(time-lastTime,0,.1f);lastTime=time;strike=Mathf.MoveTowards(strike,0,dt*4);collapse=Mathf.MoveTowards(collapse,dead?1:0,dt*2.2f);
            if(!dead&&windup<=0)phase+=Mathf.Max(0,speed)*dt/(kind==2?2.7f:1.65f);
            float breathe=Mathf.Sin(time*2.3f)*.018f;torso.localPosition=new Vector3(0,breathe+windup*.11f-collapse*.50f,strike*.35f);
            torso.localRotation=Quaternion.Euler(-windup*17+strike*25,Mathf.Sin(time)*1.5f,collapse*83);
            if(jaw!=null)jaw.localRotation=Quaternion.Euler(windup*24+strike*38+Mathf.Sin(time*3)*2,0,0);
            KneeFlex=0;
            for(int i=0;i<hips.Count;i++){
                float offset=((i/2+i%2)%2)*.5f,t=Mathf.Repeat(phase+offset,1),swing=Mathf.Clamp01((t-.61f)/.39f);bool stance=t<.61f&&speed>.2f&&windup<=0&&!dead;
                float step=(stance?Mathf.Lerp(.27f,-.27f,t/.61f):Mathf.Lerp(-.27f,.27f,Mathf.SmoothStep(0,1,swing)))*Mathf.Clamp01(speed/.9f);
                Vector3 target=homes[i]+Vector3.forward*step;target.y+=Mathf.Sin(swing*Mathf.PI)*.25f*Mathf.Clamp01(speed);target.x*=1+windup*.14f;
                if(dead)target=Vector3.Lerp(target,new Vector3(target.x*.7f,-.55f,target.z*.65f),collapse);
                Vector3 world=root.TransformPoint(target);if(Contact!=null&&!dead){Vector3 grounded=Contact(world);world=grounded+root.up*Mathf.Sin(swing*Mathf.PI)*.26f*Mathf.Clamp01(speed);if(stance||speed<.2f){if(!planted[i]){plants[i]=grounded;planted[i]=true;}if(Vector3.Distance(plants[i],grounded)>1.1f*root.lossyScale.y)plants[i]=grounded;world=plants[i];}else planted[i]=false;}
                Vector3 pole=root.TransformDirection(new Vector3(homes[i].x>0?1:-1,.15f,.12f));JointMotion.Solve(root,hips[i],knees[i],feet[i],world,pole,.72f,.83f);KneeFlex=Mathf.Max(KneeFlex,Quaternion.Angle(hips[i].rotation,knees[i].rotation));
            }
            if(dead&&!wasDead)deathTime=time;wasDead=dead;block.SetFloat("_Hurt",hurt*3);block.SetFloat("_Dissolve",dead?Mathf.Clamp01((time-deathTime-3)/1.5f):0);foreach(var renderer in renderers)if(renderer!=null)renderer.SetPropertyBlock(block);
        }
        float deathTime;bool wasDead;
    }

    public sealed class Geometry {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<int> triangles = new List<int>();
        public readonly List<Color> colors = new List<Color>();
        public void Ellipsoid(Vector3 center, Vector3 size, Color color, int sides = 14, int rings = 10, Quaternion rotation = default(Quaternion)) {
            if (rotation == default(Quaternion)) rotation = Quaternion.identity;
            int start = vertices.Count;
            for (int y = 0; y <= rings; y++) for (int x = 0; x <= sides; x++) {
                float phi = y * Mathf.PI / rings, theta = x * Mathf.PI * 2 / sides;
                Vector3 d = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                float rib = 1 + .025f * Mathf.Sin(theta * 6) * Mathf.Sin(phi * 3);
                vertices.Add(center + rotation * Vector3.Scale(d * rib, size)); colors.Add(color);
                if (y < rings && x < sides) {
                    int i = start + y * (sides + 1) + x;
                    triangles.AddRange(new[] { i, i + 1, i + sides + 1, i + 1, i + sides + 2, i + sides + 1 });
                }
            }
        }
        public void Horn(Vector3 start, Vector3 end, float radius, Color color, Vector3 bend, int sides = 8, int rings = 5) {
            int first = vertices.Count; Vector3 forward = (end - start).normalized;
            Vector3 right = Vector3.Cross(forward, Mathf.Abs(forward.y) > .9f ? Vector3.right : Vector3.up).normalized, up = Vector3.Cross(forward, right);
            for (int r = 0; r <= rings; r++) for (int a = 0; a <= sides; a++) {
                float t = r / (float)rings, angle = a * Mathf.PI * 2 / sides;
                vertices.Add(Vector3.Lerp(start, end, t) + bend * Mathf.Sin(t * Mathf.PI) + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius * Mathf.Pow(1 - t, .7f));
                colors.Add(color);
                if (r < rings && a < sides) { int i = first + r * (sides + 1) + a; triangles.AddRange(new[] { i, i + 1, i + sides + 1, i + 1, i + sides + 2, i + sides + 1 }); }
            }
        }
        public void Blade(Vector3 center, Vector3 right, Vector3 up, float width, float length, Color color) {
            int i = vertices.Count;
            vertices.AddRange(new[] { center - right * width, center + right * width, center + up * length + right * width * .2f, center + up * length - right * width * .2f });
            colors.AddRange(new[] { color, color, new Color(color.r, color.g, color.b, 1), new Color(color.r, color.g, color.b, 1) });
            triangles.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
        }
        public Mesh Mesh(string name) {
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }

    readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
    readonly Mesh[] creatureBodies = new Mesh[3];
    Mesh legMesh, crystalMesh, nestMesh;
    readonly Dictionary<Color,Material> beamMaterials = new Dictionary<Color,Material>();
    public Material BeamMaterial(Color color) {if(beamMaterials.TryGetValue(color,out var value))return value;value=Own(new Material(Shader.Find("Orbit/Glow")));value.SetColor("_Color",color);beamMaterials[color]=value;return value;}
    public readonly Material chitin, vegetation, metal, ceramic, dark, cyan, amber, violet;
    public SurfaceArt() {
        chitin = Own(new Material(Shader.Find("Orbit/XenoChitin")));
        vegetation = Own(new Material(Shader.Find("Orbit/AlienFlora")));
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
        obj.GetComponent<Renderer>().sharedMaterial = material; return obj.transform;
    }
    Mesh Body(int kind) {
        if (creatureBodies[kind] != null) return creatureBodies[kind];
        var g = new Geometry(); bool spitter = kind == 1, boss = kind == 2;
        Color shell = boss ? new Color(.3f, .12f, .085f, 0) : spitter ? new Color(.17f, .12f, .29f, 0) : new Color(.075f, .18f, .16f, 0);
        Color plate = boss ? new Color(.46f, .21f, .095f, 0) : new Color(.15f, .3f, .27f, 0);
        Color eye = boss ? new Color(1, .28f, .025f, 1) : spitter ? new Color(.8f, .16f, 1, 1) : new Color(.08f, .85f, .7f, 1);
        g.Ellipsoid(new Vector3(0, .05f, -.18f), new Vector3(.7f, .52f, .95f), shell);
        g.Ellipsoid(new Vector3(0, .18f, .68f), new Vector3(.52f, .4f, .58f), plate);
        g.Ellipsoid(new Vector3(0, -.02f, 1.08f), new Vector3(.33f, .15f, .14f), new Color(.018f, .028f, .032f, 0));
        for (int side = -1; side <= 1; side += 2) {
            g.Ellipsoid(new Vector3(side * .26f, .33f, 1.06f), new Vector3(.105f, .09f, .085f), eye, 10, 7);
            g.Ellipsoid(new Vector3(side * .43f, .28f, .87f), new Vector3(.055f, .05f, .06f), eye, 8, 6);
            g.Horn(new Vector3(side * .27f, -.04f, 1), new Vector3(side * .13f, -.1f, 1.55f), .11f, plate, new Vector3(side * .25f, -.12f, .05f));
            for (int rib = 0; rib < 4; rib++) g.Ellipsoid(new Vector3(side * .44f, .28f, -.62f + rib * .34f), new Vector3(.32f, .18f, .23f), plate, 10, 7, Quaternion.Euler(0, side * 18, side * -22));
        }
        for (int i = 0; i < 5; i++) g.Horn(new Vector3(0, .43f, -.85f + i * .35f), new Vector3(0, .95f + (i % 2) * .17f, -1.07f + i * .35f), .16f, plate, Vector3.back * .08f);
        if (spitter) {
            g.Ellipsoid(new Vector3(0, .38f, -.5f), new Vector3(.72f, .65f, .65f), shell);
            for (int i = 0; i < 5; i++) { float a = i * Mathf.PI * 2 / 5; g.Ellipsoid(new Vector3(Mathf.Cos(a) * .51f, .6f + Mathf.Sin(a) * .3f, -.53f), new Vector3(.18f, .24f, .23f), eye, 10, 7); }
        }
        if (boss) { g.Ellipsoid(new Vector3(0, .69f, .28f), new Vector3(.25f, .2f, .28f), eye); g.Horn(new Vector3(0, .5f, .87f), new Vector3(0, 1.35f, 1.17f), .2f, plate, Vector3.back * .12f); }
        creatureBodies[kind] = Own(g.Mesh("Xeno carapace / mandibles / bioluminescent organs " + kind)); return creatureBodies[kind];
    }
    Mesh Leg() {
        if(legMesh!=null)return legMesh;var g=new Geometry();Color shell=new Color(.10f,.23f,.20f,0);
        g.Ellipsoid(new Vector3(0,-.34f,0),new Vector3(.13f,.38f,.14f),shell,12,8);g.Ellipsoid(Vector3.zero,new Vector3(.18f,.17f,.18f),new Color(.17f,.31f,.26f,0));
        legMesh=Own(g.Mesh("Jointed upper chitin limb"));return legMesh;
    }
    Mesh shinMesh,clawMesh,jawMesh;
    Mesh Shin(){if(shinMesh!=null)return shinMesh;var g=new Geometry();g.Horn(Vector3.zero,new Vector3(0,-.83f,0),.13f,new Color(.12f,.25f,.22f,0),new Vector3(.09f,0,.04f));shinMesh=Own(g.Mesh("Tapered articulated lower leg"));return shinMesh;}
    Mesh Claw(){if(clawMesh!=null)return clawMesh;var g=new Geometry();for(int i=-1;i<=1;i++)g.Horn(new Vector3(i*.055f,0,0),new Vector3(i*.11f,-.02f,.33f),.045f,new Color(.25f,.37f,.28f,0),Vector3.down*.04f);clawMesh=Own(g.Mesh("Three grounded talons"));return clawMesh;}
    Mesh Jaw(){if(jawMesh!=null)return jawMesh;var g=new Geometry();g.Ellipsoid(Vector3.forward*.15f,new Vector3(.26f,.09f,.27f),new Color(.06f,.10f,.08f,0));for(int i=-1;i<=1;i++)g.Horn(new Vector3(i*.15f,.03f,.3f),new Vector3(i*.13f,.17f,.34f),.035f,new Color(.6f,.67f,.42f,0),Vector3.zero);jawMesh=Own(g.Mesh("Hinged jaw and teeth"));return jawMesh;}
    public CreatureView Creature(Transform parent,int kind){
        var view=new CreatureView{kind=kind};view.root=new GameObject(kind==2?"BROOD WARDEN":kind==1?"SPORECASTER":"RIFT STALKER").transform;view.root.SetParent(parent,false);view.torso=JointMotion.Joint("Breathing and attack thorax",view.root,Vector3.zero);MeshObject("Organic segmented carapace",view.torso,Body(kind),chitin);view.jaw=JointMotion.Joint("Biting mandible hinge",view.torso,new Vector3(0,-.13f,1.02f));MeshObject("Opening jaw",view.jaw,Jaw(),chitin);
        int pairs=kind==1?3:2;
        for(int i=0;i<pairs;i++)for(int side=-1;side<=1;side+=2){float z=.62f-i*(pairs==3?.62f:1.25f);var hip=JointMotion.Joint("Leg hip / IK",view.root,new Vector3(side*.58f,0,z));MeshObject("Upper chitin",hip,Leg(),chitin);var knee=JointMotion.Joint("Leg knee / IK",hip,Vector3.down*.72f);MeshObject("Tapered shin",knee,Shin(),chitin);var foot=JointMotion.Joint("Ground planted talons",knee,Vector3.down*.83f);MeshObject("Claws",foot,Claw(),chitin);view.hips.Add(hip);view.knees.Add(knee);view.feet.Add(foot);view.homes.Add(new Vector3(side*1.12f,-1.08f,z+.08f));view.plants.Add(Vector3.zero);view.planted.Add(false);}
        view.renderers.AddRange(view.root.GetComponentsInChildren<Renderer>());return view;
    }
    public Mesh Crystal() {
        if (crystalMesh != null) return crystalMesh;
        var g = new Geometry(); Color c = new Color(.08f, .6f, .75f, 1);
        for (int i = 0; i < 7; i++) { float a = i * 2.39996f; Vector3 p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (i == 0 ? 0 : .8f); g.Horn(p, p + new Vector3(p.x * .3f, 1.2f + (i % 3) * .45f, p.z * .3f), .24f, c, Vector3.zero, 5, 2); }
        crystalMesh = Own(g.Mesh("Alien mineral specimen / faceted cluster")); return crystalMesh;
    }
    public Transform Site(Transform parent, string type) {
        var root = new GameObject(type).transform; root.SetParent(parent, false);
        if (type == "SPECIMEN") { MeshObject("Resonant alien mineral", root, Crystal(), chitin); Piece("Mineral pedestal", root, PrimitiveType.Sphere, Vector3.down * .15f, new Vector3(2.1f, .55f, 2), dark); }
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
            if(nestMesh!=null){MeshObject("Chitin spires and brood eggs",root,nestMesh,chitin);return root;}
            var nest = new Geometry();
            for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6; Vector3 p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (3.5f + (i % 3) * .3f); nest.Horn(p, p * .8f + Vector3.up * (2.4f + (i % 4) * .6f), .46f, new Color(.18f, .12f, .22f, 0), -p * .15f); }
            for (int i = 0; i < 8; i++) { float a = i * 2.4f; nest.Ellipsoid(new Vector3(Mathf.Cos(a) * 2, .45f, Mathf.Sin(a) * 2), new Vector3(.65f, .9f, .65f), new Color(.5f, .12f, .7f, .8f)); }
            nestMesh=Own(nest.Mesh("Organic guardian nest"));MeshObject("Chitin spires and brood eggs", root, nestMesh, chitin);
        }
        return root;
    }
    public Transform Rifle(Transform crew) {
        var root = new GameObject("PULSE CARBINE / animated muzzle").transform; root.SetParent(crew, false);root.gameObject.layer=crew.gameObject.layer;
        Piece("Receiver", root, PrimitiveType.Cube, Vector3.zero, new Vector3(.15f, .23f, .62f), metal);
        Piece("Ceramic barrel shroud", root, PrimitiveType.Cube, new Vector3(0, .02f, .39f), new Vector3(.19f, .17f, .44f), ceramic);
        Piece("Barrel", root, PrimitiveType.Cylinder, new Vector3(0, .025f, .68f), new Vector3(.1f, .11f, .1f), dark, Quaternion.Euler(90, 0, 0));
        Piece("Battery cell", root, PrimitiveType.Cube, new Vector3(.09f, .03f, .08f), new Vector3(.03f, .09f, .35f), cyan);
        Piece("Grip", root, PrimitiveType.Cube, new Vector3(0, -.18f, -.12f), new Vector3(.09f, .24f, .13f), dark, Quaternion.Euler(-15, 0, 0)); return root;
    }
    public void Dispose() { foreach (var obj in owned) if (obj != null) UnityEngine.Object.Destroy(obj); owned.Clear(); }
}
