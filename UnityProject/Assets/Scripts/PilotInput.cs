using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;

// A single sampled frame feeds flight, boots, rover and menus. No joystick-number assumptions.
public sealed class PilotInput {
 public enum Button { Cross, Circle, Square, Triangle, L1, R1, L3, R3, Options, Share, Up, Down, Left, Right }
 public bool Automated,Disconnected,Connected,UsingPad;
 public Gamepad TestDevice;
 public VoyagerSettings Settings;
 public Vector2 Move,Look,RawMove,RawLook;
 public float L2,R2,RawL2,RawR2;
 public bool ThrottleReleased,TouchpadDown;
 public Vector2Int Navigation{get;private set;}
 public string DeviceName="No controller connected",DeviceLayout="";
 public bool WaitingForRelease{get{return blockedButtons!=0||blockMove||blockLook||blockL2||blockR2||blockTouchpad;}}
 public Gamepad Device{get{return device;}}
 Gamepad device;
 uint held,pressed,previous,physicalHeld,physicalPressed,physicalPrevious,logicalSample,blockedButtons;
 bool blockMove,blockLook,blockL2,blockR2,blockTouchpad,touchHeld;
 Vector2 sampledMove,sampledLook;
 float sampledL2,sampledR2,nextNavigation;
 Vector2Int navigationHeld;
 public bool Down(Button b){return (pressed&(1u<<(int)b))!=0;}
 public bool Held(Button b){return (held&(1u<<(int)b))!=0;}
 public bool PhysicalHeld(int i){return i>=0&&i<14&&(physicalHeld&(1u<<i))!=0;}
 public static Vector2 Deadzone(Vector2 v){float n=v.magnitude;return n<=.16f?Vector2.zero:v/n*Mathf.Clamp01((n-.16f)/.84f);}
 static bool On(ButtonControl b){return b!=null&&b.isPressed;}
 void BlockSample(){
  blockedButtons|=physicalHeld;blockMove|=sampledMove!=Vector2.zero;blockLook|=sampledLook!=Vector2.zero;
  blockL2|=sampledL2>0;blockR2|=sampledR2>0;blockTouchpad|=touchHeld;
 }
 // Held controls wait for their own release. One deflected stick cannot block every button.
 public void SuppressUntilNeutral(){
  BlockSample();pressed=held=previous=0;Move=Look=Vector2.zero;L2=R2=0;TouchpadDown=ThrottleReleased=false;
  Navigation=navigationHeld=Vector2Int.zero;nextNavigation=0;
 }
 Gamepad ChooseDevice(){
  if(TestDevice!=null)return TestDevice;if(Automated)return null;
  var current=Gamepad.current;if(current!=null&&current.added&&current.enabled)return current;
  foreach(var candidate in Gamepad.all)if(candidate.added&&candidate.enabled)return candidate;
  return null;
 }
 public void Poll(float now=-1){
  if(now<0)now=Time.unscaledTime;
  Gamepad next=ChooseDevice();if(next!=null&&(!next.added||!next.enabled))next=null;
  bool changed=next!=device;Disconnected=device!=null&&next==null;float lastR2=R2;
  if(changed){
   device=next;previous=physicalPrevious=blockedButtons=0;blockMove=blockLook=blockL2=blockR2=blockTouchpad=touchHeld=false;
   logicalSample=0;sampledMove=sampledLook=Vector2.zero;sampledL2=sampledR2=0;navigationHeld=Vector2Int.zero;nextNavigation=0;
   if(!Automated)Debug.Log("CONTROLLER_DEVICE "+(device==null?"disconnected":device.displayName+" / "+device.layout));
  }
  Connected=device!=null;pressed=held=physicalPressed=physicalHeld=0;Move=Look=RawMove=RawLook=Vector2.zero;L2=R2=RawL2=RawR2=0;Navigation=Vector2Int.zero;TouchpadDown=ThrottleReleased=false;
  if(!Connected){previous=0;UsingPad=false;DeviceName="No controller connected";DeviceLayout="";return;}
  DeviceName=device.displayName;DeviceLayout=device.layout;
  RawMove=device.leftStick.ReadUnprocessedValue();RawLook=device.rightStick.ReadUnprocessedValue();RawL2=device.leftTrigger.ReadValue();RawR2=device.rightTrigger.ReadValue();
  sampledMove=Settings==null?Deadzone(RawMove):Settings.Filter(RawMove,false);sampledLook=Settings==null?Deadzone(RawLook):Settings.Filter(RawLook,true);
  sampledL2=Mathf.InverseLerp(.07f,1,RawL2);sampledR2=Mathf.InverseLerp(.07f,1,RawR2);
  for(int i=0;i<14;i++)if(On(Physical(i)))physicalHeld|=1u<<i;
  physicalPressed=physicalHeld&~physicalPrevious;physicalPrevious=physicalHeld;logicalSample=0;
  for(int i=0;i<14;i++)if(PhysicalHeld(Settings==null?i:Settings.buttons[i]))logicalSample|=1u<<i;
  bool previousTouch=touchHeld;var dual=device as DualShockGamepad;touchHeld=dual!=null&&On(dual.touchpadButton);
  if(changed){BlockSample();physicalPressed=0;}
  // Gate physical buttons so changing a mapping while its button is held cannot create a new press.
  blockedButtons&=physicalHeld;uint available=physicalHeld&~blockedButtons;held=0;
  for(int i=0;i<14;i++)if((available&(1u<<(Settings==null?i:Settings.buttons[i])))!=0)held|=1u<<i;
  pressed=held&~previous;previous=held;
  if(sampledMove==Vector2.zero)blockMove=false;if(sampledLook==Vector2.zero)blockLook=false;
  if(sampledL2==0)blockL2=false;if(sampledR2==0)blockR2=false;if(!touchHeld)blockTouchpad=false;
  Move=blockMove?Vector2.zero:sampledMove;Look=blockLook?Vector2.zero:sampledLook;L2=blockL2?0:sampledL2;R2=blockR2?0:sampledR2;
  TouchpadDown=touchHeld&&!previousTouch&&!blockTouchpad;ThrottleReleased=lastR2>0&&R2==0;
  bool padActivity=logicalSample!=0||sampledMove!=Vector2.zero||sampledLook!=Vector2.zero||sampledL2>0||sampledR2>0||touchHeld||ThrottleReleased;
  if(padActivity)UsingPad=true;
  else if(!Automated&&((Keyboard.current!=null&&Keyboard.current.anyKey.wasPressedThisFrame)||(Mouse.current!=null&&Mouse.current.delta.ReadValue().sqrMagnitude>.1f)))UsingPad=false;
  SampleNavigation(now);
 }
 void SampleNavigation(float now){
  Vector2 value=new Vector2((Held(Button.Right)?1:0)-(Held(Button.Left)?1:0),(Held(Button.Up)?1:0)-(Held(Button.Down)?1:0));
  if(value==Vector2.zero){value=Move;float threshold=navigationHeld==Vector2Int.zero?.55f:.3f;if(value.magnitude<threshold)value=Vector2.zero;}
  Vector2Int direction=value==Vector2.zero?Vector2Int.zero:Mathf.Abs(value.y)>=Mathf.Abs(value.x)?new Vector2Int(0,value.y>0?1:-1):new Vector2Int(value.x>0?1:-1,0);
  if(direction!=navigationHeld){navigationHeld=direction;Navigation=direction;nextNavigation=now+.38f;}
  else if(direction!=Vector2Int.zero&&now>=nextNavigation){Navigation=direction;nextNavigation=now+.11f;}
 }
 public ButtonControl Physical(int i){
  if(device==null)return null;
  switch(i){case 0:return device.buttonSouth;case 1:return device.buttonEast;case 2:return device.buttonWest;case 3:return device.buttonNorth;
   case 4:return device.leftShoulder;case 5:return device.rightShoulder;case 6:return device.leftStickButton;case 7:return device.rightStickButton;
   case 8:return device.startButton;case 9:return device.selectButton;case 10:return device.dpad.up;case 11:return device.dpad.down;case 12:return device.dpad.left;default:return device.dpad.right;}
 }
 public int PhysicalPressed(){for(int i=0;i<14;i++)if((physicalPressed&(1u<<i))!=0)return i;return -1;}
}
