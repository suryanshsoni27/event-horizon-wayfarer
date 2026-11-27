using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Opt-in filming only: --cosmos-record --feature-inserts. Uses real scene objects and reward paths.
public sealed partial class DefenseGame {
 int insertFrame;
 string insertFolder;
 IEnumerator InsertFrames(int count,System.Action<int> frame=null){
  for(int i=0;i<count;i++){
   if(frame!=null)frame(i);
   yield return new WaitForEndOfFrame();
   ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(insertFolder,(insertFrame++).ToString("D4")+".png"));
   yield return new WaitForSecondsRealtime(1f/12);
  }
 }
 void InsertView(Vector3 target,Vector3 outward,float distance){
  Vector3 up=(target-space.Planet.body.P).normalized;
  player=Make(target+outward.normalized*distance,Vector3.zero);
  shipRotation=desiredRotation=Quaternion.LookRotation(target-Pos(player),up);
  rocket.position=Pos(player);rocket.rotation=shipRotation;
  cam.transform.position=Pos(player)-ShipForward*12+ShipUp*4;
  cam.transform.rotation=Quaternion.LookRotation(target-cam.transform.position,up);
 }
 IEnumerator FeatureInserts(){
  yield return new WaitForSeconds(1);quality=2;ApplyQuality();cosmosRecording=true;
  insertFolder=System.IO.Path.Combine(EvidenceDir(),"feature-insert-frames");System.IO.Directory.CreateDirectory(insertFolder);
  var checks=new Dictionary<string,bool>();ResetRound();paused=true;chartOpen=true;chartSystem=0;chartPort=0;
  yield return InsertFrames(48);chartSystem=4;chartPort=2;yield return InsertFrames(24);
  checks["atlas_catalogue_visible"]=catalog.Count==32&&ports.Count==128;chartOpen=false;
  space.Tick(0,false,Pos(player));var station=space.Orbitals.Find(o=>o.station);health=42;
  yield return InsertFrames(72,i=>{space.Tick(1f/12,true,Pos(player));Vector3 at=station.root.position;InsertView(at,new Vector3(.45f,.65f,1),130);if(i==25)checks["station_repair"]=space.Service(Pos(player),0)&&health==100;});
  var satellite=space.Orbitals.Find(o=>o.name.StartsWith("KEPLER 1"));
  yield return InsertFrames(60,i=>{space.Tick(1f/12,true,Pos(player));InsertView(satellite.root.position,(satellite.root.position-space.Planet.body.P).normalized+Vector3.forward*.65f,58);if(i==24)checks["satellite_scan"]=space.Service(Pos(player),0)&&satellite.scanned;});
  var wreck=space.Orbitals.Find(o=>o.name.StartsWith("DERELICT"));
  yield return InsertFrames(48,i=>{space.Tick(1f/12,true,Pos(player));InsertView(wreck.root.position,new Vector3(-.6f,.35f,1),95);});
  checks["derelict_rendered"]=wreck.root.gameObject.activeInHierarchy;
  var rock=space.Rocks.Find(r=>Vector3.Distance(space.World(r),station.root.position)>300);
  Vector3 rockAt=space.World(rock),outward=(Pos(player)-rockAt).normalized;
  InsertView(rockAt,outward,rock.radius+12);int before=space.Mined,partsBefore=salvageCount;foreach(var a in agents){a.status=2;a.respawn=999;a.root.gameObject.SetActive(false);}
  yield return InsertFrames(96,i=>{
   rockAt=space.World(rock);InsertView(rockAt,outward,rock.radius+12);
   if(i==8||i==34)Fire(Pos(player)+ShipForward*2,ShipForward*18);
   playerAction=-Gravity(Pos(player));for(int step=0;step<8;step++)PhysicsTick(.01f);
   space.Tick(1f/12,true,Pos(player));
  });
  checks["native_projectiles_mine_asteroid"]=space.Mined==before+1&&salvageCount==partsBefore+2;
  paused=true;upgradeOpen=true;yield return InsertFrames(36);checks["earned_parts_fit_engine"]=Fit(0);yield return InsertFrames(24);upgradeOpen=false;
  var comet=GameObject.Find("Passing icy debris / artistic comet tail");
  yield return InsertFrames(60,i=>{space.Tick(1f/12,true,Pos(player));InsertView(comet.transform.position,new Vector3(.1f,.2f,1),220);});
  checks["comet_and_tail_visible"]=comet.activeInHierarchy;
  var photos=GetComponent<HelmetPhotoUI>();photos.AddLocalTestProfile(System.IO.Path.Combine(EvidenceDir(),"tour-synthetic-portrait.png"),true);
  yield return InsertFrames(72);checks["local_crew_photo_preview"]=photos.RosterCount==1&&photos.CachedCount==1;
  SaveChecks("feature-insert-checks.json",checks);Debug.Log("FEATURE_INSERTS frames="+insertFrame+" "+SurfaceJSON(checks));
  yield return new WaitForSeconds(.5f);Application.Quit();
 }
}
