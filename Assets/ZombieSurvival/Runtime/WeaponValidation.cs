using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
namespace DeadDistrict {
 public sealed class WeaponValidation : MonoBehaviour {
  static bool started;string output;bool capture;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot(){var args=RuntimeLaunch.Arguments();bool capture=Array.IndexOf(args,"-weapon-capture")>=0;if(started||(!capture&&Array.IndexOf(args,"-weapon-smoke")<0))return;started=true;var v=new GameObject("Opt-in weapon validation").AddComponent<WeaponValidation>();DontDestroyOnLoad(v.gameObject);v.capture=capture;int n=Array.IndexOf(args,"-validation-output");v.output=n>=0?args[n+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;v.StartCoroutine(v.Run());}
  void Require(bool result,string reason){if(!result)throw new Exception(reason);}
  IEnumerator Run(){var routine=Core();while(true){object current=null;bool moved=false;string failure=null;try{moved=routine.MoveNext();if(moved)current=routine.Current;}catch(Exception e){failure=e.ToString();}if(failure!=null){Time.timeScale=1;File.WriteAllText(Path.Combine(output,"weapon-failure.txt"),failure);Debug.LogError("WEAPON_VALIDATION_FAIL "+failure);Application.Quit(1);yield break;}if(!moved)yield break;yield return current;}}
  IEnumerator Photo(string name){if(!capture)yield break;Time.timeScale=0;yield return new WaitForSecondsRealtime(.12f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name));yield return new WaitForSecondsRealtime(.25f);Time.timeScale=1;Require(File.Exists(Path.Combine(output,name)),"Screenshot missing "+name);}
  Vector3 Floor(Vector3 desired){Require(NavMesh.SamplePosition(desired,out var nav,.8f,NavMesh.AllAreas),"Missing fixture floor "+desired);return nav.position;}
  void Switch(SurvivalGame g){GameObject.Find("Switch weapon").GetComponent<Button>().onClick.Invoke();}
  IEnumerator Core(){
   yield return null;yield return null;var g=SurvivalGame.Instance;g.CombatValidation=true;g.AutoTest=false;g.Supplies.Clear();var loot=g.WeaponCrates;
   Require(g.config.shotgunMagazineSize==60,"Expected sixty-round shotgun magazine");
   Require(g.CurrentWeapon==WeaponKind.Rifle&&g.Ammo==100&&!g.ShotgunUnlocked&&!g.SwitchWeapon(),"Initial weapon state");
   Require(!loot.TrySpawn(new Vector3(999,0,999)),"Outside-map weapon crate");
   Require(loot.TrySpawn(g.PlayerPosition+new Vector3(4,0,1)),"Visible weapon crate");yield return new WaitForSeconds(.15f);yield return Photo("weapon-crate.png");loot.Clear();
   Require(loot.TrySpawn(g.PlayerPosition+Vector3.right*1.2f),"Pickup fixture");var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=8;wall.transform.position=g.PlayerPosition+Vector3.right*.6f+Vector3.up;wall.transform.localScale=new Vector3(.12f,2,2);
   yield return new WaitForSeconds(.2f);Require(!g.ShotgunUnlocked&&loot.ActiveCount==1,"Wall allowed weapon pickup");g.TogglePause();Require(!g.CollectShotgun(),"Paused pickup");yield return new WaitForSecondsRealtime(.15f);g.TogglePause();Destroy(wall);
   yield return new WaitForSeconds(.2f);Require(g.ShotgunUnlocked&&g.CurrentWeapon==WeaponKind.Shotgun&&g.Ammo==g.config.shotgunMagazineSize&&loot.Collected==1&&loot.ActiveCount==0,"Actual automatic weapon pickup");
   Require(GameObject.Find("Survivor").GetComponentInChildren<LoftActor>().ShotgunVisible,"Shotgun model did not replace rifle");yield return Photo("shotgun-equipped.png");
   var cam=Camera.main;var forward=cam.transform.forward;forward.y=0;forward.Normalize();var right=cam.transform.right;right.y=0;right.Normalize();
   // Keep the fan fixture inside the closer gameplay camera; offscreen/cover guards remain tested below.
   for(int i=0;i<3;i++){var z=g.Enemies[i];z.Spawn(Floor(g.PlayerPosition+forward*3.5f+right*(i-1)*.7f),0);z.Health.SetHealth(10000);Require(g.CanAutoTarget(z),"Fan target invisible "+i+" head="+cam.WorldToViewportPoint(z.transform.position+Vector3.up*z.VisualHeight));}
   int before=g.ShotsFired;float deadline=Time.time+2;while(g.ShotsFired==before&&Time.time<deadline)yield return null;
   int victims=0;float damage=0;for(int i=0;i<3;i++){float dealt=10000-g.Enemies[i].Health.CurrentHealth;if(dealt>0)victims++;damage+=dealt;}
   Require(g.ShotsFired==before+1&&g.Ammo==g.config.shotgunMagazineSize-1&&g.ShotgunShotsFired==1&&g.PelletVisuals==7&&victims>=2&&damage==g.PelletImpacts*28,"Fan shot/ammo/damage: victims="+victims+" damage="+damage+" pelletHits="+g.PelletImpacts);
   yield return Photo("shotgun-fan-hit.png");for(int i=0;i<3;i++)g.Enemies[i].Retire();
   Switch(g);Require(g.CurrentWeapon==WeaponKind.Rifle&&g.Ammo==100,"Rifle ammo after shotgun");
   var center=g.Enemies[1];center.Spawn(Floor(g.PlayerPosition+forward*3.5f),0);center.Health.SetHealth(10000);before=g.ShotsFired;deadline=Time.time+2;while(g.ShotsFired<before+3&&Time.time<deadline)yield return null;Require(g.ShotsFired==before+3&&g.Ammo==97,"Rifle actual firing");center.Retire();Switch(g);Require(g.CurrentWeapon==WeaponKind.Shotgun&&g.Ammo==g.config.shotgunMagazineSize-1,"Shotgun ammo not retained");
   center.Spawn(Floor(g.PlayerPosition+forward*3.5f),0);center.Health.SetHealth(10000);wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=8;wall.transform.position=g.PlayerPosition+forward*2.5f+Vector3.up;wall.transform.localScale=Vector3.one*2;yield return null;
   before=g.ShotsFired;yield return new WaitForSeconds(.9f);Require(g.ShotsFired==before&&center.Health.CurrentHealth==10000,"Shotgun firing through wall");center.Retire();Destroy(wall);yield return null;
   var offscreen=g.Enemies[4];offscreen.Spawn(Floor(g.PlayerPosition-forward*11),0);offscreen.Health.SetHealth(10000);Require(!g.CanAutoTarget(offscreen),"Offscreen fixture visible");before=g.ShotsFired;yield return new WaitForSeconds(.9f);Require(g.ShotsFired==before&&offscreen.Health.CurrentHealth==10000,"Shotgun hit offscreen enemy");offscreen.Retire();
   center.Spawn(Floor(g.PlayerPosition+forward*3.5f),0);center.Health.SetHealth(50000);deadline=Time.time+g.config.shotgunMagazineSize/g.config.shotgunRoundsPerSecond+2;float firstRapid=-1,lastRapid=-1;int rapidShots=0,lastShotCount=g.ShotsFired;
   while(!g.Reloading&&Time.time<deadline){if(g.ShotsFired>lastShotCount){if(firstRapid<0)firstRapid=Time.time;lastRapid=Time.time;rapidShots++;lastShotCount=g.ShotsFired;}yield return null;}
   float cadence=(lastRapid-firstRapid)/Mathf.Max(1,rapidShots-1);Require(rapidShots>=20&&cadence>.18f&&cadence<.24f,"Shotgun cadence incorrect: "+cadence);
   Require(g.Reloading&&g.Ammo==0&&g.ShotgunShotsFired==g.config.shotgunMagazineSize,"Shotgun actual magazine/reload");center.Retire();
   Switch(g);Require(g.CurrentWeapon==WeaponKind.Rifle&&g.Ammo==97&&!g.Reloading,"Switch during reload altered rifle");Switch(g);yield return new WaitForSeconds(.1f);Require(g.Reloading&&g.Ammo==0,"Switch refilled empty shotgun");
   Require(g.ApplySupply(SupplyKind.Ammo)&&g.Ammo==g.config.shotgunMagazineSize&&!g.Reloading,"Ammo supply shotgun refill/cancel");yield return new WaitForSeconds(2.2f);Require(g.Ammo==g.config.shotgunMagazineSize&&!g.Reloading,"Old shotgun reload timer restored state");
   loot.Clear();Require(loot.TrySpawn(g.PlayerPosition),"Full crate preservation fixture");yield return new WaitForSeconds(.2f);Require(loot.ActiveCount==1&&g.Ammo==g.config.shotgunMagazineSize,"Full shotgun consumed crate");
   g.TogglePause();Require(!g.SwitchWeapon()&&!g.CollectShotgun(),"Pause switched/collected weapon");g.TogglePause();Require(g.BeginGrenadeAim()&&!g.SwitchWeapon(),"Grenade aiming allowed switch");g.EndGrenadeAim(false);
   Switch(g);Require(g.Ammo==97,"Independent rifle magazine changed");
   // Exercise real music sources with the director suspended so it cannot overwrite pressure.
   bool musicEnabled=g.Sound.MusicEnabled;g.enabled=false;if(!musicEnabled)g.Sound.ToggleMusic();g.Sound.SetPressure(true);yield return new WaitForSecondsRealtime(1.7f);Require(g.Sound.HasBattleMusic&&g.Sound.BattleMusicVolume>.15f&&g.Sound.ExplorationMusicVolume<.01f,"Battle crossfade");
   g.Sound.SetPressure(false);yield return new WaitForSecondsRealtime(1.7f);Require(g.Sound.ExplorationMusicVolume>.15f&&g.Sound.BattleMusicVolume<.01f,"Exploration crossfade");
   g.Sound.ToggleMusic();yield return new WaitForSecondsRealtime(1.7f);Require(g.Sound.ExplorationMusicVolume<.001f&&g.Sound.BattleMusicVolume<.001f,"Music mute");if(musicEnabled)g.Sound.ToggleMusic();g.enabled=true;
   int fanHits=g.PelletImpacts,fanVisuals=g.PelletVisuals,shotSounds=g.Sound.ShotgunSoundsPlayed;Require(shotSounds==g.config.shotgunMagazineSize&&g.Sound.LoadedClips>=21,"Dedicated shotgun sounds/audio count");
   g.PlayerHealth.Kill();Require(g.Dead&&loot.ActiveCount==0&&!g.SwitchWeapon(),"Death retained loot or switch");g.Restart();yield return new WaitForSeconds(.4f);g=SurvivalGame.Instance;Require(g&&!g.Dead&&!g.ShotgunUnlocked&&g.CurrentWeapon==WeaponKind.Rifle&&g.Ammo+g.ShotsFired==100&&g.Reloads==0&&g.WeaponCrates.ActiveCount==3,"Restart weapon/crate state: unlocked="+g.ShotgunUnlocked+" weapon="+g.CurrentWeapon+" ammo="+g.Ammo+" crates="+g.WeaponCrates.ActiveCount);
   File.WriteAllText(Path.Combine(output,"weapon-pass.json"),$"{{\"passed\":true,\"automaticWeaponPickup\":true,\"wallBlocksPickup\":true,\"shotgunSevenPelletsPerShell\":true,\"fanHitsMultipleEnemies\":true,\"firstFanVictims\":{victims},\"firstFanDamage\":{damage},\"shotgunMeanShotInterval\":{cadence},\"shotgunMagazineCapacity\":60,\"shotgunMagazineShotsBeforeReload\":true,\"independentAmmo\":true,\"switchDuringReloadNoRefill\":true,\"ammoSupplyCancelsReload\":true,\"fullWeaponCratePreserved\":true,\"offscreenAndCoverProtection\":true,\"pauseGrenadeAimAndDeathGuards\":true,\"buttonSwitch\":true,\"modelSwitch\":true,\"shotgunSounds\":{shotSounds},\"pelletHits\":{fanHits},\"pelletVisuals\":{fanVisuals},\"musicCrossfadeAndMute\":true,\"restartReset\":true}}");Debug.Log("WEAPON_VALIDATION_PASS");Application.Quit(0);
  }
 }
}
