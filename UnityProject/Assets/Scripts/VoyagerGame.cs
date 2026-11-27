using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed partial class DefenseGame {
 enum VoyagerPanel {None,Home,Pause,Settings,Contracts,Outfitter,Colonies,Chapters,Defeat,HeadPilot}
 VoyagerState voyager;CivilizationModel legacyCivilization;
 VoyagerSettings preferences;
 VoyagerPanel voyagerPanel;
 bool voyagerQA,voyagerBenchmark,voyagerRecord,voyagerRestoring,campaignStarted,interactionToggle,aimToggle;
 int newCampaignScenario;bool newGamePlus;
 int voyagerSelection,settingsPage,moduleCategory,slotSelection=1,overwriteSlot,rebindAction=-1,rebindKey=-1;
 float rebindReadyAt,checkpointClock,chapterBanner=6,hitFeedback,lastDamageAt,aimBlend;
 string saveStatus="Progress saves during exploration.",voyagerToast="";
 float toastTime;
 Transform voyagerModuleRoot;
 VoyagerAudio voyagerAudio;
 readonly List<float> frameTimes=new List<float>(2048);
 public bool VoyagerActive{get{return voyager!=null;}}
 public bool VoyagerMenuOpen{get{return voyager!=null&&voyagerPanel!=VoyagerPanel.None;}}
 public float MouseLookX{get{return preferences==null?1:preferences.mouseSensitivity*(preferences.invertX?-1:1);}}
 public float MouseLookY{get{return preferences==null?1:preferences.mouseSensitivity*(preferences.invertY?-1:1);}}
 public int RelayWaves{get{return voyager==null?3:3+(voyager.chapter>=6?1:0)+(voyager.chapter>=10?1:0);}}
 public float RelayDuration{get{return RelayWaves*8;}}
 public float ThreatHealth{get{return voyager==null?1:CampaignChapters.EnemyHealth(voyager.chapter,preferences.difficulty);}}
 float VoyagerHullBonus{get{return voyager==null?0:voyager.HullBonus;}}
 public bool KeyHeld(KeyCode original){return GameKey(original);}
 public bool KeyPressed(KeyCode original){return GameKeyDown(original);}
 string CampaignPath(int slot){return Path.Combine(voyagerQA||voyagerBenchmark||voyagerRecord?EvidenceDir():Application.persistentDataPath,"wayfarer-campaign-slot-"+slot+".json");}
 string SettingsPath{get{return Path.Combine(Application.persistentDataPath,"wayfarer-settings-v1.json");}}
 void InitializeVoyager(string[] args){
  voyagerQA=Array.IndexOf(args,"--voyager-qa")>=0;voyagerBenchmark=Array.IndexOf(args,"--voyager-benchmark")>=0;voyagerRecord=Array.IndexOf(args,"--voyager-record")>=0;
  if(legacyUniverse||qa&&!voyagerQA&&!voyagerBenchmark&&!voyagerRecord||civilization==null)return;
  preferences=(!qa&&!automatedPlayback?CampaignFiles.Read<VoyagerSettings>(SettingsPath,x=>x.UpgradeAndValidate()):null)??new VoyagerSettings();
  legacyCivilization=!qa&&!automatedPlayback?ReadCivilization(CivilizationPath):null;voyager=new VoyagerState();foreach(var sectorEntry in sectors.Values)CreateStellarCorona(sectorEntry.root.transform.Find("Sun / unlandable light source"));Controls.Settings=preferences;ApplyPreferences();
  civilOpen=false;civilizationScene.HidePreview();voyagerPanel=VoyagerPanel.Home;paused=true;
  voyagerAudio=gameObject.AddComponent<VoyagerAudio>();voyagerAudio.Initialize(preferences);
  for(int i=1;i<=3;i++)if(ReadCampaign(i)!=null){slotSelection=i;break;}
  if(voyagerQA)StartCoroutine(Array.IndexOf(args,"--ecology-qa")>=0?EcologyChecks():Array.IndexOf(args,"--head-pilot-qa")>=0?HeadPilotChecks():Array.IndexOf(args,"--production-qa")>=0?ProductionChecks():VoyagerChecks());
  else if(voyagerBenchmark)StartCoroutine(VoyagerBenchmark());
  else if(voyagerRecord)StartCoroutine(Array.IndexOf(args,"--ecology-record")>=0?EcologyRecord():Array.IndexOf(args,"--production-record")>=0?ProductionRecord():VoyagerRecord());
 }
 CampaignCheckpoint ReadCampaign(int slot){return CampaignFiles.Read<CampaignCheckpoint>(CampaignPath(slot),x=>x.Validate());}
 CampaignCheckpoint CaptureCampaign(){
  var checkpoint=new CampaignCheckpoint{slot=voyager.slot,savedUtc=DateTime.UtcNow.ToString("O"),civilization=civilization,campaign=voyager,
   ship=player,rotation=shipRotation,health=health,elapsed=simSeconds,cellX=cellX,cellY=cellY,cellZ=cellZ,score=score,
   tiers=(int[])tiers.Clone(),salvageCount=salvageCount,wheels=wheelsRequested,wheelDeployment=wheelDeployment,
   roverYaw=roverCameraYaw,roverPitch=roverCameraPitch,flightAssist=flightAssist,chartSystem=chartSystem,chartPort=chartPort,
   cruising=cruising,landingAssist=landingAssist,cruiseBraking=cruiseBraking,throttle=throttle,routeLeg=routeLeg,destination=destinationName,routeTarget=new CampaignCheckpoint.SavedPoint{x=routeGlobal.x,y=routeGlobal.y,z=routeGlobal.z},crew=expedition.Capture(),surface=surface.Capture(),space=space.Capture(),hits=hits,arrivals=arrivalCount,quality=quality};
  foreach(var c in catalog)checkpoint.catalogue.Add(new CampaignCheckpoint.CatalogueEntry{name=c.name,key=c.key,x=c.location.x,y=c.location.y,z=c.location.z});
  foreach(var point in routeWaypoints)checkpoint.route.Add(new CampaignCheckpoint.SavedPoint{x=point.x,y=point.y,z=point.z});
  foreach(var r in civilRockets)if(r.state.captured==0)checkpoint.rockets.Add(new CampaignCheckpoint.SavedRocket{state=r.state,key=r.key,age=r.age,altitude=r.maxAltitude,credited=r.credited});
  return checkpoint;
 }
 bool SaveCampaign(){
  if(voyager==null||!campaignStarted||voyagerRestoring||health<=0||outcome!=""||inTidal)return false;
  try{var data=CaptureCampaign();CampaignFiles.Write(CampaignPath(voyager.slot),data,x=>x.Validate());checkpointClock=0;slotsCached=false;saveStatus="Slot "+voyager.slot+" saved "+DateTime.Now.ToString("HH:mm");return true;}
  catch(Exception e){saveStatus="Save unavailable. Your last checkpoint is preserved.";Toast(saveStatus);Debug.LogWarning("Campaign save failed: "+e.GetType().Name);return false;}
 }
 void RebuildCivilPresentation(){
  if(civilizationScene!=null)Destroy(civilizationScene);
  frontierArt=new FrontierArt();civilizationScene=gameObject.AddComponent<CivilizationScene>();civilizationScene.Initialize(this,frontierArt,expedition);
  civilPaint=civilization.paint;ApplyCivilHull();civilizationScene.Sync(civilization,GroundActive?expedition.Focus:Pos(player));
 }
 bool LoadCampaign(int slot){
  var data=ReadCampaign(slot);if(data==null){Toast("No valid checkpoint in slot "+slot);return false;}
  voyagerRestoring=true;ClearContractViews();ResetRound();civilization=data.civilization;voyager=data.campaign;
  cellX=data.cellX;cellY=data.cellY;cellZ=data.cellZ;catalog=new List<StarSystem>();ports=new List<Port>();
  for(int i=0;i<data.catalogue.Count;i++){var c=data.catalogue[i];AddCatalogSystem(i,c.name,new GalacticPoint(c.x,c.y,c.z),c.key);}
  player=data.ship;shipRotation=desiredRotation=data.rotation;chartSystem=data.chartSystem;chartPort=data.chartPort;
  tiers=(int[])data.tiers.Clone();salvageCount=data.salvageCount;score=data.score;health=data.health;simSeconds=data.elapsed;hits=data.hits;arrivalCount=data.arrivals;
  wheelsRequested=data.wheels;wheelDeployment=data.wheelDeployment;roverCameraYaw=data.roverYaw;roverCameraPitch=data.roverPitch;roverBrake=true;
  flightAssist=data.flightAssist;throttle=data.throttle;cruising=data.cruising;landingAssist=data.landingAssist;cruiseBraking=data.cruiseBraking||!data.cruising&&Vel(player).magnitude>100;routeLeg=data.routeLeg;routeGlobal=new GalacticPoint(data.routeTarget.x,data.routeTarget.y,data.routeTarget.z);destinationName=data.destination;routeWaypoints.Clear();foreach(var point in data.route)routeWaypoints.Add(new GalacticPoint(point.x,point.y,point.z));playerAction=Vector3.zero;Stream(true);
  surface.Restore(data.surface);space.Restore(data.space);expedition.Restore(data.crew);expedition.Status(player);
  BuildModules();RebuildCivilPresentation();foreach(var flight in data.rockets)RestoreRocket(flight);rocket.position=Pos(player);rocket.rotation=shipRotation;RoverPose(0);
  cam.transform.position=(GroundActive?expedition.Focus:Pos(player))-ShipForward*8+ShipUp*3;
  cam.transform.rotation=Quaternion.LookRotation(ShipForward,ShipUp);expedition.Status(player);surface.Sync();
  outcome="";inTidal=false;invulnerable=3;voyagerRestoring=false;campaignStarted=true;voyagerPanel=VoyagerPanel.None;civilOpen=false;
  slotSelection=slot;chapterBanner=5;ResumeFlight();Toast("Checkpoint restored / Chapter "+(voyager.chapter+1));return true;
 }
 void RestoreRocket(CampaignCheckpoint.SavedRocket saved){var planet=expedition.planets.Find(p=>p.key==saved.key);if(planet==null)return;var root=frontierArt.Rocket(null);root.position=Pos(saved.state);root.rotation=Quaternion.FromToRotation(Vector3.up,(root.position-planet.body.P).normalized);var flame=new GameObject("Rocket / restored exhaust").AddComponent<LineRenderer>();flame.transform.SetParent(root,false);flame.useWorldSpace=false;flame.positionCount=2;flame.SetPositions(new[]{Vector3.up,Vector3.down*12});flame.startWidth=1.2f;flame.endWidth=4;flame.sharedMaterial=frontierArt.beamOrange;civilRockets.Add(new CivilRocket{state=saved.state,key=saved.key,age=saved.age,maxAltitude=saved.altitude,credited=saved.credited,root=root,flame=flame});}
 void NewCampaign(int slot,int scenario=0){
  voyagerRestoring=true;ClearContractViews();ResetRound();civilization=new CivilizationModel();voyager=new VoyagerState{slot=slot,scenario=scenario,seed=7319+scenario*311,legacy=newGamePlus?1:0};
  if(scenario==1){civilization.alloy=550;foreach(var f in civilization.factions)f.relation-=15;}
  if(scenario==2){civilization.alloy=500;civilization.energy=250;}
  RebuildCivilPresentation();health=HullMaximum;voyagerRestoring=false;campaignStarted=true;slotSelection=slot;
  voyagerPanel=VoyagerPanel.None;overwriteSlot=0;chapterBanner=8;landingAssist=true;throttle=0;paused=false;ResumeFlight();SaveCampaign();
  Toast("Chapter 1 / First Light. VTOL descent engaged; K / D-pad up cancels.");
 }
 void ApplyPreferences(){
  if(preferences==null)return;quality=preferences.quality;ApplyQuality();QualitySettings.vSyncCount=0;Application.targetFrameRate=preferences.targetFps;
  var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
  if(pipeline!=null){pipeline.renderScale=quality==0?.75f:quality==1?.9f:1;pipeline.shadowDistance=quality==0?140:quality==1?240:360;}
  if(ProductionVisuals.Current!=null)ProductionVisuals.Current.Apply(cam,preferences);
  if(!qa&&!automatedPlayback)try{CampaignFiles.Write(SettingsPath,preferences,x=>x.UpgradeAndValidate());}catch(Exception){Toast("Settings could not be saved.");}
 }
 void OpenVoyager(VoyagerPanel panel){
  if(voyager==null)return;SuspendHeadPilot();voyagerPanel=panel;voyagerSelection=0;civilOpen=false;civilPlacing=false;chartOpen=upgradeOpen=false;
  civilizationScene.HideGhost();civilizationScene.HidePreview();if(surface!=null)surface.Journal=false;
  paused=true;if(voyagerAudio!=null)voyagerAudio.Silence();releaseFire=true;playerAction=Vector3.zero;roverDrive=0;Controls.SuppressUntilNeutral();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
  StopRumble();if(panel==VoyagerPanel.Contracts)EnsureContracts();
 }
 void CloseVoyager(){civilizationScene.HidePreview();voyagerPanel=VoyagerPanel.None;overwriteSlot=0;rebindAction=rebindKey=-1;if(campaignStarted)ResumeFlight();else OpenVoyager(VoyagerPanel.Home);}
 bool VoyagerMenuInput(){
  if(voyager==null)return false;
  if(preferences.toggleInteract&&((!automatedPlayback&&Input.GetKeyDown(preferences.Key(KeyCode.E)))||PadDown(PilotInput.Button.R1)))interactionToggle=!interactionToggle;
  if(preferences.toggleAim&&(!automatedPlayback&&Input.GetMouseButtonDown(1)||Controls.L2>.6f&&aimBlend<=.6f))aimToggle=!aimToggle;aimBlend=Controls.L2;
  if(campaignStarted&&(outcome!=""||inTidal&&tidalTime>2.8f)&&voyagerPanel!=VoyagerPanel.Defeat)OpenVoyager(VoyagerPanel.Defeat);
  if(voyagerPanel==VoyagerPanel.None){
   if(GameKeyDown(KeyCode.F5)){Toast(SaveCampaign()?"Checkpoint saved":saveStatus);return true;}
   if(GameKeyDown(KeyCode.F9)){OpenVoyager(VoyagerPanel.Pause);return true;}
   if(GameKeyDown(KeyCode.Tab)){OpenVoyager(VoyagerPanel.Contracts);return true;}
   if(!civilOpen&&!chartOpen&&!upgradeOpen&&(surface==null||!surface.Journal)&&(GameKeyDown(KeyCode.Escape)||GameKeyDown(KeyCode.F1))){OpenVoyager(VoyagerPanel.Pause);return true;}
   return false;
  }
  if(voyagerPanel==VoyagerPanel.Home)civilizationScene.AnimatePreview(Time.unscaledDeltaTime,Vector2.zero);if(voyagerPanel==VoyagerPanel.Outfitter)civilizationScene.AnimatePreview(Time.unscaledDeltaTime,Controls.Look);if(rebindAction>=0||rebindKey>=0)return true;
  if(GameKeyDown(KeyCode.Escape)||PadDown(PilotInput.Button.Circle)){
   if(voyagerPanel==VoyagerPanel.Home)return true;if(voyagerPanel==VoyagerPanel.Defeat){campaignStarted=false;OpenVoyager(VoyagerPanel.Home);return true;}
   if(voyagerPanel==VoyagerPanel.Pause)CloseVoyager();else OpenVoyager(campaignStarted?VoyagerPanel.Pause:VoyagerPanel.Home);return true;
  }
  if(GameKeyDown(KeyCode.DownArrow)||PadDown(PilotInput.Button.Down))voyagerSelection++;
  if(GameKeyDown(KeyCode.UpArrow)||PadDown(PilotInput.Button.Up))voyagerSelection--;
  int direction=(GameKeyDown(KeyCode.RightArrow)||PadDown(PilotInput.Button.Right)||PadDown(PilotInput.Button.R1)?1:0)-(GameKeyDown(KeyCode.LeftArrow)||PadDown(PilotInput.Button.Left)||PadDown(PilotInput.Button.L1)?1:0);
  if(voyagerPanel==VoyagerPanel.Home){slotSelection=(slotSelection-1+direction+3)%3+1;if(direction!=0)overwriteSlot=0;voyagerSelection=(voyagerSelection+5)%5;}
  if(voyagerPanel==VoyagerPanel.Defeat)voyagerSelection=(voyagerSelection+2)%2;
  if(voyagerPanel==VoyagerPanel.HeadPilot){voyagerSelection=(voyagerSelection+HeadPilotRows)%HeadPilotRows;if(direction!=0)HeadPilotAction(voyagerSelection,direction);}
  if(voyagerPanel==VoyagerPanel.Pause)voyagerSelection=(voyagerSelection+9)%9;
  if(voyagerPanel==VoyagerPanel.Outfitter){moduleCategory=(moduleCategory+direction+4)%4;voyagerSelection=(voyagerSelection+3)%3;}
  if(voyagerPanel==VoyagerPanel.Settings){voyagerSelection=(voyagerSelection+SettingsRows())%SettingsRows();if(direction!=0)AdjustSetting(voyagerSelection,direction);}
  if(voyagerPanel==VoyagerPanel.Contracts){var board=EnsureContracts();if(board.Count>0)voyagerSelection=(voyagerSelection+board.Count)%board.Count;}
  if(voyagerPanel==VoyagerPanel.Colonies){int count=Mathf.Max(1,civilization.colonies.Count);voyagerSelection=(voyagerSelection+count)%count;if(direction!=0&&civilization.colonies.Count>0){var p=voyager.Policy(civilization.colonies[voyagerSelection].port);p.focus=(p.focus+direction+4)%4;}}
  if(GameKeyDown(KeyCode.Return)||PadDown(PilotInput.Button.Cross)){VoyagerPrimary();Controls.SuppressUntilNeutral();}
  if(PadDown(PilotInput.Button.Square)||GameKeyDown(KeyCode.Delete))VoyagerSecondary();
  if(PadDown(PilotInput.Button.Triangle)||GameKeyDown(KeyCode.G)){
   if(voyagerPanel==VoyagerPanel.Home){newCampaignScenario=(newCampaignScenario+1)%3;Toast("New expedition scenario / "+new[]{"Frontier","Conflict","Scarcity"}[newCampaignScenario]);}
   if(voyagerPanel==VoyagerPanel.Settings)settingsPage=(settingsPage+1)%4;
   if(voyagerPanel==VoyagerPanel.Colonies)CreateSupplyRoute();
   if(voyagerPanel==VoyagerPanel.Chapters)BeginFlagshipAssault();
  }
  return true;
 }
 void VoyagerPrimary(){
  if(voyagerPanel==VoyagerPanel.HeadPilot){HeadPilotAction(voyagerSelection);return;}
  if(voyagerPanel==VoyagerPanel.Home){
   if(voyagerSelection==0){if(ReadCampaign(slotSelection)==null&&legacyCivilization!=null){var imported=JsonUtility.FromJson<CivilizationModel>(JsonUtility.ToJson(legacyCivilization));NewCampaign(slotSelection);civilization=imported;RebuildCivilPresentation();health=HullMaximum;SaveCampaign();Toast("Previous civilization imported. The original file is preserved.");}else LoadCampaign(slotSelection);}
   else if(voyagerSelection==1){if(ReadCampaign(slotSelection)!=null&&overwriteSlot!=slotSelection){overwriteSlot=slotSelection;Toast("This slot has a campaign. Select New campaign again to replace it.");}else NewCampaign(slotSelection,newCampaignScenario);}
   else if(voyagerSelection==2)OpenVoyager(VoyagerPanel.Settings);
   else if(voyagerSelection==3){OpenVoyager(VoyagerPanel.Chapters);}
   else Application.Quit();return;
  }
  if(voyagerPanel==VoyagerPanel.Pause){
   switch(voyagerSelection){case 0:CloseVoyager();break;case 1:Toast(SaveCampaign()?"Checkpoint saved":saveStatus);break;case 2:LoadCampaign(voyager.slot);break;case 3:OpenVoyager(VoyagerPanel.Contracts);break;case 4:OpenVoyager(VoyagerPanel.Outfitter);civilizationScene.Preview(3,civilization.blueprint,civilization.paint,civilization);break;case 5:OpenVoyager(VoyagerPanel.Colonies);break;case 6:OpenVoyager(VoyagerPanel.Chapters);break;case 7:OpenVoyager(VoyagerPanel.Settings);break;case 8:if(SaveCampaign())Application.Quit();break;}return;
  }
  if(voyagerPanel==VoyagerPanel.Defeat){if(voyagerSelection==0){if(!LoadCampaign(voyager.slot))OpenVoyager(VoyagerPanel.Home);}else{campaignStarted=false;OpenVoyager(VoyagerPanel.Home);}return;}
  if(voyagerPanel==VoyagerPanel.Settings){AdjustSetting(voyagerSelection,1);return;}
  if(voyagerPanel==VoyagerPanel.Contracts){var board=EnsureContracts();if(board.Count==0)return;var c=board[Mathf.Clamp(voyagerSelection,0,board.Count-1)];string why;if(!c.accepted){if(voyager.Accept(c,out why)){AnchorContract(c);Toast("Contract accepted / "+c.title);SaveCampaign();}else Toast(why);}else NavigateContract(c);return;}
  if(voyagerPanel==VoyagerPanel.Outfitter){InstallModule(moduleCategory,voyagerSelection);return;}
  if(voyagerPanel==VoyagerPanel.Colonies){if(voyager.crisis.active){string why;Toast(voyager.ResolveCrisis(civilization,out why)?"Relief delivered / crisis resolved":why);}else if(civilization.colonies.Count>0&&voyager.food>=15){voyager.food-=15;voyager.Policy(civilization.colonies[Mathf.Clamp(voyagerSelection,0,civilization.colonies.Count-1)].port).food+=15;Toast("Emergency provisions delivered / 15 rations");}else Toast("No emergency rations available. Use a provisioning policy or supply route.");SaveCampaign();return;}
  if(voyagerPanel==VoyagerPanel.Chapters&&campaignStarted){CloseVoyager();return;}
 }
 void VoyagerSecondary(){
  if(voyagerPanel==VoyagerPanel.Settings){if(settingsPage==3){preferences.antialiasing=2;preferences.bloom=.25f;preferences.fieldOfView=65;preferences.shadowQuality=2;preferences.quality=1;preferences.targetFps=60;}else if(settingsPage==1)preferences.keys.Clear();else if(settingsPage==2)preferences.buttons=new VoyagerSettings().buttons;else{var reset=new VoyagerSettings();reset.keys=preferences.keys;reset.buttons=preferences.buttons;preferences=reset;Controls.Settings=preferences;}ApplyPreferences();Toast("Settings page reset to defaults");return;}
  if(voyagerPanel==VoyagerPanel.Home){if(!slotsCached)RefreshSlotCache();if(Array.Exists(slotCache,x=>x!=null&&x.campaign.campaignComplete)){newGamePlus=!newGamePlus;Toast(newGamePlus?"New Game Plus / legacy badge enabled":"Standard new expedition selected");}else Toast("Complete the twelve chapters to unlock a legacy playthrough.");return;}
  if(voyagerPanel==VoyagerPanel.Contracts){var board=EnsureContracts();if(board.Count==0)return;var c=board[Mathf.Clamp(voyagerSelection,0,board.Count-1)];if(c.accepted&&!c.completed){if(c.carrying){voyager.cargo=Mathf.Max(0,voyager.cargo-8);c.carrying=false;}c.failed=true;Toast("Contract abandoned; no reward awarded.");ClearContractViews();SaveCampaign();}}
  else if(voyagerPanel==VoyagerPanel.Colonies)NegotiatePeace();
 }
 void Toast(string message){voyagerToast=message;toastTime=6;Notice(message);}
 public void DecoratePreview(Transform root){if(voyagerModuleRoot==null)return;var copy=Instantiate(voyagerModuleRoot.gameObject).transform;copy.SetParent(root,false);foreach(var t in copy.GetComponentsInChildren<Transform>())t.gameObject.layer=28;}
 public float CameraFov(float baseline){return baseline+(preferences==null?0:preferences.fieldOfView-65);}
 public float CityAltitude(Vector3 point,float native){return voyager!=null&&civilizationScene!=null?civilizationScene.FloorAltitude(point,native):native;}
 public float EffectsVolume{get{return preferences==null?1:preferences.effects;}}
 void StopRumble(){try{if(Gamepad.current!=null)Gamepad.current.SetMotorSpeeds(0,0);}catch(Exception){}}
 void Rumble(float low,float high){if(preferences==null||!preferences.rumble||!Controls.Connected)return;try{Gamepad.current.SetMotorSpeeds(Mathf.Clamp01(low),Mathf.Clamp01(high));}catch(Exception){}}
 void VoyagerTick(float dt){
  if(voyager==null)return;float elapsed=Mathf.Min(dt,.1f);toastTime=Mathf.Max(0,toastTime-dt);chapterBanner=Mathf.Max(0,chapterBanner-dt);hitFeedback=Mathf.Max(0,hitFeedback-dt);
  if(hitFeedback<=0)StopRumble();else if(preferences.motion)cam.transform.position+=cam.transform.right*(Mathf.Sin(Time.unscaledTime*58)*hitFeedback*.04f);
  if(voyagerAudio!=null)voyagerAudio.Tick(elapsed,!paused&&campaignStarted,GroundActive,expedition.Sprinting,expedition.Speed,RoverDeployed,Vel(player).magnitude,throttle,expedition.Density,civilization.wars.Exists(w=>!w.resolved&&w.travel<=0),cam.transform.position);
  if(!campaignStarted||paused||outcome!=""||inTidal)return;
  if(frameTimes.Count>=3600)frameTimes.RemoveRange(0,600);frameTimes.Add(Time.unscaledDeltaTime*1000);
  VoyagerVisualTick(elapsed);voyager.TickEconomy(elapsed,civilization);VoyagerContractsTick(elapsed);UpdateJourney();
  foreach(var w in civilization.wars){if(w.flagship&&w.resolved&&w.won)voyager.flagshipDefeated=true;if(w.resolved&&w.won&&!voyager.recordedWars.Contains(w.id)){voyager.recordedWars.Add(w.id);if(w.raid)voyager.defensesWon++;}}
  if(CampaignChapters.Advance(voyager,civilization)){chapterBanner=8;SaveCampaign();Toast(voyager.campaignComplete?"Campaign complete / Light of Civilization":"CHAPTER "+(voyager.chapter+1)+" / "+CampaignChapters.Titles[voyager.chapter]);if(voyagerAudio!=null)voyagerAudio.Cue("chapter",cam.transform.position);if(voyager.campaignComplete)OpenVoyager(VoyagerPanel.Chapters);}
  checkpointClock+=elapsed;if(checkpointClock>=25)SaveCampaign();
 }
 void UpdateJourney(){
  if(expedition.Alt(Pos(player))<1.2f&&Vel(player).magnitude<1.5f)voyager.landed=true;
  if(RoverReady)voyager.tires=true;if(GroundActive)voyager.walked=true;
  foreach(var record in surface.records.Values){if(record.recorder)voyager.recorder=true;if(record.extracted)voyager.extracted=true;}
  if(civilization.Count(0)>0)voyager.founded=true;if(civilization.launches>0)voyager.launched=true;
  var p=expedition.Nearest(GroundActive?expedition.Focus:Pos(player));var port=p==null?null:ports.Find(x=>x.key==p.key);
  if(port!=null&&(GroundActive||expedition.Alt(Pos(player))<1400)){int id=port.system*4+port.index;if(!voyager.visited.Contains(id))voyager.visited.Add(id);}
  if(civilization.wars.Exists(w=>w.raid&&w.resolved&&w.won))voyager.firstDefense=true;
 }
 bool InstallModule(int category,int choice){
  if(cruising||RoverDeployed||Vel(player).magnitude>3){Toast("Park and retract tires before changing ship modules.");return false;}
  string why;if(!voyager.CanInstall(category,choice,civilization.blueprint,out why)){Toast(why);return false;}
  int index=category*3+choice;bool owned=choice==0||(voyager.unlockedModules&(1<<index))!=0;
  if(!owned&&!civilization.Pay(VoyagerState.ModuleCost[index],10,choice==2?25:15)){Toast("Insufficient alloy, energy or science for this module.");return false;}
  float fraction=health/HullMaximum;voyager.unlockedModules|=1<<index;voyager.SetModule(category,choice);health=fraction*HullMaximum;voyager.shield=Mathf.Min(voyager.shield,voyager.ShieldMax);
  ApplyCivilHull();civilizationScene.Preview(3,civilization.blueprint,civilization.paint,civilization);SaveCampaign();Toast(VoyagerState.ModuleNames[index]+" installed");return true;
 }
 float VoyagerDamage(float amount){
  if(voyager==null)return amount;
  amount*=CampaignChapters.EnemyDamage(voyager.chapter,preferences.difficulty);lastDamageAt=voyager.time;
  voyager.shieldDelay=6;float absorbed=Mathf.Min(voyager.shield,amount);voyager.shield-=absorbed;voyager.heat=Mathf.Max(30,voyager.heat);hitFeedback=.22f;Rumble(.45f,.65f);
  if(voyagerAudio!=null)voyagerAudio.Cue(absorbed>0?"shield":"impact",cam.transform.position);return amount-absorbed;
 }
 public Vector3 AssistedAim(Vector3 origin,Vector3 aim,bool ground){
  if(voyager==null||!preferences.aimAssist||!PadMode)return aim;
  float best=ground?5:4;Vector3 chosen=aim;
  if(ground&&surface.Current!=null){foreach(var f in surface.Current.foes)if(!f.dead){Vector3 delta=surface.World(f)+surface.Up(surface.World(f))*(f.kind==2?2:1)-origin;float angle=Vector3.Angle(aim,delta);if(angle<best&&delta.magnitude<85){best=angle;chosen=delta.normalized;}}}
  else foreach(var unit in civilUnits)if(unit.enemy&&unit.HP>0){Vector3 delta=Pos(unit.state)-origin;float angle=Vector3.Angle(aim,delta);if(angle<best&&delta.magnitude<1000){best=angle;chosen=delta.normalized;}}
  return Vector3.RotateTowards(aim,chosen,.6f*Mathf.Deg2Rad,0);
 }
 void ApplyVoyagerModules(){
  if(voyager==null||civilDesign==null)return;
  if(voyagerModuleRoot!=null)Destroy(voyagerModuleRoot.gameObject);
  voyagerModuleRoot=new GameObject("Voyager / installed modules").transform;voyagerModuleRoot.SetParent(civilDesign,false);
  for(int side=-1;side<=1;side+=2){
   if(voyager.weapon>0){var gun=Shape("Module / "+VoyagerState.ModuleNames[3+voyager.weapon],PrimitiveType.Cylinder,new Vector3(side*1.2f,.05f,.25f),new Vector3(.12f,voyager.weapon==1?.95f:.5f,.12f),new Color(.22f,.35f,.42f),voyagerModuleRoot);gun.localRotation=Quaternion.Euler(90,0,0);}
   if(voyager.drive>0)Shape("Module / engine heat sink",PrimitiveType.Cube,new Vector3(side*.85f,.25f,-1),new Vector3(.35f,.22f,.8f),new Color(.12f,.28f,.35f),voyagerModuleRoot);
   if(voyager.hold==1)Shape("Module / freight canister",PrimitiveType.Cube,new Vector3(side*1.0f,-.3f,-.5f),new Vector3(.45f,.5f,1),new Color(.5f,.26f,.10f),voyagerModuleRoot);
  }
 }
 void CreateSupplyRoute(){
  if(civilization.colonies.Count<2){Toast("Establish two colonies before assigning a supply route.");return;}
  var from=civilization.colonies[Mathf.Clamp(voyagerSelection,0,civilization.colonies.Count-1)];var to=civilization.colonies.Find(x=>x.port!=from.port);
  var existing=voyager.routes.Find(x=>x.source==from.port&&x.target==to.port);
  if(existing!=null){existing.enabled=!existing.enabled;Toast(existing.enabled?"Supply route resumed":"Supply route paused");}
  else if(voyager.routes.Count<24&&civilization.Pay(80,30,10)){voyager.routes.Add(new VoyagerState.SupplyRoute{source=from.port,target=to.port});Toast("Autonomous provision route established / 90 second trips");}
  else Toast("Route requires 80 alloy / 30 energy / 10 science");SaveCampaign();
 }
 void NegotiatePeace(){
  var w=civilization.wars.Find(x=>!x.resolved&&!x.flagship);if(w==null){Toast("No conventional war is active.");return;}
  if(!civilization.Pay(160,60,30)){Toast("Peace offer needs 160 alloy / 60 energy / 30 science");return;}
  civilization.fleet=Mathf.Min(24,civilization.fleet+CivilizationModel.Living(w.friendly));w.resolved=true;w.won=false;
  civilization.factions[w.enemy].relation=25;Toast("Ceasefire agreed / surviving ships returned to reserve");SaveCampaign();
 }
 void BeginFlagshipAssault(){
  if(!campaignStarted||voyager.chapter!=11||voyager.flagshipDefeated){Toast("The final assault unlocks in Chapter 12.");return;}
  if(civilization.dysonBuilt<8||civilization.fleet<4){Toast("Prepare eight Dyson sectors and four reserve warships.");return;}
  if(civilization.wars.Exists(w=>w.flagship&&!w.resolved)){CivilNavigateWar();return;}
  string why;var war=civilization.Strike(2,civilization.colonies[0].port,false,out why);
  if(war==null){Toast(why);return;}war.flagship=true;war.hostile[0]=1200;for(int i=1;i<war.hostile.Length;i++)war.hostile[i]*=ThreatHealth;war.scaled=true;voyager.flagshipWar=war.id;InitializeCivilWarRoute(war);
  Toast("THE ALIEN ARK / final assault dispatched. Join the orbital front.");SaveCampaign();
 }
}
