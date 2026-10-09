using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
namespace DeadDistrict.Editor {
 public static class PrototypeSetup {
  const string ScenePath="Assets/ZombieSurvival/Scenes/DeadDistrict.unity";
  [MenuItem("Dead District/Rebuild prototype scene")]
  public static void Create() {
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var lighting=new LightingSettings{bakedGI=false,realtimeGI=false};AssetDatabase.CreateAsset(lighting,"Assets/ZombieSurvival/Settings/RealtimeLighting.asset");Lightmapping.lightingSettings=lighting;
   var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,"Assets/ZombieSurvival/Settings/MobileRenderer.asset");
   var pipeline=UniversalRenderPipelineAsset.Create(renderer);pipeline.name="Dead District Mobile URP";pipeline.msaaSampleCount=2;pipeline.renderScale=1;pipeline.shadowDistance=48;pipeline.supportsCameraDepthTexture=false;pipeline.supportsCameraOpaqueTexture=false;
   AssetDatabase.CreateAsset(pipeline,"Assets/ZombieSurvival/Settings/MobileURP.asset");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
   QualitySettings.vSyncCount=0;QualitySettings.shadows=UnityEngine.ShadowQuality.HardOnly;
   var config=AssetDatabase.LoadAssetAtPath<SurvivalConfig>("Assets/ZombieSurvival/Settings/SurvivalConfig.asset");
   if(!config){config=ScriptableObject.CreateInstance<SurvivalConfig>();AssetDatabase.CreateAsset(config,"Assets/ZombieSurvival/Settings/SurvivalConfig.asset");}
   var arena=new GameObject("Abandoned district");
   var ground=BlockVisuals.MakeMaterial("Asphalt",new Color(.16f,.2f,.22f));
   var curb=BlockVisuals.MakeMaterial("Concrete",new Color(.3f,.34f,.33f));
   var brick=BlockVisuals.MakeMaterial("Derelict facades",new Color(.22f,.29f,.29f));
   var stripe=BlockVisuals.MakeMaterial("Faded road paint",new Color(.65f,.64f,.46f));
   var rust=BlockVisuals.MakeMaterial("Rust",new Color(.37f,.24f,.17f));
   var window=BlockVisuals.MakeMaterial("Shuttered windows",new Color(.06f,.12f,.14f));
   BlockVisuals.Shape("Ground",PrimitiveType.Cube,arena.transform,new Vector3(0,-.3f,0),new Vector3(84,.6f,84),ground,true);
   for(int i=-8;i<=8;i++) {
    BlockVisuals.Shape("Lane marking",PrimitiveType.Cube,arena.transform,new Vector3(0,.008f,i*4.5f),new Vector3(.16f,.015f,2),stripe);
    BlockVisuals.Shape("Cross street marking",PrimitiveType.Cube,arena.transform,new Vector3(i*4.5f,.009f,0),new Vector3(2,.015f,.16f),stripe);
   }
   // Road blocks are sparse enough for a wide, mobile-readable combat space.
   for(int side=-1;side<=1;side+=2)for(int i=-1;i<=1;i++) {
    Vector3 p=new Vector3(side*31,0,i*24);
    var b=BlockVisuals.Shape("Abandoned building",PrimitiveType.Cube,arena.transform,p+Vector3.up*3,new Vector3(13,6,15),brick,true,8);
    BlockVisuals.Shape("Roof",PrimitiveType.Cube,b.transform,new Vector3(0,.51f,0),new Vector3(1.05f,.035f,1.05f),curb);
    for(int row=0;row<2;row++)for(int j=0;j<3;j++)BlockVisuals.Shape("Window",PrimitiveType.Cube,arena.transform,p+new Vector3(-side*6.52f,1.8f+row*2,j*4-4),new Vector3(.06f,1.3f,1.5f),window);
   }
   // Four cars and low barricades provide line-of-fire and pathfinding tests.
   Car(arena.transform,new Vector3(-11,0,8),new Color(.33f,.42f,.43f),window);
   Car(arena.transform,new Vector3(12,0,-9),new Color(.45f,.24f,.18f),window);
   Car(arena.transform,new Vector3(10,0,19),new Color(.41f,.4f,.3f),window);
   Car(arena.transform,new Vector3(-14,0,-21),new Color(.24f,.36f,.36f),window);
   for(int i=0;i<3;i++)BlockVisuals.Shape("Concrete barricade",PrimitiveType.Cube,arena.transform,new Vector3(7+i*2.2f,.7f,6),new Vector3(1.8f,1.4f,.9f),curb,true,8);
   for(int i=0;i<5;i++)BlockVisuals.Shape("Shipping crate",PrimitiveType.Cube,arena.transform,new Vector3(-17+(i%2)*2.2f,.7f,18+(i/2)*2.1f),new Vector3(1.9f,1.4f,1.8f),rust,true,8);
   for(int sign=-1;sign<=1;sign+=2) {
    BlockVisuals.Shape("Boundary east-west",PrimitiveType.Cube,arena.transform,new Vector3(sign*42,1.5f,0),new Vector3(1,3,85),brick,true,8);
    BlockVisuals.Shape("Boundary north-south",PrimitiveType.Cube,arena.transform,new Vector3(0,1.5f,sign*42),new Vector3(85,3,1),brick,true,8);
   }
   var surface=arena.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;surface.layerMask=(1<<0)|(1<<8);
   surface.overrideVoxelSize=true;surface.voxelSize=.16f;surface.BuildNavMesh();
   if(surface.navMeshData)AssetDatabase.CreateAsset(surface.navMeshData,"Assets/ZombieSurvival/Settings/DistrictNavMesh.asset");
   var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";var c=camera.GetComponent<Camera>();c.fieldOfView=50;c.nearClipPlane=.2f;c.farClipPlane=120;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.085f,.14f,.16f);
   c.GetUniversalAdditionalCameraData().renderPostProcessing=false;camera.transform.position=new Vector3(0,22,-19);camera.transform.rotation=Quaternion.Euler(49,0,0);
   var light=new GameObject("Late afternoon",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.color=new Color(.95f,.85f,.66f);light.shadows=LightShadows.Hard;light.transform.rotation=Quaternion.Euler(55,-25,0);
   RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.58f,.65f,.66f);RenderSettings.fog=true;RenderSettings.fogColor=c.backgroundColor;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=45;RenderSettings.fogEndDistance=100;
   var game=new GameObject("Survival systems").AddComponent<SurvivalGame>();game.config=config;game.hud=new GameObject("HUD").AddComponent<SurvivalHud>();
   var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var layers=tags.FindProperty("layers");layers.GetArrayElementAtIndex(24).stringValue="Infected";tags.ApplyModifiedProperties();
   Physics.IgnoreLayerCollision(24,24,true);
   PlayerSettings.companyName="Independent Prototype";PlayerSettings.productName="Dead District";PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
   PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
   PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.independent.deaddistrict");PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS,"com.independent.deaddistrict");
   PlayerSettings.colorSpace=ColorSpace.Linear;
   var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;settings.ApplyModifiedProperties();}
   Directory.CreateDirectory("Assets/ZombieSurvival/Resources/DeadDistrict");
   var fx=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));fx.SetColor("_BaseColor",Color.white);AssetDatabase.CreateAsset(fx,"Assets/ZombieSurvival/Resources/DeadDistrict/FX.mat");
   File.Copy("Assets/TopDownEngine/Demos/Loft3D/Sounds/LoftToyGun.wav","Assets/ZombieSurvival/Resources/DeadDistrict/Rifle.wav",true);
   File.Copy("Assets/TopDownEngine/Demos/Loft3D/Sounds/LoftExplosion.wav","Assets/ZombieSurvival/Resources/DeadDistrict/Explosion.wav",true);
   EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();AssetDatabase.Refresh();
   Debug.Log("PROTOTYPE_SCENE_CREATED navmesh="+(surface.navMeshData!=null));
  }
  static void Car(Transform parent,Vector3 pos,Color color,Material window) {
   var mat=BlockVisuals.MakeMaterial("Abandoned car",color);
   BlockVisuals.Shape("Car chassis",PrimitiveType.Cube,parent,pos+Vector3.up*.55f,new Vector3(1.9f,1.1f,4.2f),mat,true,8);
   BlockVisuals.Shape("Car cabin",PrimitiveType.Cube,parent,pos+new Vector3(0,1.3f,-.2f),new Vector3(1.7f,.6f,2.1f),window,true,8);
  }
  [MenuItem("Dead District/Build Mac prototype")]
  public static void PrepareAndBuild(){Create();Build();}
  public static void Build() {
   PlayerSettings.bundleVersion=MobileBuild.Version;
   PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
   var path=Path.GetFullPath("../DeadDistrict.app");
   var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=ChapterCatalog.ScenePaths,target=BuildTarget.StandaloneOSX,locationPathName=path,options=BuildOptions.Development});
   if(r.summary.result!=BuildResult.Succeeded)throw new System.Exception("Build failed: "+r.summary.result);
   Debug.Log("PROTOTYPE_BUILD_PASS bytes="+r.summary.totalSize+" path="+path);
  }
 }
}
