using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed partial class DefenseGame {
 static HeadPilotFrame HeadFixture(){return new HeadPilotFrame{status=2,faces=1,width=640,height=480,sequence=1,valid=1,confidence=.99f,yaw=7,pitch=-3,roll=2,mouthOpen=.04f,mouthWidth=.4f,x=.3f,y=.2f,w=.3f,h=.4f};}
 static void HeadFeed(HeadPilotModel model,ref HeadPilotFrame frame,int count,bool flying=true,float dt=.05f){for(int i=0;i<count;i++){frame.sequence++;model.Tick(frame,dt,flying);}}
 static void HeadCalibrate(HeadPilotModel model,ref HeadPilotFrame frame){
  frame=HeadFixture();model.BeginCalibration();HeadFeed(model,ref frame,32,false);
  if(model.Settings.mouthControls){frame.mouthOpen=.8f;HeadFeed(model,ref frame,28,false);frame.mouthOpen=.04f;frame.mouthWidth=.6f;HeadFeed(model,ref frame,28,false);}
  frame=HeadFixture();frame.sequence+=1000;model.Tick(frame,.05f,false);
 }
 void HeadInject(ref HeadPilotFrame frame,int count,bool tickGame=false,bool flying=true,float dt=.02f){for(int i=0;i<count;i++){frame.sequence++;headPilot.Inject(frame,dt,flying);if(tickGame)Tick(dt);}}
 IEnumerator HeadPilotChecks(){
  yield return new WaitForSeconds(1);var checks=new Dictionary<string,bool>();
  var privateFiles=new List<string>{SettingsPath,Path.Combine(Application.persistentDataPath,"wayfarer-head-pilot-v1.json")};for(int i=1;i<=3;i++)privateFiles.Add(Path.Combine(Application.persistentDataPath,"wayfarer-campaign-slot-"+i+".json"));
  var stamps=new List<DateTime>();foreach(string path in privateFiles)stamps.Add(File.Exists(path)?File.GetLastWriteTimeUtc(path):DateTime.MinValue);
  checks["native_frame_abi_matches_managed_layout"]=headPilot.Supported&&HeadPilotCamera.fp_frame_size()==Marshal.SizeOf(typeof(HeadPilotFrame));
  checks["camera_starts_off_without_a_preview"]=!headPilot.Requested&&headPilot.Preview==null&&HeadPilotCamera.NativeSnapshot.status==0;
  checks["automated_mode_cannot_request_camera_permission"]=!headPilot.StartCamera()&&!headPilot.Requested&&HeadPilotCamera.NativeSnapshot.status==0;
  var directory=new DirectoryInfo(Application.dataPath);while(directory!=null&&!directory.Name.EndsWith(".app"))directory=directory.Parent;
  string info=directory==null?"":File.ReadAllText(Path.Combine(directory.FullName,"Contents/Info.plist"));
  checks["app_declares_camera_permission_and_continuity_support"]=info.Contains("NSCameraUsageDescription")&&info.Contains("NSCameraUseContinuityCameraDeviceType")&&info.Contains("optional local face-flight");
  byte[] raw=File.ReadAllBytes(Path.Combine(EvidenceDir(),"head-pilot-face.bgra"));HeadPilotFrame detected;
  checks["apple_vision_detects_pose_in_public_test_image"]=HeadPilotCamera.fp_analyse_bgra(raw,raw.Length,512,512,2048,out detected)==1&&detected.faces==1&&detected.valid==1&&detected.confidence>.65f;
  HeadPilotFrame blank;byte[] black=new byte[256*256*4];checks["blank_frames_never_become_pilot_input"]=HeadPilotCamera.fp_analyse_bgra(black,black.Length,256,256,1024,out blank)==1&&blank.faces==0&&blank.valid==0;
  checks["native_detector_rejects_truncated_frame_buffers"]=HeadPilotCamera.fp_analyse_bgra(raw,100,512,512,2048,out blank)==0;
  checks["native_detector_rejects_invalid_stride"]=HeadPilotCamera.fp_analyse_bgra(raw,raw.Length,512,512,1,out blank)==0;
  checks["offline_detection_never_opens_the_camera"]=HeadPilotCamera.NativeSnapshot.status==0&&!headPilot.Requested;
  float[] landmarks={-30,-1.5f,30,-1.5f,30,1.5f,-30,1.5f,-40,-3,40,-3,40,3,-40,3,-31,40,-29,40,29,40,31,40};
  float mouth0,width0,mouthTilt,widthTilt,mouthNear,widthNear;HeadPilotCamera.fp_mouth_metrics(landmarks,landmarks.Length,4,4,2,2,0,out mouth0,out width0);
  var rotated=(float[])landmarks.Clone();var closer=(float[])landmarks.Clone();float ca=Mathf.Cos(25*Mathf.Deg2Rad),sa=Mathf.Sin(25*Mathf.Deg2Rad);
  for(int i=0;i<landmarks.Length;i+=2){rotated[i]=landmarks[i]*ca-landmarks[i+1]*sa;rotated[i+1]=landmarks[i]*sa+landmarks[i+1]*ca;closer[i]*=1.4f;closer[i+1]*=1.4f;}
  HeadPilotCamera.fp_mouth_metrics(rotated,rotated.Length,4,4,2,2,25,out mouthTilt,out widthTilt);HeadPilotCamera.fp_mouth_metrics(closer,closer.Length,4,4,2,2,0,out mouthNear,out widthNear);
  checks["head_tilt_does_not_turn_closed_lips_into_throttle"]=Mathf.Abs(mouth0-.05f)<.001f&&Mathf.Abs(mouthTilt-mouth0)<.001f&&Mathf.Abs(widthTilt-width0)<.001f;
  checks["moving_closer_does_not_mimic_a_smile"]=Mathf.Abs(mouthNear-mouth0)<.001f&&Mathf.Abs(widthNear-width0)<.001f;
  var options=new HeadPilotSettings();var model=new HeadPilotModel(options);var frame=HeadFixture();
  checks["mouth_controls_are_enabled_for_hands_free_flight"]=options.mouthControls;
  checks["uncalibrated_input_cannot_arm"]=!model.Arm();model.BeginCalibration();frame.faces=0;HeadFeed(model,ref frame,50,false);
  checks["missing_face_cannot_calibrate"]=!model.Calibrated;
  model.BeginCalibration();frame=HeadFixture();for(int i=0;i<60;i++){frame.yaw=i%2==0?-10:15;HeadFeed(model,ref frame,1,false);}
  checks["moving_head_cannot_capture_a_false_neutral"]=!model.Calibrated;
  HeadCalibrate(model,ref frame);checks["neutral_and_two_expressions_calibrate_separately"]=model.Calibrated&&model.CalibrationStep==0;
  checks["calibrated_visible_face_can_arm"]=model.Arm();frame.yaw+=15;HeadFeed(model,ref frame,10);
  checks["steering_waits_for_a_neutral_head_after_arming"]=model.NeedsNeutral&&model.Axes.sqrMagnitude==0;
  frame=HeadFixture();frame.sequence+=2000;HeadFeed(model,ref frame,12);checks["neutral_settle_engages_steering"]=!model.NeedsNeutral&&model.Armed;
  frame.yaw+=15;frame.pitch+=12;frame.roll+=15;HeadFeed(model,ref frame,15);
  checks["head_turn_nod_and_tilt_drive_three_axes"]=model.Axes.x>.3f&&model.Axes.y>.3f&&model.Axes.z>.3f;
  checks["steering_rates_are_bounded"]=Mathf.Abs(model.Axes.x)<=1&&Mathf.Abs(model.Axes.y)<=1&&Mathf.Abs(model.Axes.z)<=1;
  options.invertYaw=options.invertPitch=options.invertRoll=true;HeadFeed(model,ref frame,20);checks["all_head_axes_can_be_inverted"]=model.Axes.x<-.3f&&model.Axes.y<-.3f&&model.Axes.z<-.3f;options.invertYaw=options.invertPitch=options.invertRoll=false;
  frame=HeadFixture();frame.sequence+=3000;frame.yaw+=1;frame.pitch+=1;frame.roll+=1;HeadFeed(model,ref frame,30);checks["calibrated_deadzone_suppresses_small_head_jitter"]=model.Axes.magnitude<.001f;
  frame=HeadFixture();frame.sequence+=4000;frame.mouthOpen=.8f;HeadFeed(model,ref frame,3);checks["brief_mouth_motion_does_not_add_throttle"]=model.ThrottleRate==0;HeadFeed(model,ref frame,6);
  checks["held_open_mouth_adds_throttle"]=model.ThrottleRate>0&&!model.Brake;
  frame.mouthOpen=.04f;HeadFeed(model,ref frame,1);checks["relaxed_mouth_stops_adding_throttle"]=model.ThrottleRate==0;
  frame.mouthWidth=.6f;HeadFeed(model,ref frame,9);checks["held_smile_brakes_without_thrust"]=model.Brake&&model.ThrottleRate==0;
  frame.mouthWidth=.4f;HeadFeed(model,ref frame,1);checks["relaxing_the_smile_releases_braking"]=!model.Brake;
  model.Suspend();frame.mouthOpen=.8f;HeadFeed(model,ref frame,20);checks["held_expression_cannot_bypass_neutral_rearm"]=model.NeedsNeutral&&model.ThrottleRate==0;
  frame=HeadFixture();frame.sequence+=5000;HeadFeed(model,ref frame,10);model.Tick(frame,.05f,false);checks["menus_suspend_all_face_flight_outputs"]=model.Axes.sqrMagnitude==0&&model.ThrottleRate==0&&!model.Brake&&model.NeedsNeutral;
  HeadFeed(model,ref frame,10);frame.faces=0;HeadFeed(model,ref frame,1);checks["lost_tracking_immediately_clears_flight_intent"]=model.Axes.sqrMagnitude==0&&model.ThrottleRate==0&&!model.Brake;
  HeadFeed(model,ref frame,10);checks["sustained_tracking_loss_disarms_and_requests_pause"]=!model.Armed&&model.ConsumeSafetyPause()&&!model.ConsumeSafetyPause();frame=HeadFixture();frame.sequence+=6000;HeadFeed(model,ref frame,30);
  checks["tracking_recovery_never_rearms_automatically"]=!model.Armed;
  model.Arm();HeadFeed(model,ref frame,12);frame.faces=2;HeadFeed(model,ref frame,1);checks["a_second_face_immediately_disarms_steering"]=!model.Armed&&model.ConsumeSafetyPause();
  frame=HeadFixture();frame.age=.5f;checks["stale_video_frames_are_not_usable"]=!frame.Usable;frame=HeadFixture();frame.yaw=float.NaN;checks["nonfinite_pose_is_not_usable"]=!frame.Usable;
  var settingsFile=Path.Combine(EvidenceDir(),"head-pilot-options-qa.json");options.sensitivity=1.4f;CampaignFiles.Write(settingsFile,options,x=>x.Validate());var restored=CampaignFiles.Read<HeadPilotSettings>(settingsFile,x=>x.Validate());checks["head_options_roundtrip_without_storing_calibration"]=restored!=null&&restored.sensitivity==1.4f&&!File.ReadAllText(settingsFile).Contains("neutral");
  checks["invalid_head_options_are_rejected"]=!new HeadPilotSettings{smoothing=float.NaN}.Validate()&&!new HeadPilotSettings{sensitivity=10}.Validate();
  var headOnly=new HeadPilotModel(new HeadPilotSettings{mouthControls=false});var headOnlyFrame=HeadFixture();HeadCalibrate(headOnly,ref headOnlyFrame);bool headOnlyReady=headOnly.Calibrated&&headOnly.Arm();HeadFeed(headOnly,ref headOnlyFrame,12);headOnlyFrame.mouthOpen=.8f;headOnlyFrame.mouthWidth=.6f;HeadFeed(headOnly,ref headOnlyFrame,20);
  checks["head_only_mode_leaves_throttle_and_brake_to_normal_controls"]=headOnlyReady&&headOnly.ThrottleRate==0&&!headOnly.Brake;
  // Exercise the actual game input path and native motion, rather than the model alone.
  NewCampaign(3);expedition.ResetCrew();wheelsRequested=false;wheelDeployment=0;landingAssist=cruising=cruiseBraking=false;invulnerable=10000;chapterBanner=0;
  player=Make(ports[0].global-OriginGlobal+Vector3.back*20000,Vector3.zero);FloatingOrigin();Stream(true);shipRotation=desiredRotation=Quaternion.identity;throttle=0;
  headPilot.Model.BeginCalibration();frame=HeadFixture();HeadInject(ref frame,80,false,false);frame.mouthOpen=.8f;HeadInject(ref frame,60,false,false);frame.mouthOpen=.04f;frame.mouthWidth=.6f;HeadInject(ref frame,60,false,false);frame=HeadFixture();frame.sequence+=7000;HeadInject(ref frame,1,false,false);headPilot.Model.Arm();HeadInject(ref frame,24,false,true);
  Quaternion before=shipRotation;frame.yaw+=18;HeadInject(ref frame,40,true);checks["head_pose_rotates_the_actual_ship"]=Quaternion.Angle(before,shipRotation)>15&&Quaternion.Angle(rocket.rotation,shipRotation)<.01f;
  frame=HeadFixture();frame.sequence+=8000;frame.mouthOpen=.8f;HeadInject(ref frame,65,true);checks["mouth_throttle_accelerates_through_native_physics"]=throttle>.25f&&Vector3.Dot(Vel(player),ShipForward)>.2f;
  var pad=InputSystem.AddDevice<Gamepad>();Controls.TestDevice=pad;PadFeed(pad,new GamepadState());PadFeed(pad,new GamepadState().WithButton(GamepadButton.South),.01f);PadFeed(pad,new GamepadState(),.01f);float headThrottle=throttle;HeadInject(ref frame,10,true);
  checks["connected_idle_controller_does_not_cancel_mouth_throttle"]=Controls.UsingPad&&headPilot.Model.Armed&&throttle>=headThrottle;
  headPilot.Settings.mouthControls=false;frame.mouthOpen=.04f;HeadInject(ref frame,1);PadFeed(pad,new GamepadState{rightTrigger=.7f},.02f);
  checks["head_only_steering_supports_controller_trigger_throttle"]=headPilot.Model.Armed&&throttle>.5f;headPilot.Settings.mouthControls=true;
  PadFeed(pad,new GamepadState{rightStick=Vector2.right},.02f);checks["controller_stick_can_take_over_from_head_steering"]=!headPilot.Model.Armed;
  Controls.TestDevice=null;InputSystem.RemoveDevice(pad);frame=HeadFixture();frame.sequence+=8500;HeadInject(ref frame,1,false,false);headPilot.Model.Arm();HeadInject(ref frame,24);
  headPilot.Model.Suspend();throttle=.8f;frame.mouthOpen=.8f;HeadInject(ref frame,30,true);checks["neutral_gate_cuts_existing_ship_throttle"]=headPilot.Model.NeedsNeutral&&throttle==0;
  frame=HeadFixture();frame.sequence+=8700;HeadInject(ref frame,24);
  player=Make(Pos(player),ShipForward*12);frame.mouthOpen=.04f;frame.mouthWidth=.6f;HeadInject(ref frame,55,true);checks["smile_braking_reduces_actual_native_velocity"]=throttle==0&&Vel(player).magnitude<11;
  frame=HeadFixture();frame.sequence+=9000;HeadInject(ref frame,1);int weapons=fired;HeadInject(ref frame,15,true);checks["face_gestures_do_not_fire_weapons"]=fired==weapons;
  frame.faces=0;HeadInject(ref frame,24,true);checks["tracking_loss_opens_real_pause_and_unlocks_cursor"]=paused&&voyagerPanel==VoyagerPanel.Pause&&Cursor.lockState==CursorLockMode.None&&!headPilot.Model.Armed&&throttle==0;
  headPilot.Model.Reset();checks["stopping_clears_session_calibration"]=!headPilot.Model.Calibrated&&headPilot.Model.Axes.sqrMagnitude==0;
  OpenVoyager(VoyagerPanel.HeadPilot);toastTime=0;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"head-pilot-setup.png"));
  bool privatePreserved=true;for(int i=0;i<privateFiles.Count;i++)privatePreserved&=stamps[i]==(File.Exists(privateFiles[i])?File.GetLastWriteTimeUtc(privateFiles[i]):DateTime.MinValue);
  checks["qa_preserves_all_private_campaign_and_settings_files"]=privatePreserved;
  SaveChecks("head-pilot-runtime-checks.json",checks);yield return new WaitForSeconds(.5f);Application.Quit();
 }
}
