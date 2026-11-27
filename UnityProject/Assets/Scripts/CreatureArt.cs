using System.Collections.Generic;
using UnityEngine;

public sealed partial class SurfaceArt {
    readonly Dictionary<int,Mesh> faunaMeshes=new Dictionary<int,Mesh>();
    Mesh FaunaMesh(SurfaceEcology.Species s,int part) {
        int key=s.Id*10+part;
        if(faunaMeshes.TryGetValue(key,out var cached))return cached;
        var g=new Geometry();Color shell=s.shell,plate=s.plate,organ=s.organ;
        Color joint=Color.Lerp(shell,Color.black,.32f);joint.a=0;
        Color tooth=new Color(.61f,.55f,.36f,0);
        bool desert=s.biome==SurfaceEcology.Biome.Arid,ice=s.biome==SurfaceEcology.Biome.Frozen,boss=s.role==2,ranged=s.role==1;
        if(ice)tooth=new Color(.69f,.76f,.75f,0);
        if(part==0) {
            // Separate thorax, neck, rib armor and pelvis, rather than one spherical primitive.
            Vector3 trunk=desert?new Vector3(.78f,.36f,1.08f):ice?new Vector3(.63f,.56f,.98f):new Vector3(.53f,.42f,1.08f);
            if(boss)trunk.x*=1.24f;
            g.Ellipsoid(new Vector3(0,.02f,-.23f),trunk,shell);
            g.Ellipsoid(new Vector3(0,-.14f,.63f),new Vector3(.34f,.28f,.53f),joint);
            g.Ellipsoid(new Vector3(0,.08f,.86f),new Vector3(ice?.50f:.39f,ice?.36f:.28f,.47f),plate);
            g.Ellipsoid(new Vector3(0,-.12f,1.18f),new Vector3(.27f,.09f,.24f),joint);
            // Armored ribs overlap like real chitin. Narrow gaps retain dark, soft undersides.
            int ribs=desert?7:5;
            for(int j=0;j<ribs;j++) {
                float z=-1.02f+j*(desert?.26f:.37f),w=trunk.x*(.72f+.23f*Mathf.Sin((j+.5f)/ribs*Mathf.PI));
                g.Ellipsoid(new Vector3(0,.16f+trunk.y*.7f,z),new Vector3(w,.13f,.21f),plate,22,10);
                for(int side=-1;side<=1;side+=2) {
                    g.Ellipsoid(new Vector3(side*w*.85f,.06f,z+.04f),new Vector3(.14f,.22f,.23f),plate,16,10,Quaternion.Euler(0,side*12,side*25));
                    if(!desert||boss)g.Horn(new Vector3(side*w*.76f,trunk.y*.6f,z),new Vector3(side*(w+.2f),.65f+(j%2)*.14f,z-.23f),.085f,plate,new Vector3(side*.08f,.06f,0));
                }
            }
            for(int side=-1;side<=1;side+=2) {
                // Recessed eyes, tear ducts and nostrils, with restrained emission.
                g.Ellipsoid(new Vector3(side*.31f,.23f,1.07f),new Vector3(.10f,.065f,.11f),joint,16,10);
                g.Ellipsoid(new Vector3(side*.335f,.255f,1.15f),new Vector3(.055f,.028f,.045f),organ,16,10);
                g.Horn(new Vector3(side*.17f,-.05f,1.25f),new Vector3(side*.26f,-.30f,1.57f),ice?.1f:.06f,tooth,new Vector3(side*.13f,-.05f,.05f));
                if(ice) {
                    // Thick insulating collar and branching ice armor distinguish cryogenic fauna.
                    for(int j=0;j<5;j++)g.Horn(new Vector3(side*(.33f+j*.035f),.16f, .60f-j*.23f),new Vector3(side*(.70f+j*.04f),.47f,.36f-j*.27f),.12f,plate,new Vector3(0,.1f,0));
                } else if(!desert) {
                    g.Horn(new Vector3(side*.30f,.18f,1.10f),new Vector3(side*.68f,.30f,1.68f),.026f,plate,new Vector3(side*.13f,.18f,0));
                }
            }
            // Ranged species have a distinct functional organ and silhouette in each habitat.
            if(ranged&&!desert&&!ice) {
                for(int j=0;j<5;j++) {
                    float a=j*2.39996f;Vector3 p=new Vector3(Mathf.Cos(a)*.42f,.53f+(j%2)*.17f,-.35f+Mathf.Sin(a)*.42f);
                    g.Horn(p-Vector3.up*.24f,p+Vector3.up*.22f,.12f,shell,Vector3.zero);
                    g.Ellipsoid(p+Vector3.up*.22f,new Vector3(.38f,.14f,.33f),plate,24,10);
                    for(int k=0;k<5;k++){float b=k*Mathf.PI*.4f;g.Ellipsoid(p+new Vector3(Mathf.Cos(b)*.25f,.17f,Mathf.Sin(b)*.22f),new Vector3(.045f,.035f,.045f),organ,10,6);}
                }
            } else if(ranged&&desert) {
                for(int j=0;j<5;j++)for(int side=-1;side<=1;side+=2) {
                    Vector3 p=new Vector3(side*.44f,.38f,-.85f+j*.32f);
                    g.Horn(p,p+new Vector3(side*.14f,.42f,-.26f),.16f,plate,Vector3.zero,5,3);
                    g.Ellipsoid(p+Vector3.up*.08f,new Vector3(.055f,.045f,.13f),organ,12,8);
                }
            } else if(ranged&&ice) {
                for(int j=0;j<7;j++) {
                    Vector3 p=new Vector3((j%2==0?-1:1)*.21f,.39f,-.88f+j*.23f);
                    g.Horn(p,p+new Vector3(p.x*1.4f,.85f+(j%3)*.17f,-.21f),.13f,plate,Vector3.zero,6,3);
                    g.Ellipsoid(p+Vector3.up*.28f,new Vector3(.10f,.27f,.10f),organ,12,8);
                }
            }
            if(boss) {
                g.Ellipsoid(new Vector3(0,-.07f,.92f),new Vector3(.22f,.15f,.18f),organ,20,12);
                for(int side=-1;side<=1;side+=2) {
                    Vector3 p=new Vector3(side*.38f,.28f,.75f),end=new Vector3(side*(desert?.95f:.72f),ice?1.28f:1.1f,1.15f);
                    g.Horn(p,end,.18f,plate,new Vector3(side*.13f,.20f,-.2f));
                    if(ice)g.Horn(Vector3.Lerp(p,end,.65f),end+new Vector3(side*.38f,.05f,-.35f),.10f,tooth,Vector3.zero);
                    if(desert)g.Horn(new Vector3(side*.43f,-.07f,.95f),new Vector3(side*.55f,-.25f,1.85f),.15f,tooth,new Vector3(side*.2f,.12f,.05f));
                }
            }
        } else if(part==1) {
            float w=ice?.19f:desert?.11f:.14f;
            g.Ellipsoid(new Vector3(0,-.34f,0),new Vector3(w,.38f,w*.95f),shell,18,12);
            g.Ellipsoid(Vector3.zero,new Vector3(w*1.35f,w*1.15f,w*1.35f),joint,18,12);
            g.Ellipsoid(new Vector3(0,-.19f,.07f),new Vector3(w*.9f,.21f,w*.65f),plate,16,10);
        } else if(part==2) {
            g.Horn(Vector3.zero,new Vector3(0,-.83f,0),ice?.17f:.12f,shell,new Vector3(.07f,0,-.08f));
            g.Ellipsoid(new Vector3(0,-.13f,.03f),new Vector3(.13f,.20f,.12f),plate,16,10);
        } else if(part==3) {
            g.Ellipsoid(new Vector3(0,.015f,.08f),new Vector3(ice?.22f:.14f,.08f,.22f),shell,16,10);
            for(int j=-1;j<=1;j++)g.Horn(new Vector3(j*.09f,0,.13f),new Vector3(j*.17f,-.02f,ice?.50f:.38f),.05f,tooth,Vector3.down*.06f);
        } else if(part==4) {
            g.Ellipsoid(Vector3.forward*.17f,new Vector3(.26f,.08f,.28f),shell,18,10);
            for(int j=-2;j<=2;j++)g.Horn(new Vector3(j*.09f,.03f,.3f),new Vector3(j*.075f,.14f,.37f),.027f,tooth,Vector3.zero,10,4);
        } else if(part==5) {
            g.Ellipsoid(new Vector3(0,0,-.24f),new Vector3(.17f,.15f,.31f),plate,18,12);
            if(desert)g.Horn(new Vector3(0,0,-.45f),new Vector3(0,-.34f,-.68f),.1f,tooth,Vector3.forward*.08f);
        } else {
            g.Horn(Vector3.zero,new Vector3(.1f,-.07f,.75f),.14f,plate,Vector3.left*.14f);
            g.Horn(new Vector3(.1f,-.07f,.65f),new Vector3(.22f,-.04f,1.06f),.13f,plate,Vector3.left*.2f);
            g.Horn(new Vector3(.12f,-.07f,.64f),new Vector3(-.10f,-.04f,1.02f),.08f,tooth,Vector3.right*.1f);
        }
        cached=Own(g.Mesh(s.name+" / anatomical part "+part));faunaMeshes.Add(key,cached);return cached;
    }
    public CreatureView Creature(Transform parent,int kind) { return Creature(parent,SurfaceEcology.For("HOME",kind)); }
    public CreatureView Creature(Transform parent,SurfaceEcology.Species species) {
        int kind=species.role;bool desert=species.biome==SurfaceEcology.Biome.Arid,ice=species.biome==SurfaceEcology.Biome.Frozen;
        var view=new CreatureView{kind=kind,species=species};
        view.root=new GameObject(species.name).transform;view.root.SetParent(parent,false);
        view.torso=JointMotion.Joint("Breathing and attack thorax",view.root,Vector3.zero);
        MeshObject("Layered dermis and carapace",view.torso,FaunaMesh(species,0),chitin);
        view.jaw=JointMotion.Joint("Biting mandible hinge",view.torso,new Vector3(0,-.17f,1.02f));
        MeshObject("Hinged jaw / individual teeth",view.jaw,FaunaMesh(species,4),chitin);
        int pairs=desert?(kind==0?4:3):ice?2:kind==0?2:3;
        for(int j=0;j<pairs;j++)for(int side=-1;side<=1;side+=2) {
            float z=Mathf.Lerp(.62f,-.76f,j/(float)(pairs-1)),width=desert?.67f:ice?.54f:.53f;
            var hip=JointMotion.Joint("Leg hip / IK",view.root,new Vector3(side*width,0,z));
            MeshObject("Dermal upper limb",hip,FaunaMesh(species,1),chitin);
            var knee=JointMotion.Joint("Leg knee / IK",hip,Vector3.down*.72f);MeshObject("Armored lower limb",knee,FaunaMesh(species,2),chitin);
            var foot=JointMotion.Joint("Ground planted talons",knee,Vector3.down*.83f);MeshObject("Weight bearing foot",foot,FaunaMesh(species,3),chitin);
            view.hips.Add(hip);view.knees.Add(knee);view.feet.Add(foot);view.homes.Add(new Vector3(side*(desert?1.3f:1.07f),-1.08f,z+.12f));view.plants.Add(Vector3.zero);view.planted.Add(false);
        }
        if(desert||(!ice&&kind==0)) {
            Transform parentJoint=view.torso;
            for(int j=0;j<(desert?4:3);j++) {
                var t=JointMotion.Joint(desert?"Articulated stinger segment":"Counterbalance tail",parentJoint,j==0?new Vector3(0,.05f,-1):new Vector3(0,0,-.46f));
                MeshObject("Tapered caudal armor",t,FaunaMesh(species,5),chitin);t.localScale=Vector3.one*(1-j*.12f);view.tail.Add(t);parentJoint=t;
            }
        }
        if(desert&&kind!=1)for(int side=-1;side<=1;side+=2) {
            var p=JointMotion.Joint("Closing grasping pincer",view.torso,new Vector3(side*.63f,-.08f,.65f));
            var piece=MeshObject("Chitin chela",p,FaunaMesh(species,6),chitin);piece.transform.localScale=new Vector3(side,1,1);view.pincers.Add(p);
        }
        view.renderers.AddRange(view.root.GetComponentsInChildren<Renderer>());return view;
    }
}
