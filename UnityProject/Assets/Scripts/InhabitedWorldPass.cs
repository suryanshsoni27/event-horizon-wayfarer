using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed partial class DefenseGame {
    void InhabitedCamera(PlanetExpedition.Planet p,int role,float orbit=0) {
        var f=surface.Current.foes.Find(x=>x.kind==role&&!x.dead);Vector3 up=surface.Up(surface.World(f)),front=f.view.root.forward,side=Vector3.Cross(up,front),center=surface.World(f)+up*.1f;
        Vector3 direction=Quaternion.AngleAxis(orbit,up)*(front*.96f+side*.28f).normalized;
        ClearCreatureCamera(center+up*.15f,up,front,side,4.6f,orbit,.65f);civilFilmLook=center+up*.20f;
        float alt=expedition.Alt(civilFilmAt);if(alt<.4f)civilFilmAt+=up*(.4f-alt);
        cam.transform.SetPositionAndRotation(civilFilmAt,Quaternion.LookRotation(civilFilmLook-civilFilmAt,up));
    }
    void OutpostCamera(PlanetExpedition.Planet p,float orbit=0) {
        Vector3 anchor=p.body.P+surface.Current.anchor,up=surface.Up(anchor),front=PlanetExpedition.SurfaceTangent(up,ShipForward),side=Vector3.Cross(up,front);
        civilFilmLook=anchor+front*46+up*5;civilFilmAt=anchor-front*48+side*75+up*38;
        Vector3 d=civilFilmAt-civilFilmLook;civilFilmAt=civilFilmLook+Quaternion.AngleAxis(orbit,up)*d;float alt=expedition.Alt(civilFilmAt);if(alt<3)civilFilmAt+=up*(3-alt);
        cam.transform.SetPositionAndRotation(civilFilmAt,Quaternion.LookRotation(civilFilmLook-civilFilmAt,up));
    }
    void CoastCamera(PlanetExpedition.Planet p,bool mountains=false,float orbit=0) {
        Vector3 sun=new Vector3(-.25f,.2f,-.94f).normalized,chosen=sun;float best=float.MaxValue;
        for(int i=0;i<2400;i++) {
            float y=1-2*(i+.5f)/2400,a=i*2.39996f;Vector3 n=new Vector3(Mathf.Sqrt(1-y*y)*Mathf.Cos(a),y,Mathf.Sqrt(1-y*y)*Mathf.Sin(a));
            if(Vector3.Dot(n,sun)<.35f)continue;float h=PlanetExpedition.Height(n,p.body.seed,p.body.radius);float cost=mountains?-h:Mathf.Abs(h-1.6f);
            if(cost<best){best=cost;chosen=n;}
        }
        Vector3 t=PlanetExpedition.SurfaceTangent(chosen,Vector3.forward),side=Vector3.Cross(chosen,t);float height=PlanetExpedition.Height(chosen,p.body.seed,p.body.radius);Vector3 at=p.body.P+chosen*((float)p.body.radius+height);
        if(mountains){civilFilmAt=at-t*370+side*150+chosen*120;civilFilmLook=at+t*180-chosen*25;}
        else {
            Vector3 landward=t;float gradient=-999;
            for(int i=0;i<16;i++){Vector3 d=Quaternion.AngleAxis(i*22.5f,chosen)*t;float h=PlanetExpedition.Height((at+d*60-p.body.P).normalized,p.body.seed,p.body.radius);if(h>gradient){gradient=h;landward=d;}}
            civilFilmAt=at+landward*30+chosen*6;civilFilmLook=at-landward*100-chosen*2;
        }
        civilFilmAt=civilFilmLook+Quaternion.AngleAxis(orbit,chosen)*(civilFilmAt-civilFilmLook);float alt=expedition.Alt(civilFilmAt);if(alt<3)civilFilmAt+=chosen*(3-alt);
        cam.transform.SetPositionAndRotation(civilFilmAt,Quaternion.LookRotation(civilFilmLook-civilFilmAt,chosen));
    }
    IEnumerator InhabitedWorldChecks() {
        yield return new WaitForSeconds(1);preferences.quality=2;ApplyPreferences();var checks=new Dictionary<string,bool>();
        checks["occupation_preserves_home_and_black_holes"]=SurfaceEcology.Civilization("HOME")==SurfaceEcology.Society.Wildlife&&SurfaceEcology.Civilization("S1P3")==SurfaceEcology.Society.Wildlife&&SurfaceEcology.Civilization("S2P1")==SurfaceEcology.Society.Wildlife;
        checks["two_advanced_societies_are_discoverable"]=SurfaceEcology.Civilization("S1P0")==SurfaceEcology.Society.AurelianConcord&&SurfaceEcology.Civilization("S2P0")==SurfaceEcology.Society.AstronautEnclave;
        var identities=new HashSet<int>();
        for(int society=1;society<=2;society++) {
            string suffix=society==1?"aurelian":"astronaut";var p=StageEcology(0,"S"+society+"P0");
            for(int i=0;i<100;i++)yield return null;surface.Step(.01f);var settlement=SurfaceSettlement.Current;var scenery=GetComponent<SurfaceScenery>();
            checks["actual_destination_spawns_armed_humanoids_"+suffix]=surface.Inhabited&&surface.Current.foes.TrueForAll(f=>f.view!=null&&f.view.species.humanoid&&f.view.Muzzle!=null&&f.view.humanoid.legs.Length==2);
            bool finite=true,animated=true;foreach(var f in surface.Current.foes){identities.Add(surface.Species(f).Id);var h=f.view.humanoid;h.velocity=f.view.root.forward*2;Vector3 start=f.view.root.position;f.view.Animate(1,2,0,0,false);var knee=h.legs[0].lower.rotation;float angle=0,flex=0;for(int pose=1;pose<=24;pose++){f.view.root.position=start+f.view.root.forward*pose*.04f;f.view.Animate(1+pose*.04f,2,0,0,false);angle=Mathf.Max(angle,Quaternion.Angle(knee,h.legs[0].lower.rotation));flex=Mathf.Max(flex,f.view.KneeFlex);}animated&=flex>5&&angle>1;f.view.root.position=start;foreach(var mf in f.view.root.GetComponentsInChildren<MeshFilter>())foreach(var v in mf.sharedMesh.vertices)finite&=VoyagerState.Finite(v.x)&&VoyagerState.Finite(v.y)&&VoyagerState.Finite(v.z);}
            checks["biped_meshes_and_weapon_transforms_are_finite_"+suffix]=finite;
            var aimFoe=surface.Current.foes[0];Vector3 aimAt=surface.World(aimFoe),aimUp=surface.Up(aimAt),aimForward=aimFoe.view.root.forward;float hitDistance;bool headHit;
            checks["humanoid_head_has_its_own_weak_hit_volume_"+suffix]=surface.HumanRayHit(aimFoe,aimAt+aimUp*.81f+aimForward*4,-aimForward,10,out hitDistance,out headHit)&&headHit;
            checks["humanoid_body_hit_does_not_award_headshot_"+suffix]=surface.HumanRayHit(aimFoe,aimAt+aimUp*.19f+aimForward*4,-aimForward,10,out hitDistance,out headHit)&&!headHit;
            checks["bipeds_have_animated_two_bone_legs_"+suffix]=animated;
            checks["inhabited_world_has_research_and_launch_architecture_"+suffix]=settlement.BoundPlanet==p&&settlement.Buildings==5&&settlement.Root.Find("Orbital launch facility")!=null;
            checks["settlement_and_forest_fit_native_contact_budget_"+suffix]=settlement.Contacts.Count+scenery.Contacts.Count+SurfaceScatter.Current.Count<=192;
            Vector3 wall=p.body.P+settlement.Contacts[0],up=surface.Up(wall),t=PlanetExpedition.SurfaceTangent(up,ShipForward);var native=Make(wall+t*.1f,Vector3.zero);PlanetExpedition.StepMode(ref native,Vector3.zero,.001f,2);
            checks["native_physics_blocks_settlement_building_"+suffix]=Vector3.Distance(Pos(native),wall)>settlement.Radii[0]+1.12f;
            var first=surface.Current.foes.Find(f=>f.kind==0);first.cooldown=0;first.attacking=false;first.burst=0;surface.Current.age=7;
            bool sight=false;Vector3 originalCrew=expedition.Focus;
            for(int angle=0;angle<24&&!sight;angle++) {
                Vector3 d=Quaternion.AngleAxis(angle*15,up)*t;first.local=surface.CreaturePoint(originalCrew+d*13)-p.body.P;first.velocity=Vector3.zero;first.view.root.position=surface.World(first);first.view.root.rotation=Quaternion.LookRotation((originalCrew-first.view.root.position).normalized,up);first.view.Aim(originalCrew);first.view.Animate(7,0,0,0,false);sight=surface.SoldierSight(first,originalCrew+up*.12f);
            }
            checks["rifle_uses_clear_muzzle_line_of_sight_"+suffix]=sight;
            foreach(var f in surface.Current.foes)if(f!=first){f.stun=30;f.cooldown=1000;}
            health=100;voyager.shield=0;invulnerable=0;int shots=surface.RifleShots;for(int step=0;step<450;step++)surface.Step(.01f);
            checks["soldier_fires_native_three_round_burst_"+suffix]=surface.RifleShots>=shots+3&&surface.Reloads>0;
            checks["native_rifle_projectile_damages_crew_"+suffix]=health<100;Debug.Log("INHABITED_COMBAT "+suffix+" shots="+surface.RifleShots+" hp="+health);
            first.local=wall+t*(settlement.Radii[0]+3)-p.body.P;first.view.root.position=surface.World(first);first.view.root.rotation=Quaternion.LookRotation(-t,up);first.view.Aim(wall-t*8);first.view.Animate(12,0,0,0,false);
            checks["solid_architecture_blocks_weapon_sight_"+suffix]=!surface.SoldierSight(first,wall-t*(settlement.Radii[0]+3));
            expedition.ResolveStructure(wall-t*(settlement.Radii[0]+3));first.attacking=true;first.windup=.001f;first.burst=0;first.cooldown=0;int beforeBlocked=surface.RifleShots;surface.Step(.02f);
            checks["cover_cancels_actual_soldier_attack_"+suffix]=surface.RifleShots==beforeBlocked&&!first.attacking&&first.burst==0&&surface.BlockedShots>0;expedition.ResolveStructure(originalCrew);
            checks["architecture_is_batched_for_rendering_"+suffix]=settlement.RenderBatches>0&&settlement.RenderBatches<=12;
            first.local=surface.CreaturePoint(originalCrew+t*3)-p.body.P;first.burst=2;first.attacking=true;first.windup=.3f;surface.Pulse();checks["pulse_interrupts_armed_burst_"+suffix]=first.stun>0&&first.burst==0&&!first.attacking;
            int id=surface.Species(first).Id;first.contractId=802;float hp=first.hp;bool saved=SaveCampaign()&&LoadCampaign(3);paused=true;ecologyShowcase=true;for(int i=0;i<80;i++)yield return null;p=expedition.Nearest(expedition.Focus);first=surface.Current.foes.Find(f=>f.kind==0);
            checks["checkpoint_restores_armed_species_hp_and_contract_"+suffix]=saved&&first!=null&&surface.Species(first).Id==id&&first.hp==hp&&first.contractId==802&&first.view.Muzzle!=null;
            checks["checkpoint_restores_anchored_settlement_"+suffix]=SurfaceSettlement.Current.BoundPlanet==p&&SurfaceSettlement.Current.Buildings==5;
            Vector3 before=SurfaceSettlement.Current.Contacts[0]+p.body.P-expedition.Focus;RebaseOrigin(new Vector3(320,-160,240));yield return null;
            checks["settlement_contacts_survive_origin_shift_"+suffix]=Vector3.Distance(SurfaceSettlement.Current.Contacts[0]+p.body.P-expedition.Focus,before)<.02f;
            for(int role=0;role<3;role++){var f=surface.Current.foes.Find(x=>x.kind==role);f.stun=1000;f.cooldown=1000;f.attacking=false;f.burst=0;f.local=surface.CreaturePoint(expedition.Focus+t*7)-p.body.P;f.view.root.position=surface.World(f);f.view.root.rotation=Quaternion.LookRotation(-t,surface.Up(surface.World(f)));f.view.Animate(0,0,0,0,false);foreach(var other in surface.Current.foes)other.view.root.gameObject.SetActive(other==f);InhabitedCamera(p,role);ecologyTitle=SurfaceEcology.SocietyName(p.key);ecologyDetail=surface.Species(f).name+" / skinned anatomical rig, suit armor and physical rifle";for(int frame=0;frame<10;frame++)yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"inhabited-"+suffix+"-"+role+".png"));}
            foreach(var f in surface.Current.foes)f.view.root.gameObject.SetActive(true);OutpostCamera(p);ecologyDetail="Military research outpost / solid buildings / orbital supply rocket / research terminals";for(int frame=0;frame<15;frame++)yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"inhabited-"+suffix+"-outpost.png"));
        }
        checks["six_unique_armed_roles_preserve_nine_wildlife_species"]=identities.Count==6&&SurfaceEcology.For("HOME",0).Id==0&&SurfaceEcology.For("S0P1",2).Id==5&&SurfaceEcology.For("S0P2",2).Id==8;
        var home=StageEcology(0);for(int frame=0;frame<100;frame++)yield return null;var trees=GetComponent<SurfaceScenery>();
        checks["new_campaign_enables_versioned_alpine_terrain"]=expedition.LandscapeVersion==1&&home.body.seed>=1000&&CaptureCampaign().landscapeVersion==1;
        bool parity=true;float difference=0;for(int i=0;i<512;i++){Vector3 n=new Vector3(Mathf.Sin(i*1.31f),Mathf.Cos(i*.63f),Mathf.Sin(i*.79f+.3f)).normalized;float modern=PlanetExpedition.Height(n,home.body.seed,home.body.radius);parity&=Math.Abs(modern-bh_terrain_height(expedition.planets.IndexOf(home),n.x,n.y,n.z))<.003;difference=Mathf.Max(difference,modern-PlanetExpedition.Height(n,home.body.seed-1000,home.body.radius));}
        checks["mountains_match_native_physics_at_512_directions"]=parity;
        checks["alpine_profile_adds_real_ridge_geometry"]=difference>30;
        home.lod.RefineAt(expedition.Focus,100);int seamSamples;float seam=home.lod.MaximumSeamError(out seamSamples);checks["modern_terrain_edges_remain_watertight_without_skirts"]=seamSamples>100&&seam<.02f;
        bool noSkirts=true;foreach(var filter in home.root.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh.name.StartsWith("Stitched cube-sphere",StringComparison.Ordinal))foreach(var index in filter.sharedMesh.triangles)noSkirts&=index<17*17;
        checks["terrain_does_not_render_redundant_vertical_skirt_walls"]=noSkirts;
        checks["dense_forest_has_bounded_geometry_and_native_near_trunks"]=trees.Landmarks>=45&&trees.Vertices<400000&&trees.Contacts.Count==57;
        Debug.Log("INHABITED_FOREST trees="+trees.Landmarks+" vertices="+trees.Vertices+" contacts="+trees.Contacts.Count);
        var water=home.root.GetComponentInChildren<PlanetWater>();checks["temperate_planet_has_real_water_surface"]=water!=null&&water.Surface.vertexCount>200000&&water.Material.shader.isSupported;
        checks["arid_and_ice_worlds_keep_their_own_surface"]=expedition.planets.TrueForAll(p=>SurfaceEcology.Climate(p.key)==SurfaceEcology.Biome.Temperate||p.root.GetComponentInChildren<PlanetWater>()==null);
        CoastCamera(home);ecologyTitle="PELAGOS / COASTAL WATER";ecologyDetail="Rendered shallow sea / animated ripples, reflections and depth-based shoreline foam";for(int frame=0;frame<100;frame++)yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"inhabited-coast.png"));var wet=ScreenCapture.CaptureScreenshotAsTexture();water.GetComponent<Renderer>().enabled=false;yield return new WaitForEndOfFrame();var dry=ScreenCapture.CaptureScreenshotAsTexture();float pixels=0;for(int y=2;y<9;y++)for(int x=1;x<9;x++)pixels+=Mathf.Abs(wet.GetPixel(x*wet.width/10,y*wet.height/10).grayscale-dry.GetPixel(x*dry.width/10,y*dry.height/10).grayscale);water.GetComponent<Renderer>().enabled=true;Destroy(wet);Destroy(dry);
        checks["water_surface_changes_actual_rendered_pixels"]=pixels/56>.002f;Debug.Log("INHABITED_WATER pixel_difference="+pixels/56);
        CoastCamera(home,true);ecologyTitle="PELAGOS / ALPINE HIGHLANDS";ecologyDetail="New-campaign ridges / native terrain collision matches visible geometry";for(int frame=0;frame<120;frame++)yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"inhabited-mountains.png"));
        LandscapeCamera(home,0);ecologyTitle="PELAGOS / DENSE CANOPY";ecologyDetail="Broadleaf and evergreen trees / streamed near and far detail / solid nearby trunks";for(int frame=0;frame<15;frame++)yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"inhabited-forest.png"));
        var legacy=CaptureCampaign();legacy.landscapeVersion=0;checks["older_checkpoint_generation_remains_valid"]=legacy.Validate();legacy.landscapeVersion=2;checks["unknown_terrain_generation_is_rejected"]=!legacy.Validate();
        SaveChecks("inhabited-worlds-runtime-checks.json",checks);yield return new WaitForSeconds(.5f);Application.Quit();
    }
    IEnumerator InhabitedWorldBenchmark() {
        yield return new WaitForSeconds(1);bool ultra=Array.IndexOf(Environment.GetCommandLineArgs(),"--production-ultra")>=0;preferences.quality=ultra?2:1;ApplyPreferences();
        var report=new BenchmarkReport{unity=Application.unityVersion,device=SystemInfo.deviceModel,processor=SystemInfo.processorType,graphics=SystemInfo.graphicsDeviceName,width=Screen.width,height=Screen.height,targetFps=preferences.targetFps,quality=ultra?"Ultra":"Balanced",renderScale=(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset).renderScale,antiAliasing=cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().antialiasing.ToString()};
        for(int world=0;world<3;world++) {
            var p=StageEcology(0,world==0?null:"S"+world+"P0");paused=false;cosmosRecording=true;invulnerable=10000;surface.Current.age=7;foreach(var foe in surface.Current.foes)foe.cooldown=.3f;
            Vector3 up=surface.Up(expedition.Focus),forward=PlanetExpedition.SurfaceTangent(up,ShipForward),side=Vector3.Cross(up,forward),start=expedition.Focus;expedition.ScriptWalk(forward*.65f);expedition.SetAim(start+forward*20);
            // Keep the ordinary gameplay simulation running while following the walking crew.
            var phase=BenchmarkPhaseFrames(world==0?"live-forest-wildlife":"live-armed-outpost-"+world,report);
            while(phase.MoveNext()){civilFilmAt=expedition.Focus-forward*5+side*3+up*2;civilFilmLook=expedition.Focus+forward*6+up*.25f;yield return phase.Current;}
            Debug.Log("INHABITED_BENCHMARK_ACTIVITY world="+world+" nativeSteps="+surface.NativeSteps+" projectiles="+surface.ProjectilesFired+" travelled="+Vector3.Distance(start,expedition.Focus)+" batches="+(SurfaceSettlement.Current==null?0:SurfaceSettlement.Current.RenderBatches));expedition.ScriptWalk(Vector3.zero);paused=true;
        }
        File.WriteAllText(Path.Combine(EvidenceDir(),"inhabited-worlds-performance.json"),JsonUtility.ToJson(report,true));Debug.Log("INHABITED_BENCHMARK_COMPLETE");Application.Quit();
    }
    IEnumerator InhabitedWorldRecord() {
        yield return new WaitForSeconds(1);preferences.quality=2;ApplyPreferences();Time.captureFramerate=30;productionFolder=Path.Combine(EvidenceDir(),"inhabited-worlds-frames");Directory.CreateDirectory(productionFolder);productionFrame=0;
        for(int society=1;society<=2;society++) {
            var p=StageEcology(0,"S"+society+"P0");for(int frame=0;frame<75;frame++)yield return null;
            ecologyTitle=SurfaceEcology.SocietyName(p.key);ecologyDetail="Military research settlement / orbital launch facility / solid architecture";
            yield return ProductionFrames(150,"outpost-"+society,i=>OutpostCamera(p,Mathf.Lerp(-5,5,i/149f)));
            for(int role=0;role<3;role++){var f=surface.Current.foes.Find(x=>x.kind==role);ecologyDetail=surface.Species(f).name+" / original articulated suit and weapon";yield return ProductionFrames(90,"armed-"+society+"-"+role,i=>{InhabitedCamera(p,role,Mathf.Lerp(-8,12,i/89f));f.view.Aim(expedition.Focus);f.view.Animate(i/30f,0,Mathf.Sin(i/89f*Mathf.PI)*.5f,0,false);});}
        }
        var home=StageEcology(0);for(int frame=0;frame<90;frame++)yield return null;
        ecologyTitle="PELAGOS / DENSE FOREST";ecologyDetail="Broadleaf crowns and evergreen trees / native near-trunk contacts";yield return ProductionFrames(150,"forest",i=>LandscapeCamera(home,Mathf.Lerp(-4,5,i/149f)));
        ecologyTitle="PELAGOS / SHALLOW COAST";ecologyDetail="Moving water highlights / reflections / depth-based shoreline foam";CoastCamera(home);for(int frame=0;frame<100;frame++)yield return null;Vector3 at=civilFilmAt,look=civilFilmLook,up=surface.Up(at);yield return ProductionFrames(150,"coast",i=>{civilFilmAt=look+Quaternion.AngleAxis(Mathf.Lerp(-1,1,i/149f),up)*(at-look);civilFilmLook=look;});
        ecologyTitle="PELAGOS / ALPINE HIGHLANDS";ecologyDetail="Sharper native-matched ridges for new campaigns / existing saves preserve their terrain";CoastCamera(home,true);for(int frame=0;frame<120;frame++)yield return null;at=civilFilmAt;look=civilFilmLook;up=surface.Up(at);yield return ProductionFrames(150,"mountains",i=>{civilFilmAt=look+Quaternion.AngleAxis(Mathf.Lerp(-1,1,i/149f),up)*(at-look);civilFilmLook=look;});
        Debug.Log("INHABITED_WORLDS_FILM_COMPLETE frames="+productionFrame);Time.captureFramerate=0;yield return new WaitForSeconds(.5f);Application.Quit();
    }
}
