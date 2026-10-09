using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
namespace DeadDistrict.Editor {
 public static class HoundAssets {
  const string Model="Assets/ZombieSurvival/Art/InfectedHound/Dog.fbx";
  public static void Build(){
   var importer=(ModelImporter)AssetImporter.GetAtPath(Model);importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=true;importer.isReadable=true;
   var clips=importer.defaultClipAnimations;foreach(var c in clips){c.loopTime=true;c.lockRootRotation=true;c.lockRootPositionXZ=true;c.lockRootHeightY=true;}importer.clipAnimations=clips;importer.SaveAndReimport();
   const string folder="Assets/ZombieSurvival/Art/InfectedHound/";
   var animation=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
   var idle=animation.First(c=>c.name.Contains("Idle"));var walk=animation.First(c=>c.name.Contains("Walking"));
   string controllerPath=folder+"Hound.controller";AssetDatabase.DeleteAsset(controllerPath);var controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("Speed",AnimatorControllerParameterType.Float);controller.AddParameter("Fast",AnimatorControllerParameterType.Bool);
   var machine=controller.layers[0].stateMachine;var locomotion=machine.AddState("Locomotion");var tree=new BlendTree{name="Four leg locomotion",blendParameter="Speed"};AssetDatabase.AddObjectToAsset(tree,controller);tree.AddChild(idle,0);tree.AddChild(walk,.15f);locomotion.motion=tree;var running=machine.AddState("Purchased running");running.motion=tree;
   var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Infected hound ash brown"};mat.SetColor("_BaseColor",new Color(.32f,.24f,.17f));mat.SetFloat("_Smoothness",.08f);AssetDatabase.DeleteAsset(folder+"Hound.mat");AssetDatabase.CreateAsset(mat,folder+"Hound.mat");
   var root=new GameObject("Infected hound");var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Model),root.transform);model.name="Four leg body";model.transform.localRotation=Quaternion.Euler(0,90,0);
   var animator=model.GetComponent<Animator>();if(!animator)animator=model.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;animator.Rebind();animator.Update(0);idle.SampleAnimation(model,0);animator.Update(.001f);
   bool found=false;var bounds=new Bounds();foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()){var baked=new Mesh();r.BakeMesh(baked,true);foreach(var v in baked.vertices){var point=r.transform.TransformPoint(v);if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);}UnityEngine.Object.DestroyImmediate(baked);r.sharedMaterial=mat;r.updateWhenOffscreen=false;}
   float scale=1.08f/bounds.size.y;model.transform.localScale=Vector3.one*scale;model.transform.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);
   root.AddComponent<LoftActor>().Quadruped=true;
   PrefabUtility.SaveAsPrefabAsset(root,"Assets/ZombieSurvival/Resources/DeadDistrict/InfectedHound.prefab");Debug.Log("HOUND_ASSET_PASS originalBounds="+bounds+" scale="+scale+" clips="+string.Join(",",animation.Select(c=>c.name)));UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();Preview();
  }
  public static void Preview(){
   var go=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("DeadDistrict/InfectedHound"));var a=go.GetComponentInChildren<Animator>();a.Rebind();a.Update(0);a.Play("Purchased running",0,.3f);a.Update(0);
   foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()){var mesh=new Mesh();r.BakeMesh(mesh,true);Debug.Log("DOG_PREVIEW renderer="+r.bounds.ToString("F4")+" baked="+mesh.bounds.ToString("F4")+" scale="+r.transform.lossyScale);UnityEngine.Object.DestroyImmediate(mesh);}
   var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.transform.position=new Vector3(2,1.8f,2.5f);camera.transform.LookAt(new Vector3(0,.6f,0));camera.orthographic=true;camera.orthographicSize=1.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.19f,.23f);
   var light=new GameObject("Preview light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(40,-35,0);RenderSettings.ambientLight=Color.gray;
   var rt=new RenderTexture(800,800,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(800,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,800),0,0);image.Apply();System.IO.File.WriteAllBytes("/tmp/deaddistrict-dog-preview.png",image.EncodeToPNG());
  }
  public static void Inspect(){
   var importer=(ModelImporter)AssetImporter.GetAtPath(Model);importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=true;importer.SaveAndReimport();
   foreach(var c in AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>())Debug.Log("HOUND_CLIP "+c.name+" length="+c.length);
   var g=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
   foreach(var t in g.GetComponentsInChildren<Transform>())Debug.Log("HOUND_BONE "+t.name+" position="+t.position+" scale="+t.localScale);
   foreach(var r in g.GetComponentsInChildren<Renderer>())Debug.Log("HOUND_MESH "+r.name+" bounds="+r.bounds+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m.name)));
   UnityEngine.Object.DestroyImmediate(g);
  }
 }
}
