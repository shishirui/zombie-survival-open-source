using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace DeadDistrict {
 public sealed class LootHintValidation:MonoBehaviour {
  string output;SurvivalGame g;Text label;float trackingError;int boundaryFlickers,targetSwitches;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var a=RuntimeLaunch.Arguments();if(Array.IndexOf(a,"-loot-hint-smoke")<0)return;var v=new GameObject("Opt-in loot hint validation").AddComponent<LootHintValidation>();int n=Array.IndexOf(a,"-validation-output");v.output=n>=0?a[n+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;v.StartCoroutine(v.Run());}
  void Check(bool ok,string why){if(!ok)throw new Exception(why);}
  void Move(Vector3 p){var c=g.PlayerHealth.GetComponent<CharacterController>();c.enabled=false;c.transform.position=p;c.enabled=true;}
  bool Shows(string text)=>label.enabled&&label.text==text;
  IEnumerator Run(){var c=Core();while(true){object item=null;bool more=false;string error=null;try{more=c.MoveNext();if(more)item=c.Current;}catch(Exception ex){error=ex.ToString();}if(error!=null){File.WriteAllText(Path.Combine(output,"loot-hint-failure.txt"),error);Debug.LogError("LOOT_HINT_FAIL "+error);Application.Quit(1);yield break;}if(!more)yield break;yield return item;}}
  IEnumerator Core(){yield return null;yield return null;g=SurvivalGame.Instance;g.CombatValidation=true;g.Growth.Enabled=false;g.Supplies.Clear();g.WeaponCrates.Clear();label=GameObject.Find("Nearest supply").GetComponent<Text>();
   Check(g.Supplies.TrySpawn(SupplyKind.Ammo,new Vector3(3,0,0)),"Tracking fixture");var root=g.Supplies.transform.Cast<Transform>().First(t=>t.gameObject.activeSelf&&t.name.StartsWith("Pooled supply"));var position=root.position;Check(root.GetComponentsInChildren<Transform>().Any(t=>t!=root&&Mathf.Abs(t.localScale.x-1.2f)<.001f),"Supply model not enlarged at spawn");
   yield return new WaitForSeconds(.25f);
   for(int i=0;i<55;i++){Move(new Vector3(0,0,Mathf.Sin(i*.13f)*.8f));yield return new WaitForEndOfFrame();Check(Shows("补弹箱"),"Moving caption disappeared");var expected=Camera.main.WorldToScreenPoint(position+Vector3.up*1.35f);var actual=RectTransformUtility.WorldToScreenPoint(null,label.rectTransform.position);trackingError=Mathf.Max(trackingError,Vector2.Distance(actual,expected));}
   Check(trackingError<.8f,"Loot caption lags camera while moving: "+trackingError+" screen pixels");
   for(int i=0;i<90;i++){Move(position+Vector3.forward*(i/6%2==0?4.44f:4.56f));yield return new WaitForEndOfFrame();if(!Shows("补弹箱"))boundaryFlickers++;}
   Check(boundaryFlickers==0,"Caption flickers at distance boundary: "+boundaryFlickers);
   Move(position+Vector3.forward*6);yield return new WaitForSeconds(.2f);Check(!label.enabled||label.text=="","Caption retained after walking away");
   g.Supplies.Clear();Move(Vector3.zero);Check(g.Supplies.TrySpawn(SupplyKind.Medkit,new Vector3(-1,0,3))&&g.Supplies.TrySpawn(SupplyKind.Ammo,new Vector3(1,0,3)),"Adjacent supply fixtures");Move(new Vector3(-.6f,0,0));yield return new WaitForSeconds(.25f);Check(Shows("急救包"),"Initial nearest item");
   for(int i=0;i<80;i++){Move(new Vector3(i/6%2==0?-.08f:.08f,0,0));yield return new WaitForEndOfFrame();if(!Shows("急救包"))targetSwitches++;}
   Check(targetSwitches==0,"Equal-distance items fight for caption");
   Check(g.WeaponCrates.TrySpawn(new Vector3(2.5f,0,4)),"Weapon caption fixture");Move(new Vector3(2,0,2));yield return new WaitForSeconds(.35f);Check(Shows("补弹箱"),"Weapon overrides closer supply or target never switches");
   typeof(SurvivalGame).GetProperty("Ammo").SetValue(g,50);Move(new Vector3(1,0,3));yield return null;yield return new WaitForEndOfFrame();Check(g.Ammo==100&&!Shows("补弹箱"),"Collected item keeps stale caption");
   g.Supplies.Clear();Move(new Vector3(2.5f,0,1));yield return new WaitForSeconds(.25f);Check(Shows("霰弹枪箱"),"Weapon label missing");g.WeaponCrates.Clear();yield return null;yield return new WaitForEndOfFrame();Check(!label.enabled||label.text=="","Retired weapon retains caption");
   Move(Vector3.zero);Check(g.Supplies.TrySpawn(SupplyKind.Medkit,new Vector3(0,0,3)),"Wall hint fixture");yield return new WaitForSeconds(.2f);Check(Shows("急救包"),"Wall fixture caption missing");
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=8;wall.transform.position=new Vector3(0,1,1.5f);wall.transform.localScale=new Vector3(3,2,.3f);Physics.SyncTransforms();yield return new WaitForSeconds(.2f);Check(!label.enabled||label.text=="","Caption visible through solid cover");Destroy(wall);yield return new WaitForSeconds(.3f);Check(Shows("急救包"),"Caption not restored after cover removed");
   g.TogglePause();yield return new WaitForEndOfFrame();Check(!label.enabled||label.text=="","Caption visible on pause");g.TogglePause();yield return null;yield return new WaitForEndOfFrame();Check(Shows("急救包"),"Caption missing on resume");
   ScreenCapture.CaptureScreenshot(Application.isMobilePlatform?"loot-hint.png":Path.Combine(output,"loot-hint.png"));yield return new WaitForSeconds(.3f);g.PlayerHealth.Kill();yield return new WaitForEndOfFrame();Check(!label.enabled||label.text=="","Caption visible after death");
   File.WriteAllText(Path.Combine(output,"loot-hint-pass.json"),JsonUtility.ToJson(new Report{passed=true,maximumTrackingErrorPixels=trackingError,boundaryFlickerFrames=boundaryFlickers,unwantedTargetSwitchFrames=targetSwitches,nearerTargetEventuallySelected=true,weaponAndSupplyShareSelection=true,collectedAndRetiredHintsRemoved=true,coverPauseDeathHandled=true},true));Debug.Log("LOOT_HINT_PASS");Application.Quit(0);
  }
  [Serializable]class Report{public bool passed,nearerTargetEventuallySelected,weaponAndSupplyShareSelection,collectedAndRetiredHintsRemoved,coverPauseDeathHandled;public float maximumTrackingErrorPixels;public int boundaryFlickerFrames,unwantedTargetSwitchFrames;}
 }
}
