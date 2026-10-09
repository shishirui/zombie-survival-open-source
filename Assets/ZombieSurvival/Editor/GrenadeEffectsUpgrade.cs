using UnityEngine;
using UnityEditor;
namespace DeadDistrict.Editor {
 public static class GrenadeEffectsUpgrade {
  public static void Apply(){
   var pack=Resources.Load<PackVisuals>("DeadDistrict/PackVisuals");
   // Keep the original purchased grenade's layers and timing; enlarge only its presentation.
   pack.explosion=PresentationUpgrade.Effect("EpicGrenade","Prefabs/Combat/Explosions/GrenadeExplosion/GrenadeExplosionFire.prefab",1.65f,false);
   EditorUtility.SetDirty(pack);AssetDatabase.SaveAssets();
  }
  public static void ApplyAndBuild(){Apply();PrototypeSetup.Build();}
 }
}
