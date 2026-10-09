using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class EnemyImpactValidation : MonoBehaviour {
  string output;bool capture;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot(){var args=Environment.GetCommandLineArgs();bool capture=Array.IndexOf(args,"-enemy-impact-capture")>=0;if(!capture&&Array.IndexOf(args,"-enemy-impact-smoke")<0)return;var v=new GameObject("Opt-in enemy impact validation").AddComponent<EnemyImpactValidation>();v.capture=capture;int n=Array.IndexOf(args,"-validation-output");v.output=n>=0?args[n+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;v.StartCoroutine(v.Run());}
  IEnumerator Run(){
   yield return null;yield return null;var g=SurvivalGame.Instance;g.CombatValidation=true;g.AutoTest=false;var z=g.Enemies[0];var forward=Camera.main.transform.forward;forward.y=0;forward.Normalize();
   if(!NavMesh.SamplePosition(g.PlayerPosition+forward*4,out var sample,.5f,NavMesh.AllAreas)){Fail("No walkable test position");yield break;}
   z.Spawn(sample.position,0);z.Health.SetHealth(1000);
   var actor=z.GetComponentInChildren<LoftActor>();var renderer=z.GetComponentInChildren<SkinnedMeshRenderer>();var material=renderer.sharedMaterial;var baseColor=material.GetColor("_BaseColor");var body=actor.GetComponentInChildren<Animator>().transform;Vector3 originalPosition=body.localPosition;
   int shots=g.ShotsFired;float deadline=Time.time+2;while(g.ShotsFired==shots&&Time.time<deadline)yield return null;
   yield return new WaitForSeconds(.045f);
   var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,0);
   bool nonfatal=z.Alive&&z.Health.CurrentHealth<1000&&g.VisualEffects.EnemyImpactsPlayed>0&&z.HitReactionsPlayed>0;
   bool flash=block.HasColor("_BaseColor")&&block.GetColor("_BaseColor")!=baseColor;
   bool recoil=Vector3.Distance(body.localPosition,originalPosition)>.01f;
   bool sharedMaterialSafe=material.GetColor("_BaseColor")==baseColor;
   if(capture){ScreenCapture.CaptureScreenshot(Path.Combine(output,"enemy-hit.png"));yield return new WaitForSeconds(.3f);if(!File.Exists(Path.Combine(output,"enemy-hit.png"))){Fail("Hit capture missing");yield break;}}
   int deaths=g.VisualEffects.DeathsPlayed;z.Health.SetHealth(1);deadline=Time.time+2;while(z.Alive&&Time.time<deadline)yield return null;
   bool critical=!z.Alive&&g.VisualEffects.DeathsPlayed>deaths;
   if(capture){yield return new WaitForSeconds(.045f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"enemy-kill.png"));yield return new WaitForSeconds(.3f);if(!File.Exists(Path.Combine(output,"enemy-kill.png"))){Fail("Kill capture missing");yield break;}}
   if(!NavMesh.SamplePosition(g.PlayerPosition-forward*19,out sample,.5f,NavMesh.AllAreas)){Fail("No reuse position");yield break;}
   z.Spawn(sample.position,0);yield return null;renderer.GetPropertyBlock(block,0);
   bool reuseClean=!z.HitReactionActive&&Vector3.Distance(body.localPosition,originalPosition)<.001f&&(!block.HasColor("_BaseColor")||block.GetColor("_BaseColor")==baseColor)&&material.GetColor("_BaseColor")==baseColor;
   if(!nonfatal||!flash||!recoil||!sharedMaterialSafe||!critical||!reuseClean){Fail($"nonfatal={nonfatal} flash={flash} recoil={recoil} materialSafe={sharedMaterialSafe} critical={critical} reuseClean={reuseClean}");yield break;}
   File.WriteAllText(Path.Combine(output,"enemy-impact-pass.json"),"{\"passed\":true,\"nonfatalBloodImpact\":true,\"hitFlash\":true,\"visualRecoil\":true,\"sharedMaterialUnchanged\":true,\"criticalKillEffect\":true,\"pooledRespawnClearsReaction\":true}");Debug.Log("ENEMY_IMPACT_PASS");Application.Quit(0);
  }
  void Fail(string reason){File.WriteAllText(Path.Combine(output,"enemy-impact-failure.txt"),reason);Debug.LogError("ENEMY_IMPACT_FAIL "+reason);Application.Quit(1);}
 }
}
