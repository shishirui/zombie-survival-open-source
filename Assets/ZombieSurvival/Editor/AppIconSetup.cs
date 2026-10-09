using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
namespace DeadDistrict.Editor {
 public static class AppIconSetup {
  public static void Apply(){
   const string path="Assets/ZombieSurvival/Art/AppIcon/DeadDistrict-AppIcon.png";
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);
   if(importer==null)throw new Exception("Missing app icon importer");
   importer.textureType=TextureImporterType.Default;importer.maxTextureSize=2048;
   importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;
   importer.npotScale=TextureImporterNPOTScale.None;importer.alphaSource=TextureImporterAlphaSource.None;importer.SaveAndReimport();
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(texture==null||texture.width<1024||texture.width!=texture.height)throw new Exception("App icon must be square and at least 1024 pixels");
   int slots=0;foreach(var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS)){
    var icons=PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS,kind);
    foreach(var icon in icons){icon.SetTexture(texture);slots++;}
    PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS,kind,icons);
   }
   if(slots==0)throw new Exception("No iOS icon slots assigned");
   AssetDatabase.SaveAssets();Debug.Log("APP_ICON_ASSIGNED slots="+slots+" source="+texture.width+"x"+texture.height);
  }
 }
}
