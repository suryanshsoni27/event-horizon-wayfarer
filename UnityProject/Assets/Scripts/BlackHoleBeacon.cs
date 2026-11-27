using UnityEngine;

// An angular distant proxy keeps an edge-on disk discoverable. Native position/size are unchanged.
public sealed class BlackHoleBeacon : MonoBehaviour {
 Transform image;Material material;MaterialPropertyBlock block=new MaterialPropertyBlock();
 public void Initialize(float rs){var core=transform.Find("Event shadow / distant approximation");if(core!=null)core.GetComponent<Renderer>().enabled=false;var q=GameObject.CreatePrimitive(PrimitiveType.Quad);Destroy(q.GetComponent<Collider>());q.name="Distant lensing proxy / switches to numerical rays nearby";image=q.transform;image.SetParent(transform,false);image.localScale=Vector3.one*rs*18;material=new Material(Shader.Find("Orbit/BlackHoleDistant"));q.GetComponent<Renderer>().sharedMaterial=material;q.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
 public void Refresh(Camera cam){if(image==null||cam==null)return;Vector3 view=(cam.transform.position-transform.position).normalized;Vector3 right=Vector3.Cross(Vector3.up,view);if(right.sqrMagnitude<.001f)right=Vector3.right;Vector3 up=Vector3.Cross(view,right).normalized;image.rotation=Quaternion.LookRotation(-view,up);block.SetFloat("_Inclination",Mathf.Abs(Vector3.Dot(view,Vector3.up)));image.GetComponent<Renderer>().SetPropertyBlock(block);}
 void OnDestroy(){if(material!=null)Destroy(material);}
}
