using UnityEngine;
namespace DeadDistrict {
 // Small, fixed scene effects. No per-frame allocations, realtime lights or physics.
 public sealed class DistrictAtmosphere : MonoBehaviour {
  ParticleSystem[] particles;
  float[] rates;
  float nextCheck;
  bool visible,paused;
  public bool Emitting=>visible;
  public int ParticleBudget {get {int n=0;foreach(var p in particles)n+=p.main.maxParticles;return n;}}
  void Awake(){
   particles=GetComponentsInChildren<ParticleSystem>(true);rates=new float[particles.Length];
   for(int i=0;i<particles.Length;i++){rates[i]=particles[i].emission.rateOverTimeMultiplier;particles[i].Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}
   MobilePreferences.Changed+=ApplyComfort;ApplyComfort();
  }
  void ApplyComfort(){for(int i=0;i<particles.Length;i++){var emission=particles[i].emission;emission.rateOverTimeMultiplier=rates[i]*(MobilePreferences.Current.reducedFlash?.45f:1);}}
  void Update(){
   var game=SurvivalGame.Instance;if(!game||!game.PlayerHealth)return;
   bool freeze=game.Paused;
   if(freeze!=paused){paused=freeze;foreach(var p in particles)if(visible){if(freeze)p.Pause(false);else p.Play(false);}}
   if(Time.unscaledTime<nextCheck)return;nextCheck=Time.unscaledTime+.25f;
   float range=visible?26:22;
   bool wanted=game.RunStarted&&!game.Dead&&!game.Completed&&(game.PlayerPosition-transform.position).sqrMagnitude<range*range;
   if(wanted==visible)return;visible=wanted;
   foreach(var p in particles){if(!wanted)p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);else {p.Play(false);if(freeze)p.Pause(false);}}
  }
  void OnDisable(){if(particles!=null)foreach(var p in particles)if(p)p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);visible=false;}
  void OnDestroy(){MobilePreferences.Changed-=ApplyComfort;}
 }
}
