using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DeadDistrict.Editor {
 // Run this in an isolated source copy, not the active gameplay development checkout.
 public static class DistributionBuild {
  public static string TeamId=>Environment.GetEnvironmentVariable("DEAD_DISTRICT_APPLE_TEAM")??"";
  public static string BundleId=>Environment.GetEnvironmentVariable("DEAD_DISTRICT_BUNDLE_ID")??"com.example.deaddistrict";
  public static void ExportIOS(){
   string output=Environment.GetEnvironmentVariable("DEAD_DISTRICT_RELEASE_OUTPUT");
   string number=Environment.GetEnvironmentVariable("DEAD_DISTRICT_RELEASE_BUILD");
   if(string.IsNullOrWhiteSpace(output)||!Path.IsPathRooted(output))throw new ArgumentException("Set an absolute DEAD_DISTRICT_RELEASE_OUTPUT outside the Unity project.");
   string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
   output=Path.GetFullPath(output);
   if(output==project||output.StartsWith(project+Path.DirectorySeparatorChar))throw new ArgumentException("Release output must be outside the Unity project.");
   if(!int.TryParse(number,out int build)||build<1)throw new ArgumentException("Set a positive DEAD_DISTRICT_RELEASE_BUILD; never reuse an uploaded build number.");
   PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS,BundleId);
   PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS,ScriptingImplementation.IL2CPP);
   PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.iOS.buildNumber=number;
   PlayerSettings.iOS.appleDeveloperTeamID=TeamId;
   PlayerSettings.iOS.appleEnableAutomaticSigning=false;
   PlayerSettings.iOS.iOSManualProvisioningProfileID="";
   PlayerSettings.iOS.targetOSVersionString="15.0";
   PlayerSettings.iOS.sdkVersion=iOSSdkVersion.DeviceSDK;
   PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
   PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
   PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
    scenes=ChapterCatalog.ScenePaths,target=BuildTarget.iOS,
    locationPathName=output,options=BuildOptions.None
   });
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Release iOS export failed: "+report.summary.result);
   Debug.Log("DISTRIBUTION_IOS_EXPORT_PASS team="+TeamId+" bundle="+BundleId+" version="+MobileBuild.Version+" build="+number+" development=false output="+output);
  }
 }
}
