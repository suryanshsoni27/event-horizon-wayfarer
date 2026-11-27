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
            // One sculpted axial skin surface runs from the pelvis through the neck and muzzle.
            // Width, depth and centre line are deliberately different for each climate.
            float w=desert?.78f:ice?.70f:.54f,h=desert?.33f:ice?.59f:.43f;
            if(boss){w*=1.22f;h*=1.12f;}
            g.Profile(new[]{
                new Vector4(-1.40f,.015f,.02f,-.04f),new Vector4(-1.18f,w*.65f,h*.69f,-.03f),
                new Vector4(-.65f,w,h,.02f),new Vector4(0,w*.96f,h*.94f,.02f),
                new Vector4(.45f,w*.70f,h*.76f,.03f),new Vector4(.73f,w*.54f,h*.65f,.02f),
                new Vector4(.97f,w*(ice?.70f:.60f),h*.65f,.04f),new Vector4(1.27f,w*.42f,h*.42f,-.05f),
                new Vector4(1.48f,.025f,.03f,-.08f)},shell,plate,44,72,desert?.035f:.018f);
            // Shingle plates follow the back curvature: shallow shells instead of bead-like ribs.
            int ribs=desert?7:5;
            for(int j=0;j<ribs;j++) {
                float z=-1.02f+j*(desert?.235f:.32f),f=Mathf.Sin((j+.8f)/(ribs+1)*Mathf.PI);
                float width=w*(.64f+.30f*f),y=.02f+h*(.70f+.08f*f);
                g.ArmorPlate(z,width,y,desert?.33f:.46f,plate);
                if(!desert||boss)for(int side=-1;side<=1;side+=2) {
                    Vector3 at=new Vector3(side*width*.82f,y+.03f,z);
                    g.Horn(at,at+new Vector3(side*.20f,.23f+(j%2)*.12f,-.42f),.065f,plate,new Vector3(side*.04f,.13f,0),14,10);
                }
            }
            for(int side=-1;side<=1;side+=2) {
                // Narrow eyes sit in a broad brow; tiny pupils carry the biological emission mask.
                Vector3 eye=new Vector3(side*w*.48f,.14f,1.14f);
                g.Ellipsoid(eye,new Vector3(.078f,.038f,.088f),joint,20,12,Quaternion.Euler(0,side*28,side*15));
                g.Ellipsoid(eye+new Vector3(side*.018f,.004f,.057f),new Vector3(.026f,.017f,.032f),organ,16,10);
                g.Tube(new Vector3(side*.08f,.22f,1.27f),new Vector3(side*w*.58f,.20f,.98f),.065f,.048f,Vector3.up*.055f,plate);
                g.Horn(new Vector3(side*.17f,-.07f,1.28f),new Vector3(side*.27f,-.32f,1.61f),ice?.10f:.052f,tooth,new Vector3(side*.12f,-.04f,.02f),18,12);
                if(ice)for(int j=0;j<6;j++) {
                    Vector3 p=new Vector3(side*(w*.72f),.18f,.52f-j*.23f);
                    g.Horn(p,p+new Vector3(side*.30f,.22f,-.25f),.10f,plate,new Vector3(0,.12f,0),16,10);
                }
                else if(!desert)g.Horn(new Vector3(side*.26f,.13f,1.08f),new Vector3(side*.57f,.31f,1.76f),.018f,plate,new Vector3(side*.12f,.12f,0),12,12);
                // Small lateral breathing slits are sunken, dark and non-emissive.
                for(int j=0;j<4;j++)g.Tube(new Vector3(side*w*.94f,-.01f,-.42f+j*.16f),new Vector3(side*w*.83f,-.15f,-.38f+j*.16f),.016f,.012f,Vector3.zero,joint,8,3);
            }
            if(ranged&&!desert&&!ice)for(int j=0;j<5;j++) {
                float a=j*2.39996f;Vector3 p=new Vector3(Mathf.Cos(a)*.39f,.52f+(j%2)*.16f,-.35f+Mathf.Sin(a)*.42f);
                g.Tube(p-Vector3.up*.22f,p+Vector3.up*.25f,.085f,.11f,Vector3.right*.035f,shell);
                // Flared fruiting bodies with a thin rim and radial gills.
                g.Ellipsoid(p+Vector3.up*.27f,new Vector3(.40f,.10f,.34f),plate,32,12);
                for(int k=0;k<12;k++){float b=k*Mathf.PI/6;g.Tube(p+Vector3.up*.19f,p+new Vector3(Mathf.Cos(b)*.33f,.20f,Mathf.Sin(b)*.28f),.008f,.014f,Vector3.zero,joint,6,3);}
            }
            else if(ranged)for(int j=0;j<(ice?7:5);j++)for(int side=-1;side<=1;side+=2) {
                Vector3 p=new Vector3(side*(ice?.22f:.44f),h*.85f,-.89f+j*.25f);
                g.Horn(p,p+new Vector3(side*.18f,ice?.72f+(j%3)*.13f:.45f,-.28f),ice?.11f:.14f,plate,Vector3.zero,ice?6:5,5);
                g.Tube(p+Vector3.up*.04f,p+Vector3.up*.18f,.038f,.022f,Vector3.zero,organ,12,4);
            }
            if(boss) {
                g.Ellipsoid(new Vector3(0,-.12f,1.03f),new Vector3(.19f,.09f,.14f),organ,24,12);
                for(int side=-1;side<=1;side+=2) {
                    Vector3 p=new Vector3(side*.34f,.29f,.73f),end=new Vector3(side*(desert?.93f:.68f),ice?1.25f:1.05f,1.07f);
                    g.Horn(p,end,.15f,plate,new Vector3(side*.18f,.16f,-.22f),22,16);
                    if(ice)g.Horn(Vector3.Lerp(p,end,.64f),end+new Vector3(side*.32f,.05f,-.4f),.09f,tooth,Vector3.zero,16,10);
                    if(desert)g.Horn(new Vector3(side*.4f,-.04f,.99f),new Vector3(side*.51f,-.28f,1.87f),.12f,tooth,new Vector3(side*.19f,.15f,.03f),20,14);
                }
            }
        } else if(part==1) {
            float w=ice?.18f:desert?.105f:.13f;
            g.Tube(new Vector3(0,.045f,0),new Vector3(0,-.74f,0),w*.75f,w*.49f,new Vector3(.035f,0,.08f),shell,24,18);
            g.Tube(new Vector3(0,-.07f,.04f),new Vector3(0,-.55f,.04f),w,w*.51f,Vector3.forward*.035f,plate,22,14);
            for(int j=0;j<3;j++)g.Tube(new Vector3(-w*.7f,-.61f-j*.03f,0),new Vector3(w*.7f,-.61f-j*.03f,0),.013f,.013f,Vector3.back*.02f,joint,10,3);
        } else if(part==2) {
            g.Tube(Vector3.zero,new Vector3(0,-.84f,0),ice?.14f:.09f,.036f,new Vector3(.045f,0,-.055f),shell,24,18);
            g.Horn(new Vector3(0,-.09f,.035f),new Vector3(0,-.60f,.01f),ice?.13f:.09f,plate,new Vector3(0,0,.07f),20,16);
        } else if(part==3) {
            g.Ellipsoid(new Vector3(0,.01f,.07f),new Vector3(ice?.18f:.12f,.055f,.17f),shell,22,12);
            for(int j=-1;j<=1;j++)g.Horn(new Vector3(j*.075f,0,.13f),new Vector3(j*.14f,-.03f,ice?.46f:.35f),.035f,tooth,Vector3.down*.04f,16,12);
        } else if(part==4) {
            g.Profile(new[]{new Vector4(-.04f,.03f,.02f,0),new Vector4(.14f,.24f,.065f,-.02f),new Vector4(.37f,.18f,.055f,0),new Vector4(.49f,.025f,.02f,.03f)},shell,plate,28,24,.01f);
            for(int j=-3;j<=3;j++)g.Horn(new Vector3(j*.06f,.03f,.27f),new Vector3(j*.055f,.11f,.33f),.018f,tooth,Vector3.zero,12,6);
        } else if(part==5) {
            g.Tube(Vector3.forward*.04f,new Vector3(0,0,-.49f),.14f,.105f,Vector3.up*.035f,plate,24,16);
            if(desert)g.Horn(new Vector3(0,0,-.43f),new Vector3(0,-.35f,-.68f),.075f,tooth,Vector3.forward*.10f,18,12);
        } else {
            g.Tube(Vector3.zero,new Vector3(.1f,-.07f,.67f),.115f,.14f,Vector3.left*.12f,plate,24,18);
            g.Horn(new Vector3(.1f,-.07f,.62f),new Vector3(.22f,-.04f,1.09f),.11f,plate,Vector3.left*.18f,22,16);
            g.Horn(new Vector3(.12f,-.07f,.62f),new Vector3(-.10f,-.04f,1.03f),.065f,tooth,Vector3.right*.10f,18,14);
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
