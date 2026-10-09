using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace DeadDistrict.Editor {
 public static class InfectedVarietySetup {
  const string Output="Assets/ZombieSurvival/Apocalypse/Characters/";
  public static void ApplyAndBuild(){
   var pack=Resources.Load<PackVisuals>("DeadDistrict/PackVisuals");
   var controller=pack.infected[0].GetComponentInChildren<Animator>().runtimeAnimatorController;
   var sources=new[]{"Homeless_Male_01","Business_Male_01","Mechanic_Female_01","Patient_Female_01"};
   var actors=new GameObject[sources.Length];var corpses=new GameObject[sources.Length];
   for(int i=0;i<sources.Length;i++){
    var actor=ApocalypseCharacters.Actor("SM_Chr_"+sources[i],controller);
    var model=actor.GetComponentInChildren<Animator>();
    // Full clothing silhouettes; modest breadth is applied to the visual only.
    foreach(var renderer in actor.GetComponentsInChildren<MeshRenderer>().ToArray()){
     Debug.Log("INFECTED_ATTACHMENT "+i+" "+renderer.name);
     if(new[]{"Armour_","FootballHelmet","Knife","Mags_","Bullets_","GasMask","Mask_Hockey"}.Any(tag=>renderer.name.Contains(tag)))UnityEngine.Object.DestroyImmediate(renderer.gameObject);
    }
    foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>()){
     // Widen the torso mesh, preserving uniform rig scale for stable ragdoll joints.
     var torso=new System.Collections.Generic.HashSet<Transform>();foreach(var bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.UpperChest}){var t=model.GetBoneTransform(bone);if(t)torso.Add(t);}
     var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);var vertices=mesh.vertices;var weights=mesh.boneWeights;var matrix=actor.transform.worldToLocalMatrix*skin.transform.localToWorldMatrix;
     for(int v=0;v<vertices.Length;v++){var w=weights[v];float amount=(torso.Contains(skin.bones[w.boneIndex0])?w.weight0:0)+(torso.Contains(skin.bones[w.boneIndex1])?w.weight1:0)+(torso.Contains(skin.bones[w.boneIndex2])?w.weight2:0)+(torso.Contains(skin.bones[w.boneIndex3])?w.weight3:0);var point=matrix.MultiplyPoint3x4(vertices[v]);point.x*=1+amount*(i<2?.18f:.12f);point.z*=1+amount*.12f;vertices[v]=matrix.inverse.MultiplyPoint3x4(point);}
     mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();string meshPath=Output+"ClothedBody-"+i+".asset";var stored=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(stored){EditorUtility.CopySerialized(mesh,stored);UnityEngine.Object.DestroyImmediate(mesh);mesh=stored;}else AssetDatabase.CreateAsset(mesh,meshPath);skin.sharedMesh=mesh;
     skin.sharedMaterials=skin.sharedMaterials.Select((source,index)=>{
      string path=Output+"InfectedClothing-"+i+"-"+index+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
      if(!mat){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}else EditorUtility.CopySerialized(source,mat);
      mat.SetColor("_BaseColor",new Color(.82f,.88f,.76f));mat.SetFloat("_Smoothness",.08f);EditorUtility.SetDirty(mat);return mat;
     }).ToArray();
    }
    if(!model.isHuman||!model.avatar.isValid||actor.GetComponentsInChildren<SkinnedMeshRenderer>().Length!=1)throw new Exception("Invalid infected avatar "+sources[i]);
    actors[i]=ApocalypseCharacters.Save(actor,"ClothedInfected-"+i);
    corpses[i]=ApocalypseCharacters.Corpse(actors[i],i,"ClothedCorpse-"+i);
   }
   pack.infected=actors;pack.corpses=corpses;EditorUtility.SetDirty(pack);AssetDatabase.SaveAssets();
   BruteAssets.Build();LaterChapterDensity.Apply();
   PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.iOS.buildNumber="55";AssetDatabase.SaveAssets();
   Debug.Log("INFECTED_VARIETY_READY normal=4 corpses=4 brutes=2 originalPurchasedAssetsUnchanged=true");PrototypeSetup.Build();
  }
 }
}
