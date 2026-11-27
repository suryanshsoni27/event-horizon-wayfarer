using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class OrbitDemo : MonoBehaviour {
 [Serializable,StructLayout(LayoutKind.Sequential)] public struct State { public double x,y,z,vx,vy,vz; public int captured; }
 [DllImport("blackhole",CallingConvention=CallingConvention.Cdecl)] static extern void bh_step(ref State s,double ax,double ay,double az,double dt);
 [Serializable] public class Model { public int dimensions,trainingTransitions; public float heldoutRmse;public float[] radialCenters,radialSigmas,wx,wy,wz; }
 public State state;DefenseGame game;Model model;Transform astronaut,flame;LineRenderer trail,forecast,stem;List<Vector3> history=new List<Vector3>();
 Material glow,solid,rayMaterial;int quality=1;Camera cam;float accumulator,elapsed,distance=31,yaw=32,pitch=24;bool paused,capture,recording,planner;float planCountdown;Vector3 plannedAction;Vector3 action,lastMouse;string output;float forecastError;int frames;float minZ=0,maxZ=0;
 GUIStyle label,small,title,number;
 public Vector3 Position{get{return new Vector3((float)state.x,(float)state.y,(float)state.z);}}
 public Vector3 Velocity{get{return new Vector3((float)state.vx,(float)state.vy,(float)state.vz);}}
 void Start(){
  Application.targetFrameRate=60;Screen.SetResolution(Mathf.Min(1280,Display.main.systemWidth-80),Mathf.Min(800,Display.main.systemHeight-100),false);
  capture=Array.IndexOf(Environment.GetCommandLineArgs(),"--capture")>=0;output=Environment.GetEnvironmentVariable("BH_EVIDENCE")??Application.persistentDataPath;
  model=JsonUtility.FromJson<Model>(Resources.Load<TextAsset>("predictor").text);if(model.dimensions!=3)throw new Exception("A trained 3D predictor is required.");
  glow=new Material(Shader.Find("Orbit/Glow"));solid=new Material(Shader.Find("Universal Render Pipeline/Lit"));
  cam=Camera.main;cam.clearFlags=CameraClearFlags.SolidColor;cam.orthographic=false;cam.fieldOfView=53;cam.nearClipPlane=.1f;cam.farClipPlane=200;cam.backgroundColor=new Color(.018f,.028f,.052f);UpdateCamera();
  RenderSettings.ambientLight=new Color(.3f,.37f,.5f);
  var light=new GameObject("Warm key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(25,-30,0);
  var rim=new GameObject("Cool rim").AddComponent<Light>();rim.type=LightType.Directional;rim.intensity=1.1f;rim.color=new Color(.3f,.6f,1);rim.transform.rotation=Quaternion.Euler(-25,140,0);
  BuildSpace();BuildRayRenderer();BuildAstronaut();flame.SetParent(null,true);flame.localScale=new Vector3(.18f,.8f,.18f);flame.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.15f,.7f,1,.8f),true);
  trail=Line("Actual xyz trajectory",new Color(.1f,.65f,1,.7f),.035f);forecast=Line("Learned xyz prediction",new Color(.35f,1,.65f,.85f),.048f);
  stem=Line("Depth projection to navigation deck",new Color(.4f,.7f,.9f,.3f),.018f);stem.positionCount=2;
  gameObject.AddComponent<HelmetPhotoUI>().Initialize(astronaut);ResetOrbit();game=gameObject.AddComponent<DefenseGame>();game.Initialize(this,cam,rayMaterial);if(capture&&game==null)StartCoroutine(Capture());
 }
 Material Mat(Color c,bool additive=false){var m=new Material(additive?glow:solid);m.SetColor(additive?"_Color":"_BaseColor",c);if(!additive){m.SetFloat("_Smoothness",0);m.SetFloat("_SpecularHighlights",0);m.SetFloat("_EnvironmentReflections",0);}return m;}
 Transform Sphere(string name,Vector3 pos,Vector3 scale,Color c,Transform parent=null){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=Mat(c);Destroy(g.GetComponent<Collider>());return g.transform;}
 LineRenderer Line(string name,Color color,float width){var g=new GameObject(name);var l=g.AddComponent<LineRenderer>();l.material=Mat(color,true);l.widthMultiplier=width;l.numCapVertices=3;return l;}
 void Ring(string name,float radius,float width,Color c,float tilt=0,int n=240){var l=Line(name,c,width);l.loop=true;l.positionCount=n;var p=new Vector3[n];var q=Quaternion.Euler(tilt,0,0);for(int i=0;i<n;i++){float a=i*2*Mathf.PI/n;p[i]=q*new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);}l.SetPositions(p);}
 void BuildAstronaut(){
  astronaut=new GameObject("Astronaut • placeholder").transform;astronaut.localScale=Vector3.one*1.3f;
  Sphere("Backpack",new Vector3(0,-.05f,.15f),new Vector3(.57f,.82f,.36f),new Color(.2f,.26f,.33f),astronaut);
  Sphere("Pressure suit",Vector3.zero,new Vector3(.55f,.72f,.38f),new Color(.84f,.87f,.87f),astronaut);
  Sphere("Helmet",new Vector3(0,.56f,0),new Vector3(.61f,.61f,.53f),new Color(.92f,.95f,.94f),astronaut);
  Sphere("Gold visor",new Vector3(0,.57f,-.23f),new Vector3(.48f,.37f,.14f),new Color(.26f,.16f,.045f),astronaut);
  Sphere("Visor gleam",new Vector3(-.12f,.65f,-.31f),new Vector3(.09f,.12f,.025f),new Color(.7f,.84f,.94f),astronaut);
  for(int side=-1;side<=1;side+=2){var arm=Sphere("Glove and arm",new Vector3(side*.42f,.05f,0),new Vector3(.22f,.64f,.24f),new Color(.78f,.83f,.87f),astronaut);arm.localRotation=Quaternion.Euler(0,0,side*22);Sphere("Boot",new Vector3(side*.18f,-.6f,0),new Vector3(.24f,.58f,.29f),new Color(.65f,.72f,.77f),astronaut);}
  Sphere("Mission patch",new Vector3(0,.12f,-.21f),new Vector3(.18f,.19f,.03f),new Color(.95f,.3f,.08f),astronaut);
  flame=Sphere("Thruster plume",new Vector3(0,-1,0),new Vector3(.14f,.6f,.14f),new Color(.1f,.7f,1),astronaut);flame.gameObject.SetActive(false);
 }
 void BuildSpace(){
  UnityEngine.Random.InitState(12);
  for(int i=0;i<0;i++){Vector3 p=UnityEngine.Random.onUnitSphere*UnityEngine.Random.Range(55,85);var l=Line("Spatial star",new Color(.5f,.65f,.9f,UnityEngine.Random.Range(.3f,.95f)),UnityEngine.Random.Range(.025f,.075f));l.positionCount=2;l.SetPositions(new[]{p,p+Vector3.up*.03f});}
  // A horizontal navigation deck and orthogonal great circles make camera parallax visible.
  for(int i=-16;i<=16;i+=2){var l=Line("XZ navigation deck",new Color(.14f,.3f,.46f,.27f),.015f);l.positionCount=2;l.SetPositions(new[]{new Vector3(i,-6,-16),new Vector3(i,-6,16)});l=Line("XZ navigation deck",new Color(.14f,.3f,.46f,.27f),.015f);l.positionCount=2;l.SetPositions(new[]{new Vector3(-16,-6,i),new Vector3(16,-6,i)});}
  Ring("XY navigation meridian",12,.014f,new Color(.3f,.45f,.6f,.25f));Ring("XZ navigation meridian",12,.014f,new Color(.3f,.45f,.6f,.25f),90);
  var yz=Line("YZ navigation meridian",new Color(.3f,.45f,.6f,.25f),.014f);yz.loop=true;yz.positionCount=240;var points=new Vector3[240];for(int i=0;i<240;i++){float a=i*Mathf.PI*2/240;points[i]=new Vector3(0,12*Mathf.Cos(a),12*Mathf.Sin(a));}yz.SetPositions(points);
  var axes=new[]{Vector3.right,Vector3.up,Vector3.forward};var colors=new[]{new Color(1,.3f,.2f,.55f),new Color(.35f,1,.55f,.55f),new Color(.3f,.55f,1,.55f)};
  for(int i=0;i<3;i++){var l=Line("World axis "+i,colors[i],.026f);l.positionCount=2;l.SetPositions(new[]{axes[i]*2,axes[i]*11});Sphere("Axis endpoint",axes[i]*11,Vector3.one*.12f,colors[i]);}

 }

 void BuildRayRenderer(){
  var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Numerical Schwarzschild light rays";Destroy(quad.GetComponent<Collider>());quad.transform.SetParent(cam.transform,false);quad.transform.localPosition=Vector3.forward;float height=2*Mathf.Tan(cam.fieldOfView*Mathf.Deg2Rad*.5f);quad.transform.localScale=new Vector3(height*cam.aspect,height,1);
  rayMaterial=new Material(Shader.Find("Orbit/Schwarzschild"));quad.GetComponent<Renderer>().sharedMaterial=rayMaterial;quad.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;quad.GetComponent<Renderer>().receiveShadows=false;ApplyQuality();
 }
 void ApplyQuality(){var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;if(pipeline!=null)pipeline.renderScale=quality==0?.6f:quality==1?.85f:1;
  if(rayMaterial!=null){rayMaterial.SetFloat("_Steps",quality==0?150:quality==1?220:320);rayMaterial.SetFloat("_StepScale",quality==0?.065f:quality==1?.045f:.03f);}
 }
 public void ClearTrail(){history.Clear();}
 void ResetOrbit(){double speed=Math.Sqrt(60.0/9),a=55*Math.PI/180;state=new State{x=9,y=0,z=0,vx=0,vy=speed*Math.Cos(a),vz=speed*Math.Sin(a),captured=0};history.Clear();accumulator=0;elapsed=0;minZ=maxZ=0;}
 public Vector3 PredictAcceleration(Vector3 p,Vector3 u){
  int n=model.radialCenters.Length,k=3*n;float x=model.wx[k]*u.x+model.wx[k+1]*u.y+model.wx[k+2]*u.z+model.wx[k+3],y=model.wy[k]*u.x+model.wy[k+1]*u.y+model.wy[k+2]*u.z+model.wy[k+3],z=model.wz[k]*u.x+model.wz[k+1]*u.y+model.wz[k+2]*u.z+model.wz[k+3];float r=p.magnitude;
  for(int i=0;i<n;i++){float d=r-model.radialCenters[i];if(Mathf.Abs(d)>5*model.radialSigmas[i])continue;float f=Mathf.Exp(-d*d/(2*model.radialSigmas[i]*model.radialSigmas[i]));x+=f*(model.wx[i]*p.x+model.wx[n+i]*p.y+model.wx[2*n+i]*p.z);y+=f*(model.wy[i]*p.x+model.wy[n+i]*p.y+model.wy[2*n+i]*p.z);z+=f*(model.wz[i]*p.x+model.wz[n+i]*p.y+model.wz[2*n+i]*p.z);}
  return new Vector3(x,y,z);
 }
 // Receding-horizon controller: every candidate is simulated using learned acceleration only.
 Vector3 Plan(Vector3 initialPosition,Vector3 initialVelocity){
  float best=float.PositiveInfinity;Vector3 bestAction=Vector3.zero;
  for(int ix=-1;ix<=1;ix++)for(int iy=-1;iy<=1;iy++)for(int iz=-1;iz<=1;iz++){
   Vector3 u=new Vector3(ix,iy,iz)*.5f,p=initialPosition,v=initialVelocity;float cost=.4f*u.sqrMagnitude;
   for(int k=0;k<80;k++){v+=PredictAcceleration(p,u)*.03f;p+=v*.03f;float r=p.magnitude;if(r<2.4f||r>16){cost+=100000;break;}float radial=Vector3.Dot(p,v)/r;cost+=(.8f*(r-9)*(r-9)+3*radial*radial+.5f*Mathf.Pow(v.magnitude-Mathf.Sqrt(60f/9),2))*.03f;}
   cost+=5*Mathf.Pow(p.magnitude-9,2);if(cost<best){best=cost;bestAction=u;}
  }
  return bestAction;
 }
 void UpdateCamera(){cam.transform.position=Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-distance);cam.transform.LookAt(Vector3.zero,Vector3.up);}
 void Update(){
  frames++;if(HelmetPhotoUI.BlocksControls){action=Vector3.zero;accumulator=0;if(game!=null)game.HoldForUI();return;}if(game!=null){game.Tick(Time.deltaTime);RenderAgent();return;}if(Input.GetKeyDown(KeyCode.P)){planner=!planner;planCountdown=0;}if(Input.GetKeyDown(KeyCode.F)){quality=(quality+1)%3;ApplyQuality();}
  if(Input.GetKeyDown(KeyCode.R))ResetOrbit();if(Input.GetKeyDown(KeyCode.Space))paused=!paused;
  if(Input.GetKeyDown(KeyCode.V)){yaw=32;pitch=24;distance=31;}
  Vector3 mouse=Input.mousePosition;if(!HelmetPhotoUI.PointerOverUI&&Input.GetMouseButton(0)&&!Input.GetMouseButtonDown(0)){Vector3 delta=mouse-lastMouse;yaw+=delta.x*.3f;pitch=Mathf.Clamp(pitch-delta.y*.3f,-75,75);}lastMouse=mouse;
  if(!HelmetPhotoUI.PointerOverUI)distance=Mathf.Clamp(distance-Input.mouseScrollDelta.y*1.4f,14,65);
  if(recording){yaw+=Time.deltaTime*19;pitch=12+Mathf.Sin(elapsed*.5f)*22;}UpdateCamera();
  action=new Vector3((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0),(Input.GetKey(KeyCode.E)?1:0)-(Input.GetKey(KeyCode.Q)?1:0));
  if(planner&&action==Vector3.zero&&state.captured==0&&!paused){planCountdown-=Time.deltaTime;if(planCountdown<=0){plannedAction=Plan(Position,Velocity);planCountdown=.3f;}action=plannedAction;}
  if(!paused&&state.captured==0){accumulator+=Mathf.Min(Time.deltaTime,.1f);while(accumulator>=.01f){bh_step(ref state,action.x,action.y,action.z,.01);accumulator-=.01f;elapsed+=.01f;}history.Add(Position);if(history.Count>2200)history.RemoveAt(0);}
  RenderAgent();
 }
 void RenderAgent(){
  if(game!=null&&game.SurfaceVehicle){astronaut.gameObject.SetActive(false);flame.gameObject.SetActive(false);forecast.enabled=trail.enabled=stem.enabled=false;return;}
  if(game!=null){forecast.enabled=game.ShowTelemetry;trail.enabled=game.ShowTelemetry;action=game.AgentAction;history.Add(Position);if(history.Count>160)history.RemoveAt(0);stem.enabled=false;}
  minZ=Mathf.Min(minZ,(float)state.z);maxZ=Mathf.Max(maxZ,(float)state.z);
  astronaut.position=Position;var v=Velocity;if(v.sqrMagnitude>.001f)astronaut.rotation=Quaternion.Slerp(astronaut.rotation,Quaternion.LookRotation(-v.normalized,Position.normalized),Time.deltaTime*3);
  flame.gameObject.SetActive(action.sqrMagnitude>.01f&&state.captured==0);if(action.sqrMagnitude>.01f){flame.position=Position-action.normalized*1.15f;flame.rotation=Quaternion.FromToRotation(Vector3.up,action.normalized);}astronaut.gameObject.SetActive(state.captured==0);
  stem.SetPositions(new[]{Position,new Vector3(Position.x,-6,Position.z)});trail.positionCount=history.Count;trail.SetPositions(history.ToArray());
  if(game!=null&&!game.ShowTelemetry)return;
  // Roll out only the learned 3D model; current action is held constant for this forecast.
  Vector3 p=Position,vel=Velocity;var points=new List<Vector3>();
  for(int i=0;i<200&&state.captured==0;i++){if(game!=null)game.AdvancePrediction(ref p,ref vel,action,.03f);else{vel+=PredictAcceleration(p,action)*.03f;p+=vel*.03f;}points.Add(p);if(game==null?(p.sqrMagnitude<3.24f||p.sqrMagnitude>90000):game.NearHazard(p)<1.8f)break;}
  forecast.positionCount=points.Count;forecast.SetPositions(points.ToArray());
  var truth=state;if(game!=null)DefenseGame.Step(ref truth,game.AgentAction,.01f);else bh_step(ref truth,action.x,action.y,action.z,.01);forecastError=Vector3.Distance(new Vector3((float)truth.vx,(float)truth.vy,(float)truth.vz),Velocity+(game!=null?game.Predict(Position,game.AgentAction):PredictAcceleration(Position,action))*.01f);
 }
 void Styles(){if(label!=null)return;label=new GUIStyle{fontSize=18,normal={textColor=new Color(.8f,.88f,.93f)}};small=new GUIStyle(label){fontSize=14};title=new GUIStyle(label){fontSize=38,fontStyle=FontStyle.Bold};number=new GUIStyle(label){fontSize=25};}
 void Text(float x,float y,string s,GUIStyle style=null){GUI.Label(new Rect(x,y,740,60),s,style??label);}
 void Panel(float x,float y,float w,float h){GUI.color=new Color(.025f,.05f,.075f,.94f);GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Color.white;}
 string XYZ(Vector3 p){return p.x.ToString("F2")+"  /  "+p.y.ToString("F2")+"  /  "+p.z.ToString("F2");}
 void AxisLabel(Vector3 p,string text){var q=cam.WorldToScreenPoint(p);if(q.z>0){GUI.Label(new Rect(q.x/Screen.width*1600,1000-q.y/Screen.height*1000,140,30),text,small);}}
 void OnGUI(){if(game!=null){game.DrawGUI();return;}Styles();GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1600f,Screen.height/1000f,1));
  Text(48,37,"E V E N T   /   H O R I Z O N",small);Text(45,66,"Orbit beyond the plane.",title);Text(48,119,"3D FIELD LAB    /    INCLINED LAUNCH 55°",small);
  Text(1170,42,"LIGHT: NUMERICAL SCHWARZSCHILD / Rs = 1",small);Text(1170,70,"F   RAY QUALITY: "+new[]{"LOW","MEDIUM","HIGH"}[quality],small);
  AxisLabel(Vector3.right*11,"+X");AxisLabel(Vector3.up*11,"+Y");AxisLabel(Vector3.forward*11,"+Z  DEPTH");
  Panel(48,725,400,235);Text(70,744,state.captured==1?"SIGNAL LOST":paused?"SIMULATION PAUSED":"3D ORBIT ACTIVE",number);
  Text(70,784,"POSITION XYZ   /   "+XYZ(Position),small);Text(70,814,"VELOCITY XYZ   /   "+XYZ(Velocity),small);Text(70,844,"DEPTH Z    "+state.z.ToString("F2")+" u",label);Text(70,876,"RADIUS  "+Position.magnitude.ToString("F2")+" u     SPEED  "+Velocity.magnitude.ToString("F2")+" u/s",small);Text(70,906,"ELAPSED  "+elapsed.ToString("F1")+" s    •    C++ dt 0.01 s",small);Text(70,935,"3 AXES OF TRANSLATION / UNLIMITED THRUST",small);
  Panel(1170,725,382,235);Text(1192,744,"LEARNED 3D WORLD MODEL",small);Text(1192,775,model.trainingTransitions.ToString("N0")+" 3D transitions",number);Text(1192,815,"OFFLINE TRAINED • radial-feature regressor",small);Text(1192,847,"Holdout accel RMSE: "+model.heldoutRmse.ToString("F5"),small);Text(1192,877,"Live 3D Δv error: "+forecastError.ToString("F6"),small);Text(1192,908,"MINT: LEARNED 6 s / BLUE: ACTUAL",small);Text(1192,936,Position.magnitude<2.4f||Position.magnitude>16?"OUTSIDE MODEL TRAINING DOMAIN":"VALIDATED DOMAIN: RADIUS 2.4–16",small);
  Text(495,760,planner?"MODEL PLANNER / HOLD RADIUS 9":"MANUAL THRUST / 3 AXES",label);Text(495,793,"P   TOGGLE PLANNER  •  27 ACTIONS / 2.4 s",small);Text(495,820,"THRUST XYZ: "+XYZ(action),small);Text(495,850,"W / S   ±Y     A / D   ±X     Q / E   ±Z",label);Text(495,885,"DRAG   ORBIT CAMERA     SCROLL   ZOOM",small);Text(495,916,"V   RESET VIEW     R   RESET ORBIT     SPACE   PAUSE",small);
  Text(48,976,"NEWTONIAN ASTRONAUT • CAPTURE RADIUS 1.8 • SCHWARZSCHILD LIGHT • PROCEDURAL THIN DISK • NO SPIN / NO GRMHD",small);
  if(state.captured==1){Panel(550,420,500,110);Text(580,440,"Beyond the capture boundary.",number);Text(580,487,"Press R to launch a new inclined orbit.",small);}
 }
 IEnumerator Capture(){
  System.IO.Directory.CreateDirectory(output);
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"--record")>=0){
   string folder=System.IO.Path.Combine(output,"frames-3d");System.IO.Directory.CreateDirectory(folder);yield return new WaitForSeconds(1);recording=true;
   for(int i=0;i<96;i++){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,i.ToString("D3")+".png"));yield return new WaitForSecondsRealtime(1f/12);}
   recording=false;
  }
  yield return new WaitForSeconds(1);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(output,"unity-demo-3d.png"));yield return new WaitForSeconds(2);
  System.IO.File.WriteAllText(System.IO.Path.Combine(output,"runtime-capture-3d.json"),"{\"dimensions\":3,\"perspective\":"+(!cam.orthographic).ToString().ToLower()+",\"zMin\":"+minZ.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"zMax\":"+maxZ.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"frames\":"+frames+",\"cameraYaw\":"+yaw.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");Application.Quit();
 }
}
