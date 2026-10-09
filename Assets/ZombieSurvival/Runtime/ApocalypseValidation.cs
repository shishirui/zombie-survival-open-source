using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class ApocalypseValidation:MonoBehaviour {
  string output;SurvivalGame game;int photos;bool capture;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=RuntimeLaunch.Arguments();bool capture=Array.IndexOf(args,"-apocalypse-capture")>=0;
   if(!capture&&Array.IndexOf(args,"-apocalypse-smoke")<0)return;
   var v=new GameObject("Opt-in Apocalypse verification").AddComponent<ApocalypseValidation>();v.capture=capture;
   int i=Array.IndexOf(args,"-validation-output");v.output=i>=0?args[i+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);
   if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;v.StartCoroutine(v.Run());
  }
  void Check(bool condition,string message){if(!condition)throw new Exception(message);}
  IEnumerator Run(){var steps=Core();while(true){bool more=false;object item=null;string error=null;try{more=steps.MoveNext();if(more)item=steps.Current;}catch(Exception e){error=e.ToString();}if(error!=null){File.WriteAllText(Path.Combine(output,"apocalypse-failure.txt"),error);Debug.LogError("APOCALYPSE_RUNTIME_FAIL "+error);Application.Quit(1);yield break;}if(!more)yield break;yield return item;}}
  void Move(Vector3 pos){var c=game.PlayerHealth.GetComponent<CharacterController>();c.enabled=false;c.transform.position=pos;c.enabled=true;}
  IEnumerator Photo(string name,Vector3 position){Move(position);yield return new WaitForSeconds(.9f);if(!capture)yield break;Time.timeScale=0;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.4f);Time.timeScale=1;photos++;}
  IEnumerator Core(){
   yield return null;yield return null;game=SurvivalGame.Instance;game.CombatValidation=true;game.config.aimRange=0;game.Growth.Enabled=false;
   var root=GameObject.Find("Apocalypse district dressing");Check(root&&root.transform.childCount>=65,"New streets missing");
   foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())foreach(var mat in renderer.sharedMaterials)Check(mat&&mat.shader&&mat.shader.isSupported&&mat.shader.name=="Universal Render Pipeline/Lit"&&mat.GetTexture("_BaseMap"),"Unsupported or missing street material");
   // All street-side room approaches are reachable while doors stay closed.
   foreach(int side in new[]{-1,1})foreach(int z in new[]{-24,0,24}){var path=new NavMeshPath();Check(NavMesh.CalculatePath(Vector3.zero,new Vector3(side*21,0,z),NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Street route blocked");}
   Check(!Physics.CheckCapsule(new Vector3(0,.4f,0),new Vector3(0,1.7f,0),.4f,1<<8),"Start overlaps new art");
   // Exercise real player movement across the dressed road, not just a navigation query.
   var controller=game.PlayerHealth.GetComponent<CharacterController>();Move(new Vector3(-3,0,-2));game.enabled=false;
   foreach(var dest in new[]{new Vector3(3,0,-2),new Vector3(3,0,4),new Vector3(-3,0,4),new Vector3(-3,0,-2)}){
    float deadline=Time.time+4;
    while(Vector3.Distance(controller.transform.position,dest)>.15f&&Time.time<deadline){var d=dest-controller.transform.position;d.y=0;controller.Move(d.normalized*4*Time.deltaTime+Vector3.down*2*Time.deltaTime);yield return null;}
    var remaining=dest-controller.transform.position;remaining.y=0;Check(remaining.magnitude<.2f,"Player caught by road dressing");
   }
   game.enabled=true;
   for(int i=0;i<6;i++){var position=new Vector3(-4+i*1.5f,0,6+i%2);Check(NavMesh.SamplePosition(position,out var hit,1,NavMesh.AllAreas),"Enemy preview point missing");game.Enemies[i].Spawn(hit.position,0,EnemyKind.Normal);}
   yield return Photo("01-crossroads",Vector3.zero);
   foreach(var e in game.Enemies)if(e.Alive)e.Retire();
   yield return Photo("02-curbside",new Vector3(-17,0,-13));
   yield return Photo("03-shop-approach",new Vector3(20,0,-24));
   foreach(var door in game.World.Props.Where(p=>p.Kind==WorldPropKind.Door))if(!door.Open)Check(door.Toggle(),"Door cannot open");
   yield return new WaitForSeconds(.8f);
   yield return Photo("04-shop-interior",new Vector3(29,0,-24));
   File.WriteAllText(Path.Combine(output,"apocalypse-pass.json"),"{\"passed\":true,\"urpMaterialsSupported\":true,\"sixStreetApproaches\":true,\"realPlayerRoadTraversal\":true,\"dressingObjects\":"+root.transform.childCount+",\"screenshots\":"+photos+"}");Debug.Log("APOCALYPSE_RUNTIME_PASS");Application.Quit(0);
  }
 }
}
