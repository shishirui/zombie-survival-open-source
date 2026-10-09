using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
namespace DeadDistrict {
 public sealed class ChapterValidation:MonoBehaviour {
  static bool started;SurvivalGame g;string output;float clock;int total,peak;MobilePreferences.Data prefs;
  static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var a=RuntimeLaunch.Arguments();if(started||(!a.Contains("-chapter-smoke")&&!a.Contains("-wave-pacing-smoke")))return;started=true;var v=new GameObject("Opt-in chapter validation").AddComponent<ChapterValidation>();DontDestroyOnLoad(v.gameObject);int n=Array.IndexOf(a,"-validation-output");v.output=n>=0?a[n+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);SurvivalGame.Instance.CombatValidation=true;v.StartCoroutine(v.Run());}
  void Check(bool ok,string why){if(!ok)throw new Exception(why);}
  void Step(float dt=.1f){clock+=dt;typeof(SurvivalGame).GetProperty("Elapsed").SetValue(g,clock);typeof(SurvivalGame).GetMethod("SpawnDirector",Private).Invoke(g,new object[]{clock});Check(g.ActiveCount==g.Enemies.Count(z=>z.Alive)&&g.ActiveCount<=g.config.maxAlive,"Population accounting");peak=Mathf.Max(peak,g.ActiveCount);}
  void KillTo(int count){foreach(var z in g.Enemies.Where(z=>z.Alive).Skip(count).ToArray())z.Health.Kill();Check(g.ActiveCount==count,"Kill accounting");}
  void MovePlayer(Vector3 p){var t=GameObject.Find("Survivor").transform;var cc=t.GetComponent<CharacterController>();cc.enabled=false;t.position=p;cc.enabled=true;Physics.SyncTransforms();}
  IEnumerator Photo(string name){typeof(SurvivalHud).GetField("nextRefresh",Private).SetValue(g.hud,0f);g.hud.Refresh();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name));yield return new WaitForSecondsRealtime(.2f);}
  IEnumerator Run(){prefs=MobilePreferences.Current;var core=Core();while(true){object item=null;bool more=false;string fail=null;try{more=core.MoveNext();if(more)item=core.Current;}catch(Exception e){fail=e.ToString();}if(fail!=null){MobilePreferences.Set(prefs);Time.timeScale=1;File.WriteAllText(Path.Combine(output,"chapter-failure.txt"),fail);Debug.LogError("CHAPTER_FAIL "+fail);Application.Quit(1);yield break;}if(!more)yield break;yield return item;}}
  IEnumerator Core(){yield return null;yield return null;g=SurvivalGame.Instance;Check(Mathf.Abs(Camera.main.orthographicSize-7.7f)<.001f&&Mathf.Abs(g.Chapter.eliteWindup-.7f)<.001f,"Camera or 0.7-second elite warning missing");g.Growth.Enabled=false;g.enabled=false;g.Supplies.Clear();g.WeaponCrates.Clear();UnityEngine.Random.InitState(421);clock=Time.time;g.StartChapter();Check(g.Wave==1&&g.OpeningCount==8&&g.ActiveCount==8&&g.PendingEnemies==16,"Opening budget");int cap=g.config.maxAlive;g.config.maxAlive=8;Step(3);Check(g.PendingEnemies==16&&g.ActiveCount==8,"Blocked spawn consumed queue");g.config.maxAlive=cap;
   for(int wave=1;wave<=8;wave++){
    for(int i=0;g.PendingEnemies>0&&i<2000;i++)Step();Check(g.PendingEnemies==0&&g.Wave==wave,"Spawn queue stuck: "+wave);var alive=g.Enemies.Where(z=>z.Alive).ToArray();Check(alive.Length==g.CurrentWave.Count,"Wave count: "+wave);total+=alive.Length;
    foreach(var group in g.CurrentWave.groups.GroupBy(x=>new{x.kind,x.elite}))Check(alive.Count(z=>z.Kind==group.Key.kind&&z.IsElite==group.Key.elite)==group.Sum(x=>x.count),"Wrong enemy mixture: "+wave);
    Check(wave>=3||alive.All(z=>z.Kind==EnemyKind.Normal),"Dog too early");Check(wave>=5||alive.All(z=>z.Kind!=EnemyKind.Brute),"Brute too early");Step(120);Check(g.Wave==wave&&g.ActiveCount==alive.Length&&float.IsPositiveInfinity(g.NextHorde),"Timed wave overlap");
    if(wave==8)break;
    KillTo(1);Step(120);Check(g.Wave==wave&&float.IsPositiveInfinity(g.NextHorde),"One survivor advances wave");KillTo(0);Step();Check(Mathf.Abs(g.NextHorde-clock-5)<.001f,"Break not five seconds");
    if(wave==5){Check(g.RestSuppliesPlaced==2,"Missing recovery supplies");yield return Photo("wave6-rest.png");}
    g.PauseForShell();Step(0);Check(g.Wave==wave&&g.ActiveCount==0,"Paused director spawned");yield return new WaitForSecondsRealtime(.12f);Check(g.Wave==wave,"Paused wave progressed");g.ResumeRun();
    Step(4.8f);Check(g.Wave==wave&&g.ActiveCount==0,"Early next wave");Step(.21f);Check(g.Wave==wave+1,"No next wave");yield return null;
   }
   Check(total==286&&peak==63&&g.Elite&&g.Elite.IsElite,"Chapter budget or elite");
   // Keep the elite, remove escorts, then exercise the real attack state machine.
   foreach(var z in g.Enemies.Where(z=>z.Alive&&!z.IsElite).Skip(1).ToArray())z.Health.Kill();var boss=g.Elite;var attack=boss.EliteAttack;
   var origin=g.PlayerPosition;Check(NavMesh.SamplePosition(origin+Vector3.forward*5,out var p,1,NavMesh.AllAreas),"No elite test space");boss.Agent.Warp(p.position);boss.Agent.isStopped=true;float t=Time.time+2;attack.Tick(t);Check(attack.Phase==EliteSlam.Stage.Windup,"Elite did not telegraph");Vector3 locked=attack.ImpactPoint;float hp=g.PlayerHealth.CurrentHealth;
   attack.Tick(t+g.Chapter.eliteWindup-.05f);Check(attack.Slams==0&&g.PlayerHealth.CurrentHealth==hp,"Strike before warning finishes");yield return Photo("elite-warning.png");
   Vector3 escape=origin+Vector3.right*5;Check(NavMesh.SamplePosition(escape,out var e,1,NavMesh.AllAreas),"No dodge space");MovePlayer(e.position);attack.Tick(t+g.Chapter.eliteWindup+.01f);Check(attack.Slams==1&&attack.Hits==0&&g.PlayerHealth.CurrentHealth==hp&&attack.ImpactPoint==locked,"Warning tracks player or escape still hits");
   float before=boss.Health.CurrentHealth;boss.TakeBullet(100,Vector3.forward,false);Check(Mathf.Abs(before-boss.Health.CurrentHealth-145)<.01f,"Recovery vulnerability missing");
   MovePlayer(origin);attack.Tick(t+4);attack.Tick(t+7.1f);Check(attack.Phase==EliteSlam.Stage.Windup,"No repeat attack");attack.Tick(t+8.4f);Check(attack.Slams==2&&attack.Hits==1&&Mathf.Abs(hp-g.PlayerHealth.CurrentHealth-g.Chapter.eliteDamage)<.01f,"Standing in zone should take one hit");float after=g.PlayerHealth.CurrentHealth;attack.Tick(t+8.5f);Check(g.PlayerHealth.CurrentHealth==after,"Slam double damage");yield return new WaitForSecondsRealtime(.3f);
   attack.Tick(t+11);attack.Tick(t+14.1f);g.PlayerHealth.BeginDodge(2);attack.Tick(t+15.4f);Check(attack.Slams==3&&g.PlayerHealth.CurrentHealth==after,"Dodge invulnerability ignored");g.PlayerHealth.ClearDodge();
   attack.Tick(t+18);attack.Tick(t+21.1f);var reduced=prefs;reduced.reducedFlash=true;MobilePreferences.Set(reduced);Check(GameObject.Find("Elite danger boundary")!=null,"Warning hidden under reduced flash");g.PauseForShell();float frozen=Time.time;yield return new WaitForSecondsRealtime(.3f);Check(Time.time==frozen&&attack.Slams==3,"Pause advanced attack");g.ResumeRun();
   boss.Health.Kill();Check(!GameObject.Find("Elite danger boundary")&&!GameObject.Find("Elite charge progress"),"Dead boss retained warning");attack.Tick(t+23);Check(attack.Slams==3,"Dead boss still strikes");Step(5);Check(!g.Completed&&g.ActiveCount==1,"Victory before last escort clears");KillTo(0);Step();Check(!g.Completed,"No victory settling delay");Step(1.2f);Check(g.Completed&&g.Paused&&g.WavesCleared==8&&g.hud.MobilePage=="victory"&&g.Kills==286,"Missing final victory");Step(100);Check(g.Wave==8&&g.ActiveCount==0,"Ninth wave appeared");g.ResumeRun();Check(g.Paused,"Completed run resumed");yield return Photo("chapter-victory.png");
   foreach(var rect in GameObject.Find("Chapter victory").GetComponentsInChildren<RectTransform>()){if(!rect.GetComponent<Button>()&&!rect.GetComponent<Text>())continue;var corners=new Vector3[4];rect.GetWorldCorners(corners);foreach(var c in corners)Check(c.x>=Screen.safeArea.xMin-2&&c.x<=Screen.safeArea.xMax+2&&c.y>=Screen.safeArea.yMin-2&&c.y<=Screen.safeArea.yMax+2,"Victory outside safe area: "+rect.name);}
   const string id="validation-42";const string key="DeadDistrict.Chapter.validation-42.v1";bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);try{PlayerPrefs.DeleteKey(key);ChapterProgress.Complete(id,300,286);ChapterProgress.Complete(id,350,250);ChapterProgress.Complete(id,280,286);var record=ChapterProgress.Read(id);Check(record.clears==3&&record.bestSeconds==280&&record.bestKills==286,"Progress persistence");}finally{if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
   g.hud.OpenHome();Check(!GameObject.Find("Continue current run"),"Home offers completed run");yield return Photo("chapter-home.png");MobilePreferences.Set(prefs);
   g.hud.ShowVictory();var oldGame=g;GameObject.Find("Replay chapter").GetComponent<Button>().onClick.Invoke();while(ReferenceEquals(SurvivalGame.Instance,oldGame))yield return null;yield return null;g=SurvivalGame.Instance;Check(Mathf.Abs(Camera.main.orthographicSize-7.7f)<.001f&&Mathf.Abs(g.Chapter.eliteWindup-.7f)<.001f,"Camera or 0.7-second elite warning missing");g.enabled=false;Check(g&&!g.Completed&&!g.Dead&&g.Wave==1&&g.Kills==0&&g.Growth.Level==1&&g.PendingEnemies>0&&!g.Elite,"Replay not fresh");
   File.WriteAllText(Path.Combine(output,"chapter-pass.json"),"{\"passed\":true,\"waves\":8,\"enemies\":286,\"peakAlive\":63,\"fullClearAndFiveSecondBreak\":true,\"recoverySupplies\":2,\"eliteTelegraphFixedPoint\":true,\"dodgeAvoidsSlam\":true,\"recoveryDamageBonus\":true,\"deadEliteCancelsAttack\":true,\"victoryAndReplay\":true,\"recordPersistence\":true}");Debug.Log("CHAPTER_PASS");Application.Quit(0);
  }
 }
}
