using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class HordeLoadValidation:MonoBehaviour {
  string output;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var a=RuntimeLaunch.Arguments();if(Array.IndexOf(a,"-horde-load-smoke")<0)return;var v=new GameObject("Opt-in horde load sample").AddComponent<HordeLoadValidation>();int n=Array.IndexOf(a,"-validation-output");v.output=n>=0?a[n+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);v.StartCoroutine(v.Run());}
  IEnumerator Run(){yield return null;yield return null;var g=SurvivalGame.Instance;g.CombatValidation=true;g.Growth.Enabled=false;g.config.aimRange=20;g.PlayerHealth.SetHealth(100000);foreach(var z in g.Enemies)z.Retire();g.Supplies.Clear();g.WeaponCrates.Clear();int count=0;
   for(int i=0;i<600&&count<80;i++){float a=i*2.399963f;var p=g.PlayerPosition+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(6+i%12);if(!NavMesh.SamplePosition(p,out var nav,.8f,NavMesh.AllAreas)||!g.CanSpawnEnemyAt(nav.position))continue;var z=g.Enemies[count];z.Spawn(nav.position,g.config.enemySpeed,count%15==0?EnemyKind.Brute:count%10==0?EnemyKind.Runner:EnemyKind.Normal);z.Health.SetHealth(100000);count++;}
   yield return new WaitForSeconds(2);var times=new List<float>();float start=Time.realtimeSinceStartup;bool photo=false;while(Time.realtimeSinceStartup-start<20){times.Add(Time.unscaledDeltaTime);if(!photo&&Time.realtimeSinceStartup-start>8){photo=true;ScreenCapture.CaptureScreenshot(Application.isMobilePlatform?"horde-load.png":Path.Combine(output,"horde-load.png"));}yield return null;}times.Sort();float median=times[times.Count/2],p95=times[(int)(times.Count*.95f)];int alive=0;foreach(var z in g.Enemies)if(z.Alive)alive++;
   var r=new Report{device=SystemInfo.deviceModel,graphics=SystemInfo.graphicsDeviceName,spawned=count,alive=alive,frames=times.Count,seconds=20,medianFPS=1/median,p95FrameMilliseconds=p95*1000,shots=g.ShotsFired,passed=count==80&&alive==80&&!g.Dead,scope="20-second development-build crowded combat sample, not a thermal/endurance benchmark"};File.WriteAllText(Path.Combine(output,"horde-load-report.json"),JsonUtility.ToJson(r,true));Debug.Log("HORDE_LOAD_COMPLETE "+JsonUtility.ToJson(r));Application.Quit(r.passed?0:1);
  }
  [Serializable]class Report{public bool passed;public string device,graphics,scope;public int spawned,alive,frames,shots;public float seconds,medianFPS,p95FrameMilliseconds;}
 }
}
