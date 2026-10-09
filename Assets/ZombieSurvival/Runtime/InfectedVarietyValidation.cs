using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class InfectedVarietyValidation:MonoBehaviour {
  SurvivalGame g;string output;int screenshots;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var a=RuntimeLaunch.Arguments();if(!a.Contains("-infected-variety-smoke"))return;ChapterProgress.IsolatedValidation=true;SurvivalGame.Instance.CombatValidation=true;var v=new GameObject("Opt-in infected appearance validation").AddComponent<InfectedVarietyValidation>();int i=Array.IndexOf(a,"-validation-output");v.output=a[i+1];Directory.CreateDirectory(v.output);v.StartCoroutine(v.Run());}
  void Check(bool ok,string why){if(!ok)throw new Exception(why);}
  IEnumerator Run(){var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(Core());while(stack.Count>0){object item=null;bool more=false;string fail=null;try{more=stack.Peek().MoveNext();if(more)item=stack.Peek().Current;}catch(Exception e){fail=e.ToString();}if(fail!=null){Time.timeScale=1;File.WriteAllText(Path.Combine(output,"variety-failure.txt"),fail);Debug.LogError(fail);Application.Quit(1);yield break;}if(!more){stack.Pop();continue;}if(item is IEnumerator nested)stack.Push(nested);else yield return item;}}
  IEnumerator Photo(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.25f);screenshots++;}
  IEnumerator Core(){
   yield return null;yield return null;g=SurvivalGame.Instance;g.enabled=false;g.Growth.Enabled=false;g.Supplies.Clear();g.WeaponCrates.Clear();
   var pack=Resources.Load<PackVisuals>("DeadDistrict/PackVisuals");Check(pack.infected.Length==4&&pack.corpses.Length==4,"Variant count mismatch");
   Check(ChapterCatalog.Get(0).waves.Sum(w=>w.Count)==286&&g.config.maxAlive==100,"First chapter difficulty changed");
   foreach(var e in g.Enemies.Where(z=>z.Alive))e.Retire();
   var cam=Camera.main;cam.transform.position=new Vector3(0,6,-10);cam.transform.LookAt(new Vector3(0,1,5));cam.orthographicSize=5;
   var variants=g.Enemies.Take(4).ToArray();var meshIds=new System.Collections.Generic.HashSet<int>();
   for(int i=0;i<4;i++){
    var e=variants[i];e.Spawn(new Vector3(-4.5f+i*3,0,5),0);e.Model.rotation=Quaternion.Euler(0,180,0);e.Actor.MoveInfected(1.4f,.8f);
    var a=e.Model.GetComponentInChildren<Animator>();Check(a.isHuman&&a.avatar.isValid&&!a.applyRootMotion,"Humanoid rig invalid");
    var skin=e.Model.GetComponentInChildren<SkinnedMeshRenderer>();meshIds.Add(skin.sharedMesh.GetInstanceID());Check(skin.sharedMaterials.All(m=>m&&m.shader.isSupported),"Material unsupported");
    Check(e.GetComponent<CapsuleCollider>().radius==.35f,"Visual width changed hitbox");
   }
   Check(meshIds.Count==4,"Variants reuse same body mesh");yield return new WaitForSeconds(.6f);yield return Photo("01-four-clothed-infected");
   // Animate and hit every variant, then verify each corpse keeps its own body and attachments.
   foreach(var e in variants){e.ReactToHit(Vector3.forward);Check(e.HitReactionsPlayed>0,"No hit response");}yield return new WaitForSeconds(.3f);
   var skins=variants.Select(e=>e.Model.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh).ToArray();foreach(var e in variants)e.TakeBullet(10000,Vector3.forward,false);
   yield return null;var corpses=g.Corpses.GetComponentsInChildren<CorpseVisual>();Check(corpses.Length==4&&corpses.All(c=>skins.Contains(c.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh)),"Corpse appearance changed on death");
   foreach(var c in corpses)foreach(var j in c.GetComponentsInChildren<CharacterJoint>())Check(Vector3.Distance(j.transform.TransformPoint(j.anchor),j.connectedBody.transform.TransformPoint(j.connectedAnchor))<.05f,"Joint stretched on death: "+c.name+" "+j.name+" gap="+Vector3.Distance(j.transform.TransformPoint(j.anchor),j.connectedBody.transform.TransformPoint(j.connectedAnchor)));
   yield return new WaitForSeconds(3.6f);yield return Photo("02-matching-settled-corpses");
   Check(g.Corpses.SimulatedCount==0,"Bodies did not settle");var bodies=corpses.SelectMany(c=>c.bodies).ToArray();var positions=bodies.Select(b=>b.position).ToArray();yield return new WaitForSeconds(.8f);for(int i=0;i<bodies.Length;i++)Check(Vector3.Distance(bodies[i].position,positions[i])<.001f,"Settled corpse rises or moves");yield return new WaitForSeconds(1.3f);Check(g.Corpses.ActiveCount==0,"Corpse expiry failed");
   // Use slots whose normal appearance differs from their brute appearance to catch wrong corpse selection.
   for(int i=0;i<2;i++){var e=g.Enemies[i+2];e.Spawn(new Vector3(-2+i*4,0,5),0,EnemyKind.Brute);e.Model.rotation=Quaternion.Euler(0,180,0);e.Actor.MoveInfected(1,.6f);}yield return new WaitForSeconds(.5f);yield return Photo("03-heavy-infected");
   foreach(var e in g.Enemies.Where(z=>z.Alive).ToArray())e.TakeBullet(10000,Vector3.forward,false);yield return new WaitForSeconds(3.6f);Check(g.Corpses.SimulatedCount==0,"Brute corpse did not settle");yield return Photo("04-heavy-corpses");
   for(int i=0;i<16;i++){var e=g.Enemies[i];e.Spawn(new Vector3(-5+i%4*3,0,4+i/4*2),0);e.Model.rotation=Quaternion.Euler(0,195,0);e.Actor.MoveInfected(1.4f,.8f);}
   cam.transform.position=g.PlayerPosition+new Vector3(-14,21,-14);cam.transform.rotation=Quaternion.Euler(48,45,0);cam.orthographicSize=9;yield return new WaitForSeconds(.5f);yield return Photo("05-gameplay-crowd");
   File.WriteAllText(Path.Combine(output,"variety-pass.json"),"{\"passed\":true,\"distinctBodies\":4,\"matchedCorpses\":4,\"heavies\":2,\"settledAndExpired\":true,\"screenshots\":"+screenshots+"}");Application.Quit(0);
  }
 }
}
