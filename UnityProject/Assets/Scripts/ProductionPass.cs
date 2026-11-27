using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering.Universal;

public sealed partial class DefenseGame {
 static bool ClosedMesh(Mesh mesh){var edges=new Dictionary<ulong,int>();var indices=mesh.triangles;for(int i=0;i<indices.Length;i+=3)for(int j=0;j<3;j++){int a=indices[i+j],b=indices[i+(j+1)%3];ulong key=((ulong)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);edges[key]=edges.ContainsKey(key)?edges[key]+1:1;}foreach(int count in edges.Values)if(count!=2)return false;return edges.Count>0;}
 IEnumerator ProductionChecks(){
  yield return new WaitForSeconds(1);var checks=new Dictionary<string,bool>();
  string real=Path.Combine(Application.persistentDataPath,"wayfarer-campaign-slot-1.json"),settings=SettingsPath;
  DateTime realStamp=File.Exists(real)?File.GetLastWriteTimeUtc(real):DateTime.MinValue,settingsStamp=File.Exists(settings)?File.GetLastWriteTimeUtc(settings):DateTime.MinValue;
  NewCampaign(3);StageVoyagerCity();paused=true;
  for(int i=0;i<180;i++)yield return null;
  var planet=expedition.Nearest(expedition.Focus);var colony=civilization.Find(planet.key);int seamSamples;
  float seam=planet.lod.MaximumSeamError(out seamSamples);checks["adaptive_mesh_edges_are_continuous"]=seamSamples>1000&&seam<.02f;
  checks["terrain_lod_stays_within_600_node_budget"]=planet.lod.NodeCount<=600&&planet.lod.MaxDepth==10;
  float gap=0;bool contact=true;foreach(var district in colony.districts){Vector3 at=civilizationScene.DistrictPosition(district.id),up=district.normal;
   float rendered=planet.lod.RenderedRadius(up),level=Vector3.Dot(at-planet.body.P,up);gap=Mathf.Max(gap,Mathf.Abs(level-rendered-.24f));
   contact&=Mathf.Abs(CityAltitude(at+up*1.15f,10)-1.15f)<.03f;
  }
  checks["graded_ground_meets_every_constructed_platform"]=gap<.04f;checks["crew_contacts_match_constructed_platform_floors"]=contact;
  Vector3 far=(colony.anchor*(float)planet.body.radius+PlanetExpedition.SurfaceTangent(colony.anchor,Vector3.right)*1200).normalized;
  checks["foundation_grading_preserves_distant_native_terrain"]=Mathf.Abs(planet.lod.SampleSurface(far).magnitude-(float)planet.body.radius-PlanetExpedition.Height(far,planet.body.seed,planet.body.radius))<.01f;
  var cloud=Array.Find(planet.root.GetComponentsInChildren<Transform>(),t=>t.name=="Weather / moving cloud layer");checks["clouds_clear_the_analytical_maximum_mountain_height"]=cloud!=null&&cloud.localScale.x*.5f>(float)planet.body.radius*1.145f;
  checks["hdr_reflections_survive_campaign_resets"]=ProductionVisuals.Current!=null&&ProductionVisuals.Current.ReflectionsReady;
  checks["hero_pressure_hull_is_a_closed_mesh"]=ClosedMesh(ProductionGeometry.PressureHull);
  checks["beveled_armor_has_smooth_edge_normals"]=ProductionGeometry.BeveledBox.vertexCount>24&&Array.Exists(ProductionGeometry.BeveledBox.normals,n=>Mathf.Abs(n.x)>.2f&&Mathf.Abs(n.y)>.2f);
  bool maps=true;foreach(string name in new[]{"hull_diff","hull_normal","hull_arm","alloy_arm","suit_normal","suit_arm"}){var texture=Resources.Load<Texture2D>("Surfaces/"+name);maps&=texture!=null&&texture.width==1024&&texture.mipmapCount>1;}checks["licensed_pbr_maps_are_packaged_with_mipmaps"]=maps;
  checks["pbr_shader_has_depth_and_shadow_passes"]=frontierArt.armor.FindPass("DepthOnly")>=0&&frontierArt.armor.FindPass("ShadowCaster")>=0&&frontierArt.armor.shader.isSupported;
  var alien=frontierArt.AlienWarship(null,true);checks["alien_flagship_has_a_distinct_original_silhouette"]=alien.name.Contains("crescent")&&alien.GetComponentsInChildren<MeshFilter>().Length>=4;Destroy(alien.gameObject);
  Debug.Log("PRODUCTION_METRICS seam="+seam+" samples="+seamSamples+" foundationError="+gap);
  toastTime=chapterBanner=0;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"production-city-after.png"));
  Vector3 localSurface=planet.lod.SampleSurface(colony.anchor);GalacticPoint world=OriginGlobal+planet.body.P+localSurface;RebaseOrigin(new Vector3(320,0,-160));
  checks["graded_terrain_preserves_absolute_origin_coordinates"]=WorldDistance(world,OriginGlobal+planet.body.P+planet.lod.SampleSurface(colony.anchor))<.02f;
  string legacy="{\"version\":1,\"quality\":1,\"targetFps\":60,\"difficulty\":1,\"mouseSensitivity\":1.7,\"stickSensitivity\":0.8,\"deadzone\":0.2,\"master\":0.6,\"music\":0.4,\"effects\":0.5,\"uiScale\":1.1,\"buttons\":[0,1,2,3,4,5,6,7,8,9,10,11,12,13],\"keys\":[{\"original\":119,\"replacement\":273}]}";
  var migrated=JsonUtility.FromJson<VoyagerSettings>(legacy);checks["v1_preferences_migrate_without_losing_controls"]=migrated.UpgradeAndValidate()&&migrated.version==2&&migrated.mouseSensitivity==1.7f&&migrated.Key(KeyCode.W)==KeyCode.UpArrow&&migrated.fieldOfView==65;
  string preferenceFixture=Path.Combine(EvidenceDir(),"production-settings-v2.json");CampaignFiles.Write(preferenceFixture,migrated,x=>x.Validate());checks["migrated_preferences_roundtrip_with_checksum"]=CampaignFiles.Read<VoyagerSettings>(preferenceFixture,x=>x.Validate()).mouseSensitivity==1.7f;
  var bad=new VoyagerSettings{fieldOfView=float.NaN};checks["graphics_preferences_reject_nonfinite_values"]=!bad.Validate();bad=new VoyagerSettings{version=99};checks["future_preference_versions_are_rejected"]=!bad.UpgradeAndValidate();
  preferences.antialiasing=0;preferences.bloom=0;preferences.shadowQuality=0;ApplyPreferences();
  checks["anti_aliasing_can_be_disabled_live"]=cam.GetUniversalAdditionalCameraData().antialiasing==AntialiasingMode.None;
  checks["shadow_setting_changes_actual_light"]=GameObject.Find("Warm key").GetComponent<Light>().shadows==LightShadows.None;
  var volume=GameObject.Find("Cinematic color / local procedural art").GetComponent<UnityEngine.Rendering.Volume>();Bloom actualBloom;volume.profile.TryGet(out actualBloom);bool bloomDisabled=actualBloom!=null&&actualBloom.intensity.value==0;preferences.bloom=.6f;ApplyPreferences();checks["bloom_control_changes_the_active_postprocess"]=bloomDisabled&&Mathf.Abs(actualBloom.intensity.value-.6f)<.001f;
  preferences.antialiasing=2;preferences.shadowQuality=2;ApplyPreferences();checks["smaa_can_be_enabled_live"]=cam.GetUniversalAdditionalCameraData().antialiasing==AntialiasingMode.SubpixelMorphologicalAntiAliasing;
  OpenVoyager(VoyagerPanel.Settings);settingsPage=3;preferences.BindKey(KeyCode.W,KeyCode.UpArrow);preferences.fieldOfView=85;VoyagerSecondary();checks["graphics_page_reset_preserves_keyboard_bindings"]=preferences.fieldOfView==65&&preferences.antialiasing==2&&preferences.Key(KeyCode.W)==KeyCode.UpArrow;
  string fixture=Path.Combine(EvidenceDir(),"v03-campaign-fixture.json");var old=CampaignFiles.Read<CampaignCheckpoint>(fixture,x=>x.Validate());
  if(old!=null){File.Copy(fixture,CampaignPath(1),true);bool loaded=LoadCampaign(1);bool missions=true;foreach(var record in old.surface.expeditions){SurfaceOperations.Expedition actual;if(!surface.records.TryGetValue(record.key,out actual)||actual.recorder!=record.recorder||actual.relay!=record.relay||actual.core!=record.core||actual.scans!=record.scans||actual.extracted!=record.extracted)missions=false;}checks["actual_v03_checkpoint_loads_in_v04"]=loaded&&voyager.chapter==old.campaign.chapter&&missions&&GroundActive==old.crew.onFoot;checks["v03_checkpoint_preserves_ship_and_crew_positions"]=loaded&&Vector3.Distance(Pos(player),Pos(old.ship))<.02f&&Vector3.Distance(expedition.Focus,Pos(old.crew.foot))<.02f;checks["upgraded_campaign_can_save_and_reload"]=SaveCampaign()&&LoadCampaign(1)&&CaptureCampaign().Validate();}
  else{checks["actual_v03_checkpoint_loads_in_v04"]=false;checks["v03_checkpoint_preserves_ship_and_crew_positions"]=false;checks["upgraded_campaign_can_save_and_reload"]=false;}
  StageEcology(0);paused=true;surface.Scripted=true;ecologyShowcase=false;foreach(var foe in surface.Current.foes)surface.Damage(foe,10000);yield return null;
  Vector3 direction=PlanetExpedition.SurfaceTangent((expedition.Focus-expedition.Nearest(expedition.Focus).body.P).normalized,ShipForward);expedition.ScriptWalk(direction);expedition.Sprinting=false;float phase=expedition.Motion.GaitPhase;
  for(int i=0;i<400;i++){expedition.Status(player);expedition.FootStep(.01f);}float walk=expedition.Speed,walkCycles=(expedition.Motion.GaitPhase-phase)/4;phase=expedition.Motion.GaitPhase;expedition.Sprinting=true;
  for(int i=0;i<400;i++){expedition.Status(player);expedition.FootStep(.01f);}float runCycles=(expedition.Motion.GaitPhase-phase)/4;
  checks["sprint_is_distinct_from_human_scaled_walking"]=walk>1.8f&&walk<3&&expedition.Speed>walk*1.7f&&expedition.Motion.State=="RUNNING";
  checks["walk_and_run_cadences_remain_in_authored_ranges"]=walkCycles>.7f&&walkCycles<1.8f&&runCycles>1.4f&&runCycles<3.2f;
  checks["distance_driven_foot_ik_stays_bounded"]=VoyagerState.Finite(expedition.Motion.MaxPlantSlip)&&expedition.Motion.MaxPlantSlip<.18f;
  Debug.Log("PRODUCTION_GAIT walk="+walk+" run="+expedition.Speed+" walkCycles="+walkCycles+" runCycles="+runCycles+" plantError="+expedition.Motion.MaxPlantSlip);
  preferences.fieldOfView=80;expedition.Sprinting=false;for(int i=0;i<50;i++)expedition.FootCamera(.05f);checks["field_of_view_changes_the_actual_crew_camera"]=cam.fieldOfView>77&&cam.fieldOfView<81;preferences.fieldOfView=65;
  outcome="SHIP DESTROYED / QA";Cursor.lockState=CursorLockMode.Locked;VoyagerMenuInput();checks["defeat_releases_the_pointer_and_pauses"]=voyagerPanel==VoyagerPanel.Defeat&&paused&&Cursor.visible&&Cursor.lockState==CursorLockMode.None;
  var pad=InputSystem.AddDevice<Gamepad>();Controls.TestDevice=pad;PadFeed(pad,new GamepadState());PadFeed(pad,new GamepadState().WithButton(GamepadButton.DpadDown));PadFeed(pad,new GamepadState());PadFeed(pad,new GamepadState().WithButton(GamepadButton.South));
  checks["controller_can_return_from_defeat_to_slots"]=voyagerPanel==VoyagerPanel.Home&&!campaignStarted;PadFeed(pad,new GamepadState());checks["defeat_does_not_recapture_the_home_menu"]=voyagerPanel==VoyagerPanel.Home;
  Controls.TestDevice=null;InputSystem.RemoveDevice(pad);outcome="";toastTime=0;OpenVoyager(VoyagerPanel.Home);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"production-title.png"));
  NewCampaign(3);civilization.alloy=civilization.science=10000;civilization.blueprint=3;civilization.ownedHulls|=8;civilization.paint=1;ApplyCivilHull();OpenVoyager(VoyagerPanel.Outfitter);civilizationScene.Preview(3,3,1,civilization);toastTime=0;yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"production-ship.png"));
  OpenVoyager(VoyagerPanel.Settings);settingsPage=3;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"production-settings.png"));
  checks["production_qa_preserves_private_campaign_and_settings"]=realStamp==(File.Exists(real)?File.GetLastWriteTimeUtc(real):DateTime.MinValue)&&settingsStamp==(File.Exists(settings)?File.GetLastWriteTimeUtc(settings):DateTime.MinValue);
  SaveChecks("production-runtime-checks.json",checks);yield return new WaitForSeconds(.5f);Application.Quit();
 }
 int productionFrame;string productionFolder;
 IEnumerator ProductionFrames(int count,string phase,Action<int> pose=null){
  voyagerFilmPhase=phase.Replace('-',' ').ToUpperInvariant();Debug.Log("PRODUCTION_FILM "+phase+" frame="+productionFrame);float started=Time.time;
  for(int i=0;i<count;i++){if(pose!=null)pose(i);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(productionFolder,(productionFrame++).ToString("D5")+".png"));}
  Debug.Log("PRODUCTION_FILM_TIMING "+phase+" frames="+count+" simulationSeconds="+(Time.time-started));
 }
 IEnumerator ProductionRecord(){
  yield return new WaitForSeconds(1);preferences.quality=2;preferences.antialiasing=2;ApplyPreferences();Time.captureFramerate=30;productionFolder=Path.Combine(EvidenceDir(),"production-frames");Directory.CreateDirectory(productionFolder);
  NewCampaign(3);civilization.blueprint=3;civilization.ownedHulls|=8;civilization.paint=1;ApplyCivilHull();OpenVoyager(VoyagerPanel.Outfitter);civilizationScene.Preview(3,3,1,civilization);toastTime=chapterBanner=0;
  yield return ProductionFrames(270,"pbr-ship-designs",i=>{if(i==135){civilization.blueprint=2;civilization.paint=2;ApplyCivilHull();civilizationScene.Preview(3,2,2,civilization);}});
  CloseVoyager();StageVoyagerCity();paused=true;for(int i=0;i<180;i++)yield return null;paused=false;yield return ProductionFrames(300,"stitched-terrain-and-graded-cities");
  NewCampaign(3);StageSurface();surface.Scripted=true;foreach(var foe in surface.Current.foes)surface.Damage(foe,10000);paused=false;cosmosRecording=true;invulnerable=10000;civilFilmCamera=true;civilFilmClearance=.3f;
  var planet=expedition.Nearest(expedition.Focus);Vector3 up=(expedition.Focus-planet.body.P).normalized,forward=PlanetExpedition.SurfaceTangent(up,ShipForward),side=Vector3.Cross(up,forward);
  yield return ProductionFrames(360,"walk-sprint-and-terrain-footwork",i=>{expedition.ScriptWalk(forward);expedition.Sprinting=i>=120;expedition.SetAim(expedition.Focus+forward*20);surface.ScriptAim=expedition.Focus+forward*20;civilFilmAt=expedition.Focus+side*3.8f-forward*4.2f+up*2.2f;civilFilmLook=expedition.Focus+up*.4f;});
  cosmosRecording=false;civilFilmClearance=25;expedition.ScriptWalk(Vector3.zero);StageVoyagerCity();StageVoyagerWar();paused=false;yield return ProductionFrames(240,"original-alien-ark-and-fleet-combat");
  civilFilmCamera=false;expanseRecording=false;expedition.ResetCrew();var home=expedition.planets.Find(p=>p.key=="HOME");if(home==null){player=Make(ports[0].global-OriginGlobal+Vector3.back*18000,Vector3.zero);Stream(true);home=expedition.planets.Find(p=>p.key=="HOME");}
  var body=home.body;player=Make(body.P+new Vector3(-.6f,.6f,-1).normalized*18000,Vector3.zero);FloatingOrigin();Stream(true);home=expedition.planets.Find(p=>p.key=="HOME");civilFilmCamera=true;civilFilmAt=Pos(player);civilFilmLook=home.body.P;paused=true;yield return ProductionFrames(210,"planetary-biomes-clouds-and-atmosphere");
  OpenVoyager(VoyagerPanel.Settings);settingsPage=3;yield return ProductionFrames(120,"graphics-controls-and-save-compatibility");
  Debug.Log("PRODUCTION_FILM_COMPLETE frames="+productionFrame+" width="+Screen.width+" height="+Screen.height);Time.captureFramerate=0;yield return new WaitForSeconds(.5f);Application.Quit();
 }
}
