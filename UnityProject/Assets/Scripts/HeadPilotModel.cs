using System;
using System.Runtime.InteropServices;
using UnityEngine;

[Serializable]
public sealed class HeadPilotSettings {
 public int version=1,camera;
 public float sensitivity=1,deadzone=2.5f,smoothing=.12f;
 public bool mouthControls=true,mirror=true,preview=true,invertYaw,invertPitch,invertRoll;
 public bool Validate(){return version==1&&camera>=0&&camera<32&&VoyagerState.Finite(sensitivity)&&sensitivity>=.5f&&sensitivity<=2&&VoyagerState.Finite(deadzone)&&deadzone>=1&&deadzone<=7&&VoyagerState.Finite(smoothing)&&smoothing>=.04f&&smoothing<=.3f;}
}

[StructLayout(LayoutKind.Sequential)]
public struct HeadPilotFrame {
 public int status,faces,width,height,sequence,valid;
 public float yaw,pitch,roll,confidence,mouthOpen,mouthWidth,x,y,w,h,age;
 public bool Usable {get{return status==2&&faces==1&&valid==1&&width>0&&height>0&&confidence>=.65f&&confidence<=1&&h>=.075f&&h<=1&&w>=.04f&&w<=1&&x>=0&&y>=0&&x+w<=1.01f&&y+h<=1.01f&&age>=0&&age<.3f&&Finite(yaw)&&Finite(pitch)&&Finite(roll)&&Mathf.Abs(yaw)<75&&Mathf.Abs(pitch)<65&&Mathf.Abs(roll)<70&&Finite(mouthOpen)&&mouthOpen>=0&&Finite(mouthWidth)&&mouthWidth>=0&&mouthWidth<=3;}}
 static bool Finite(float n){return !float.IsNaN(n)&&!float.IsInfinity(n);}
 public Vector3 Angles {get{return new Vector3(yaw,pitch,roll);}}
}

