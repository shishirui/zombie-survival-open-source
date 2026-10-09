using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace DeadDistrict.Editor {
 public static class LauncherAssets {
  const string Folder="Assets/ZombieSurvival/Launcher/";const string Loft="Assets/TopDownEngine/Demos/Loft3D/Prefabs/Weapons/";
  static Bounds BoundsOf(GameObject root){var all=root.GetComponentsInChildren<MeshRenderer>();if(all.Length==0)throw new Exception("No launcher mesh");var b=all[0].bounds;foreach(var r in all)b.Encapsulate(r.bounds);return b;}
  static Material Convert(Material source){string path=Folder+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat)return mat;mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.HasProperty("_Color")?source.GetColor("_Color"):Color.white);mat.SetTexture("_BaseMap",source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.HasProperty("_MainTex")?source.GetTexture("_MainTex"):null);mat.SetFloat("_Smoothness",.2f);mat.enableInstancing=true;AssetDatabase.CreateAsset(mat,path);return mat;}
  static void Materials(GameObject o){foreach(var r in o.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(Convert).ToArray();}
  static void Strip(GameObject root){VisualUpgrade.Strip(root,true);foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true))if(ps)UnityEngine.Object.DestroyImmediate(ps.gameObject);}
  static GameObject Load(string path,Transform parent){var original=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!original)throw new Exception("Missing "+path);var model=UnityEngine.Object.Instantiate(original,parent);Strip(model);foreach(var a in model.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);model.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);model.transform.localScale=Vector3.one;Materials(model);return model;}
  static GameObject Save(GameObject root,string name){var saved=PrefabUtility.SaveAsPrefabAsset(root,Folder+name+".prefab");UnityEngine.Object.DestroyImmediate(root);return saved;}
  static Material Flat(string name,Color color){string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.18f);EditorUtility.SetDirty(m);return m;}
  static void LauncherSilhouette(Transform gun){
   // Keep the purchased stock and grip; add a broad tube and rotating drum so this
   // adaptation is recognizable as a launcher instead of the demo's shotgun-shaped model.
   var dark=Flat("LauncherDarkMetal",new Color(.11f,.14f,.14f));var olive=Flat("LauncherDrum",new Color(.24f,.28f,.20f));var bore=Flat("LauncherBore",new Color(.012f,.018f,.018f));
   var tube=BlockVisuals.Shape("Wide launch tube",PrimitiveType.Cylinder,gun,new Vector3(0,.11f,.36f),new Vector3(.22f,.31f,.22f),dark);tube.transform.localRotation=Quaternion.Euler(90,0,0);
   var drum=BlockVisuals.Shape("Rotating grenade drum",PrimitiveType.Cylinder,gun,new Vector3(0,-.065f,.055f),new Vector3(.36f,.15f,.36f),olive);drum.transform.localRotation=Quaternion.Euler(0,0,90);
   var mouth=BlockVisuals.Shape("Dark launch aperture",PrimitiveType.Cylinder,gun,new Vector3(0,.11f,.677f),new Vector3(.178f,.004f,.178f),bore);mouth.transform.localRotation=Quaternion.Euler(90,0,0);
  }
  public static void Apply(){Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var pack=Resources.Load<PackVisuals>("DeadDistrict/PackVisuals");
   var gun=new GameObject("Grenade launcher");var model=Load(Loft+"Weapons/LoftGrenadeLauncher.prefab",gun.transform);var bounds=BoundsOf(model);model.transform.localScale*=1.15f/bounds.size.z;bounds=BoundsOf(model);model.transform.position+=new Vector3(-bounds.center.x,.11f-bounds.center.y,.11f-bounds.center.z);bounds=BoundsOf(model);LauncherSilhouette(gun.transform);
   var flash=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ZombieSurvival/PackVisuals/Prefabs/EpicMuzzle.prefab"),gun.transform);flash.name="Epic Muzzle";flash.transform.localPosition=new Vector3(0,.13f,bounds.max.z+.035f);flash.transform.localRotation=Quaternion.identity;flash.transform.localScale*=.8f;pack.launcherWeapon=Save(gun,"LauncherWeapon");
   var round=new GameObject("Launcher round");var shell=Load(Loft+"Projectiles/LoftGrenade.prefab",round.transform);bounds=BoundsOf(shell);shell.transform.localScale*=.25f/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);bounds=BoundsOf(shell);shell.transform.position-=bounds.center;pack.launcherProjectile=Save(round,"LauncherProjectile");
   var crate=UnityEngine.Object.Instantiate(pack.shotgunCrate);crate.name="Launcher weapon crate";var display=UnityEngine.Object.Instantiate(pack.launcherWeapon,crate.transform);Strip(display);display.transform.localPosition=new Vector3(0,.68f,0);display.transform.localRotation=Quaternion.Euler(0,90,0);display.transform.localScale=Vector3.one*.85f;
   var tealPath=Folder+"LauncherCrateTeal.mat";var teal=AssetDatabase.LoadAssetAtPath<Material>(tealPath);if(!teal){teal=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(teal,tealPath);}teal.SetColor("_BaseColor",new Color(.08f,.55f,.62f));teal.SetFloat("_Smoothness",.15f);EditorUtility.SetDirty(teal);
   foreach(var renderer in crate.GetComponentsInChildren<MeshRenderer>()){var mats=renderer.sharedMaterials;for(int i=0;i<mats.Length;i++)if(AssetDatabase.GetAssetPath(mats[i]).Contains("ShotgunCrateGold"))mats[i]=teal;renderer.sharedMaterials=mats;}
   pack.launcherCrate=Save(crate,"LauncherCrate");EditorUtility.SetDirty(pack);var config=AssetDatabase.LoadAssetAtPath<SurvivalConfig>("Assets/ZombieSurvival/Settings/SurvivalConfig.asset");config.launcherMagazineSize=12;config.launcherRange=18;config.launcherRoundsPerSecond=1;config.launcherReloadSeconds=1.5f;config.launcherDamage=220;config.launcherRadius=3.2f;EditorUtility.SetDirty(config);
   PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.iOS.buildNumber="48";AssetDatabase.SaveAssets();File.WriteAllText(Folder+"Sources.txt",Loft+"Weapons/LoftGrenadeLauncher.prefab\n"+Loft+"Projectiles/LoftGrenade.prefab\nAdapt purchased stock/grip with wide tube and drum; reuse existing adapted EpicMuzzle, compact explosion and LoftShotgun audio at lower pitch.\nSecond and third chapter middle weapon site uses launcher; other two remain shotguns.\n");Debug.Log("LAUNCHER_ASSETS_READY");
  }
  public static void ApplyAndBuild(){Apply();PrototypeSetup.Build();}
 }
}
