using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class CorpseValidation:MonoBehaviour {
  string output;SurvivalGame g;readonly List<Frame> frames=new List<Frame>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var args=RuntimeLaunch.Arguments();if(Array.IndexOf(args,"-corpse-capture")<0)return;var r=new GameObject("Opt-in corpse validation").AddComponent<CorpseValidation>();int n=Array.IndexOf(args,"-validation-output");r.output=n>=0?args[n+1]:Application.persistentDataPath;Directory.CreateDirectory(r.output);if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;r.StartCoroutine(r.Run());}
  void Check(bool ok,string why){if(!ok)throw new Exception(why);}
  IEnumerator Run(){var core=Core();while(true){object item=null;bool more=false;string fail=null;try{more=core.MoveNext();if(more)item=core.Current;}catch(Exception e){fail=e.ToString();}if(fail!=null){Time.timeScale=1;File.WriteAllText(Path.Combine(output,"corpse-failure.txt"),fail);Debug.LogError("CORPSE_VALIDATION_FAIL "+fail);Application.Quit(1);yield break;}if(!more)yield break;yield return item;}}
  Frame Measure(string phase){var f=new Frame{phase=phase};var models=g.Corpses.GetComponentsInChildren<CorpseVisual>();f.models=models.Length;f.simulated=g.Corpses.SimulatedCount;foreach(var m in models){foreach(var rb in m.bodies){f.maxBodyHeight=Mathf.Max(f.maxBodyHeight,rb.position.y);f.minBodyHeight=Mathf.Min(f.minBodyHeight,rb.position.y);}foreach(var j in m.GetComponentsInChildren<CharacterJoint>()){if(j.connectedBody)f.maxAnchorSeparation=Mathf.Max(f.maxAnchorSeparation,Vector3.Distance(j.transform.TransformPoint(j.anchor),j.connectedBody.transform.TransformPoint(j.connectedAnchor)));}var hips=m.bones[(int)HumanBodyBones.Hips];var head=m.bones[(int)HumanBodyBones.Head];f.maxHeadHipsHeight=Mathf.Max(f.maxHeadHipsHeight,Mathf.Abs(head.position.y-hips.position.y));}return f;}
  IEnumerator Photo(string name){frames.Add(Measure(name));File.WriteAllText(Path.Combine(output,"corpse-diagnostics.json"),JsonUtility.ToJson(new Report{frames=frames.ToArray()},true));Time.timeScale=0;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Application.isMobilePlatform?name+".png":Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.25f);float deadline=Time.realtimeSinceStartup+3;while(!File.Exists(Path.Combine(output,name+".png"))&&Time.realtimeSinceStartup<deadline)yield return null;Check(File.Exists(Path.Combine(output,name+".png")),"Screenshot missing: "+name);Time.timeScale=1;}
  IEnumerator Core(){yield return null;yield return null;g=SurvivalGame.Instance;g.CombatValidation=true;g.Growth.Enabled=false;g.config.aimRange=0;g.Supplies.Clear();g.WeaponCrates.Clear();Camera.main.orthographicSize=6.5f;var forward=Camera.main.transform.forward;forward.y=0;forward.Normalize();var right=Camera.main.transform.right;right.y=0;right.Normalize();
   for(int i=0;i<3;i++){var point=g.PlayerPosition+forward*3+right*(i-1)*3;Check(NavMesh.SamplePosition(point,out var nav,.8f,NavMesh.AllAreas),"Fixture floor missing");g.Enemies[i].Spawn(nav.position,0,(EnemyKind)i);}
   yield return new WaitForSeconds(.3f);for(int i=0;i<3;i++)g.Enemies[i].TakeBullet(1000,forward,false);yield return Photo("bullet-start");yield return new WaitForSeconds(.35f);yield return Photo("bullet-falling");yield return new WaitForSeconds(1);yield return Photo("bullet-early-settle");yield return new WaitForSeconds(2.1f);yield return Photo("bullet-settled");
   yield return new WaitForSeconds(.95f);var stillBodies=g.Corpses.GetComponentsInChildren<CorpseVisual>().SelectMany(m=>m.bodies).ToArray();var stillPositions=stillBodies.Select(rb=>rb.transform.position).ToArray();var stillRotations=stillBodies.Select(rb=>rb.transform.rotation).ToArray();
   yield return new WaitForSeconds(.95f);for(int i=0;i<stillBodies.Length;i++)Check(Vector3.Distance(stillPositions[i],stillBodies[i].transform.position)<.001f&&Quaternion.Angle(stillRotations[i],stillBodies[i].transform.rotation)<.1f,"Settled corpse moves/twists during exit");yield return Photo("bullet-before-disappear");
   yield return new WaitForSeconds(.3f);Check(g.Corpses.ActiveCount==0,"Corpse expiry failed");
   for(int i=0;i<3;i++){var point=g.PlayerPosition+forward*3+right*(i-1)*3;Check(NavMesh.SamplePosition(point,out var nav,.8f,NavMesh.AllAreas),"Blast floor missing");var z=g.Enemies[i];z.Spawn(nav.position,0,(EnemyKind)i);z.TakeBlast(1000,z.transform.position-forward*2);}
   yield return new WaitForSeconds(.4f);yield return Photo("blast-falling");yield return new WaitForSeconds(3.1f);yield return Photo("blast-settled");
   Check(frames[0].maxAnchorSeparation<.001f,"Death pose snapped at joints");foreach(var f in frames){Check(f.models==2&&f.minBodyHeight>-.15f&&f.maxAnchorSeparation<.05f,"Invalid body bounds or stretched joints at "+f.phase);if(f.phase.EndsWith("settled"))Check(f.maxBodyHeight<1&&f.maxHeadHipsHeight<.5f&&f.simulated==0,"Corpse not resting naturally: "+JsonUtility.ToJson(f));}
   File.WriteAllText(Path.Combine(output,"corpse-report.json"),JsonUtility.ToJson(new Report{passed=true,frames=frames.ToArray()},true));Debug.Log("CORPSE_CAPTURE_COMPLETE");Application.Quit(0);
  }
  [Serializable]class Frame{public string phase;public int models,simulated;public float maxBodyHeight,minBodyHeight=100,maxAnchorSeparation,maxHeadHipsHeight;}
  [Serializable]class Report{public bool passed;public Frame[] frames;}
 }
}
