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
 public static class FreightChapterSetup {
  const string Folder="Assets/ZombieSurvival/Apocalypse/Freight/";
  static Transform root;static readonly HashSet<string> used=new HashSet<string>();
  static Bounds BoundsOf(GameObject o){var r=o.GetComponentsInChildren<MeshRenderer>();if(r.Length==0)throw new Exception("No mesh: "+o.name);var b=r[0].bounds;foreach(var x in r)b.Encapsulate(x.bounds);return b;}
  static Material Convert(Material source){string path=Folder+"Materials/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetTexture("_BaseMap",source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.GetTexture("_MainTex"));m.SetColor("_BaseColor",source.HasProperty("_Color")?source.GetColor("_Color"):Color.white);m.SetFloat("_Smoothness",.08f);m.enableInstancing=true;if(source.HasProperty("_Mode")&&Mathf.Approximately(source.GetFloat("_Mode"),1)){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.5f);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}AssetDatabase.CreateAsset(m,path);return m;}
  static Material Flat(string name,Color color,string texture=null,Vector2? tiling=null){string path=Folder+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.04f);m.enableInstancing=true;if(texture!=null)m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));if(tiling.HasValue)m.SetTextureScale("_BaseMap",tiling.Value);EditorUtility.SetDirty(m);return m;}
  static GameObject Place(string name,float x,float z,float width,float yaw=0,bool solid=false,bool fade=false){
   string path=AssetDatabase.FindAssets(name+" t:Prefab",new[]{"Assets/PolygonApocalypse"}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(a=>Path.GetFileNameWithoutExtension(a)==name);if(path==null)throw new Exception("Missing freight prefab "+name);used.Add(path);
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
  static WaveDefinition W(string hint,float hp,float damage,bool rest,params SpawnGroup[] groups)=>new WaveDefinition{title="",warning=hint,health=hp,damage=damage,supplies=rest,groups=groups,spawnInterval=.3f,burstSize=8,burstPause=1.6f};
  static void Data(){const string path="Assets/ZombieSurvival/Resources/DeadDistrict/FreightDepot.asset";var c=AssetDatabase.LoadAssetAtPath<ChapterDefinition>(path);if(!c){c=ScriptableObject.CreateInstance<ChapterDefinition>();AssetDatabase.CreateAsset(c,path);}c.id="freight-depot";c.title="废弃货运站";c.sceneName="FreightDepot";c.number=3;c.arenaHalfSize=54;c.chargeElite=false;c.acidElite=true;c.streetRooms=false;c.eliteName="腐蚀巨兽";c.description="仓库货场 · 穿行掩体\n腐蚀精英";c.victoryTitle="货场肃清";c.eliteHealth=6200;c.eliteDamage=21;c.eliteWindup=.7f;c.eliteRecovery=3;c.eliteCooldown=.75f;c.eliteRange=14;c.acidRadius=2.6f;c.acidLifetime=3;c.acidTickDamage=7;c.acidTickInterval=.7f;c.acidFlightTime=.22f;
   c.worldCrates=new[]{new Vector3(-7,0,-8),new Vector3(8,0,8),new Vector3(-34,0,-14),new Vector3(34,0,15),new Vector3(-8,0,36),new Vector3(8,0,-36)};
   c.worldExplosives=new[]{new Vector3(-8,0,13),new Vector3(9,0,-12),new Vector3(-30,0,-26),new Vector3(29,0,26)};
   c.weaponSites=new[]{new Vector3(4,0,1),new Vector3(-7,0,-19),new Vector3(7,0,20)};c.supplyRoutes=new[]{new Vector3(-5,0,7),new Vector3(6,0,-7),new Vector3(-36,0,0),new Vector3(36,0,0)};
   c.waves=new[]{
    W("感染者进入装卸区",1.12f,1.08f,false,G(EnemyKind.Normal,34),G(EnemyKind.Runner,2,30,3)),
    W("利用集装箱切开尸群",1.18f,1.12f,false,G(EnemyKind.Normal,23,-45),G(EnemyKind.Runner,6,45,2),G(EnemyKind.Normal,23,40)),
    W("重装感染者堵住通路",1.26f,1.18f,false,G(EnemyKind.Normal,21,-40),G(EnemyKind.Brute,3,0,2),G(EnemyKind.Runner,7,55,2),G(EnemyKind.Normal,21,40)),
    W("两侧装卸通道出现尸潮",1.33f,1.22f,false,G(EnemyKind.Normal,27,-65),G(EnemyKind.Runner,8,60,2),G(EnemyKind.Brute,3,15,2),G(EnemyKind.Normal,27,65)),
    W("保持移动，留出退路",1.4f,1.27f,false,G(EnemyKind.Normal,21,40),G(EnemyKind.Runner,9,-45,2),G(EnemyKind.Brute,4,0,1),G(EnemyKind.Normal,21,-40)),
    W("补给已投放，准备最后一战",1.3f,1.18f,true,G(EnemyKind.Normal,28),G(EnemyKind.Runner,2,40,2)),
    W("货场入口的混合尸群正在集结",1.5f,1.34f,false,G(EnemyKind.Normal,34,-60),G(EnemyKind.Runner,12,60,2),G(EnemyKind.Brute,5,0,2),G(EnemyKind.Normal,34,60)),
    W("精英来袭，避开腐蚀液落点",1.55f,1.38f,false,G(EnemyKind.Brute,1,0,0,true),G(EnemyKind.Normal,23,-40,4),G(EnemyKind.Runner,6,45,2),G(EnemyKind.Brute,3,20,2))};
   LaterChapterDensity.Configure(c);if(c.waves.Sum(w=>w.Count)!=4262)throw new Exception("Freight budget invalid: "+c.waves.Sum(w=>w.Count));EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();
  }
  [MenuItem("Dead District/Build third chapter map")]
  public static void Apply(){Directory.CreateDirectory(Folder+"Materials");AssetDatabase.Refresh();Data();used.Clear();var scene=EditorSceneManager.OpenScene(ChapterCatalog.ScenePaths[0]);EditorSceneManager.SaveScene(scene,ChapterCatalog.ScenePaths[2]);foreach(var o in scene.GetRootGameObjects())if(o.name=="Abandoned district"||o.name=="Apocalypse atmosphere")UnityEngine.Object.DestroyImmediate(o);root=new GameObject("Freight depot").transform;
   var ground=Flat("Worn loading asphalt",new Color(.31f,.34f,.36f),"Assets/ZombieSurvival/Art/District asphalt.png",new Vector2(24,24));var slab=Flat("Loading apron",new Color(.42f,.43f,.4f),"Assets/ZombieSurvival/Art/District paving.png",new Vector2(8,6));var concrete=Flat("Depot boundary concrete",new Color(.35f,.38f,.4f));var paint=Flat("Freight warning yellow",new Color(.86f,.62f,.19f));
   BlockVisuals.Shape("Depot ground",PrimitiveType.Cube,root,new Vector3(0,-.3f,0),new Vector3(110,.6f,110),ground,true);
   BlockVisuals.Shape("North loading apron",PrimitiveType.Cube,root,new Vector3(-21,.012f,18),new Vector3(29,.02f,27),slab);
   BlockVisuals.Shape("South loading apron",PrimitiveType.Cube,root,new Vector3(22,.012f,-18),new Vector3(29,.02f,27),slab);
   foreach(int side in new[]{-1,1}){BlockVisuals.Shape("Depot boundary",PrimitiveType.Cube,root,new Vector3(side*54,1.3f,0),new Vector3(1,2.6f,109),concrete,true,8);BlockVisuals.Shape("Depot boundary",PrimitiveType.Cube,root,new Vector3(0,1.3f,side*54),new Vector3(109,2.6f,1),concrete,true,8);}
   // Large closed buildings stand back from the main battle lane, with a clear loop around each.
   Place("SM_Bld_Warehouse_Brick_01",-23,20,22,90,true,true);Place("SM_Bld_Warehouse_Concrete_01",24,-21,22,-90,true,true);
   Place("SM_Prop_Shipping_Container_02",-18,-10,13,0,true,true);Place("SM_Prop_Shipping_Container_03",17,12,13,0,true,true);
   Place("SM_Prop_Shipping_Container_01",0,30,12,90,true,true);Place("SM_Prop_Shipping_Container_03",-5,-31,11,90,true,true);
   Place("SM_Veh_BigRig_Trailer_Tanker_Damaged_01",-31,-30,16,90,true,true);Place("SM_Veh_Army_Truck_01",34,31,12,-90,true,true);
   Place("SM_Bld_Industrial_Small_01",-35,43,13,90,true,true);Place("SM_Bld_WaterTank_01",37,-41,9,0,true,true);
   Place("SM_Prop_Pipes_Rusted_01",-30,-9,7,0,true);Place("SM_Prop_Pipes_Valves_01",31,9,6,90,true);
   Place("SM_Prop_BarrelStack_01_Tarp",-21,-23,4,20,true);Place("SM_Prop_Crate_Large_01",22,25,4,-10,true);
   foreach(var a in new[]{new Vector3(-12,0,24),new Vector3(13,0,-24),new Vector3(-40,0,-24),new Vector3(40,0,23)}){Place("SM_Prop_Pallet_01",a.x,a.z,2,30);Place("SM_Prop_Crate_02",a.x+1.3f,a.z+1,1.4f,15);Place("SM_Prop_TrashPile_02",a.x-1.5f,a.z+1.2f,2.5f,90);}
   Place("SM_Prop_Barrel_Nuke_01",-29,-36,1.2f,0,true);Place("SM_Prop_Barrel_Warning_01",-26,-34,1.2f,0,true);
   for(int i=0;i<8;i++){float z=-21+i*6;BlockVisuals.Shape("Loading lane edge",PrimitiveType.Cube,root,new Vector3(-6,.035f,z),new Vector3(.18f,.025f,2.6f),paint);BlockVisuals.Shape("Loading lane edge",PrimitiveType.Cube,root,new Vector3(6,.035f,z),new Vector3(.18f,.025f,2.6f),paint);}
   foreach(int side in new[]{-1,1})for(int i=-2;i<=2;i++){Place("SM_Prop_Fence_01",side*51,i*18,12,side>0?90:-90);Place("SM_Prop_LightPole_01",i*19,side*50,2.2f,side>0?180:0);}
   for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Place("SM_Env_Overgrowth_04",Mathf.Cos(a)*49,Mathf.Sin(a)*49,2.7f,i*30);}
   var sun=GameObject.Find("Late afternoon").GetComponent<Light>();sun.color=new Color(.92f,.95f,1);sun.intensity=1.55f;sun.transform.rotation=Quaternion.Euler(50,-35,0);RenderSettings.ambientLight=new Color(.58f,.65f,.7f);RenderSettings.fogColor=new Color(.2f,.25f,.3f);
   var surface=root.gameObject.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.layerMask=(1<<0)|(1<<8);surface.overrideVoxelSize=true;surface.voxelSize=.16f;Physics.SyncTransforms();surface.BuildNavMesh();const string navPath="Assets/ZombieSurvival/Settings/FreightNavMesh.asset";var old=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);if(old){EditorUtility.CopySerialized(surface.navMeshData,old);surface.RemoveData();surface.navMeshData=old;surface.AddData();EditorUtility.SetDirty(old);}else AssetDatabase.CreateAsset(surface.navMeshData,navPath);
   EditorBuildSettings.scenes=ChapterCatalog.ScenePaths.Select(x=>new EditorBuildSettingsScene(x,true)).ToArray();PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.iOS.buildNumber="45";EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText(Folder+"used-assets.txt",string.Join("\n",used.OrderBy(x=>x)));Validate();Debug.Log("FREIGHT_MAP_READY prefabs="+used.Count+" children="+root.childCount);
  }
  public static void Validate(){var path=new NavMeshPath();foreach(var p in new[]{Vector3.zero,new Vector3(0,0,43),new Vector3(43,0,0),new Vector3(0,0,-43),new Vector3(-43,0,0),new Vector3(-8,0,-20),new Vector3(8,0,22)})if(!NavMesh.CalculatePath(Vector3.zero,p,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Freight route blocked: "+p);
   var cfg=ChapterCatalog.Get(2);foreach(var p in cfg.worldCrates.Concat(cfg.worldExplosives).Concat(cfg.weaponSites).Concat(cfg.supplyRoutes))if(Physics.CheckCapsule(p+Vector3.up*.4f,p+Vector3.up*1.7f,.5f,1<<8)||!NavMesh.CalculatePath(Vector3.zero,p,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Freight pickup blocked: "+p);
   int triangles=0;foreach(var r in root.GetComponentsInChildren<MeshRenderer>()){if(r.sharedMaterials.Any(m=>!m||m.shader.name!="Universal Render Pipeline/Lit"))throw new Exception("Invalid freight material");triangles+=r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3;}if(triangles>240000)throw new Exception("Freight triangle budget "+triangles);Debug.Log("FREIGHT_MAP_VALID triangles="+triangles+" connectedRoutes=7");
  }
  public static void AcidMaterials(){
   foreach(string name in new[]{"AcidPool","AcidWarning"}){
    string path="Assets/ZombieSurvival/Resources/DeadDistrict/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Resources.Load<Material>("DeadDistrict/FX"));AssetDatabase.CreateAsset(m,path);}
    m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_ZWrite",0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);m.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;m.SetColor("_BaseColor",Color.white);
    if(name=="AcidPool"){m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Epic Toon FX/Textures/splat02_flat_3x3.png"));m.SetTextureScale("_BaseMap",Vector2.one/3);m.SetTextureOffset("_BaseMap",new Vector2(1f/3,1f/3));}EditorUtility.SetDirty(m);
   }AssetDatabase.SaveAssets();
  }
  public static void FinishVisualsAndBuild(){AcidMaterials();PrototypeSetup.Build();}
  public static void ApplyAndBuild(){Apply();AcidMaterials();PrototypeSetup.Build();}
 }
}
