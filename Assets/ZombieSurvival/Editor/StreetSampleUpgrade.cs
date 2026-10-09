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
 public static class StreetSampleUpgrade {
  const string ScenePath="Assets/ZombieSurvival/Scenes/DeadDistrict.unity";
  const string Folder="Assets/ZombieSurvival/Apocalypse/StreetSample/";
  const string RootName="Commercial street sample";
  static Transform root;
  static readonly HashSet<string> used=new HashSet<string>();
  static Bounds BoundsOf(GameObject g){var rr=g.GetComponentsInChildren<MeshRenderer>();if(rr.Length==0)throw new Exception("No mesh "+g.name);var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
  static Material Convert(Material source){
   if(!source)throw new Exception("Missing source material");
   string path=Folder+"Materials/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".mat";
   var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
   m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=source.name+" street";
   m.SetTexture("_BaseMap",source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.GetTexture("_MainTex"));
   m.SetColor("_BaseColor",source.HasProperty("_Color")?source.GetColor("_Color"):Color.white);
   m.SetFloat("_Smoothness",.12f);m.SetFloat("_Metallic",0);m.enableInstancing=true;
   if(source.HasProperty("_Mode")&&Mathf.Approximately(source.GetFloat("_Mode"),1)){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.5f);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}
   AssetDatabase.CreateAsset(m,path);return m;
  }
  static GameObject Place(string relative,Vector3 pos,float width,float yaw=0,bool solid=false){
   var path="Assets/PolygonApocalypse/Prefabs/"+relative+".prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)throw new Exception("Missing "+path);used.Add(path);
   var g=new GameObject(Path.GetFileName(relative));g.transform.SetParent(root,false);
   var model=UnityEngine.Object.Instantiate(prefab,g.transform);VisualUpgrade.Strip(model,false);
   foreach(var a in model.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);
   foreach(var l in model.GetComponentsInChildren<LODGroup>(true))UnityEngine.Object.DestroyImmediate(l);
   model.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.Euler(0,yaw,0));model.transform.localScale=Vector3.one;
   foreach(var r in model.GetComponentsInChildren<MeshRenderer>(true)){r.sharedMaterials=r.sharedMaterials.Select(Convert).ToArray();r.shadowCastingMode=ShadowCastingMode.On;}
   var b=BoundsOf(g);model.transform.localScale*=width/Mathf.Max(b.size.x,b.size.z);b=BoundsOf(g);model.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
   if(solid){b=BoundsOf(g);g.layer=8;var c=g.AddComponent<BoxCollider>();c.center=b.center;c.size=b.size;}
   g.transform.position=pos;return g;
  }
  public static void Inspect(){
   Directory.CreateDirectory(Folder+"Materials");AssetDatabase.Refresh();
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);root=new GameObject("Preview").transform;
   var light=new GameObject("Sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(48,-35,0);
   RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.65f,.65f);
   string folder=Environment.GetEnvironmentVariable("STREET_OUTPUT");Directory.CreateDirectory(folder);
   foreach(string name in new[]{"SM_Bld_Cafe_01","SM_Bld_Shop_Small_03","SM_Bld_Diner_01"}){
    var go=Place("Buildings/"+name,Vector3.zero,9);var b=BoundsOf(go);Debug.Log("STREET_SOURCE "+name+" bounds="+b+" meshes="+go.GetComponentsInChildren<MeshRenderer>().Length);
    var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=8;camera.transform.position=b.center+new Vector3(-13,16,-16);camera.transform.LookAt(b.center);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.27f,.29f);
    var rt=new RenderTexture(900,700,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(900,700,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,900,700),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),tex.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(go);
   }
   AssetDatabase.SaveAssets();
  }
  static Material Flat(string name,Color color,string texture=null,Vector2? tiling=null){
   string path=Folder+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.05f);m.enableInstancing=true;
   if(texture!=null)m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));if(tiling.HasValue)m.SetTextureScale("_BaseMap",tiling.Value);EditorUtility.SetDirty(m);return m;
  }
  static void Slab(string name,Vector3 pos,Vector3 size,Material mat){BlockVisuals.Shape(name,PrimitiveType.Cube,root,pos,size,mat);}
  static void Occlusion(GameObject go){
   var component=go.AddComponent<StreetOcclusion>();component.obstacle=go.GetComponent<BoxCollider>();
   component.surfaces=go.GetComponentsInChildren<MeshRenderer>().Select(r=>new StreetOcclusion.Surface{renderer=r,opaque=r.sharedMaterials,translucent=r.sharedMaterials.Select(m=>{
    string path=AssetDatabase.GetAssetPath(m).Replace(".mat","-ghost.mat");var ghost=AssetDatabase.LoadAssetAtPath<Material>(path);if(ghost)return ghost;
    ghost=new Material(m);ghost.SetFloat("_Surface",1);ghost.SetFloat("_Blend",0);ghost.SetFloat("_ZWrite",0);ghost.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);ghost.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);ghost.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);ghost.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);ghost.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");ghost.DisableKeyword("_ALPHAPREMULTIPLY_ON");ghost.SetOverrideTag("RenderType","Transparent");ghost.renderQueue=3000;ghost.SetShaderPassEnabled("ShadowCaster",false);AssetDatabase.CreateAsset(ghost,path);return ghost;
   }).ToArray()}).ToArray();
  }
  static void RetireOld(Transform arena){
   // Remove duplicate cover and household props only in the redesigned street area.
   foreach(var t in arena.GetComponentsInChildren<Transform>(true).ToArray()){
    if(!t||t.IsChildOf(root))continue;Vector3 p=t.position;
    bool remove=t.name=="Lane marking"||t.name=="Cross street marking"||t.name=="Crosswalk stripe"||t.name=="Street sign 隔离区";
    if(t.name=="Concrete barricade"||t.name=="Barricade warning stripe"||t.name=="Checkpoint planter")remove=true;
    if((t.name=="Car chassis"||t.name=="Car cabin"||t.name=="Pack prop LoftCar")&&Vector2.Distance(new Vector2(p.x,p.z),new Vector2(-11,8))<1)remove=true;
    if(t.name.StartsWith("Apocalypse SM_Prop_Car")&&Vector2.Distance(new Vector2(p.x,p.z),new Vector2(-11,8))<1)remove=true;
    if(t.parent&&t.parent.name=="Apocalypse district dressing"&&(t.name.Contains("Sandbag_Wall")||t.name.Contains("Barrier_Concrete_01")))remove=true;
    if(t.name.StartsWith("Pack prop")&&Mathf.Abs(p.x)<24&&p.z>-2&&p.z<16)remove=true;
    if(t.parent&&t.parent.name=="Apocalypse district dressing"&&p.x>16&&p.x<23&&p.z>7&&p.z<16)remove=true;
    if(t.name.StartsWith("Apocalypse SM_Env_Road_Patch")&&p.z>-16&&p.z<11)remove=true;
    if(remove)t.gameObject.SetActive(false);
   }
  }
  [MenuItem("Dead District/Build commercial street sample")]
  public static void Apply(){
   Directory.CreateDirectory(Folder+"Materials");AssetDatabase.Refresh();used.Clear();
   var scene=EditorSceneManager.OpenScene(ScenePath);var arena=GameObject.Find("Abandoned district");
   var prior=GameObject.Find(RootName);if(prior)UnityEngine.Object.DestroyImmediate(prior);
   root=new GameObject(RootName).transform;root.SetParent(arena.transform,false);RetireOld(arena.transform);
   var paving=Flat("Warm concrete",new Color(.57f,.55f,.48f),"Assets/ZombieSurvival/Art/District paving.png",new Vector2(5,5));
   var curb=Flat("Curb concrete",new Color(.57f,.59f,.56f));var paint=Flat("Worn ivory paint",new Color(.71f,.69f,.55f));
   var yellow=Flat("Faded lane yellow",new Color(.64f,.46f,.16f));var asphalt=Flat("Opening asphalt",new Color(.24f,.285f,.30f),"Assets/ZombieSurvival/Art/District asphalt.png",new Vector2(18,18));
   GameObject.Find("Ground").GetComponent<MeshRenderer>().sharedMaterial=asphalt;
   // Broad pedestrian aprons are flush enough to traverse without a physical curb.
   Slab("Diner pedestrian apron",new Vector3(-13,.005f,6.5f),new Vector3(12,.05f,13),paving);
   Slab("Grocery pedestrian apron",new Vector3(11,.006f,.8f),new Vector3(12,.05f,11.5f),paving);
   Slab("Diner low curb",new Vector3(-13,.035f,.0f),new Vector3(12,.12f,.2f),curb);
   Slab("Grocery low curb",new Vector3(11,.035f,-4.95f),new Vector3(12,.12f,.2f),curb);
   // Distinct low silhouettes remain visible in the opening combat camera.
   var diner=Place("Buildings/SM_Bld_Diner_01",new Vector3(-13,.035f,8),9,0,true);Occlusion(diner);
   var grocery=Place("Buildings/SM_Bld_Shop_Small_03",new Vector3(10,.035f,1.3f),9,180,true);
   Place("Buildings/SM_Bld_Diner_Sign",new Vector3(-7.3f,.035f,2),1.1f,0,true);
   var table=Place("Props/SM_Prop_Cafe_Table_01",new Vector3(-13,.035f,2.8f),1.5f,0,true);
   Place("Props/SM_Prop_Cafe_Sign_01",new Vector3(-13,BoundsOf(table).max.y+.01f,2.8f),.22f,25);
   Place("Props/SM_Prop_Cafe_Chair_01",new Vector3(-14.15f,.035f,2.8f),.65f,90);
   Place("Props/SM_Prop_Cafe_Chair_01",new Vector3(-12,.035f,2),.65f,-35);
   Place("Props/SM_Prop_FlowerPot_03",new Vector3(-17,.035f,1.3f),1.25f,15,true);
   Place("Props/SM_Prop_FlowerPot_03",new Vector3(16,.035f,-3.7f),1.2f,15,true);
   var sign=Place("Props/SM_Prop_Sign_Grocery_01",new Vector3(10,2.72f,BoundsOf(grocery).min.z-.035f),2.25f,180);
   sign.transform.SetParent(grocery.transform,true);Occlusion(grocery);
   Place("Props/SM_Prop_ShoppingCart_Broken_01",new Vector3(14,.035f,-3.4f),1.35f,25,true);
   Place("Props/SM_Prop_Crate_Open_04",new Vector3(15.7f,.035f,-1.6f),.85f,-8,true);
   Place("Props/SM_Prop_TrashPile_05",new Vector3(16,.035f,5.5f),1.7f,25);
   Place("Props/SM_Prop_TrashBag_01",new Vector3(15,.035f,5.3f),.65f,30);
   Place("Props/SM_Prop_TrashBag_01",new Vector3(-17.9f,.04f,4.8f),.65f,15);
   Place("Props/SM_Prop_Papers_01",new Vector3(-16,.065f,1.2f),1.2f,70);
   Place("Environment/SM_Env_Overgrowth_06",new Vector3(-16.8f,.04f,10),1.5f,40);
   Place("Environment/SM_Env_Overgrowth_04",new Vector3(14,.04f,4.8f),1.5f,0);
   // A crashed bus, angled barricade and rubbish form one legible curbside landmark.
   Place("Props/SM_Prop_Car_Wrecked_Bus_01",new Vector3(-12,0,-5.2f),6.8f,90,true);
   Place("Props/SM_Prop_Barrier_Concrete_Broken_01",new Vector3(-7.7f,0,-5.5f),2.0f,22,true);
   Place("Props/SM_Prop_TrashPile_02",new Vector3(-16.4f,.03f,-4.8f),1.8f,15);
   Place("Props/SM_Prop_Pallet_02",new Vector3(-16,.02f,-7),1.25f,25,true);
   Place("Props/SM_Prop_Papers_01",new Vector3(-8,.065f,-3),1.2f,30);
   Place("Props/SM_Prop_Papers_01",new Vector3(5,.065f,-5.8f),1.1f,95);
   Place("Props/SM_Prop_Sign_Stop_01",new Vector3(-5.7f,0,-1.4f),.65f,180,true);
   // Worn lane markings and parking bays explain the spaces without filling the firing lanes.
   for(int z=-36;z<=36;z+=5)if(Mathf.Abs(z)>6)foreach(float x in new[]{-.2f,.2f})Slab("Broken centre line",new Vector3(x,.022f,z),new Vector3(.10f,.01f,2.5f),yellow);
   for(int i=0;i<7;i++)Slab("Opening pedestrian crossing",new Vector3(-4.2f+i*1.4f,.032f,-3.1f),new Vector3(.65f,.012f,2.0f),paint);
   for(int i=0;i<4;i++)Slab("Parking divider",new Vector3(8.7f+i*3.2f,.03f,-9),new Vector3(.09f,.015f,5.0f),paint);
   Slab("Parking edge",new Vector3(13.5f,.03f,-11.5f),new Vector3(9.7f,.015f,.09f),paint);
   Place("Environment/SM_Env_RoadPiece_Damaged_03",new Vector3(3.8f,.027f,6.5f),1.5f,55);
   Place("Environment/SM_Env_RoadPiece_Damaged_01",new Vector3(-4,.027f,-8),1.65f,-15);
   // Static street dressing has no new realtime lights or particle systems.
   foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>()){
    if(!renderer.GetComponentInParent<StreetOcclusion>())GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.BatchingStatic);
   }
   var surface=arena.GetComponent<NavMeshSurface>();surface.BuildNavMesh();string nav=Folder+"StreetNavMesh.asset";
   var stored=AssetDatabase.LoadAssetAtPath<NavMeshData>(nav);if(stored){EditorUtility.CopySerialized(surface.navMeshData,stored);surface.navMeshData=stored;EditorUtility.SetDirty(stored);}else AssetDatabase.CreateAsset(surface.navMeshData,nav);
   PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.iOS.buildNumber="41";
   EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText(Folder+"used-assets.txt",string.Join("\n",used.OrderBy(x=>x)));
   Validate();Debug.Log("STREET_SAMPLE_APPLIED prefabs="+used.Count+" objects="+root.childCount);
  }
  public static void Validate(){
   Physics.SyncTransforms();
   var sample=GameObject.Find(RootName);if(!sample||sample.GetComponentsInChildren<StreetOcclusion>().Length!=2)throw new Exception("Storefronts missing");
   if(sample.GetComponentsInChildren<Light>().Length!=0||sample.GetComponentsInChildren<ParticleSystem>().Length!=0)throw new Exception("Unexpected visual overhead");
   int triangles=0;foreach(var r in sample.GetComponentsInChildren<MeshRenderer>()){
    foreach(var m in r.sharedMaterials)if(!m||m.shader.name!="Universal Render Pipeline/Lit")throw new Exception("Invalid street material");
    triangles+=r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3;
   }
   foreach(int side in new[]{-1,1})foreach(int z in new[]{-24,0,24}){
    var path=new NavMeshPath();if(!NavMesh.CalculatePath(Vector3.zero,new Vector3(side*21,0,z),NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Street approach blocked");
   }
   foreach(var p in new[]{Vector3.zero,new Vector3(4,0,1),new Vector3(12,0,6),new Vector3(-12,0,-6),new Vector3(7,0,9),new Vector3(-7,0,11),new Vector3(-9,0,13)}){
    // The west weapon crate may relocate to a nearby reachable point using existing SpawnNear.
    if(p==new Vector3(-12,0,-6))continue;
    if(Physics.CheckCapsule(p+Vector3.up*.4f,p+Vector3.up*1.7f,.45f,1<<8))throw new Exception("Existing spawn/prop covered "+p+" by "+string.Join(",",Physics.OverlapCapsule(p+Vector3.up*.4f,p+Vector3.up*1.7f,.45f,1<<8).Select(c=>c.name)));
   }
   if(triangles>150000)throw new Exception("Sample mesh budget exceeded "+triangles);
   Debug.Log("STREET_SAMPLE_ASSET_PASS triangles="+triangles+" objects="+sample.transform.childCount);
  }
  public static void ApplyAndBuild(){Apply();PrototypeSetup.Build();}
 }
}
