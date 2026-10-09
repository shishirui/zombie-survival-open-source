using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DeadDistrict {
 public sealed partial class SurvivalGame {
  public ChapterDefinition Chapter {get;private set;}
  public bool ChapterRunning {get;private set;}
  public bool Completed {get;private set;}
  public int WavesCleared {get;private set;}
  public int PendingEnemies=>pendingHorde;
  public SurvivalEnemy Elite {get;private set;}
  public WaveDefinition CurrentWave=>ChapterRunning&&Wave>0?Chapter.waves[Mathf.Min(Wave-1,Chapter.Count-1)]:null;
  public float EnemyHealthScale=>CurrentWave==null?new HordePressure(Elapsed,config).health:CurrentWave.health;
  public float EnemyDamageScale=>CurrentWave==null?new HordePressure(Elapsed,config).damage:CurrentWave.damage;
  public int RestSuppliesPlaced {get;private set;}
  struct Order {public EnemyKind kind;public bool elite;public float angle,delay;}
  readonly List<Order> chapterOrders=new List<Order>();
  EnemyKind? directedKind;bool directedElite;float victoryAt=float.PositiveInfinity;
  public void StartChapter(){if(ChapterRunning||Completed)return;ChapterRunning=true;BeginChapterWave(1,true);}
  void BeginChapterWave(int number,bool opening=false){
   Wave=number;NextHorde=float.PositiveInfinity;hordeSpawnIndex=0;chapterOrders.Clear();
   var spec=CurrentWave;hordeAngle=UnityEngine.Random.Range(0,Mathf.PI*2);
   if(player.position.sqrMagnitude>25*25)hordeAngle=Mathf.Atan2(-player.position.z,-player.position.x);
   foreach(var group in spec.groups)for(int i=0;i<group.count;i++)chapterOrders.Add(new Order{kind=group.kind,elite=group.elite,angle=EntryAngle(group,spec,i),delay=i==0?group.delay:spec.spawnInterval+(i%spec.burstSize==0?spec.burstPause:0)});
   if(opening){SeedOpeningEnemies();hordeSpawnIndex=OpeningCount;}else Sound.Horde();
   pendingHorde=chapterOrders.Count-hordeSpawnIndex;
   nextHordeSpawn=Time.time+(opening?1.5f:chapterOrders[hordeSpawnIndex].delay);
  }
  float EntryAngle(SpawnGroup group,WaveDefinition wave,int index){
   int lanes=group.kind==EnemyKind.Normal&&!group.elite?Mathf.Clamp(Chapter.spawnLanes,1,3):1;
   // Later chapters alternate entrances per enemy, so the lanes approach together.
   int lane=(Chapter.number>=2?index:index/Mathf.Max(1,wave.burstSize))%lanes;
   float offset=lanes>1?Mathf.Lerp(-Chapter.spawnSpread,Chapter.spawnSpread,lane/(float)(lanes-1)):0;
   return (group.direction+offset)*Mathf.Deg2Rad;
  }
  void SpawnDirector(float now){
   if(Completed||Dead||Paused||!ChapterRunning)return;
   if(pendingHorde==0&&ActiveCount==0){
    if(Wave==Chapter.Count){
     WavesCleared=Wave;
     if(float.IsPositiveInfinity(victoryAt))victoryAt=Elapsed+1.1f;
     if(Elapsed>=victoryAt&&!GrenadeInFlight)CompleteChapter();
     return;
    }
    if(float.IsPositiveInfinity(NextHorde)){
     WavesCleared=Wave;NextHorde=Elapsed+WaveBreakSeconds;
     if(Chapter.waves[Wave].supplies)PlaceRestSupplies();
    }
    if(Elapsed>=NextHorde)BeginChapterWave(Wave+1);
   }
   if(pendingHorde<=0||now<nextHordeSpawn)return;
   if(Chapter.number>=2){SpawnNearbyReinforcements(now);return;}
   var order=chapterOrders[hordeSpawnIndex];directedKind=order.kind;directedElite=order.elite;
   bool spawned;
   try{spawned=TrySpawn(hordeAngle+order.angle+UnityEngine.Random.Range(-.12f,.12f))||TrySpawn(hordeAngle+order.angle+UnityEngine.Random.Range(-.6f,.6f))||TrySpawn(UnityEngine.Random.Range(0,Mathf.PI*2));}
   finally{directedKind=null;directedElite=false;}
   if(!spawned){nextHordeSpawn=now+.25f;return;}
   pendingHorde--;hordeSpawnIndex++;nextHordeSpawn=now+(pendingHorde>0?chapterOrders[hordeSpawnIndex].delay:0);
  }
  void SpawnNearbyReinforcements(float now){
   // Preserve elapsed spawn time instead of losing it to frame rounding. Bound
   // catch-up after a slow frame/pause so path checks cannot create a large spike.
   nextHordeSpawn=Mathf.Max(nextHordeSpawn,now-.06f);
   int budget=Chapter.number==2?2:3;
   for(int i=0;i<budget&&pendingHorde>0&&now>=nextHordeSpawn;i++){
    // A full pool has no spawn debt; a freed slot is available on the next frame.
    if(ActiveCount>=config.maxAlive){nextHordeSpawn=now;return;}
    var order=chapterOrders[hordeSpawnIndex];directedKind=order.kind;directedElite=order.elite;bool spawned;
    try{spawned=TrySpawn(hordeAngle+order.angle+UnityEngine.Random.Range(-.12f,.12f))||TrySpawn(hordeAngle+order.angle+UnityEngine.Random.Range(-.6f,.6f))||TrySpawn(UnityEngine.Random.Range(0,Mathf.PI*2));}
    finally{directedKind=null;directedElite=false;}
    if(!spawned){nextHordeSpawn=now+.08f;return;}
    pendingHorde--;hordeSpawnIndex++;if(pendingHorde>0)nextHordeSpawn+=chapterOrders[hordeSpawnIndex].delay;
   }
  }
  void PlaceRestSupplies(){
   foreach(var kind in new[]{SupplyKind.Medkit,SupplyKind.Grenade}){
    bool placed=false;
    for(int i=0;i<24&&!placed;i++){float angle=i*2.399963f;Vector3 p=PlayerPosition+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(3+i%3);placed=Supplies.TryPlaceRestSupply(kind,p);}
    if(placed)RestSuppliesPlaced++;
   }
  }
  void CompleteChapter(){
   if(Completed||Dead||Wave!=Chapter.Count||ActiveCount!=0||pendingHorde!=0)return;
   Completed=true;NextHorde=float.PositiveInfinity;Growth.Cancel();EndGrenadeAim(false);CancelDodge();Corpses.FreezeAll();
   Supplies.Clear();WeaponCrates.Clear();PickupMessage="";PickupMessageUntil=0;targetRing.gameObject.SetActive(false);
   bool validation=RuntimeLaunch.Arguments().Any(a=>a.EndsWith("-smoke")||a.EndsWith("-capture"));
   if(!validation||ChapterProgress.IsolatedValidation)ChapterProgress.Complete(Chapter.id,Elapsed,Kills);
   PauseForShell();Sound.Victory();hud.ShowVictory();Debug.Log("CHAPTER_COMPLETED id="+Chapter.id+" waves="+Wave+" kills="+Kills+" seconds="+Elapsed);
  }
 }
}
