using UnityEditor;
namespace DeadDistrict.Editor {
 public static class ReadabilityBalance {
  public static void ApplyAndBuild(){
   LaterChapterDensity.Apply();
   PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.iOS.buildNumber="51";
   AssetDatabase.SaveAssets();PrototypeSetup.Build();
  }
 }
}
