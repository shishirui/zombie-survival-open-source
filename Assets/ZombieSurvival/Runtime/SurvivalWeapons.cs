using UnityEngine;
namespace DeadDistrict {
 public enum WeaponKind { Rifle, Shotgun, Launcher }
 public sealed partial class SurvivalGame {
  sealed class WeaponState {public int ammo;public float nextShot,reloadEnd;}
  readonly WeaponState rifleState=new WeaponState(),shotgunState=new WeaponState(),launcherState=new WeaponState();
  WeaponState CurrentState=>CurrentWeapon==WeaponKind.Launcher?launcherState:CurrentWeapon==WeaponKind.Shotgun?shotgunState:rifleState;
  public WeaponKind CurrentWeapon {get;private set;}=WeaponKind.Rifle;
  public bool ShotgunUnlocked {get;private set;}
  public bool LauncherUnlocked {get;private set;}
  public LauncherProjectiles Launcher {get;private set;}
  public bool WeaponAllowed(WeaponKind kind)=>kind==WeaponKind.Rifle||(kind==WeaponKind.Shotgun?Chapter.number>=2:kind==WeaponKind.Launcher&&Chapter.number>=3);
  public bool HasAlternateWeapon=>ShotgunUnlocked||LauncherUnlocked;
  public WeaponKind NextWeapon {get {for(int i=1;i<=3;i++){var k=(WeaponKind)(((int)CurrentWeapon+i)%3);if(k==WeaponKind.Rifle||(k==WeaponKind.Shotgun?ShotgunUnlocked:LauncherUnlocked))return k;}return CurrentWeapon;}}
  public ActionSymbol NextWeaponSymbol=>NextWeapon==WeaponKind.Launcher?ActionSymbol.Launcher:NextWeapon==WeaponKind.Shotgun?ActionSymbol.Shotgun:ActionSymbol.Rifle;
  public int LauncherAmmo=>launcherState.ammo;
  public int LauncherShotsFired {get;private set;}
  public string WeaponName=>CurrentWeapon==WeaponKind.Launcher?"榴弹发射器":CurrentWeapon==WeaponKind.Shotgun?"霰弹枪":"步枪";
  public int MagazineCapacity=>CurrentWeapon==WeaponKind.Launcher?config.launcherMagazineSize:CurrentWeapon==WeaponKind.Shotgun?config.shotgunMagazineSize:config.magazineSize;
  public float WeaponRange=>CurrentWeapon==WeaponKind.Launcher?config.launcherRange:CurrentWeapon==WeaponKind.Shotgun?config.shotgunRange:config.aimRange;
  public float WeaponFireRate=>CurrentWeapon==WeaponKind.Launcher?config.launcherRoundsPerSecond:CurrentWeapon==WeaponKind.Shotgun?config.shotgunRoundsPerSecond:config.roundsPerSecond;
  public float WeaponReloadSeconds=>CurrentWeapon==WeaponKind.Launcher?config.launcherReloadSeconds:CurrentWeapon==WeaponKind.Shotgun?config.shotgunReloadSeconds:config.reloadSeconds;
  public int ShotgunAmmo=>shotgunState.ammo;
  public int ShotgunShotsFired {get;private set;}
  public int PelletImpacts {get;private set;}
  public int PelletVisuals {get;private set;}
  float[] pelletDamage;Vector3[] pelletPoints,pelletDirections;
  public bool CanReload=>ready&&!Dead&&!Paused&&!Rolling&&!IsGrenadeAiming&&!Reloading&&Ammo<MagazineCapacity;
  public bool TryReload(){if(!CanReload)return false;reloadEnd=Time.time+WeaponReloadSeconds;Sound.Reload(CurrentWeapon==WeaponKind.Shotgun,WeaponReloadSeconds);return true;}
  void InitializeWeapons(){
   rifleState.ammo=config.magazineSize;shotgunState.ammo=config.shotgunMagazineSize;
   pelletDamage=new float[config.maxAlive];pelletPoints=new Vector3[config.maxAlive];pelletDirections=new Vector3[config.maxAlive];
   playerActor.AddShotgun(pack.shotgunWeapon);playerActor.AddLauncher(pack.launcherWeapon);launcherState.ammo=config.launcherMagazineSize;
   Launcher=gameObject.AddComponent<LauncherProjectiles>();Launcher.Initialize(this,pack.launcherProjectile);
  }
  public bool CanCollectShotgun=>WeaponAllowed(WeaponKind.Shotgun)&&!Dead&&!Paused&&!Rolling&&!IsGrenadeAiming&&(!ShotgunUnlocked||shotgunState.ammo<config.shotgunMagazineSize||shotgunState.reloadEnd>0);
  public bool CollectShotgun(){
   if(!CanCollectShotgun)return false;
   bool first=!ShotgunUnlocked;ShotgunUnlocked=true;shotgunState.ammo=config.shotgunMagazineSize;shotgunState.reloadEnd=0;if(CurrentWeapon==WeaponKind.Shotgun)Sound.CancelReload();
   if(first)EquipWeapon(WeaponKind.Shotgun);
   NotifyPickup(first?"获得霰弹枪 · 可切换步枪":"霰弹枪弹匣已补满");Feedback.Pickup(PlayerPosition,PlayerPosition,new Color(1,.78f,.2f),false);Sound.Pickup(false);return true;
  }
  public bool CanCollectLauncher=>WeaponAllowed(WeaponKind.Launcher)&&!Dead&&!Paused&&!Rolling&&!IsGrenadeAiming&&(!LauncherUnlocked||launcherState.ammo<config.launcherMagazineSize||launcherState.reloadEnd>0);
  public bool CollectLauncher(){if(!CanCollectLauncher)return false;bool first=!LauncherUnlocked;LauncherUnlocked=true;launcherState.ammo=config.launcherMagazineSize;launcherState.reloadEnd=0;if(CurrentWeapon==WeaponKind.Launcher)Sound.CancelReload();if(first)EquipWeapon(WeaponKind.Launcher);NotifyPickup(first?"获得榴弹发射器 · 范围爆破":"榴弹弹匣已补满");Feedback.Pickup(PlayerPosition,PlayerPosition,new Color(1,.78f,.2f),false);Sound.Pickup(false);return true;}
  public bool SwitchWeapon(){if(!HasAlternateWeapon)return false;bool changed=EquipWeapon(NextWeapon);if(changed)Sound.WeaponSwitch();return changed;}
  bool EquipWeapon(WeaponKind kind){
   if(!WeaponAllowed(kind)||(kind==WeaponKind.Shotgun&&!ShotgunUnlocked)||(kind==WeaponKind.Launcher&&!LauncherUnlocked)||Dead||Paused||Rolling||IsGrenadeAiming||kind==CurrentWeapon)return false;
   // Switching cancels a partial reload and preserves each gun's actual remaining rounds.
   CurrentState.reloadEnd=0;Sound.CancelReload();CurrentWeapon=kind;nextShot=Mathf.Max(nextShot,Time.time+.15f);
   playerActor.EquipWeapon(kind);if(playerActor.Muzzle)muzzle.position=playerActor.Muzzle.position;
   target=null;nextScan=0;NotifyPickup("已切换："+WeaponName);return true;
  }
  void FireWeapon(Vector3? propAim=null){
   if(playerActor.Muzzle)muzzle.position=playerActor.Muzzle.position;
   if(CurrentWeapon==WeaponKind.Launcher){
    var destination=propAim??target.AimPoint;
    if(Launcher.Launch(muzzle.position,destination,config.launcherDamage,config.launcherRadius)){LauncherShotsFired++;playerActor.Shoot();flash.intensity=MobilePreferences.Current.reducedFlash?1:4;cameraKick=Mathf.Max(cameraKick,.085f);Sound.LauncherShot();}
    return;
   }
   bool shotgun=CurrentWeapon==WeaponKind.Shotgun;int pellets=shotgun?config.shotgunPellets:1;
   if(shotgun)ShotgunShotsFired++;
   for(int i=0;i<pelletDamage.Length;i++)pelletDamage[i]=0;
   Vector3 from=player.position+Vector3.up*1.15f;
   Vector3 center=((propAim??(target.AimPoint))-from).normalized;
   for(int i=0;i<pellets;i++){
    Vector3 direction=Quaternion.AngleAxis(shotgun?(i-(pellets-1)*.5f)*(18f/(pellets-1)):0,Vector3.up)*center;
    Vector3 end=from+direction*WeaponRange;
    if(Physics.Raycast(from,direction,out RaycastHit hit,WeaponRange,(1<<EnemyLayer)|(1<<ObstacleLayer))){
     end=hit.point;var z=hit.collider.GetComponent<SurvivalEnemy>();
     if(z){
      // A pellet cannot damage an enemy outside the visible play area or through cover.
      if(z.Alive&&CanAutoTarget(z)){pelletDamage[z.Slot]+=shotgun?config.shotgunPelletDamage:config.bulletDamage;pelletPoints[z.Slot]=hit.point;pelletDirections[z.Slot]=direction;if(shotgun)PelletImpacts++;}
     }else {VisualEffects.Impact(hit.point,hit.normal);var prop=hit.collider.GetComponent<WorldProp>();if(prop)prop.Hit(shotgun?config.shotgunPelletDamage:config.bulletDamage);}
    }
    VisualEffects.Bullet(muzzle.position,end,shotgun);if(shotgun)PelletVisuals++;
   }
   for(int i=0;i<pelletDamage.Length;i++)if(pelletDamage[i]>0){
    var z=enemies[i];VisualEffects.EnemyImpact(pelletPoints[i],pelletDirections[i],shotgun?1.35f:1);z.ReactToHit(pelletDirections[i]);Sound.Hit();z.TakeBullet(pelletDamage[i],pelletDirections[i],shotgun);
   }
   flash.intensity=(shotgun?6:4)*(MobilePreferences.Current.reducedFlash?.25f:1);playerActor.Shoot();if(shotgun)cameraKick=Mathf.Max(cameraKick,.075f);
   Sound.Shot(shotgun);
  }
 }
}
