using System;
using System.Collections.Generic;
using UnityEngine;

// Campaign rules are local and deterministic. Runtime scene objects never enter a save.
[Serializable]
public sealed class VoyagerState {
 public int version=1,slot=1,seed=7319,scenario,tutorial,contractSerial=1,contractsDone,trades,crisesResolved,chapter,nativeKills,flagshipWar,defensesWon,lastBoardChapter=-1;
 public bool campaignComplete,flagshipDefeated;
 public float time,food=80,cargo,shield=35,heat,shieldDelay,worldClock,crisisClock;
 public int drive,weapon,defense,hold,unlockedModules=1,legacy;
 public bool landed,tires,walked,recorder,extracted,founded,launched,firstDefense,journeyReward;
 public List<int> visited=new List<int>(),recordedWars=new List<int>();
 public List<string> victories=new List<string>();
 public List<VoyagerContract> contracts=new List<VoyagerContract>();
 public List<ColonyPolicy> policies=new List<ColonyPolicy>();
 public List<FactionAgenda> agendas=new List<FactionAgenda>();
 public List<SupplyRoute> routes=new List<SupplyRoute>();
 public Crisis crisis=new Crisis();
 [Serializable] public sealed class ColonyPolicy {public int port,focus;public float food=65,morale=75,output; }
 [Serializable] public sealed class FactionAgenda {public int faction,industry=1,technology=1;public float treasury=80,clock;public List<int> worlds=new List<int>();}
 [Serializable] public sealed class SupplyRoute {public int source,target;public float progress;public int deliveries;public bool enabled=true;}
 [Serializable] public sealed class Crisis {public int kind=-1,port;public float remaining;public bool active;public string title="";}
 public static readonly string[] ModuleNames={
  "Vector drive","Ion sprint drive","Armored freight drive",
  "Pulse repeater","Rail accelerator","Scatter battery",
  "Balanced deflector","Deep shield","Reactive armor",
  "Survey hold","Freight expansion","Research laboratory"};
 public static readonly int[] ModuleCost={0,140,110,0,190,160,0,170,150,0,130,180};
 public static readonly int[] ModulePower={1,3,1,2,4,3,1,3,1,1,1,3};
 public static readonly string[] ModuleDescriptions={
  "Reliable handling. 1 reactor power.","+25% thrust, +20% speed; +heat. 3 power.","+35 hull; -12% thrust. 1 power.",
  "Balanced cannon and heat. 2 power.","One fast heavy projectile; longer cooldown. 4 power.","Five short-range pellets; high heat. 3 power.",
  "35 regenerating shield. 1 power.","80 regenerating shield; -8% thrust. 3 power.","+70 hull; 15 shield; -12% speed. 1 power.",
  "18 cargo capacity. 1 power.","+30 cargo; -10% thrust. 1 power.","+50% contract science; 12 cargo. 3 power."};
 public VoyagerState(){for(int i=0;i<3;i++)agendas.Add(new FactionAgenda{faction=i,worlds=new List<int>{i==0?1:i==1?2:4}});}
 public int Module(int category){return category==0?drive:category==1?weapon:category==2?defense:hold;}
 public void SetModule(int category,int choice){if(category==0)drive=choice;else if(category==1)weapon=choice;else if(category==2)defense=choice;else hold=choice;}
 public int Reactor(int hull){return 7+Mathf.Clamp(hull,0,3)*2;}
 public int Draw {get{return ModulePower[drive]+ModulePower[3+weapon]+ModulePower[6+defense]+ModulePower[9+hold];}}
 public float HullBonus {get{return (drive==2?35:0)+(defense==2?70:0);}}
 public float ShieldMax {get{return defense==1?80:defense==2?15:35;}}
 public float Thrust {get{return (drive==1?1.25f:drive==2?.88f:1)*(defense==1?.92f:1)*(hold==1?.90f:1);}}
 public float Speed {get{return (drive==1?1.2f:1)*(defense==2?.88f:1);}}
 public int Capacity(int hull){return (hold==2?12:18)+(hold==1?30:0)+(hull==1?35:0);}
 public bool CanInstall(int category,int choice,int hull,out string why){
  why="";if(category<0||category>3||choice<0||choice>2){why="Unknown module";return false;}
  int index=category*3+choice,old=category*3+Module(category);
  if(Draw-ModulePower[old]+ModulePower[index]>Reactor(hull)){why="Reactor limit: choose a lower power module first";return false;}
  int cap=category==3?(choice==2?12:18)+(choice==1?30:0)+(hull==1?35:0):Capacity(hull);
  if(cargo>cap){why="Deliver cargo before reducing hold capacity";return false;}return true;
 }
 public ColonyPolicy Policy(int port){var p=policies.Find(x=>x.port==port);if(p==null){p=new ColonyPolicy{port=port};policies.Add(p);}return p;}
 public int Owner(int port,CivilizationModel civ){if(civ.colonies.Exists(c=>c.port==port))return -1;foreach(var a in agendas)if(a.worlds.Contains(port))return a.faction;return -1;}
 public List<VoyagerContract> Board(int port,CivilizationModel civ){
  if(lastBoardChapter!=chapter){contracts.RemoveAll(c=>!c.accepted);lastBoardChapter=chapter;}
  var result=contracts.FindAll(c=>!c.completed&&!c.failed&&(c.accepted||c.port==port));
  int available=result.FindAll(c=>!c.accepted).Count;
  while(available<6){int serial=contractSerial++;int kind=(serial+port+seed)%(chapter<3?4:7);
   if(kind==5&&civ.colonies.Count<2)kind=serial%4;
   if(kind==6&&(civ.colonies.Count==0||chapter<4||civ.fleet<2))kind=1;
   var c=new VoyagerContract{id=serial,kind=kind,port=port,faction=serial%3,targetPort=port,
    title=VoyagerContract.Names[kind],reward=95+kind*25+chapter*10,science=20+kind*7+chapter*3,
    normal=Quaternion.AngleAxis(serial*137.5f,Vector3.up)*new Vector3(.18f,.94f,-.29f).normalized};
   if(kind==5){var destination=civ.colonies.Find(x=>x.port!=port);if(destination!=null)c.targetPort=destination.port;}
   contracts.Add(c);result.Add(c);available++;
  }
  contracts.RemoveAll(c=>(c.completed||c.failed||!c.accepted&&c.port!=port)&&c.id<contractSerial-30);
  foreach(var c in result){c.utility=c.accepted?1000:20;c.why="A frontier contract that funds your next expedition.";
   if(c.kind==1&&civ.science<80){c.utility+=60;c.why="Your science reserve is low. Geological surveys fund research.";}
   if(c.kind==2&&civ.alloy<250){c.utility+=70;c.why="Recovering lost cargo can fund your next district or ship module.";}
   if(c.kind==5&&policies.Exists(p=>p.food<25)){c.utility+=90;c.why="A colony is short of food. Delivering provisions restores its supply.";}
   if(c.kind==6&&civ.wars.Exists(w=>w.raid&&!w.resolved)){c.utility+=100;c.why="An incoming raid threatens your people. Reinforce the local front.";}
   if(c.kind==4){c.utility+=chapter>=4?30:-20;c.why="The convoy needs an armed ship in orbit. Protect it to earn faction trust.";}
   if(c.kind==0&&civ.factions[c.faction].relation<70){c.utility+=30;c.why="Rescuing their surveyor improves diplomatic relations.";}
  }
  result.Sort((a,b)=>b.utility.CompareTo(a.utility));return result;
 }
 public bool Accept(VoyagerContract c,out string why){why="";if(c==null||c.completed||c.failed){why="Contract unavailable";return false;}if(c.accepted)return true;if(contracts.FindAll(x=>x.accepted&&!x.completed&&!x.failed).Count>=3){why="Three active contracts maximum";return false;}if(c.kind==4&&contracts.Exists(x=>x.accepted&&x.kind==4&&!x.completed&&!x.failed)){why="Finish the active escort first";return false;}c.accepted=true;return true;}
 public bool Complete(VoyagerContract c,CivilizationModel civ){if(c==null||!c.accepted||c.completed||c.failed)return false;c.completed=true;contractsDone++;civ.alloy=Mathf.Min(1000000,civ.alloy+c.reward);civ.science=Mathf.Min(1000000,civ.science+c.science*(hold==2?1.5f:1));civ.factions[c.faction].relation=Mathf.Min(100,civ.factions[c.faction].relation+8);civ.influence+=8;if(c.kind==5){trades++;Policy(c.targetPort).food=Mathf.Min(200,Policy(c.targetPort).food+12);}return true;}
 public void TickEconomy(float dt,CivilizationModel civ){
  time+=dt;shieldDelay=Mathf.Max(0,shieldDelay-dt);shield=Mathf.MoveTowards(shield,ShieldMax,dt*(shieldDelay<=0&&heat<25?3:0));heat=Mathf.Max(0,heat-dt*9);
  foreach(var colony in civ.colonies){var p=Policy(colony.port);int homes=civ.Count(colony,1);
   p.food=Mathf.Clamp(p.food+((.07f+homes*.18f)*(p.focus==3?2.4f:1)-colony.population*.0034f)*dt,0,200);
   p.morale=Mathf.MoveTowards(p.morale,p.food>10?85:35,dt*.3f);
   float factor=p.morale<40?.55f:p.focus==1?1.35f:p.focus==2?.78f:p.focus==3?.7f:1;
   float correction=civ.Count(colony,2)*1.8f*(factor-1)*dt;
   civ.alloy=Mathf.Clamp(civ.alloy+correction,0,1000000);
   if(p.focus==2&&civ.energy>0)civ.science=Mathf.Min(1000000,civ.science+civ.Count(colony,4)*.20f*dt);
   if(p.food<=0)colony.population=Mathf.Max(12,colony.population-dt*.35f);
   p.output=factor;
  }
  foreach(var route in routes){if(!route.enabled)continue;var from=civ.colonies.Find(x=>x.port==route.source);var to=civ.colonies.Find(x=>x.port==route.target);if(from==null||to==null)continue;
   route.progress+=dt/90;if(route.progress>=1){var source=Policy(route.source);if(source.food>=15){source.food-=15;Policy(route.target).food=Mathf.Min(200,Policy(route.target).food+15);route.deliveries++;trades++;civ.alloy=Mathf.Min(1000000,civ.alloy+25);}route.progress=0;}
  }
  foreach(var a in agendas){a.clock+=dt;a.treasury+=dt*(.16f+a.worlds.Count*.04f);if(a.clock>=120){a.clock=0;if(a.treasury>=45){a.treasury-=45;a.industry=Mathf.Min(12,a.industry+1);a.technology=Mathf.Min(10,a.technology+1);
    for(int n=0;n<96;n++){int port=(seed+a.industry*11+a.faction*23+n)%128;if(port%4!=3&&Owner(port,civ)<0&&!agendas.Exists(x=>x.worlds.Contains(port))&&!civ.colonies.Exists(x=>x.port==port)){a.worlds.Add(port);break;}}
   }}}
  if(civ.Level>=3&&!crisis.active){crisisClock+=dt;if(crisisClock>=360){crisisClock=0;crisis=new Crisis{kind=crisesResolved%3,port=civ.colonies[0].port,active=true,remaining=180,title=new[]{"Stellar interference","Frontier food emergency","Diplomatic blockade"}[crisesResolved%3]};}}
  if(crisis.active){crisis.remaining-=dt;if(crisis.kind==0)civ.energy=Mathf.Max(0,civ.energy-dt*8);if(crisis.remaining<=0){civ.alloy*=.90f;Policy(crisis.port).morale=Mathf.Max(20,Policy(crisis.port).morale-15);crisis.active=false;}}
  UnlockVictories(civ);
 }
 public bool ResolveCrisis(CivilizationModel civ,out string why){why="";if(!crisis.active){why="No active crisis";return false;}if(!civ.Pay(crisis.kind==1?60:150,crisis.kind==0?80:30,crisis.kind==0?45:15)){why="Relief needs alloy, energy and research";return false;}if(crisis.kind==1)Policy(crisis.port).food+=80;if(crisis.kind==2)foreach(var f in civ.factions)f.relation=Mathf.Min(100,f.relation+12);crisis.active=false;crisesResolved++;civ.influence+=30;return true;}
 public void UnlockVictories(CivilizationModel civ){
  if(visited.Count>=8&&contractsDone>=10&&!victories.Contains("PATHFINDER"))victories.Add("PATHFINDER");
  if(civ.factions.TrueForAll(f=>f.relation>=70)&&trades>=5&&!victories.Contains("CONCORD"))victories.Add("CONCORD");
  if(civ.warsWon>=6&&firstDefense&&!victories.Contains("GUARDIAN"))victories.Add("GUARDIAN");
  if(civ.dysonBuilt==12&&civ.colonies.Count>=3&&!victories.Contains("STARFORGER"))victories.Add("STARFORGER");
 }
 public bool Validate(){
  if(version!=1||slot<1||slot>3||chapter<0||chapter>11||tutorial<0||tutorial>10||drive<0||drive>2||weapon<0||weapon>2||defense<0||defense>2||hold<0||hold>2||contracts==null||contracts.Count>80||policies==null||policies.Count>128||agendas==null||agendas.Count!=3||routes==null||routes.Count>24||visited==null||recordedWars==null||recordedWars.Count>10000||victories==null||crisis==null)return false;
  foreach(float v in new[]{time,food,cargo,shield,heat,shieldDelay,worldClock,crisisClock})if(!Finite(v)||v<0||v>10000000)return false;
  foreach(var c in contracts)if(c==null||c.kind<0||c.kind>6||c.port<0||c.port>=128||c.targetPort<0||c.targetPort>=128||c.faction<0||c.faction>2||!Finite(c.progress)||!Finite(c.escortHealth))return false;
  foreach(var p in policies)if(p==null||p.port<0||p.port>=128||p.focus<0||p.focus>3||!Finite(p.food)||!Finite(p.morale))return false;
  foreach(var a in agendas)if(a==null||a.faction<0||a.faction>2||a.worlds==null||a.worlds.Count>128||!Finite(a.treasury)||!Finite(a.clock))return false;
  foreach(var r in routes)if(r==null||r.source<0||r.source>=128||r.target<0||r.target>=128||!Finite(r.progress))return false;
  return Finite(crisis.remaining);
 }
 public static bool Finite(float n){return !float.IsNaN(n)&&!float.IsInfinity(n);}
}

[Serializable]
public sealed class VoyagerContract {
 public int id,kind,port,targetPort,faction,stage,count,reward,science,foeId;
 public string title,why;public float utility;
 public bool accepted,completed,failed,spawned,carrying;
 public float progress,escortHealth=100;
 public Vector3 normal,anchor;
 public double ex,ey,ez,evx,evy,evz;
 public static readonly string[] Names={"Recover a stranded surveyor","Survey three geological signals","Recover a lost cargo cache","Hunt an elite alien","Escort a supply shuttle","Deliver colony provisions","Defend the frontier"};
 public string Instruction {get{return kind==0?(carrying?"Return the survivor to your ship":"Reach the distress signal and assist the surveyor"):kind==1?"Scan geological signals: "+count+" / 3":kind==2?(carrying?"Return the recovered cargo to your ship":"Recover the marked cargo cache"):kind==3?"Eliminate the marked elite":kind==4?"Stay within 220m of the supply shuttle; protect it":kind==5?(carrying?"Deliver provisions to the destination colony":"Collect provisions near your colony command spire"):"Repel the raid at this colony";}}
}
