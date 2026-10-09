using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class SpawnAccessValidation:MonoBehaviour {
  string output;SurvivalGame g;int accepted,inside,closedAccepted,openAccepted,roomPlayerAccepted;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var args=RuntimeLaunch.Arguments();if(Array.IndexOf(args,"-spawn-access-smoke")<0)return;var v=new GameObject("Opt-in spawn access validation").AddComponent<SpawnAccessValidation>();int n=Array.IndexOf(args,"-validation-output");v.output=n>=0?args[n+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;v.StartCoroutine(v.Run());}
  void Check(bool ok,string why){if(!ok)throw new Exception(why);}
  IEnumerator Run(){var core=Core();while(true){object item=null;bool more=false;string fail=null;try{more=core.MoveNext();if(more)item=core.Current;}catch(Exception e){fail=e.ToString();}if(fail!=null){File.WriteAllText(Path.Combine(output,"spawn-access-failure.txt"),fail);Debug.LogError("SPAWN_ACCESS_FAIL "+fail);Application.Quit(1);yield break;}if(!more)yield break;yield return item;}}
  bool InRoom(Vector3 p)=>Mathf.Abs(p.x)>24.3f&&Mathf.Abs(p.x)<37.7f&&new[]{-24,0,24}.Any(z=>Mathf.Abs(p.z-z)<7.7f);
  void Move(Vector3 p){var c=g.PlayerHealth.GetComponent<CharacterController>();c.enabled=false;c.transform.position=p;c.enabled=true;}
  bool HasPath(Vector3 from,Vector3 to){var path=new NavMeshPath();return NavMesh.CalculatePath(from,to,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;}
  void Batch(Vector3 destination){
   int start=accepted;
   for(int i=0;i<240;i++){
    if(!g.TrySpawn(i*2.399963f))continue;
    var z=g.Enemies.First(e=>e.Alive);accepted++;if(InRoom(z.transform.position))inside++;
    Check(HasPath(z.transform.position,destination),"Spawn trapped behind a closed door at "+z.transform.position);
    Check(!Physics.CheckCapsule(z.transform.position+Vector3.up*.45f,z.transform.position+Vector3.up*1.7f,.42f,1<<8),"Spawn clips obstacle");
    Check(g.MinSpawnDistance>=22&&g.SpawnViolations==0,"Unsafe spawn distance");z.Retire();typeof(SurvivalGame).GetProperty("ActiveCount").SetValue(g,0);
   }
   Check(accepted-start>20,"Valid outdoor spawns starved: "+(accepted-start));Check(inside==0,"Random reinforcement spawned inside a room: "+inside);
  }
  IEnumerator Core(){yield return null;yield return null;g=SurvivalGame.Instance;g.CombatValidation=true;g.Growth.Enabled=false;g.Supplies.Clear();g.WeaponCrates.Clear();typeof(SurvivalGame).GetProperty("Elapsed").SetValue(g,180f);UnityEngine.Random.InitState(92418);g.enabled=false;yield return new WaitForSeconds(.6f);
   var doors=g.World.Props.Where(p=>p.Kind==WorldPropKind.Door).ToArray();Check(doors.Length==6,"Six doors fixture");
   foreach(var d in doors){var outside=d.transform.position+d.transform.forward*2.6f;var interior=d.transform.position-d.transform.forward*2.5f;Check(!HasPath(interior,outside),"Closed room unexpectedly reachable");}
   Batch(g.PlayerPosition);closedAccepted=accepted;
   foreach(var d in doors)Check(d.Toggle(),"Open door fixture");yield return new WaitForSeconds(.7f);Batch(g.PlayerPosition);openAccepted=accepted-closedAccepted;
   foreach(var d in doors)Check(d.Toggle(),"Close door fixture");
   // Exercise immediately before carving settles as well as after it settles.
   Batch(g.PlayerPosition);yield return new WaitForSeconds(.7f);Batch(g.PlayerPosition);
   var door=doors.First(d=>d.transform.position.x>0&&Mathf.Abs(d.transform.position.z)<1);var insidePoint=door.transform.position-door.transform.forward*2.5f;var outsidePoint=door.transform.position+door.transform.forward*2.6f;
   Move(insidePoint);g.config.aimRange=0;g.enabled=true;yield return new WaitForSeconds(.8f);g.enabled=false;int before=accepted;Batch(outsidePoint);roomPlayerAccepted=accepted-before;
   // A legitimate outdoor pursuer must still be able to break in.
   var attacker=g.Enemies[0];Check(NavMesh.SamplePosition(outsidePoint,out var floor,.7f,NavMesh.AllAreas),"Siege floor");attacker.Spawn(floor.position,g.config.enemySpeed);g.enabled=true;float deadline=Time.time+12;while(!door.Broken&&Time.time<deadline)yield return null;Check(door.Broken,"Outdoor enemies cannot break into player's room");attacker.Retire();g.enabled=false;
   File.WriteAllText(Path.Combine(output,"spawn-access-pass.json"),JsonUtility.ToJson(new Report{passed=true,acceptedSpawns=accepted,indoorSpawns=inside,closedDoorSpawns=closedAccepted,openDoorSpawns=openAccepted,spawnsWhilePlayerIndoors=roomPlayerAccepted,allSixDoorsIsolateRooms=true,carvingTransitionSafe=true,outdoorEnemiesStillBreakIn=true,reachablePathsAndClearance=true},true));Debug.Log("SPAWN_ACCESS_PASS");Application.Quit(0);
  }
  [Serializable]class Report{public bool passed,allSixDoorsIsolateRooms,carvingTransitionSafe,outdoorEnemiesStillBreakIn,reachablePathsAndClearance;public int acceptedSpawns,indoorSpawns,closedDoorSpawns,openDoorSpawns,spawnsWhilePlayerIndoors;}
 }
}
