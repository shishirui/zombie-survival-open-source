using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class CombatFeelValidation : MonoBehaviour {
  string output;bool capture;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot(){var args=Environment.GetCommandLineArgs();bool capture=Array.IndexOf(args,"-combat-feel-capture")>=0;if(!capture&&Array.IndexOf(args,"-combat-feel-smoke")<0)return;var v=new GameObject("Opt-in combat feel validation").AddComponent<CombatFeelValidation>();v.capture=capture;int n=Array.IndexOf(args,"-validation-output");v.output=n>=0?args[n+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;v.StartCoroutine(v.Run());}
  IEnumerator Run(){
   yield return null;yield return null;var g=SurvivalGame.Instance;g.CombatValidation=true;g.AutoTest=false;g.Supplies.Clear();var z=g.Enemies[0];var forward=Camera.main.transform.forward;forward.y=0;forward.Normalize();
   if(!NavMesh.SamplePosition(g.PlayerPosition+forward*4,out var sample,.5f,NavMesh.AllAreas)){Fail("No test floor");yield break;}
   z.Spawn(sample.position,0);int start=g.ShotsFired,previous=start;var times=new List<float>();float deadline=Time.time+3;bool bars=true,fiveHits=true;
   while(z.Alive&&Time.time<deadline){
    yield return null;
    if(g.ShotsFired>previous){previous=g.ShotsFired;int hits=previous-start;times.Add(Time.time);yield return new WaitForEndOfFrame();float expected=Mathf.Max(0,130-26*hits);fiveHits&=Mathf.Abs(z.Health.CurrentHealth-expected)<.01f;
     if(hits<5)bars&=z.Alive&&g.EnemyBars.VisibleFor(z.Slot)&&Mathf.Abs(g.EnemyBars.FillFor(z.Slot)-expected/130)<.01f;
     if(capture&&hits==3){Time.timeScale=0;ScreenCapture.CaptureScreenshot(Path.Combine(output,"combat-hit-healthbar.png"));yield return new WaitForSecondsRealtime(.3f);Time.timeScale=1;if(!File.Exists(Path.Combine(output,"combat-hit-healthbar.png"))){Fail("Missing combat screenshot");yield break;}}
    }
   }
   fiveHits&=!z.Alive&&previous-start==5;yield return new WaitForEndOfFrame();bool deathHides=!g.EnemyBars.VisibleFor(z.Slot);
   if(capture){Time.timeScale=0;ScreenCapture.CaptureScreenshot(Path.Combine(output,"combat-kill-splash.png"));yield return new WaitForSecondsRealtime(.3f);Time.timeScale=1;}
   // A second stationary victim measures sustained cadence without acquisition delay.
   z.Spawn(sample.position,0);z.Health.SetHealth(10000);yield return new WaitForSeconds(.16f);bool reset=g.EnemyBars.VisibleFor(z.Slot)&&Mathf.Approximately(g.EnemyBars.FillFor(z.Slot),1);
   int shots=g.ShotsFired;float begin=Time.time;while(g.ShotsFired<shots+10&&Time.time-begin<3)yield return null;float end=Time.time;bool cadence=(g.ShotsFired-shots)>=10&&(end-begin)<1.1f;
   var body=z.transform.position+Vector3.up*1.1f;var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=8;wall.transform.position=Vector3.Lerp(Camera.main.transform.position,body,.75f);wall.transform.localScale=Vector3.one*2;yield return new WaitForSeconds(.15f);bool occluded=!g.EnemyBars.VisibleFor(z.Slot);Destroy(wall);yield return null;
   z.Retire();yield return null;bool retired=!g.EnemyBars.VisibleFor(z.Slot);
   bool originalSound=g.Sound.SfxEnabled;if(!originalSound)g.Sound.ToggleSfx();g.Sound.Explosion();yield return new WaitForSeconds(.2f);for(int i=0;i<36;i++)g.Sound.Shot();yield return new WaitForSeconds(.65f);bool tail=g.Sound.ExplosionPlaying&&g.Sound.ExplosionDuration>2;
   g.TogglePause();yield return new WaitForSecondsRealtime(.1f);bool paused=!g.Sound.ExplosionPlaying;g.TogglePause();yield return null;bool resumed=g.Sound.ExplosionPlaying;g.Sound.ToggleSfx();bool muted=!g.Sound.ExplosionPlaying;g.Sound.Explosion();muted&=!g.Sound.ExplosionPlaying;if(originalSound)g.Sound.ToggleSfx();
   if(!fiveHits||!bars||!deathHides||!reset||!cadence||!retired||!occluded||!tail||!paused||!resumed||!muted||g.VisualEffects.StainsPlayed<5){Fail($"fiveHits={fiveHits} bars={bars} deathHides={deathHides} reset={reset} cadence={cadence} elapsed={end-begin} retired={retired} occluded={occluded} tail={tail} pause={paused} resume={resumed} mute={muted}");yield break;}
   File.WriteAllText(Path.Combine(output,"combat-feel-pass.json"),$"{{\"passed\":true,\"roundsPerSecond\":{g.config.roundsPerSecond},\"enemyHealth\":{g.config.enemyHealth},\"fiveActualBulletHitsToKill\":true,\"healthBarFollowsActualDamage\":true,\"deathRetireAndRespawnBars\":true,\"tenShotsSeconds\":{(end-begin).ToString(System.Globalization.CultureInfo.InvariantCulture)},\"barsHiddenBehindWall\":true,\"persistentGroundSplashes\":true,\"explosionTailSurvives36GunSounds\":true,\"explosionPauseResumeMute\":true}}");Debug.Log("COMBAT_FEEL_PASS");Application.Quit(0);
  }
  void Fail(string reason){Time.timeScale=1;File.WriteAllText(Path.Combine(output,"combat-feel-failure.txt"),reason);Debug.LogError("COMBAT_FEEL_FAIL "+reason);Application.Quit(1);}
 }
}
