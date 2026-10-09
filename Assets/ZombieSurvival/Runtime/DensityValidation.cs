using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeadDistrict {
 // Opt-in real-time arrival measurement; never active in ordinary gameplay.
 public sealed class DensityValidation:MonoBehaviour {
  [Serializable] public sealed class Sample {public float seconds;public int alive,inView,within12,kills,pending;}
  [Serializable] public sealed class Result {public int chapter;public bool firing;public int cap,spawned,maxInView,maxWithin12,kills;public float meanSpawnDistance,meanInView,meanWithin12,capSeconds,firstReinforcementInView=-1;public List<Sample> samples=new List<Sample>();}
  [Serializable] public sealed class Report {public bool passed=true;public string platform="Mac standalone; invulnerable stationary arrival fixture, not phone acceptance";public List<Result> results=new List<Result>();}
  static bool started;string output;readonly Report report=new Report();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var a=RuntimeLaunch.Arguments();if(started||!a.Contains("-density-smoke"))return;started=true;ChapterProgress.IsolatedValidation=true;SurvivalGame.Instance.CombatValidation=true;SceneManager.sceneLoaded+=(s,m)=>{if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;};var v=new GameObject("Opt-in density validation").AddComponent<DensityValidation>();DontDestroyOnLoad(v);v.output=a[Array.IndexOf(a,"-validation-output")+1];Directory.CreateDirectory(v.output);v.StartCoroutine(v.Run());}
  void Check(bool value,string reason){if(!value)throw new Exception(reason);}
  IEnumerator Run(){var core=Core();while(true){object item=null;bool more=false;try{more=core.MoveNext();if(more)item=core.Current;}catch(Exception e){File.WriteAllText(Path.Combine(output,"density-failure.txt"),e.ToString());Debug.LogError(e);Application.Quit(1);yield break;}if(!more)yield break;yield return item;}}
  bool InView(SurvivalEnemy e){var p=Camera.main.WorldToViewportPoint(e.AimPoint);return p.z>0&&p.x>0&&p.x<1&&p.y>0&&p.y<1;}
  IEnumerator Core(){
   for(int chapter=1;chapter<3;chapter++)for(int mode=0;mode<2;mode++){
    SceneManager.LoadScene(ChapterCatalog.Get(chapter).sceneName);yield return null;yield return null;
    var g=SurvivalGame.Instance;UnityEngine.Random.InitState(1261+chapter);g.Growth.Enabled=false;g.PlayerHealth.BeginGrowthProtection(90);if(mode==0)g.config.aimRange=0;g.StartChapter();g.CombatValidation=false;
    var result=new Result{chapter=chapter+1,firing=mode==1,cap=g.config.maxAlive};var seen=g.Enemies.Select(e=>e.SpawnCount).ToArray();var reinforcement=new HashSet<string>();float began=Time.time,nextSample=began,spawnDistance=0;int samples=0;float sumView=0,sumNear=0;
    while(Time.time-began<22){
     Check(!g.Dead&&!g.Paused,"Density fixture stopped");int view=0,near=0;float age=Time.time-began;
     foreach(var e in g.Enemies){if(!e.Alive)continue;string id=e.Slot+":"+e.SpawnCount;float d=Vector3.Distance(e.transform.position,g.PlayerPosition);
      if(e.SpawnCount!=seen[e.Slot]){seen[e.Slot]=e.SpawnCount;result.spawned++;spawnDistance+=d;reinforcement.Add(id);Check(d>10,"Reinforcement spawned against player");Check(!Physics.CheckCapsule(e.transform.position+Vector3.up*.5f,e.transform.position+Vector3.up*1.4f,.35f,1<<8),"Reinforcement born in obstacle");var p=Camera.main.WorldToViewportPoint(e.AimPoint);Check(!(p.z>0&&p.x>0&&p.x<1&&p.y>0&&p.y<1),"Reinforcement visibly popped in");}
      if(InView(e)){view++;if(result.firstReinforcementInView<0&&reinforcement.Contains(id))result.firstReinforcementInView=age;}if(d<12)near++;
     }
     if(g.ActiveCount>=g.config.maxAlive)result.capSeconds+=Time.deltaTime;
     if(Time.time>=nextSample){nextSample=Time.time+.5f;result.samples.Add(new Sample{seconds=age,alive=g.ActiveCount,inView=view,within12=near,kills=g.Kills,pending=g.PendingEnemies});result.maxInView=Mathf.Max(result.maxInView,view);result.maxWithin12=Mathf.Max(result.maxWithin12,near);if(age>=5){samples++;sumView+=view;sumNear+=near;}}
     yield return null;
    }
    Check(result.spawned>50&&samples>20&&result.firstReinforcementInView>=0,"Arrival observation incomplete");result.meanSpawnDistance=spawnDistance/result.spawned;result.meanInView=sumView/samples;result.meanWithin12=sumNear/samples;result.kills=g.Kills;report.results.Add(result);
    yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"chapter-"+(chapter+1)+(mode==1?"-rifle":"-hold-fire")+".png"));yield return new WaitForSecondsRealtime(.2f);
   }
   File.WriteAllText(Path.Combine(output,"density-pass.json"),JsonUtility.ToJson(report,true));Debug.Log("DENSITY_PASS");Application.Quit(0);
  }
 }
}
