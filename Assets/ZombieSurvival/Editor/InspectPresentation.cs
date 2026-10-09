using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
namespace DeadDistrict.Editor {
 public static class InspectPresentation {
  public static void Run(){
   EditorSceneManager.OpenScene("Assets/ZombieSurvival/Scenes/DeadDistrict.unity");
   var r=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(x=>x.name.Contains("Wall")||x.name.Contains("wall")||x.name.Contains("hood")).Take(14);
   foreach(var x in r)foreach(var m in x.sharedMaterials)if(m)Debug.Log("ACTUAL_SCENE_MAT "+x.name+" "+AssetDatabase.GetAssetPath(m)+" "+m.shader.name+" "+(m.HasProperty("_BaseColor")?m.GetColor("_BaseColor").ToString():"no base color"));
  }
 }
}
