using System;
using System.IO;
using UnityEngine;

public sealed partial class DefenseGame {
 HeadPilotCamera headPilot;
 bool headPilotQA;
 public bool HeadFlightEligible {get{return voyager!=null&&campaignStarted&&!paused&&voyagerPanel==VoyagerPanel.None&&!HelmetPhotoUI.BlocksControls&&!civilOpen&&!chartOpen&&!upgradeOpen&&!orbit&&!GroundActive&&!RoverDeployed&&!cruising&&!landingAssist&&!cruiseBraking&&!inTidal&&outcome=="";}}
 bool HeadSteeringActive {get{return headPilot!=null&&headPilot.Model.Armed&&HeadFlightEligible&&!headPilot.Model.NeedsNeutral;}}
 void InitializeHeadPilot(string[] args){
  if(voyager==null)return;headPilotQA=Array.IndexOf(args,"--head-pilot-qa")>=0;
  headPilot=gameObject.AddComponent<HeadPilotCamera>();
  headPilot.Initialize(this,automatedPlayback||qa,Path.Combine(qa||automatedPlayback?EvidenceDir():Application.persistentDataPath,"wayfarer-head-pilot-v1.json"));
  headPilot.Testing=headPilotQA;
 }
 public void HeadPilotEmergency(string message){
  if(headPilot!=null)headPilot.Model.Disarm();throttle=0;playerAction=Vector3.zero;desiredRotation=shipRotation;
  if(voyager!=null&&campaignStarted)OpenVoyager(VoyagerPanel.Pause);else paused=true;
  releaseFire=true;Notice(message);Toast(message);
 }
 bool HeadPilotTick(){
  if(headPilot==null)return false;
  if(!automatedPlayback&&!HelmetPhotoUI.BlocksControls&&Input.GetKeyDown(KeyCode.F6)){
   bool armed=headPilot.Model.Armed||headPilot.Model.ConsumeSafetyPause();headPilot.StopCamera();if(armed)HeadPilotEmergency("Camera stopped. Resume with keyboard/controller, or use F7 to set up face flight.");return armed;
  }
  if(headPilot.Model.ConsumeSafetyPause()){HeadPilotEmergency(headPilot.Model.Message);return true;}
  if(headPilot.Model.Armed&&(headPilot.Model.NeedsNeutral||!headPilot.Model.Valid))throttle=0;
  if(headPilot.Model.Armed&&HeadFlightEligible&&!headPilot.Model.Valid){throttle=0;desiredRotation=shipRotation;}
  if(!automatedPlayback&&!HelmetPhotoUI.BlocksControls&&Input.GetKeyDown(KeyCode.F7)){
   if(voyagerPanel==VoyagerPanel.HeadPilot)OpenVoyager(campaignStarted?VoyagerPanel.Pause:VoyagerPanel.Home);else OpenVoyager(VoyagerPanel.HeadPilot);return true;
  }
  if(!automatedPlayback&&!HelmetPhotoUI.BlocksControls&&Input.GetKeyDown(KeyCode.F8)&&headPilot.Requested){
   throttle=0;desiredRotation=shipRotation;OpenVoyager(VoyagerPanel.HeadPilot);headPilot.Model.BeginCalibration();return true;
  }
  return false;
 }
 void HeadPilotFlightInput(float dt,ref float mouseX,ref float mouseY,ref float yaw,ref float pitch,ref float roll){
  if(!HeadSteeringActive)return;
  bool keyTurn=GameKey(KeyCode.LeftArrow)||GameKey(KeyCode.RightArrow)||GameKey(KeyCode.UpArrow)||GameKey(KeyCode.DownArrow)||GameKey(KeyCode.Q)||GameKey(KeyCode.E);
  if(Mathf.Abs(mouseX)+Mathf.Abs(mouseY)>3||Controls.Look.sqrMagnitude>.16f||keyTurn){headPilot.Model.ManualTakeover();Toast("Manual steering took over. Re-arm face flight in F7.");return;}
  mouseX=mouseY=0;
  // Remove mouse contribution already included by the normal input calculation.
  yaw=0;pitch=0;roll=0;
  Vector3 axes=headPilot.Model.Axes;yaw=axes.x*65*dt;pitch=axes.y*55*dt;roll=axes.z*75*dt;
  throttle=Mathf.Clamp01(throttle+headPilot.Model.ThrottleRate*dt);if(headPilot.Model.Brake)throttle=0;
 }
 bool HeadGestureBrake {get{return HeadSteeringActive&&headPilot.Model.Brake;}}
 void SuspendHeadPilot(){if(headPilot!=null&&headPilot.Model.Armed){headPilot.Model.Suspend();throttle=0;desiredRotation=shipRotation;}}
 const int HeadPilotRows=13;
 void HeadPilotAction(int row,int direction=1){
  if(headPilot==null)return;var s=headPilot.Settings;
  if(row==0){if(headPilot.Requested)headPilot.StopCamera();else headPilot.StartCamera();return;}
  if(row==1){headPilot.StopCamera();headPilot.RefreshCameras();if(headPilot.Cameras.Length>0)s.camera=(s.camera+direction+headPilot.Cameras.Length)%headPilot.Cameras.Length;}
  if(row==2){if(headPilot.Requested||headPilot.Testing)headPilot.Model.BeginCalibration();else Toast("Start the camera before calibration.");return;}
  if(row==3){
   if(headPilot.Model.Armed){headPilot.Model.Disarm();return;}
   if(headPilot.Model.Arm()){
    if(campaignStarted&&!GroundActive&&!RoverDeployed){cruising=landingAssist=cruiseBraking=false;throttle=0;desiredRotation=shipRotation;CloseVoyager();}
    else Toast("Face flight armed for manual flight. Ground controls stay on keyboard/controller.");
   }else Toast(headPilot.Model.Message);return;
  }
  if(row==4)s.sensitivity=Mathf.Clamp(s.sensitivity+direction*.1f,.5f,2);
  if(row==5)s.deadzone=Mathf.Clamp(s.deadzone+direction*.5f,1,7);
  if(row==6)s.smoothing=Mathf.Clamp(s.smoothing+direction*.02f,.04f,.3f);
  if(row==7){s.mouthControls=!s.mouthControls;headPilot.Model.BeginCalibration();}
  if(row==8)s.invertYaw=!s.invertYaw;
  if(row==9)s.invertPitch=!s.invertPitch;
  if(row==10)s.invertRoll=!s.invertRoll;
  if(row==11)s.preview=!s.preview;
  if(row==12)s.mirror=!s.mirror;
  headPilot.Model.Suspend();headPilot.SaveOptions();
 }
 string HeadPilotLabel(int row){
  var s=headPilot.Settings;
  return new[]{headPilot.Requested?"STOP CAMERA":"START CAMERA",headPilot.Testing?"CAMERA / AUTOMATED TEST":headPilot.Cameras.Length==0?"CAMERA / Refresh devices":"CAMERA / "+headPilot.Cameras[Mathf.Clamp(s.camera,0,headPilot.Cameras.Length-1)],"CALIBRATE / NEUTRAL + EXPRESSIONS",headPilot.Model.Armed?"DISABLE FACE STEERING":"ARM / SWITCH TO MANUAL FLIGHT", "Sensitivity / "+s.sensitivity.ToString("F1"),"Dead zone / "+s.deadzone.ToString("F1")+"°","Smoothing / "+s.smoothing.ToString("F2")+" s","Mouth throttle + smile brake / "+OnOff(s.mouthControls),"Invert turn / "+OnOff(s.invertYaw),"Invert pitch / "+OnOff(s.invertPitch),"Invert bank / "+OnOff(s.invertRoll),"Live preview / "+OnOff(s.preview),"Mirror preview / "+OnOff(s.mirror)}[row];
 }
 void DrawHeadPilotSetup(){
  VText(new Rect(80,177,1440,54),"FACE FLIGHT / YOU ARE THE PILOT",VoyagerFont.Heading);
  VText(new Rect(84,237,705,45),"Turn, nod and tilt to steer. Open mouth to add throttle; smile to brake.",VoyagerFont.Small,vCyan);
  for(int i=0;i<HeadPilotRows;i++)if(VButton(new Rect(80,300+i*46,710,40),HeadPilotLabel(i),voyagerSelection==i)){voyagerSelection=i;HeadPilotAction(i);}
  VBox(new Rect(835,260,685,410),new Color(.018f,.04f,.06f));
  if(headPilot.Settings.preview&&headPilot.Preview!=null){
   Rect picture=FitHeadPreview(new Rect(850,274,655,380),headPilot.Preview.width,headPilot.Preview.height);GUI.DrawTextureWithTexCoords(picture,headPilot.Preview,new Rect(headPilot.Settings.mirror?1:0,1,headPilot.Settings.mirror?-1:1,-1),false);
   if(headPilot.Frame.faces==1){var f=headPilot.Frame;Rect box=new Rect(picture.x+(headPilot.Settings.mirror?1-f.x-f.w:f.x)*picture.width,picture.y+(1-f.y-f.h)*picture.height,f.w*picture.width,f.h*picture.height);HeadPreviewBorder(box,headPilot.Model.Valid?vCyan:vGold);}
  }else VText(new Rect(887,392,570,150),headPilot.Testing?"AUTOMATED TEST / NO LIVE CAMERA":headPilot.Settings.preview?"Your live camera appears here after you start it.":"Preview hidden. Tracking can still run.",VoyagerFont.Body,vMuted);
  VText(new Rect(850,678,655,50),headPilot.Status,VoyagerFont.Small,headPilot.Frame.status<0?vGold:vMuted);
  VText(new Rect(850,736,655,62),headPilot.Model.Message,VoyagerFont.Body,vCyan);
  VText(new Rect(850,809,310,27),"MOUTH / THROTTLE "+Mathf.RoundToInt(headPilot.Model.MouthMeter*100)+"%",VoyagerFont.Small);
  VText(new Rect(1190,809,315,27),"SMILE / BRAKE "+Mathf.RoundToInt(headPilot.Model.SmileMeter*100)+"%",VoyagerFont.Small);
  VBar(new Rect(850,844,310,6),headPilot.Model.MouthMeter,vCyan);VBar(new Rect(1190,844,315,6),headPilot.Model.SmileMeter,vGold);
  if(headPilot.Model.CalibrationStep>0)VBar(new Rect(850,878,655,7),headPilot.Model.CalibrationProgress,vCyan);
  VText(new Rect(850,895,655,30),headPilot.Model.Armed?"ARMED / "+headPilot.Model.Gesture:headPilot.Model.Calibrated?"CALIBRATED / READY TO ARM":"CAMERA STARTS ONLY WHEN YOU ENABLE IT",VoyagerFont.Small,vGold);
  VText(new Rect(82,902,710,28),"F6 camera off  •  F7 setup  •  F8 recalibrate  •  Frames stay on this Mac.",VoyagerFont.Small);
 }
 static Rect FitHeadPreview(Rect bounds,int width,int height){float scale=Mathf.Min(bounds.width/width,bounds.height/height),w=width*scale,h=height*scale;return new Rect(bounds.center.x-w*.5f,bounds.center.y-h*.5f,w,h);}
 void HeadPreviewBorder(Rect r,Color color){VBox(new Rect(r.x,r.y,r.width,2),color);VBox(new Rect(r.x,r.yMax-2,r.width,2),color);VBox(new Rect(r.x,r.y,2,r.height),color);VBox(new Rect(r.xMax-2,r.y,2,r.height),color);}
 public void DrawHeadPilotOverlay(){
  if(headPilot==null||VoyagerMenuOpen||HelmetPhotoUI.BlocksControls||!headPilot.Requested||qa||automatedPlayback)return;
  Matrix4x4 old=GUI.matrix;int depth=GUI.depth;GUI.depth=-5;VStyles();VMatrix();
  Rect card=new Rect(1190,670,370,240);VBox(card,new Color(.006f,.02f,.035f,.94f));
  VText(new Rect(1203,679,342,31),headPilot.Model.Armed?"FACE FLIGHT / "+(headPilot.Model.NeedsNeutral?"CENTER HEAD":"ACTIVE"):"CAMERA ON / MANUAL STEERING",VoyagerFont.Small,vCyan);
  if(headPilot.Settings.preview&&headPilot.Preview!=null)GUI.DrawTextureWithTexCoords(new Rect(1204,716,152,114),headPilot.Preview,new Rect(headPilot.Settings.mirror?1:0,1,headPilot.Settings.mirror?-1:1,-1),false);
  VText(new Rect(1368,718,182,108),headPilot.Model.Gesture+"\nTHROTTLE "+Mathf.RoundToInt(throttle*100)+"%",VoyagerFont.Small);
  VText(new Rect(1204,846,341,50),"F6 camera off / manual\nF7 setup · F8 recalibrate",VoyagerFont.Small,vGold);GUI.depth=depth;GUI.matrix=old;
 }
}
