using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

// Checksummed, bounded, atomic files. A corrupt primary never replaces a valid backup.
public static class CampaignFiles {
 [Serializable] sealed class Envelope {public int version=1;public string checksum,payload;}
 static string Digest(string value){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
 public static T Read<T>(string path,Func<T,bool> validate) where T:class {
  foreach(string file in new[]{path,path+".backup"})try{
   if(!File.Exists(file)||new FileInfo(file).Length>8*1024*1024)continue;
   var wrapper=JsonUtility.FromJson<Envelope>(File.ReadAllText(file));
   if(wrapper==null||wrapper.version!=1||string.IsNullOrEmpty(wrapper.payload)||wrapper.checksum!=Digest(wrapper.payload))continue;
   var value=JsonUtility.FromJson<T>(wrapper.payload);if(value!=null&&validate(value))return value;
  }catch(Exception){}return null;
 }
 public static void Write<T>(string path,T value,Func<T,bool> validate) where T:class {
  if(value==null||!validate(value))throw new InvalidDataException("Invalid campaign data");
  string payload=JsonUtility.ToJson(value);if(payload.Length>6*1024*1024)throw new InvalidDataException("Campaign exceeds file budget");
  string json=JsonUtility.ToJson(new Envelope{checksum=Digest(payload),payload=payload},true);
  Directory.CreateDirectory(Path.GetDirectoryName(path));string pending=path+".pending";
  using(var file=new FileStream(pending,FileMode.Create,FileAccess.Write,FileShare.None)){
   byte[] bytes=Encoding.UTF8.GetBytes(json);file.Write(bytes,0,bytes.Length);file.Flush(true);
  }
  if(File.Exists(path)){
   bool validPrimary=false;
   try{var previous=JsonUtility.FromJson<Envelope>(File.ReadAllText(path));validPrimary=previous!=null&&previous.version==1&&previous.payload!=null&&previous.checksum==Digest(previous.payload)&&validate(JsonUtility.FromJson<T>(previous.payload));}catch(Exception){}
   File.Replace(pending,path,validPrimary?path+".backup":null);
  }else File.Move(pending,path);
 }
}

[Serializable]
public sealed class CampaignCheckpoint {
 public int landscapeVersion; // Missing in v0.3-v0.7 checkpoints: zero keeps their original terrain.
 public int version=1,slot=1,quality=1,chartSystem,chartPort,salvageCount,hits,kills,arrivals;
 public long cellX,cellY,cellZ,score;
 public string savedUtc;
 public OrbitDemo.State ship;
 public Quaternion rotation=Quaternion.identity;
 public float health=100,elapsed,wheelDeployment,roverYaw,roverPitch=16;
 public bool wheels,flightAssist=true,cruising,landingAssist,cruiseBraking;
 public float throttle;public int routeLeg;public string destination;public SavedPoint routeTarget=new SavedPoint();public List<SavedPoint> route=new List<SavedPoint>();public List<SavedRocket> rockets=new List<SavedRocket>();
 [Serializable] public sealed class SavedPoint{public double x,y,z;}
 [Serializable] public sealed class SavedRocket{public OrbitDemo.State state;public string key;public float age,altitude;public bool credited;}
 public int[] tiers=new int[4];
 public CivilizationModel civilization;
 public VoyagerState campaign;
 public PlanetExpedition.SavedCrew crew;
 public SurfaceOperations.SavedSurface surface;
 public SpaceEnvironment.SavedSpace space;
 public List<CatalogueEntry> catalogue=new List<CatalogueEntry>();
 [Serializable] public sealed class CatalogueEntry {public string name,key;public double x,y,z;}
 public bool Validate(){
  if(landscapeVersion<0||landscapeVersion>1||version!=1||slot<1||slot>3||civilization==null||!civilization.Validate()||campaign==null||!campaign.Validate()||campaign.slot!=slot||crew==null||surface==null||space==null||tiers==null||tiers.Length!=4||catalogue==null||catalogue.Count<32||catalogue.Count>128||chartSystem<0||chartSystem>=catalogue.Count||chartPort<0||chartPort>3)return false;
  if(Math.Abs(cellX)>1000000000L||Math.Abs(cellY)>1000000000L||Math.Abs(cellZ)>1000000000L)return false;
  foreach(int t in tiers)if(t<0||t>3)return false;
  if(!ValidState(ship)||!VoyagerState.Finite(health)||health<=0||health>10000||!VoyagerState.Finite(elapsed)||elapsed<0||!VoyagerState.Finite(wheelDeployment)||wheelDeployment<0||wheelDeployment>1||salvageCount<0)return false;
  if(campaign.Draw>campaign.Reactor(civilization.blueprint)||campaign.cargo>campaign.Capacity(civilization.blueprint))return false;
  if(route==null||route.Count>64||routeTarget==null||rockets==null||rockets.Count>8||!VoyagerState.Finite(throttle)||throttle<0||throttle>1)return false;
  if(cruising&&(route.Count==0||routeLeg<0||routeLeg>=route.Count))return false;
  foreach(var point in route)if(point==null||!Finite(point.x)||!Finite(point.y)||!Finite(point.z))return false;
  foreach(var rocket in rockets)if(rocket==null||!ValidState(rocket.state)||string.IsNullOrEmpty(rocket.key)||!VoyagerState.Finite(rocket.age)||!VoyagerState.Finite(rocket.altitude))return false;
  if(!ValidRotation(rotation)||!crew.Validate()||!surface.Validate()||!space.Validate())return false;
  var keys=new HashSet<string>();foreach(var c in catalogue)if(c==null||string.IsNullOrEmpty(c.key)||!keys.Add(c.key)||string.IsNullOrEmpty(c.name)||!Finite(c.x)||!Finite(c.y)||!Finite(c.z))return false;
  return true;
 }
 public static bool Finite(double x){return !double.IsInfinity(x)&&!double.IsNaN(x)&&Math.Abs(x)<1e14;}
 public static bool ValidState(OrbitDemo.State s){return Finite(s.x)&&Finite(s.y)&&Finite(s.z)&&Finite(s.vx)&&Finite(s.vy)&&Finite(s.vz)&&Math.Abs(s.x)<1000000000&&Math.Abs(s.y)<1000000000&&Math.Abs(s.z)<1000000000&&s.captured==0;}
 static bool ValidRotation(Quaternion q){return VoyagerState.Finite(q.x)&&VoyagerState.Finite(q.y)&&VoyagerState.Finite(q.z)&&VoyagerState.Finite(q.w)&&q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w>.5f;}
}
