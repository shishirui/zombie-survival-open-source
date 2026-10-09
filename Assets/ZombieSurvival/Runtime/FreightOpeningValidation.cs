using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace DeadDistrict {
 public sealed class FreightOpeningValidation:MonoBehaviour {
  string output;static bool started;SurvivalGame g;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var a=RuntimeLaunch.Arguments();if(started||!a.Contains("-freight-opening-smoke"))return;started=true;ChapterProgress.IsolatedValidation=true;var v=new GameObject("Opt-in freight opening validation").AddComponent<FreightOpeningValidation>();DontDestroyOnLoad(v.gameObject);int i=Array.IndexOf(a,"-validation-output");v.output=i>=0?a[i+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);v.StartCoroutine(v.Run());}
  void Check(bool ok,string why){if(!ok)throw new Exception(why);}
  IEnumerator Run(){var routine=Core();while(true){object item=null;bool more=false;string fail=null;try{more=routine.MoveNext();if(more)item=routine.Current;}catch(Exception e){fail=e.ToString();}if(fail!=null){File.WriteAllText(Path.Combine(output,"freight-opening-failure.txt"),fail);Debug.LogError("FREIGHT_OPENING_FAIL "+fail);Application.Quit(1);yield break;}if(!more)yield break;yield return item;}}
  IEnumerator Core(){yield return null;yield return null;g=SurvivalGame.Instance;ChapterProgress.Complete(g.Chapter.id,300,286);ChapterProgress.Complete(ChapterCatalog.Get(1).id,360,342);var old=g;g.BeginChapter(2);while(ReferenceEquals(old,SurvivalGame.Instance))yield return null;yield return null;g=SurvivalGame.Instance;Check(g.Chapter.number==3&&g.Wave==1&&g.OpeningCount==8,"Freight opening not populated");Check(g.WeaponCrates.ActiveCount==3,"Freight opening crates not present");float start=Time.time;int initialAmmo=g.Ammo;while(Time.time-start<10&&!g.Dead)yield return null;Check(!g.Dead&&g.Kills>0&&g.ShotsFired>0,"No live automatic combat in freight");g.hud.Refresh();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"freight-live.png"));yield return new WaitForSecondsRealtime(.3f);
   // Capture actual map overview while the runtime is frozen.
   g.enabled=false;foreach(var e in g.Enemies)if(e.Alive)e.Agent.isStopped=true;RenderSettings.fog=false;Camera.main.farClipPlane=300;Camera.main.orthographicSize=55;Camera.main.transform.position=new Vector3(-42,62,-42);Camera.main.transform.rotation=Quaternion.Euler(48,45,0);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"freight-overview.png"));yield return new WaitForSecondsRealtime(.3f);
   File.WriteAllText(Path.Combine(output,"freight-opening-pass.json"),"{\"passed\":true,\"openingEnemies\":8,\"weaponCrates\":3,\"killsAtTenSeconds\":"+g.Kills+",\"shots\":"+g.ShotsFired+"}");Debug.Log("FREIGHT_OPENING_PASS");Application.Quit(0);
  }
 }
}
