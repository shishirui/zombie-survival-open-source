using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class CombatChainValidation:MonoBehaviour {
  string output;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var a=RuntimeLaunch.Arguments();if(Array.IndexOf(a,"-combat-chain-smoke")<0)return;var v=new GameObject("Opt-in late target chain validation").AddComponent<CombatChainValidation>();int n=Array.IndexOf(a,"-validation-output");v.output=n>=0?a[n+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);SurvivalGame.Instance.CombatValidation=true;v.StartCoroutine(v.Run());}
  IEnumerator Run(){yield return null;yield return null;var g=SurvivalGame.Instance;g.CombatValidation=true;g.Growth.Enabled=false;g.Supplies.Clear();g.WeaponCrates.Clear();var forward=Camera.main.transform.forward;forward.y=0;forward.Normalize();var right=Camera.main.transform.right;right.y=0;right.Normalize();var a=g.Enemies[0];var b=g.Enemies[1];
   if(!NavMesh.SamplePosition(g.PlayerPosition+forward*3.5f-right,out var spot,1,NavMesh.AllAreas)){Fail("Missing first fixture");yield break;}a.Spawn(spot.position,0);a.Health.SetHealth(10000);int before=g.ShotsFired;float deadline=Time.time+2;while(g.ShotsFired<before+3&&Time.time<deadline)yield return null;if(g.ShotsFired<before+3){Fail("Initial firing failed");yield break;}
   // A different opponent enters view only as the current victim dies, rather than waiting in view throughout.
   float previousShot=Time.time;before=g.ShotsFired;a.Health.Kill();if(!NavMesh.SamplePosition(g.PlayerPosition+forward*3.5f+right,out spot,1,NavMesh.AllAreas)){Fail("Missing second fixture");yield break;}b.Spawn(spot.position,0);b.Health.SetHealth(10000);deadline=Time.time+1;while(g.ShotsFired==before&&Time.time<deadline)yield return null;float gap=Time.time-previousShot;
   File.WriteAllText(Path.Combine(output,"chain-measurement.json"),"{\"switchGapSeconds\":"+gap.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"ammo\":"+g.Ammo+"}");
   if(g.ShotsFired!=before+1||gap>.15f||g.Reloading){Fail("Late-arriving visible target stalled continuous fire: gap="+gap+" ammo="+g.Ammo);yield break;}
   // Slow weapons must also hand off on their own cadence, without a new reaction wait on top.
   g.config.roundsPerSecond=2;before=g.ShotsFired;deadline=Time.time+1;while(g.ShotsFired==before&&Time.time<deadline)yield return null;previousShot=Time.time;before=g.ShotsFired;b.Health.Kill();yield return new WaitForSeconds(.4f);a.Spawn(spot.position,0);a.Health.SetHealth(10000);deadline=Time.time+1;while(g.ShotsFired==before&&Time.time<deadline)yield return null;float slowGap=Time.time-previousShot;if(g.ShotsFired!=before+1||slowGap>.61f){Fail("Slow weapon handoff stalled: "+slowGap);yield break;}
   File.WriteAllText(Path.Combine(output,"combat-chain-pass.json"),"{\"passed\":true,\"newTargetDuringCombatNoExtraWait\":true,\"slowWeaponHandoff\":true,\"switchGapSeconds\":"+gap.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");Debug.Log("COMBAT_CHAIN_PASS");Application.Quit(0);
  }
  void Fail(string why){File.WriteAllText(Path.Combine(output,"combat-chain-failure.txt"),why);Debug.LogError("COMBAT_CHAIN_FAIL "+why);Application.Quit(1);}
 }
}
