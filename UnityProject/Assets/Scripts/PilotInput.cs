using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// A single sampled frame feeds flight, boots, rover and menus. No joystick-number assumptions.
public sealed class PilotInput {
 public enum Button { Cross, Circle, Square, Triangle, L1, R1, L3, R3, Options, Share, Up, Down, Left, Right }
 public bool Automated,Disconnected,Connected,UsingPad;
 public Gamepad TestDevice;
 public Vector2 Move,Look;public float L2,R2;
 public string DeviceName="No controller connected";
 Gamepad device;uint held,pressed,previous;bool neutralGate;
 public bool Down(Button b){return (pressed&(1u<<(int)b))!=0;}
 public bool Held(Button b){return (held&(1u<<(int)b))!=0;}
 public static Vector2 Deadzone(Vector2 v){float n=v.magnitude;return n<=.16f?Vector2.zero:v/n*Mathf.Clamp01((n-.16f)/.84f);}
 static bool On(ButtonControl b){return b!=null&&b.isPressed;}
 public void SuppressUntilNeutral(){neutralGate=true;pressed=0;held=0;Move=Look=Vector2.zero;L2=R2=0;}
 public void Poll(){
  Gamepad next=TestDevice!=null?TestDevice:Automated?null:Gamepad.current;
  if(next!=null&&(!next.added||!next.enabled))next=null;
  Disconnected=device!=null&&next==null;
  if(next!=device){device=next;previous=0;if(next!=null)neutralGate=true;}
  Connected=device!=null;pressed=held=0;Move=Look=Vector2.zero;L2=R2=0;
  if(!Connected){previous=0;DeviceName="No controller connected";return;}
  DeviceName=device.displayName;
  Move=Deadzone(device.leftStick.ReadUnprocessedValue());Look=Deadzone(device.rightStick.ReadUnprocessedValue());L2=Mathf.InverseLerp(.07f,1,device.leftTrigger.ReadValue());R2=Mathf.InverseLerp(.07f,1,device.rightTrigger.ReadValue());
  ButtonControl[] controls={device.buttonSouth,device.buttonEast,device.buttonWest,device.buttonNorth,device.leftShoulder,device.rightShoulder,device.leftStickButton,device.rightStickButton,device.startButton,device.selectButton,device.dpad.up,device.dpad.down,device.dpad.left,device.dpad.right};
  for(int i=0;i<controls.Length;i++)if(On(controls[i]))held|=1u<<i;
  if(neutralGate){bool neutral=held==0&&Move==Vector2.zero&&Look==Vector2.zero&&L2==0&&R2==0;previous=held;held=0;Move=Look=Vector2.zero;L2=R2=0;if(neutral){neutralGate=false;previous=0;}return;}
  pressed=held&~previous;previous=held;
  if(held!=0||Move.sqrMagnitude>0||Look.sqrMagnitude>0||L2>0||R2>0)UsingPad=true;
  if(!Automated&&((Keyboard.current!=null&&Keyboard.current.anyKey.wasPressedThisFrame)||(Mouse.current!=null&&Mouse.current.delta.ReadValue().sqrMagnitude>.1f)))UsingPad=false;
 }
}
