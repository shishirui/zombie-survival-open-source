using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeadDistrict {
 public sealed class OcclusionValidation:MonoBehaviour {
  string output;readonly List<Report> reports=new List<Report>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=RuntimeLaunch.Arguments();if(!args.Contains("-occlusion-smoke"))return;
   ChapterProgress.IsolatedValidation=true;SurvivalGame.Instance.CombatValidation=true;
   var v=new GameObject("Opt-in occlusion validation").AddComponent<OcclusionValidation>();DontDestroyOnLoad(v.gameObject);
   v.output=args[Array.IndexOf(args,"-validation-output")+1];Directory.CreateDirectory(v.output);v.StartCoroutine(v.Run());
  }
  void Check(bool value,string why){if(!value)throw new Exception(why);}
  IEnumerator Run(){var stack=new Stack<IEnumerator>();stack.Push(Core());while(stack.Count>0){object item=null;bool more=false;string fail=null;try{more=stack.Peek().MoveNext();if(more)item=stack.Peek().Current;}catch(Exception e){fail=e.ToString();}if(fail!=null){File.WriteAllText(Path.Combine(output,"occlusion-failure.txt"),fail);Debug.LogError(fail);Application.Quit(1);yield break;}if(!more){stack.Pop();continue;}if(item is IEnumerator nested)stack.Push(nested);else yield return item;}}
  void Move(SurvivalGame g,Vector3 p){var controller=g.PlayerHealth.GetComponent<CharacterController>();controller.enabled=false;controller.transform.position=p;controller.enabled=true;}
  IEnumerator Core(){
   for(int chapter=0;chapter<ChapterCatalog.Count;chapter++){
    if(chapter>0){SceneManager.LoadScene(ChapterCatalog.Get(chapter).sceneName);yield return null;}
    yield return null;yield return null;
    var g=SurvivalGame.Instance;g.CombatValidation=true;g.enabled=false;g.Growth.Enabled=false;
    foreach(var enemy in g.Enemies)if(enemy.Alive)enemy.Retire();
    var covers=FindObjectsByType<StreetOcclusion>(FindObjectsSortMode.None);var cam=Camera.main;
    Check(OcclusionPreparation.Succeeded,"Warmup did not submit scene "+chapter);
    Check(OcclusionPreparation.PreparedSurfaces==covers.Sum(c=>c.surfaces.Count(s=>s.renderer.enabled&&s.renderer.gameObject.activeInHierarchy)),"Missing prepared surface");
    Check(!GameObject.Find("Temporary occlusion preparation")&&!cam.targetTexture,"Warmup leaked into gameplay");
    var report=new Report{chapter=chapter+1,covers=covers.Length,surfaces=OcclusionPreparation.PreparedSurfaces,prepareMs=OcclusionPreparation.Milliseconds};
    int materials=Resources.FindObjectsOfTypeAll<Material>().Length;var block=new MaterialPropertyBlock();
    foreach(var cover in covers){
     bool colliderEnabled=cover.obstacle.enabled;Bounds bounds=cover.obstacle.bounds;
     for(int visit=0;visit<2;visit++){
      cam.transform.position=bounds.center+new Vector3(-12,18,-12);cam.transform.LookAt(bounds.center);
      Move(g,new Vector3(bounds.center.x,0,bounds.center.z));
      float end=Time.realtimeSinceStartup+.4f;float largest=0;
      do{yield return null;largest=Mathf.Max(largest,Time.unscaledDeltaTime*1000);}while(Time.realtimeSinceStartup<end);
      Check(cover.ObscuringPlayer&&cover.Opacity<.2f,"Cover did not fade: "+cover.name);
      foreach(var s in cover.surfaces){Check(s.renderer.sharedMaterials.SequenceEqual(s.translucent),"Transparent materials differ");s.renderer.GetPropertyBlock(block);Check(Mathf.Abs(block.GetColor("_BaseColor").a-.15f)<.01f,"Incorrect opacity");}
      if(visit==0)report.firstVisitMaxMs=Mathf.Max(report.firstVisitMaxMs,largest);else report.repeatVisitMaxMs=Mathf.Max(report.repeatVisitMaxMs,largest);
      if(visit==0&&cover==covers[0]){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"chapter-"+(chapter+1)+"-transparent.png"));}
      Move(g,new Vector3(bounds.center.x+50,0,bounds.center.z-50));yield return new WaitForSecondsRealtime(.4f);
      Check(!cover.ObscuringPlayer&&cover.Opacity>.999f,"Cover did not restore: "+cover.name);
      foreach(var s in cover.surfaces){Check(s.renderer.sharedMaterials.SequenceEqual(s.opaque),"Opaque materials differ");s.renderer.GetPropertyBlock(block);Check(block.isEmpty,"Stale alpha block");}
      Check(cover.obstacle.enabled==colliderEnabled&&cover.obstacle.bounds==bounds,"Collision changed");report.transitions+=2;
     }
     Move(g,new Vector3(bounds.center.x,0,bounds.center.z));yield return new WaitForSecondsRealtime(.35f);cover.enabled=false;
     Check(cover.Opacity==1&&!cover.ObscuringPlayer&&cover.surfaces.All(s=>s.renderer.sharedMaterials.SequenceEqual(s.opaque)),"Disable did not restore");cover.enabled=true;
    }
    Check(Resources.FindObjectsOfTypeAll<Material>().Length==materials,"Fade created material instances");
    report.passed=true;reports.Add(report);Debug.Log("OCCLUSION_CHAPTER_PASS "+JsonUtility.ToJson(report));
   }
   File.WriteAllText(Path.Combine(output,"occlusion-pass.json"),JsonUtility.ToJson(new Results{chapters=reports.ToArray()},true));Application.Quit(0);
  }
  [Serializable]class Results{public Report[] chapters;}
  [Serializable]class Report{public int chapter,covers,surfaces,transitions;public double prepareMs;public float firstVisitMaxMs,repeatVisitMaxMs;public bool passed;}
 }
}
