using UnityEngine;

public sealed partial class DefenseGame {
 public PilotInput Controls{get;private set;}
 int workshopSelection;bool controllerRecording;
 public bool PadMode{get{return Controls!=null&&Controls.UsingPad;}}
 public bool PadDown(PilotInput.Button b){return Controls!=null&&Controls.Down(b);}
 public bool PadHeld(PilotInput.Button b){return Controls!=null&&Controls.Held(b);}
 public bool CrewFire{get{return (!automatedPlayback&&(Input.GetMouseButton(0)||Input.GetKey(KeyCode.LeftControl)))||(Controls!=null&&Controls.R2>.15f);}}
 public bool CrewAim{get{return (!automatedPlayback&&Input.GetMouseButton(1))||(Controls!=null&&Controls.L2>.15f);}}
 public bool CrewInteract{get{return !civilPlacing&&((!automatedPlayback&&Input.GetKey(KeyCode.E))||PadHeld(PilotInput.Button.R1));}}
 public bool CrewReload{get{return !civilPlacing&&((!automatedPlayback&&Input.GetKeyDown(KeyCode.R))||PadDown(PilotInput.Button.Square));}}
 public bool CrewPulse{get{return !civilPlacing&&((!automatedPlayback&&Input.GetKeyDown(KeyCode.Q))||PadDown(PilotInput.Button.Circle));}}
 bool PadKeyDown(KeyCode k){
  if(Controls==null)return false;
  if(k==KeyCode.Escape)return PadDown(PilotInput.Button.Options);
  if(k==KeyCode.Return)return paused&&PadDown(PilotInput.Button.Cross);
  if(k==KeyCode.J)return !paused&&PadDown(PilotInput.Button.Triangle);
  if(k==KeyCode.O)return GroundActive&&PadDown(PilotInput.Button.L1);
  if(k==KeyCode.M)return !legacyUniverse&&PadDown(PilotInput.Button.Share);
  if(k==KeyCode.U)return !paused&&!GroundActive&&!RoverDeployed&&PadDown(PilotInput.Button.R3);
  if(paused||GroundActive||RoverDeployed)return false;
  if(k==KeyCode.K)return PadDown(PilotInput.Button.Up);
  if(k==KeyCode.Y)return PadDown(PilotInput.Button.Circle);
  if(k==KeyCode.Alpha9)return PadDown(PilotInput.Button.Left);
  if(k==KeyCode.Alpha0)return PadDown(PilotInput.Button.Right);
  return false;
 }
 // Navigation runs before flight input. The same button cannot operate a menu and fire a weapon.
 bool ControllerMenu(){
  Controls.Poll();
  if(Controls.Disconnected&&(!automatedPlayback||Controls.TestDevice!=null)){paused=true;releaseFire=true;playerAction=Vector3.zero;roverDrive=roverSteer=0;Notice("Controller disconnected / paused. Reconnect, then press Cross or Enter.");Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
  if(outcome!=""){if(PadDown(PilotInput.Button.Cross)){ResetRound();Controls.SuppressUntilNeutral();}return false;}
  if(surface!=null&&surface.Journal&&PadDown(PilotInput.Button.Circle)){surface.Journal=false;if(!journalWasPaused)ResumeFlight();return true;}
  if(chartOpen){
   if(PadDown(PilotInput.Button.Left)||PadDown(PilotInput.Button.L1)){chartSystem=(chartSystem+catalog.Count-1)%catalog.Count;chartPort=0;}
   if(PadDown(PilotInput.Button.Right)||PadDown(PilotInput.Button.R1)){chartSystem=(chartSystem+1)%catalog.Count;chartPort=0;}
   if(PadDown(PilotInput.Button.Up))chartPort=(chartPort+3)%4;
   if(PadDown(PilotInput.Button.Down))chartPort=(chartPort+1)%4;
   if(PadDown(PilotInput.Button.Cross)){EngageRoute();Controls.SuppressUntilNeutral();return true;}
   if(PadDown(PilotInput.Button.Circle)||PadDown(PilotInput.Button.Options)){ToggleChart();Controls.SuppressUntilNeutral();return true;}
  }
  if(upgradeOpen){
   if(PadDown(PilotInput.Button.Up))workshopSelection=(workshopSelection+3)%4;
   if(PadDown(PilotInput.Button.Down))workshopSelection=(workshopSelection+1)%4;
   if(PadDown(PilotInput.Button.Cross))Fit(workshopSelection);
   if(PadDown(PilotInput.Button.Circle)||PadDown(PilotInput.Button.R3)||PadDown(PilotInput.Button.Options)){upgradeOpen=false;ResumeFlight();return true;}
  }
  return false;
 }
 void DrawControllerHints(bool overlay){
  if(legacyUniverse)return;
  if(!overlay){
   if(RoverDeployed&&!GroundActive){Label(430,902,PadMode?"ROVER / R2 forward • L2 brake / reverse • left stick steer • Cross handbrake":"ROVER / W/S drive • A/D steer • Space handbrake • mouse/arrows camera",small);Label(430,930,PadMode?"Right stick camera • Triangle exit • D-pad down retract tires • Options pause":"J exit / board • 6 retract tires • Esc pause",small);}
   else if(PadMode){Label(420,901,GroundActive?"Left stick move • right stick aim • L3 run • Cross hop • R2 fire • L2 aim":"Sticks: strafe / look • R2 thrust • L2 brake • L1/R1 roll • Cross boost • Square fire",small);Label(420,930,GroundActive?"R1 hold interact • Square reload • Circle pulse • L1 missions • Triangle board":"Triangle exit • Share atlas • D-pad: up land / down tires / left black hole / right service",small);}
  }
  if(paused&&!controllerRecording&&!cosmosRecording&&!upgradeOpen&&!chartOpen&&(surface==null||!surface.Journal)&&outcome==""&&!HelmetPhotoUI.BlocksControls&&!inTidal){Panel(new Rect(360,337,880,239));Label(380,349,"DUALSENSE / "+(Controls.Connected?Controls.DeviceName:"Connect your PS5 controller via USB or Bluetooth"),text);Label(380,390,"Sticks move / look • R2 thrust or fire • L2 brake or aim • Options pauses",small);Label(380,424,"Share: star atlas • D-pad + shoulders select • Cross confirms • Circle returns",small);Label(380,458,"Flight: D-pad up VTOL • down deploy tires • left black hole • right service",small);Label(380,492,"Rover: R2 drive / L2 reverse • left stick steer • Triangle exit • down retract",small);Label(380,526,"Boots: L3 run • Cross hop • R1 interact • Square reload • Circle pulse • L1 journal",small);Label(380,560,"Keyboard still works. Land, settle, then press 6 / D-pad down for tires.",small);}
 }
}
