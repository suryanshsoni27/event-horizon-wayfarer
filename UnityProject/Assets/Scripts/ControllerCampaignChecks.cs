using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

// Raw transport fixtures exercise Unity's actual HID preprocessing, not just GamepadState.
[StructLayout(LayoutKind.Explicit,Size=64)] public struct ControllerUSBReport:IInputStateTypeInfo {
 public FourCC format{get{return new FourCC('H','I','D');}}
 [FieldOffset(0)]public byte id;[FieldOffset(1)]public byte lx;[FieldOffset(2)]public byte ly;[FieldOffset(3)]public byte rx;[FieldOffset(4)]public byte ry;
 [FieldOffset(5)]public byte l2;[FieldOffset(6)]public byte r2;[FieldOffset(8)]public byte buttons0;[FieldOffset(9)]public byte buttons1;[FieldOffset(10)]public byte buttons2;
}
[StructLayout(LayoutKind.Explicit,Size=78)] public struct ControllerBluetoothReport:IInputStateTypeInfo {
 public FourCC format{get{return new FourCC('H','I','D');}}
 [FieldOffset(0)]public byte id;[FieldOffset(2)]public byte lx;[FieldOffset(3)]public byte ly;[FieldOffset(4)]public byte rx;[FieldOffset(5)]public byte ry;
 [FieldOffset(6)]public byte l2;[FieldOffset(7)]public byte r2;[FieldOffset(9)]public byte buttons0;[FieldOffset(10)]public byte buttons1;[FieldOffset(11)]public byte buttons2;
}
[StructLayout(LayoutKind.Explicit,Size=10)] public struct ControllerMinimalReport:IInputStateTypeInfo {
 public FourCC format{get{return new FourCC('H','I','D');}}
 [FieldOffset(0)]public byte id;[FieldOffset(1)]public byte lx;[FieldOffset(2)]public byte ly;[FieldOffset(3)]public byte rx;[FieldOffset(4)]public byte ry;
 [FieldOffset(5)]public byte buttons0;[FieldOffset(6)]public byte buttons1;[FieldOffset(7)]public byte buttons2;[FieldOffset(8)]public byte l2;[FieldOffset(9)]public byte r2;
}

