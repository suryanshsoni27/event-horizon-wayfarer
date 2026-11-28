using UnityEngine;

public sealed partial class DefenseGame {
 public PilotInput Controls{get;private set;}
 int workshopSelection;bool controllerRecording;
 public bool PadMode{get{return Controls!=null&&Controls.UsingPad;}}
 public bool PadDown(PilotInput.Button b){return Controls!=null&&Controls.Down(b);}
 public bool PadHeld(PilotInput.Button b){return Controls!=null&&Controls.Held(b);}
 bool MenuUp{get{return Controls!=null&&(Controls.Navigation.y>0||PadDown(PilotInput.Button.Up));}}
 bool MenuDown{get{return Controls!=null&&(Controls.Navigation.y<0||PadDown(PilotInput.Button.Down));}}
 bool MenuLeft{get{return Controls!=null&&(Controls.Navigation.x<0||PadDown(PilotInput.Button.Left));}}
 bool MenuRight{get{return Controls!=null&&(Controls.Navigation.x>0||PadDown(PilotInput.Button.Right));}}
 public bool CrewFire{get{return (!automatedPlayback&&(Input.GetMouseButton(0)||GameKey(KeyCode.LeftControl)))||(Controls!=null&&Controls.R2>.15f);}}
 public bool CrewAim{get{return (preferences!=null&&preferences.toggleAim?aimToggle:!automatedPlayback&&Input.GetMouseButton(1))||(Controls!=null&&(preferences!=null&&preferences.toggleAim?aimToggle:Controls.L2>.15f));}}
 public bool CrewInteract{get{return !civilPlacing&&(preferences!=null&&preferences.toggleInteract?interactionToggle:GameKey(KeyCode.E)||PadHeld(PilotInput.Button.R1));}}
 public bool CrewReload{get{return !civilPlacing&&((!automatedPlayback&&GameKeyDown(KeyCode.R))||PadDown(PilotInput.Button.Square));}}
 public bool CrewPulse{get{return !civilPlacing&&((!automatedPlayback&&GameKeyDown(KeyCode.Q))||PadDown(PilotInput.Button.Circle));}}
 bool PadKeyDown(KeyCode k){
  if(Controls==null)return false;
  if(k==KeyCode.Escape)return PadDown(PilotInput.Button.Options);
  if(k==KeyCode.Return)return paused&&PadDown(PilotInput.Button.Cross);
  if(k==KeyCode.J)return !paused&&PadDown(PilotInput.Button.Triangle);
  if(k==KeyCode.O)return GroundActive&&PadDown(PilotInput.Button.L1);
  if(k==KeyCode.M)return !legacyUniverse&&(PadDown(PilotInput.Button.Share)||Controls.TouchpadDown);
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
  if(Controls.Disconnected&&(!automatedPlayback||Controls.TestDevice!=null)){
   paused=true;releaseFire=true;throttle=0;interactionToggle=aimToggle=false;playerAction=Vector3.zero;roverDrive=roverSteer=0;roverBrake=true;StopRumble();
   Notice("Controller disconnected / paused. Reconnect, release the held buttons, then press Cross or Enter.");Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
   if(voyager!=null)OpenVoyager(campaignStarted?VoyagerPanel.Pause:VoyagerPanel.Home);return true;
  }
  if(VoyagerMenuInput())return true;
  if(outcome!=""){if(PadDown(PilotInput.Button.Cross)){ResetRound();Controls.SuppressUntilNeutral();}return false;}
  if(surface!=null&&surface.Journal&&PadDown(PilotInput.Button.Circle)){surface.Journal=false;if(!journalWasPaused)ResumeFlight();return true;}
  if(chartOpen){
   if(MenuLeft||PadDown(PilotInput.Button.L1)||GameKeyDown(KeyCode.LeftArrow)){chartSystem=(chartSystem+catalog.Count-1)%catalog.Count;chartPort=0;}
   if(MenuRight||PadDown(PilotInput.Button.R1)||GameKeyDown(KeyCode.RightArrow)){chartSystem=(chartSystem+1)%catalog.Count;chartPort=0;}
   if(MenuUp||GameKeyDown(KeyCode.UpArrow))chartPort=(chartPort+3)%4;
   if(MenuDown||GameKeyDown(KeyCode.DownArrow))chartPort=(chartPort+1)%4;
   if(PadDown(PilotInput.Button.Cross)||GameKeyDown(KeyCode.Return)||GameKeyDown(KeyCode.T)){EngageRoute();Controls.SuppressUntilNeutral();return true;}
   if(PadDown(PilotInput.Button.Circle)||GameKeyDown(KeyCode.Escape)||GameKeyDown(KeyCode.M)){ToggleChart();Controls.SuppressUntilNeutral();return true;}
   return true;
  }
  if(upgradeOpen){
   if(MenuUp||GameKeyDown(KeyCode.UpArrow))workshopSelection=(workshopSelection+3)%4;
   if(MenuDown||GameKeyDown(KeyCode.DownArrow))workshopSelection=(workshopSelection+1)%4;
   if(PadDown(PilotInput.Button.Cross)||GameKeyDown(KeyCode.Return))Fit(workshopSelection);
   for(int i=0;i<4;i++)if(GameKeyDown((KeyCode)((int)KeyCode.Alpha1+i)))Fit(i);
   if(PadDown(PilotInput.Button.Circle)||GameKeyDown(KeyCode.U)||GameKeyDown(KeyCode.Escape)){upgradeOpen=false;ResumeFlight();return true;}
   return true;
  }
  return false;
 }
 void ManualFlightTakeover(){
  if(paused||SurfaceVehicle||outcome!=""||inTidal||orbit)return;
  bool manual=Controls.R2>.15f||Controls.L2>.15f||Controls.Move.sqrMagnitude>.12f||Controls.Look.sqrMagnitude>.12f||PadHeld(PilotInput.Button.Cross);
  if(!manual)return;
  if(landingAssist){landingAssist=false;throttle=0;desiredRotation=shipRotation;Notice("Manual flight / controller took over VTOL descent. D-pad up resumes landing assist.");}
  if(cruising){cruising=false;cruiseBraking=Controls.L2>.15f||Vel(player).magnitude>100;Notice(cruiseBraking?"Manual control requested / slowing from cruise speed.":"Cruise disengaged / manual flight.");}
 }
 void CancelRebinding(){rebindAction=rebindKey=-1;rebindOptionsAt=-1;Controls.SuppressUntilNeutral();Toast("Binding cancelled / previous controls preserved.");}
 void CompleteControllerBinding(int physical){
  preferences.BindButton(rebindAction,physical);rebindAction=-1;rebindOptionsAt=-1;ApplyPreferences();Controls.SuppressUntilNeutral();
 }
 void ControllerRebindingInput(float now=-1){
  if(now<0)now=Time.unscaledTime;if(now<rebindReadyAt)return;
  if(rebindKey>=0){if(PadDown(PilotInput.Button.Options)||PadDown(PilotInput.Button.Circle))CancelRebinding();return;}
  if(rebindAction<0)return;
  // A short Options press can be assigned. Holding it always offers a way out.
  if(Controls.PhysicalHeld(8)){
   if(rebindOptionsAt<0)rebindOptionsAt=now;
   if(now-rebindOptionsAt>=.65f)CancelRebinding();return;
  }
  if(rebindOptionsAt>=0){CompleteControllerBinding(8);return;}
  int physical=Controls.PhysicalPressed();if(physical>=0)CompleteControllerBinding(physical);
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
