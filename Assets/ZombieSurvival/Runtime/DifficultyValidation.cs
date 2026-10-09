using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class DifficultyValidation:MonoBehaviour {
  string output;SurvivalGame g;readonly List<Sample> samples=new List<Sample>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=RuntimeLaunch.Arguments();if(Array.IndexOf(args,"-difficulty-smoke")<0)return;
   var v=new GameObject("Opt-in difficulty validation").AddComponent<DifficultyValidation>();DontDestroyOnLoad(v.gameObject);int n=Array.IndexOf(args,"-validation-output");v.output=n>=0?args[n+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;v.StartCoroutine(v.Run());
  }
  static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
  static void Set(SurvivalGame g,string name,float value){typeof(SurvivalGame).GetProperty(name).SetValue(g,value);}
  IEnumerator Run(){var core=Core();while(true){object item=null;bool more=false;string fail=null;try{more=core.MoveNext();if(more)item=core.Current;}catch(Exception e){fail=e.ToString();}if(fail!=null){Time.timeScale=1;File.WriteAllText(Path.Combine(output,"difficulty-failure.txt"),fail);Debug.LogError("DIFFICULTY_FAIL "+fail);Application.Quit(1);yield break;}if(!more)yield break;yield return item;}}
  IEnumerator Core(){
   yield return null;yield return null;g=SurvivalGame.Instance;g.CombatValidation=true;g.Growth.Enabled=false;g.Supplies.Clear();g.enabled=false;
   var z=g.Enemies[0];Check(NavMesh.SamplePosition(g.PlayerPosition+Vector3.forward*.8f,out var near,.25f,NavMesh.AllAreas),"No contact fixture floor");
   foreach(float seconds in new[]{0f,60f,180f,300f,600f}){
    Set(g,"Elapsed",seconds);var pressure=new HordePressure(seconds,g.config);
    var sample=new Sample{seconds=seconds,fronts=pressure.fronts,hordeInterval=pressure.hordeInterval,normalChance=1-pressure.runnerChance-pressure.bruteChance};
    var speeds=new List<float>();var health=new List<float>();var damage=new List<float>();
    foreach(EnemyKind kind in new[]{EnemyKind.Normal,EnemyKind.Runner,EnemyKind.Brute}){
     z.Spawn(near.position,g.config.enemySpeed,kind);float hp=z.Health.CurrentHealth;health.Add(hp);speeds.Add(z.Agent.speed);damage.Add(z.ContactDamage);
     Check(z.Agent.speed<g.config.moveSpeed,"Enemy faster than unupgraded player");
     z.Health.DamageEnabled();z.Health.Damage(26,g.gameObject,0,0,Vector3.forward);float wounded=z.Health.CurrentHealth;
     Set(g,"Elapsed",seconds+120);Check(Mathf.Approximately(wounded,z.Health.CurrentHealth),"Live enemy gained health as difficulty changed");Set(g,"Elapsed",seconds);
     g.PlayerHealth.ResetForSpawn();z.Tick(Time.time+1);Check(Mathf.Abs(100-g.PlayerHealth.CurrentHealth-z.ContactDamage)<.02f,"Contact damage not applied to real player health");z.Retire();
    }
    sample.health=health.ToArray();sample.speed=speeds.ToArray();sample.contactDamage=damage.ToArray();samples.Add(sample);
    Check(pressure.fronts==(seconds<60?1:seconds<180?2:3),"Incorrect attack fronts");
    Check(pressure.hordeInterval>=20&&pressure.trickleInterval>=.30f&&sample.normalChance>=.45f,"Unbounded pressure");
    for(int i=0;i<pressure.fronts;i++)Check(Mathf.Abs(HordePressure.FrontOffset(pressure.fronts,i))<1.1f,"No escape side");
   }
   Check(samples[0].health[0]==130&&samples[0].speed[1]==4.3f&&samples[0].contactDamage[0]==8,"Opening balance changed");
   Check(samples[3].health[0]==195&&samples[3].contactDamage[2]>25&&samples[3].speed[1]>5.5f,"Late-game escalation missing");
   Check(samples[3].health[0]==samples[4].health[0]&&samples[3].speed[1]==samples[4].speed[1],"Difficulty cap missing");
   // Exercise the real director and combat at the three-minute stage, with default health and no test invulnerability.
   Set(g,"Elapsed",180);Set(g,"NextHorde",180);g.PlayerHealth.ResetForSpawn();g.CombatValidation=false;g.enabled=true;Time.timeScale=2;float began=g.Elapsed;float real=Time.realtimeSinceStartup;int frontsObserved=0;
   while(!g.Dead&&g.Elapsed-began<65){
    frontsObserved=Mathf.Max(frontsObserved,(int)typeof(SurvivalGame).GetField("hordeFronts",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(g));
    Check(g.ActiveCount<=g.config.maxAlive&&g.SpawnViolations==0,"Population or spawn safety failed");
    foreach(var enemy in g.Enemies)if(enemy.Alive)Check(enemy.Agent.isOnNavMesh,"Enemy left NavMesh");
    Check(Time.realtimeSinceStartup-real<75,"Combat timeout");yield return null;
   }
   bool stationaryDied=g.Dead;float survived=g.Elapsed-began;float hpAfter=g.PlayerHealth.CurrentHealth;
   Check(g.Wave>=1&&frontsObserved==3&&g.ShotsFired>0&&g.MinSpawnDistance>=22,"Late-game director not exercised");
   Check(hpAfter<100,"Late-game stationary player never under threat");
   g.Restart();yield return null;yield return null;yield return null;var fresh=SurvivalGame.Instance;
   Check(fresh!=g&&fresh.Elapsed<1&&fresh.Wave==0&&fresh.PlayerHealth.CurrentHealth==100&&new HordePressure(fresh.Elapsed,fresh.config).fronts==1,"Difficulty failed to reset");
   File.WriteAllText(Path.Combine(output,"difficulty-pass.json"),JsonUtility.ToJson(new Report{passed=true,stages=samples.ToArray(),liveHealthPreserved=true,actualContactDamageVerified=true,lateStationaryDied=stationaryDied,lateStationarySeconds=survived,lateRemainingHealth=hpAfter,restartResetsDifficulty=true,spawnSafetyAndPoolCap=true,note="Scripted stationary combat, upgrades disabled; not human difficulty acceptance or phone performance proof"},true));Debug.Log("DIFFICULTY_PASS");Application.Quit(0);
  }
  [Serializable]class Sample{public float seconds,hordeInterval,normalChance;public int fronts;public float[] health,speed,contactDamage;}
  [Serializable]class Report{public bool passed,liveHealthPreserved,actualContactDamageVerified,lateStationaryDied,restartResetsDifficulty,spawnSafetyAndPoolCap;public float lateStationarySeconds,lateRemainingHealth;public Sample[] stages;public string note;}
 }
}
