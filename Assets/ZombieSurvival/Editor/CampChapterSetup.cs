using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
namespace DeadDistrict.Editor {
 public static class CampChapterSetup {
  const string Folder="Assets/ZombieSurvival/Apocalypse/Camp/";
  static Transform root;static readonly HashSet<string> used=new HashSet<string>();
  static Bounds BoundsOf(GameObject o){var r=o.GetComponentsInChildren<MeshRenderer>();if(r.Length==0)throw new Exception("No mesh: "+o.name);var b=r[0].bounds;foreach(var x in r)b.Encapsulate(x.bounds);return b;}
  static Material Convert(Material source){string path=Folder+"Materials/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetTexture("_BaseMap",source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.GetTexture("_MainTex"));m.SetColor("_BaseColor",source.HasProperty("_Color")?source.GetColor("_Color"):Color.white);m.SetFloat("_Smoothness",.08f);m.enableInstancing=true;if(source.HasProperty("_Mode")&&Mathf.Approximately(source.GetFloat("_Mode"),1)){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.5f);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}AssetDatabase.CreateAsset(m,path);return m;}
  static Material Flat(string name,Color color,string texture=null,Vector2? tiling=null){string path=Folder+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.04f);m.enableInstancing=true;if(texture!=null)m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));if(tiling.HasValue)m.SetTextureScale("_BaseMap",tiling.Value);EditorUtility.SetDirty(m);return m;}
  static GameObject Place(string name,float x,float z,float width,float yaw=0,bool solid=false,bool fade=false){
   string path=AssetDatabase.FindAssets(name+" t:Prefab",new[]{"Assets/PolygonApocalypse"}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(a=>Path.GetFileNameWithoutExtension(a)==name);if(path==null)throw new Exception("Missing camp prefab "+name);used.Add(path);
   var g=new GameObject(name);g.transform.SetParent(root,false);var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),g.transform);VisualUpgrade.Strip(model,false);foreach(var a in model.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);foreach(var l in model.GetComponentsInChildren<LODGroup>(true))UnityEngine.Object.DestroyImmediate(l);
   model.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.Euler(0,yaw,0));model.transform.localScale=Vector3.one;foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(Convert).ToArray();var b=BoundsOf(g);model.transform.localScale*=width/Mathf.Max(b.size.x,b.size.z);b=BoundsOf(g);model.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
   if(solid){b=BoundsOf(g);g.layer=8;var box=g.AddComponent<BoxCollider>();box.center=b.center;box.size=b.size;
    // Broken tents have real openings: keep a trigger bounds volume only for fading,
    // and use their static mesh surfaces for bullets, movement and baked navigation.
    if(name.Contains("Tent")){g.layer=2;box.isTrigger=true;foreach(var filter in model.GetComponentsInChildren<MeshFilter>()){filter.gameObject.layer=8;var mesh=filter.gameObject.AddComponent<MeshCollider>();mesh.sharedMesh=filter.sharedMesh;}}
   }
   g.transform.position=new Vector3(x,0,z);if(fade)Occlusion(g);return g;
  }
  static void Occlusion(GameObject go){var o=go.AddComponent<StreetOcclusion>();o.obstacle=go.GetComponent<BoxCollider>();o.surfaces=go.GetComponentsInChildren<MeshRenderer>().Select(r=>new StreetOcclusion.Surface{renderer=r,opaque=r.sharedMaterials,translucent=r.sharedMaterials.Select(m=>{string path=AssetDatabase.GetAssetPath(m).Replace(".mat","-ghost.mat");var ghost=AssetDatabase.LoadAssetAtPath<Material>(path);if(ghost)return ghost;ghost=new Material(m);ghost.SetFloat("_Surface",1);ghost.SetFloat("_Blend",0);ghost.SetFloat("_ZWrite",0);ghost.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);ghost.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);ghost.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);ghost.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);ghost.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");ghost.SetOverrideTag("RenderType","Transparent");ghost.renderQueue=3000;ghost.SetShaderPassEnabled("ShadowCaster",false);AssetDatabase.CreateAsset(ghost,path);return ghost;}).ToArray()}).ToArray();}
  static SpawnGroup G(EnemyKind k,int n,float dir=0,float delay=0,bool elite=false)=>new SpawnGroup{kind=k,count=n,direction=dir,delay=delay,elite=elite};
  static WaveDefinition W(string hint,float hp,float damage,bool rest,params SpawnGroup[] groups)=>new WaveDefinition{title="",warning=hint,health=hp,damage=damage,supplies=rest,groups=groups,spawnInterval=.28f,burstSize=8,burstPause=1.6f};
  static void Data(){const string path="Assets/ZombieSurvival/Resources/DeadDistrict/QuarantineCamp.asset";var c=AssetDatabase.LoadAssetAtPath<ChapterDefinition>(path);if(!c){c=ScriptableObject.CreateInstance<ChapterDefinition>();AssetDatabase.CreateAsset(c,path);}c.id="quarantine-camp";c.title="废弃隔离营地";c.sceneName="QuarantineCamp";c.number=2;c.arenaHalfSize=48;c.chargeElite=true;c.streetRooms=false;c.eliteName="营地冲撞者";c.eliteHealth=5400;c.eliteDamage=36;c.eliteWindup=.7f;c.eliteRecovery=1.1f;c.eliteCooldown=1.25f;c.eliteRange=11;
   c.worldCrates=new[]{new Vector3(-8,0,9),new Vector3(9,0,-10),new Vector3(-28,0,10),new Vector3(28,0,-17),new Vector3(-9,0,29),new Vector3(10,0,-32)};
   c.worldExplosives=new[]{new Vector3(-8,0,17),new Vector3(9,0,-18),new Vector3(-27,0,-6),new Vector3(28,0,5)};
   c.weaponSites=new[]{new Vector3(4,0,1),new Vector3(12,0,6),new Vector3(-12,0,-6)};c.supplyRoutes=new[]{new Vector3(8,0,5),new Vector3(-8,0,-6),new Vector3(27,0,-10),new Vector3(-27,0,11)};
   c.waves=new[]{
    W("感染者进入营地",1.02f,1.03f,false,G(EnemyKind.Normal,28)),
    W("留意混入尸群的感染犬",1.08f,1.06f,false,G(EnemyKind.Normal,19,-20),G(EnemyKind.Runner,4,35,2),G(EnemyKind.Normal,19,0)),
    W("两侧入口出现尸群",1.14f,1.1f,false,G(EnemyKind.Normal,17,-60),G(EnemyKind.Runner,8,50,2),G(EnemyKind.Normal,17,60)),
    W("重装感染者靠近",1.2f,1.14f,false,G(EnemyKind.Normal,22,-45),G(EnemyKind.Brute,2,15,2),G(EnemyKind.Runner,6,45,1),G(EnemyKind.Normal,22,55)),
    W("利用掩体，避免被包围",1.28f,1.18f,false,G(EnemyKind.Normal,18,30),G(EnemyKind.Runner,7,-45,2),G(EnemyKind.Brute,3,0,1),G(EnemyKind.Normal,18,-50)),
    W("附近补给已投放",1.2f,1.12f,true,G(EnemyKind.Normal,26),G(EnemyKind.Runner,2,35,2)),
    W("混合尸潮正在集结",1.4f,1.27f,false,G(EnemyKind.Normal,30,-50),G(EnemyKind.Runner,10,45,1),G(EnemyKind.Brute,4,0,2),G(EnemyKind.Normal,30,50)),
    W("精英来袭，向两侧躲避冲撞",1.44f,1.33f,false,G(EnemyKind.Brute,1,0,0,true),G(EnemyKind.Normal,23,-40,4),G(EnemyKind.Runner,4,45,2),G(EnemyKind.Brute,2,20,3))};
   LaterChapterDensity.Configure(c);if(c.waves.Sum(w=>w.Count)!=2040)throw new Exception("Camp budget invalid");EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();
  }
  public static void Apply(){
   Directory.CreateDirectory(Folder+"Materials");AssetDatabase.Refresh();Data();used.Clear();
   var scene=EditorSceneManager.OpenScene(ChapterCatalog.ScenePaths[0]);EditorSceneManager.SaveScene(scene,ChapterCatalog.ScenePaths[1]);
   foreach(var o in scene.GetRootGameObjects())if(o.name=="Abandoned district"||o.name=="Apocalypse atmosphere")UnityEngine.Object.DestroyImmediate(o);
   root=new GameObject("Quarantine camp").transform;
   var soil=Flat("Packed camp soil",new Color(.32f,.32f,.245f),"Assets/ZombieSurvival/Art/District asphalt.png",new Vector2(22,22));
   var lane=Flat("Dusty transport lane",new Color(.44f,.43f,.35f),"Assets/ZombieSurvival/Art/District paving.png",new Vector2(4,12));var concrete=Flat("Checkpoint concrete",new Color(.34f,.37f,.32f));var paint=Flat("Faded checkpoint paint",new Color(.78f,.65f,.33f));
   BlockVisuals.Shape("Camp ground",PrimitiveType.Cube,root,new Vector3(0,-.3f,0),new Vector3(98,.6f,98),soil,true);
   BlockVisuals.Shape("Central transport route",PrimitiveType.Cube,root,new Vector3(0,.009f,0),new Vector3(13,.02f,90),lane);
   BlockVisuals.Shape("Camp cross route",PrimitiveType.Cube,root,new Vector3(0,.011f,0),new Vector3(90,.02f,10),lane);
   foreach(int side in new[]{-1,1}){
    BlockVisuals.Shape("Camp outer boundary",PrimitiveType.Cube,root,new Vector3(side*48,1.3f,0),new Vector3(1,2.6f,97),concrete,true,8);
    BlockVisuals.Shape("Camp outer boundary",PrimitiveType.Cube,root,new Vector3(0,1.3f,side*48),new Vector3(97,2.6f,1),concrete,true,8);
    for(int z=-36;z<=36;z+=12)Place("SM_Prop_Wall_Quarantine_Base_01",side*46,z,10,side>0?90:-90);
    for(int x=-36;x<=36;x+=12)Place("SM_Prop_Wall_Quarantine_Top_01",x,side*46,10,side>0?0:180);
   }
   Place("SM_Bld_Quarantine_Tent_03",-18,9,11,90,true,true);Place("SM_Bld_Quarantine_Tent_02",18,-10,11,-90,true,true);
   Place("SM_Bld_Military_Tent_Damaged_03",-18,-13,10,0,true,true);Place("SM_Bld_Quarantine_Tent_02",18,15,10,180,true,true);
   Place("SM_Bld_Military_Tent_Damaged_03",-32,31,11,90,true,true);Place("SM_Bld_Quarantine_Tent_03",31,-31,11,-90,true,true);
   Place("SM_Veh_Army_Truck_01",-9,23,10.5f,90,true,true);Place("SM_Veh_Army_Truck_01",12,-26,10.5f,-90,true,true);
   Place("SM_Prop_Shipping_Container_01",-33,0,11,0,true,true);Place("SM_Prop_Shipping_Container_01",33,0,11,0,true,true);
   Place("SM_Prop_Generator_01",-25,16,3,90,true);Place("SM_Prop_Generator_01",26,-19,3,-90,true);
   Place("SM_Prop_Sandbag_Wall_03",-6,16,4.5f,90,true);Place("SM_Prop_Sandbag_Wall_03",7,-14,4.5f,90,true);Place("SM_Prop_Barrier_Concrete_Bare_01",24,28,5,0,true);Place("SM_Prop_Barricade_Wired_02",-25,-28,5,0,true);
   Place("SM_Prop_Sign_Quarantine_01",-9,-5,1f,135);Place("SM_Prop_Sign_Military_01",9,6,1f,-45);
   foreach(var a in new[]{new Vector3(-25,0,9),new Vector3(25,0,15),new Vector3(-17,0,-21),new Vector3(18,0,-18)}){Place("SM_Prop_Crate_Open_04",a.x,a.z,1.8f,30);Place("SM_Prop_TrashPile_05",a.x+1.4f,a.z+1,2,15);}
   for(int i=0;i<16;i++){float angle=i*Mathf.PI/8;Place("SM_Env_Overgrowth_06",Mathf.Cos(angle)*43,Mathf.Sin(angle)*43,3,i*35);}
   for(int i=-3;i<=3;i++)BlockVisuals.Shape("Checkpoint lane dash",PrimitiveType.Cube,root,new Vector3(0,.029f,i*11),new Vector3(.18f,.025f,2),paint);
   var sun=GameObject.Find("Late afternoon").GetComponent<Light>();sun.color=new Color(1,.91f,.74f);sun.intensity=1.45f;sun.transform.rotation=Quaternion.Euler(52,-30,0);RenderSettings.ambientLight=new Color(.6f,.65f,.58f);RenderSettings.fogColor=new Color(.22f,.27f,.24f);
   var surface=root.gameObject.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.layerMask=(1<<0)|(1<<8);surface.overrideVoxelSize=true;surface.voxelSize=.16f;Physics.SyncTransforms();surface.BuildNavMesh();
   const string navPath="Assets/ZombieSurvival/Settings/CampNavMesh.asset";var old=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);if(old){EditorUtility.CopySerialized(surface.navMeshData,old);surface.RemoveData();surface.navMeshData=old;surface.AddData();EditorUtility.SetDirty(old);}else AssetDatabase.CreateAsset(surface.navMeshData,navPath);
   EditorBuildSettings.scenes=ChapterCatalog.ScenePaths.Select(x=>new EditorBuildSettingsScene(x,true)).ToArray();PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.iOS.buildNumber="45";
   EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText(Folder+"used-assets.txt",string.Join("\n",used.OrderBy(x=>x)));Validate();Debug.Log("CAMP_MAP_READY prefabs="+used.Count+" children="+root.childCount);
  }
  public static void Validate(){
   var path=new NavMeshPath();foreach(var p in new[]{Vector3.zero,new Vector3(0,0,36),new Vector3(36,0,12),new Vector3(-36,0,-12),new Vector3(0,0,-36),new Vector3(-28,0,20),new Vector3(28,0,-20)})if(!NavMesh.CalculatePath(Vector3.zero,p,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Camp route blocked: "+p);
   var cfg=ChapterCatalog.Get(1);foreach(var p in cfg.worldCrates.Concat(cfg.worldExplosives).Concat(cfg.weaponSites).Concat(cfg.supplyRoutes))if(Physics.CheckCapsule(p+Vector3.up*.4f,p+Vector3.up*1.7f,.5f,1<<8)||!NavMesh.CalculatePath(Vector3.zero,p,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Camp pickup blocked: "+p);
   var camp=GameObject.Find("Quarantine camp");int triangles=0;foreach(var r in camp.GetComponentsInChildren<MeshRenderer>()){if(r.sharedMaterials.Any(m=>!m||m.shader.name!="Universal Render Pipeline/Lit"))throw new Exception("Invalid camp material");triangles+=r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3;}if(triangles>240000)throw new Exception("Camp triangle budget "+triangles);Debug.Log("CAMP_MAP_VALID triangles="+triangles+" connectedRoutes=7");
  }
  public static void ApplyAndBuild(){Apply();PrototypeSetup.Build();}
 }
}
