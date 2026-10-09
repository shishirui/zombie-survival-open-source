using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
namespace DeadDistrict.Editor {
 public static class BaseValidation {
  public static void Validate() {
   var scene=EditorSceneManager.OpenScene("Assets/TopDownEngine/Demos/Minimal3D/MinimalScene3D.unity");
   int missing=0; foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
   Debug.Log("BASE_SCENE missing scripts="+missing);
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{scene.path}, locationPathName=Path.GetFullPath("../../work/BaseDemo.app"),target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
   if(report.summary.result!=BuildResult.Succeeded || missing>0)throw new System.Exception("Base demo validation failed");
   Debug.Log("BASE_DEMO_BUILD_PASS");
  }
 }
}
