using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Suit design is original. Anatomical topology and the common rig are Quaternius CC0.
// Rigid equipment follows bones; the pressure garment, face and fingers are skinned.
public sealed class SkinnedCharacterArt : MonoBehaviour {
    readonly List<Object> owned = new List<Object>();
    static readonly Dictionary<Mesh, Mesh> garments = new Dictionary<Mesh, Mesh>();
    public Animator Animator { get; private set; }
    public Transform Model { get; private set; }
    public Transform HeadSocket { get; private set; }
    public Transform ChestSocket { get; private set; }
    public float Scale { get; private set; }
    public int SkinVertices { get; private set; }
    public bool Helmet { get; private set; }

    Material Suit(Color c, float metal = .1f, bool fabric = false) {
        var m = ProductionMaterials.Create(c, metal, fabric ? .28f : .43f, 0, 0, fabric);
        owned.Add(m); return m;
    }
    Material Skin(bool female, Color tint) {
        var l = PerformanceLibrary.Current; var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.name = "Anatomical skin / authored albedo and tangent normals";
        m.SetTexture("_BaseMap", female ? l.skinFemale : l.skinMale); m.SetColor("_BaseColor", tint);
        m.SetTexture("_BumpMap", female ? l.normalFemale : l.normalMale);
        m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", .65f); m.SetFloat("_Smoothness", .28f);
        owned.Add(m); return m;
    }
    public static SkinnedCharacterArt Create(Transform actor, bool crew, bool female, bool helmet, Color fabric, Color plate, Color skin, int role) {
        var a = actor.gameObject.AddComponent<SkinnedCharacterArt>();
        a.Build(crew, female, helmet, fabric, plate, skin, role); return a;
    }
    void Build(bool crew, bool female, bool helmet, Color fabricColor, Color plateColor, Color skinColor, int role) {
        Helmet = helmet; Scale = crew ? .90f : 1.12f;
        Model = Instantiate(female ? PerformanceLibrary.Current.female : PerformanceLibrary.Current.male, transform).transform;
        Model.name = "SKINNED PERFORMANCE / 65 bone Humanoid";
        Model.localPosition = Vector3.down * (crew ? .86f : 1.08f);
        Model.localRotation = Quaternion.Euler(0, crew ? 0 : 180, 0); Model.localScale = Vector3.one * Scale;
        Animator = Model.GetComponent<Animator>(); Animator.applyRootMotion = false;
        Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var fabric = Suit(fabricColor, .06f, true); var plate = Suit(plateColor, .48f);
        var dark = Suit(new Color(.018f, .027f, .035f), .25f);
        var accent = Suit(crew ? new Color(.94f, .24f, .045f) : Color.Lerp(plateColor, Color.white, .35f), .45f);
        var skin = Skin(female, skinColor);
        foreach (var r in Model.GetComponentsInChildren<SkinnedMeshRenderer>()) {
            r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
            r.quality = SkinQuality.Bone4; SkinVertices += r.sharedMesh.vertexCount;
            if (r.name.Contains("Eye") && !r.name.Contains("brow")) {
                var eyes = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                eyes.SetTexture("_BaseMap", PerformanceLibrary.Current.eyes); eyes.SetFloat("_Smoothness", .7f); owned.Add(eyes); r.sharedMaterial = eyes;
            } else if (r.name.Contains("brow")) r.sharedMaterial = dark;
            else { r.sharedMesh = Garment(r); r.sharedMaterials = new[]{fabric, skin, dark}; }
        }
        HeadSocket = Socket("Tracked helmet / portrait mount", HumanBodyBones.Head);
        ChestSocket = Socket("Load bearing chest harness", HumanBodyBones.UpperChest);
        if (ChestSocket == null) ChestSocket = Socket("Load bearing chest harness", HumanBodyBones.Chest);
        // Shaped overlapping ceramic plates, with flexible space left at shoulders and elbows.
        Plate("Split breastplate / port", ChestSocket, new Vector3(-.105f, -.015f, -.105f), new Vector3(.20f,.25f,.095f), plate, Quaternion.Euler(4, -8, -7));
        Plate("Split breastplate / starboard", ChestSocket, new Vector3(.105f, -.015f, -.105f), new Vector3(.20f,.25f,.095f), plate, Quaternion.Euler(4, 8, 7));
        Plate("Sternum orange identification inlay", ChestSocket, new Vector3(0,.015f,-.164f), new Vector3(.035f,.17f,.012f), accent);
        Plate("Abdominal floating plate", ChestSocket, new Vector3(0,-.205f,-.09f), new Vector3(.29f,.10f,.08f), plate, Quaternion.Euler(-9,0,0));
        Plate("Backpack composite housing", ChestSocket, new Vector3(0,-.01f,.19f), new Vector3(.31f,.39f,.15f), dark);
        for (int side = -1; side <= 1; side += 2) {
            Plate("Oxygen cylinder / machined rails", ChestSocket, new Vector3(side*.105f,-.02f,.29f), new Vector3(.075f,.31f,.075f), plate);
            Plate("Chest harness buckle", ChestSocket, new Vector3(side*.18f,-.13f,-.155f), new Vector3(.055f,.07f,.025f), dark);
        }
        for (int i = 0; i < 2; i++) {
            bool left = i == 0; float side = left ? 1 : -1;
            var shoulder = Socket("Floating shoulder cup", left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
            Plate("Shoulder ceramic shell", shoulder, new Vector3(side*.015f,-.015f,0), new Vector3(.18f,.18f,.26f), plate, Quaternion.Euler(0,0,side*12));
            var forearm = Socket("Forearm computer and bracer", left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            Plate("Forearm plate", forearm, new Vector3(side*.10f,0,.025f), new Vector3(.19f,.105f,.12f), plate);
            Plate("Suit safety band", forearm, new Vector3(side*.15f,0,.025f), new Vector3(.035f,.11f,.125f), accent);
            var thigh = Socket("Hip utility anchor", left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
            Plate("Thigh contour plate", thigh, new Vector3(0,-.17f,-.075f), new Vector3(.13f,.24f,.075f), plate, Quaternion.Euler(-6,0,side*3));
            Plate("Utility magazine pouch", thigh, new Vector3(side*.095f,-.08f,0), new Vector3(.09f,.17f,.11f), dark);
            var shin = Socket("Articulated knee and greave", left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
            Plate("Knee impact shield", shin, new Vector3(0,0,-.065f), new Vector3(.14f,.12f,.07f), plate, Quaternion.Euler(10,0,0));
            Plate("Shin shield", shin, new Vector3(0,-.18f,-.035f), new Vector3(.115f,.24f,.07f), plate, Quaternion.Euler(-5,0,0));
            var boot = Socket("Boot tread attachment", left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
            Plate("Boot sole", boot, new Vector3(0,-.06f,-.055f), new Vector3(.14f,.05f,.27f), dark);
            Plate("Toe cap", boot, new Vector3(0,-.01f,-.13f), new Vector3(.14f,.065f,.12f), plate, Quaternion.Euler(-8,0,0));
        }
        if (helmet) {
            // This sealed shell has a lip, ear hinges, crown rails and a recessed panoramic visor.
            Sphere("Pressure helmet / shell", HeadSocket, new Vector3(0,.055f,.008f), new Vector3(.39f,.45f,.39f), plate);
            Sphere("Visor rubber gasket", HeadSocket, new Vector3(0,.055f,-.13f), new Vector3(.342f,.305f,.20f), dark);
            var glass = Suit(new Color(.055f,.13f,.17f), .85f); glass.SetFloat("_Smoothness", .92f);
            Sphere("Panoramic polarized visor", HeadSocket, new Vector3(0,.065f,-.165f), new Vector3(.312f,.267f,.16f), glass);
            Plate("Helmet chin seal", HeadSocket, new Vector3(0,-.105f,-.09f), new Vector3(.29f,.045f,.25f), dark);
            for(int s=-1;s<=1;s+=2) {
                Plate("Helmet comms and lamp", HeadSocket, new Vector3(s*.195f,.055f,-.015f), new Vector3(.055f,.13f,.16f), dark);
                Plate("Crown service rail", HeadSocket, new Vector3(s*.105f,.24f,.015f), new Vector3(.03f,.025f,.20f), accent);
            }
        } else {
            // Close cut hair follows the actual head; the face below uses the imported skin texture.
            var library=PerformanceLibrary.Current;
            var hair=Instantiate(female?library.hairFemale:library.hairMale,HeadSocket).transform;
            hair.name="Authored hairstyle / follows head bone";hair.localPosition=Vector3.down*(female?1.55f:1.60f);hair.localRotation=Quaternion.identity;hair.localScale=Vector3.one;
            var hairMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));owned.Add(hairMaterial);
            hairMaterial.SetTexture("_BaseMap",library.hairColor);hairMaterial.SetColor("_BaseColor",new Color(.19f,.22f,.24f));hairMaterial.SetFloat("_Smoothness",.28f);
            hairMaterial.SetTexture("_BumpMap",library.hairNormal);hairMaterial.EnableKeyword("_NORMALMAP");
            foreach(var r in hair.GetComponentsInChildren<Renderer>())r.sharedMaterial=hairMaterial;
            Plate("Temple communications implant", HeadSocket, new Vector3(.137f,.065f,.025f), new Vector3(.032f,.06f,.09f), plate);
        }
        foreach(var t in Model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
    }
    Transform Socket(string name, HumanBodyBones bone) {
        Transform b = Animator.GetBoneTransform(bone); if (b == null) return null;
        var t = new GameObject(name).transform; t.SetParent(b, false);
        // Neutral socket coordinates use the model's forward (-Z), rather than exporter bone axes.
        t.rotation = Model.rotation; t.localScale = Vector3.one; return t;
    }
    static Mesh Garment(SkinnedMeshRenderer renderer) {
        Mesh original = renderer.sharedMesh;
        if (garments.TryGetValue(original, out var cached)) return cached;
        var m = Instantiate(original); m.name = original.name + " / tailored pressure garment submeshes";
        var triangles = original.triangles; var w = original.boneWeights;
        var categories = new int[original.vertexCount];
        for (int i=0;i<categories.Length;i++) {
            var b=w[i]; float head=0,extremity=0;
            int[] indices={b.boneIndex0,b.boneIndex1,b.boneIndex2,b.boneIndex3}; float[] weights={b.weight0,b.weight1,b.weight2,b.weight3};
            for(int j=0;j<4;j++){string n=renderer.bones[indices[j]].name;if(n=="Head"||n=="neck_01")head+=weights[j];if(n.Contains("hand_")||n.Contains("foot_")||n.Contains("ball_")||n.Contains("thumb_")||n.Contains("index_")||n.Contains("middle_")||n.Contains("ring_")||n.Contains("pinky_"))extremity+=weights[j];}
            categories[i]=head>.7f?1:extremity>.7f?2:0;
        }
        var parts=new[]{new List<int>(),new List<int>(),new List<int>()};
        for(int i=0;i<triangles.Length;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2];int category=categories[a]==categories[b]&&categories[b]==categories[c]?categories[a]:0;parts[category].Add(a);parts[category].Add(b);parts[category].Add(c);}
        m.subMeshCount=3;for(int i=0;i<3;i++)m.SetTriangles(parts[i],i);garments[original]=m;return m;
    }
    void Plate(string name,Transform parent,Vector3 p,Vector3 s,Material m,Quaternion q=default(Quaternion)) {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;t.localScale=s;t.gameObject.layer=parent.gameObject.layer;
        if(q!=default(Quaternion))t.localRotation=q;
        var mesh=SculptedPlate;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=m;
    }
    static Mesh sculptedPlate;
    static Mesh SculptedPlate {
        get {
            if(sculptedPlate!=null)return sculptedPlate;
            // Lofted ceramic surface: tapered corners, a raised central ridge and a returned rim.
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            const int sides=32,rings=10;
            for(int ring=0;ring<=rings;ring++)for(int i=0;i<=sides;i++) {
                float a=i*Mathf.PI*2/sides,t=ring/(float)rings,r=Mathf.Sin(t*Mathf.PI);
                float x=Mathf.Sign(Mathf.Cos(a))*Mathf.Pow(Mathf.Abs(Mathf.Cos(a)),.44f)*r*.5f;
                float y=Mathf.Sign(Mathf.Sin(a))*Mathf.Pow(Mathf.Abs(Mathf.Sin(a)),.62f)*r*.5f;
                x*=.85f+.15f*(y+.5f);float z=Mathf.Cos(t*Mathf.PI)*.5f;
                if(z<0)z-=.10f*r*(1-Mathf.Abs(x)*1.3f);
                vertices.Add(new Vector3(x,y,z));uv.Add(new Vector2(x+.5f,y+.5f));
                if(ring<rings&&i<sides){int k=ring*(sides+1)+i;triangles.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});}
            }
            sculptedPlate=new Mesh{name="Original / lofted tapered ceramic armor with rolled rim"};sculptedPlate.SetVertices(vertices);sculptedPlate.SetUVs(0,uv);sculptedPlate.SetTriangles(triangles,0);sculptedPlate.RecalculateNormals();sculptedPlate.RecalculateBounds();return sculptedPlate;
        }
    }
    void Sphere(string name,Transform parent,Vector3 p,Vector3 s,Material m) {JointMotion.Piece(name,parent,PrimitiveType.Sphere,p,s,m);}
    void OnDestroy(){foreach(var o in owned)if(o!=null)Destroy(o);}
}
