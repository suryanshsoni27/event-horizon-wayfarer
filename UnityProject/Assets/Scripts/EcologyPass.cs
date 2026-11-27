using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed partial class DefenseGame {
    bool ecologyShowcase;string ecologyTitle,ecologyDetail;
    PlanetExpedition.Planet StageEcology(int climate,string destination=null) {
        NewCampaign(3);paused=true;chapterBanner=toastTime=0;invulnerable=10000;
        if(destination!=null){var port=ports.Find(p=>p.key==destination);chartSystem=port.system;player=Make(port.global-OriginGlobal+Vector3.up*(port.radius+200),Vector3.zero);FloatingOrigin();Stream(true);}
        var planet=expedition.planets.Find(p=>p.key==(destination??(climate==0?"HOME":"S0P"+climate)));
        Vector3 sun=new Vector3(-.25f,.2f,-.94f).normalized,up=sun;float best=float.MaxValue;
        // Pick an ordinary native surface with daylight and low local relief for a readable art review.
        for(int j=0;j<80;j++) {
            Vector3 n=(sun+new Vector3(Mathf.Sin(j*2.39996f),Mathf.Cos(j*1.7f),Mathf.Sin(j*.83f))*.48f).normalized;
            float h=PlanetExpedition.Height(n,planet.body.seed,planet.body.radius);if(h<3)continue;
            Vector3 t=PlanetExpedition.SurfaceTangent(n,Vector3.forward),s=Vector3.Cross(n,t);float slope=0;
            foreach(var offset in new[]{t*16,-t*16,s*16,-s*16})slope=Mathf.Max(slope,Mathf.Abs(PlanetExpedition.Height((n*(float)planet.body.radius+offset).normalized,planet.body.seed,planet.body.radius)-h));
            if(slope<best){best=slope;up=n;}
        }
        Vector3 forward=PlanetExpedition.SurfaceTangent(up,Vector3.forward),at=planet.body.P+up*((float)planet.body.radius+PlanetExpedition.Height(up,planet.body.seed,planet.body.radius)+.6f);
        player=Make(at,Vector3.zero);shipRotation=desiredRotation=Quaternion.LookRotation(forward,up);FloatingOrigin();Stream(true);
        rocket.position=Pos(player);rocket.rotation=shipRotation;expedition.Status(player);GroundBoard();surface.Scripted=true;expedition.ScriptWalk(Vector3.zero);
        surface.Current.age=0;var foes=surface.Current.foes;
        for(int j=0;j<foes.Count;j++)surface.Damage(foes[j],10000);surface.Step(6);surface.Current.age=0;
        for(int role=0;role<3;role++) {
            Vector3 side=Vector3.Cross(up,forward),offset=role==0?-side*5+forward*6:role==1?side*5+forward*7:forward*15;
            var f=surface.Spawn(role,expedition.Focus+offset);f.view.root.rotation=Quaternion.LookRotation(-forward,up);f.cooldown=1000;
        }
        cam.fieldOfView=58;ecologyTitle=planet.name+" / "+SurfaceEcology.Habitat((SurfaceEcology.Biome)climate);
        ecologyDetail="Native terrain / original articulated creatures / physically based materials";
        ecologyShowcase=true;civilFilmCamera=true;civilFilmClearance=.3f;
        EcologyCamera(planet,0,0);return planet;
    }
    void EcologyCamera(PlanetExpedition.Planet planet,int role,float orbit) {
        var foe=surface.Current.foes.Find(f=>f.kind==role&&!f.dead);Vector3 up=surface.Up(surface.World(foe)),forward=foe.view.root.forward,side=Vector3.Cross(up,forward),center=surface.World(foe)+up*(role==2?.6f:0);
        float radius=role==2?10.5f:5.5f;Vector3 dir=Quaternion.AngleAxis(orbit,up)*(forward*.82f+side*.57f).normalized;
        ClearCreatureCamera(center,up,forward,side,radius,orbit,role==2?3.0f:1.6f);
        cam.transform.position=civilFilmAt;cam.transform.rotation=Quaternion.LookRotation(civilFilmLook-civilFilmAt,up);
    }
    void DrawEcologyShowcase() {
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1600f,Screen.height/1000f,1));
        GUI.color=new Color(.015f,.025f,.028f,.92f);GUI.DrawTexture(new Rect(0,0,1600,103),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,925,1600,75),Texture2D.whiteTexture);GUI.color=Color.white;
        var title=new GUIStyle(GUI.skin.label){fontSize=30,fontStyle=FontStyle.Bold,normal={textColor=new Color(.88f,.91f,.87f)}};
        var detail=new GUIStyle(GUI.skin.label){fontSize=18,normal={textColor=new Color(.72f,.78f,.73f)}};
        GUI.Label(new Rect(42,16,1516,43),ecologyTitle,title);GUI.Label(new Rect(44,61,1500,35),ecologyDetail,detail);
        GUI.Label(new Rect(44,945,1510,34),"RECORDED UNITY PLAYER / NATIVE PLANET SURFACES / STAGED ENCOUNTERS AND CAMERAS",detail);
    }
    IEnumerator EcologyChecks() {
        yield return new WaitForSeconds(1);preferences.quality=2;ApplyPreferences();var checks=new Dictionary<string,bool>();var ids=new HashSet<int>();var meshes=new HashSet<Mesh>();
        string real=Path.Combine(Application.persistentDataPath,"wayfarer-campaign-slot-1.json");DateTime stamp=File.Exists(real)?File.GetLastWriteTimeUtc(real):DateTime.MinValue;
        for(int biome=0;biome<3;biome++) {
            var planet=StageEcology(biome);string suffix=((SurfaceEcology.Biome)biome).ToString().ToLowerInvariant();
            for(int j=0;j<120;j++)yield return null;
            var field=GetComponent<SurfaceFlora>();field.Refresh();yield return new WaitForSeconds(.8f);
            checks["native_planet_has_correct_ecology_"+suffix]=field.Biome==(SurfaceEcology.Biome)biome&&field.Plants>0;
            checks["vegetation_matches_climate_"+suffix]=biome==0?field.Grass>0&&field.IceClusters==0&&field.Succulents==0:biome==1?field.Grass==0&&field.Succulents>0&&field.IceClusters==0:field.Grass==0&&field.Succulents==0&&field.IceClusters>0;
            checks["combined_ecology_mesh_has_bounded_geometry_"+suffix]=field.Vertices>100&&field.Vertices<350000;Debug.Log("ECOLOGY_MESH "+suffix+" plants="+field.Plants+" vertices="+field.Vertices);
            checks["actual_spawn_uses_destination_species_"+suffix]=surface.Current.foes.Count==3&&surface.Current.foes.TrueForAll(f=>f.view.species==surface.Species(f)&&f.view.root.name==surface.Species(f).name);
            bool valid=true,animated=true;
            foreach(var foe in surface.Current.foes) {
                ids.Add(surface.Species(foe).Id);var body=foe.view.torso.GetComponentInChildren<MeshFilter>().sharedMesh;meshes.Add(body);
                foreach(var mf in foe.view.root.GetComponentsInChildren<MeshFilter>()) {
                    var m=mf.sharedMesh;valid&=m.vertexCount>0&&m.normals.Length==m.vertexCount;
                    foreach(var v in m.vertices)valid&=VoyagerState.Finite(v.x)&&VoyagerState.Finite(v.y)&&VoyagerState.Finite(v.z);
                }
                foe.view.Animate(1,2,0,0,false);var knee=foe.view.knees[0].rotation;foe.view.Animate(1.5f,2,.8f,0,false);
                animated&=Quaternion.Angle(knee,foe.view.knees[0].rotation)>1&&foe.view.KneeFlex>5;
            }
            checks["species_geometry_and_normals_are_finite_"+suffix]=valid;
            checks["species_have_grounded_articulated_animation_"+suffix]=animated;
            var first=surface.Current.foes[0];float oldHp=first.hp;surface.Damage(first,7);first.contractId=719;
            var checkpoint=CaptureCampaign();var encoded=JsonUtility.ToJson(checkpoint);var restored=JsonUtility.FromJson<CampaignCheckpoint>(encoded);
            checks["checkpoint_retains_hurt_species_and_contract_"+suffix]=restored.Validate()&&restored.surface.expeditions.Exists(e=>e.key==planet.key&&e.foes.Exists(f=>f.kind==0&&f.contractId==719&&Mathf.Abs(f.hp-(oldHp-7))<.01f));
            bool saved=SaveCampaign()&&LoadCampaign(3);paused=true;ecologyShowcase=true;
            planet=expedition.Nearest(expedition.Focus);first=surface.Current.foes.Find(f=>f.kind==0);GetComponent<SurfaceFlora>().Refresh();for(int frame=0;frame<65;frame++)yield return null;
            checks["loaded_terrain_and_ecology_share_the_live_planet_"+suffix]=GetComponent<SurfaceFlora>().BoundPlanet==planet&&SurfaceScatter.Current.BoundPlanet==planet;
            checks["actual_load_recreates_same_planet_species_"+suffix]=saved&&first!=null&&first.view.species.Id==biome*3&&first.contractId==719&&Mathf.Abs(first.hp-(oldHp-7))<.01f;
            var originBefore=surface.World(first)-expedition.Focus;RebaseOrigin(new Vector3(320,-160,240));surface.Step(.01f);
            checks["species_survives_floating_origin_"+suffix]=Vector3.Distance(surface.World(first)-expedition.Focus,originBefore)<.1f;
            // Real attacks execute the ordinary windup and native movement, rather than inspecting tuning values.
            surface.Current.age=7;Vector3 up=surface.Up(expedition.Focus),t=PlanetExpedition.SurfaceTangent(up,ShipForward);
            first.local=surface.CreaturePoint(expedition.Focus+t*1.8f)-planet.body.P;first.velocity=Vector3.zero;first.cooldown=0;
            float before=health;invulnerable=0;voyager.shield=0;for(int j=0;j<180&&health==before;j++)surface.Step(.01f);
            checks["biome_melee_windup_deals_damage_"+suffix]=health<before;Debug.Log("ECOLOGY_COMBAT "+suffix+" before="+before+" after="+health+" range="+Vector3.ProjectOnPlane(surface.World(first)-expedition.Focus,up).magnitude+" attack="+first.attacking+" windup="+first.windup);
            first.local=surface.CreaturePoint(expedition.Focus+t*1.8f)-planet.body.P;first.attacking=true;first.windup=.01f;first.local+=t*16;first.velocity=Vector3.zero;invulnerable=0;before=health;surface.Step(.02f);
            checks["biome_telegraph_can_be_dodged_"+suffix]=health==before;
            var ranged=surface.Current.foes.Find(f=>f.kind==1);ranged.local=surface.CreaturePoint(expedition.Focus+t*12)-planet.body.P;ranged.velocity=Vector3.zero;ranged.cooldown=0;ranged.attacking=false;
            int fired=surface.ProjectilesFired;for(int j=0;j<150&&surface.ProjectilesFired==fired;j++)surface.Step(.01f);
            checks["biome_ranged_windup_launches_native_projectile_"+suffix]=surface.ProjectilesFired>fired;
            checks["native_species_motion_executes_"+suffix]=surface.NativeSteps>50;
            // Restore review poses after the combat fixtures and capture all nine actual creature meshes.
            for(int role=0;role<3;role++) {
                var foe=surface.Current.foes.Find(f=>f.kind==role);foe.local=surface.CreaturePoint(expedition.Focus+t*(role==2?15:6)+Vector3.Cross(up,t)*(role==0?-5:role==1?5:0))-planet.body.P;
                foe.velocity=Vector3.zero;foe.attacking=false;foe.hurt=0;foe.view.root.rotation=Quaternion.LookRotation(-t,up);
            }
            surface.Current.age=0;surface.Step(.01f);
            for(int role=0;role<3;role++) {
                EcologyCamera(planet,role,10);ecologyDetail=SurfaceEcology.For(planet,role).name+" / "+SurfaceEcology.For(planet,role).tactic;
                for(int j=0;j<12;j++)yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"ecology-"+suffix+"-"+role+".png"));
            }
        }
        var resource=Resources.Load<UnityEngine.Object>("ContactOcclusion");Debug.Log("ECOLOGY_RENDER_RESOURCE "+(resource==null?"missing":resource.GetType().FullName)+" postProcessing="+cam.GetUniversalAdditionalCameraData().renderPostProcessing);
        var renderer=cam.GetUniversalAdditionalCameraData().scriptableRenderer;
        checks["packaged_renderer_has_postprocessing_resources"]=cam.GetUniversalAdditionalCameraData().renderPostProcessing&&renderer!=null&&Resources.Load<ScreenSpaceAmbientOcclusion>("ContactOcclusion")!=null;
        var contact=Resources.Load<ScreenSpaceAmbientOcclusion>("ContactOcclusion");preferences.quality=0;ApplyPreferences();yield return new WaitForEndOfFrame();bool lowOff=contact!=null&&!contact.isActive;preferences.quality=2;ApplyPreferences();yield return new WaitForEndOfFrame();checks["contact_occlusion_follows_actual_quality_setting"]=lowOff&&contact.isActive;
        var volume=GameObject.Find("Cinematic color / local procedural art").GetComponent<Volume>();ColorAdjustments grade;volume.profile.TryGet(out grade);
        grade.postExposure.Override(-1);yield return new WaitForEndOfFrame();var dark=ScreenCapture.CaptureScreenshotAsTexture();
        grade.postExposure.Override(0);yield return new WaitForEndOfFrame();var lit=ScreenCapture.CaptureScreenshotAsTexture();float darkMean=0,litMean=0;
        for(int y=2;y<8;y++)for(int x=2;x<8;x++){darkMean+=dark.GetPixel(x*dark.width/10,y*dark.height/10).grayscale;litMean+=lit.GetPixel(x*lit.width/10,y*lit.height/10).grayscale;}
        checks["color_grading_changes_actual_rendered_pixels"]=litMean>darkMean*1.08f;Destroy(dark);Destroy(lit);
        checks["nine_distinct_species_are_spawned"]=ids.Count==9&&meshes.Count==9;
        var chitin=new Material(Shader.Find("Orbit/XenoChitin"));checks["creature_pbr_depth_and_dissolve_shadows_supported"]=chitin.shader.isSupported&&chitin.FindPass("DepthOnly")>=0&&chitin.FindPass("ShadowCaster")>=0;Destroy(chitin);
        var rock=new Material(Shader.Find("Orbit/PlanetRock"));checks["climate_rock_shader_has_supported_depth_and_shadows"]=rock.shader.isSupported&&rock.FindPass("DepthOnly")>=0&&rock.FindPass("ShadowCaster")>=0;Destroy(rock);
        checks["qa_preserves_private_checkpoint"]=stamp==(File.Exists(real)?File.GetLastWriteTimeUtc(real):DateTime.MinValue);
        ecologyShowcase=false;SaveChecks("ecology-runtime-checks.json",checks);yield return new WaitForSeconds(.5f);Application.Quit();
    }
    IEnumerator EcologyRecord() {
        yield return new WaitForSeconds(1);preferences.quality=2;preferences.antialiasing=2;ApplyPreferences();Time.captureFramerate=30;
        productionFolder=Path.Combine(EvidenceDir(),"ecology-frames");Directory.CreateDirectory(productionFolder);productionFrame=0;
        for(int biome=0;biome<3;biome++) {
            var planet=StageEcology(biome);for(int j=0;j<90;j++)yield return null;foreach(var f in surface.Current.foes)f.local=surface.CreaturePoint(surface.World(f))-planet.body.P;surface.Step(.01f);
            for(int role=0;role<3;role++) {
                var foe=surface.Current.foes.Find(f=>f.kind==role);ecologyDetail=foe.view.species.name+" / "+foe.view.species.tactic;
                yield return ProductionFrames(120,"ecology-"+biome+"-"+role,i=>{EcologyCamera(planet,role,Mathf.Lerp(-12,18,i/119f));foe.view.Animate(i/30f,0,Mathf.Sin(i/119f*Mathf.PI)*.3f,0,false);});
            }
        }
        Debug.Log("ECOLOGY_FILM_COMPLETE frames="+productionFrame+" width="+Screen.width+" height="+Screen.height);Time.captureFramerate=0;yield return new WaitForSeconds(.5f);Application.Quit();
    }
}
