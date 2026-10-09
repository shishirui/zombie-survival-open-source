using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace DeadDistrict {
 public sealed class AtmosphereValidation:MonoBehaviour {
  string output;SurvivalGame game;MobilePreferences.Data previous;bool restore;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=RuntimeLaunch.Arguments();if(Array.IndexOf(args,"-atmosphere-capture")<0)return;
   var v=new GameObject("Opt-in atmosphere verification").AddComponent<AtmosphereValidation>();int i=Array.IndexOf(args,"-validation-output");v.output=i>=0?args[i+1]:Application.persistentDataPath;Directory.CreateDirectory(v.output);
   if(SurvivalGame.Instance)SurvivalGame.Instance.CombatValidation=true;v.StartCoroutine(v.Run());
  }
  void Check(bool condition,string why){if(!condition)throw new Exception(why);}
  IEnumerator Run(){var steps=Core();while(true){bool more=false;object item=null;string error=null;try{more=steps.MoveNext();if(more)item=steps.Current;}catch(Exception e){error=e.ToString();}if(error!=null){File.WriteAllText(Path.Combine(output,"atmosphere-failure.txt"),error);Debug.LogError("ATMOSPHERE_FAIL "+error);Application.Quit(1);yield break;}if(!more)yield break;yield return item;}}
  void Move(Vector3 position){var c=game.PlayerHealth.GetComponent<CharacterController>();c.enabled=false;c.transform.position=position;c.enabled=true;}
  IEnumerator Photo(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.3f);}
  IEnumerator Core(){
   yield return null;yield return null;game=SurvivalGame.Instance;game.CombatValidation=true;game.Growth.Enabled=false;game.config.aimRange=0;
   previous=MobilePreferences.Current;restore=true;var prefs=previous;prefs.reducedFlash=false;MobilePreferences.Set(prefs);
   var root=GameObject.Find("Apocalypse atmosphere");var sites=root.GetComponentsInChildren<DistrictAtmosphere>();Check(sites.Length==4,"Wrong ambient site count");
   var particles=root.GetComponentsInChildren<ParticleSystem>();int cap=particles.Sum(p=>p.main.maxParticles);Check(cap<=108,"Particle capacity exceeded");
   Check(root.GetComponentsInChildren<Light>().Length==0&&root.GetComponentsInChildren<Collider>().Length==0,"Added light or collision");
   foreach(var renderer in root.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)Check(material.shader.isSupported&&(((ParticleSystemRenderer)renderer).renderMode==ParticleSystemRenderMode.Mesh||material.GetTexture("_BaseMap")),"Missing effect shader or texture");
   // Move between both wrecks and dust sites using the normal camera; distance culling must recover.
   for(int i=0;i<sites.Length;i++){
    Move(sites[i].transform.position+new Vector3(0,-sites[i].transform.position.y,-3));yield return new WaitForSeconds(3);
    Check(sites[i].Emitting&&sites[i].GetComponentsInChildren<ParticleSystem>().Any(p=>p.particleCount>0),"Nearby effect never became visible");
    Check(particles.Sum(p=>p.particleCount)<=cap,"Live particles exceeded capacity");yield return Photo("ambient-"+i);
   }
   var smoke=particles.First(p=>p.name=="FX_Smoke_Small_01");float rate=smoke.emission.rateOverTimeMultiplier;
   prefs.reducedFlash=true;MobilePreferences.Set(prefs);yield return null;Check(smoke.emission.rateOverTimeMultiplier<rate*.5f,"Reduced-flash emission was not reduced");
   prefs.reducedFlash=false;MobilePreferences.Set(prefs);Check(Mathf.Abs(smoke.emission.rateOverTimeMultiplier-rate)<.001f,"Comfort restoration failed");
   var ps=sites[sites.Length-1].GetComponentsInChildren<ParticleSystem>()[0];game.TogglePause();yield return null;float before=ps.time;yield return new WaitForSecondsRealtime(.5f);Check(Mathf.Abs(ps.time-before)<.001f,"Paused ambient animation advanced");game.TogglePause();yield return new WaitForSeconds(.4f);Check(ps.time!=before,"Ambient did not resume");
   Move(new Vector3(100,0,100));yield return new WaitForSeconds(.6f);Check(sites.All(s=>!s.Emitting)&&particles.All(p=>p.particleCount==0),"Distant effects not cleared");
   Move(sites[0].transform.position+new Vector3(0,-sites[0].transform.position.y,-3));yield return new WaitForSeconds(.8f);Check(sites[0].Emitting&&sites[0].GetComponentsInChildren<ParticleSystem>().Any(p=>p.particleCount>0),"Distance return did not restart effects");
   Move(Vector3.zero);game.Supplies.Clear();game.WeaponCrates.Clear();
   var points=new[]{new Vector3(-4,0,3),new Vector3(-1.5f,0,4),new Vector3(1.5f,0,4),new Vector3(4,0,3)};
   for(int i=0;i<4;i++)Check(game.Supplies.TrySpawn((SupplyKind)i,points[i]),"Pickup showcase placement");Check(game.WeaponCrates.TrySpawn(new Vector3(0,0,7)),"Weapon case placement");
   yield return new WaitForSeconds(1);yield return Photo("pickup-overview");
   Camera.main.orthographicSize=4.5f;Move(new Vector3(0,0,4));game.enabled=false;yield return new WaitForSeconds(.5f);yield return Photo("pickup-close");
   var pack=Resources.Load<PackVisuals>("DeadDistrict/PackVisuals");Check(pack.supplies.Take(3).All(p=>p.name.Contains("Case")||p.name.Contains("Supply")),"New supply references missing");
   foreach(var prefab in pack.supplies.Concat(new[]{pack.shotgunCrate})){Check(prefab.GetComponentsInChildren<Collider>().Length==0,"Pickup blocks navigation");foreach(var renderer in prefab.GetComponentsInChildren<Renderer>())foreach(var m in renderer.sharedMaterials)Check(m.shader.isSupported,"Pickup material unsupported");}
   MobilePreferences.Set(previous);restore=false;
   File.WriteAllText(Path.Combine(output,"atmosphere-pass.json"),"{\"passed\":true,\"sites\":4,\"particleCap\":"+cap+",\"distanceCullingAndReturn\":true,\"pauseAndResume\":true,\"reducedFlashEmission\":true,\"noExtraLightsOrColliders\":true,\"pickupMaterialsAndNonblocking\":true}");Debug.Log("ATMOSPHERE_PASS");Application.Quit(0);
  }
  void OnDestroy(){if(restore)MobilePreferences.Set(previous);}
 }
}
