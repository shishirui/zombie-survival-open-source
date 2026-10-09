using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
namespace DeadDistrict {
 public sealed class SceneDressingValidation:MonoBehaviour {
  static bool started;string output;SurvivalGame g;int photos;readonly List<string> results=new List<string>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var a=RuntimeLaunch.Arguments();if(started||!a.Contains("-dressing-smoke"))return;started=true;ChapterProgress.IsolatedValidation=true;if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;SceneManager.sceneLoaded+=(s,m)=>{if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;};var v=new GameObject("Opt-in scene dressing validation").AddComponent<SceneDressingValidation>();DontDestroyOnLoad(v.gameObject);int n=Array.IndexOf(a,"-validation-output");v.output=n>=0?a[n+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);v.StartCoroutine(v.Run());}
  void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  void Move(Vector3 p){var c=g.PlayerHealth.GetComponent<CharacterController>();c.enabled=false;c.transform.position=p;c.enabled=true;Physics.SyncTransforms();Camera.main.transform.position=p+new Vector3(-14,21,-14);}
  IEnumerator Photo(string name){g.hud.Refresh();yield return new WaitForSecondsRealtime(.8f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.2f);photos++;}
  IEnumerator Run(){var stack=new Stack<IEnumerator>();stack.Push(Core());while(stack.Count>0){object item=null;bool more=false;string failure=null;try{more=stack.Peek().MoveNext();if(more)item=stack.Peek().Current;}catch(Exception e){failure=e.ToString();}if(failure!=null){Time.timeScale=1;File.WriteAllText(Path.Combine(output,"dressing-failure.txt"),failure);Debug.LogError("DRESSING_RUNTIME_FAIL "+failure);Application.Quit(1);yield break;}if(!more){stack.Pop();continue;}if(item is IEnumerator nested)stack.Push(nested);else yield return item;}}
  IEnumerator Core(){yield return null;yield return null;g=SurvivalGame.Instance;for(int i=0;i<2;i++)ChapterProgress.Complete(ChapterCatalog.Get(i).id,300,300);
   for(int chapter=1;chapter<=3;chapter++){
    if(chapter>1){var old=g;Check(g.BeginChapter(chapter-1),"Chapter failed");while(ReferenceEquals(old,SurvivalGame.Instance))yield return null;yield return null;g=SurvivalGame.Instance;}
    g.CombatValidation=true;g.enabled=false;g.Growth.Enabled=false;foreach(var e in g.Enemies)if(e.Alive)e.Retire();g.ResumeRun();
    var root=GameObject.Find("Chapter detail pass");Check(root,"Missing detail root");var colliders=root.GetComponentsInChildren<BoxCollider>();
    foreach(var renderer in root.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)Check(material&&material.shader&&material.shader.isSupported,"Unsupported material "+renderer.name);
    foreach(var prop in g.World.Props)foreach(var collider in colliders)Check(!collider.bounds.Intersects(prop.GetComponent<Collider>().bounds),"Interaction intersects new dressing: "+prop.name+" / "+collider.name);
    foreach(var site in g.Chapter.weaponSites!=null&&g.Chapter.weaponSites.Length>0?g.Chapter.weaponSites:new[]{new Vector3(4,0,1),new Vector3(12,0,6),new Vector3(-12,0,-6)})typeof(WeaponLoot).GetMethod("SpawnNear",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(g.WeaponCrates,new object[]{site});
    Check(g.WeaponCrates.ActiveCount==3,"Three opening weapon sites must remain accessible");
    foreach(var cover in root.GetComponentsInChildren<StreetOcclusion>())Check(cover.obstacle&&cover.surfaces.Length>0,"Tall cover missing player visibility protection");
    Move(Vector3.zero);yield return Photo("chapter-"+chapter+"-opening");
    var sites=chapter==1?new[]{new Vector3(7,0,-16),new Vector3(-6,0,17),new Vector3(11,0,11)}:chapter==2?new[]{new Vector3(6,0,3),new Vector3(-6,0,8),new Vector3(-6,0,-27)}:new[]{new Vector3(6,0,-6),new Vector3(-6,0,8),new Vector3(7,0,24),new Vector3(-19,0,-31)};
    foreach(var site in sites){Check(NavMesh.SamplePosition(site,out var hit,2,NavMesh.AllAreas),"Photo site inaccessible "+site);var path=new NavMeshPath();Check(NavMesh.CalculatePath(Vector3.zero,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Landmark disconnected "+site);Move(hit.position);yield return Photo("chapter-"+chapter+"-detail-"+Array.IndexOf(sites,site));}
    foreach(var fx in root.GetComponentsInChildren<DistrictAtmosphere>()){
     Move(fx.transform.position+Vector3.right*3);yield return new WaitForSecondsRealtime(.4f);Check(fx.Emitting,"Nearby ambient effect inactive");var systems=fx.GetComponentsInChildren<ParticleSystem>();float end=Time.unscaledTime+2;bool emitted=systems.Any(p=>p.particleCount>0);while(!emitted&&Time.unscaledTime<end){yield return null;emitted=systems.Any(p=>p.particleCount>0);}Check(emitted,"Ambient effect emits no particles at "+fx.transform.position);g.PauseForShell();yield return null;Check(systems.All(p=>p.isPaused),"Ambient effect not paused with game");g.ResumeRun();yield return null;Move(fx.transform.position+Vector3.right*35);yield return new WaitForSecondsRealtime(.4f);Check(!fx.Emitting&&systems.All(p=>p.particleCount==0),"Distant effect not culled");
    }
    Move(Vector3.zero);RenderSettings.fog=false;var cam=Camera.main;cam.farClipPlane=300;cam.orthographicSize=chapter==1?43:chapter==2?48:55;cam.transform.position=new Vector3(-42,62,-42);cam.transform.rotation=Quaternion.Euler(48,45,0);yield return Photo("chapter-"+chapter+"-overview");results.Add("chapter="+chapter+" newSolidProps="+colliders.Length+" ambientSites="+root.GetComponentsInChildren<DistrictAtmosphere>().Length+" openingWeapons=3 interactionsClear=true landmarksReachable=true");
   }
   File.WriteAllText(Path.Combine(output,"dressing-pass.txt"),string.Join("\n",results)+"\nscreenshots="+photos);Debug.Log("DRESSING_RUNTIME_PASS");Application.Quit(0);
  }
 }
}
