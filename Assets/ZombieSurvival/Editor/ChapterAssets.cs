using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace DeadDistrict.Editor {
 public static class ChapterAssets {
  static SpawnGroup G(EnemyKind kind,int n,float direction=0,float delay=0,bool elite=false)=>new SpawnGroup{kind=kind,count=n,direction=direction,delay=delay,elite=elite};
  static WaveDefinition W(string title,string hint,float hp,float damage,bool supplies,params SpawnGroup[] groups)=>new WaveDefinition{title=title,warning=hint,health=hp,damage=damage,supplies=supplies,groups=groups};
  public static void Apply(){
   const string path="Assets/ZombieSurvival/Resources/DeadDistrict/CommercialStreet.asset";
   var c=AssetDatabase.LoadAssetAtPath<ChapterDefinition>(path);if(!c){c=ScriptableObject.CreateInstance<ChapterDefinition>();AssetDatabase.CreateAsset(c,path);}
   c.waves=new[]{
    W("街头异动","普通感染者靠近",.9f,1,false,G(EnemyKind.Normal,24)),
    W("尸群集结","密集尸群正在逼近",1,1,false,G(EnemyKind.Normal,38)),
    W("犬群突袭","感染犬混入尸群",1.05f,1.03f,false,G(EnemyKind.Normal,14,-20),G(EnemyKind.Runner,6,25,2),G(EnemyKind.Normal,14,0,1)),
    W("两翼来袭","留意两侧，保持移动",1.12f,1.08f,false,G(EnemyKind.Normal,21,-65),G(EnemyKind.Runner,4,55,1),G(EnemyKind.Normal,21,65,1)),
    W("重装入场","胖僵尸靠近，留好手雷",1.2f,1.12f,false,G(EnemyKind.Normal,15,-20),G(EnemyKind.Brute,2,0,2),G(EnemyKind.Runner,4,35,1),G(EnemyKind.Normal,15,20)),
    W("短暂喘息","附近补给已投放",1.18f,1.1f,true,G(EnemyKind.Normal,22),G(EnemyKind.Runner,2,20,2)),
    W("街区沦陷","混合尸潮，准备突围",1.3f,1.22f,false,G(EnemyKind.Normal,26,-50),G(EnemyKind.Runner,8,50,1),G(EnemyKind.Brute,3,0,1),G(EnemyKind.Normal,26,40)),
    W("街区暴君","最终精英出现，避开红圈",1.35f,1.3f,false,G(EnemyKind.Brute,1,0,0,true),G(EnemyKind.Normal,16,-35,4),G(EnemyKind.Runner,4,40,2))
   };
   c.waves[1].spawnInterval=.22f;c.waves[3].burstSize=12;c.waves[6].spawnInterval=.22f;c.waves[6].burstPause=1;
   if(c.Count!=8||c.waves.Sum(w=>w.Count)!=286||c.waves.Take(2).Any(w=>w.groups.Any(g=>g.kind!=EnemyKind.Normal))||c.waves.Take(4).Any(w=>w.groups.Any(g=>g.kind==EnemyKind.Brute))||c.waves.Sum(w=>w.groups.Where(g=>g.elite).Sum(g=>g.count))!=1)throw new Exception("Invalid chapter progression");
   EditorUtility.SetDirty(c);PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.iOS.buildNumber="42";AssetDatabase.SaveAssets();Debug.Log("CHAPTER_ASSET_PASS waves=8 enemies=286");
  }
  public static void ApplyAndBuild(){Apply();PrototypeSetup.Build();}
 }
}
