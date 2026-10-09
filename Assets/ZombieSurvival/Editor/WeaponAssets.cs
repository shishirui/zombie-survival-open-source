using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace DeadDistrict.Editor {
 public static class WeaponAssets {
  const string Loft="Assets/TopDownEngine/Demos/Loft3D/";
  static GameObject Load(string path){VisualUpgrade.reused.Add(Loft+path);var g=AssetDatabase.LoadAssetAtPath<GameObject>(Loft+path);if(!g)throw new Exception("Missing weapon asset "+path);return g;}
  public static void Prepare(PackVisuals pack){
   var gun=UnityEngine.Object.Instantiate(Load("Prefabs/Weapons/Weapons/LoftShotgun.prefab"));VisualUpgrade.Strip(gun);
   foreach(var animator in gun.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(animator);
   foreach(var t in gun.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="ShootFeedback").ToArray())if(t)UnityEngine.Object.DestroyImmediate(t.gameObject);
   gun.transform.localPosition=new Vector3(.17f,1.45f,.46f);gun.transform.localRotation=Quaternion.identity;gun.transform.localScale=Vector3.one*1.35f;
   VisualUpgrade.ConvertRenderers(gun);VisualUpgrade.LimitParticles(gun,true);
   // Reuse the same purchased Epic muzzle family, with a larger burst for the shotgun.
   var muzzle=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ZombieSurvival/PackVisuals/Prefabs/EpicMuzzle.prefab");
   foreach(var ps in gun.GetComponentsInChildren<ParticleSystem>(true).Where(x=>x.name.Contains("Muzzle")).ToArray())if(ps)UnityEngine.Object.DestroyImmediate(ps.gameObject);
   var flash=UnityEngine.Object.Instantiate(muzzle,gun.transform);flash.name="Epic Muzzle";flash.transform.localPosition=new Vector3(0,.18f,.78f);flash.transform.localRotation=Quaternion.identity;flash.transform.localScale*=1.4f;
   pack.shotgunWeapon=VisualUpgrade.Save(gun,"ShotgunWeapon");
   var root=new GameObject("ShotgunCrate");var crate=UnityEngine.Object.Instantiate(Load("Prefabs/ItemPickers/LoftWeaponCrateShotgun.prefab"),root.transform);VisualUpgrade.Strip(crate,false);
   foreach(var animator in crate.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(animator);
   crate.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);crate.transform.localScale=Vector3.one;VisualUpgrade.ConvertRenderers(crate);
   foreach(var renderer in crate.GetComponentsInChildren<Renderer>()){
    var materials=renderer.sharedMaterials;
    for(int i=0;i<materials.Length;i++)if(materials[i]&&AssetDatabase.GetAssetPath(materials[i]).Contains("LoftShotgunRifleAmmoMaterial")){
     const string goldPath="Assets/ZombieSurvival/PackVisuals/Materials/ShotgunCrateGold.mat";
     var gold=AssetDatabase.LoadAssetAtPath<Material>(goldPath);
     if(!gold){gold=new Material(materials[i]);AssetDatabase.CreateAsset(gold,goldPath);}else EditorUtility.CopySerialized(materials[i],gold);
     gold.SetColor("_BaseColor",new Color(.85f,.43f,.08f));EditorUtility.SetDirty(gold);materials[i]=gold;
    }
    renderer.sharedMaterials=materials;
   }
   var rr=crate.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)).ToArray();var bounds=rr[0].bounds;foreach(var r in rr)bounds.Encapsulate(r.bounds);
   crate.transform.localScale*=1.6f/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);bounds=rr[0].bounds;foreach(var r in rr)bounds.Encapsulate(r.bounds);
   crate.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
   pack.shotgunCrate=VisualUpgrade.Save(root,"ShotgunCrate");EditorUtility.SetDirty(pack);AssetDatabase.SaveAssets();
  }
 }
}
