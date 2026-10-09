using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class GrenadeVisualValidation:MonoBehaviour {
  string output;SurvivalGame g;int peakParticles;float blastAt;bool capture;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var a=RuntimeLaunch.Arguments();if(Array.IndexOf(a,"-grenade-visual-smoke")<0)return;var v=new GameObject("Opt-in grenade visual validation").AddComponent<GrenadeVisualValidation>();v.capture=Array.IndexOf(a,"-grenade-visual-capture")>=0;int n=Array.IndexOf(a,"-validation-output");v.output=n>=0?a[n+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;v.StartCoroutine(v.Run());}
  void Check(bool ok,string why){if(!ok)throw new Exception(why);}
  IEnumerator Run(){var core=Core();while(true){object item=null;bool more=false;string fail=null;try{more=core.MoveNext();if(more)item=core.Current;}catch(Exception e){fail=e.ToString();}if(fail!=null){Time.timeScale=1;File.WriteAllText(Path.Combine(output,"grenade-visual-failure.txt"),fail);Debug.LogError("GRENADE_VISUAL_FAIL "+fail);Application.Quit(1);yield break;}if(!more)yield break;yield return item;}}
  IEnumerator Core(){yield return null;yield return null;g=SurvivalGame.Instance;g.CombatValidation=true;g.Growth.Enabled=false;g.Supplies.Clear();g.WeaponCrates.Clear();g.config.aimRange=0;
   Check(g.BeginGrenadeAim(),"Cannot aim grenade");g.SetGrenadeAim(new Vector2(-.6f,0));var landing=g.GrenadeAimPoint;
   for(int i=0;i<3;i++){Check(NavMesh.SamplePosition(landing+new Vector3((i-1)*.8f,0,i%2*.6f),out var nav,.4f,NavMesh.AllAreas),"Victim floor");g.Enemies[i].Spawn(nav.position,0);}
   var outside=landing+Vector3.forward*6.5f;Check(NavMesh.SamplePosition(outside,out var far,.5f,NavMesh.AllAreas),"Outside radius fixture");g.Enemies[3].Spawn(far.position,0);
   int stock=g.Grenades;g.EndGrenadeAim(true);Check(g.Grenades==stock-1&&g.GrenadesThrown==1,"Throw stock");while(g.GrenadeInFlight)yield return null;blastAt=Time.time;
   Check(g.GrenadeKills==3&&!g.Enemies[0].Alive&&!g.Enemies[1].Alive&&!g.Enemies[2].Alive&&g.Enemies[3].Alive,"Visual change altered blast damage/radius");g.Enemies[3].Retire();
   var roots=g.VisualEffects.transform.Cast<Transform>().Where(t=>t.name=="EpicGrenade(Clone)").ToArray();Check(roots.Length==3,"Explosion pool must stay at three slots");
   foreach(var t in roots)foreach(var p in t.GetComponentsInChildren<ParticleSystem>(true)){Check(!p.main.loop&&p.main.stopAction==ParticleSystemStopAction.None,"Looping or destructive pooled effect");var renderer=p.GetComponent<ParticleSystemRenderer>();Check(renderer.sharedMaterials.All(m=>m&&m.shader&&m.shader.name.StartsWith("Universal Render Pipeline")),"Explosion material not URP compatible");}
   foreach(float phase in new[]{.06f,.18f,.40f,.85f,1.6f}){
    while(Time.time-blastAt<phase){peakParticles=Mathf.Max(peakParticles,roots.Sum(t=>t.GetComponentsInChildren<ParticleSystem>(true).Sum(p=>p.particleCount)));yield return null;}
    if(capture){Time.timeScale=0;yield return new WaitForEndOfFrame();string name="grenade-"+Mathf.RoundToInt(phase*1000)+"ms.png";ScreenCapture.CaptureScreenshot(Application.isMobilePlatform?name:Path.Combine(output,name));yield return new WaitForSecondsRealtime(.18f);Check(File.Exists(Path.Combine(output,name)),"Capture missing");Time.timeScale=1;}
   }
   yield return new WaitForSeconds(2.1f);Check(roots.All(t=>!t.gameObject.activeSelf),"Explosion never returns to pool");
   for(int i=0;i<7;i++)g.VisualEffects.Explosion(landing+Vector3.right*(i%3));yield return new WaitForSeconds(.2f);Check(roots.Count(t=>t.gameObject.activeSelf)==3,"Pool reuse/cap");int stacked=roots.Sum(t=>t.GetComponentsInChildren<ParticleSystem>(true).Sum(p=>p.particleCount));Check(stacked<900,"Overlapping explosion particle budget exceeded: "+stacked);
   yield return new WaitForSeconds(3.5f);Check(roots.All(t=>!t.gameObject.activeSelf),"Reused explosions did not expire");
   File.WriteAllText(Path.Combine(output,"grenade-visual-pass.json"),JsonUtility.ToJson(new Report{passed=true,actualManualGrenade=true,kills=g.GrenadeKills,stockConsumed=1,blastRadiusUnchanged=true,poolCapacity=roots.Length,peakSingleExplosionParticles=peakParticles,overlappingParticles=stacked,pooledEffectsExpireAndReuse=true,materialsURP=true},true));Debug.Log("GRENADE_VISUAL_PASS");Application.Quit(0);
  }
  [Serializable]class Report{public bool passed,actualManualGrenade,blastRadiusUnchanged,pooledEffectsExpireAndReuse,materialsURP;public int kills,stockConsumed,poolCapacity,peakSingleExplosionParticles,overlappingParticles;}
 }
}
