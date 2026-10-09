using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
namespace DeadDistrict {
 public sealed class GrenadeAimValidation : MonoBehaviour {
  string output;bool capture;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot(){var args=Environment.GetCommandLineArgs();bool capture=Array.IndexOf(args,"-grenade-aim-capture")>=0;if(!capture&&Array.IndexOf(args,"-grenade-aim-smoke")<0)return;var v=new GameObject("Opt-in manual grenade validation").AddComponent<GrenadeAimValidation>();v.capture=capture;int n=Array.IndexOf(args,"-validation-output");v.output=n>=0?args[n+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;v.StartCoroutine(v.Run());}
  void Require(bool condition,string reason){if(!condition)throw new Exception(reason);}
  IEnumerator Run(){var routine=Core();while(true){bool next=false;object current=null;string failure=null;try{next=routine.MoveNext();if(next)current=routine.Current;}catch(Exception e){failure=e.ToString();}if(failure!=null){Time.timeScale=1;File.WriteAllText(Path.Combine(output,"grenade-aim-failure.txt"),failure);Debug.LogError("GRENADE_AIM_FAIL "+failure);Application.Quit(1);yield break;}if(!next)yield break;yield return current;}}
  IEnumerator Core(){
   yield return null;yield return null;var g=SurvivalGame.Instance;g.CombatValidation=true;var pad=FindFirstObjectByType<GrenadeAimInput>();var rect=(RectTransform)pad.transform;var center=(Vector2)RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));float radius=rect.rect.width*.5f*rect.GetComponentInParent<Canvas>().scaleFactor;var e=new PointerEventData(EventSystem.current){pointerId=7,position=center};int count=g.Grenades;
   pad.OnPointerDown(e);Require(g.IsGrenadeAiming&&!g.GrenadeAimValid&&g.Grenades==count,"press consumed grenade or aimed without direction");
   e.position=center+Vector2.right*radius*.5f;pad.OnDrag(e);yield return null;
   Vector3 delta=g.GrenadeAimPoint-g.PlayerPosition;var right=Camera.main.transform.right;right.y=0;right.Normalize();Require(g.GrenadeAimValid&&Vector3.Dot(delta.normalized,right)>.99f&&delta.magnitude>4&&delta.magnitude<8,"right drag direction or adjustable distance");
   var other=new PointerEventData(EventSystem.current){pointerId=8,position=center+Vector2.up*radius};Vector3 desired=g.GrenadeAimPoint;pad.OnDrag(other);pad.OnPointerUp(other);Require(g.IsGrenadeAiming&&(g.GrenadeAimPoint-desired).sqrMagnitude<.001f,"another finger changed or released aim");
   SurvivalInput.Move=new Vector2(.6f,.2f);
   if(capture){yield return new WaitForSeconds(.15f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"grenade-aim.png"));yield return new WaitForSeconds(.3f);Require(File.Exists(Path.Combine(output,"grenade-aim.png")),"aim screenshot missing");}
   desired=g.GrenadeAimPoint;var preview=GameObject.Find("Manual grenade trajectory").GetComponent<LineRenderer>();Require(preview.gameObject.activeSelf&&Vector3.Distance(preview.GetPosition(31),desired)<.01f,"preview endpoint differs from chosen landing");
   Require(NavMesh.SamplePosition(desired,out var landing,.5f,NavMesh.AllAreas),"manual destination is not walkable fixture");var victim=g.Enemies[0];victim.Spawn(landing.position,0);var forward=Camera.main.transform.forward;forward.y=0;forward.Normalize();Require(NavMesh.SamplePosition(g.PlayerPosition+forward*2,out var close,.5f,NavMesh.AllAreas),"auto target fixture missing");var autoTarget=g.Enemies[1];autoTarget.Spawn(close.position,0);autoTarget.Health.SetHealth(1000);
   pad.OnPointerUp(e);Require(!g.IsGrenadeAiming&&g.Grenades==count-1&&g.GrenadesThrown==1&&Vector3.Distance(g.GrenadeLandingPoint,desired)<.01f,"release did not use manual destination");Require(SurvivalInput.Move==new Vector2(.6f,.2f),"grenade gesture cleared movement finger");pad.OnPointerUp(e);Require(g.Grenades==count-1,"duplicate release threw again");
   yield return new WaitForSeconds(1.1f);Require(!victim.Alive&&autoTarget.Alive&&g.GrenadeKills>=1,"grenade ignored chosen blast area or chased auto target");autoTarget.Retire();SurvivalInput.Move=Vector2.zero;
   e.position=center;pad.OnPointerDown(e);e.position=center+Vector2.up*radius*3;pad.OnDrag(e);Require(Mathf.Abs(Vector3.Distance(g.PlayerPosition,g.GrenadeAimPoint)-12)<.01f,"maximum throw distance");g.TogglePause();Require(!g.IsGrenadeAiming,"pause retained aim");pad.OnPointerUp(e);Require(g.Grenades==count-1,"pause caused throw on release");g.TogglePause();
   e.position=center;pad.OnPointerDown(e);pad.OnPointerUp(e);Require(g.Grenades==count-1&&!g.IsGrenadeAiming,"tap caused accidental throw");
   pad.OnPointerDown(e);e.position=center-Vector2.right*radius;pad.OnDrag(e);e.position=center;pad.OnDrag(e);pad.OnPointerUp(e);Require(g.Grenades==count-1,"return to origin failed to cancel");
   pad.OnPointerDown(e);e.position=center-Vector2.up*radius;pad.OnDrag(e);pad.Cancel();pad.OnPointerUp(e);Require(g.Grenades==count-1&&!g.IsGrenadeAiming,"focus/disable cancellation consumed grenade");
   e.position=center;pad.OnPointerDown(e);e.position=center+Vector2.up*radius;pad.OnDrag(e);pad.OnPointerUp(e);Require(g.Grenades==0,"second manual throw did not consume final grenade");yield return new WaitForSeconds(1);e.position=center;pad.OnPointerDown(e);Require(!g.IsGrenadeAiming,"zero stock allowed aim");
   g.ApplySupply(SupplyKind.Grenade);pad.OnPointerDown(e);e.position=center+Vector2.right*radius;pad.OnDrag(e);g.PlayerHealth.Kill();pad.OnPointerUp(e);Require(g.Dead&&!g.IsGrenadeAiming&&g.GrenadesThrown==2,"death leaked a pending throw");
   File.WriteAllText(Path.Combine(output,"grenade-aim-pass.json"),"{\"passed\":true,\"manualDirectionAndDistance\":true,\"releaseUsesPreviewDestination\":true,\"manualBlastArea\":true,\"independentMovementFinger\":true,\"ignoresOtherPointer\":true,\"singleConsumption\":true,\"rangeCap12m\":true,\"tapAndReturnCancel\":true,\"pauseAndFocusCancel\":true,\"zeroStockRejected\":true,\"deathCancelsPendingThrow\":true}");Debug.Log("GRENADE_AIM_PASS");Application.Quit(0);
  }
 }
}
