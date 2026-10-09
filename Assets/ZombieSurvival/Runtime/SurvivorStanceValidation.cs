using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace DeadDistrict {
 public sealed class SurvivorStanceValidation:MonoBehaviour {
  string output;SurvivalGame g;LoftActor actor;Animator animator;float rightError,leftError;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var a=RuntimeLaunch.Arguments();if(!a.Contains("-survivor-stance-smoke"))return;ChapterProgress.IsolatedValidation=true;SurvivalGame.Instance.CombatValidation=true;var v=new GameObject("Opt-in survivor stance validation").AddComponent<SurvivorStanceValidation>();v.output=a[Array.IndexOf(a,"-validation-output")+1];Directory.CreateDirectory(v.output);v.StartCoroutine(v.Run());}
  void Check(bool ok,string why){if(!ok)throw new Exception(why);}
  IEnumerator Run(){var stack=new Stack<IEnumerator>();stack.Push(Core());while(stack.Count>0){object item=null;bool more=false;string fail=null;try{more=stack.Peek().MoveNext();if(more)item=stack.Peek().Current;}catch(Exception e){fail=e.ToString();}if(fail!=null){Time.timeScale=1;File.WriteAllText(Path.Combine(output,"stance-failure.txt"),fail);Debug.LogError(fail);Application.Quit(1);yield break;}if(!more){stack.Pop();continue;}if(item is IEnumerator nested)stack.Push(nested);else yield return item;}}
  Transform Gun=>actor.transform.Find(g.CurrentWeapon==WeaponKind.Launcher?"Grenade launcher":g.CurrentWeapon==WeaponKind.Shotgun?"Shotgun weapon":"Weapon");
  void Measure(){
   var gun=Gun;var right=animator.GetBoneTransform(HumanBodyBones.RightHand);var left=animator.GetBoneTransform(HumanBodyBones.LeftHand);
   float r=Vector3.Distance(right.position,gun.TransformPoint(new Vector3(0,.05f,-.16f)));var grip=g.CurrentWeapon==WeaponKind.Shotgun?new Vector3(-.02f,.12f,.13f):new Vector3(-.02f,.12f,.1f);float l=Vector3.Distance(left.position,gun.TransformPoint(grip));rightError=Mathf.Max(rightError,r);leftError=Mathf.Max(leftError,l);
   Check(r<.02f&&l<.06f,"Hands separated from weapon: "+g.CurrentWeapon+" right="+r+" left="+l);Check(Vector3.Dot(gun.forward,actor.transform.forward)>.999f,"Aim direction changed with pose");Check(actor.Muzzle&&actor.Muzzle.position.y>.8f&&actor.Muzzle.position.y<2,"Muzzle height invalid");
  }
  IEnumerator Photo(string name){g.hud.Refresh();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.2f);}
  IEnumerator Core(){
   yield return null;yield return null;g=SurvivalGame.Instance;g.enabled=false;g.Growth.Enabled=false;g.Supplies.Clear();g.WeaponCrates.Clear();actor=GameObject.Find("Survivor").GetComponentInChildren<LoftActor>();animator=actor.GetComponentInChildren<Animator>();actor.transform.rotation=Quaternion.Euler(0,90,0);
   var camera=Camera.main;camera.transform.position=new Vector3(2.5f,3,-7);camera.transform.LookAt(new Vector3(0,1,0));camera.orthographicSize=2.8f;
   for(int weapon=0;weapon<3;weapon++){
    if(weapon==1)Check(g.CollectShotgun(),"Shotgun selection failed");if(weapon==2)Check(g.CollectLauncher(),"Launcher selection failed");animator.SetFloat("Speed",0);yield return new WaitForSeconds(.5f);
    Check(animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name.Contains("RifleAimingIdle"))&&animator.GetCurrentAnimatorClipInfo(1).Any(c=>c.clip.name.Contains("RifleAimingIdle")),"Purchased aiming stance missing");
    var leftFoot=actor.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position);var rightFoot=actor.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightFoot).position);Check(Mathf.Abs(leftFoot.z-rightFoot.z)>.12f,"Idle feet remain parallel neutral stance");
    for(int i=0;i<35;i++){yield return new WaitForEndOfFrame();Measure();}yield return Photo("idle-"+g.CurrentWeapon);
    for(int i=0;i<70;i++){actor.Move(1);yield return new WaitForEndOfFrame();Measure();}Check(animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name.Contains("Running")),"Purchased running lost");yield return Photo("moving-"+g.CurrentWeapon);
    animator.SetFloat("Speed",0);yield return new WaitForSeconds(.5f);g.PauseForShell();yield return new WaitForEndOfFrame();var muzzle=actor.Muzzle.position;yield return new WaitForSecondsRealtime(.3f);Check(Vector3.Distance(muzzle,actor.Muzzle.position)<.001f,"Paused weapon drifts: "+Vector3.Distance(muzzle,actor.Muzzle.position));g.ResumeRun();
   }
   camera.transform.position=g.PlayerPosition+new Vector3(-14,21,-14);camera.transform.rotation=Quaternion.Euler(48,45,0);camera.orthographicSize=8.4f;
   g.config.aimRange=0;g.enabled=true;Check(g.TryRoll(),"Roll rejected");yield return new WaitForSeconds(.1f);Check(g.Rolling&&!Gun.gameObject.activeSelf&&animator.GetLayerWeight(1)==0,"Rifle stance overrides roll");yield return new WaitForSeconds(.6f);Check(!g.Rolling&&Gun.gameObject.activeSelf&&animator.GetLayerWeight(1)==1,"Roll did not restore aiming stance");g.enabled=false;animator.SetFloat("Speed",0);yield return new WaitForSeconds(.5f);yield return new WaitForEndOfFrame();Measure();
   camera.transform.position=g.PlayerPosition+new Vector3(-14,21,-14);camera.transform.rotation=Quaternion.Euler(48,45,0);camera.orthographicSize=8.4f;yield return Photo("restored-gameplay-view");
   File.WriteAllText(Path.Combine(output,"stance-pass.json"),"{\"passed\":true,\"purchasedAimIdle\":true,\"weapons\":3,\"idleRunGripAndDirection\":true,\"pauseAndRollRestore\":true,\"rightGripMaxError\":"+rightError.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"leftGripMaxError\":"+leftError.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");Application.Quit(0);
  }
 }
}
