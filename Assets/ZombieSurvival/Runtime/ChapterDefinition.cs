using System;
using UnityEngine;
namespace DeadDistrict {
 [CreateAssetMenu(menuName="Dead District/Chapter")]
 public sealed class ChapterDefinition:ScriptableObject {
  public string id="commercial-street",title="废弃商业街",sceneName="DeadDistrict";
  public float eliteHealth=4800,eliteDamage=34,eliteWindup=.7f,eliteRadius=3.2f,eliteRange=10,eliteRecovery=1.6f,eliteCooldown=3;
  public int number=1;
  public const int MaximumActiveEnemies=320;
  [Range(20,MaximumActiveEnemies)] public int maxAlive=100;
  [Range(1,3)] public int spawnLanes=1;
  [Range(0,150)] public float spawnSpread;
  public float arenaHalfSize=42;
  public bool chargeElite,acidElite,streetRooms=true;
  public string description="",victoryTitle="";
  public float acidRadius=2,acidLifetime=3,acidTickDamage=7,acidTickInterval=.7f,acidFlightTime=.22f;
  public string eliteName="街区暴君";
  public float chargeSpeed=12,chargeDistance=13,chargeWidth=1.4f;
  public Vector3[] worldCrates,worldExplosives,weaponSites,supplyRoutes;
  public WaveDefinition[] waves;
  public int Count=>waves.Length;
  public string VictoryTitle=>string.IsNullOrEmpty(victoryTitle)?number==1?"街区肃清":"营地肃清":victoryTitle;
  public string Description=>string.IsNullOrEmpty(description)?number==1?"开阔街道\n熟悉生存战斗":"帐篷营地 · 掩体绕行\n冲撞精英":description;
 }
 [Serializable] public sealed class WaveDefinition {
  public string title,warning;
  public float health=1,damage=1,spawnInterval=.3f,burstPause=1.5f;
  public int burstSize=8;
  public bool supplies;
  public SpawnGroup[] groups;
  public int Count {get {int n=0;foreach(var g in groups)n+=g.count;return n;}}
 }
 [Serializable] public sealed class SpawnGroup {
  public EnemyKind kind;
  public int count;
  public bool elite;
  public float direction,delay;
 }
}
