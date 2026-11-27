using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class HeadPilotCamera : MonoBehaviour {
 [DllImport("facepilot",CallingConvention=CallingConvention.Cdecl)] public static extern int fp_frame_size();
 [DllImport("facepilot",CallingConvention=CallingConvention.Cdecl)] static extern int fp_camera_count();
 [DllImport("facepilot",CallingConvention=CallingConvention.Cdecl)] static extern int fp_camera_name(int index,StringBuilder name,int capacity);
 [DllImport("facepilot",CallingConvention=CallingConvention.Cdecl)] static extern int fp_start(int camera);
 [DllImport("facepilot",CallingConvention=CallingConvention.Cdecl)] static extern void fp_stop();
 [DllImport("facepilot",CallingConvention=CallingConvention.Cdecl)] static extern int fp_poll(out HeadPilotFrame frame);
 [DllImport("facepilot",CallingConvention=CallingConvention.Cdecl)] static extern int fp_copy_preview([Out] byte[] pixels,int capacity,out int width,out int height,out int sequence);
 [DllImport("facepilot",CallingConvention=CallingConvention.Cdecl)] public static extern int fp_analyse_bgra([In] byte[] pixels,int length,int width,int height,int stride,out HeadPilotFrame frame);
 [DllImport("facepilot",CallingConvention=CallingConvention.Cdecl)] public static extern int fp_mouth_metrics([In] float[] points,int floatCount,int innerCount,int outerCount,int leftCount,int rightCount,float rollDegrees,out float openness,out float width);
 public static HeadPilotFrame NativeSnapshot {get{HeadPilotFrame frame;fp_poll(out frame);return frame;}}
 public HeadPilotSettings Settings {get;private set;}
 public HeadPilotModel Model {get;private set;}
 public HeadPilotFrame Frame {get;private set;}
 public Texture2D Preview {get;private set;}
 public bool Supported {get;private set;}
 public bool Requested {get;private set;}
 public bool Testing {get;set;}
 public bool PreviewIncludesLiveFace {get{return Preview!=null&&!Testing;}}
 public string[] Cameras {get;private set;}=new string[0];
 public string Status {get;private set;}="Camera off.";
 DefenseGame owner;bool automated;string settingsPath;byte[] pixels;int previewSequence=-1;float requestedAt;

 public void Initialize(DefenseGame game,bool automatic,string path){
  owner=game;automated=automatic;settingsPath=path;
  Settings=(!automated?CampaignFiles.Read<HeadPilotSettings>(path,x=>x.Validate()):null)??new HeadPilotSettings();
  Model=new HeadPilotModel(Settings);
  if(Application.platform!=RuntimePlatform.OSXPlayer&&Application.platform!=RuntimePlatform.OSXEditor){Status="Face flight currently requires macOS 12 or newer.";return;}
  try{Supported=fp_frame_size()==Marshal.SizeOf(typeof(HeadPilotFrame));if(Supported)RefreshCameras();}
  catch(DllNotFoundException){Status="Build the macOS facepilot native library to use a camera.";}
  catch(EntryPointNotFoundException){Status="Rebuild the facepilot native library for this version.";}
 }
 public void RefreshCameras(){
  if(!Supported)return;
  int count=Mathf.Clamp(fp_camera_count(),0,32);var names=new string[count];
  for(int i=0;i<count;i++){var name=new StringBuilder(256);names[i]=fp_camera_name(i,name,256)==1?name.ToString():"Camera "+(i+1);}
  Cameras=names;if(count>0)Settings.camera=Mathf.Clamp(Settings.camera,0,count-1);
 }
 public bool StartCamera(){
  if(!Supported||automated||Testing){Status=automated?"Automated checks never request a camera.":"Camera requires the macOS player.";return false;}
  StopCamera();RefreshCameras();
  if(Cameras.Length==0){Status="No camera found. Connect a webcam or an available Continuity Camera, then retry.";return false;}
  Requested=true;requestedAt=Time.realtimeSinceStartup;Status="Waiting for camera permission / starting capture…";fp_start(Settings.camera);return true;
 }
 public void SaveOptions(){if(!Settings.Validate())return;try{CampaignFiles.Write(settingsPath,Settings,x=>x.Validate());}catch(Exception){Status="These options could not be saved; current session still works.";}}
 public void StopCamera(){
  if(Supported&&!Testing)fp_stop();Requested=false;Frame=default(HeadPilotFrame);if(Model!=null)Model.Reset();
  ClearPreview();Status="Camera off. Frames and calibration cleared.";
 }
 void ClearPreview(){if(pixels!=null)Array.Clear(pixels,0,pixels.Length);pixels=null;if(Preview!=null)Destroy(Preview);Preview=null;previewSequence=-1;}
 public void Inject(HeadPilotFrame frame,float dt,bool flying){if(!Testing)throw new InvalidOperationException("Pose injection is restricted to an explicit QA session.");Frame=frame;Model.Tick(frame,dt,flying);}
 void Update(){
  if(Testing||!Supported||!Requested)return;
  HeadPilotFrame frame;if(fp_poll(out frame)!=1)return;Frame=frame;
  Model.Tick(frame,Time.unscaledDeltaTime,owner!=null&&owner.HeadFlightEligible);
  if(frame.status==2&&((frame.sequence==0&&Time.realtimeSinceStartup-requestedAt>12)||frame.sequence!=0&&frame.age>10)){
   bool armed=Model.Armed||Model.ConsumeSafetyPause();StopCamera();Status="No video frames received. Check the camera, then start it again.";if(armed&&owner!=null)owner.HeadPilotEmergency(Status);return;
  }
  Status=frame.status==-1?"Camera access denied. Enable Event Horizon in macOS Privacy & Security → Camera, then retry.":frame.status==-2?"The selected camera is no longer available.":frame.status==-3?"Camera disconnected or unavailable. Stop and restart capture.":frame.status==-5?"This app needs a camera usage description; rebuild it.":frame.status==1?"Allow camera access in the macOS prompt.":frame.faces>1?"More than one face visible. Steering is suspended.":!frame.Usable?"Find one clear, well-lit face in the preview.":"Tracking one face locally.";
  if(frame.status<0){Requested=false;ClearPreview();return;}
  if(!Settings.preview||frame.status!=2||frame.sequence==previewSequence||frame.width<1||frame.height<1||frame.width>1920||frame.height>1080)return;
  int size=frame.width*frame.height*4;
  if(pixels==null||pixels.Length!=size){ClearPreview();pixels=new byte[size];Preview=new Texture2D(frame.width,frame.height,TextureFormat.BGRA32,false,false){name="Face flight / live memory-only preview",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};}
  int width,height,seq;if(fp_copy_preview(pixels,pixels.Length,out width,out height,out seq)!=size||width!=frame.width||height!=frame.height)return;
  Preview.LoadRawTextureData(pixels);Preview.Apply(false,false);previewSequence=seq;
 }
 void OnApplicationFocus(bool focused){if(!focused&&Requested&&Frame.status==2){bool armed=Model.Armed;StopCamera();if(armed&&owner!=null)owner.HeadPilotEmergency("Camera stopped while the game was in the background. Re-enable it in F7.");}}
 void OnApplicationQuit(){StopCamera();}
 void OnDestroy(){StopCamera();}
}
