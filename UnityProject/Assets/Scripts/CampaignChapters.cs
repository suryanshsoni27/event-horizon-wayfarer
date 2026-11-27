using UnityEngine;

public static class CampaignChapters {
 public static readonly string[] Titles={
  "First Light","Echoes Below","A Home Among Stars","Torch to Orbit",
  "The Vanguard","Across the Black","Broken Accord","The Siege",
  "Star Harvest","War of Three Suns","The Long Blackout","Light of Civilization"};
 public static readonly string[] Stories={
  "Your survey craft reaches Pelagos. A distress recorder is transmitting beneath the clouds.",
  "The recorder leads to an abandoned relay. Something under the colony has awakened.",
  "Survival becomes settlement. Build a home that can power and supply itself.",
  "Launch the Aurora survey rocket. Your civilization is ready to look beyond the horizon.",
  "Rival fleets are watching. Build a shipyard, customize your craft and assemble a vanguard.",
  "A second world opens new possibilities. Establish a colony and earn the trust of the frontier.",
  "The first treaty has broken. Join your fleet and prove that your worlds can defend themselves.",
  "The frontier is under siege. Reinforce your colonies and keep supplies moving.",
  "Your cities need the power of a star. Construct the first four collector sectors.",
  "Three powers compete for the frontier. Secure three worlds and win the orbital war.",
  "Stellar interference threatens the network. Keep your people supplied and resolve the crises.",
  "An alien ark approaches the star. Complete the Dyson project and destroy its flagship."};
 public static readonly string[] Objectives={
  "Land, deploy tires, disembark and recover the flight recorder.",
  "Scan specimens, defend the relay, defeat the Warden and extract to your ship.",
  "Complete a command spire, foundry, solar farm and observatory.",
  "Build a launch complex and launch an Aurora rocket above 1 km.",
  "Complete a shipyard, equip a non-standard module and assemble three reserve warships.",
  "Own two worlds, visit two worlds and finish three frontier contracts.",
  "Win a planetary war and personally destroy two hostile warships.",
  "Win two defensive raids, build two defense batteries and operate a supply route.",
  "Complete four Dyson sectors and finish six frontier contracts.",
  "Own three worlds, win three wars and personally destroy six warships.",
  "Complete eight Dyson sectors, resolve two crises and complete five deliveries.",
  "Complete all twelve Dyson sectors, own three worlds and destroy the alien flagship."};
 static float Fraction(float n,float goal){return Mathf.Clamp01(n/goal);}
 public static float Progress(int chapter,VoyagerState s,CivilizationModel c){
  switch(chapter){
   case 0:return ((s.landed?1:0)+(s.tires?1:0)+(s.walked?1:0)+(s.recorder?1:0))/4f;
   case 1:return s.extracted?1:0;
   case 2:return (Fraction(c.Count(0),1)+Fraction(c.Count(2),1)+Fraction(c.Count(3),1)+Fraction(c.Count(4),1))/4;
   case 3:return Fraction(c.launches,1);
   case 4:return (Fraction(c.Count(6),1)+Fraction(c.fleet,3)+(s.drive+s.weapon+s.defense+s.hold>0?1:0))/3;
   case 5:return (Fraction(c.colonies.Count,2)+Fraction(s.visited.Count,2)+Fraction(s.contractsDone,3))/3;
   case 6:return (Fraction(c.warsWon,1)+Fraction(s.nativeKills,2))/2;
   case 7:return (Fraction(s.defensesWon,2)+Fraction(c.Count(7),2)+Fraction(s.routes.FindAll(r=>r.enabled).Count,1))/3;
   case 8:return (Fraction(c.dysonBuilt,4)+Fraction(s.contractsDone,6))/2;
   case 9:return (Fraction(c.colonies.Count,3)+Fraction(c.warsWon,3)+Fraction(s.nativeKills,6))/3;
   case 10:return (Fraction(c.dysonBuilt,8)+Fraction(s.crisesResolved,2)+Fraction(s.trades,5))/3;
   default:return (Fraction(c.dysonBuilt,12)+Fraction(c.colonies.Count,3)+(s.flagshipDefeated?1:0))/3;
  }
 }
 public static bool Advance(VoyagerState state,CivilizationModel civ){
  if(state.campaignComplete||Progress(state.chapter,state,civ)<.9999f)return false;
  civ.alloy=Mathf.Min(1000000,civ.alloy+75+state.chapter*45);
  civ.science=Mathf.Min(1000000,civ.science+20+state.chapter*12);
  civ.influence+=15;
  if(state.chapter==11){state.campaignComplete=true;if(!state.victories.Contains("WAYFARER"))state.victories.Add("WAYFARER");}
  else{state.chapter++;if(state.chapter==10&&state.crisesResolved<2&&!state.crisis.active)state.crisisClock=360;}
  return true;
 }
 // Each successive chapter raises actual enemy durability and damage, independently of difficulty.
 public static float EnemyHealth(int chapter,int difficulty){return (1+Mathf.Clamp(chapter,0,11)*.075f)*(difficulty==0?.72f:difficulty==2?1.25f:1);}
 public static float EnemyDamage(int chapter,int difficulty){return (1+Mathf.Clamp(chapter,0,11)*.055f)*(difficulty==0?.65f:difficulty==2?1.3f:1);}
}
