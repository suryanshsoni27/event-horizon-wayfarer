using System;
using System.Text.RegularExpressions;
using UnityEngine;

public sealed partial class DefenseGame {
 enum VoyagerFont {Small,Body,Heading,Hero}
 GUIStyle vSmall,vBody,vHeading,vHero,vButton;
 readonly CampaignCheckpoint[] slotCache=new CampaignCheckpoint[3];
 bool slotsCached;int homePreviewSlot=-1;
 static readonly Color vCyan=new Color(.23f,.91f,.83f),vGold=new Color(1,.70f,.30f),vWhite=new Color(.91f,.95f,1),vMuted=new Color(.65f,.75f,.83f);
 static readonly string[] padNames={"Cross","Circle","Square","Triangle","L1","R1","L3","R3","Options","Share","D-pad up","D-pad down","D-pad left","D-pad right"};
 static readonly KeyCode[] remappableKeys={KeyCode.W,KeyCode.S,KeyCode.A,KeyCode.D,KeyCode.Space,KeyCode.LeftShift,KeyCode.J,KeyCode.E,KeyCode.R,KeyCode.Q,KeyCode.M,KeyCode.K,KeyCode.B,KeyCode.U,KeyCode.O,KeyCode.Tab,KeyCode.F5};
 static readonly string[] keyActions={"Forward / throttle","Reverse / brake","Strafe / steer left","Strafe / steer right","Brake / hop","Boost / sprint","Board / disembark","Interact / place","Reload on foot","Pulse / roll","Stellar atlas","VTOL descent","Civilization command","Salvage upgrades","Expedition journal","Frontier contracts","Quick checkpoint"};
 void VStyles(){
  if(vBody!=null)return;
  vSmall=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true,normal={textColor=Color.white}};
  vBody=new GUIStyle(vSmall){fontSize=22};vHeading=new GUIStyle(vBody){fontSize=32,fontStyle=FontStyle.Bold};
  vHero=new GUIStyle(vBody){fontSize=60,fontStyle=FontStyle.Bold};vButton=new GUIStyle(vBody){alignment=TextAnchor.MiddleLeft,fontStyle=FontStyle.Bold};
 }
 void VMatrix(){float scale=Mathf.Min(Screen.width/1600f,Screen.height/1000f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)*.5f,(Screen.height-1000*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);}
 void VBox(Rect r,Color c){GUI.color=c.linear;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
 void VText(Rect r,string message,VoyagerFont font=VoyagerFont.Body,Color? color=null,bool resolveBindings=true){
  VStyles();var style=font==VoyagerFont.Small?vSmall:font==VoyagerFont.Heading?vHeading:font==VoyagerFont.Hero?vHero:vBody;
  int original=style.fontSize;style.fontSize=Mathf.RoundToInt(original*(preferences==null?1:preferences.uiScale));
  Color ink=color??(font==VoyagerFont.Small?vMuted:vWhite);if(preferences!=null&&preferences.highContrast&&ink==vMuted)ink=Color.white;GUI.color=ink.linear;GUI.Label(r,resolveBindings?ControlHints(message):message,style);GUI.color=Color.white;style.fontSize=original;
 }
 bool VButton(Rect r,string text,bool selected=false,bool resolveBindings=true){
  bool hover=!automatedPlayback&&r.Contains(Event.current.mousePosition);
  VBox(r,selected?new Color(.045f,.23f,.25f):hover?new Color(.06f,.13f,.18f):new Color(.025f,.065f,.10f));
  if(selected)VBox(new Rect(r.x,r.y,4,r.height),vCyan);
  bool pressed=GameButton(r,"",GUIStyle.none);if(pressed&&voyagerAudio!=null)voyagerAudio.Cue("ui",cam.transform.position);VText(new Rect(r.x+22,r.y+10,r.width-35,r.height-9),text,VoyagerFont.Body,selected?vCyan:vWhite,resolveBindings);return pressed;
 }
 void VBar(Rect r,float value,Color color){VBox(r,new Color(.07f,.13f,.18f));VBox(new Rect(r.x,r.y,r.width*Mathf.Clamp01(value),r.height),color);}
 public string ControlHints(string value){
  if(preferences==null||value==null)return value;
  return Regex.Replace(value,@"\b(D-pad up|D-pad down|D-pad left|D-pad right|Cross|Circle|Square|Triangle|L1|R1|L3|R3|Options|Share)\b",m=>{
   int index=Array.FindIndex(padNames,n=>string.Equals(n,m.Value,StringComparison.OrdinalIgnoreCase));
   return index<0?m.Value:padNames[preferences.buttons[index]];
  },RegexOptions.IgnoreCase);
 }
 void RefreshSlotCache(){for(int i=0;i<3;i++)slotCache[i]=ReadCampaign(i+1);slotsCached=true;}
 bool DrawVoyagerMenu(){
  if(!VoyagerMenuOpen)return false;VStyles();VMatrix();
  VBox(new Rect(0,0,1600,1000),new Color(.008f,.019f,.033f,1));
  VBox(new Rect(55,47,5,63),vCyan);VText(new Rect(79,37,1100,70),"WAYFARER",VoyagerFont.Hero);
  VText(new Rect(85,114,1050,36),"TWELVE HORIZONS  /  A CIVILIZATION BEGINS WITH ONE LANDING",VoyagerFont.Small,vCyan);
  if(voyagerPanel!=VoyagerPanel.Home&&voyagerPanel!=VoyagerPanel.Defeat&&VButton(new Rect(1270,59,270,54),"CIRCLE / BACK")){OpenVoyager(campaignStarted?VoyagerPanel.Pause:VoyagerPanel.Home);}
  if(voyagerRecord)VText(new Rect(900,125,640,33),"STAGED SHOWCASE / SEEDED RESOURCES",VoyagerFont.Small,vGold);
  if(voyagerPanel==VoyagerPanel.Home)DrawVoyagerHome();
  else if(voyagerPanel==VoyagerPanel.Pause)DrawVoyagerPause();
  else if(voyagerPanel==VoyagerPanel.Settings)DrawVoyagerSettings();
  else if(voyagerPanel==VoyagerPanel.Contracts)DrawVoyagerContracts();
  else if(voyagerPanel==VoyagerPanel.Outfitter)DrawVoyagerOutfitter();
  else if(voyagerPanel==VoyagerPanel.Colonies)DrawVoyagerColonies();
  else if(voyagerPanel==VoyagerPanel.Chapters)DrawVoyagerChapters();
  else if(voyagerPanel==VoyagerPanel.HeadPilot)DrawHeadPilotSetup();
  else DrawVoyagerDefeat();
  VBox(new Rect(60,934,1480,1),new Color(.15f,.25f,.30f));
  VText(new Rect(64,951,1470,38),toastTime>0?voyagerToast:"Left stick / D-pad / arrows select • Cross / Enter act • Circle / Escape back • "+(Controls.Connected?Controls.DeviceName:"No controller connected"),VoyagerFont.Small,toastTime>0?vGold:vMuted);
  if(rebindAction>=0||rebindKey>=0)DrawRebinding();
  return true;
 }
 void DrawVoyagerHome(){
  if(!slotsCached)RefreshSlotCache();var selected=slotCache[slotSelection-1];
  VText(new Rect(77,176,740,160),"Every horizon.\nA new beginning.",VoyagerFont.Hero);
  VText(new Rect(82,345,728,89),"Explore alien worlds. Design your ship. Build cities, defend your civilization, and harvest the light of a star.",VoyagerFont.Body,vMuted);
  string[] actions={selected==null?(legacyCivilization==null?"CONTINUE / no checkpoint in this slot":"IMPORT PREVIOUS CIVILIZATION"):"CONTINUE / CHAPTER "+(selected.campaign.chapter+1),overwriteSlot==slotSelection?"CONFIRM / REPLACE THIS CAMPAIGN":"NEW CAMPAIGN","SETTINGS & ACCESSIBILITY","THE TWELVE CHAPTERS","QUIT"};
  for(int i=0;i<5;i++)if(VButton(new Rect(82,440+i*72,706,61),actions[i],voyagerSelection==i)){voyagerSelection=i;VoyagerPrimary();}
  if(homePreviewSlot!=slotSelection||!civilizationScene.PreviewActive){var display=selected==null?civilization:selected.civilization;civilizationScene.Preview(3,display.blueprint,display.paint,display);homePreviewSlot=slotSelection;}
  VBox(new Rect(885,200,650,450),new Color(.018f,.047f,.069f));
  if(civilizationScene.PreviewTexture!=null){civilizationScene.FramePreview(618f/205);GUI.DrawTexture(new Rect(900,365,618,205),civilizationScene.PreviewTexture,ScaleMode.StretchToFill,false);}
  VText(new Rect(916,222,580,47),selected==null?"YOUR NEXT EXPEDITION":CampaignChapters.Titles[selected.campaign.chapter],VoyagerFont.Heading);
  VText(new Rect(919,284,576,80),selected==null?"Twelve horizons. One civilization. A ship that changes with your journey.":CampaignChapters.Stories[selected.campaign.chapter],VoyagerFont.Body,vMuted);
  VText(new Rect(919,578,570,66),selected==null?"NORMAL RESOURCES / THREE SAVE SLOTS\nKEYBOARD, MOUSE & DUALSENSE":selected.civilization.LevelName+"\n"+selected.civilization.colonies.Count+" worlds / "+selected.campaign.contractsDone+" contracts\n"+FriendlySaveTime(selected.savedUtc),VoyagerFont.Small,vCyan);
  VText(new Rect(895,685,610,35),"CAMPAIGN SLOTS / LEFT OR RIGHT TO SELECT",VoyagerFont.Small);
  for(int i=1;i<=3;i++){var saved=slotCache[i-1];if(VButton(new Rect(888+(i-1)*217,740,201,102),"SLOT "+i+"\n"+(saved==null?"Empty":"Chapter "+(saved.campaign.chapter+1)),slotSelection==i)){slotSelection=i;overwriteSlot=0;}}
  if(VButton(new Rect(82,834,706,61),"TRIANGLE / SCENARIO: "+new[]{"Frontier","Conflict","Scarcity"}[newCampaignScenario]+(newGamePlus?" / LEGACY":"")))newCampaignScenario=(newCampaignScenario+1)%3;
  if(legacyCivilization!=null&&selected==null)VText(new Rect(81,902,710,30),"Import your earlier civilization into this empty slot. Its original file is preserved.",VoyagerFont.Small);
 }
 static string FriendlySaveTime(string value){DateTime at;return DateTime.TryParse(value,out at)?at.ToLocalTime().ToString("MMM d, HH:mm"):"Checkpoint available";}
 void DrawVoyagerPause(){
  string[] choices={"RESUME EXPEDITION","SAVE CHECKPOINT","LOAD LAST CHECKPOINT","FRONTIER CONTRACTS","SHIP OUTFITTER","COLONIES & DIPLOMACY","CAMPAIGN / 12 CHAPTERS","SETTINGS & ACCESSIBILITY","FACE FLIGHT / CAMERA SETUP","SAVE & QUIT"};
  for(int i=0;i<choices.Length;i++)if(VButton(new Rect(78,209+i*64,610,56),choices[i],voyagerSelection==i)){voyagerSelection=i;VoyagerPrimary();}
  VText(new Rect(760,230,740,59),"CHAPTER "+(voyager.chapter+1)+" / "+CampaignChapters.Titles[voyager.chapter],VoyagerFont.Heading);
  VText(new Rect(765,321,708,128),CampaignChapters.Stories[voyager.chapter],VoyagerFont.Body,vMuted);
  VText(new Rect(765,488,710,130),CampaignChapters.Objectives[voyager.chapter],VoyagerFont.Body);
  VBar(new Rect(765,643,710,9),CampaignChapters.Progress(voyager.chapter,voyager,civilization),vCyan);
  VText(new Rect(766,703,704,100),civilization.LevelName+" / "+civilization.Population.ToString("F0")+" citizens\n"+civilization.alloy.ToString("F0")+" alloy / "+civilization.energy.ToString("F0")+" energy / "+civilization.science.ToString("F0")+" science",VoyagerFont.Body,vCyan);
  VText(new Rect(767,836,710,57),"Slot "+voyager.slot+" / "+saveStatus+"\nF5 saves while exploring. Checkpoints preserve your expedition.",VoyagerFont.Small);
  if(VButton(new Rect(765,775,710,49),"FACE FLIGHT / CAMERA SETUP (F7)"))OpenVoyager(VoyagerPanel.HeadPilot);
 }
 int SettingsRows(){return settingsPage==0?18:settingsPage==1?remappableKeys.Length:settingsPage==2?14:6;}
 string SettingLabel(int index){
  if(settingsPage==1)return keyActions[index]+"   /   "+preferences.Key(remappableKeys[index]);
  if(settingsPage==3)return new[]{"Graphics preset / "+new[]{"Performance","Balanced","Ultra"}[preferences.quality],"Frame rate target / "+preferences.targetFps,"Anti-aliasing / "+new[]{"Off","FXAA","SMAA"}[preferences.antialiasing],"Bloom / "+preferences.bloom.ToString("F2"),"Base field of view / "+preferences.fieldOfView.ToString("F0"),"Shadows / "+new[]{"Off","Hard","Soft"}[preferences.shadowQuality]}[index];
  if(settingsPage==2)return padNames[index]+" action   /   "+padNames[preferences.buttons[index]];
  string[] values={"Mouse sensitivity / "+preferences.mouseSensitivity.ToString("F1"),"Stick sensitivity / "+preferences.stickSensitivity.ToString("F1"),
   "Stick deadzone / "+preferences.deadzone.ToString("F2"),"Invert look X / "+OnOff(preferences.invertX),"Invert look Y / "+OnOff(preferences.invertY),
   "Aim assistance / "+OnOff(preferences.aimAssist),"Camera motion / "+OnOff(preferences.motion),"Controller vibration / "+OnOff(preferences.rumble),
   "Toggle interaction / "+OnOff(preferences.toggleInteract),"Toggle aim / "+OnOff(preferences.toggleAim),
   "Difficulty / "+new[]{"Explorer","Wayfarer","Veteran"}[preferences.difficulty],"Graphics / "+new[]{"Performance","Balanced","Ultra"}[preferences.quality],
   "Master volume / "+Mathf.RoundToInt(preferences.master*100)+"%","Music volume / "+Mathf.RoundToInt(preferences.music*100)+"%","Effects volume / "+Mathf.RoundToInt(preferences.effects*100)+"%",
   "Text scale / "+preferences.uiScale.ToString("F2"),"Frame rate target / "+preferences.targetFps,"High contrast UI / "+OnOff(preferences.highContrast)};
  return values[index];
 }
 static string OnOff(bool value){return value?"ON":"OFF";}
 void AdjustSetting(int row,int delta){
  if(settingsPage==1){rebindKey=row;rebindOptionsAt=-1;rebindReadyAt=Time.unscaledTime+.25f;return;}if(settingsPage==2){rebindAction=row;rebindOptionsAt=-1;rebindReadyAt=Time.unscaledTime+.25f;return;}
  if(settingsPage==3){switch(row){case 0:preferences.quality=(preferences.quality+delta+3)%3;break;case 1:preferences.targetFps=preferences.targetFps==30?60:preferences.targetFps==60?120:30;break;case 2:preferences.antialiasing=(preferences.antialiasing+delta+3)%3;break;case 3:preferences.bloom=Mathf.Clamp(preferences.bloom+delta*.05f,0,.8f);break;case 4:preferences.fieldOfView=Mathf.Clamp(preferences.fieldOfView+delta*5,55,90);break;case 5:preferences.shadowQuality=(preferences.shadowQuality+delta+3)%3;break;}ApplyPreferences();return;}
  switch(row){
   case 0:preferences.mouseSensitivity=Mathf.Clamp(preferences.mouseSensitivity+delta*.1f,.2f,3);break;
   case 1:preferences.stickSensitivity=Mathf.Clamp(preferences.stickSensitivity+delta*.1f,.2f,3);break;
   case 2:preferences.deadzone=Mathf.Clamp(preferences.deadzone+delta*.02f,.04f,.4f);break;
   case 3:preferences.invertX=!preferences.invertX;break;case 4:preferences.invertY=!preferences.invertY;break;
   case 5:preferences.aimAssist=!preferences.aimAssist;break;case 6:preferences.motion=!preferences.motion;break;case 7:preferences.rumble=!preferences.rumble;StopRumble();break;
   case 8:preferences.toggleInteract=!preferences.toggleInteract;interactionToggle=false;break;case 9:preferences.toggleAim=!preferences.toggleAim;aimToggle=false;break;
   case 10:preferences.difficulty=(preferences.difficulty+delta+3)%3;break;case 11:preferences.quality=(preferences.quality+delta+3)%3;break;
   case 12:preferences.master=Mathf.Clamp01(preferences.master+delta*.05f);break;case 13:preferences.music=Mathf.Clamp01(preferences.music+delta*.05f);break;
   case 14:preferences.effects=Mathf.Clamp01(preferences.effects+delta*.05f);break;case 15:preferences.uiScale=Mathf.Clamp(preferences.uiScale+delta*.05f,.85f,1.25f);break;
   case 16:preferences.targetFps=preferences.targetFps==30?60:preferences.targetFps==60?120:30;break;case 17:preferences.highContrast=!preferences.highContrast;break;
  }ApplyPreferences();
 }
 void DrawVoyagerSettings(){
  VText(new Rect(80,177,1320,53),"Make the expedition feel right.",VoyagerFont.Heading);
  string[] pages={"FEEL & AUDIO","KEYBOARD ACTIONS","CONTROLLER ACTIONS","GRAPHICS & CAMERA"};
  for(int i=0;i<4;i++)if(VButton(new Rect(80+i*368,243,352,50),pages[i],settingsPage==i)){settingsPage=i;voyagerSelection=0;}
  int count=SettingsRows();for(int i=0;i<count;i++){
   int col=i%2,row=i/2;Rect rect=new Rect(80+col*750,322+row*62,727,51);
   if(VButton(rect,SettingLabel(i),voyagerSelection==i,settingsPage!=2)){voyagerSelection=i;AdjustSetting(i,1);}
  }
  if(settingsPage==2)DrawControllerDiagnostics();
  if(VButton(new Rect(80,887,710,43),"FACE FLIGHT / CAMERA SETUP (F7)"))OpenVoyager(VoyagerPanel.HeadPilot);
  VText(new Rect(825,894,690,33),"Left / right adjusts • Cross selects • Triangle pages • Square resets",VoyagerFont.Small);
 }
 void DrawControllerDiagnostics(){
  VBox(new Rect(80,764,1436,112),new Color(.018f,.047f,.069f));
  VText(new Rect(96,772,1400,26),Controls.Connected?"LIVE CONTROLLER / "+Controls.DeviceName+" / "+Controls.DeviceLayout:"NO CONTROLLER / Connect by USB or pair DualSense in macOS Bluetooth settings.",VoyagerFont.Small,Controls.Connected?vCyan:vGold,false);
  string raw="Left stick "+Controls.RawMove.ToString("F2")+" • Right stick "+Controls.RawLook.ToString("F2")+" • L2 "+Controls.RawL2.ToString("F2")+" • R2 "+Controls.RawR2.ToString("F2");
  VText(new Rect(96,801,1400,27),raw,VoyagerFont.Small,vWhite,false);
  string buttons="";for(int i=0;i<14;i++)if(Controls.PhysicalHeld(i))buttons+=(buttons.Length>0?", ":"")+padNames[i];
  VText(new Rect(96,832,1400,36),"Buttons: "+(buttons.Length==0?"none":buttons)+(Controls.WaitingForRelease?" • Release held controls to use those controls again.":" • Touchpad opens atlas. PS and microphone buttons have no gameplay action."),VoyagerFont.Small,vMuted,false);
 }
 void DrawRebinding(){
  VBox(new Rect(0,0,1600,1000),new Color(0,.01f,.025f,.96f));
  VText(new Rect(280,365,1080,70),rebindKey>=0?"PRESS A KEY":"PRESS A CONTROLLER BUTTON",VoyagerFont.Hero);
  VText(new Rect(285,473,1030,125),rebindKey>=0?"Options, Circle or Escape cancels. Press a keyboard key to assign it.":"Press a button to assign it. Hold Options to cancel, or press Escape. Conflicting assignments are swapped. Triggers retain their analog thrust / brake and fire / aim roles.",VoyagerFont.Body,vMuted,rebindKey>=0);
  if(Time.unscaledTime<rebindReadyAt)return;
  if(Event.current.type==EventType.KeyDown&&Event.current.keyCode!=KeyCode.None){
   if(Event.current.keyCode==KeyCode.Escape){CancelRebinding();Event.current.Use();return;}
   if(rebindKey>=0){preferences.BindKey(remappableKeys[rebindKey],Event.current.keyCode);rebindKey=-1;Event.current.Use();ApplyPreferences();}
  }
 }
 void DrawVoyagerContracts(){
  var board=EnsureContracts();VText(new Rect(80,182,1400,52),"FRONTIER / CONTRACT NETWORK",VoyagerFont.Heading);
  VText(new Rect(84,242,1400,52),"Three active contracts maximum. Rewards change faction relations and fund your civilization.",VoyagerFont.Small);
  int selected=Mathf.Clamp(voyagerSelection,0,Mathf.Max(0,board.Count-1));int start=Mathf.Max(0,selected-7);
  for(int i=start;i<Mathf.Min(board.Count,start+8);i++){var c=board[i];if(VButton(new Rect(80,320+(i-start)*67,790,58),(c.accepted?"ACTIVE / ":"")+c.title,selected==i))voyagerSelection=i;}
  if(board.Count==0)return;var current=board[selected];
  VText(new Rect(924,331,590,95),current.title,VoyagerFont.Heading);
  VText(new Rect(928,448,575,174),current.Instruction+"\n\n"+current.why,VoyagerFont.Body,vMuted);
  VText(new Rect(928,632,570,112),ports[current.port].name+"\n"+civilization.factions[current.faction].name+"\nREWARD / "+current.reward+" alloy / "+current.science+" science",VoyagerFont.Body,vCyan);
  if(VButton(new Rect(925,784,585,58),current.accepted?"CROSS / TRACK & SET COURSE":"CROSS / ACCEPT CONTRACT",true))VoyagerPrimary();
  if(current.accepted&&VButton(new Rect(925,856,585,49),"SQUARE / ABANDON CONTRACT"))VoyagerSecondary();
 }
 void DrawVoyagerOutfitter(){
  VText(new Rect(80,180,1400,54),"YOUR SHIP / YOUR WAY TO PLAY",VoyagerFont.Heading);
  string[] categories={"PROPULSION","WEAPON","DEFENSE","PAYLOAD"};for(int i=0;i<4;i++)if(VButton(new Rect(80+i*365,249,350,51),categories[i],moduleCategory==i)){moduleCategory=i;voyagerSelection=voyager.Module(i);}
  for(int i=0;i<3;i++){int index=moduleCategory*3+i;bool owned=i==0||(voyager.unlockedModules&(1<<index))!=0;if(VButton(new Rect(80,350+i*130,760,112),VoyagerState.ModuleNames[index]+"\n"+(voyager.Module(moduleCategory)==i?"INSTALLED":owned?"OWNED / EQUIP":VoyagerState.ModuleCost[index]+" ALLOY / "+(i==2?25:15)+" SCIENCE"),voyagerSelection==i))voyagerSelection=i;}
  VText(new Rect(85,790,740,100),"REACTOR / "+voyager.Draw+" / "+voyager.Reactor(civilization.blueprint)+" power\nCARGO / "+voyager.cargo+" / "+voyager.Capacity(civilization.blueprint)+" capacity",VoyagerFont.Body,vCyan);
  if(civilizationScene.PreviewTexture!=null){civilizationScene.FramePreview(4f/3);GUI.DrawTexture(new Rect(910,321,600,362),civilizationScene.PreviewTexture,ScaleMode.ScaleToFit,false);}
  int selection=moduleCategory*3+Mathf.Clamp(voyagerSelection,0,2);
  VText(new Rect(925,709,580,115),VoyagerState.ModuleDescriptions[selection],VoyagerFont.Body,vMuted);
  if(VButton(new Rect(920,840,590,59),"CROSS / INSTALL MODULE",true))VoyagerPrimary();
 }
 void DrawVoyagerColonies(){
  VText(new Rect(80,180,1400,54),"COLONIES / PEOPLE, SUPPLY & DIPLOMACY",VoyagerFont.Heading);
  int start=Mathf.Max(0,voyagerSelection-4);
  for(int i=start;i<Mathf.Min(civilization.colonies.Count,start+5);i++){var colony=civilization.colonies[i];var p=voyager.Policy(colony.port);if(VButton(new Rect(80,277+(i-start)*84,825,75),colony.name+" / "+new[]{"Balanced","Industry","Research","Provisioning"}[p.focus]+"\nFood "+p.food.ToString("F0")+" / morale "+p.morale.ToString("F0")+"% / population "+colony.population.ToString("F0"),voyagerSelection==i)){voyagerSelection=i;p.focus=(p.focus+1)%4;SaveCampaign();}}
  if(civilization.colonies.Count==0)VText(new Rect(85,302,800,120),"Land, disembark and place your first command spire through B / Civilization Command.",VoyagerFont.Body,vMuted);
  VText(new Rect(953,283,540,57),"THE FRONTIER POWERS",VoyagerFont.Heading);
  for(int i=0;i<3;i++){var a=voyager.agendas[i];var f=civilization.factions[i];VText(new Rect(955,370+i*113,545,105),f.name+"\nRelations "+f.relation+" / "+a.worlds.Count+" worlds\nIndustry "+a.industry+" / research "+a.technology,VoyagerFont.Body,f.relation>=70?vCyan:vMuted);}
  VText(new Rect(85,740,820,66),"Left / right changes colony focus. Provisioning produces food; industry and research specialize output. Supply routes feed other worlds.",VoyagerFont.Small);
  if(VButton(new Rect(80,827,825,60),"TRIANGLE / CREATE OR TOGGLE SUPPLY ROUTE"))CreateSupplyRoute();
  if(VButton(new Rect(953,763,550,52),"SQUARE / NEGOTIATE CEASEFIRE"))NegotiatePeace();
  if(!voyager.crisis.active&&civilization.colonies.Count>0&&VButton(new Rect(953,865,550,45),"CROSS / DONATE 15 RATIONS ("+voyager.food.ToString("F0")+")"))VoyagerPrimary();
  if(voyager.crisis.active){VText(new Rect(955,825,545,35),voyager.crisis.title+" / "+voyager.crisis.remaining.ToString("F0")+"s",VoyagerFont.Small,vGold);if(VButton(new Rect(953,865,550,45),"CROSS / FUND CRISIS RELIEF",true))VoyagerPrimary();}
 }
 void DrawVoyagerChapters(){
  VText(new Rect(80,176,1400,53),"TWELVE CHAPTERS / A CIVILIZATION'S STORY",VoyagerFont.Heading);
  for(int i=0;i<12;i++){bool current=i==voyager.chapter;Color color=i<voyager.chapter||voyager.campaignComplete?vCyan:current?vWhite:vMuted;
   VBox(new Rect(80,263+i*48,760,41),current?new Color(.03f,.18f,.20f):new Color(.017f,.04f,.065f));
   VText(new Rect(95,267+i*48,725,38),(i+1).ToString("D2")+" / "+CampaignChapters.Titles[i]+(i<voyager.chapter||voyager.campaignComplete?" / COMPLETE":current?" / CURRENT":""),VoyagerFont.Body,color);
  }
  VText(new Rect(914,280,600,98),CampaignChapters.Titles[voyager.chapter],VoyagerFont.Heading);
  VText(new Rect(919,394,577,147),CampaignChapters.Stories[voyager.chapter],VoyagerFont.Body,vMuted);
  VText(new Rect(919,562,577,135),CampaignChapters.Objectives[voyager.chapter],VoyagerFont.Body);
  VBar(new Rect(920,727,575,9),CampaignChapters.Progress(voyager.chapter,voyager,civilization),vCyan);
  VText(new Rect(920,767,580,76),"LEGACIES / "+(voyager.victories.Count==0?"Your story is still being written.":string.Join(", ",voyager.victories)),VoyagerFont.Small,vGold);
  if(voyager.chapter==11&&campaignStarted&&VButton(new Rect(916,855,590,55),"TRIANGLE / LAUNCH FINAL ASSAULT",true))BeginFlagshipAssault();
 }
 void DrawVoyagerDefeat(){
  VText(new Rect(220,285,1200,90),"THE EXPEDITION IS NOT OVER.",VoyagerFont.Hero);
  VText(new Rect(230,410,1130,123),outcome+"\nYour last checkpoint preserves your civilization and expedition. Return, prepare, and try again.",VoyagerFont.Body,vMuted);
  if(VButton(new Rect(230,601,1130,76),"CROSS / RESTORE LAST CHECKPOINT",voyagerSelection==0)){voyagerSelection=0;VoyagerPrimary();}
  if(VButton(new Rect(230,704,1130,65),"RETURN TO CAMPAIGN SLOTS",voyagerSelection==1)){voyagerSelection=1;VoyagerPrimary();}
 }
 string JourneyInstruction(){
  if(voyager.chapter==0){if(!voyager.landed)return "VTOL approach / descend to Pelagos. K / D-pad up toggles assistance.";if(!voyager.tires)return "Park, then deploy tires: 6 / D-pad down.";if(!voyager.walked)return "Leave the rover: "+preferences.Key(KeyCode.J)+" / Triangle.";return "Follow RECORDER. Hold "+preferences.Key(KeyCode.E)+" / R1 to recover it.";}
  if(voyager.chapter==1){if(surface.Current!=null){if(surface.Current.scans<3)return "Follow the specimen markers. Hold "+preferences.Key(KeyCode.E)+" / R1 to scan: "+surface.Current.scans+" / 3.";if(!surface.Current.relay)return "Restart the relay, then stay close and defend it.";if(!surface.Current.core)return "Defeat the Warden. Aim at its glowing organs, then recover the core.";}return "Return to your parked rover and board with "+preferences.Key(KeyCode.J)+" / Triangle to extract.";}
  if(voyager.chapter==2)return "B / D-pad down: City Planner. Build a command spire, foundry, solar farm and observatory.";
  if(voyager.chapter==3)return "City Planner > Launch complex. G / Triangle assembles; L / Square launches.";
  if(voyager.chapter==4&&RoverDeployed)return "Board, stow tires, then open Options > Ship Outfitter to fit a module.";
  return CampaignChapters.Objectives[voyager.chapter];
 }
 void VMarker(Vector3 world,string label,Color color){
  Vector3 p=cam.WorldToScreenPoint(world);if(p.z<=0)return;float x=p.x/Screen.width*1600,y=1000-p.y/Screen.height*1000;
  if(x<40||x>1560||y<120||y>820)return;VBox(new Rect(x-4,y-4,8,8),color);VText(new Rect(x-160,y+14,350,52),label+" / "+Range(Vector3.Distance(GroundActive?expedition.Focus:Pos(player),world)),VoyagerFont.Small,color);
 }
 bool DrawVoyagerHUD(){
  if(voyager==null||voyagerPanel!=VoyagerPanel.None||civilOpen||chartOpen||upgradeOpen||inTidal)return false;
  VStyles();VMatrix();if(voyagerRecord&&civilFilmCamera){VBox(new Rect(0,0,1600,100),new Color(.005f,.018f,.032f,.97f));VText(new Rect(45,20,1450,65),"TWELVE HORIZONS / "+voyagerFilmPhase,VoyagerFont.Heading);VBox(new Rect(0,947,1600,53),new Color(.005f,.018f,.032f,.97f));VText(new Rect(45,961,1480,30),"RECORDED UNITY GAMEPLAY / SEEDED RESOURCES / SCRIPTED INPUT AND CAMERAS",VoyagerFont.Small,vGold);DrawCivilWar();return true;}VBox(new Rect(35,26,1530,75),new Color(.01f,.028f,.046f,.95f));
  VText(new Rect(57,36,900,37),"CH "+(voyager.chapter+1).ToString("D2")+" / "+CampaignChapters.Titles[voyager.chapter].ToUpperInvariant(),VoyagerFont.Body,vCyan);
  VText(new Rect(60,75,900,23),expedition.PlanetName+" / "+(GroundActive?"SURFACE EXPEDITION":RoverDeployed?"ROVER":"FLIGHT"),VoyagerFont.Small);
  VText(new Rect(1040,39,505,47),"A "+civilization.alloy.ToString("F0")+"   E "+civilization.energy.ToString("F0")+"   S "+civilization.science.ToString("F0"),VoyagerFont.Body);
  VBar(new Rect(39,101,1522,3),CampaignChapters.Progress(voyager.chapter,voyager,civilization),vCyan);
  VBox(new Rect(1020,130,540,143),new Color(.012f,.034f,.055f,.94f));
  VText(new Rect(1040,143,495,32),"CURRENT CHAPTER",VoyagerFont.Small,vGold);
  VText(new Rect(1040,182,490,86),JourneyInstruction(),VoyagerFont.Body);
  if(surface!=null&&GroundActive)surface.Draw(false);
  if(GroundActive)VMarker(Pos(player),"YOUR SHIP / "+preferences.Key(KeyCode.J)+" / TRIANGLE",vCyan);
  DrawContractMarkers();DrawCivilWar();
  VBox(new Rect(36,805,480,123),new Color(.01f,.028f,.046f,.95f));
  VText(new Rect(56,814,440,32),(GroundActive?"SUIT":"HULL")+" "+Mathf.CeilToInt(health)+" / "+HullMaximum.ToString("F0")+"    SHIELD "+voyager.shield.ToString("F0"),VoyagerFont.Body);
  VBar(new Rect(58,857,433,6),health/HullMaximum,vCyan);VBar(new Rect(58,873,433,5),voyager.shield/voyager.ShieldMax,new Color(.36f,.63f,1));
  VText(new Rect(58,892,435,31),"HEAT "+voyager.heat.ToString("F0")+"% / CARGO "+voyager.cargo+" / "+voyager.Capacity(civilization.blueprint),VoyagerFont.Small,voyager.heat>80?vGold:vMuted);
  VText(new Rect(1070,830,490,80),(GroundActive?expedition.Speed:Vel(player).magnitude).ToString("F1")+" m/s\n"+(cruising?"CRUISE / "+destinationName+" / "+Range(routeDistance):landingAssist?"VTOL DESCENT":GroundActive?expedition.Sprinting?"SPRINT":"ON FOOT":RoverDeployed?"TIRES DEPLOYED":"MANUAL FLIGHT"),VoyagerFont.Body,vCyan);
  VText(new Rect(45,952,1510,35),GroundActive?"L3 run / Cross hop / R2 fire / R1 interact / Triangle board / D-pad down civilization / Options menu":RoverDeployed?"R2 drive / L2 reverse / left stick steer / Triangle exit / D-pad down tires / L3 civilization / Options menu":"R2 thrust / L2 brake / Square fire / Share atlas / L3 civilization / Options menu / Tab contracts",VoyagerFont.Small);
  if(!PadMode)VText(new Rect(45,923,1500,31),preferences.Key(KeyCode.B)+" civilization / "+preferences.Key(KeyCode.Tab)+" contracts / "+preferences.Key(KeyCode.J)+" board / "+preferences.Key(KeyCode.K)+" land / F5 checkpoint / Esc menu",VoyagerFont.Small);
  if(!GroundActive&&expedition.Alt(Pos(player))<1.15f&&Vel(player).magnitude<1.5f){if(VButton(new Rect(549,820,430,43),"6 / D-PAD DOWN: "+(RoverDeployed?"STOW TIRES":"DEPLOY TIRES")))ToggleWheels();if(VButton(new Rect(549,872,430,43),"J / TRIANGLE: DISEMBARK"))GroundBoard();}
  if(toastTime>0){VBox(new Rect(400,714,1160,64),new Color(.02f,.08f,.10f,.96f));VText(new Rect(420,725,1110,52),voyagerToast,VoyagerFont.Body,vCyan);}
  if(chapterBanner>0){VBox(new Rect(40,349,820,128),new Color(.007f,.023f,.04f,.95f));VText(new Rect(60,361,775,28),"CHAPTER "+(voyager.chapter+1).ToString("D2"),VoyagerFont.Small,vGold);VText(new Rect(60,405,775,58),CampaignChapters.Titles[voyager.chapter],VoyagerFont.Heading);}
  if(voyager.policies.Exists(p=>p.food<15))VText(new Rect(505,194,485,55),"LOW PROVISIONS / use colony policies and supply routes",VoyagerFont.Small,vGold);
  if(voyager.crisis.active)VText(new Rect(505,124,485,65),voyager.crisis.title+" / "+voyager.crisis.remaining.ToString("F0")+"s / relief in colony command",VoyagerFont.Small,vGold);
  if(telemetry&&frameTimes.Count>20)VText(new Rect(560,851,485,42),"FRAME "+frameTimes[frameTimes.Count-1].ToString("F1")+" ms / "+(1000/frameTimes[frameTimes.Count-1]).ToString("F0")+" FPS",VoyagerFont.Small);
  return true;
 }
}
