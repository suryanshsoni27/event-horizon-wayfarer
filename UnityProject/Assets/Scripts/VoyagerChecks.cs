using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Profiling;

public sealed partial class DefenseGame {
 IEnumerator VoyagerChecks(){
  yield return new WaitForSeconds(1);
  var checks=new Dictionary<string,bool>();
  string real=Path.Combine(Application.persistentDataPath,"wayfarer-campaign-slot-1.json");
  DateTime realStamp=File.Exists(real)?File.GetLastWriteTimeUtc(real):DateTime.MinValue;
  NewCampaign(1);paused=true;chapterBanner=0;
  checks["new_campaign_uses_normal_resources"]=civilization.alloy==650&&civilization.energy==340&&civilization.science==80;
  checks["three_separate_slot_paths"]=CampaignPath(1)!=CampaignPath(2)&&CampaignPath(2)!=CampaignPath(3);
  checks["qa_slot_path_is_evidence_only"]=CampaignPath(1).StartsWith(EvidenceDir())&&CampaignPath(1)!=real;
  // Drive the opening descent through the real native solver with normal starting resources.
  NewCampaign(2);paused=true;chapterBanner=0;int descentSteps=0;
  while(descentSteps<20000&&(expedition.Alt(Pos(player))>1||Vel(player).magnitude>1.5f)){
   for(int i=0;i<100;i++){playerAction=LandingForce();shipRotation=desiredRotation;PhysicsTick(.01f);expedition.Status(player);descentSteps++;}
   yield return null;
  }
  checks["fresh_campaign_can_land_through_native_vtol"]=expedition.Alt(Pos(player))<1.05f&&Vel(player).magnitude<1.5f&&health>0;
  checks["fresh_landing_can_deploy_tires"]=ToggleWheels();for(int i=0;i<150;i++){RoverStep(.01f);expedition.Status(player);}GroundBoard();
  checks["fresh_landing_can_start_surface_expedition"]=GroundActive&&surface.Current!=null&&RoverReady;
  NewCampaign(1);paused=true;chapterBanner=0;
  var settings=new VoyagerSettings();checks["default_preferences_valid"]=settings.Validate();
  settings.BindButton(0,2);checks["controller_rebinding_swaps_conflicts"]=settings.buttons[0]==2&&settings.buttons[2]==0&&settings.Validate();
  settings.BindKey(KeyCode.W,KeyCode.UpArrow);checks["keyboard_actions_remap"]=settings.Key(KeyCode.W)==KeyCode.UpArrow;
  settings.deadzone=.30f;checks["custom_deadzone_filters_drift"]=settings.Filter(new Vector2(.2f,0),false)==Vector2.zero;
  settings.invertX=true;settings.stickSensitivity=2;checks["inverted_look_and_sensitivity_apply"]=settings.Filter(Vector2.right,true).x< -1.9f;
  settings.deadzone=float.NaN;checks["nan_preferences_rejected"]=!settings.Validate();
  var state=new VoyagerState();var model=new CivilizationModel();string why;
  checks["fresh_campaign_state_valid"]=state.Validate();
  checks["cargo_has_actual_capacity"]=state.Capacity(1)>state.Capacity(0);
  state.cargo=30;checks["loaded_cargo_blocks_smaller_hold"]=!state.CanInstall(3,2,0,out why);state.cargo=0;
  state.drive=1;state.weapon=1;state.defense=1;checks["reactor_budget_prevents_overpowered_modules"]=!state.CanInstall(3,2,0,out why);
  state=new VoyagerState{defense=1};checks["shield_module_changes_capacity"]=state.ShieldMax==80;
  state.defense=2;checks["armor_has_speed_tradeoff"]=state.HullBonus>=70&&state.Speed<1;
  var board=state.Board(0,model);checks["procedural_board_has_six_distinct_jobs"]=board.Count==6&&new HashSet<int>(board.ConvertAll(c=>c.id)).Count==6;
  checks["fresh_board_does_not_offer_unavailable_trade"]=!board.Exists(c=>c.kind==5||c.kind==6);
  state.Accept(board[0],out why);state.Accept(board[1],out why);state.Accept(board[2],out why);
  checks["active_contract_limit_is_three"]=!state.Accept(board[3],out why);
  float alloy=model.alloy;checks["contract_reward_changes_world_state"]=state.Complete(board[0],model)&&model.alloy>alloy&&model.factions[board[0].faction].relation> -100;
  alloy=model.alloy;checks["contract_reward_cannot_repeat"]=!state.Complete(board[0],model)&&model.alloy==alloy;
  checks["twelve_story_chapters_authored"]=CampaignChapters.Titles.Length==12&&CampaignChapters.Objectives.Length==12&&CampaignChapters.Stories.Length==12;
  bool harder=true;for(int i=1;i<12;i++)harder&=CampaignChapters.EnemyHealth(i,1)>CampaignChapters.EnemyHealth(i-1,1)&&CampaignChapters.EnemyDamage(i,1)>CampaignChapters.EnemyDamage(i-1,1);
  checks["every_chapter_raises_enemy_threat"]=harder;
  checks["difficulty_preserves_chapter_order"]=CampaignChapters.EnemyHealth(4,2)>CampaignChapters.EnemyHealth(4,1)&&CampaignChapters.EnemyDamage(4,0)<CampaignChapters.EnemyDamage(4,1);
  state=new VoyagerState();model=new CivilizationModel();checks["chapter_does_not_complete_without_objectives"]=!CampaignChapters.Advance(state,model);
  state.landed=state.tires=state.walked=state.recorder=true;alloy=model.alloy;
  checks["chapter_completion_advances_and_rewards"]=CampaignChapters.Advance(state,model)&&state.chapter==1&&model.alloy>alloy;
  alloy=model.alloy;checks["next_chapter_has_its_own_gate"]=!CampaignChapters.Advance(state,model)&&model.alloy==alloy;
  state.chapter=11;state.flagshipDefeated=true;model.dysonBuilt=12;
  for(int i=0;i<3;i++)model.colonies.Add(new CivilizationModel.Colony{key="Q"+i,port=i,anchor=Vector3.up});
  checks["final_chapter_requires_flagship_and_civilization"]=CampaignChapters.Advance(state,model)&&state.campaignComplete&&state.victories.Contains("WAYFARER");
  alloy=model.alloy;checks["campaign_completion_is_idempotent"]=!CampaignChapters.Advance(state,model)&&model.alloy==alloy;
  state=new VoyagerState();model=new CivilizationModel();model.colonies.Add(new CivilizationModel.Colony{key="HOME",port=0,anchor=Vector3.up,population=110});
  model.colonies.Add(new CivilizationModel.Colony{key="S0P1",port=1,anchor=Vector3.up,population=110});
  state.Policy(0).food=100;state.Policy(1).food=0;state.routes.Add(new VoyagerState.SupplyRoute{source=0,target=1,progress=.999f});
  state.TickEconomy(.1f,model);checks["supply_routes_deliver_real_provisions"]=state.Policy(1).food>10&&state.trades==1;
  state.Policy(1).food=0;state.Policy(1).morale=36;state.TickEconomy(1,model);checks["food_shortage_reduces_morale"]=state.Policy(1).morale<36;
  state.agendas[0].clock=120;state.TickEconomy(.1f,model);checks["factions_expand_their_territory"]=state.agendas[0].worlds.Count>1&&state.agendas[0].industry>1;
  model.colonies[1].population=28;state.Policy(1).food=0;state.Policy(1).focus=3;state.TickEconomy(1,model);checks["provisioning_policy_changes_food_production"]=state.Policy(1).food>0;
  var delivery=new VoyagerContract{id=600,kind=5,accepted=true,targetPort=1,faction=0,reward=0,science=0};float provisions=state.Policy(1).food;bool delivered=state.Complete(delivery,model);checks["courier_deliveries_feed_the_destination_colony"]=delivered&&Mathf.Abs(state.Policy(1).food-provisions-12)<.001f;
  state.crisis=new VoyagerState.Crisis{active=true,kind=1,remaining=120,port=0};checks["crisis_relief_consumes_resources"]=state.ResolveCrisis(model,out why)&&!state.crisis.active&&state.crisesResolved==1;
  state.crisis=new VoyagerState.Crisis{active=true,kind=2,remaining=.01f,port=0};alloy=model.alloy;state.TickEconomy(.1f,model);
  checks["failed_crisis_has_bounded_consequences"]=!state.crisis.active&&model.alloy<alloy&&model.colonies.Count==2;
  state.contractsDone=10;for(int i=0;i<8;i++)state.visited.Add(i);state.UnlockVictories(model);state.UnlockVictories(model);
  checks["alternative_victories_unlock_once"]=state.victories.FindAll(x=>x=="PATHFINDER").Count==1;
  // Mid-campaign gates exercise the complete twelve-chapter graph.
  var gates=new VoyagerState();var gateCiv=new CivilizationModel();
  gateCiv.colonies.Add(new CivilizationModel.Colony{key="G0",port=0,anchor=Vector3.up});gateCiv.colonies.Add(new CivilizationModel.Colony{key="G1",port=1,anchor=Vector3.up});
  gates.visited.AddRange(new[]{0,1});gates.contractsDone=2;checks["chapter_six_requires_completed_contracts"]=CampaignChapters.Progress(5,gates,gateCiv)<1;gates.contractsDone=3;checks["chapter_six_combines_expansion_and_exploration"]=CampaignChapters.Progress(5,gates,gateCiv)==1;
  gateCiv.warsWon=1;checks["chapter_seven_requires_player_combat"]=CampaignChapters.Progress(6,gates,gateCiv)<1;gates.nativeKills=2;checks["chapter_seven_records_personal_warship_kills"]=CampaignChapters.Progress(6,gates,gateCiv)==1;
  gates.defensesWon=2;gateCiv.colonies[0].districts.Add(new CivilizationModel.District{kind=7,progress=1});gateCiv.colonies[1].districts.Add(new CivilizationModel.District{kind=7,progress=1});gates.routes.Add(new VoyagerState.SupplyRoute{source=0,target=1});checks["chapter_eight_uses_lifetime_defenses_and_supply"]=CampaignChapters.Progress(7,gates,gateCiv)==1;
  gateCiv.dysonBuilt=3;gates.contractsDone=6;checks["chapter_nine_needs_four_complete_sectors"]=CampaignChapters.Progress(8,gates,gateCiv)<1;gateCiv.dysonBuilt=4;checks["chapter_nine_combines_research_and_contracts"]=CampaignChapters.Progress(8,gates,gateCiv)==1;
  gateCiv.colonies.Add(new CivilizationModel.Colony{key="G2",port=2,anchor=Vector3.up});gateCiv.warsWon=3;gates.nativeKills=6;checks["chapter_ten_requires_three_worlds_and_wars"]=CampaignChapters.Progress(9,gates,gateCiv)==1;
  gateCiv.dysonBuilt=8;gates.trades=5;gates.crisesResolved=1;checks["chapter_eleven_requires_two_crises"]=CampaignChapters.Progress(10,gates,gateCiv)<1;gates.crisesResolved=2;checks["chapter_eleven_requires_network_recovery"]=CampaignChapters.Progress(10,gates,gateCiv)==1;
  gateCiv.dysonBuilt=12;checks["dyson_alone_does_not_complete_the_final_chapter"]=CampaignChapters.Progress(11,gates,gateCiv)<1;
  OpenVoyager(VoyagerPanel.Home);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"voyager-title.png"));
  CloseVoyager();StageSurface();surface.Scripted=false;paused=true;voyagerPanel=VoyagerPanel.None;expedition.Status(player);
  surface.Current.recorder=true;surface.Current.sites[0].done=true;salvageCount=7;tiers[0]=1;BuildModules();
  voyager.food=123;voyager.chapter=2;health=82;wheelsRequested=true;wheelDeployment=1;roverBrake=true;RoverPose(0);expedition.Status(player);
  Vector3 footBefore=expedition.Focus,shipBefore=Pos(player);
  var checkpoint=CaptureCampaign();checks["full_checkpoint_valid"]=checkpoint.Validate();
  checks["checkpoint_contains_surface_and_rover"]=checkpoint.crew.onFoot&&checkpoint.wheels&&checkpoint.surface.expeditions.Count>0&&checkpoint.surface.expeditions[0].recorder;
  checks["full_checkpoint_writes_atomically"]=SaveCampaign()&&ReadCampaign(1)!=null;
  health=1;salvageCount=0;tiers[0]=0;voyager.food=0;surface.Current.recorder=false;
  checks["load_restores_entire_expedition"]=LoadCampaign(1)&&health==82&&salvageCount==7&&tiers[0]==1&&Mathf.Abs(voyager.food-123)<.01f&&GroundActive&&RoverReady&&surface.Current.recorder;
  checks["load_preserves_ship_and_crew_positions"]=Vector3.Distance(expedition.Focus,footBefore)<.01f&&Vector3.Distance(Pos(player),shipBefore)<.01f;
  paused=true;float foodBefore=voyager.Policy(0).food;VoyagerTick(.1f);checks["pause_freezes_economy_and_contracts"]=voyager.Policy(0).food==foodBefore;
  SaveCampaign();File.WriteAllText(CampaignPath(1),"{corrupt");checks["corrupt_primary_recovers_valid_backup"]=ReadCampaign(1)!=null;
  checks["recovered_slot_can_be_written_again"]=SaveCampaign()&&ReadCampaign(1)!=null;
  var bad=CaptureCampaign();bad.ship.x=double.NaN;checks["checkpoint_rejects_invalid_physics"]=!bad.Validate();
  var other=new VoyagerState{slot=2};string atomic=Path.Combine(EvidenceDir(),"voyager-atomic-fixture.json");
  CampaignFiles.Write(atomic,other,x=>x.Validate());other.food=111;CampaignFiles.Write(atomic,other,x=>x.Validate());File.WriteAllText(atomic,"bad");
  checks["checksum_backup_retains_previous_valid_data"]=CampaignFiles.Read<VoyagerState>(atomic,x=>x.Validate()).food==80;
  // Origin changes preserve the absolute checkpoint, including a parked rover and boots.
  Vector3 shift=new Vector3(320,0,-160);var global=GlobalPosition;RebaseOrigin(shift);
  var shifted=CaptureCampaign();checks["checkpoint_origin_coordinates_are_consistent"]=WorldDistance(global,GlobalPosition)<.01f&&shifted.cellX==cellX&&shifted.crew.onFoot;
  checks["origin_shift_checkpoint_roundtrips"]=SaveCampaign()&&LoadCampaign(1)&&WorldDistance(global,GlobalPosition)<.02f;
  paused=true;OpenVoyager(VoyagerPanel.Settings);settingsPage=0;voyagerSelection=0;float sensitivity=preferences.mouseSensitivity;AdjustSetting(0,1);
  checks["settings_change_live_sensitivity"]=preferences.mouseSensitivity>sensitivity&&Controls.Settings==preferences;
  settingsPage=2;rebindAction=0;preferences.BindButton(0,2);rebindAction=-1;
  var pad=InputSystem.AddDevice<Gamepad>();Controls.TestDevice=pad;Controls.SuppressUntilNeutral();InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();Controls.Poll();
  InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.West));InputSystem.Update();Controls.Poll();
  checks["remapped_physical_button_drives_logical_action"]=Controls.Down(PilotInput.Button.Cross)&&!Controls.Down(PilotInput.Button.Square);
  Controls.TestDevice=null;InputSystem.RemoveDevice(pad);preferences=new VoyagerSettings();Controls.Settings=preferences;
  CloseVoyager();paused=true;expedition.ResetCrew();wheelsRequested=false;wheelDeployment=0;player.vx=player.vy=player.vz=0;
  civilization.alloy=1000;civilization.science=500;health=HullMaximum*.5f;
  checks["module_install_consumes_resources_and_changes_stats"]=InstallModule(0,1)&&voyager.drive==1&&voyager.Thrust>1&&civilization.alloy==860;
  checks["module_install_preserves_health_fraction"]=Mathf.Abs(health/HullMaximum-.5f)<.01f;
  float beforeHull=HullMaximum;checks["armor_module_changes_actual_hull"]=InstallModule(2,2)&&HullMaximum>beforeHull;
  float shieldBefore=voyager.shield;float hp=health;invulnerable=0;GroundDamage(10);
  checks["shield_absorbs_actual_incoming_damage"]=voyager.shield<shieldBefore&&health==hp;
  voyager.heat=0;int count=shots.Count;voyager.weapon=1;shotCooldown=0;PlayerFire(shipRotation);
  checks["rail_module_fires_native_heavy_projectile"]=shots.Count==count+1&&shots[shots.Count-1].damage>2&&Vel(shots[shots.Count-1].state).magnitude>200;
  count=shots.Count;voyager.heat=101;PlayerFire(shipRotation);checks["overheat_prevents_firing"]=shots.Count==count;
  voyager.heat=0;voyager.weapon=2;PlayerFire(shipRotation);checks["scatter_module_has_five_short_lived_pellets"]=shots.Count==count+5&&shots[shots.Count-1].life<2;
  civilization.blueprint=3;civilization.ownedHulls|=8;voyager.weapon=1;ApplyCivilHull();OpenVoyager(VoyagerPanel.Outfitter);civilizationScene.Preview(3,civilization.blueprint,civilization.paint,civilization);
  yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"voyager-outfitter.png"));
  OpenVoyager(VoyagerPanel.Chapters);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"voyager-chapters.png"));
  // Physical contract interactions and the vulnerable native escort, across save/load.
  NewCampaign(3);StageSurface();surface.Scripted=false;paused=true;chapterBanner=0;voyager.contracts.Clear();preferences.toggleInteract=true;
  var rescue=new VoyagerContract{id=8000,kind=0,port=0,targetPort=0,faction=0,accepted=true,title="QA rescue",reward=95,science=20};voyager.contracts.Add(rescue);AnchorContract(rescue);
  var crewSave=expedition.Capture();crewSave.onFoot=true;crewSave.foot=Make(ContractPoint(rescue),Vector3.zero);expedition.Restore(crewSave);expedition.Status(player);surface.Sync();surface.Interact(false,0);interactionToggle=true;
  for(int i=0;i<13;i++)VoyagerContractsTick(.1f);
  checks["rescue_requires_real_interaction_and_cargo_space"]=rescue.carrying&&voyager.cargo==8;
  expedition.ResetCrew();VoyagerContractsTick(.1f);checks["rescue_finishes_only_after_returning_to_ship"]=rescue.completed&&voyager.cargo==0&&voyager.contractsDone==1;
  var survey=new VoyagerContract{id=8001,kind=1,port=0,targetPort=0,faction=0,accepted=true,title="QA survey",reward=100,science=35};voyager.contracts.Add(survey);AnchorContract(survey);
  for(int scan=0;scan<3;scan++){crewSave=expedition.Capture();crewSave.onFoot=true;crewSave.foot=Make(ContractPoint(survey),Vector3.zero);expedition.Restore(crewSave);expedition.Status(player);surface.Sync();surface.Interact(false,0);interactionToggle=true;for(int i=0;i<13;i++)VoyagerContractsTick(.1f);}
  checks["survey_contract_needs_three_separate_scans"]=survey.completed&&survey.count==3;
  var bounty=new VoyagerContract{id=8002,kind=3,port=0,targetPort=0,faction=1,accepted=true,title="QA bounty",reward=120,science=35};voyager.contracts.Add(bounty);AnchorContract(bounty);VoyagerContractsTick(.1f);
  var elite=surface.Current.foes.Find(f=>f.contractId==bounty.id);checks["bounty_creates_a_persistent_elite"]=elite!=null&&elite.maxHp>80;
  if(elite!=null){surface.Damage(elite,10000);VoyagerContractsTick(.1f);}checks["bounty_requires_actual_target_defeat"]=bounty.completed;
  expedition.ResetCrew();var escort=new VoyagerContract{id=8003,kind=4,port=0,targetPort=0,faction=0,accepted=true,title="QA escort",reward=200,science=45};voyager.contracts.Add(escort);AnchorContract(escort);
  Vector3 convoy=new GalacticPoint(escort.ex,escort.ey,escort.ez)-OriginGlobal;player=Make(convoy+Vector3.right*20,Vector3.zero);FloatingOrigin();Stream(true);Vector3 escortBefore=new GalacticPoint(escort.ex,escort.ey,escort.ez)-OriginGlobal;
  for(int i=0;i<100;i++)EscortTick(escort,.01f);
  checks["escort_shuttle_moves_through_native_solver"]=escortView!=null&&Vector3.Distance(Pos(escortState),escortBefore)>2;
  float escortHp=escort.escortHealth;bool struck=VoyagerShotHit(Pos(escortState)-Vector3.right*10,Pos(escortState)+Vector3.right*10,true);checks["hostile_projectiles_damage_escorted_shuttle"]=struck&&escort.escortHealth<escortHp;
  double escortGlobalX=escort.ex;float savedEscortHp=escort.escortHealth;SaveCampaign();LoadCampaign(3);paused=true;escort=voyager.contracts.Find(c=>c.id==8003);checks["escort_progress_and_health_survive_checkpoint"]=escort!=null&&Math.Abs(escort.ex-escortGlobalX)<.01&&escort.escortHealth==savedEscortHp;
  checks["settings_bindings_do_not_conflict_with_default_keys"]=settings.Key(KeyCode.UpArrow)==KeyCode.W;
  expedition.ResetCrew();wheelsRequested=false;wheelDeployment=0;cruising=true;landingAssist=false;routeWaypoints.Clear();var travelGoal=GlobalPosition+Vector3.right*5000;routeGlobal=travelGoal;routeWaypoints.Add(travelGoal);routeLeg=0;destinationName="QA cruise";player.vx=400;
  checks["cruise_checkpoint_is_valid"]=SaveCampaign();cruising=false;routeWaypoints.Clear();LoadCampaign(3);paused=true;
  checks["cruise_route_and_velocity_resume_safely"]=cruising&&routeWaypoints.Count==1&&routeLeg==0&&Math.Abs(player.vx-400)<.01&&destinationName=="QA cruise";
  cruising=false;landingAssist=true;SaveCampaign();landingAssist=false;LoadCampaign(3);paused=true;checks["vtol_assistance_survives_checkpoint"]=landingAssist;
  landingAssist=false;var near=expedition.Nearest(Pos(player));var savedFlight=new CampaignCheckpoint.SavedRocket{key=near.key,state=Make(Pos(player)+ShipUp*20,Vector3.up*8),age=4,altitude=100,credited=false};RestoreRocket(savedFlight);SaveCampaign();foreach(var flight in civilRockets)Destroy(flight.root.gameObject);civilRockets.Clear();LoadCampaign(3);paused=true;
  checks["rocket_physics_and_survey_credit_survive_checkpoint"]=civilRockets.Count==1&&Mathf.Abs(civilRockets[0].age-4)<.01f&&!civilRockets[0].credited&&Vel(civilRockets[0].state).magnitude>7;
  NewCampaign(3);StageVoyagerCity();voyager.chapter=11;civilization.fleet=6;civilization.dysonBuilt=8;BeginFlagshipAssault();var ark=civilization.wars.Find(w=>w.flagship&&!w.resolved);
  checks["final_assault_creates_the_actual_armored_flagship"]=ark!=null&&ark.hostile[0]==1200;
  if(ark!=null){ark.travel=0;ark.routed=true;Vector3 front=CivilBattleGlobal(ark)-OriginGlobal;player=Make(front+Vector3.up*30,Vector3.zero);FloatingOrigin();Stream(true);CivilWarTick(.01f);var boss=civilUnits.Find(u=>u.war==ark&&u.enemy&&u.index==0);bool struckBoss=false;if(boss!=null){Vector3 at=Pos(boss.state);struckBoss=CivilHit(at+Vector3.right*12-Vector3.forward*30,at+Vector3.right*12+Vector3.forward*30,true);}
   checks["large_flagship_has_a_matching_weapon_hit_area"]=boss!=null&&boss.root.localScale.x==9&&struckBoss&&boss.HP<1200;
  }else checks["large_flagship_has_a_matching_weapon_hit_area"]=false;
  checks["qa_never_modifies_real_campaign"]=(!File.Exists(real)?DateTime.MinValue:File.GetLastWriteTimeUtc(real))==realStamp;
  SaveChecks("voyager-runtime-checks.json",checks);yield return null;Application.Quit();
 }
 [Serializable] sealed class BenchmarkReport {public string unity,device,processor,graphics;public int width,height,targetFps;public bool staged=true,captureDisabled=true;public List<BenchmarkPhase> phases=new List<BenchmarkPhase>();}
 [Serializable] sealed class BenchmarkPhase {public string name;public int frames;public float averageMs,p95Ms,p99Ms,minimumFps;public long allocatedMemory;public double gpuMs;public bool gpuTimingAvailable;}
 IEnumerator BenchmarkPhaseFrames(string name,BenchmarkReport report){
  for(int i=0;i<90;i++)yield return null;
  var samples=new List<float>();var timing=new FrameTiming[1];double gpu=0;int gpuCount=0;
  for(int i=0;i<360;i++){FrameTimingManager.CaptureFrameTimings();yield return null;samples.Add(Time.unscaledDeltaTime*1000);if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0){gpu+=timing[0].gpuFrameTime;gpuCount++;}}
  samples.Sort();float sum=0;foreach(float t in samples)sum+=t;
  report.phases.Add(new BenchmarkPhase{name=name,frames=samples.Count,averageMs=sum/samples.Count,p95Ms=samples[(int)(samples.Count*.95f)],p99Ms=samples[(int)(samples.Count*.99f)],minimumFps=1000/samples[samples.Count-1],allocatedMemory=Profiler.GetTotalAllocatedMemoryLong(),gpuMs=gpuCount>0?gpu/gpuCount:0,gpuTimingAvailable=gpuCount>0});
  Debug.Log("VOYAGER_BENCHMARK "+name+" avg="+(sum/samples.Count).ToString("F1")+"ms p95="+samples[(int)(samples.Count*.95f)].ToString("F1")+"ms");
 }
 IEnumerator VoyagerBenchmark(){
  yield return new WaitForSeconds(1);NewCampaign(3);chapterBanner=0;voyager.chapter=5;landingAssist=false;invulnerable=10000;
  var report=new BenchmarkReport{unity=Application.unityVersion,device=SystemInfo.deviceModel,processor=SystemInfo.processorType,graphics=SystemInfo.graphicsDeviceName,width=Screen.width,height=Screen.height,targetFps=preferences.targetFps};
  yield return BenchmarkPhaseFrames("orbital-flight",report);
  StageVoyagerCity();paused=false;yield return BenchmarkPhaseFrames("populated-city",report);
  StageVoyagerWar();paused=false;yield return BenchmarkPhaseFrames("native-fleet-battle",report);
  StageVoyagerDyson();paused=false;yield return BenchmarkPhaseFrames("dyson-collector-shell",report);
  File.WriteAllText(Path.Combine(EvidenceDir(),"voyager-performance.json"),JsonUtility.ToJson(report,true));Debug.Log("VOYAGER_BENCHMARK_COMPLETE");Application.Quit();
 }
 void StageVoyagerCity(){
  StageSurface();surface.Scripted=true;foreach(var f in surface.Current.foes)surface.Damage(f,10000);invulnerable=10000;
  civilization.alloy=civilization.energy=civilization.science=100000;
  CivilFixtureDistrict(0);CivilFixtureDistrict(1);CivilFixtureDistrict(2);CivilFixtureDistrict(3);CivilFixtureDistrict(4);CivilFixtureDistrict(5);civilization.launches=1;CivilFixtureDistrict(6);CivilFixtureDistrict(7);
  civilizationScene.Sync(civilization,expedition.Focus);civilFilmCamera=true;var p=expedition.Nearest(expedition.Focus);var hub=civilization.Find(p.key).districts[0];Vector3 at=civilizationScene.DistrictPosition(hub.id),up=hub.normal;
  Vector3 centroid=Vector3.zero;foreach(var district in civilization.Find(p.key).districts)centroid+=civilizationScene.DistrictPosition(district.id);centroid/=civilization.Find(p.key).districts.Count;civilFilmLook=centroid+up*16;float best=float.MaxValue;Vector3 forward=PlanetExpedition.SurfaceTangent(up,ShipForward);for(int a=0;a<24;a++){Vector3 direction=Quaternion.AngleAxis(a*15,up)*forward;for(int h=180;h<500;h+=40){Vector3 candidate=centroid-direction*250+up*h;bool clear=true;for(int k=1;k<=24;k++)if(expedition.Alt(Vector3.Lerp(civilFilmLook,candidate,k/24f))<12){clear=false;break;}if(clear&&h<best){best=h;civilFilmAt=candidate;break;}}}if(best==float.MaxValue)civilFilmAt=centroid+up*500;civilOpen=false;voyagerPanel=VoyagerPanel.None;chapterBanner=0;
 }
 void StageVoyagerWar(){
  civilFilmCamera=false;expedition.ResetCrew();wheelsRequested=false;wheelDeployment=0;
  civilization.fleet=6;voyager.chapter=11;civilization.dysonBuilt=Mathf.Max(8,civilization.dysonBuilt);BeginFlagshipAssault();var w=civilization.wars.Find(front=>front.flagship&&!front.resolved);w.travel=0;w.routed=true;
  Vector3 battle=CivilBattleGlobal(w)-OriginGlobal;player=Make(battle+new Vector3(0,30,-280),Vector3.zero);shipRotation=desiredRotation=Quaternion.LookRotation(battle-Pos(player));
  FloatingOrigin();Stream(true);expanseRecording=true;expanseAction=-Gravity(Pos(player));civilFilmCamera=true;civilFilmAt=Pos(player)-ShipForward*30+ShipUp*20;civilFilmLook=CivilBattleGlobal(w)-OriginGlobal;
 }
 void StageVoyagerDyson(){
  civilFilmCamera=false;expanseRecording=false;civilization.dysonBuilt=12;civilization.dysonSystem=0;civilizationScene.Sync(civilization,Pos(player));
  Vector3 sun=CivilStarPosition(0);player=Make(sun+new Vector3(900,700,11000),Vector3.zero);shipRotation=desiredRotation=Quaternion.LookRotation(sun-Pos(player));FloatingOrigin();Stream(true);sun=CivilStarPosition(0);
  civilFilmCamera=true;civilFilmAt=Pos(player)-ShipForward*30;civilFilmLook=sun;expanseRecording=true;expanseAction=-Gravity(Pos(player));
 }
 int voyagerFilmFrame;string voyagerFilmFolder,voyagerFilmPhase="";
 IEnumerator VoyagerFilmFrames(int count,string phase){
  voyagerFilmPhase=phase.Replace("-"," ").ToUpperInvariant();Debug.Log("VOYAGER_FILM "+phase+" frame="+voyagerFilmFrame);
  for(int i=0;i<count;i++){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(voyagerFilmFolder,(voyagerFilmFrame++).ToString("D4")+".png"));yield return new WaitForSecondsRealtime(1f/12);}
 }
 IEnumerator VoyagerRecord(){
  yield return new WaitForSeconds(1);voyagerFilmFolder=Path.Combine(EvidenceDir(),"voyager-frames");Directory.CreateDirectory(voyagerFilmFolder);
  NewCampaign(3);paused=true;chapterBanner=0;OpenVoyager(VoyagerPanel.Chapters);yield return VoyagerFilmFrames(72,"twelve-chapter-campaign");
  CloseVoyager();StageVoyagerCity();paused=false;yield return VoyagerFilmFrames(120,"cities-and-foundations");
  civilization.FabricateRocket(CivilLocal,out string why);AdvanceCivil(civilization,12.1f);civilFilmCamera=false;LaunchCivilRocket();yield return VoyagerFilmFrames(168,"native-rocket-launch");
  civilFollowTime=0;paused=true;expedition.ResetCrew();wheelsRequested=false;wheelDeployment=0;player.vx=player.vy=player.vz=0;civilization.blueprint=3;civilization.ownedHulls|=8;civilization.paint=1;voyager.drive=1;voyager.weapon=1;voyager.defense=2;voyager.hold=1;ApplyCivilHull();
  OpenVoyager(VoyagerPanel.Outfitter);civilizationScene.Preview(3,civilization.blueprint,civilization.paint,civilization);yield return VoyagerFilmFrames(72,"modular-ship-loadouts");
  OpenVoyager(VoyagerPanel.Contracts);yield return VoyagerFilmFrames(60,"frontier-contracts");
  OpenVoyager(VoyagerPanel.Colonies);yield return VoyagerFilmFrames(60,"colonies-supply-and-factions");
  OpenVoyager(VoyagerPanel.Settings);yield return VoyagerFilmFrames(60,"settings-and-accessibility");
  CloseVoyager();StageVoyagerWar();paused=false;yield return VoyagerFilmFrames(120,"alien-flagship-and-fleet-combat");
  StageVoyagerDyson();paused=true;yield return VoyagerFilmFrames(72,"dyson-project");
  OpenVoyager(VoyagerPanel.Chapters);voyager.chapter=11;yield return VoyagerFilmFrames(60,"final-chapter");
  Debug.Log("VOYAGER_FILM_COMPLETE frames="+voyagerFilmFrame);yield return new WaitForSeconds(.5f);Application.Quit();
 }
}
