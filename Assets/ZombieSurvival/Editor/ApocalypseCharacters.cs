using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
namespace DeadDistrict.Editor {
 public static class ApocalypseCharacters {
  const string Source="Assets/PolygonApocalypse/Prefabs/";
  const string Output="Assets/ZombieSurvival/Apocalypse/Characters/";
  static readonly Dictionary<Material,Material> Materials=new Dictionary<Material,Material>();
  [MenuItem("Dead District/Apply Apocalypse characters (batch 2)")]
  public static void Apply(){
   Directory.CreateDirectory(Output);AssetDatabase.Refresh();Materials.Clear();
   var pack=Resources.Load<PackVisuals>("DeadDistrict/PackVisuals");
   var oldPlayer=pack.survivor;var oldEnemy=pack.infected[0];
   var playerController=oldPlayer.GetComponentInChildren<Animator>().runtimeAnimatorController;
   var enemyController=oldEnemy.GetComponentInChildren<Animator>().runtimeAnimatorController;
   // Particle presets are copied intact; only the visible gun meshes and muzzle location change.
   var rifleTemplate=oldPlayer.transform.Find("Weapon").gameObject;
   var rifle=Weapon(rifleTemplate,"SM_Wep_AssaultRifle_01",false);
   var shotgun=Weapon(pack.shotgunWeapon,"SM_Wep_Shotgun_01",true);
   pack.shotgunWeapon=Save(shotgun,"ApocalypseShotgun");
   var player=Actor("SM_Chr_Cool_Male_01",playerController);
   rifle.name="Weapon";rifle.transform.SetParent(player.transform,false);
   pack.survivor=Save(player,"ApocalypseSurvivor");
   pack.infected=new[]{Save(Actor("SM_Chr_Zombie_Male_01",enemyController),"ApocalypseInfectedMale"),Save(Actor("SM_Chr_Zombie_Female_01",enemyController),"ApocalypseInfectedFemale")};
   pack.corpses=new[]{Corpse(pack.infected[0],0),Corpse(pack.infected[1],1)};
   EditorUtility.SetDirty(pack);AssetDatabase.SaveAssets();BruteAssets.Build();
   PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.iOS.buildNumber="38";AssetDatabase.SaveAssets();
   foreach(var actor in new[]{pack.survivor,pack.infected[0],pack.infected[1]}){
    var a=actor.GetComponentInChildren<Animator>();if(!a||!a.isHuman||!a.avatar.isValid)throw new Exception("Invalid retarget avatar "+actor.name);
    if(actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length!=1)throw new Exception("Unused character meshes remained "+actor.name);
    Debug.Log("APOCALYPSE_ACTOR_READY "+actor.name+" validHumanoid=true singleSkin=true");
   }
   Debug.Log("APOCALYPSE_CHARACTERS_PASS survivor=1 infected=2 weapons=2 matchedCorpses=2 brutes=2");
  }
  internal static GameObject Actor(string name,RuntimeAnimatorController controller){
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Characters/"+name+".prefab");if(!source)throw new Exception("Missing "+name);
   var root=new GameObject(name);var model=UnityEngine.Object.Instantiate(source,root.transform);model.name="Animated model";VisualUpgrade.Strip(model,false);
   // The source prefab stores every character as disabled alternatives. Strip those from the playable copy.
   foreach(var r in model.GetComponentsInChildren<Renderer>(true).Where(r=>!r.gameObject.activeInHierarchy).ToArray())if(r)UnityEngine.Object.DestroyImmediate(r.gameObject);
   foreach(var r in model.GetComponentsInChildren<Renderer>(true)){r.sharedMaterials=r.sharedMaterials.Select(Convert).ToArray();r.shadowCastingMode=ShadowCastingMode.On;}
   var a=model.GetComponent<Animator>();a.runtimeAnimatorController=controller;a.applyRootMotion=false;a.Rebind();a.Update(0);
   var b=ActualBounds(model);model.transform.localScale*=1.95f/b.size.y;b=ActualBounds(model);model.transform.localPosition-=Vector3.up*b.min.y;
   root.AddComponent<LoftActor>();return root;
  }
  static Bounds ActualBounds(GameObject root){
   bool set=false;Bounds result=new Bounds();
   foreach(var renderer in root.GetComponentsInChildren<Renderer>()){
    if(renderer is SkinnedMeshRenderer skin){var mesh=new Mesh();skin.BakeMesh(mesh);foreach(var p in mesh.vertices){var world=skin.transform.TransformPoint(p);if(!set){result=new Bounds(world,Vector3.zero);set=true;}else result.Encapsulate(world);}UnityEngine.Object.DestroyImmediate(mesh);}
    else {if(!set){result=renderer.bounds;set=true;}else result.Encapsulate(renderer.bounds);}
   }
   if(!set||result.size.y<.1f)throw new Exception("Empty model bounds");return result;
  }
  static GameObject Weapon(GameObject template,string name,bool shotgun){
   var root=UnityEngine.Object.Instantiate(template);root.name=shotgun?"ApocalypseShotgun":"Weapon";
   foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true).ToArray())if(r){var f=r.GetComponent<MeshFilter>();UnityEngine.Object.DestroyImmediate(r);if(f)UnityEngine.Object.DestroyImmediate(f);}
   // Remove empty mesh parents from earlier runs while keeping every particle object.
   var previous=root.transform.Find("Apocalypse gun mesh");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
   var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Weapons/Guns/"+name+".prefab"),root.transform);model.name="Apocalypse gun mesh";VisualUpgrade.Strip(model,false);
   foreach(var a in model.GetComponentsInChildren<Animator>())UnityEngine.Object.DestroyImmediate(a);
   model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one*(shotgun?1.45f:1);
   model.transform.localPosition=shotgun?new Vector3(0,.069f,-.279f):new Vector3(0,.022f,-.217f);
   foreach(var r in model.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(Convert).ToArray();
   var muzzle=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Epic Muzzle");muzzle.localPosition=shotgun?new Vector3(0,.15f,.492f):new Vector3(0,.15f,.512f);muzzle.localRotation=Quaternion.identity;
   root.transform.localPosition=new Vector3(.12f,1.24f,.34f);root.transform.localRotation=Quaternion.identity;root.transform.localScale=Vector3.one*1.1f;return root;
  }
  static Material Convert(Material source){
   if(!source)throw new Exception("Missing character material");if(Materials.TryGetValue(source,out var result))return result;
   // On an idempotent rebuild, an existing project material is already compatible.
   if(AssetDatabase.GetAssetPath(source).StartsWith(Output))return source;
   string path=Output+source.name+".mat";result=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!result){result=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(result,path);}
   result.SetTexture("_BaseMap",source.GetTexture("_MainTex"));result.SetColor("_BaseColor",Color.white);result.SetFloat("_Smoothness",.13f);result.SetFloat("_Metallic",0);result.enableInstancing=true;EditorUtility.SetDirty(result);Materials[source]=result;return result;
  }
  internal static GameObject Save(GameObject root,string name){var saved=PrefabUtility.SaveAsPrefabAsset(root,Output+name+".prefab");UnityEngine.Object.DestroyImmediate(root);return saved;}
  static readonly HumanBodyBones[] BodyIds={HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg};
  internal static GameObject Corpse(GameObject source,int variant,string name=null){
   var root=UnityEngine.Object.Instantiate(source);VisualUpgrade.Strip(root,false);var a=root.GetComponentInChildren<Animator>();a.Rebind();a.Update(0);
   var visual=root.AddComponent<CorpseVisual>();visual.bones=new Transform[(int)HumanBodyBones.LastBone];for(int i=0;i<visual.bones.Length;i++)visual.bones[i]=a.GetBoneTransform((HumanBodyBones)i);
   var map=new Dictionary<HumanBodyBones,Rigidbody>();
   foreach(var id in BodyIds){
    var t=a.GetBoneTransform(id);if(!t)throw new Exception("Ragdoll bone missing "+id);t.gameObject.layer=25;var rb=t.gameObject.AddComponent<Rigidbody>();rb.mass=id==HumanBodyBones.Hips?10:id==HumanBodyBones.Spine?12:id==HumanBodyBones.Head?4:3;rb.isKinematic=true;rb.detectCollisions=false;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;rb.interpolation=RigidbodyInterpolation.Interpolate;map[id]=rb;
    if(id==HumanBodyBones.Hips){var c=t.gameObject.AddComponent<BoxCollider>();c.size=new Vector3(.28f,.20f,.22f)/a.transform.localScale.x;c.center=t.InverseTransformVector(Vector3.up*.015f);}
    else if(id==HumanBodyBones.Head){var c=t.gameObject.AddComponent<SphereCollider>();c.radius=.115f/a.transform.localScale.x;c.center=t.InverseTransformVector(Vector3.up*.08f);}
    else {
     HumanBodyBones end=id==HumanBodyBones.Spine?HumanBodyBones.Neck:id==HumanBodyBones.LeftUpperArm?HumanBodyBones.LeftLowerArm:id==HumanBodyBones.LeftLowerArm?HumanBodyBones.LeftHand:id==HumanBodyBones.RightUpperArm?HumanBodyBones.RightLowerArm:id==HumanBodyBones.RightLowerArm?HumanBodyBones.RightHand:id==HumanBodyBones.LeftUpperLeg?HumanBodyBones.LeftLowerLeg:id==HumanBodyBones.LeftLowerLeg?HumanBodyBones.LeftFoot:id==HumanBodyBones.RightUpperLeg?HumanBodyBones.RightLowerLeg:HumanBodyBones.RightFoot;
     var delta=t.InverseTransformPoint(a.GetBoneTransform(end).position);int axis=Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?0:1;if(Mathf.Abs(delta.z)>Mathf.Abs(delta[axis]))axis=2;
     var c=t.gameObject.AddComponent<CapsuleCollider>();c.direction=axis;c.center=delta*.5f;c.radius=(id==HumanBodyBones.Spine?.14f:id.ToString().Contains("Leg")?.075f:.055f)/a.transform.localScale.x;c.height=Mathf.Max(c.radius*2,delta.magnitude*.88f);
    }
   }
   foreach(var id in BodyIds){if(id==HumanBodyBones.Hips)continue;
    HumanBodyBones parent=id==HumanBodyBones.Spine||id==HumanBodyBones.LeftUpperLeg||id==HumanBodyBones.RightUpperLeg?HumanBodyBones.Hips:id==HumanBodyBones.LeftLowerLeg?HumanBodyBones.LeftUpperLeg:id==HumanBodyBones.RightLowerLeg?HumanBodyBones.RightUpperLeg:id==HumanBodyBones.LeftLowerArm?HumanBodyBones.LeftUpperArm:id==HumanBodyBones.RightLowerArm?HumanBodyBones.RightUpperArm:HumanBodyBones.Spine;
    var rb=map[id];var j=rb.gameObject.AddComponent<CharacterJoint>();j.connectedBody=map[parent];j.anchor=Vector3.zero;j.autoConfigureConnectedAnchor=false;j.connectedAnchor=j.connectedBody.transform.InverseTransformPoint(rb.position);
    j.axis=rb.transform.InverseTransformDirection(root.transform.right);j.swingAxis=rb.transform.InverseTransformDirection(root.transform.forward);
    j.lowTwistLimit=new SoftJointLimit{limit=-30};j.highTwistLimit=new SoftJointLimit{limit=30};j.swing1Limit=new SoftJointLimit{limit=id==HumanBodyBones.Head?25:55};j.swing2Limit=new SoftJointLimit{limit=30};j.enableProjection=true;j.projectionDistance=.025f;j.projectionAngle=10;j.enableCollision=false;
   }
   visual.bodies=BodyIds.Select(id=>map[id]).ToArray();visual.hips=map[HumanBodyBones.Hips];visual.surfaces=root.GetComponentsInChildren<SkinnedMeshRenderer>();foreach(var r in visual.surfaces)((SkinnedMeshRenderer)r).updateWhenOffscreen=true;a.enabled=false;
   return Save(root,name??("ApocalypseCorpse"+variant));
  }
  public static void ApplyAndBuild(){Apply();PrototypeSetup.Build();}
 }
}
