using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace DeadDistrict.Editor {
 public static class BruteAssets {
  public static void Build(){
   const string folder="Assets/ZombieSurvival/Resources/DeadDistrict/Brutes";System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();var pack=Resources.Load<PackVisuals>("DeadDistrict/PackVisuals");
   for(int k=0;k<Mathf.Min(2,pack.infected.Length);k++){
    var root=Object.Instantiate(pack.infected[k]);var animator=root.GetComponentInChildren<Animator>();var torso=new HashSet<Transform>();
    foreach(var id in new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.UpperChest}){var bone=animator.GetBoneTransform(id);if(bone)torso.Add(bone);}
    int index=0;foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>()){
     var mesh=Object.Instantiate(renderer.sharedMesh);mesh.name="Broad torso "+k+" "+index;var vertices=mesh.vertices;var weights=mesh.boneWeights;var matrix=root.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
     for(int i=0;i<vertices.Length;i++){var w=weights[i];float amount=(torso.Contains(renderer.bones[w.boneIndex0])?w.weight0:0)+(torso.Contains(renderer.bones[w.boneIndex1])?w.weight1:0)+(torso.Contains(renderer.bones[w.boneIndex2])?w.weight2:0)+(torso.Contains(renderer.bones[w.boneIndex3])?w.weight3:0);var v=matrix.MultiplyPoint3x4(vertices[i]);v.x*=1+amount*.9f;v.z*=1+amount*.85f;vertices[i]=matrix.inverse.MultiplyPoint3x4(v);}
     mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();string path=folder+"/Body-"+k+"-"+index+++".asset";AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(mesh,path);renderer.sharedMesh=mesh;var b=renderer.localBounds;b.Expand(b.size*.65f);renderer.localBounds=b;
    }
    PrefabUtility.SaveAsPrefabAsset(root,folder+"/Brute-"+k+".prefab");Object.DestroyImmediate(root);
   }
   AssetDatabase.SaveAssets();Debug.Log("BRUTE_ASSETS_PASS mesh thickness changed; rig and physics proportions preserved");
  }
 }
}
