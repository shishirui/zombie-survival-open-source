using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class ContinuousFireValidation : MonoBehaviour {
  string output;readonly List<float> shots=new List<float>();bool capture;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot(){var args=RuntimeLaunch.Arguments();bool capture=Array.IndexOf(args,"-continuous-fire-capture")>=0;if(!capture&&Array.IndexOf(args,"-continuous-fire-smoke")<0)return;var v=new GameObject("Opt-in continuous fire validation").AddComponent<ContinuousFireValidation>();v.capture=capture;int n=Array.IndexOf(args,"-validation-output");v.output=n>=0?args[n+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;v.StartCoroutine(v.Run());}
  IEnumerator Run(){
   yield return null;yield return null;var g=SurvivalGame.Instance;g.CombatValidation=true;g.AutoTest=false;g.Supplies.Clear();var cam=Camera.main;var forward=cam.transform.forward;forward.y=0;forward.Normalize();var right=cam.transform.right;right.y=0;right.Normalize();
   // Distinct directions force muzzle turning, not just retargeting collinear colliders.
   var positions=new[]{g.PlayerPosition+forward*4-right*2,g.PlayerPosition+forward*4+right*2,g.PlayerPosition-forward*3+right*3};
   for(int i=0;i<3;i++){if(!NavMesh.SamplePosition(positions[i],out var nav,.5f,NavMesh.AllAreas)){Fail("Missing test floor");yield break;}g.Enemies[i].Spawn(nav.position,0);if(!g.CanAutoTarget(g.Enemies[i])){Fail("Victim not visible");yield break;}}
   int start=g.ShotsFired,previous=start,kills=g.Kills;float began=Time.time,deadline=began+4;bool health=true;float initialDelay=-1;
   while(g.Kills<kills+3&&Time.time<deadline){yield return null;if(g.ShotsFired>previous){if(g.ShotsFired!=previous+1){Fail("Missed shot sample");yield break;}previous=g.ShotsFired;shots.Add(Time.time);if(initialDelay<0)initialDelay=Time.time-began;int dealt=0;for(int i=0;i<3;i++)dealt+=Mathf.RoundToInt(130-g.Enemies[i].Health.CurrentHealth);health&=dealt==(previous-start)*26;}}
   if(g.Kills!=kills+3||shots.Count!=15||!health||initialDelay<.2f){Fail($"kills={g.Kills-kills} shots={shots.Count} damage={health} initialDelay={initialDelay}");yield break;}
   float gap1=shots[5]-shots[4],gap2=shots[10]-shots[9],maxGap=0;for(int i=1;i<shots.Count;i++)maxGap=Mathf.Max(maxGap,shots[i]-shots[i-1]);
   if(gap1>.15f||gap2>.15f||maxGap>.15f){Fail($"Victim switch pauses: {gap1}, {gap2}, max={maxGap}");yield break;}
   int emptyShots=g.ShotsFired;var model=GameObject.Find("Survivor").GetComponentInChildren<LoftActor>().transform;var heldDirection=model.forward;float maximumIdleTurn=0;float idleUntil=Time.time+.35f;while(Time.time<idleUntil){yield return null;maximumIdleTurn=Mathf.Max(maximumIdleTurn,Vector3.Angle(heldDirection,model.forward));}bool empty=g.ShotsFired==emptyShots;
   if(maximumIdleTurn>1){Fail("Gun turns toward empty space after final kill: "+maximumIdleTurn);yield break;}
   // A newly revealed enemy still receives the first-sight delay after the chain ends.
   var fresh=g.Enemies[0];if(!NavMesh.SamplePosition(g.PlayerPosition+forward*4,out var point,.5f,NavMesh.AllAreas)){Fail("Missing fresh victim floor");yield break;}fresh.Spawn(point.position,0);fresh.Health.SetHealth(10000);float revealed=Time.time;yield return new WaitForSeconds(.12f);bool delay=g.ShotsFired==emptyShots;
   while(g.ShotsFired==emptyShots&&Time.time-revealed<1)yield return null;bool resumes=g.ShotsFired>emptyShots;float firstSightSeconds=Time.time-revealed;
   if(firstSightSeconds>.33f){Fail("Empty-scene reacquisition too slow: "+firstSightSeconds);yield break;}
   // A brief obstruction must stop damage but not erase an already established target lock.
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=8;wall.transform.position=Vector3.Lerp(g.PlayerPosition,fresh.transform.position,.5f)+Vector3.up*1.2f;wall.transform.localScale=new Vector3(1,2.4f,1);
   yield return null;int beforeCover=g.ShotsFired;float coveredHealth=fresh.Health.CurrentHealth;yield return new WaitForSeconds(.08f);bool coverStops=g.ShotsFired==beforeCover&&fresh.Health.CurrentHealth==coveredHealth&&!g.CanAutoTarget(fresh);Destroy(wall);yield return null;float uncovered=Time.time;
   while(g.ShotsFired==beforeCover&&Time.time-uncovered<.5f)yield return null;float reacquireSeconds=Time.time-uncovered;
   if(!coverStops||g.ShotsFired==beforeCover||reacquireSeconds>.16f){Fail($"Brief cover resume: blocked={coverStops} delay={reacquireSeconds}");yield break;}
   // Let the actual magazine empty: reload remains the intentional firing break.
   deadline=Time.time+g.config.magazineSize/g.config.roundsPerSecond+2;while(!g.Reloading&&Time.time<deadline)yield return null;int reloadShots=g.ShotsFired;bool magazine=g.Ammo==0&&reloadShots-start==g.config.magazineSize;yield return new WaitForSeconds(.25f);bool reload=g.Reloading&&g.ShotsFired==reloadShots;deadline=Time.time+2;while(g.ShotsFired==reloadShots&&Time.time<deadline)yield return null;bool reloadResumes=g.ShotsFired>reloadShots;
   if(!empty||!delay||!resumes||!reload||!reloadResumes||!magazine){Fail($"empty={empty} delay={delay} resumes={resumes} reload={reload} reloadResumes={reloadResumes} magazine={magazine}");yield break;}
   if(capture){ScreenCapture.CaptureScreenshot(Application.isMobilePlatform?"continuous-fire.png":Path.Combine(output,"continuous-fire.png"));yield return new WaitForSeconds(.3f);}
   File.WriteAllText(Path.Combine(output,"continuous-fire-pass.json"),$"{{\"passed\":true,\"threeZombiesKilledBy15ActualShots\":true,\"perShotDamageCorrect\":true,\"firstSightDelay\":true,\"turningAcrossTargets\":true,\"killSwitchGap1Seconds\":{Num(gap1)},\"killSwitchGap2Seconds\":{Num(gap2)},\"maxShotGapSeconds\":{Num(maxGap)},\"noTargetStopsFire\":true,\"idleHeadingStable\":true,\"maxIdleTurnDegrees\":{Num(maximumIdleTurn)},\"firstSightSeconds\":{Num(firstSightSeconds)},\"briefCoverStopsFire\":true,\"briefCoverResumeSeconds\":{Num(reacquireSeconds)},\"newlyVisibleTargetDelay\":true,\"magazineReloadAndResume\":true,\"magazineCapacity\":{g.config.magazineSize},\"actualShotsBeforeReload\":{reloadShots-start}}}");Debug.Log("CONTINUOUS_FIRE_PASS");Application.Quit(0);
  }
  static string Num(float n)=>n.ToString(System.Globalization.CultureInfo.InvariantCulture);
  void Fail(string reason){File.WriteAllText(Path.Combine(output,"continuous-fire-failure.txt"),reason);Debug.LogError("CONTINUOUS_FIRE_FAIL "+reason);Application.Quit(1);}
 }
}
