using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class StreetSampleValidation:MonoBehaviour {
  string output;SurvivalGame game;int photos,traversed;float travelSeconds;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=RuntimeLaunch.Arguments();if(Array.IndexOf(args,"-street-capture")<0)return;
   var v=new GameObject("Opt-in street sample verification").AddComponent<StreetSampleValidation>();
   int i=Array.IndexOf(args,"-validation-output");v.output=i>=0?args[i+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);
   if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;v.StartCoroutine(v.Run());
  }
  void Check(bool condition,string message){if(!condition)throw new Exception(message);}
  IEnumerator Run(){var steps=Core();while(true){bool more=false;object item=null;string error=null;try{more=steps.MoveNext();if(more)item=steps.Current;}catch(Exception e){error=e.ToString();}if(error!=null){Time.timeScale=1;File.WriteAllText(Path.Combine(output,"street-failure.txt"),error);Debug.LogError("STREET_RUNTIME_FAIL "+error);Application.Quit(1);yield break;}if(!more)yield break;yield return item;}}
  void Move(Vector3 pos){var c=game.PlayerHealth.GetComponent<CharacterController>();c.enabled=false;c.transform.position=pos;c.enabled=true;}
  IEnumerator Photo(string name){yield return new WaitForSeconds(.8f);Time.timeScale=0;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.3f);Time.timeScale=1;photos++;}
  IEnumerator Core(){
   yield return null;yield return null;game=SurvivalGame.Instance;game.CombatValidation=true;game.Growth.Enabled=false;game.config.aimRange=0;
   var root=GameObject.Find("Commercial street sample");Check(root,"Sample missing");var buildings=root.GetComponentsInChildren<StreetOcclusion>();Check(buildings.Length==2,"Two storefronts required");
   foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())foreach(var mat in renderer.sharedMaterials)Check(mat&&mat.shader&&mat.shader.isSupported,"Unsupported material");
   var spawnNear=typeof(WeaponLoot).GetMethod("SpawnNear",BindingFlags.NonPublic|BindingFlags.Instance);
   foreach(var p in new[]{new Vector3(4,0,1),new Vector3(12,0,6),new Vector3(-12,0,-6)})spawnNear.Invoke(game.WeaponCrates,new object[]{p});
   Check(game.WeaponCrates.ActiveCount==3,"An opening weapon crate could not relocate around cover");game.WeaponCrates.Clear();
   foreach(var prop in game.World.Props){foreach(var obstacle in root.GetComponentsInChildren<BoxCollider>())Check(!obstacle.bounds.Intersects(prop.GetComponent<Collider>().bounds),"World interaction inside new obstacle: "+prop.transform.position+" / "+obstacle.name);}
   for(int i=0;i<6;i++){var position=new Vector3(-4+i*1.5f,0,6+i%2);Check(NavMesh.SamplePosition(position,out var hit,1,NavMesh.AllAreas),"Preview navigation");game.Enemies[i].Spawn(hit.position,0,EnemyKind.Normal);}
   Move(Vector3.zero);yield return Photo("01-opening-same-camera");foreach(var e in game.Enemies)if(e.Alive)e.Retire();
   Move(new Vector3(-8,0,0));yield return Photo("02-diner-corner");Move(new Vector3(3,0,-4));yield return Photo("03-grocery-parking");
   foreach(var building in buildings){
    var b=building.obstacle.bounds;var point=new Vector3(b.center.x,0,b.max.z+.8f);Move(point);yield return new WaitForSeconds(1.3f);
    Check(building.ObscuringPlayer&&building.Opacity<.2f,"Building did not reveal player "+building.name+" opacity="+building.Opacity);
    Check(building.obstacle.enabled,"Visibility must not remove collision");
    foreach(var s in building.surfaces)foreach(var mat in s.renderer.sharedMaterials)Check(mat.shader.isSupported&&mat.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"),"Ghost material missing in player build");
    yield return Photo("04-visibility-"+building.name);
    Move(Vector3.zero);yield return new WaitForSeconds(1.4f);Check(building.Opacity>.99f,"Building did not restore opacity");
   }
   // Drive the actual controller around the complete block perimeter and up the central lane.
   var controller=game.PlayerHealth.GetComponent<CharacterController>();game.enabled=false;Move(Vector3.zero);float started=Time.time;
   foreach(var dest in new[]{new Vector3(0,0,8),new Vector3(0,0,16),new Vector3(19,0,16),new Vector3(19,0,-15),new Vector3(0,0,-15),new Vector3(-20,0,-15),new Vector3(-20,0,16),new Vector3(0,0,16),Vector3.zero}){
    var path=new NavMeshPath();Check(NavMesh.CalculatePath(controller.transform.position,dest,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Disconnected street route "+dest);
    // Follow navigation corners with the same physical capsule used in actual play.
    foreach(var corner in path.corners.Skip(1)){
     float deadline=Time.time+12;Vector3 delta;
     do{delta=corner-controller.transform.position;delta.y=0;if(delta.magnitude<.18f)break;controller.Move(delta.normalized*Mathf.Min(6*Time.deltaTime,delta.magnitude)+Vector3.down*2*Time.deltaTime);yield return null;}while(Time.time<deadline);
     delta=corner-controller.transform.position;delta.y=0;Check(delta.magnitude<.2f,"Controller stuck near "+controller.transform.position+" heading to "+corner);
    }traversed++;
   }
   travelSeconds=Time.time-started;game.enabled=true;Move(Vector3.zero);yield return new WaitForSeconds(1);
   // Verify real automatic target selection and damage still work down the opening lane.
   game.config.aimRange=20;for(int i=0;i<4;i++){var point=new Vector3(-2+i*1.3f,0,8);game.Enemies[i].Spawn(point,game.config.enemySpeed,EnemyKind.Normal);}
   float end=Time.time+8;int kills=game.Kills;while(game.Kills<kills+4&&Time.time<end)yield return null;Check(game.Kills>=kills+4,"Opening firing lane blocked");yield return Photo("05-live-combat");
   game.enabled=false;var cam=Camera.main;cam.orthographicSize=24;cam.transform.position=new Vector3(-30,45,-30);cam.transform.rotation=Quaternion.Euler(48,45,0);yield return Photo("06-block-overview");
   File.WriteAllText(Path.Combine(output,"street-pass.json"),JsonUtility.ToJson(new Report{passed=true,openingWeapons=3,storefronts=2,visibilityFadesAndRestores=true,physicalCollisionPreserved=true,worldPropsClear=true,controllerRouteLegs=traversed,routeSeconds=travelSeconds,automaticFireKills=game.Kills-kills,screenshots=photos},true));Debug.Log("STREET_RUNTIME_PASS");Application.Quit(0);
  }
  [Serializable]class Report{public bool passed,visibilityFadesAndRestores,physicalCollisionPreserved,worldPropsClear;public int openingWeapons,storefronts,controllerRouteLegs,automaticFireKills,screenshots;public float routeSeconds;}
 }
}
