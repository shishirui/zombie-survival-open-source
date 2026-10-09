using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace DeadDistrict.Editor {
 public static class LaterChapterDensity {
  public static void Configure(ChapterDefinition c){
   if(c.number==1)return;
   int[][] counts=c.number==2?new[]{new[]{192},new[]{130,4,129},new[]{115,8,115},new[]{150,2,6,149},new[]{125,7,3,124},new[]{181,2},new[]{209,10,4,209},new[]{1,159,4,2}}:new[]{new[]{419,2},new[]{288,6,288},new[]{264,3,7,264},new[]{332,8,3,332},new[]{264,9,4,263},new[]{351,2},new[]{419,12,5,419},new[]{1,288,6,3}};
   if(c.number!=2&&c.number!=3)throw new Exception("Unsupported density chapter");
   if(c.waves.Length!=8)throw new Exception("Unexpected wave count");
   bool camp=c.number==2;c.maxAlive=camp?270:320;c.spawnLanes=3;c.spawnSpread=camp?100:120;
   // Derive from the original chapter curves so repeated editor runs are idempotent.
   float[] health=camp?new[]{1.02f,1.08f,1.14f,1.2f,1.28f,1.2f,1.4f,1.44f}:new[]{1.12f,1.18f,1.26f,1.33f,1.4f,1.3f,1.5f,1.55f};
   float[] damage=camp?new[]{1.03f,1.06f,1.1f,1.14f,1.18f,1.12f,1.27f,1.33f}:new[]{1.08f,1.12f,1.18f,1.22f,1.27f,1.18f,1.34f,1.38f};
   for(int w=0;w<8;w++){
    var wave=c.waves[w];if(wave.groups.Length!=counts[w].Length)throw new Exception("Unexpected group structure");
    for(int i=0;i<wave.groups.Length;i++)wave.groups[i].count=counts[w][i];
    wave.spawnInterval=camp?.034f:.020f;wave.burstSize=camp?48:64;wave.burstPause=camp?.20f:.16f;
    wave.health=Mathf.Round(health[w]*(camp?1.12f:1.18f)*1000)/1000;
    wave.damage=Mathf.Round(damage[w]*(camp?1.18f:1.30f)*1000)/1000;
   }
   EditorUtility.SetDirty(c);
  }
  public static void Apply(){foreach(string name in new[]{"QuarantineCamp","FreightDepot"})Configure(Resources.Load<ChapterDefinition>("DeadDistrict/"+name));AssetDatabase.SaveAssets();Debug.Log("LATER_CHAPTER_DENSITY_READY camp=2040 freight=4262 aliveCaps=270/320 firstChapterUnchanged=true");}
 }
}
