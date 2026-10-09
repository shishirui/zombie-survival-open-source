using System;
using UnityEngine;
using UnityEditor;
namespace DeadDistrict.Editor {
 public static class ActionIconAssets {
  [MenuItem("Dead District/Import licensed action icons")]
  public static void Apply(){
   AssetDatabase.Refresh();
   foreach(var name in new[]{"grenade","roll","reload","rifle","shotgun","door","lock"}){
    var path="Assets/ZombieSurvival/Resources/DeadDistrict/ActionIcons/"+name+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
    if(!importer)throw new Exception("Missing icon "+name);
    importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Uncompressed;
    importer.SaveAndReimport();if(!AssetDatabase.LoadAssetAtPath<Sprite>(path))throw new Exception("Sprite import failed "+name);
   }
   PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.iOS.buildNumber="40";AssetDatabase.SaveAssets();Debug.Log("ACTION_ICONS_IMPORTED count=7 license=CC-BY-3.0");
  }
  public static void ApplyAndBuild(){Apply();PrototypeSetup.Build();}
 }
}
