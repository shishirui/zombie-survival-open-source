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
 public static class ApocalypseUpgrade {
  const string ScenePath="Assets/ZombieSurvival/Scenes/DeadDistrict.unity";
  const string Source="Assets/PolygonApocalypse/";
  const string Output="Assets/ZombieSurvival/Apocalypse/";
  const string RootName="Apocalypse district dressing";
  static readonly Dictionary<Material,Material> Materials=new Dictionary<Material,Material>();
  static readonly HashSet<string> Used=new HashSet<string>();
  static Transform root;
  [MenuItem("Dead District/Apply Apocalypse streets (batch 1)")]
  public static void Apply(){
   Directory.CreateDirectory(Output+"Materials");AssetDatabase.Refresh();Materials.Clear();Used.Clear();
   var scene=EditorSceneManager.OpenScene(ScenePath);var arena=GameObject.Find("Abandoned district");
   if(!arena)throw new Exception("Existing arena missing; this upgrade requires the current game scene");
   var previous=GameObject.Find(RootName);if(previous)UnityEngine.Object.DestroyImmediate(previous);
   root=new GameObject(RootName).transform;root.SetParent(arena.transform,false);
   // Preserve established room entrances, cover collision, spawn rules and arena bounds.
   var existing=arena.GetComponentsInChildren<Transform>(true).ToArray();
   int car=0;
   string[] cars={"SM_Prop_Car_Wrecked_Rusted_01","SM_Prop_Car_Wrecked_OpenBoot_01","SM_Prop_Car_Wrecked_Van_01","SM_Prop_Car_Wrecked_Squished_01"};
   foreach(var t in existing){
    if(t.name=="Car chassis"){
     var pos=new Vector3(t.position.x,0,t.position.z);
     var visual=Place("Props/"+cars[car++%cars.Length],pos,4.2f);
     FitFootprint(visual,new Vector2(1.9f,4.2f));
    }
    if(t.name=="Pack prop LoftCar")Hide(t);
    if(Mathf.Abs(t.position.x)<24&&(t.name=="Pack prop LoftBookCase"||t.name=="Pack prop LoftSofa"||t.name=="Pack prop LoftDesk"||t.name=="Pack prop LoftDeskChair"||t.name=="Pack prop LoftCoffeeTable")){
     Hide(t);foreach(var c in t.GetComponentsInChildren<Collider>())c.enabled=false;
     if(t.name=="Pack prop LoftBookCase")Place("Props/SM_Prop_Barricade_Pallet_01",t.position,2.5f,90,true);
     else if(t.name=="Pack prop LoftSofa")Place("Props/SM_Prop_Barrier_Concrete_Broken_01",t.position,2.7f,90,true);
     else if(t.name=="Pack prop LoftDesk")Place("Props/SM_Prop_Crate_02",t.position,1.5f,20,true);
     else if(t.name=="Pack prop LoftCoffeeTable")Place("Props/SM_Prop_Pallet_02",t.position,1.2f,15,true);
    }
    if(t.name=="Concrete barricade"){
     Hide(t);var b=t.GetComponent<BoxCollider>().bounds;
     var v=Place("Props/SM_Prop_Barrier_Concrete_01",new Vector3(b.center.x,b.min.y,b.center.z),b.size.x);
     FitSize(v,b.size);
    }
    if(t.name=="Barricade warning stripe")Hide(t);
    if(t.name=="Loft wall module"){
     Hide(t);var b=t.GetComponent<BoxCollider>().bounds;
     var v=Place("Buildings/SM_Bld_Bunker_Wall_Concrete_x2_01",new Vector3(b.center.x,b.min.y,b.center.z),1);
     FitSize(v,b.size);
    }
    if(t.name=="Pack prop LoftLamp"||t.name=="Pack prop LoftLampSmall"){
     // Keep indoor table lamps; replace only the street's placeholder lamps.
     if(Mathf.Abs(t.position.x)<24){Hide(t);Place("Props/SM_Prop_LightPole_01",t.position,3.6f);}
    }
    if(t.name=="Checkpoint planter"){
     Hide(t);var b=t.GetComponent<BoxCollider>().bounds;
     var v=Place("Props/SM_Prop_Sandbag_Wall_01",new Vector3(b.center.x,b.min.y,b.center.z),2.4f);
     FitSize(v,b.size);
    }
    if(t.name=="Pack prop LoftPlant"&&Mathf.Abs(t.position.x)<14)Hide(t);
   }
   // Road details stay flat and non-solid; no collision lips to catch movement.
   Place("Environment/SM_Env_Road_Patch_01",new Vector3(4,.028f,8),2.8f,18);
   Place("Environment/SM_Env_Road_Patch_01",new Vector3(-5,.029f,-11),2.3f,155);
   Place("Environment/SM_Env_Road_Patch_01",new Vector3(8,.029f,-24),2.1f,70);
   Place("Environment/SM_Env_RoadPiece_Damaged_01",new Vector3(-17,.03f,19),2.6f,10);
   Place("Environment/SM_Env_RoadPiece_Damaged_03",new Vector3(17,.03f,-19),2.7f,160);
   // Readable, sparse curbside clusters, away from all six doorway approaches.
   foreach(int s in new[]{-1,1}){
    foreach(int z in new[]{-34,-12,12,34}){
     Place("Props/SM_Prop_TrashPile_"+(z>0?"01":"02"),new Vector3(s*22.3f,0,z),1.7f,s*35);
     Place("Props/SM_Prop_TrashBag_01",new Vector3(s*21.1f,0,z+1),.55f,20);
     Place("Environment/SM_Env_Overgrowth_"+(z>0?"04":"01"),new Vector3(s*23.1f,.015f,z-1.4f),1.6f,s*65);
     Place("Props/SM_Prop_Papers_01",new Vector3(s*18,.04f,z+.8f),1.1f,s*50);
    }
    Place("Props/SM_Prop_RubbishBin_01",new Vector3(s*21,0,-16),1.05f,s*90,true);
    Place("Props/SM_Prop_ShoppingCart_Broken_01",new Vector3(s*21.5f,0,-19),1.25f,s*30,true);
    Place("Props/SM_Prop_Pallet_02",new Vector3(s*22,0,17),1.3f,s*12,true);
    Place("Props/SM_Prop_TrafficLight_02",new Vector3(s*19,0,s*7),3.6f,-s*90);
    // Store fixtures are against the back wall; central loot aisle stays three metres wide.
    Place("Props/SM_Prop_ShopShelf_Long_01",new Vector3(s*34.5f,0,-28.8f),2.6f,-s*90,true);
    Place("Props/SM_Prop_ShopFridge_01",new Vector3(s*35,0,-20),2.0f,-s*90,true);
    // Distant facades lie outside the playable boundary, preserving the cutaway interiors.
    foreach(int z in new[]{-24,0,24}){
     Place("Buildings/SM_Bld_Shop_Small_"+(z==0?"02":"01"),new Vector3(s*48.5f,-.05f,z),13,s<0?90:-90);
    }
   }
   // Keep new art opaque, palette-textured and instanced; no extra realtime lights or particles.
   foreach(var t in root.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic);
   var surface=arena.GetComponent<NavMeshSurface>();surface.BuildNavMesh();
   string nav=Output+"DistrictNavMesh.asset";var old=AssetDatabase.LoadAssetAtPath<NavMeshData>(nav);
   if(old){EditorUtility.CopySerialized(surface.navMeshData,old);surface.navMeshData=old;EditorUtility.SetDirty(old);}else AssetDatabase.CreateAsset(surface.navMeshData,nav);
   EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   File.WriteAllText(Output+"used-assets.txt",string.Join("\n",Used.OrderBy(x=>x)));
   Validate();Debug.Log("APOCALYPSE_APPLIED objects="+root.childCount+" sourcePrefabs="+Used.Count+" materials="+Materials.Count);
  }
  static void Hide(Transform t){foreach(var r in t.GetComponentsInChildren<Renderer>(true))r.enabled=false;}
  static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<MeshRenderer>();if(rs.Length==0)throw new Exception("No mesh "+g.name);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
  static GameObject Place(string relative,Vector3 pos,float longest,float yaw=0,bool solid=false){
   string path=Source+"Prefabs/"+relative+".prefab";var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new Exception("Missing "+path);Used.Add(path);
   var wrapper=new GameObject("Apocalypse "+Path.GetFileName(relative));wrapper.transform.SetParent(root,false);
   var model=UnityEngine.Object.Instantiate(source,wrapper.transform);model.name="Model";VisualUpgrade.Strip(model,false);
   foreach(var a in model.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);
   foreach(var l in model.GetComponentsInChildren<LODGroup>(true))UnityEngine.Object.DestroyImmediate(l);
   model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.Euler(0,yaw,0);model.transform.localScale=Vector3.one;
   foreach(var r in model.GetComponentsInChildren<MeshRenderer>(true)){
    r.sharedMaterials=r.sharedMaterials.Select(Convert).ToArray();
    if(relative.Contains("Road_Patch")){
     var patchPath=Output+"Materials/AsphaltRepair.mat";var patch=AssetDatabase.LoadAssetAtPath<Material>(patchPath);
     if(!patch){patch=new Material(r.sharedMaterial);AssetDatabase.CreateAsset(patch,patchPath);}
     patch.SetColor("_BaseColor",new Color(.20f,.24f,.27f));EditorUtility.SetDirty(patch);r.sharedMaterial=patch;
    }r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;
   }
   var b=BoundsOf(wrapper);model.transform.localScale*=longest/Mathf.Max(b.size.x,b.size.y,b.size.z);b=BoundsOf(wrapper);model.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
   if(solid){b=BoundsOf(wrapper);wrapper.layer=8;var collider=wrapper.AddComponent<BoxCollider>();collider.center=b.center;collider.size=b.size;}
   wrapper.transform.position=pos;return wrapper;
  }
  static void FitSize(GameObject g,Vector3 size){var b=BoundsOf(g);g.transform.localScale=Vector3.Scale(g.transform.localScale,new Vector3(size.x/Mathf.Max(.001f,b.size.x),size.y/Mathf.Max(.001f,b.size.y),size.z/Mathf.Max(.001f,b.size.z)));}
  static void FitFootprint(GameObject g,Vector2 size){var b=BoundsOf(g);if(b.size.x>b.size.z){g.transform.rotation=Quaternion.Euler(0,90,0);b=BoundsOf(g);}float s=Mathf.Min(size.x/b.size.x,size.y/b.size.z);g.transform.localScale*=s;}
  static Material Convert(Material source){
   if(!source)throw new Exception("Missing source material");if(Materials.TryGetValue(source,out var mat))return mat;
   var path=Output+"Materials/"+source.name+"-"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".mat";
   mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
   var texture=source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.HasProperty("_MainTex")?source.GetTexture("_MainTex"):null;
   mat.SetTexture("_BaseMap",texture);mat.SetColor("_BaseColor",source.HasProperty("_Color")?source.GetColor("_Color"):Color.white);
   mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.12f);mat.SetFloat("_Cull",2);mat.enableInstancing=true;
   if(source.HasProperty("_Mode")&&Mathf.Approximately(source.GetFloat("_Mode"),1)){mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.5f);mat.EnableKeyword("_ALPHATEST_ON");mat.renderQueue=2450;}
   EditorUtility.SetDirty(mat);Materials[source]=mat;return mat;
  }
  public static void Validate(){
   var go=GameObject.Find(RootName);if(!go||go.transform.childCount<65)throw new Exception("Missing street dressing");
   int triangles=0;foreach(var r in go.GetComponentsInChildren<MeshRenderer>()){
    foreach(var m in r.sharedMaterials)if(!m||!m.shader||m.shader.name!="Universal Render Pipeline/Lit"||!m.GetTexture("_BaseMap"))throw new Exception("Unconverted or untextured material "+r.name);
    var f=r.GetComponent<MeshFilter>();if(f&&f.sharedMesh)triangles+=f.sharedMesh.triangles.Length/3;
   }
   foreach(var t in go.GetComponentsInChildren<Transform>())if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script");
   foreach(int s in new[]{-1,1})foreach(int z in new[]{-24,0,24}){
    var p=new NavMeshPath();if(!NavMesh.CalculatePath(new Vector3(s*21,0,z),new Vector3(s*27,0,z),NavMesh.AllAreas,p)||p.status!=NavMeshPathStatus.PathComplete)throw new Exception("Blocked room approach "+s+" "+z);
   }
   Debug.Log("APOCALYPSE_ASSET_PASS objects="+go.transform.childCount+" meshTriangles="+triangles+" solidProps="+go.GetComponentsInChildren<Collider>().Length);
  }
  public static void ApplyAndBuild(){Apply();PrototypeSetup.Build();}
 }
}
