using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class BuildDemo {
 public static void Build(){
  PlayerSettings.colorSpace=ColorSpace.Linear;
  foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Surfaces"})){string path=AssetDatabase.GUIDToAssetPath(guid);var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;importer.textureType=TextureImporterType.Default;importer.sRGBTexture=!path.Contains("_normal");importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}

  var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/OrbitRenderer.asset");if(renderer==null){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,"Assets/OrbitRenderer.asset");}
  var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/OrbitPipeline.asset");if(pipeline==null){pipeline=ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();AssetDatabase.CreateAsset(pipeline,"Assets/OrbitPipeline.asset");}
  var serialized=new SerializedObject(pipeline);var additional=serialized.FindProperty("m_AdditionalLightsRenderingMode");if(additional!=null)additional.intValue=1;var shadows=serialized.FindProperty("m_MainLightShadowsSupported");if(shadows!=null)shadows.boolValue=true;var resolution=serialized.FindProperty("m_MainLightShadowmapResolution");if(resolution!=null)resolution.intValue=2048;var list=serialized.FindProperty("m_RendererDataList");list.arraySize=1;list.GetArrayElementAtIndex(0).objectReferenceValue=renderer;serialized.ApplyModifiedPropertiesWithoutUndo();
  var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
  var included=graphics.FindProperty("m_AlwaysIncludedShaders");
  foreach(var name in new[]{"Orbit/Glow","Orbit/XenoChitin","Orbit/AlienFlora","Orbit/Exhaust","Orbit/PlanetSurface","Orbit/Atmosphere","Orbit/Clouds","Orbit/BlackHoleDisk","Orbit/BlackHoleDistant","Orbit/Asteroid","Orbit/HelmetPhoto","Orbit/GalaxyParticles","Orbit/Schwarzschild","Universal Render Pipeline/Lit"}){var shader=Shader.Find(name);if(shader==null)throw new System.Exception("Missing shader "+name);bool found=false;for(int i=0;i<included.arraySize;i++)if(included.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;if(!found){included.InsertArrayElementAtIndex(included.arraySize);included.GetArrayElementAtIndex(included.arraySize-1).objectReferenceValue=shader;}}
  graphics.ApplyModifiedPropertiesWithoutUndo();
  GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
  PlayerSettings.companyName="Orbital Field Lab";PlayerSettings.productName="Event Horizon";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=800;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var camera=new GameObject("Main Camera");camera.tag="MainCamera";camera.AddComponent<Camera>();camera.AddComponent<UniversalAdditionalCameraData>();
  new GameObject("Orbital Field Lab").AddComponent<OrbitDemo>();
  System.IO.Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/Orbit.unity");
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Orbit.unity",true)};
  AssetDatabase.SaveAssets();
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Orbit.unity"},locationPathName="../Build/Event Horizon.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);
  Debug.Log("ORBIT_BUILD_SUCCESS "+report.summary.totalSize);
 }
}
