using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DeadDistrict.Editor {
 public static class MobileBuild {
  public const string Version="0.13.0";
  [MenuItem("Dead District/Export iOS prototype")]
  public static void ExportIOS() {
   AppIconSetup.Apply();
   PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
   PlayerSettings.iOS.targetOSVersionString = "15.0";
   PlayerSettings.bundleVersion = Version;
   PlayerSettings.iOS.buildNumber = "64";
   PlayerSettings.iOS.appleEnableAutomaticSigning = false;
   PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
   PlayerSettings.allowedAutorotateToLandscapeLeft = true;
   PlayerSettings.allowedAutorotateToLandscapeRight = true;
   PlayerSettings.allowedAutorotateToPortrait = false;
   PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
   var path = Environment.GetEnvironmentVariable("DEAD_DISTRICT_IOS_OUTPUT");
   if (string.IsNullOrEmpty(path)) path = Path.GetFullPath("../DeadDistrict-iOS");
   var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
    scenes = ChapterCatalog.ScenePaths,
    target = BuildTarget.iOS,
    locationPathName = path,
    options = BuildOptions.Development
   });
   if (report.summary.result != BuildResult.Succeeded)
    throw new Exception("iOS export failed: " + report.summary.result);
   Debug.Log("PROTOTYPE_IOS_EXPORT_PASS path=" + path);
  }
 }
}
