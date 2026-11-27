using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class BuildDemo {
 public static void Build(){
  PlayerSettings.colorSpace=ColorSpace.Linear;
  foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Surfaces"})){string path=AssetDatabase.GUIDToAssetPath(guid);var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;importer.textureType=TextureImporterType.Default;importer.sRGBTexture=!path.Contains("_normal");importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;importer.maxTextureSize=path.EndsWith("_v1.png")?2048:1024;if(path.EndsWith("_v1.png"))importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}

  var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/OrbitRenderer.asset");if(renderer==null){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,"Assets/OrbitRenderer.asset");}
  // Wire actual renderer resources: a Volume alone does not activate post processing.
  renderer.postProcessData=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
  if(renderer.postProcessData==null)throw new System.Exception("URP post processing resources are missing.");
  var contact=AssetDatabase.LoadAssetAtPath<ScreenSpaceAmbientOcclusion>("Assets/Resources/ContactOcclusion.asset");
  if(contact==null){contact=ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();contact.name="Contact occlusion";AssetDatabase.CreateAsset(contact,"Assets/Resources/ContactOcclusion.asset");}
  var contactData=new SerializedObject(contact);var ao=contactData.FindProperty("m_Settings");
  ao.FindPropertyRelative("Source").intValue=0;ao.FindPropertyRelative("NormalSamples").intValue=2;
  ao.FindPropertyRelative("Downsample").boolValue=true;ao.FindPropertyRelative("AfterOpaque").boolValue=true;
  ao.FindPropertyRelative("Intensity").floatValue=.85f;ao.FindPropertyRelative("Radius").floatValue=.36f;
  ao.FindPropertyRelative("DirectLightingStrength").floatValue=.18f;ao.FindPropertyRelative("Falloff").floatValue=80;
  ao.FindPropertyRelative("Samples").intValue=1;ao.FindPropertyRelative("BlurQuality").intValue=0;
  contactData.ApplyModifiedPropertiesWithoutUndo();contact.SetActive(true);
  renderer.rendererFeatures.RemoveAll(feature=>feature==null);
  if(!renderer.rendererFeatures.Contains(contact))renderer.rendererFeatures.Add(contact);
  EditorUtility.SetDirty(renderer);EditorUtility.SetDirty(contact);
  var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OrbitPipeline.asset");if(pipeline==null){pipeline=ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();AssetDatabase.CreateAsset(pipeline,"Assets/OrbitPipeline.asset");}
  var serialized=new SerializedObject(pipeline);var additional=serialized.FindProperty("m_AdditionalLightsRenderingMode");if(additional!=null)additional.intValue=1;var shadows=serialized.FindProperty("m_MainLightShadowsSupported");if(shadows!=null)shadows.boolValue=true;var resolution=serialized.FindProperty("m_MainLightShadowmapResolution");if(resolution!=null)resolution.intValue=2048;var list=serialized.FindProperty("m_RendererDataList");list.arraySize=1;list.GetArrayElementAtIndex(0).objectReferenceValue=renderer;serialized.ApplyModifiedPropertiesWithoutUndo();
  var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
  var included=graphics.FindProperty("m_AlwaysIncludedShaders");
  foreach(var name in new[]{"Orbit/Glow","Orbit/XenoChitin","Orbit/AlienFlora","Orbit/Exhaust","Orbit/PlanetSurface","Orbit/PlanetRock","Orbit/Atmosphere","Orbit/Clouds","Orbit/BlackHoleDisk","Orbit/BlackHoleDistant","Orbit/Asteroid","Orbit/FrontierArchitecture","Orbit/FrontierStar","Orbit/StellarCorona","Orbit/VoyagerParticle","Orbit/HelmetPhoto","Orbit/GalaxyParticles","Orbit/Schwarzschild","Universal Render Pipeline/Lit"}){var shader=Shader.Find(name);if(shader==null)throw new System.Exception("Missing shader "+name);bool found=false;for(int i=0;i<included.arraySize;i++)if(included.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;if(!found){included.InsertArrayElementAtIndex(included.arraySize);included.GetArrayElementAtIndex(included.arraySize-1).objectReferenceValue=shader;}}
  graphics.ApplyModifiedPropertiesWithoutUndo();
  var rendererState=new SerializedObject(renderer);var featureMap=rendererState.FindProperty("m_RendererFeatureMap");featureMap.arraySize=renderer.rendererFeatures.Count;
  for(int i=0;i<renderer.rendererFeatures.Count;i++){if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i],out string featureGuid,out long localId))featureMap.GetArrayElementAtIndex(i).longValue=localId;}
  rendererState.ApplyModifiedPropertiesWithoutUndo();
  GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
  var globalSettings=AssetDatabase.LoadAssetAtPath<RenderPipelineGlobalSettings>("Assets/UniversalRenderPipelineGlobalSettings.asset");
  if(globalSettings==null)throw new System.Exception("URP global runtime resources are missing.");
  EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(globalSettings);
  // Retain the complete runtime settings container in fresh and incremental standalone players.
  var preload=new System.Collections.Generic.List<Object>(PlayerSettings.GetPreloadedAssets());
  if(!preload.Contains(globalSettings))preload.Add(globalSettings);PlayerSettings.SetPreloadedAssets(preload.ToArray());
  foreach(string path in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Surfaces"})){
   var importer=AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(path)) as TextureImporter;if(importer==null)continue;
   int maxSize=importer.assetPath.EndsWith("_v1.png")?2048:1024;
   bool dataMap=importer.assetPath.Contains("normal")||importer.assetPath.Contains("_arm");bool changed=importer.sRGBTexture==dataMap||importer.anisoLevel!=8||!importer.mipmapEnabled||importer.maxTextureSize!=maxSize||importer.wrapMode!=TextureWrapMode.Repeat;
   importer.sRGBTexture=!dataMap;importer.anisoLevel=8;importer.mipmapEnabled=true;importer.maxTextureSize=maxSize;importer.wrapMode=TextureWrapMode.Repeat;if(changed)importer.SaveAndReimport();
  }
  PlayerSettings.bundleVersion="0.7.0";PlayerSettings.enableFrameTimingStats=true;PlayerSettings.companyName="Orbital Field Lab";PlayerSettings.productName="Event Horizon";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=800;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
  var playerConfig=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
  playerConfig.FindProperty("cameraUsageDescription").stringValue="Use your camera for optional local face-flight steering and a live preview. Video is not recorded or uploaded.";playerConfig.ApplyModifiedPropertiesWithoutUndo();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var camera=new GameObject("Main Camera");camera.tag="MainCamera";camera.AddComponent<Camera>();camera.AddComponent<UniversalAdditionalCameraData>();
  new GameObject("Orbital Field Lab").AddComponent<OrbitDemo>();
  System.IO.Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/Orbit.unity");
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Orbit.unity",true)};
  // This is a Resources asset as well as a renderer feature; retain its runtime-loadable identity.
  contact.hideFlags=HideFlags.None;EditorUtility.SetDirty(contact);
  AssetDatabase.SaveAssets();
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Orbit.unity"},locationPathName="../Build/Event Horizon.app",target=BuildTarget.StandaloneOSX,options=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--clean-player")>=0?BuildOptions.CleanBuildCache:BuildOptions.None});
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);
  // This is a Resources asset as well as a renderer feature; retain its runtime-loadable identity.
  contact.hideFlags=HideFlags.None;EditorUtility.SetDirty(contact);
  AssetDatabase.SaveAssets();
  string bundle=System.IO.Path.GetFullPath("../Build/Event Horizon.app"),info=System.IO.Path.Combine(bundle,"Contents/Info.plist");
  var plist=System.Xml.Linq.XDocument.Load(info);var dict=plist.Root.Element("dict");bool continuity=false;
  foreach(var element in dict.Elements("key"))if(element.Value=="NSCameraUseContinuityCameraDeviceType")continuity=true;
  if(!continuity)dict.Add(new System.Xml.Linq.XElement("key","NSCameraUseContinuityCameraDeviceType"),new System.Xml.Linq.XElement("true"));plist.Save(info);
  // Re-apply a local ad-hoc signature after the Info.plist capability update.
  var signing=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("/usr/bin/codesign","--force --deep --sign - \""+bundle+"\""){UseShellExecute=false});signing.WaitForExit();if(signing.ExitCode!=0)throw new System.Exception("Local app signing failed.");
  Debug.Log("ORBIT_BUILD_SUCCESS "+report.summary.totalSize);
 }
}
