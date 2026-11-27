using System.Collections.Generic;
using UnityEngine;

public sealed partial class DefenseGame {
 readonly Dictionary<int,Transform> contractViews=new Dictionary<int,Transform>();
 Transform escortView;
 OrbitDemo.State escortState;
 int escortId=-1;
 int CurrentPort {
  get{var p=expedition.Nearest(GroundActive?expedition.Focus:Pos(player));var port=p==null?null:ports.Find(x=>x.key==p.key);return port==null?0:Mathf.Clamp(port.system*4+port.index,0,127);}
 }
 List<VoyagerContract> EnsureContracts(){return voyager.Board(CurrentPort,civilization);}
 void AnchorContract(VoyagerContract c){
  var p=expedition.planets.Find(x=>x.key==CivilPortKey(c.port));if(p==null)return;
  Vector3 focus=GroundActive?expedition.Focus:Pos(player);var colony=civilization.colonies.Find(x=>x.port==c.port);
  Vector3 up=(focus-p.body.P).normalized,t=PlanetExpedition.SurfaceTangent(up,ShipForward);
  c.anchor=up;c.normal=(up*(float)p.body.radius+Quaternion.AngleAxis(c.id*73,up)*t*(170+c.id%4*35)).normalized;
  if(c.kind==5&&colony!=null)c.normal=colony.anchor;
  if(c.kind==4){Vector3 at=p.body.P+c.anchor*((float)p.body.radius+1150);var global=OriginGlobal+at;c.ex=global.x;c.ey=global.y;c.ez=global.z;}
  if(c.kind==6){string why;var war=civilization.wars.Find(w=>w.raid&&w.target==c.port&&!w.resolved);why="";if(war==null)war=civilization.Strike(c.faction,c.port,true,out why);if(war==null){c.failed=true;Toast(why);}else{c.foeId=war.id;InitializeCivilWarRoute(war);}}
 }
 Vector3 ContractPoint(VoyagerContract c){
  int id=c.kind==5&&c.carrying?c.targetPort:c.port;
  var planet=expedition.planets.Find(p=>p.key==CivilPortKey(id));if(planet==null)return Vector3.zero;
  Vector3 normal=c.normal;
  if(c.kind==5&&c.carrying){var target=civilization.colonies.Find(x=>x.port==c.targetPort);if(target!=null)normal=target.anchor;}
  if(c.kind==1){Vector3 t=PlanetExpedition.SurfaceTangent(normal,Vector3.right),side=Vector3.Cross(normal,t);normal=(normal*(float)planet.body.radius+t*(c.count*55)+side*(c.count%2*45)).normalized;}
  return planet.body.P+normal*((float)planet.body.radius+PlanetExpedition.Height(normal,planet.body.seed,planet.body.radius)+1.2f);
 }
 void CreateContractView(VoyagerContract c,PlanetExpedition.Planet planet){
  if(contractViews.TryGetValue(c.id,out var old)&&old!=null)return;
  Transform root=new GameObject("Contract / "+c.title).transform;root.SetParent(planet.root.transform,true);root.position=ContractPoint(c);root.rotation=Quaternion.FromToRotation(Vector3.up,(root.position-planet.body.P).normalized);
  if(c.kind==0){
   var person=Instantiate(expedition.Crew.gameObject).transform;person.SetParent(root,false);person.localPosition=Vector3.zero;person.localScale=Vector3.one*1.2f;
   foreach(var tr in person.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=0;
   var photo=person.Find("Runtime-only helmet photo");if(photo!=null)photo.gameObject.SetActive(false);
   var visor=person.Find("Visor gleam");if(visor!=null)visor.gameObject.SetActive(true);
  }else{
   var basePart=Shape("Signal cache / pressure casing",PrimitiveType.Cylinder,Vector3.down*.8f,new Vector3(2,.3f,2),new Color(.1f,.17f,.21f),root);
   Shape("Signal cache / illuminated core",PrimitiveType.Cube,Vector3.zero,new Vector3(.8f,1.1f,.8f),new Color(.2f,.6f,.65f),root);
   if(c.kind==2)for(int i=-1;i<=1;i++)Shape("Salvage / provision case",PrimitiveType.Cube,new Vector3(i*.7f,-.3f,.8f),Vector3.one*.6f,new Color(.5f,.27f,.10f),root);
  }
  var pole=new GameObject("Signal / vertical locator").AddComponent<LineRenderer>();pole.transform.SetParent(root,false);pole.useWorldSpace=false;pole.positionCount=2;pole.SetPositions(new[]{Vector3.zero,Vector3.up*7});pole.widthMultiplier=.045f;pole.sharedMaterial=frontierArt.beamCyan;
  contractViews[c.id]=root;
 }
 void ClearContractViews(){
  foreach(var item in contractViews.Values)if(item!=null)Destroy(item.gameObject);contractViews.Clear();
  if(escortView!=null)Destroy(escortView.gameObject);escortView=null;escortId=-1;
 }
 void VoyagerShift(Vector3 shift){if(voyagerAudio!=null)voyagerAudio.Shift(shift);if(voyagerVfx!=null)voyagerVfx.Shift(shift);if(escortView!=null){MoveState(ref escortState,shift);escortView.position-=shift;}}
 void FinishContract(VoyagerContract c){
  if(!voyager.Complete(c,civilization))return;
  if(c.carrying){voyager.cargo=Mathf.Max(0,voyager.cargo-8);c.carrying=false;}
  if(contractViews.TryGetValue(c.id,out var view)&&view!=null)Destroy(view.gameObject);contractViews.Remove(c.id);
  Toast("CONTRACT COMPLETE / "+c.title+" / +"+c.reward+" alloy / +"+c.science+" science");
  if(voyagerAudio!=null)voyagerAudio.Cue("success",cam.transform.position);SaveCampaign();
 }
 void VoyagerContractsTick(float dt){
  foreach(var c in voyager.contracts){
   if(!c.accepted||c.completed||c.failed)continue;
   if(c.kind==6){var war=civilization.wars.Find(w=>w.id==c.foeId);if(war!=null&&war.resolved){if(war.won)FinishContract(c);else{c.failed=true;Toast("Defense contract lost. Rebuild and try another front.");}}continue;}
   if(c.kind==4){EscortTick(c,dt);continue;}
   int target=c.kind==5&&c.carrying?c.targetPort:c.port;
   var planet=expedition.planets.Find(p=>p.key==CivilPortKey(target));if(planet==null)continue;
   if(c.kind==3){
    if(GroundActive&&surface.Current!=null&&surface.Planet==planet){
     var foe=surface.Current.foes.Find(f=>f.contractId==c.id);
     if(foe==null&&!c.spawned){foe=surface.Spawn(voyager.chapter>=3?2:1,ContractPoint(c));if(foe!=null){foe.contractId=c.id;foe.maxHp*=1.35f;foe.hp=foe.maxHp;c.spawned=true;}}
     if(foe!=null&&foe.dead)FinishContract(c);
    }continue;
   }
   if((c.kind==0||c.kind==2)&&c.carrying){
    if(contractViews.TryGetValue(c.id,out var survivor)&&survivor!=null){survivor.gameObject.SetActive(GroundActive);if(GroundActive){Vector3 up=(expedition.Focus-planet.body.P).normalized;survivor.position=Vector3.Lerp(survivor.position,expedition.Focus+expedition.Crew.forward*2.5f,1-Mathf.Exp(-3*dt));survivor.rotation=Quaternion.LookRotation(-expedition.Crew.forward,up);}}
    if(!GroundActive&&CurrentPort==c.port)FinishContract(c);continue;
   }
   if(!GroundActive||surface.Planet!=planet)continue;
   CreateContractView(c,planet);Vector3 point=ContractPoint(c);if(!c.spawned&&(c.kind==0||c.kind==2)&&surface.Current!=null&&Vector3.Distance(expedition.Focus,point)<65){for(int guard=0;guard<2;guard++){var foe=surface.Spawn(voyager.chapter>=5&&guard==1?1:0,point+PlanetExpedition.SurfaceTangent((point-planet.body.P).normalized,Vector3.right)*(guard==0?10:-10));if(foe!=null)foe.contractId=c.id;}c.spawned=true;}
   if(contractViews.TryGetValue(c.id,out var visual)&&visual!=null)visual.position=point;
   bool near=Vector3.Distance(expedition.Focus,point)<4.8f;
   if(!near||surface.ContractInputBusy){c.progress=0;continue;}
   if(!CrewInteract){c.progress=0;continue;}c.progress+=dt;
   if(c.progress<1.2f)continue;c.progress=0;interactionToggle=false;
   if(c.kind==0||c.kind==2){if(voyager.cargo+8>voyager.Capacity(civilization.blueprint)){Toast("Cargo full. Deliver a contract or fit a larger hold.");continue;}voyager.cargo+=8;c.carrying=true;Toast(c.kind==0?"Surveyor rescued / return to your ship":"Cargo recovered / return to your ship");}
   else if(c.kind==1){c.count++;if(c.count>=3)FinishContract(c);else Toast("Geological signal "+c.count+" / 3 / next signal marked");}
   else if(c.kind==5){
    if(c.carrying)FinishContract(c);
    else{var policy=voyager.Policy(c.port);if(policy.food<12){Toast("The source colony needs at least 12 provisions.");continue;}if(voyager.cargo+8>voyager.Capacity(civilization.blueprint)){Toast("Insufficient hold capacity.");continue;}policy.food-=12;voyager.cargo+=8;c.carrying=true;Toast("Provisions loaded / deliver to "+ports[c.targetPort].name);if(visual!=null)Destroy(visual.gameObject);contractViews.Remove(c.id);}
   }
   SaveCampaign();
  }
 }
 void EscortTick(VoyagerContract c,float dt){
  var p=expedition.planets.Find(x=>x.key==CivilPortKey(c.port));if(p==null)return;
  Vector3 absolute=new GalacticPoint(c.ex,c.ey,c.ez)-OriginGlobal;
  if(escortId!=c.id||escortView==null){
   if(Vector3.Distance(Pos(player),absolute)>1500)return;
   if(escortView!=null)Destroy(escortView.gameObject);escortId=c.id;escortView=frontierArt.Hull(null,1);escortView.name="Contract / vulnerable supply shuttle";escortView.localScale=Vector3.one*2;
   escortState=Make(absolute,new Vector3((float)c.evx,(float)c.evy,(float)c.evz));escortView.position=absolute;
  }
  if(c.escortHealth<=0){c.failed=true;Destroy(escortView.gameObject);escortView=null;Toast("Supply shuttle destroyed / escort contract failed");SaveCampaign();return;}
  Vector3 up=c.anchor,t=PlanetExpedition.SurfaceTangent(up,Vector3.right),axis=Vector3.Cross(up,t);
  Vector3 next=Quaternion.AngleAxis((c.stage+1)*8,axis)*up;
  Vector3 goal=p.body.P+next*((float)p.body.radius+1150),delta=goal-Pos(escortState);
  bool accompanied=!GroundActive&&Vector3.Distance(Pos(player),Pos(escortState))<220;
  Vector3 desired=accompanied?delta.normalized*Mathf.Min(40,delta.magnitude*.35f):Vector3.zero;
  Vector3 force=Vector3.ClampMagnitude((desired-Vel(escortState))*3-Gravity(Pos(escortState)),120);
  for(float t0=0;t0<dt-.00001f;t0+=.01f)Step(ref escortState,force,Mathf.Min(.01f,dt-t0));
  escortView.position=Pos(escortState);if(delta.sqrMagnitude>1)escortView.rotation=Quaternion.Slerp(escortView.rotation,Quaternion.LookRotation(delta,up),dt*2);
  var global=OriginGlobal+Pos(escortState);c.ex=global.x;c.ey=global.y;c.ez=global.z;c.evx=escortState.vx;c.evy=escortState.vy;c.evz=escortState.vz;
  if(!c.spawned&&accompanied){c.spawned=true;for(int i=2;i<agents.Length;i++){agents[i].state=Make(Pos(escortState)+new Vector3(90+i*8,30,35),Vector3.zero);agents[i].status=0;agents[i].fireTime=1;}}
  if(delta.magnitude<25){c.stage++;if(c.stage>=3){FinishContract(c);Destroy(escortView.gameObject);escortView=null;}}
 }
 bool VoyagerShotHit(Vector3 before,Vector3 after,bool enemy){
  if(voyager==null||!enemy||escortView==null)return false;
  if(SegmentDistance(Pos(escortState),before,after)>5)return false;
  var c=voyager.contracts.Find(x=>x.id==escortId);if(c==null||c.completed||c.failed)return false;
  c.escortHealth=Mathf.Max(0,c.escortHealth-12*CampaignChapters.EnemyDamage(voyager.chapter,preferences.difficulty));CinematicPresentation.Impact(Pos(escortState),Color.cyan);return true;
 }
 void NavigateContract(VoyagerContract c){
  if(c.kind==6){CloseVoyager();CivilNavigateWar();return;}
  if(c.kind==4){CloseVoyager();CivilRoute(new GalacticPoint(c.ex,c.ey,c.ez),"SUPPLY SHUTTLE");return;}
  int target=c.kind==5&&c.carrying?c.targetPort:c.port;
  if(CurrentPort==target&&GroundActive){CloseVoyager();Toast(c.Instruction+" / follow the cyan signal");return;}
  var port=ports[target];Vector3 n=c.normal;if(c.kind==5&&c.carrying){var colony=civilization.colonies.Find(x=>x.port==target);if(colony!=null)n=colony.anchor;}
  CloseVoyager();chartSystem=target/4;chartPort=target%4;CivilRoute(port.global+n*(port.radius+1200),port.name+" / CONTRACT",port,n);
 }
 void DrawContractMarkers(){
  if(voyager==null)return;
  foreach(var c in voyager.contracts)if(c.accepted&&!c.completed&&!c.failed){
   Vector3 point;
   if(c.kind==4){point=new GalacticPoint(c.ex,c.ey,c.ez)-OriginGlobal;}
   else if(c.kind==6){var w=civilization.wars.Find(x=>x.id==c.foeId);if(w==null)continue;point=CivilBattleGlobal(w)-OriginGlobal;}
   else {int port=c.kind==5&&c.carrying?c.targetPort:c.port;if(port!=CurrentPort)continue;point=(c.carrying&&(c.kind==0||c.kind==2))?Pos(player):ContractPoint(c);}
   VMarker(point,c.kind==4?"SUPPLY SHUTTLE / "+c.escortHealth.ToString("F0")+"%":c.title,new Color(.2f,.95f,.85f));
   if(GroundActive&&Vector3.Distance(expedition.Focus,point)<5&&c.kind!=3&&c.kind!=4&&c.kind!=6)VText(new Rect(540,600,700,56),(preferences.toggleInteract?"E / R1 toggle":"HOLD "+preferences.Key(KeyCode.E)+" / R1")+" / "+c.Instruction,VoyagerFont.Body);
  }
 }
}
