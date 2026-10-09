using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeadDistrict {
 // Opt-in test runner only: normal players never see forced inputs or test cheats.
 public sealed class PrototypeValidation : MonoBehaviour {
  string output;bool smoke,render;float realStart;Vector3 start;bool grenadeVerified;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot() {
   var args=Environment.GetCommandLineArgs();bool smoke=Array.IndexOf(args,"-survival-smoke")>=0,render=Array.IndexOf(args,"-survival-capture")>=0,baseline=Array.IndexOf(args,"-base-demo-smoke")>=0;
   if(!smoke&&!render&&!baseline)return;
   var runner=new GameObject("Opt-in validation runner").AddComponent<PrototypeValidation>();DontDestroyOnLoad(runner.gameObject);runner.smoke=smoke;runner.render=render;
   int p=Array.IndexOf(args,"-validation-output");runner.output=p>=0&&p+1<args.Length?args[p+1]:Application.persistentDataPath;Directory.CreateDirectory(runner.output);
   runner.StartCoroutine(baseline?runner.BaseDemo():runner.Run());
  }
  IEnumerator BaseDemo(){yield return new WaitForSeconds(5);Debug.Log("BASE_DEMO_RUNTIME_PASS scene="+SceneManager.GetActiveScene().name+" camera="+(Camera.main!=null));Application.Quit(0);}
  IEnumerator Run() {
   yield return null;yield return null;
   var g=SurvivalGame.Instance;if(g==null){Fail("Missing game");yield break;}
   realStart=Time.realtimeSinceStartup;start=g.PlayerPosition;g.AutoTest=true;g.PlayerHealth.SetHealth(smoke?1000:g.config.playerHealth);
   if(smoke)Time.timeScale=4;
   bool pauseVerified=false;
   while(g.Elapsed<58) {
    if(g.Dead){Fail("Unexpected death before coverage");yield break;}
    if(g.Elapsed>5&&!pauseVerified){
     g.TogglePause();float before=g.Elapsed;Vector3 pos=g.PlayerPosition;yield return new WaitForSecondsRealtime(.25f);
     if(Mathf.Abs(g.Elapsed-before)>.02f || Vector3.Distance(g.PlayerPosition,pos)>.01f){Fail("Pause did not freeze gameplay");yield break;}
     g.TogglePause();if(smoke)Time.timeScale=4;pauseVerified=true;
    }
    if(!render&&g.Elapsed>23&&!grenadeVerified){
     // Exercise explosion with pooled targets on a sampled walkable patch.
     var point=g.PlayerPosition+Vector3.forward*3;
     if(UnityEngine.AI.NavMesh.SamplePosition(point,out var nav,2,UnityEngine.AI.NavMesh.AllAreas)) {
      int placed=0;foreach(var z in g.Enemies)if(z.Alive&&placed++<6){z.Agent.Warp(nav.position+new Vector3((placed%2)*.4f,0,(placed/2)*.4f));z.Health.SetHealth(100);}
     }
     yield return new WaitForSeconds(.2f);g.ThrowGrenade();yield return new WaitForSeconds(1.1f);grenadeVerified=g.GrenadesThrown>0&&g.GrenadeKills>0;
    }
    if(render && g.Elapsed>19.3f&&!grenadeVerified){g.ThrowGrenade();grenadeVerified=true;}
    if(render && g.Elapsed>=20.25f) {
     g.AutoTest=false;Time.timeScale=1;
     yield return new WaitForSecondsRealtime(.05f);
     ScreenCapture.CaptureScreenshot(Path.Combine(output,"prototype-gameplay.png"));
     yield return new WaitForSecondsRealtime(1);if(!File.Exists(Path.Combine(output,"prototype-gameplay.png"))){Fail("Screenshot file missing");yield break;}Debug.Log("CAPTURE_PASS");Application.Quit(0);yield break;
    }
    yield return null;
    if(Time.realtimeSinceStartup-realStart>120){Fail("Timed out");yield break;}
   }
   var obstaclePath=new UnityEngine.AI.NavMeshPath();
   bool pathfinding=UnityEngine.AI.NavMesh.CalculatePath(new Vector3(-11,0,2),new Vector3(-11,0,14),UnityEngine.AI.NavMesh.AllAreas,obstaclePath)&&obstaclePath.status==UnityEngine.AI.NavMeshPathStatus.PathComplete&&obstaclePath.corners.Length>=3;
   bool cover=Physics.Linecast(new Vector3(-11,1,2),new Vector3(-11,1,14),1<<8);
   bool movement=Vector3.Distance(start,g.PlayerPosition)>3;
   bool pool=g.ActiveCount<=g.config.maxAlive&&g.PeakAlive>20&&g.PoolReuses>0;
   bool shooting=g.ShotsFired>30&&g.Kills>0&&g.Reloads>0;
   bool hordes=g.Wave>=2&&g.SpawnViolations==0&&g.MinSpawnDistance>=22;
   bool effects=g.VisualEffects!=null&&g.VisualEffects.ImpactsPlayed>0&&g.VisualEffects.DeathsPlayed>0&&g.VisualEffects.ExplosionsPlayed>0;
   bool presentation=g.Sound!=null&&g.Sound.LoadedClips>=21&&g.Sound.HasBattleMusic&&g.Sound.HasLoopingMusic&&g.Sound.ShotsPlayed==g.ShotsFired&&Camera.main.orthographic&&Mathf.Abs(Mathf.DeltaAngle(Camera.main.transform.eulerAngles.y,45))<1;
   int offmesh=0;foreach(var z in g.Enemies)if(z.Alive&&!z.Agent.isOnNavMesh)offmesh++;
   string report=$"{{\"movement\":{Bool(movement)},\"pause\":{Bool(pauseVerified)},\"pool\":{Bool(pool)},\"shootingReload\":{Bool(shooting)},\"hordesAndSafeSpawns\":{Bool(hordes)},\"grenade\":{Bool(grenadeVerified)},\"kills\":{g.Kills},\"shots\":{g.ShotsFired},\"reloads\":{g.Reloads},\"waves\":{g.Wave},\"peakAlive\":{g.PeakAlive},\"minSpawnDistance\":{g.MinSpawnDistance.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"offMesh\":{offmesh},\"packEffects\":{Bool(effects)},\"impacts\":{g.VisualEffects.ImpactsPlayed},\"deathFX\":{g.VisualEffects.DeathsPlayed},\"explosionFX\":{g.VisualEffects.ExplosionsPlayed}}}";
   File.WriteAllText(Path.Combine(output,"runtime-before-restart.json"),report);
   File.WriteAllText(Path.Combine(output,"presentation.json"),$"{{\"passed\":{Bool(presentation)},\"loadedAudioClips\":{g.Sound.LoadedClips},\"shotSoundTriggers\":{g.Sound.ShotsPlayed},\"loopingMusic\":{Bool(g.Sound.HasLoopingMusic)},\"orthographic\":{Bool(Camera.main.orthographic)},\"cameraYaw\":{Camera.main.transform.eulerAngles.y.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
   Debug.Log("NAVIGATION_CHECK detour="+pathfinding+" cover="+cover+" corners="+obstaclePath.corners.Length);
   if(!presentation||!movement||!pool||!shooting||!hordes||!grenadeVerified||!pauseVerified||!pathfinding||!cover||offmesh>0||!effects){Fail(report);yield break;}
   g.PlayerHealth.DamageEnabled();g.PlayerHealth.Damage(10000,g.gameObject,0,0,Vector3.zero);yield return null;
   if(!g.Dead){Fail("Death not handled");yield break;}
   g.Restart();yield return null;yield return null;yield return null;
   var next=SurvivalGame.Instance;
   bool reset=next!=g&&!next.Dead&&next.Kills==0&&next.Wave==0&&next.Ammo==next.config.magazineSize&&Mathf.Approximately(next.PlayerHealth.CurrentHealth,next.config.playerHealth)&&next.ActiveCount==next.OpeningCount;
   if(!reset){Fail("Restart did not reset run");yield break;}
   File.WriteAllText(Path.Combine(output,"runtime-pass.json"),"{\"passed\":true,\"deathAndRestart\":true,\"device\":\""+SystemInfo.deviceModel+"\",\"graphics\":\""+SystemInfo.graphicsDeviceType+"\",\"note\":\"Accelerated functional test; not a phone performance benchmark\"}");
   Debug.Log("SURVIVAL_RUNTIME_PASS "+report);Application.Quit(0);
  }
  static string Bool(bool b)=>b?"true":"false";
  void Fail(string message){File.WriteAllText(Path.Combine(output,"runtime-failure.txt"),message);Debug.LogError("SURVIVAL_RUNTIME_FAIL "+message);Application.Quit(1);}
 }
}
