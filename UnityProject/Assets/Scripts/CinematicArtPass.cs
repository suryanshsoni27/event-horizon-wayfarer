using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed partial class DefenseGame {
    static int RootVertex(int[] parents,int v){while(parents[v]!=v){parents[v]=parents[parents[v]];v=parents[v];}return v;}
    static int LargestConnectedSurface(Mesh mesh) {
        var parent=new int[mesh.vertexCount];var size=new int[parent.Length];for(int i=0;i<parent.Length;i++){parent[i]=i;size[i]=1;}
        var indices=mesh.triangles;for(int i=0;i<indices.Length;i+=3)for(int j=1;j<3;j++){int a=RootVertex(parent,indices[i]),b=RootVertex(parent,indices[i+j]);if(a!=b){parent[b]=a;size[a]+=size[b];}}
        int largest=0;for(int i=0;i<size.Length;i++)if(parent[i]==i)largest=Mathf.Max(largest,size[i]);return largest;
    }
    static bool ValidArtNormals(Mesh mesh) {
        var vertices=mesh.vertices;var n=mesh.normals;var uv=mesh.uv;var indices=mesh.triangles;int good=0,total=0;
        if(uv.Length!=vertices.Length||n.Length!=vertices.Length)return false;
        for(int i=0;i<vertices.Length;i++)if(!VoyagerState.Finite(uv[i].x)||!VoyagerState.Finite(uv[i].y)||n[i].sqrMagnitude<.8f)return false;
        for(int i=0;i<indices.Length;i+=3){int a=indices[i],b=indices[i+1],c=indices[i+2];Vector3 face=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);if(face.sqrMagnitude<.000000001f)continue;total++;if(Vector3.Dot(face.normalized,(n[a]+n[b]+n[c]).normalized)>.15f)good++;}
        return total>100&&good>total*.96f;
    }
    bool CinematicSight(Vector3 target,Vector3 camera) {for(int j=1;j<=20;j++)if(expedition.Alt(Vector3.Lerp(target,camera,j/20f))<.24f)return false;return true;}
    void PlaceReviewCreatures(PlanetExpedition.Planet planet) {
        foreach(var foe in surface.Current.foes) {
            Vector3 point=surface.World(foe);
            for(int j=0;j<8;j++){point=SurfaceScatter.Current.Clearance(point,foe.kind==2?2.9f:1.8f);point=surface.GroundPoint(point,1.15f);}
            foe.local=point-planet.body.P;
        }
        surface.Step(.01f);
    }
    bool CreatureCameraSight(Vector3 center,Vector3 up,Vector3 forward,Vector3 side,Vector3 candidate,float radius) {
        float scale=radius>8?1.9f:1;
        return CinematicSight(center+up*.12f,candidate)&&CinematicSight(center+forward*1.4f*scale,candidate)&&CinematicSight(center-forward*.9f*scale,candidate)&&CinematicSight(center+side*.7f*scale,candidate)&&CinematicSight(center-side*.7f*scale,candidate);
    }
    void ClearCreatureCamera(Vector3 center,Vector3 up,Vector3 forward,Vector3 side,float radius,float orbit,float height) {
        Vector3 selected=center+forward*radius+up*height;bool found=false;
        for(int lift=0;lift<5&&!found;lift++)for(int angle=0;angle<24;angle++) {
            float offset=angle==0?0:((angle+1)/2)*15*(angle%2==1?1:-1);
            Vector3 dir=Quaternion.AngleAxis(orbit+offset,up)*(forward*.82f+side*.57f).normalized;
            Vector3 candidate=center+dir*radius+up*(height+lift*.75f);
            if(CreatureCameraSight(center,up,forward,side,candidate,radius)){selected=candidate;found=true;break;}
        }
        civilFilmAt=selected;civilFilmLook=center+up*.12f;
    }
    void LandscapeCamera(PlanetExpedition.Planet planet,float angle) {
        Vector3 up=(expedition.Focus-planet.body.P).normalized,forward=PlanetExpedition.SurfaceTangent(up,ShipForward),right=Vector3.Cross(up,forward);
        civilFilmLook=expedition.Focus+forward*13+up*4;
        civilFilmAt=expedition.Focus-forward*13+right*(9+angle)+up*6;
    }
    IEnumerator CinematicArtChecks() {
        yield return new WaitForSeconds(1);preferences.quality=2;ApplyPreferences();var checks=new Dictionary<string,bool>();
        for(int biome=0;biome<3;biome++) {
            var planet=StageEcology(biome);for(int j=0;j<125;j++)yield return null;PlaceReviewCreatures(planet);var scenery=GetComponent<SurfaceScenery>();string suffix=((SurfaceEcology.Biome)biome).ToString().ToLowerInvariant();
            checks["landscape_is_anchored_to_live_planet_"+suffix]=scenery.BoundPlanet==planet&&scenery.Landmarks>4;
            bool foliageNormals=true;
            Transform landscapeRoot=null;foreach(Transform child in planet.root.transform)if(child.name=="STREAMED LANDSCAPES / "+((SurfaceEcology.Biome)biome)){landscapeRoot=child;break;}if(landscapeRoot!=null)foreach(var part in landscapeRoot.GetComponentsInChildren<MeshFilter>())foliageNormals&=ValidArtNormals(part.sharedMesh);
            checks["scenery_surface_normals_face_the_visible_side_"+suffix]=landscapeRoot!=null&&foliageNormals;
            checks["landscape_geometry_and_contacts_are_bounded_"+suffix]=scenery.Vertices>1000&&scenery.Vertices<150000&&scenery.Contacts.Count==scenery.Landmarks*3&&scenery.Contacts.Count<=84;
            bool valid=true,continuous=true,bound=true;Material skin=null;
            foreach(var foe in surface.Current.foes) {
                var filter=foe.view.torso.GetComponentInChildren<MeshFilter>();continuous&=LargestConnectedSurface(filter.sharedMesh)>3000;
                foreach(var part in foe.view.root.GetComponentsInChildren<MeshFilter>())valid&=ValidArtNormals(part.sharedMesh);
                skin=filter.GetComponent<Renderer>().sharedMaterial;bound&=skin.GetTexture("_AlbedoMap")==Resources.Load<Texture2D>("Surfaces/xeno_scutes_v1");
            }
            checks["creature_has_continuous_body_and_head_surface_"+suffix]=continuous;
            checks["uvs_and_normals_match_actual_mesh_winding_"+suffix]=valid;
            checks["generated_skin_art_is_bound_to_actual_encounters_"+suffix]=bound;
            Vector3 at=planet.body.P+scenery.Contacts[1],up=(at-planet.body.P).normalized,t=PlanetExpedition.SurfaceTangent(up,ShipForward);
            var state=Make(at+t*.15f,Vector3.zero);PlanetExpedition.StepMode(ref state,Vector3.zero,.001f,2);
            checks["native_crew_contact_rejects_solid_landmark_"+suffix]=Vector3.Distance(Pos(state),at)>scenery.Radii[1]+1.13f;
            Vector3 clear=SurfaceScatter.Current.Clearance(at+t*.15f,.3f);
            checks["camera_clearance_uses_same_landmark_envelope_"+suffix]=Vector3.Distance(clear,at)>=scenery.Radii[1]+.299f;
            var locations=new List<Vector3>(scenery.Contacts);scenery.Refresh();yield return new WaitForSeconds(.9f);
            bool stable=locations.Count==scenery.Contacts.Count;for(int i=0;i<Mathf.Min(locations.Count,scenery.Contacts.Count);i++)stable&=Vector3.Distance(locations[i],scenery.Contacts[i])<.001f;
            checks["scenery_rebuild_preserves_planet_relative_placement_"+suffix]=stable;
            EcologyCamera(planet,0,5);checks["review_camera_has_clear_sight_to_creature_"+suffix]=CinematicSight(civilFilmLook,civilFilmAt);for(int j=0;j<12;j++)yield return null;yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"cinematic-"+suffix+"-creature.png"));
            Time.timeScale=0;yield return new WaitForEndOfFrame();var detailed=ScreenCapture.CaptureScreenshotAsTexture();var original=skin.GetTexture("_AlbedoMap");skin.SetTexture("_AlbedoMap",Texture2D.blackTexture);
            yield return new WaitForEndOfFrame();var black=ScreenCapture.CaptureScreenshotAsTexture();float difference=0;int pixels=0;
            for(int y=3;y<9;y++)for(int x=3;x<8;x++){difference+=Mathf.Abs(detailed.GetPixel(x*detailed.width/10,y*detailed.height/10).grayscale-black.GetPixel(x*black.width/10,y*black.height/10).grayscale);pixels++;}
            checks["material_art_changes_rendered_creature_pixels_"+suffix]=difference/pixels>.004f;Debug.Log("CINEMATIC_ART pixels="+(difference/pixels)+" biome="+suffix+" landmarks="+scenery.Landmarks+" vertices="+scenery.Vertices);skin.SetTexture("_AlbedoMap",original);Destroy(detailed);Destroy(black);Time.timeScale=1;
            LandscapeCamera(planet,0);for(int j=0;j<20;j++)yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"cinematic-"+suffix+"-landscape.png"));
            var local=scenery.Contacts[0];Vector3 relative=(planet.body.P+local)-expedition.Focus;RebaseOrigin(new Vector3(320,-160,240));yield return null;
            checks["large_scenery_survives_origin_rebase_"+suffix]=scenery.BoundPlanet==planet&&Vector3.Distance(planet.body.P+scenery.Contacts[0]-expedition.Focus,relative)<.02f;
            bool saved=SaveCampaign()&&LoadCampaign(3);paused=true;ecologyShowcase=true;for(int j=0;j<65;j++)yield return null;
            checks["checkpoint_rebuilds_landmarks_against_new_planet_"+suffix]=saved&&scenery.BoundPlanet==expedition.Nearest(expedition.Focus)&&scenery.Landmarks>4;
        }
        foreach(var asset in new[]{"xeno_scutes_v1","alien_bark_v1","alien_leaf_v1"}){var texture=Resources.Load<Texture2D>("Surfaces/"+asset);checks["packaged_art_has_mipmaps_"+asset]=texture!=null&&texture.width>=1024&&texture.mipmapCount>1&&texture.wrapMode==TextureWrapMode.Repeat;}
        SaveChecks("cinematic-art-runtime-checks.json",checks);yield return new WaitForSeconds(.5f);Application.Quit();
    }
    IEnumerator CinematicArtRecord() {
        yield return new WaitForSeconds(1);preferences.quality=2;preferences.antialiasing=2;ApplyPreferences();Time.captureFramerate=30;
        productionFolder=Path.Combine(EvidenceDir(),"cinematic-art-frames");Directory.CreateDirectory(productionFolder);productionFrame=0;
        for(int biome=0;biome<3;biome++) {
            var planet=StageEcology(biome);for(int j=0;j<90;j++)yield return null;
            PlaceReviewCreatures(planet);
            ecologyDetail=biome==0?"Veined canopy / weathered trunks / native creature encounters":biome==1?"Eroded mineral pillars / dune fauna / sculpted carapaces":"Glacial formations / cryogenic fauna / ice armor";
            yield return ProductionFrames(180,"landscape-"+biome,i=>LandscapeCamera(planet,Mathf.Lerp(-4,5,i/179f)));
            for(int role=0;role<3;role++) {
                var foe=surface.Current.foes.Find(f=>f.kind==role);ecologyDetail=foe.view.species.name+" / "+foe.view.species.tactic;
                yield return ProductionFrames(100,"cinematic-fauna-"+biome+"-"+role,i=>{EcologyCamera(planet,role,Mathf.Lerp(-10,18,i/99f));foe.view.Animate(i/30f,0,Mathf.Sin(i/99f*Mathf.PI)*.2f,0,false);});
            }
        }
        Debug.Log("CINEMATIC_ART_FILM_COMPLETE frames="+productionFrame+" width="+Screen.width+" height="+Screen.height);Time.captureFramerate=0;yield return new WaitForSeconds(.5f);Application.Quit();
    }
    IEnumerator CinematicArtBenchmark() {
        yield return new WaitForSeconds(1);bool ultra=Array.IndexOf(Environment.GetCommandLineArgs(),"--production-ultra")>=0;preferences.quality=ultra?2:1;ApplyPreferences();
        var report=new BenchmarkReport{unity=Application.unityVersion,device=SystemInfo.deviceModel,processor=SystemInfo.processorType,graphics=SystemInfo.graphicsDeviceName,width=Screen.width,height=Screen.height,targetFps=preferences.targetFps,quality=ultra?"Ultra":"Balanced",renderScale=(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset).renderScale,antiAliasing=cam.GetUniversalAdditionalCameraData().antialiasing.ToString()};
        for(int biome=0;biome<3;biome++){var p=StageEcology(biome);LandscapeCamera(p,0);yield return BenchmarkPhaseFrames("cinematic-landscape-"+biome,report);EcologyCamera(p,2,10);yield return BenchmarkPhaseFrames("cinematic-guardian-"+biome,report);}
        File.WriteAllText(Path.Combine(EvidenceDir(),"cinematic-art-performance.json"),JsonUtility.ToJson(report,true));Application.Quit();
    }
}
