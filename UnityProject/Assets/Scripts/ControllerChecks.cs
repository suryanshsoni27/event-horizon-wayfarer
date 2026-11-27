using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.DualShock.LowLevel;

public sealed partial class DefenseGame {
 void PadFeed(Gamepad pad,GamepadState state,float dt=0){InputSystem.QueueStateEvent(pad,state);InputSystem.Update();Tick(dt);}
 GamepadState Press(GamepadButton button){return new GamepadState().WithButton(button);}
 void LandedFixture(){StageSurface();health=100;invulnerable=1000;foreach(var f in surface.records.Values)foreach(var foe in f.foes)surface.Damage(foe,10000);GroundBoard();surface.Scripted=false;paused=true;rocket.position=Pos(player);}
 IEnumerator ControllerChecks(){yield return new WaitForSeconds(1);controllerRecording=true;var c=new Dictionary<string,bool>();
  var dual=InputSystem.AddDevice<DualSenseGamepadHID>();Controls.TestDevice=dual;
  var neutral=new DualSenseHIDInputReport{leftStickX=128,leftStickY=128,rightStickX=128,rightStickY=128,buttons0=8};InputSystem.QueueStateEvent(dual,neutral);InputSystem.Update();Controls.Poll();
  var report=neutral;report.buttons0=0xf8;report.buttons1=0xf3;report.leftTrigger=200;report.rightTrigger=240;report.leftStickX=240;report.rightStickY=20;InputSystem.QueueStateEvent(dual,report);InputSystem.Update();Controls.Poll();
  c["dualsense_hid_face_buttons_and_shoulders"]=PadDown(PilotInput.Button.Cross)&&PadDown(PilotInput.Button.Circle)&&PadDown(PilotInput.Button.Square)&&PadDown(PilotInput.Button.Triangle)&&PadDown(PilotInput.Button.L1)&&PadDown(PilotInput.Button.R1);
  c["dualsense_hid_options_share_stick_clicks"]=PadDown(PilotInput.Button.Options)&&PadDown(PilotInput.Button.Share)&&PadDown(PilotInput.Button.L3)&&PadDown(PilotInput.Button.R3);
  c["dualsense_hid_sticks_triggers_orientation"]=Controls.Move.x>.7f&&Controls.Look.y>.7f&&Controls.L2>.6f&&Controls.R2>.8f;
  report=neutral;report.buttons0=2;InputSystem.QueueStateEvent(dual,report);InputSystem.Update();Controls.Poll();c["dualsense_hid_dpad_right"]=PadDown(PilotInput.Button.Right);
  InputSystem.RemoveDevice(dual);Controls.TestDevice=null;Controls.Poll();var pad=InputSystem.AddDevice<Gamepad>();Controls.TestDevice=pad;PadFeed(pad,new GamepadState());
  c["input_system_gamepad_connected"]=Controls.Connected;
  c["dualsense_hid_layout_registered"]=new List<string>(InputSystem.ListLayouts()).Contains("DualSenseGamepadHID");
  PadFeed(pad,new GamepadState{leftStick=new Vector2(.05f,-.06f),rightStick=new Vector2(.08f,.05f)});c["stick_drift_deadzone"]=Controls.Move==Vector2.zero&&Controls.Look==Vector2.zero;
  PadFeed(pad,Press(GamepadButton.South));c["cross_resumes_pause"]=!paused;PadFeed(pad,new GamepadState());
  Quaternion oldRotation=shipRotation;PadFeed(pad,new GamepadState{rightStick=new Vector2(.7f,.4f),leftStick=new Vector2(.5f,.3f),rightTrigger=.65f},.06f);c["analog_flight_thrust_and_steering"]=throttle>.5f&&playerAction.magnitude>1&&Quaternion.Angle(oldRotation,shipRotation)>1;
  PadFeed(pad,Press(GamepadButton.Start));Vector3 frozen=Pos(player);float time=simSeconds;PadFeed(pad,new GamepadState{rightTrigger=1,leftStick=Vector2.one},.1f);c["options_pause_freezes_native_physics"]=paused&&Pos(player)==frozen&&simSeconds==time;
  PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.South));PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.Select));c["share_opens_atlas"]=chartOpen&&paused;int oldSystem=chartSystem;PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.DpadRight));PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.DpadDown));c["dpad_navigates_system_and_destination"]=chartSystem==(oldSystem+1)%catalog.Count&&chartPort==1;
  PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.South));c["cross_engages_selected_cruise"]=cruising&&!chartOpen;PadFeed(pad,new GamepadState());player.vx=80;player.vy=player.vz=0;PadFeed(pad,new GamepadState{leftTrigger=1},.02f);c["l2_cancels_cruise_with_braking"]=!cruising&&cruiseBraking&&Vel(player).magnitude<80;
  ResetRound();ResumeFlight();PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.RightStick));c["r3_opens_workshop"]=upgradeOpen;salvageCount=8;PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.DpadDown));PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.South));c["workshop_select_and_fit"]=tiers[1]==1&&salvageCount==7;PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.East));c["circle_returns_from_workshop"]=!upgradeOpen&&!paused;
  ResetRound();c["wheel_deployment_rejected_in_space"]=!ToggleWheels()&&!RoverDeployed;
  LandedFixture();PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.South));PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.DpadDown));c["pad_deploys_tires_after_landing"]=wheelsRequested;
  for(int i=0;i<40;i++)PadFeed(pad,new GamepadState(),.01f);float folded=wheelDeployment;PadFeed(pad,Press(GamepadButton.Start));for(int i=0;i<8;i++)PadFeed(pad,new GamepadState(),.1f);c["pause_freezes_wheel_deployment"]=paused&&wheelDeployment==folded;PadFeed(pad,Press(GamepadButton.South));PadFeed(pad,new GamepadState());
  for(int i=0;i<160;i++)PadFeed(pad,new GamepadState(),.01f);RoverPose(.01f);c["animated_six_wheel_deployment"]=RoverReady&&roverView.tires.Length==6&&roverView.gameObject.activeSelf;
  c["four_crew_seated_in_rover"]=expedition.Motion.State=="DRIVING"&&expedition.Companions[2].gameObject.activeSelf&&(cam.cullingMask&(1<<29))!=0;
  var generic=expedition.Companions[2];var oldFace=generic.Find("Runtime-only helmet photo");oldFace.gameObject.SetActive(true);GetComponent<HelmetPhotoUI>().BindCompanion(generic,3);c["unassigned_rover_crew_clears_stale_portrait"]=!oldFace.gameObject.activeSelf&&generic.Find("Visor gleam").gameObject.activeSelf;
  c["rover_native_ground_clearance"]=expedition.Alt(Pos(player))>=.94f&&expedition.Alt(Pos(player))<1.3f;
  GalacticPoint start=GlobalPosition;for(int i=0;i<100;i++)PadFeed(pad,new GamepadState{rightTrigger=1},.05f);c["r2_drives_native_rover"]=WorldDistance(start,GlobalPosition)>15&&Vector3.Dot(Vel(player),ShipForward)>3&&throttle==0;oldRotation=shipRotation;
  c["wheel_retraction_rejected_while_moving"]=!ToggleWheels()&&wheelsRequested;
  for(int i=0;i<35;i++)PadFeed(pad,new GamepadState{rightTrigger=.7f,leftStick=new Vector2(.6f,0)},.05f);c["stick_steers_rover"]=Quaternion.Angle(oldRotation,shipRotation)>12;Vector3 seatRelative=expedition.Crew.position-Pos(player),wheelRelative=roverView.tires[0].position-Pos(player);RebaseOrigin(new Vector3(320,0,0));c["rover_seats_survive_origin_shifts"]=(expedition.Crew.position-Pos(player)-seatRelative).magnitude<.02f&&(roverView.tires[0].position-Pos(player)-wheelRelative).magnitude<.02f&&expedition.Motion.State=="DRIVING";
  for(int i=0;i<100;i++)PadFeed(pad,Press(GamepadButton.South),.02f);c["cross_handbrake_stops_rover"]=Vel(player).magnitude<1.5f;
  for(int i=0;i<80;i++)PadFeed(pad,new GamepadState{leftTrigger=1},.02f);c["l2_drives_reverse"]=Vector3.Dot(Vel(player),ShipForward)<-1;
  for(int i=0;i<100;i++)PadFeed(pad,Press(GamepadButton.South),.02f);PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.North));c["triangle_disembarks_parked_rover"]=GroundActive&&RoverReady;GalacticPoint parked=GlobalPosition;start=OriginGlobal+expedition.Focus;
  for(int i=0;i<240&&WorldDistance(parked,OriginGlobal+expedition.Focus)<8;i++)PadFeed(pad,new GamepadState{leftStick=Vector2.up},.025f);c["pad_walks_native_astronaut"]=WorldDistance(start,OriginGlobal+expedition.Focus)>2;c["rover_parks_while_crew_explores"]=WorldDistance(parked,GlobalPosition)<.3f;
  PadFeed(pad,Press(GamepadButton.LeftStick));c["l3_toggles_running"]=expedition.Sprinting&&expedition.RunToggle;
  PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.North));c["boarding_rejected_outside_range"]=GroundActive;
  PadFeed(pad,new GamepadState());PadFeed(pad,new GamepadState{rightTrigger=1},.2f);c["r2_fires_carbine"]=surface.Ammo<24;PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.West),.1f);c["square_starts_reload"]=surface.ReloadTime>0;for(int i=0;i<16;i++)PadFeed(pad,new GamepadState(),.1f);c["reload_completes"]=surface.Ammo==24;
  for(int i=0;i<10;i++)PadFeed(pad,new GamepadState{leftTrigger=1},.05f);c["l2_aims_camera"]=CrewAim&&cam.fieldOfView<58;PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.East),.02f);c["circle_triggers_shockwave"]=surface.PulseCooldown>7;
  PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.RightShoulder));c["r1_holds_interaction"]=CrewInteract;
  PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.LeftShoulder));c["l1_opens_mission_journal"]=surface.Journal&&paused;PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.East));c["circle_closes_journal"]=!surface.Journal&&!paused;
  for(int i=0;i<5000&&Vector3.Distance(expedition.Focus,Pos(player))>3.5f;i++){expedition.ScriptWalk(Pos(player)-expedition.Focus);expedition.Status(player);expedition.FootStep(.01f);}expedition.ScriptWalk(Vector3.zero);PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.North));c["triangle_reboards_rover"]=!GroundActive&&RoverReady;
  PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.DpadDown));c["pad_retracts_tires_when_parked"]=!wheelsRequested;for(int i=0;i<180;i++)PadFeed(pad,new GamepadState(),.01f);c["retraction_restores_flight_and_hides_crew"]=!RoverDeployed&&(cam.cullingMask&(1<<29))==0&&throttle==0;
  PadFeed(pad,new GamepadState{rightTrigger=.8f},.05f);c["flight_thrust_returns_after_retraction"]=throttle>.7f&&playerAction.magnitude>1;
  InputSystem.RemoveDevice(pad);Tick(.02f);c["controller_disconnect_pauses_safely"]=paused&&!Controls.Connected&&Controls.Move==Vector2.zero;Controls.TestDevice=null;
  pad=InputSystem.AddDevice<Gamepad>();Controls.TestDevice=pad;PadFeed(pad,Press(GamepadButton.South));c["reconnect_requires_neutral_before_resuming"]=paused;PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.South));c["reconnected_controller_resumes"]=!paused;InputSystem.RemoveDevice(pad);Controls.TestDevice=null;
  ResetRound();c["new_run_clears_rover_and_seats"]=!RoverDeployed&&!expedition.OnFoot&&!roverView.gameObject.activeSelf;
  string json=SurfaceJSON(c);System.IO.File.WriteAllText(System.IO.Path.Combine(EvidenceDir(),"controller-runtime-checks.json"),json);Debug.Log("CONTROLLER_QA "+json);yield return new WaitForSeconds(.3f);Application.Quit();
 }
 IEnumerator RoverRecord(){yield return new WaitForSeconds(1);controllerRecording=true;LandedFixture();quality=2;ApplyQuality();var pad=InputSystem.AddDevice<Gamepad>();Controls.TestDevice=pad;PadFeed(pad,new GamepadState());paused=false;surface.Scripted=true;string folder=System.IO.Path.Combine(EvidenceDir(),"rover-frames");System.IO.Directory.CreateDirectory(folder);int frame=0;bool drove=false,walked=false,reboarded=false,retracted=false;GalacticPoint start=GlobalPosition;
  while(frame<400){GamepadState state=new GamepadState();
   if(frame==18)state=Press(GamepadButton.DpadDown);
   if(frame>=55&&frame<170)state=new GamepadState{rightTrigger=frame<140?.8f:.35f,leftStick=new Vector2(frame>100?.28f:0,0)};
   if(frame>=170&&frame<204)state=Press(GamepadButton.South);
   if(frame==205)state=Press(GamepadButton.North);
   if(frame>=220&&frame<255)state=new GamepadState{leftStick=Vector2.up};
   if(frame==232)state=new GamepadState{leftStick=Vector2.up}.WithButton(GamepadButton.LeftStick);
   if(frame>=255&&frame<304&&GroundActive){expedition.ScriptWalk(Pos(player)-expedition.Focus);expedition.Sprinting=true;for(int i=0;i<8;i++){expedition.Status(player);expedition.FootStep(.01f);}}
   if(frame==308)state=Press(GamepadButton.North);
   if(frame==326)state=Press(GamepadButton.DpadDown);
   PadFeed(pad,state,.0f);if(frame==169)drove=WorldDistance(start,GlobalPosition)>15;if(frame==250)walked=GroundActive&&expedition.RunToggle;if(frame==315)reboarded=!GroundActive&&RoverReady;if(frame>360&&!RoverDeployed)retracted=true;
   if(RoverDeployed&&!GroundActive){roverCameraYaw=frame<55?-38:frame<170?-23:36;roverCameraPitch=13;}noticeTime=0;
   yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,frame.ToString("D4")+".png"));frame++;yield return new WaitForSecondsRealtime(1f/12);
  }
  var c=new Dictionary<string,bool>{{"native_rover_driven",drove},{"crew_exits_and_runs",walked},{"crew_reboards",reboarded},{"tires_retract",retracted}};System.IO.File.WriteAllText(System.IO.Path.Combine(EvidenceDir(),"rover-record-checks.json"),SurfaceJSON(c));Debug.Log("ROVER_RECORD_FINAL frames="+frame+" "+SurfaceJSON(c));InputSystem.RemoveDevice(pad);Controls.TestDevice=null;Controls.Poll();controllerRecording=false;paused=true;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(EvidenceDir(),"dualsense-pause-guide.png"));yield return new WaitForSeconds(.2f);Application.Quit();
 }
}
