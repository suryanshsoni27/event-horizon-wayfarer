using UnityEngine;

// Two-bone analytical IK shared by suit limbs and alien legs. Rest axes point down.
public static class JointMotion {
 public static void Solve(Transform basis,Transform upper,Transform lower,Transform end,Vector3 target,Vector3 pole,float a,float b){
  Vector3 h=upper.position,d=target-h;float scale=basis.lossyScale.y;a*=scale;b*=scale;float length=Mathf.Clamp(d.magnitude,Mathf.Abs(a-b)+.002f,a+b-.002f);d=d.sqrMagnitude>.000001f?d.normalized:-basis.up;Vector3 bend=Vector3.ProjectOnPlane(pole,d);if(bend.sqrMagnitude<.0001f)bend=Vector3.ProjectOnPlane(basis.right,d);bend.Normalize();float x=(a*a+length*length-b*b)/(2*length),y=Mathf.Sqrt(Mathf.Max(0,a*a-x*x));Vector3 k=h+d*x+bend*y,e=h+d*length;
  upper.rotation=Quaternion.FromToRotation(-basis.up,(k-h).normalized)*basis.rotation;lower.position=k;lower.rotation=Quaternion.FromToRotation(-basis.up,(e-k).normalized)*basis.rotation;end.position=e;end.rotation=basis.rotation;
 }
 public static Transform Joint(string name,Transform parent,Vector3 local){var t=new GameObject(name).transform;t.SetParent(parent,false);t.gameObject.layer=parent==null?0:parent.gameObject.layer;t.localPosition=local;return t;}
 public static Transform Piece(string name,Transform parent,PrimitiveType type,Vector3 p,Vector3 size,Material mat){var obj=GameObject.CreatePrimitive(type);Object.Destroy(obj.GetComponent<Collider>());obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=p;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=mat;obj.layer=parent.gameObject.layer;return obj.transform;}
 public static Material Material(Color c,float metallic=.1f,float smooth=.35f){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);return m;}
}
