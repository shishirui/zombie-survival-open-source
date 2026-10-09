using UnityEngine;
namespace DeadDistrict {
 [CreateAssetMenu(menuName="Dead District/Survival Config")]
 public sealed class SurvivalConfig : ScriptableObject {
  [Header("Player and rifle")]
  public float moveSpeed=5.8f, aimRange=20f, roundsPerSecond=12f, bulletDamage=26f, reloadSeconds=1.0f;
  public int magazineSize=100;
  public float playerHealth=100;
  [Header("Shotgun")]
  public int shotgunMagazineSize=60,shotgunPellets=7;
  public float shotgunRange=13, shotgunRoundsPerSecond=4.8f, shotgunPelletDamage=28, shotgunReloadSeconds=1.3f;
  [Header("Grenade launcher")]
  public int launcherMagazineSize=12;
  public float launcherRange=18,launcherRoundsPerSecond=1,launcherReloadSeconds=1.5f,launcherDamage=220,launcherRadius=3.2f;
  [Header("Horde - tune on device")]
  [Range(20,ChapterDefinition.MaximumActiveEnemies)] public int maxAlive=100;
  public float enemyHealth=130, enemySpeed=2.8f, attackDamage=8, attackInterval=1.1f;
  public float firstHordeAt=10, hordeInterval=28, repathInterval=.38f;
  public int hordeSize=32;
  public float arenaHalfSize=42;
  [Header("Dodge and infected variants")]
  public float rollDistance=4.2f,rollDuration=.42f,rollCooldown=3.2f;
  public float runnerHealth=78,runnerSpeed=5.4f,bruteHealth=390,bruteSpeed=1.9f;
  [Header("Grenade")]
  public float grenadeDamage=180, grenadeRadius=5, grenadeRecharge=14;
  public int maxGrenades=2;
 }
}
