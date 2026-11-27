using UnityEngine;

public sealed partial class DefenseGame {
 RoverView roverView;bool wheelsRequested,roverBrake;float wheelDeployment,roverDrive,roverSteer,roverCameraYaw,roverCameraPitch=16;
 public bool RoverDeployed{get{return !legacyUniverse&&(wheelsRequested||wheelDeployment>.001f);}}
 public bool RoverReady{get{return wheelsRequested&&wheelDeployment>=.999f;}}
 public bool SurfaceVehicle{get{return GroundActive||RoverDeployed;}}
 void BuildRover(){var root=new GameObject("SIX WHEEL SURFACE CONVERSION");root.transform.SetParent(rocket,false);roverView=root.AddComponent<RoverView>();roverView.Build();}
 void ResetRover(){wheelsRequested=false;wheelDeployment=roverDrive=roverSteer=roverCameraYaw=0;roverCameraPitch=16;roverBrake=true;if(roverView!=null)roverView.ResetView();if(rocket!=null){var canopy=rocket.Find("Canopy");if(canopy!=null)canopy.gameObject.SetActive(true);}}
 bool ToggleWheels(){
  if(legacyUniverse||expedition.OnFoot){Notice("Board the craft before operating its wheel system.");return false;}
  if(!wheelsRequested&&wheelDeployment>0){Notice("Wait for the tires to finish retracting.");return false;}
  if(Vel(player).magnitude>1.5f){Notice("Stop below 1.5 m/s before deploying or retracting tires.");return false;}
  if(!wheelsRequested&&expedition.Alt(Pos(player))>1.05f){Notice("Land and settle before deploying tires. K / D-pad up assists descent.");return false;}
  var body=expedition.Nearest(Pos(player));Vector3 up=(Pos(player)-body.body.P).normalized;
  if(Vector3.Angle(up,TerrainNormal(Pos(player)))>40){Notice("Find a gentler surface before deploying tires.");return false;}
  wheelsRequested=!wheelsRequested;landingAssist=cruising=cruiseBraking=false;throttle=0;playerAction=Vector3.zero;roverDrive=roverSteer=0;roverBrake=true;releaseFire=true;orbit=false;
  desiredRotation=shipRotation=Quaternion.LookRotation(PlanetExpedition.SurfaceTangent(up,ShipForward),up);
  if(!wheelsRequested){expedition.ResetCrew();Notice("Retracting tires / wait for flight-ready confirmation.");}else Notice("Deploying six tires and crew seats / engines locked for ground travel.");
  return true;
 }
 Vector3 TerrainNormal(Vector3 p){var body=expedition.Nearest(p);Vector3 up=(p-body.body.P).normalized,right=PlanetExpedition.SurfaceTangent(up,Vector3.right),forward=Vector3.Cross(right,up).normalized;
  System.Func<Vector3,Vector3> ground=q=>{Vector3 d=(q-body.body.P).normalized;return body.body.P+d*((float)body.body.radius+PlanetExpedition.Height(d,body.body.seed,body.body.radius));};
  Vector3 n=Vector3.Cross(ground(p+forward*1.5f)-ground(p-forward*1.5f),ground(p+right*1.5f)-ground(p-right*1.5f)).normalized;return Vector3.Dot(n,up)<0?-n:n;
 }
 void RoverInput(float dt){if(!RoverDeployed)return;throttle=0;playerAction=Vector3.zero;landingAssist=cruising=cruiseBraking=false;
  if(!automatedPlayback||Controls.TestDevice!=null){roverDrive=(GameKey(KeyCode.W)?1:0)-(GameKey(KeyCode.S)?1:0);roverSteer=(GameKey(KeyCode.D)?1:0)-(GameKey(KeyCode.A)?1:0);roverBrake=GameKey(KeyCode.Space);
   if(Controls.Connected){roverDrive=Mathf.Clamp(roverDrive+Controls.R2-Controls.L2+Controls.Move.y,-1,1);roverSteer=Mathf.Clamp(roverSteer+Controls.Move.x,-1,1);roverBrake|=PadHeld(PilotInput.Button.Cross);}
   if(!expedition.OnFoot&&!paused){roverCameraYaw+=(Controls.Look.x*80+((GameKey(KeyCode.RightArrow)?1:0)-(GameKey(KeyCode.LeftArrow)?1:0))*65)*dt+(!automatedPlayback?Input.GetAxisRaw("Mouse X")*1.5f:0);roverCameraPitch=Mathf.Clamp(roverCameraPitch-Controls.Look.y*60*dt-(!automatedPlayback?Input.GetAxisRaw("Mouse Y")*1.5f:0),4,45);}
  }
  if(expedition.OnFoot||paused||!RoverReady){roverDrive=roverSteer=0;roverBrake=true;}
 }
 void RoverStep(float dt){
  float before=wheelDeployment;wheelDeployment=Mathf.MoveTowards(wheelDeployment,wheelsRequested?1:0,dt/1.35f);
  if(before>0&&wheelDeployment==0){expedition.ResetCrew();throttle=0;Notice("Tires stowed / flight ready. R2 thrust, Cross boost; Circle levels the ship.");}
  Vector3 p=Pos(player),v=Vel(player);var body=expedition.Nearest(p);Vector3 radial=(p-body.body.P).normalized,up=TerrainNormal(p),forward=PlanetExpedition.SurfaceTangent(up,ShipForward),right=Vector3.Cross(up,forward).normalized;
  float speed=Vector3.Dot(v,forward),alt=expedition.Alt(p);bool contact=alt<1.45f;
  // Static parking brake: retain native terrain/boulder contact without downhill creep.
  if(contact&&(expedition.OnFoot||roverBrake&&v.magnitude<.25f)){
   player=Make(p,Vector3.zero);PlanetExpedition.StepMode(ref player,-Gravity(p)/.9f,dt,3);return;
  }
  float angle=contact&&RoverReady&&!expedition.OnFoot?Mathf.Tan(roverSteer*32*Mathf.Deg2Rad)*speed/2.7f*dt*Mathf.Rad2Deg:0;
  forward=Quaternion.AngleAxis(angle,up)*forward;shipRotation=desiredRotation=Quaternion.Slerp(shipRotation,Quaternion.LookRotation(forward,up),1-Mathf.Exp(-9*dt));
  float drive=Vector3.Angle(up,radial)>42?0:roverDrive;float target=roverBrake?0:drive*(drive>=0?10.5f:4.5f);
  Vector3 force=Vector3.zero;if(contact){Vector3 tangent=Vector3.ProjectOnPlane(v,up);force=forward*(target-speed)*(roverBrake?9:3.8f)-right*Vector3.Dot(tangent,right)*9-Vector3.ProjectOnPlane(Gravity(p),up);force=Vector3.ClampMagnitude(force,roverBrake?18:12);force-=radial*3;}
  PlanetExpedition.StepMode(ref player,force/.9f,dt,3);
  // The wide chassis also respects the same deterministic boulders as the native center contact.
  if(SurfaceScatter.Current!=null){Vector3 q=Pos(player),adjust=Vector3.zero;foreach(var a in roverView.anchors){Vector3 w=q+shipRotation*a,clear=SurfaceScatter.Current.Clearance(w,.54f);adjust+=Vector3.ProjectOnPlane(clear-w,radial);}if(adjust.sqrMagnitude>.00001f){adjust=Vector3.ClampMagnitude(adjust,.25f);player.x+=adjust.x;player.y+=adjust.y;player.z+=adjust.z;Vector3 velocity=Vel(player);if(Vector3.Dot(velocity,adjust)<0)velocity=Vector3.ProjectOnPlane(velocity,adjust.normalized);player.vx=velocity.x;player.vy=velocity.y;player.vz=velocity.z;}}
 }
 void RoverPose(float dt){if(roverView==null)return;float speed=Vector3.Dot(Vel(player),ShipForward);roverView.Pose(wheelDeployment,roverSteer,speed,dt,expedition,!paused&&outcome=="");var canopy=rocket.Find("Canopy");if(canopy!=null)canopy.gameObject.SetActive(wheelDeployment<.05f);
  if(RoverDeployed&&!expedition.OnFoot&&wheelDeployment>.1f)expedition.SeatCrew(rocket,Mathf.SmoothStep(0,1,wheelDeployment));
 }
 void RoverCamera(float dt){Vector3 p=Pos(player),up=ShipUp,f=Quaternion.AngleAxis(roverCameraYaw,up)*ShipForward,right=Vector3.Cross(up,f).normalized;Vector3 look=Quaternion.AngleAxis(roverCameraPitch,right)*f;Vector3 goal=p-look*9.4f+up*2.1f;cam.transform.position=Vector3.Lerp(cam.transform.position,goal,1-Mathf.Exp(-8*dt));cam.transform.rotation=Quaternion.Slerp(cam.transform.rotation,Quaternion.LookRotation(p+up*.8f-cam.transform.position,up),1-Mathf.Exp(-10*dt));cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,65,1-Mathf.Exp(-6*dt));}
 void DrawRoverOptions(bool overlay){if(legacyUniverse||GroundActive||overlay)return;
  if(RoverDeployed||expedition.Alt(Pos(player))<1.05f&&Vel(player).magnitude<1.5f){Panel(new Rect(44,645,335,144));Label(60,659,RoverDeployed?(RoverReady?"ROVER / CREW ABOARD":"WHEEL SYSTEM MOVING"):"LANDED / SURFACE OPTIONS",text);bool old=GUI.enabled;GUI.enabled=wheelDeployment==0||RoverReady;if(GameButton(new Rect(60,696,300,35),"6 / PAD DOWN : "+(RoverDeployed?"RETRACT":"DEPLOY")+" TIRES",button))ToggleWheels();GUI.enabled=old;if(GameButton(new Rect(60,739,300,35),"J / TRIANGLE : EXIT CREW",button))GroundBoard();}
 }
}
