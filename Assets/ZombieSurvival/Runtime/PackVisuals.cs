using UnityEngine;
namespace DeadDistrict {
 public sealed class PackVisuals : ScriptableObject {
  public GameObject survivor;
  public GameObject worldSupplyCrate,worldExplosive,worldDoor;
  public GameObject[] corpses;
  public GameObject shotgunWeapon,shotgunCrate;
  public GameObject launcherWeapon,launcherCrate,launcherProjectile,launcherExplosion;
  public GameObject[] infected;
  public GameObject[] supplies;
  public GameObject healPickup,supplyPickup,armorAura;
  public GameObject impact,enemyImpact,explosion,death,grenade,bullet;
 }
}