// Session-only calibration and intent. No camera permission, images, files or network access.
public sealed class HeadPilotModel {
 public readonly HeadPilotSettings Settings;
 public Vector3 Axes {get;private set;}
 public bool Armed {get;private set;}
 public bool Calibrated {get;private set;}
 public bool Valid {get;private set;}
 public bool Brake {get;private set;}
 public float ThrottleRate {get;private set;}
 public int CalibrationStep {get;private set;}
 public float CalibrationProgress {get;private set;}
 public string Message {get;private set;}="Start the camera, then calibrate while looking forward.";
 public string Gesture {get;private set;}="NEUTRAL";
 public float MouthMeter {get;private set;}
 public float SmileMeter {get;private set;}
 public bool NeedsNeutral {get;private set;}=true;
 Vector3 neutral,sum,minimum,maximum;
 float neutralOpen,neutralWidth,openReference,smileReference,openSum,widthSum,clock,windowStart,neutralTime,lostTime,gestureTime;
 int samples,lastSequence=-1,gestureCandidate;
 bool safetyPause;
 public HeadPilotModel(HeadPilotSettings settings){Settings=settings;}
 public bool ConsumeSafetyPause(){bool value=safetyPause;safetyPause=false;return value;}
 void Zero(){Axes=Vector3.zero;Brake=false;ThrottleRate=0;Gesture="NEUTRAL";gestureCandidate=0;gestureTime=0;}
 void ClearWindow(){samples=0;sum=Vector3.zero;openSum=widthSum=0;windowStart=clock;CalibrationProgress=0;}
 public void Reset(){Disarm();Calibrated=false;CalibrationStep=0;Valid=false;safetyPause=false;lastSequence=-1;MouthMeter=SmileMeter=0;neutral=Vector3.zero;neutralOpen=neutralWidth=openReference=smileReference=0;ClearWindow();Message="Camera off. Keyboard and controller remain available.";}
 public void BeginCalibration(){Disarm();Calibrated=false;safetyPause=false;lastSequence=-1;MouthMeter=SmileMeter=0;CalibrationStep=1;ClearWindow();Message="1 / Look forward with a relaxed face. Hold still.";}
 public void Suspend(){Zero();NeedsNeutral=true;neutralTime=0;lostTime=0;}
 public void Disarm(){Armed=false;Suspend();}
 public bool Arm(){
  if(!Calibrated||!Valid){Message="Finish calibration with one clearly visible face before arming.";return false;}
  Armed=true;Suspend();Message="Look forward and relax your mouth to engage steering.";return true;
 }
 public void ManualTakeover(){Disarm();Message="Manual steering took over. Re-arm face flight in F7.";}
 float Axis(float value,float range){float magnitude=Mathf.Abs(value);return magnitude<=Settings.deadzone?0:Mathf.Sign(value)*Mathf.Clamp01((magnitude-Settings.deadzone)/(range/Settings.sensitivity-Settings.deadzone));}
 Vector3 ReadAxes(HeadPilotFrame f){return new Vector3(Axis(Mathf.DeltaAngle(neutral.x,f.yaw),24)*(Settings.invertYaw?-1:1),Axis(Mathf.DeltaAngle(neutral.y,f.pitch),20)*(Settings.invertPitch?-1:1),Axis(Mathf.DeltaAngle(neutral.z,f.roll),25)*(Settings.invertRoll?-1:1));}
 void Calibrate(HeadPilotFrame f){
  if(!Valid||Mathf.Abs(f.yaw)>35||Mathf.Abs(f.pitch)>30||Mathf.Abs(f.roll)>30){ClearWindow();return;}
  if(CalibrationStep==2&&f.mouthOpen<neutralOpen+.10f){ClearWindow();return;}
  if(CalibrationStep==3&&(f.mouthWidth<neutralWidth+.035f||f.mouthOpen>neutralOpen+(openReference-neutralOpen)*.6f)){ClearWindow();return;}
  if(samples==0){minimum=maximum=f.Angles;windowStart=clock;}
  minimum=Vector3.Min(minimum,f.Angles);maximum=Vector3.Max(maximum,f.Angles);
  if((maximum-minimum).magnitude>5){ClearWindow();minimum=maximum=f.Angles;}
  sum+=f.Angles;openSum+=f.mouthOpen;widthSum+=f.mouthWidth;samples++;
  float duration=CalibrationStep==1?1.25f:.9f;CalibrationProgress=Mathf.Clamp01((clock-windowStart)/duration);
  if(clock-windowStart<duration||samples<10)return;
  if(CalibrationStep==1){neutral=sum/samples;neutralOpen=openSum/samples;neutralWidth=widthSum/samples;if(!Settings.mouthControls){Calibrated=true;CalibrationStep=0;Message="Calibrated. Look forward, then arm steering.";return;}CalibrationStep=2;Message="2 / Open your mouth wide. Hold it to sample throttle.";}
  else if(CalibrationStep==2){openReference=openSum/samples;CalibrationStep=3;Message="3 / Smile broadly with lips together. Hold to sample braking.";}
  else {smileReference=widthSum/samples;Calibrated=true;CalibrationStep=0;Message="Calibrated. Relax your face, look forward, then arm.";}
  ClearWindow();
 }
 public void Tick(HeadPilotFrame frame,float dt,bool flying){
  dt=Mathf.Clamp(dt,0,.1f);clock+=dt;Valid=frame.Usable&&(!Settings.mouthControls||frame.mouthWidth>=.05f);
  if(CalibrationStep>0){if(!Valid)ClearWindow();else if(lastSequence!=frame.sequence)Calibrate(frame);}
  lastSequence=frame.sequence;
  MouthMeter=Calibrated&&Settings.mouthControls?Mathf.Clamp01((frame.mouthOpen-neutralOpen)/Mathf.Max(.1f,openReference-neutralOpen)):0;
  SmileMeter=Calibrated&&Settings.mouthControls?Mathf.Clamp01((frame.mouthWidth-neutralWidth)/Mathf.Max(.035f,smileReference-neutralWidth)):0;
  if(!Armed){Zero();return;}
  if(!flying){Suspend();return;}
  if(!Valid){
   Zero();lostTime+=dt;
   if(frame.faces>1||frame.status<0||lostTime>=.35f){Disarm();safetyPause=true;Message="Tracking lost. Flight paused; look forward and re-arm in F7.";}
   return;
  }
  lostTime=0;Vector3 raw=ReadAxes(frame);
  int intent=!Settings.mouthControls?0:SmileMeter>.65f&&MouthMeter<.6f?2:MouthMeter>.65f?1:0;
  if(NeedsNeutral){Zero();neutralTime=raw.magnitude<.22f&&intent==0?neutralTime+dt:0;if(neutralTime<.35f)return;NeedsNeutral=false;Message="Face flight active. F6 stops the camera; F7 opens setup.";}
  Axes=Vector3.Lerp(Axes,raw,1-Mathf.Exp(-dt/Settings.smoothing));
  if(intent!=gestureCandidate){gestureCandidate=intent;gestureTime=0;}else gestureTime+=dt;
  Brake=false;ThrottleRate=0;Gesture="NEUTRAL";
  if(gestureTime>=.22f&&intent==1){ThrottleRate=.5f;Gesture="MOUTH OPEN / THROTTLE UP";}
  if(gestureTime>=.22f&&intent==2){Brake=true;Gesture="SMILE / BRAKE";}
 }
}
