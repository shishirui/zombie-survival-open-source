using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class OpeningValidation:MonoBehaviour {
  string output;SurvivalGame g;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=RuntimeLaunch.Arguments();if(Array.IndexOf(args,"-opening-smoke")<0)return;
   var runner=new GameObject("Opt-in opening validation").AddComponent<OpeningValidation>();int n=Array.IndexOf(args,"-validation-output");runner.output=n>=0?args[n+1]:Application.persistentDataPath;Directory.CreateDirectory(runner.output);runner.StartCoroutine(runner.Run());
  }
  void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  IEnumerator Run(){var core=Core();while(true){object item=null;bool more=false;string fail=null;try{more=core.MoveNext();if(more)item=core.Current;}catch(Exception e){fail=e.ToString();}if(fail!=null){Time.timeScale=1;File.WriteAllText(Path.Combine(output,"opening-failure.txt"),fail);Debug.LogError("OPENING_VALIDATION_FAIL "+fail);Application.Quit(1);yield break;}if(!more)yield break;yield return item;}}
  IEnumerator Photo(string name){Time.timeScale=0;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name));yield return new WaitForSecondsRealtime(.25f);Time.timeScale=1;}
  IEnumerator Core(){
   yield return null;yield return null;g=SurvivalGame.Instance;g.Growth.Enabled=false;
   var alive=g.Enemies.Where(z=>z.Alive).ToArray();Check(g.OpeningCount==8&&alive.Length==8,"Expected eight opening enemies, got "+g.OpeningCount+" / "+alive.Length);
   float distance=100;var path=new NavMeshPath();foreach(var z in alive){distance=Mathf.Min(distance,Vector3.Distance(z.transform.position,g.PlayerPosition));var v=Camera.main.WorldToViewportPoint(z.transform.position+Vector3.up);Check(v.z>0&&v.x>.08f&&v.x<.92f&&v.y>.08f&&v.y<.92f,"Opening enemy outside visible play area");Check(z.Kind==EnemyKind.Normal&&z.Agent.isOnNavMesh&&NavMesh.CalculatePath(z.transform.position,g.PlayerPosition,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Opening enemy type/path invalid");}
   Check(distance>=9.5f,"Opening enemy too close");yield return Photo("opening-street.png");
   while(g.ShotsFired==0&&g.Elapsed<2)yield return null;float firstShot=g.Elapsed;Check(g.ShotsFired>0,"No combat within two seconds");
   while(g.Elapsed<5)yield return null;Check(g.Kills>=3,"Opening enemies not engaging");int killsAt5=g.Kills;
   while(g.Elapsed<9)yield return null;Check(g.Wave==1,"Opening is not part of first wave");yield return Photo("opening-combat.png");
   while(g.PendingEnemies>0&&g.Elapsed<24)yield return null;Check(g.Wave==1&&g.ActiveCount+g.Kills==24&&g.PendingEnemies==0,"First wave budget missing");Check(!g.Dead&&g.SpawnViolations==0&&g.MinSpawnDistance>=22&&g.ActiveCount<=g.config.maxAlive,"Opening safety or population bound failed");
   File.WriteAllText(Path.Combine(output,"opening-pass.json"),JsonUtility.ToJson(new Report{passed=true,openingEnemies=g.OpeningCount,firstShotSeconds=firstShot,minimumOpeningDistance=distance,killsAt5Seconds=killsAt5,killsAtFirstWave=g.Kills,aliveAtFirstWave=g.ActiveCount,wavesAtCheck=g.Wave,minimumReinforcementDistance=g.MinSpawnDistance},true));Debug.Log("OPENING_VALIDATION_PASS");Application.Quit(0);
  }
  [Serializable]class Report{public bool passed;public int openingEnemies,killsAt5Seconds,killsAtFirstWave,aliveAtFirstWave,wavesAtCheck;public float firstShotSeconds,minimumOpeningDistance,minimumReinforcementDistance;}
 }
}
