using UnityEngine;
namespace DeadDistrict {
 // Sample at spawn time: enemies already in combat never gain health mid-fight.
 public readonly struct HordePressure {
  public readonly float health,damage,normalSpeed,runnerSpeed,bruteSpeed,hordeInterval,trickleInterval,runnerChance,bruteChance;
  public readonly int fronts;
  public HordePressure(float elapsed,SurvivalConfig config,int wave=0){
   float progress=Mathf.Clamp01((elapsed-30)/270);
   health=1+.5f*progress;damage=1+.6f*progress;
   normalSpeed=.8f*progress;runnerSpeed=1.25f*progress;bruteSpeed=.75f*progress;
   hordeInterval=Mathf.Lerp(config.hordeInterval,Mathf.Min(config.hordeInterval,20),Mathf.Clamp01((elapsed-30)/150));
   trickleInterval=Mathf.Max(.30f,.65f-elapsed*.0025f);
   bruteChance=wave<5?0:Mathf.Min(.14f,.06f+(wave-5)*.02f);
   runnerChance=wave<3?0:Mathf.Min(.20f,.08f+(wave-3)*.02f);
   fronts=elapsed<60?1:elapsed<180?2:3;
  }
  public EnemyKind ChooseKind(float roll){return roll<bruteChance?EnemyKind.Brute:roll<bruteChance+runnerChance?EnemyKind.Runner:EnemyKind.Normal;}
  // Alternate flanks across a 120-degree arc, leaving an escape side instead of a full ring.
  public static float FrontOffset(int fronts,int index){return fronts==1?0:fronts==2?(index%2==0?-.65f:.65f):(index%3-1)*1.0472f;}
 }
}
