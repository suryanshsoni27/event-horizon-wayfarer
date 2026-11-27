using UnityEngine;

public static class ProductionMaterials {
 public static Material Create(Color color,float metal,float smooth=.5f,float windows=0,float emission=0,bool fabric=false){
  var m=new Material(Shader.Find("Orbit/FrontierArchitecture"));m.SetColor("_BaseColor",color);m.SetColor("_Accent",windows>0?new Color(.5f,.8f,1):color);
  m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Glass",windows);m.SetFloat("_Glow",emission);m.SetFloat("_Fabric",fabric?1:0);
  m.SetTexture("_DetailMap",Resources.Load<Texture2D>("Surfaces/hull_diff"));m.SetTexture("_NormalMap",Resources.Load<Texture2D>(fabric?"Surfaces/suit_normal":"Surfaces/hull_normal"));
  m.SetTexture("_ArmMap",Resources.Load<Texture2D>(fabric?"Surfaces/suit_arm":metal>.7f?"Surfaces/alloy_arm":"Surfaces/hull_arm"));
  m.SetFloat("_DetailScale",fabric?5:.65f);m.SetFloat("_NormalStrength",fabric?.12f:.08f);m.SetFloat("_Wear",fabric?.35f:.22f);return m;
 }
}
