using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
namespace DeadDistrict.Editor {
 // Additive, repeatable dressing pass; chapter data, combat and existing map roots are preserved.
 public static class SceneDressingUpgrade {
  const string Folder="Assets/ZombieSurvival/Apocalypse/Dressing/";
  public const string RootName="Chapter detail pass";
  static Transform root,group;static int chapter,placed,skipped;static ChapterDefinition config;static Vector3[] reserved;
  static readonly List<Vector3> accessible=new List<Vector3>();
  static readonly HashSet<string> used=new HashSet<string>();static readonly List<string> report=new List<string>();
  static readonly Dictionary<string,string> sources=new Dictionary<string,string>();
  static Bounds BoundsOf(GameObject o){var r=o.GetComponentsInChildren<MeshRenderer>();if(r.Length==0)throw new Exception("No mesh "+o.name);var b=r[0].bounds;foreach(var x in r)b.Encapsulate(x.bounds);return b;}
  static string Source(string name){if(sources.TryGetValue(name,out string path))return path;path=AssetDatabase.FindAssets(name+" t:Prefab",new[]{"Assets/PolygonApocalypse"}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>Path.GetFileNameWithoutExtension(p)==name);if(path==null)throw new Exception("Missing dressing source "+name);return sources[name]=path;}
  static Material Convert(Material source,bool particle=false){string path=Folder+"Materials/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+(particle?"-particle":"")+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(Shader.Find(particle?"Universal Render Pipeline/Particles/Unlit":"Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",particle?Color.white:source.HasProperty("_Color")?source.GetColor("_Color"):Color.white);m.SetTexture("_BaseMap",source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.HasProperty("_MainTex")?source.GetTexture("_MainTex"):null);m.SetFloat("_Smoothness",.08f);m.enableInstancing=true;
   if(particle){m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;}else if(source.HasProperty("_Mode")&&Mathf.Approximately(source.GetFloat("_Mode"),1)){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.5f);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}
   AssetDatabase.CreateAsset(m,path);return m;
  }
  static Material Flat(string name,Color c){string path=Folder+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",0);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;}
  static void Group(string name){group=new GameObject(name).transform;group.SetParent(root,false);}
  static void Mark(string name,float x,float z,float w,float d,float yaw=0,Color? color=null){var g=BlockVisuals.Shape(name,PrimitiveType.Cube,group,new Vector3(x,.052f,z),new Vector3(w,.008f,d),Flat(color.HasValue?"Medical lane paint":"Faded loading paint",color??new Color(.64f,.57f,.37f)));g.transform.localRotation=Quaternion.Euler(0,yaw,0);}
  static void Bay(float x,float z,float w,float d){Mark("Loading bay left",x-w/2,z,.1f,d);Mark("Loading bay right",x+w/2,z,.1f,d);Mark("Loading bay end",x,z+d/2,w,.1f);}
  static bool Clear(Bounds b,Vector3 position){
   if(Mathf.Abs(position.x)>config.arenaHalfSize-4||Mathf.Abs(position.z)>config.arenaHalfSize-4)return false;
   if(Mathf.Abs(position.x)<4.5f&&Mathf.Abs(position.z)<32)return false;
   var expanded=b;expanded.Expand(new Vector3(.4f,.04f,.4f));
   if(Physics.CheckBox(expanded.center,expanded.extents,Quaternion.identity,1<<8,QueryTriggerInteraction.Ignore))return false;
   foreach(var p in reserved){var q=b.ClosestPoint(p+Vector3.up*.4f);q.y=0;var d=p-q;d.y=0;if(d.sqrMagnitude<2.1f*2.1f)return false;}
   if(!NavMesh.SamplePosition(position,out var hit,.8f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y)>.5f)return false;
   return true;
  }
  static GameObject Prop(string name,float x,float z,float size,float yaw=0,bool solid=true){
   var g=new GameObject(name);g.transform.SetParent(group,false);var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source(name)),g.transform);VisualUpgrade.Strip(model,false);foreach(var a in model.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);foreach(var l in model.GetComponentsInChildren<LODGroup>(true))UnityEngine.Object.DestroyImmediate(l);
   model.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.Euler(0,yaw,0));model.transform.localScale=Vector3.one;foreach(var r in model.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>Convert(m)).ToArray();var b=BoundsOf(g);model.transform.localScale*=size/Mathf.Max(b.size.x,b.size.y,b.size.z);b=BoundsOf(g);model.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
   bool fit=!solid;for(int attempt=0;attempt<(solid?49:1);attempt++){float distance=attempt==0?0:Mathf.Ceil(attempt/12f)*.65f;float angle=attempt*Mathf.PI/6;g.transform.position=new Vector3(x+Mathf.Cos(angle)*distance,solid?0:.058f,z+Mathf.Sin(angle)*distance);b=BoundsOf(g);if(!solid||Clear(b,g.transform.position)){fit=true;break;}}
   if(!fit){skipped++;report.Add("SKIPPED chapter="+chapter+" "+name+" desired="+x+","+z);UnityEngine.Object.DestroyImmediate(g);return null;}
   if(solid){g.layer=8;var c=g.AddComponent<BoxCollider>();c.center=g.transform.InverseTransformPoint(b.center);c.size=b.size;if(b.size.y>1.6f)Occlusion(g);}
   else if(b.size.y>.28f&&!name.Contains("Overgrowth")){// Low paper/rubble is ground detail, not invisible collision.
    model.transform.localScale=new Vector3(model.transform.localScale.x,model.transform.localScale.y*.12f/model.GetComponentInChildren<MeshRenderer>().bounds.size.y,model.transform.localScale.z);
   }
   Physics.SyncTransforms();placed++;used.Add(Source(name));report.Add("PLACED chapter="+chapter+" "+name+" at="+g.transform.position+" solid="+solid);return g;
  }
  static void Occlusion(GameObject g){var o=g.AddComponent<StreetOcclusion>();o.obstacle=g.GetComponent<BoxCollider>();o.surfaces=g.GetComponentsInChildren<MeshRenderer>().Select(r=>new StreetOcclusion.Surface{renderer=r,opaque=r.sharedMaterials,translucent=r.sharedMaterials.Select(m=>{string path=AssetDatabase.GetAssetPath(m).Replace(".mat","-ghost.mat");var ghost=AssetDatabase.LoadAssetAtPath<Material>(path);if(ghost)return ghost;ghost=new Material(m);ghost.SetFloat("_Surface",1);ghost.SetFloat("_Blend",0);ghost.SetFloat("_ZWrite",0);ghost.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);ghost.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);ghost.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);ghost.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);ghost.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");ghost.SetOverrideTag("RenderType","Transparent");ghost.renderQueue=3000;ghost.SetShaderPassEnabled("ShadowCaster",false);AssetDatabase.CreateAsset(ghost,path);return ghost;}).ToArray()}).ToArray();}
  static void Effect(string name,Vector3 p,bool sparks){var site=new GameObject(sparks?"Intermittent equipment sparks":"Road warning flare");site.transform.SetParent(group,false);site.transform.position=p;var fx=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source(name)),site.transform);VisualUpgrade.Strip(fx,true);fx.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);fx.transform.localScale=Vector3.one;
   foreach(var ps in fx.GetComponentsInChildren<ParticleSystem>(true)){ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);ps.transform.localPosition=Vector3.zero;ps.transform.localScale=Vector3.one;var main=ps.main;main.loop=true;main.playOnAwake=false;main.prewarm=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.cullingMode=ParticleSystemCullingMode.Pause;main.useUnscaledTime=false;main.maxParticles=sparks?8:10;main.startLifetime=sparks?.35f:.65f;main.startSpeed=sparks?1.4f:.15f;main.startSize=sparks?.055f:.13f;main.startSize3D=false;main.gravityModifier=sparks?.25f:0;main.startColor=sparks?new Color(1,.7f,.3f,.85f):new Color(1,.32f,.1f,.55f);var emission=ps.emission;emission.enabled=true;emission.rateOverTime=sparks?3:5;emission.rateOverDistance=0;emission.SetBursts(new ParticleSystem.Burst[0]);var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=25;shape.radius=.07f;shape.rotation=new Vector3(-90,0,0);var lights=ps.lights;lights.enabled=false;var sub=ps.subEmitters;sub.enabled=false;var trails=ps.trails;trails.enabled=false;var noise=ps.noise;noise.enabled=false;var collision=ps.collision;collision.enabled=false;var velocity=ps.velocityOverLifetime;velocity.enabled=false;var force=ps.forceOverLifetime;force.enabled=false;var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterials=renderer.sharedMaterials.Where(m=>m).Select(m=>Convert(m,true)).ToArray();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.maxParticleSize=.04f;}
   site.AddComponent<DistrictAtmosphere>();used.Add(Source(name));
  }
  static void Street(){
   Group("Evacuation parking");Bay(11,-16,6,7);var wreck=Prop("SM_Prop_Car_Wrecked_OpenBoot_01",11,-16,4.8f,75);Prop("SM_Prop_Cone_01",7.7f,-18,.65f);Prop("SM_Prop_Cone_01",14.5f,-18,.65f);Prop("SM_Prop_Papers_02",10,-19,2.3f,25,false);Prop("SM_Prop_ShoppingCart_01",15,-15,1.4f,30);Prop("SM_Prop_TrashBag_01",15,-17,.7f);Prop("SM_Prop_Crate_Open_02",13.5f,-20,1.1f,15);Effect("Fx_RoadFlare_01",new Vector3(8,.1f,-18.5f),false);
   Group("Abandoned street waiting area");Prop("SM_Prop_ParkBench_Broken_01",-10,17,2.8f,90);Prop("SM_Prop_VendingMachine_01",-13,17,2.2f,100);Prop("SM_Prop_RubbishBin_02",-8,17,1.15f);Prop("SM_Prop_Papers_03",-11,15,2.3f,40,false);Prop("SM_Prop_Sign_MissingPapers_01",-8,20,1.8f,130);Prop("SM_Prop_TrashPile_05",-14,18,2.1f,20,false);
   Group("Shuttered shop sidewalk");Prop("SM_Prop_VendingMachine_01",15,8,2.1f,210);Prop("SM_Prop_RubbishBin_02",13,8,1.1f);Prop("SM_Prop_ShoppingCart_Broken_01",16,11,1.6f,25);Prop("SM_Prop_Papers_02",12,10,1.7f,20,false);Prop("SM_Env_Overgrowth_06",17,10,1.8f,30,false);
   Group("Bus accident debris");Prop("SM_Prop_TrashBag_01",-10,-8,.8f,45);Prop("SM_Prop_Cone_01",-6,-7,.65f);Prop("SM_Prop_Papers_03",-12,-8,2.6f,40,false);Prop("SM_Env_RoadPiece_Damaged_07",5,12,3.2f,20,false);Prop("SM_Env_RoadPiece_Damaged_09",-4,-17,3,90,false);
  }
  static void Camp(){
   Group("Field triage point");var bed=Prop("SM_Prop_Bed_Gurney_01",9,2.5f,2.4f,90);Prop("SM_Prop_Bed_Hospital_01",12,4,2.5f,90);Prop("SM_Prop_Medical_IVStand_01",11,1,1.9f);Prop("SM_Prop_Medical_Shelf_01",14,4,2.1f,90);Prop("SM_Prop_WheelChair_01",9,5.5f,1.25f,30);Prop("SM_Prop_Papers_02",10,4,2.1f,25,false);Prop("SM_Prop_Sign_Hospital_01",8,8,1.65f,130);Mark("Medical apron left",8,3,.13f,8,0,new Color(.52f,.67f,.64f));Mark("Medical apron front",11,-1,6,.13f,0,new Color(.52f,.67f,.64f));
   Group("Triage overflow shelter");Prop("SM_Prop_Bed_Gurney_01",-10,7,2.3f,0);Prop("SM_Prop_Chair_Folding_01",-9,10,1,15);Prop("SM_Prop_Chair_Folding_01",-10.4f,11.5f,1,25);Prop("SM_Prop_Medical_Container_Broken_01",-11,4.5f,1.1f,35);Prop("SM_Prop_Papers_03",-9,8.5f,2.4f,30,false);Prop("SM_Prop_Sign_Emergency_01",-11,13,1.7f,130);
   Group("Abandoned ambulance station");Bay(-11,-28,7,9);Prop("SM_Veh_Ambulance_01",-11,-28,5.8f,5);Prop("SM_Prop_Bed_Gurney_01",-10,-22,2.3f,35);Prop("SM_Prop_Medical_IVStand_01",-12,-22,1.9f);Prop("SM_Prop_Cone_01",-7,-24,.7f);Prop("SM_Prop_Cone_01",-7,-30,.7f);Prop("SM_Prop_Papers_02",-10,-24,2.6f,45,false);Effect("Fx_RoadFlare_01",new Vector3(-7,.1f,-28),false);
   Group("Checkpoint service area");Prop("SM_Prop_Floodlights_01",26,8,5.4f,220);Prop("SM_Prop_Barricade_Concrete_01",27,6,2.8f,90);Prop("SM_Prop_Chair_Folding_01",27,11,1,20);Prop("SM_Prop_ToolBox_01",28,13,.9f);Prop("SM_Prop_Crate_Open_02",25,13,1.4f,15);Prop("SM_Prop_Papers_03",26,10,2.3f,50,false);Effect("Fx_Sparks_01",new Vector3(26,.6f,-19),true);
  }
  static void Freight(){
   Group("Spilled loading pallet");Bay(11,-6,7,8);Prop("SM_Prop_Pallet_03",10,-6,2.6f,15);Prop("SM_Prop_Crate_Large_02",13,-7,2.5f,8);Prop("SM_Prop_Crate_Open_02",8.5f,-8,1.2f,35);Prop("SM_Prop_Crate_Open_04",10,-3,1.4f,60);Prop("SM_Prop_Papers_03",10,-9,2.8f,20,false);Prop("SM_Prop_Cone_01",7,-5,.75f);Prop("SM_Prop_Sign_Caution_01",14,-4,1.7f,120);
   Group("Warehouse packing station");Bay(-10,8,6,8);Prop("SM_Prop_Spool_01",-10,8,2.2f,0);Prop("SM_Prop_Pallet_03",-11,5,2.4f,10);Prop("SM_Prop_Crate_Open_02",-8,5,1.3f,25);Prop("SM_Prop_ToolBox_01",-8,10,1,10);Prop("SM_Prop_BarrelStack_02",-10,12,2.2f,20);Prop("SM_Prop_Papers_02",-10,6,2.7f,20,false);
   Group("Tanker repair station");Prop("SM_Prop_Spool_01",-21,-29,2.5f,45);Prop("SM_Prop_ToolBox_01",-23,-31,1.1f,40);Prop("SM_Prop_Tool_Bucket_01",-23,-28,.7f,0);Prop("SM_Prop_Sign_Repairs_01",-20,-32,1.8f,110);Prop("SM_Prop_Cone_01",-20,-27,.8f);Prop("SM_Prop_Pallet_03",-21,-35,2.8f,25);Prop("SM_Prop_Papers_03",-22,-30,2.4f,30,false);Effect("Fx_Sparks_01",new Vector3(-24,.7f,-31),true);
   Group("Damaged electrical cabinet");var cabinet=Prop("SM_Prop_Generator_01",11,22,2.8f,90);Prop("SM_Prop_Floodlights_01",14,24,5.8f,220);Prop("SM_Prop_ToolBox_01",9,24,1,25);Prop("SM_Prop_Cone_01",9,21,.8f);Prop("SM_Prop_Sign_Caution_01",12,25,1.7f,110);if(cabinet)Effect("Fx_Sparks_01",cabinet.transform.position+Vector3.up*.9f,true);Prop("SM_Prop_Papers_02",11,23,2,45,false);
   Group("Worn freight approaches");Prop("SM_Env_RoadPiece_Damaged_09",4,15,3.4f,30,false);Prop("SM_Env_RoadPiece_Damaged_07",-5,-13,3.8f,120,false);Bay(-10,35,7,8);Bay(12,-35,7,8);
  }
  static Vector3[] Reserved(int index){var c=ChapterCatalog.Get(index);var list=new List<Vector3>{Vector3.zero,new Vector3(3,0,3),new Vector3(-3,0,5),new Vector3(6,0,-3),new Vector3(-5,0,-4)};foreach(var a in new[]{c.worldCrates,c.worldExplosives,c.weaponSites,c.supplyRoutes})if(a!=null)list.AddRange(a);
   if(index==0){list.AddRange(new[]{new Vector3(7,0,9),new Vector3(-9,0,-11),new Vector3(-7,0,11),new Vector3(-9,0,13),new Vector3(7,0,-13),new Vector3(10,0,24),new Vector3(4,0,1),new Vector3(12,0,6),new Vector3(-12,0,-6),new Vector3(8,0,6),new Vector3(-8,0,-6),new Vector3(16,0,-8),new Vector3(-16,0,8)});foreach(int side in new[]{-1,1})foreach(int z in new[]{-24,0,24})list.Add(new Vector3(side*24.5f,0,z));}
   return list.ToArray();
  }
  public static void Apply(){Directory.CreateDirectory(Folder+"Materials");AssetDatabase.Refresh();used.Clear();report.Clear();
   for(int i=0;i<3;i++){var scene=EditorSceneManager.OpenScene(ChapterCatalog.ScenePaths[i]);chapter=i+1;config=ChapterCatalog.Get(i);reserved=Reserved(i);var surface=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();if(!surface)throw new Exception("Missing navigation");var oldRoot=GameObject.Find(RootName);if(oldRoot){UnityEngine.Object.DestroyImmediate(oldRoot);surface.BuildNavMesh();}accessible.Clear();foreach(var p in reserved){var before=new NavMeshPath();if(NavMesh.SamplePosition(p,out var h,1.5f,NavMesh.AllAreas)&&NavMesh.CalculatePath(Vector3.zero,h.position,NavMesh.AllAreas,before)&&before.status==NavMeshPathStatus.PathComplete)accessible.Add(h.position);else report.Add("BASELINE_RELOCATED chapter="+chapter+" pickup="+p);}root=new GameObject(RootName).transform;root.SetParent(surface.transform,false);placed=skipped=0;Physics.SyncTransforms();if(i==0)Street();else if(i==1)Camp();else Freight();
    surface.BuildNavMesh();string path=Folder+"DressedChapter"+chapter+"NavMesh.asset";var nav=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);if(nav){EditorUtility.CopySerialized(surface.navMeshData,nav);surface.RemoveData();surface.navMeshData=nav;surface.AddData();EditorUtility.SetDirty(nav);}else AssetDatabase.CreateAsset(surface.navMeshData,path);
    Validate();EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   }
   PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.iOS.buildNumber="47";File.WriteAllText(Folder+"placement-report.txt",string.Join("\n",report));File.WriteAllText(Folder+"used-assets.txt",string.Join("\n",used.OrderBy(x=>x)));AssetDatabase.SaveAssets();Debug.Log("SCENE_DRESSING_READY sourcePrefabs="+used.Count);
  }
  static void Validate(){int triangles=root.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh?m.sharedMesh.triangles.Length/3:0);int particles=root.GetComponentsInChildren<ParticleSystem>(true).Sum(p=>p.main.maxParticles);if(placed<18||triangles>65000||particles>160||root.GetComponentsInChildren<Light>().Length!=0)throw new Exception("Dressing budget invalid chapter="+chapter+" placed="+placed+" triangles="+triangles+" particles="+particles);
   var path=new NavMeshPath();foreach(var p in accessible){if(!NavMesh.SamplePosition(p,out var hit,1.5f,NavMesh.AllAreas)||!NavMesh.CalculatePath(Vector3.zero,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Pickup unreachable chapter="+chapter+" "+p);foreach(var c in root.GetComponentsInChildren<BoxCollider>())if(c.bounds.SqrDistance(p+Vector3.up*.6f)<.7f*.7f)throw new Exception("Pickup obstructed "+p);}
   foreach(var p in Routes(chapter))if(!NavMesh.CalculatePath(Vector3.zero,p,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Main route disconnected "+p);
   string result="DRESSING_VALID chapter="+chapter+" props="+placed+" skipped="+skipped+" triangles="+triangles+" maxParticles="+particles;report.Add(result);Debug.Log(result);
  }
  static Vector3[] Routes(int c){if(c==1)return new[]{new Vector3(0,0,16),new Vector3(19,0,16),new Vector3(19,0,-15),new Vector3(-20,0,-15),new Vector3(-20,0,16)};if(c==2)return new[]{new Vector3(0,0,36),new Vector3(36,0,12),new Vector3(28,0,-20),new Vector3(0,0,-36),new Vector3(-36,0,-12),new Vector3(-28,0,20)};return new[]{new Vector3(0,0,43),new Vector3(43,0,0),new Vector3(0,0,-43),new Vector3(-43,0,0),new Vector3(-8,0,-20),new Vector3(8,0,22)};}
  public static void ApplyAndBuild(){Apply();PrototypeSetup.Build();}
 }
}
