using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class CombatVisibilityValidation : MonoBehaviour {
  string output;bool capture;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot(){var args=RuntimeLaunch.Arguments();bool capture=Array.IndexOf(args,"-aim-visibility-capture")>=0;if(!capture&&Array.IndexOf(args,"-aim-visibility-smoke")<0)return;var v=new GameObject("Opt-in combat visibility validation").AddComponent<CombatVisibilityValidation>();v.capture=capture;int n=Array.IndexOf(args,"-validation-output");v.output=n>=0?args[n+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;v.StartCoroutine(v.Run());}
  IEnumerator Run(){
   yield return null;yield return null;var g=SurvivalGame.Instance;g.CombatValidation=true;g.AutoTest=false;SurvivalInput.Move=Vector2.zero;
   var cam=Camera.main;var z=g.Enemies[0];Vector3 forward=cam.transform.forward;forward.y=0;forward.Normalize();
   Vector3 outside=g.PlayerPosition-forward*19;
   if(!NavMesh.SamplePosition(outside,out var sample,.5f,NavMesh.AllAreas)){Fail("No walkable offscreen test point");yield break;}
   z.Spawn(sample.position,0);z.Health.SetHealth(1000);
   bool validCase=Vector3.Distance(g.PlayerPosition,z.transform.position)<g.config.aimRange&&!Physics.Linecast(g.PlayerPosition+Vector3.up*1.1f,z.transform.position+Vector3.up*1.1f,1<<8);
   if(!validCase||g.CanAutoTarget(z)){Fail("Offscreen case did not exercise the old distance-only bug");yield break;}
   int shots=g.ShotsFired;float hp=z.Health.CurrentHealth;yield return new WaitForSeconds(.7f);
   bool offscreen=g.ShotsFired==shots&&Mathf.Approximately(hp,z.Health.CurrentHealth);
   Vector3 inside=g.PlayerPosition+forward*4;
   if(!NavMesh.SamplePosition(inside,out sample,.5f,NavMesh.AllAreas)){Fail("No walkable visible point");yield break;}
   z.Agent.Warp(sample.position);yield return null;
   bool visible=g.CanAutoTarget(z);float entered=g.Elapsed;
   yield return new WaitForSeconds(.12f);bool reactionDelay=g.ShotsFired==shots;
   float deadline=Time.time+1;while(g.ShotsFired==shots&&Time.time<deadline)yield return null;
   bool resumed=g.ShotsFired>shots&&z.Health.CurrentHealth<hp;
   float firstShotAfter=g.Elapsed-entered;
   if(capture){yield return new WaitForSeconds(.025f);ScreenCapture.CaptureScreenshot(Application.isMobilePlatform?"combat-gameplay.png":Path.Combine(output,"combat-gameplay.png"));yield return new WaitForSeconds(1);if(!File.Exists(Path.Combine(output,"combat-gameplay.png"))){Fail("Combat screenshot missing");yield break;}}
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Opt-in test blocker";wall.layer=8;wall.transform.position=Vector3.Lerp(g.PlayerPosition,z.transform.position,.5f)+Vector3.up*1.2f;wall.transform.localScale=new Vector3(1,2.4f,1);Physics.SyncTransforms();
   yield return null;shots=g.ShotsFired;hp=z.Health.CurrentHealth;bool blocked=!g.CanAutoTarget(z);yield return new WaitForSeconds(.7f);bool noBlockedShots=g.ShotsFired==shots&&Mathf.Approximately(hp,z.Health.CurrentHealth);
   Destroy(wall);yield return null;z.Agent.Warp(outside);yield return null;shots=g.ShotsFired;hp=z.Health.CurrentHealth;yield return new WaitForSeconds(.7f);bool stoppedAgain=g.ShotsFired==shots&&Mathf.Approximately(hp,z.Health.CurrentHealth);
   z.Agent.Warp(inside);yield return null;shots=g.ShotsFired;yield return new WaitForSeconds(.12f);bool longAbsenceDelay=g.ShotsFired==shots;deadline=Time.time+1;while(g.ShotsFired==shots&&Time.time<deadline)yield return null;bool longAbsenceResumes=g.ShotsFired>shots;
   bool closer=Mathf.Approximately(cam.orthographicSize,SurvivalGame.GameplayCameraSize);
   if(!offscreen||!visible||!reactionDelay||!resumed||!blocked||!noBlockedShots||!stoppedAgain||!longAbsenceDelay||!longAbsenceResumes||!closer){Fail($"offscreen={offscreen} visible={visible} delay={reactionDelay} resumed={resumed} blocked={blocked} stopped={stoppedAgain} closer={closer}");yield break;}
   File.WriteAllText(Path.Combine(output,"combat-visibility-pass.json"),$"{{\"passed\":true,\"offscreenWithinWeaponRangeIgnored\":true,\"visibleTargetFiredOn\":true,\"initialReactionDelay\":true,\"firstShotAfterSeconds\":{firstShotAfter.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"occludedTargetIgnored\":true,\"leavingScreenStopsFire\":true,\"longAbsenceRequiresNewReaction\":true,\"orthographicSize\":{SurvivalGame.GameplayCameraSize.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");Debug.Log("COMBAT_VISIBILITY_PASS");Application.Quit(0);
  }
  void Fail(string s){File.WriteAllText(Path.Combine(output,"combat-visibility-failure.txt"),s);Debug.LogError("COMBAT_VISIBILITY_FAIL "+s);Application.Quit(1);}
 }
}
