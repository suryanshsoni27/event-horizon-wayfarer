using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class VoyagerSettings {
 public int version=2,quality=1,targetFps=60,difficulty=1,antialiasing=2,shadowQuality=2;
 public float bloom=.25f,fieldOfView=65;
 public float mouseSensitivity=1,stickSensitivity=1,deadzone=.16f,master=.8f,music=.35f,effects=.8f,uiScale=1;
 public bool invertX,invertY,rumble=true,aimAssist=true,motion=true,highContrast,toggleInteract,toggleAim;
 public int[] buttons={0,1,2,3,4,5,6,7,8,9,10,11,12,13};
 public List<KeyBinding> keys=new List<KeyBinding>();
 [Serializable] public sealed class KeyBinding {public int original,replacement;}
 public KeyCode Key(KeyCode original){var b=keys.Find(x=>x.original==(int)original);return b==null?original:(KeyCode)b.replacement;}
 public void BindKey(KeyCode original,KeyCode replacement){
  KeyCode previous=Key(original);
  foreach(KeyCode candidate in Enum.GetValues(typeof(KeyCode)))if(candidate!=original&&Key(candidate)==replacement){var conflict=keys.Find(x=>x.original==(int)candidate);if(conflict==null)keys.Add(new KeyBinding{original=(int)candidate,replacement=(int)previous});else conflict.replacement=(int)previous;}
  var binding=keys.Find(x=>x.original==(int)original);if(binding==null){binding=new KeyBinding{original=(int)original};keys.Add(binding);}binding.replacement=(int)replacement;
 }
 public void BindButton(int action,int physical){if(action<0||action>=14||physical<0||physical>=14)return;int old=buttons[action],other=Array.IndexOf(buttons,physical);buttons[action]=physical;if(other>=0&&other!=action)buttons[other]=old;}
 public Vector2 Filter(Vector2 raw,bool looking){
  float n=raw.magnitude;Vector2 v=n<=deadzone?Vector2.zero:raw/n*Mathf.Clamp01((n-deadzone)/(1-deadzone));
  if(looking){v*=stickSensitivity;if(invertX)v.x=-v.x;if(invertY)v.y=-v.y;}return v;
 }
 public bool UpgradeAndValidate(){if(version==1){antialiasing=2;shadowQuality=2;bloom=.25f;fieldOfView=65;version=2;}return Validate();}
 public bool Validate(){
  if(antialiasing<0||antialiasing>2||shadowQuality<0||shadowQuality>2||!VoyagerState.Finite(bloom)||bloom<0||bloom>.8f||!VoyagerState.Finite(fieldOfView)||fieldOfView<55||fieldOfView>90)return false;
  if(version!=2||quality<0||quality>2||difficulty<0||difficulty>2||buttons==null||buttons.Length!=14||keys==null||keys.Count>128)return false;
  if(deadzone<.03f||deadzone>.4f||mouseSensitivity<.2f||mouseSensitivity>3||stickSensitivity<.2f||stickSensitivity>3||uiScale<.85f||uiScale>1.25f)return false;
  foreach(float v in new[]{deadzone,mouseSensitivity,stickSensitivity,master,music,effects,uiScale})if(!VoyagerState.Finite(v))return false;
  if(master<0||master>1||music<0||music>1||effects<0||effects>1)return false;
  var seen=new HashSet<int>();foreach(int b in buttons)if(b<0||b>13||!seen.Add(b))return false;
  foreach(var b in keys)if(b==null||!Enum.IsDefined(typeof(KeyCode),b.original)||!Enum.IsDefined(typeof(KeyCode),b.replacement))return false;
  return targetFps==30||targetFps==60||targetFps==120;
 }
}
