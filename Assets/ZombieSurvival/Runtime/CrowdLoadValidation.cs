using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
namespace DeadDistrict {
 public sealed class CrowdLoadValidation:MonoBehaviour {
  static bool started;string output;readonly List<string> reports=new List<string>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var a=RuntimeLaunch.Arguments();if(started||!a.Contains("-crowd-load-smoke"))return;started=true;ChapterProgress.IsolatedValidation=true;SurvivalGame.Instance.CombatValidation=true;SceneManager.sceneLoaded+=(s,m)=>{if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;};var v=new GameObject("Opt-in crowd load validation").AddComponent<CrowdLoadValidation>();DontDestroyOnLoad(v);v.output=a[Array.IndexOf(a,"-validation-output")+1];Directory.CreateDirectory(v.output);v.StartCoroutine(v.Run());}
  void Check(bool ok,string why){if(!ok)throw new Exception(why);}
  IEnumerator Run(){var core=Core();while(true){object item=null;bool more=false;string fail=null;try{more=core.MoveNext();if(more)item=core.Current;}catch(Exception e){fail=e.ToString();}if(fail!=null){File.WriteAllText(Path.Combine(output,"crowd-failure.txt"),fail);Application.Quit(1);yield break;}if(!more)yield break;yield return item;}}
  IEnumerator Core(){
   for(int chapter=1;chapter<3;chapter++){
    SceneManager.LoadScene(ChapterCatalog.Get(chapter).sceneName);yield return null;yield return null;var g=SurvivalGame.Instance;g.Growth.Enabled=false;g.config.aimRange=0;g.PlayerHealth.BeginGrowthProtection(60);g.Supplies.Clear();g.WeaponCrates.Clear();int cap=chapter==1?270:320;Check(g.Enemies.Count==cap,"Wrong chapter pool capacity");
    var starts=new Vector3[cap];for(int i=0;i<cap;i++){
     bool found=false;for(int n=0;n<80;n++){float angle=(i*2.399963f+n*.7f);var point=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(7+i%17);if(!NavMesh.SamplePosition(point,out var hit,1,NavMesh.AllAreas)||Physics.CheckCapsule(hit.position+Vector3.up*.5f,hit.position+Vector3.up*1.4f,.55f,1<<8))continue;
      starts[i]=hit.position;g.Enemies[i].Spawn(hit.position,g.config.enemySpeed,i%25==0?EnemyKind.Brute:i%17==0?EnemyKind.Runner:EnemyKind.Normal);found=true;break;
     }Check(found,"No crowd fixture position "+i);
    }
    typeof(SurvivalGame).GetProperty("ActiveCount").SetValue(g,cap);g.enabled=true;yield return new WaitForSeconds(2);var samples=new List<float>();float end=Time.realtimeSinceStartup+8;
    while(Time.realtimeSinceStartup<end){samples.Add(Time.unscaledDeltaTime);Check(!g.Dead&&g.Enemies.Count(e=>e.Alive)==cap,"Unexpected death or enemy loss in load fixture");yield return null;}
    int moved=g.Enemies.Where((e,i)=>Vector3.Distance(starts[i],e.transform.position)>1).Count();Check(moved>cap*.8f,"Crowd navigation stuck");samples.Sort();float mean=samples.Average(),p95=samples[Mathf.Min(samples.Count-1,(int)(samples.Count*.95f))];
    yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"chapter-"+(chapter+1)+"-crowd.png"));yield return new WaitForSecondsRealtime(.25f);
    reports.Add("{\"chapter\":"+(chapter+1)+",\"alive\":"+cap+",\"moved\":"+moved+",\"samples\":"+samples.Count+",\"meanFrameMs\":"+(mean*1000).ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"p95FrameMs\":"+(p95*1000).ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
   }
   File.WriteAllText(Path.Combine(output,"crowd-pass.json"),"{\"passed\":true,\"platform\":\"Mac standalone development, not mobile acceptance\",\"chapters\":["+string.Join(",",reports)+"]}");Application.Quit(0);
  }
 }
}