public sealed partial class DefenseGame {
 void CampaignPadTap(Gamepad pad,GamepadButton button){PadFeed(pad,new GamepadState());PadFeed(pad,Press(button));PadFeed(pad,new GamepadState());}
 void CampaignPadDefaults(){preferences=new VoyagerSettings();Controls.Settings=preferences;interactionToggle=aimToggle=false;rebindAction=rebindKey=-1;}
 void CampaignRawFeed<T>(Gamepad pad,T state,float now=-1)where T:struct,IInputStateTypeInfo{InputSystem.QueueStateEvent(pad,state);InputSystem.Update();Controls.Poll(now);}
 IEnumerator ControllerCampaignChecks(){
  yield return new WaitForSeconds(1);
  var checks=new Dictionary<string,bool>();
  var realStamps=new Dictionary<string,DateTime>();
  foreach(string file in new[]{"wayfarer-settings-v1.json","wayfarer-campaign-slot-1.json","wayfarer-campaign-slot-2.json","wayfarer-campaign-slot-3.json"}){
   string path=Path.Combine(Application.persistentDataPath,file);realStamps[path]=File.Exists(path)?File.GetLastWriteTimeUtc(path):DateTime.MinValue;
  }
  var pad=InputSystem.AddDevice<Gamepad>();Controls.TestDevice=pad;PadFeed(pad,new GamepadState());
  checks["campaign_controller_uses_isolated_saves"]=voyager!=null&&CampaignPath(1).StartsWith(EvidenceDir());
  OpenVoyager(VoyagerPanel.Home);PadFeed(pad,new GamepadState());
  PadFeed(pad,new GamepadState{leftStick=Vector2.down});
  checks["left_stick_selects_campaign_menu"]=voyagerSelection==1;
  PadFeed(pad,new GamepadState());OpenVoyager(VoyagerPanel.Home);PadFeed(pad,new GamepadState());
  PadFeed(pad,new GamepadState{leftStick=Vector2.right});
  checks["left_stick_changes_save_slot"]=slotSelection==2;
  NewCampaign(1);CampaignPadDefaults();PadFeed(pad,new GamepadState());OpenVoyager(VoyagerPanel.Pause);
  PadFeed(pad,new GamepadState{rightStick=Vector2.right});
  PadFeed(pad,new GamepadState{rightStick=Vector2.right}.WithButton(GamepadButton.DpadDown));
  checks["held_camera_stick_does_not_block_menu_buttons"]=voyagerSelection==1;
  PadFeed(pad,new GamepadState());OpenVoyager(VoyagerPanel.Settings);settingsPage=1;
  CampaignPadTap(pad,GamepadButton.South);rebindReadyAt=0;CampaignPadTap(pad,GamepadButton.Start);
  checks["controller_cancels_keyboard_rebinding"]=rebindKey<0;
  CampaignPadDefaults();OpenVoyager(VoyagerPanel.Settings);settingsPage=2;CampaignPadTap(pad,GamepadButton.South);rebindReadyAt=0;
  PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.West));
  checks["controller_rebinding_runs_in_input_tick"]=rebindAction<0&&preferences.buttons[0]==2&&preferences.buttons[2]==0;
  PadFeed(pad,Press(GamepadButton.West));PadFeed(pad,Press(GamepadButton.West));
  checks["holding_newly_assigned_button_cannot_reopen_capture"]=rebindAction<0&&preferences.buttons[0]==2;
  PadFeed(pad,new GamepadState());
  CampaignPadDefaults();preferences.BindButton(11,0);
  checks["dpad_hints_follow_rebindings"]=ControlHints("D-pad down")=="Cross";
  CampaignPadDefaults();StageSurface();voyagerPanel=VoyagerPanel.None;ResumeFlight();PadFeed(pad,new GamepadState());
  CampaignPadTap(pad,GamepadButton.Select);int system=chartSystem;CampaignPadTap(pad,GamepadButton.LeftShoulder);
  checks["surface_atlas_shoulders_do_not_open_journal"]=chartOpen&&!surface.Journal&&chartSystem==(system+catalog.Count-1)%catalog.Count;
  surface.Journal=false;civilOpen=false;chartOpen=false;voyagerPanel=VoyagerPanel.None;ResumeFlight();PadFeed(pad,new GamepadState());
  CampaignPadTap(pad,GamepadButton.Select);CampaignPadTap(pad,GamepadButton.DpadDown);
  checks["surface_atlas_dpad_does_not_open_civilization"]=chartOpen&&!civilOpen&&chartPort==1;
  chartOpen=false;civilOpen=false;surface.Journal=false;OpenVoyager(VoyagerPanel.Pause);preferences.toggleInteract=true;CampaignPadTap(pad,GamepadButton.RightShoulder);
  checks["menu_shoulders_do_not_toggle_world_interactions"]=!interactionToggle;
  CampaignPadDefaults();NewCampaign(1);PadFeed(pad,new GamepadState());
  PadFeed(pad,new GamepadState{rightTrigger=.8f,rightStick=new Vector2(.6f,.3f)},.05f);
  checks["thrust_and_steering_take_over_automatic_landing"]=!landingAssist&&throttle>.7f&&playerAction.magnitude>1;
  NewCampaign(1);PadFeed(pad,new GamepadState());PadFeed(pad,new GamepadState{leftTrigger=1},.02f);
  checks["brake_takes_over_automatic_landing"]=!landingAssist;
  NewCampaign(1);PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.South),.02f);
  checks["boost_takes_over_automatic_landing"]=!landingAssist;
  NewCampaign(1);PadFeed(pad,new GamepadState());Quaternion rollBefore=shipRotation;PadFeed(pad,Press(GamepadButton.LeftShoulder),.1f);
  checks["l1_roll_takes_over_automatic_landing"]=!landingAssist&&Quaternion.Angle(rollBefore,shipRotation)>1;
  NewCampaign(1);PadFeed(pad,new GamepadState());rollBefore=shipRotation;PadFeed(pad,Press(GamepadButton.RightShoulder),.1f);
  checks["r1_roll_takes_over_automatic_landing"]=!landingAssist&&Quaternion.Angle(rollBefore,shipRotation)>1;
  NewCampaign(1);PadFeed(pad,new GamepadState());PadFeed(pad,Press(GamepadButton.East),.1f);
  checks["circle_level_takes_over_automatic_landing"]=!landingAssist;
  NewCampaign(1);landingAssist=false;PadFeed(pad,new GamepadState());PadFeed(pad,new GamepadState{rightTrigger=.8f},.02f);PadFeed(pad,new GamepadState(),.02f);
  checks["releasing_r2_stops_main_thrust"]=throttle==0;
  OpenVoyager(VoyagerPanel.Outfitter);playerAction=Vector3.one;throttle=.7f;InputSystem.RemoveDevice(pad);Tick(.02f);
  checks["disconnect_inside_menu_clears_inputs_and_opens_pause"]=voyagerPanel==VoyagerPanel.Pause&&paused&&playerAction==Vector3.zero&&throttle==0;
  pad=InputSystem.AddDevice<Gamepad>();Controls.TestDevice=pad;
  PadFeed(pad,new GamepadState{rightStick=Vector2.right}.WithButton(GamepadButton.South));
  checks["reconnect_held_cross_cannot_resume"]=paused;
  PadFeed(pad,new GamepadState{rightStick=Vector2.right});PadFeed(pad,new GamepadState{rightStick=Vector2.right}.WithButton(GamepadButton.South));
  checks["released_cross_resumes_even_with_held_camera_stick"]=!paused;
  PadFeed(pad,new GamepadState());OpenVoyager(VoyagerPanel.Pause);PadFeed(pad,new GamepadState());
  for(int i=0;i<8;i++)CampaignPadTap(pad,GamepadButton.DpadDown);
  CampaignPadTap(pad,GamepadButton.South);
  checks["face_flight_setup_reachable_without_mouse"]=voyagerPanel==VoyagerPanel.HeadPilot;
  CampaignPadTap(pad,GamepadButton.East);
  checks["circle_returns_from_face_setup"]=voyagerPanel==VoyagerPanel.Pause;
  OpenVoyager(VoyagerPanel.Settings);settingsPage=2;CampaignPadTap(pad,GamepadButton.South);rebindReadyAt=0;
  PadFeed(pad,Press(GamepadButton.Start));rebindOptionsAt=Time.unscaledTime-1;PadFeed(pad,Press(GamepadButton.Start));
  checks["holding_options_cancels_button_rebinding"]=rebindAction<0&&preferences.buttons[0]==0;
  PadFeed(pad,new GamepadState());OpenVoyager(VoyagerPanel.Settings);settingsPage=2;CampaignPadTap(pad,GamepadButton.South);rebindReadyAt=0;
  CampaignPadTap(pad,GamepadButton.Start);
  checks["short_options_press_can_still_be_rebound"]=preferences.buttons[0]==8&&preferences.buttons[8]==0;
  CampaignPadDefaults();CloseVoyager();PadFeed(pad,new GamepadState());
  PadFeed(pad,new GamepadState{rightTrigger=1});OpenVoyager(VoyagerPanel.Pause);
  PadFeed(pad,new GamepadState{rightTrigger=1});PadFeed(pad,new GamepadState{rightTrigger=1}.WithButton(GamepadButton.South));
  checks["held_trigger_does_not_block_resume_or_leak_thrust"]=!paused&&Controls.R2==0;
  PadFeed(pad,new GamepadState());PadFeed(pad,new GamepadState{rightTrigger=.8f},.02f);
  checks["released_trigger_rearms_independently"]=throttle>.7f;
  PauseForFocusLoss();
  checks["losing_focus_opens_pause_and_clears_thrust"]=voyagerPanel==VoyagerPanel.Pause&&paused&&throttle==0&&playerAction==Vector3.zero;
  CampaignPadTap(pad,GamepadButton.South);
  checks["controller_can_resume_after_focus_returns"]=!paused&&voyagerPanel==VoyagerPanel.None;
  LandedFixture();voyagerPanel=VoyagerPanel.None;ResumeFlight();PadFeed(pad,new GamepadState());CampaignPadTap(pad,GamepadButton.North);
  PadFeed(pad,new GamepadState{rightTrigger=1},.02f);PadFeed(pad,new GamepadState{rightTrigger=1}.WithButton(GamepadButton.North),.02f);
  checks["boarding_with_fire_held_cannot_launch_ship"]=!GroundActive&&Controls.R2==0&&throttle==0;
  PadFeed(pad,new GamepadState());Controls.SuppressUntilNeutral();CampaignRawFeed(pad,new GamepadState(),100);
  CampaignRawFeed(pad,new GamepadState{leftStick=new Vector2(.3f,.2f)},100.1f);
  checks["menu_stick_threshold_ignores_small_movements"]=Controls.Navigation==Vector2Int.zero;
  CampaignRawFeed(pad,new GamepadState{leftStick=Vector2.down},100.2f);
  checks["menu_stick_initial_move_is_immediate"]=Controls.Navigation==Vector2Int.down;
  CampaignRawFeed(pad,new GamepadState{leftStick=Vector2.down},100.3f);
  checks["held_menu_stick_does_not_repeat_every_frame"]=Controls.Navigation==Vector2Int.zero;
  CampaignRawFeed(pad,new GamepadState{leftStick=Vector2.down},100.6f);
  checks["held_menu_stick_repeats_after_delay"]=Controls.Navigation==Vector2Int.down;
  CampaignRawFeed(pad,new GamepadState{leftStick=Vector2.right},100.61f);
  checks["changing_menu_direction_moves_immediately"]=Controls.Navigation==Vector2Int.right;
  PadFeed(pad,new GamepadState());InputSystem.RemoveDevice(pad);Controls.TestDevice=null;Controls.Poll();paused=true;
  var dual=InputSystem.AddDevice(new InputDeviceDescription{interfaceName="HID",manufacturer="Sony Interactive Entertainment",product="Wireless Controller",capabilities="{\"vendorId\":1356,\"productId\":3302}"})as Gamepad;Controls.TestDevice=dual;
  checks["sony_vid_pid_selects_dualsense_layout"]=dual is DualSenseGamepadHID;
  var usb=new ControllerUSBReport{id=1,lx=128,ly=128,rx=128,ry=128,buttons0=8};CampaignRawFeed(dual,usb);
  usb.buttons0=0xf8;usb.buttons1=0xf3;usb.lx=240;usb.ry=20;usb.l2=200;usb.r2=240;CampaignRawFeed(dual,usb);
  checks["usb_hid_report_maps_face_and_system_buttons"]=PadDown(PilotInput.Button.Cross)&&PadDown(PilotInput.Button.Circle)&&PadDown(PilotInput.Button.Square)&&PadDown(PilotInput.Button.Triangle)&&PadDown(PilotInput.Button.Options)&&PadDown(PilotInput.Button.Share);
  checks["usb_hid_report_maps_sticks_and_analog_triggers"]=Controls.Move.x>.7f&&Controls.Look.y>.7f&&Controls.L2>.6f&&Controls.R2>.8f;
  var bt=new ControllerBluetoothReport{id=0x31,lx=128,ly=128,rx=128,ry=128,buttons0=8};CampaignRawFeed(dual,bt);
  bt.buttons0=0xf8;bt.buttons1=0xf3;bt.lx=20;bt.ry=240;bt.l2=200;bt.r2=240;CampaignRawFeed(dual,bt);
  checks["bluetooth_hid_report_maps_buttons_and_shoulders"]=PadDown(PilotInput.Button.Cross)&&PadDown(PilotInput.Button.Circle)&&PadDown(PilotInput.Button.L1)&&PadDown(PilotInput.Button.R1)&&PadDown(PilotInput.Button.L3)&&PadDown(PilotInput.Button.R3);
  checks["bluetooth_hid_report_maps_sticks_and_triggers"]=Controls.Move.x<-.7f&&Controls.Look.y<-.7f&&Controls.L2>.6f&&Controls.R2>.8f;
  bt=new ControllerBluetoothReport{id=0x31,lx=128,ly=128,rx=128,ry=128,buttons0=8};CampaignRawFeed(dual,bt);bt.buttons2=2;CampaignRawFeed(dual,bt);
  checks["touchpad_click_opens_atlas_action"]=Controls.TouchpadDown&&PadKeyDown(KeyCode.M)&&!PadDown(PilotInput.Button.Share);
  var minimal=new ControllerMinimalReport{id=1,lx=128,ly=128,rx=128,ry=128,buttons0=8};CampaignRawFeed(dual,minimal);minimal.buttons0=0x28;minimal.r2=240;CampaignRawFeed(dual,minimal);
  checks["minimal_bluetooth_report_maps_cross_and_r2"]=PadDown(PilotInput.Button.Cross)&&Controls.R2>.8f;
  InputSystem.RemoveDevice(dual);Controls.TestDevice=null;Controls.Poll();
  var edge=InputSystem.AddDevice(new InputDeviceDescription{interfaceName="HID",manufacturer="Sony Interactive Entertainment",product="DualSense Edge Wireless Controller",capabilities="{\"vendorId\":1356,\"productId\":3570}"});
  checks["dualsense_edge_vid_pid_uses_supported_layout"]=edge is DualSenseGamepadHID;InputSystem.RemoveDevice(edge);
  bool realUntouched=true;foreach(var item in realStamps)realUntouched&=(File.Exists(item.Key)?File.GetLastWriteTimeUtc(item.Key):DateTime.MinValue)==item.Value;
  checks["controller_qa_preserves_personal_saves_and_settings"]=realUntouched;
  string json=SurfaceJSON(checks);File.WriteAllText(Path.Combine(EvidenceDir(),"controller-campaign-runtime-checks.json"),json);Debug.Log("CONTROLLER_CAMPAIGN_QA "+json);
  yield return new WaitForSeconds(.2f);Application.Quit();
 }
}
