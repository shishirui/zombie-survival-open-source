using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace DeadDistrict.Editor {
 public static class CharacterAssets {
  public static void Prepare(PackVisuals pack){
   const string path="Assets/TopDownEngine/Demos/Loft3D/Prefabs/AI/LoftSuitRagdollAI.prefab";
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new Exception("Purchased ragdoll prefab missing");VisualUpgrade.reused.Add(path);
   var template=source.GetComponentsInChildren<Animator>(true).First(a=>a.isHuman);
   pack.corpses=new GameObject[2];
   for(int variant=0;variant<2;variant++){
    var root=UnityEngine.Object.Instantiate(pack.infected[variant]);VisualUpgrade.Strip(root,false);
    var animator=root.GetComponentInChildren<Animator>();var visual=root.AddComponent<CorpseVisual>();visual.bones=new Transform[(int)HumanBodyBones.LastBone];
    var bodyMap=new Dictionary<Rigidbody,Rigidbody>();var boneMap=new Dictionary<Transform,Transform>();
    for(int i=0;i<visual.bones.Length;i++){
     var src=template.GetBoneTransform((HumanBodyBones)i);var dst=animator.GetBoneTransform((HumanBodyBones)i);visual.bones[i]=dst;if(!src||!dst)continue;boneMap[src]=dst;
     var old=src.GetComponent<Rigidbody>();if(!old)continue;
     var rb=dst.gameObject.AddComponent<Rigidbody>();EditorUtility.CopySerialized(old,rb);rb.isKinematic=true;rb.detectCollisions=false;rb.useGravity=true;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;rb.linearDamping=.8f;rb.angularDamping=1.4f;rb.maxAngularVelocity=9;dst.gameObject.layer=25;bodyMap[old]=rb;
     foreach(var collider in src.GetComponents<Collider>()){
      var target=(Collider)dst.gameObject.AddComponent(collider.GetType());EditorUtility.CopySerialized(collider,target);target.isTrigger=false;
     }
    }
    foreach(var pair in bodyMap){var old=pair.Key.GetComponent<CharacterJoint>();if(!old)continue;var joint=pair.Value.gameObject.AddComponent<CharacterJoint>();EditorUtility.CopySerialized(old,joint);joint.connectedBody=old.connectedBody&&bodyMap.TryGetValue(old.connectedBody,out var connected)?connected:null;joint.enableCollision=false;joint.enableProjection=true;joint.projectionDistance=.12f;}
    visual.bodies=bodyMap.Values.ToArray();visual.hips=visual.bones[(int)HumanBodyBones.Hips].GetComponent<Rigidbody>();visual.surfaces=root.GetComponentsInChildren<SkinnedMeshRenderer>();
    if(visual.bodies.Length<8||!visual.hips)throw new Exception("Incomplete purchased physics rig: "+visual.bodies.Length);
    foreach(var r in visual.surfaces){var skin=r as SkinnedMeshRenderer;if(skin)skin.updateWhenOffscreen=true;}
    animator.enabled=false;pack.corpses[variant]=VisualUpgrade.Save(root,variant==0?"CorpseTie":"CorpseSuit");
    Debug.Log("PURCHASED_RAGDOLL_PREPARED variant="+variant+" bodies="+visual.bodies.Length);
   }
   var config=AssetDatabase.LoadAssetAtPath<SurvivalConfig>("Assets/ZombieSurvival/Settings/SurvivalConfig.asset");config.shotgunMagazineSize=60;config.shotgunRoundsPerSecond=4.8f;EditorUtility.SetDirty(config);EditorUtility.SetDirty(pack);AssetDatabase.SaveAssets();
  }
 }
}
