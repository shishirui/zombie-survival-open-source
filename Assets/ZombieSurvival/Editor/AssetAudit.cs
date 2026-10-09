using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
namespace DeadDistrict.Editor {
 public static class AssetAudit {
  public static void Run(){
   var b=new StringBuilder();string d="Assets/TopDownEngine/Demos/Loft3D/";
   foreach(var p in new[]{"Prefabs/PlayableCharacters/LoftSuspenders.prefab","Models/Characters/Suspenders/LoftSuspenders.fbx","Models/Characters/Tie/LoftTie.fbx","Models/Characters/Suit/LoftSuit@TPose.fbx","Prefabs/Props/LoftCar.prefab","Prefabs/Weapons/Weapons/LoftAssaultRifle.prefab","Prefabs/Weapons/Projectiles/LoftBulletImpact.prefab","Prefabs/Props/LoftExplosion.prefab","Prefabs/Props/LoftDeathVFX.prefab","Prefabs/Weapons/Projectiles/LoftAssaultRifleBullet.prefab"}){
    var g=AssetDatabase.LoadAssetAtPath<GameObject>(d+p);b.AppendLine("ASSET "+p);if(!g){b.AppendLine("MISSING");continue;}
    foreach(var a in g.GetComponentsInChildren<Animator>(true))b.AppendLine("ANIMATOR "+a.name+" human="+a.isHuman+" avatar="+a.avatar?.name+" controller="+a.runtimeAnimatorController?.name);
    foreach(var r in g.GetComponentsInChildren<Renderer>(true)){b.AppendLine("RENDER "+r.name+" bounds="+r.bounds+" pos="+r.transform.position+" scale="+r.transform.lossyScale);foreach(var m in r.sharedMaterials)if(m)b.AppendLine("MAT "+m.name+" shader="+m.shader.name+" color="+(m.HasProperty("_Color")?m.GetColor("_Color").ToString():"-")+" tex="+m.mainTexture?.name+" path="+AssetDatabase.GetAssetPath(m));}
    foreach(var t in g.GetComponentsInChildren<Transform>(true))if(t.name=="Model"||t.name=="WeaponAttachmentContainer"||t.name=="MuzzleFlare")b.AppendLine("NODE "+AnimationUtility.CalculateTransformPath(t,g.transform)+" pos="+t.localPosition+" scale="+t.localScale);
    foreach(var s in g.GetComponentsInChildren<ParticleSystem>(true))b.AppendLine("PARTICLE "+s.name+" loop="+s.main.loop+" duration="+s.main.duration+" lifetime="+s.main.startLifetime.constantMax+" max="+s.main.maxParticles);
   }
   foreach(var p in new[]{"Models/Characters/Suspenders/LoftSuspenders@RifleIdle.fbx","Models/Characters/Suspenders/LoftSuspenders@Running.fbx","Models/Characters/Tie/LoftTie@Walking.fbx","Models/Characters/Suit/LoftSuit@StandingMeleeAttackHorizontal.fbx"})foreach(var a in AssetDatabase.LoadAllAssetsAtPath(d+p).OfType<AnimationClip>())b.AppendLine("CLIP "+p+" "+a.name+" human="+a.humanMotion+" length="+a.length+" loop="+a.isLooping);
   File.WriteAllText("/tmp/deaddistrict-asset-audit.txt",b.ToString());Debug.Log("ASSET_AUDIT_PASS");
  }
 }
}
