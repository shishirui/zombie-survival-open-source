using UnityEngine;
namespace DeadDistrict {
 // Fixed scene emitters. Attenuation uses the player, not the elevated camera listener.
 public sealed class EnvironmentSite:MonoBehaviour {
  public AudioClip clip;public float gain=.22f,range=20;public bool global,intermittent;public Renderer beacon;
  AudioSource voice;ParticleSystem[] systems;float[] rates;bool active,frozen;float nextBurst,duckUntil;int shots,blasts;MaterialPropertyBlock tint;
  public bool Active=>active;public AudioSource Voice=>voice;public int BurstCount {get;private set;}
  public int ParticleBudget {get {int n=0;foreach(var p in systems)n+=p.main.maxParticles;return n;}}
  void Awake(){systems=GetComponentsInChildren<ParticleSystem>(true);rates=new float[systems.Length];for(int i=0;i<systems.Length;i++){rates[i]=systems[i].emission.rateOverTimeMultiplier;systems[i].Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}if(clip){voice=gameObject.AddComponent<AudioSource>();voice.clip=clip;voice.loop=!intermittent;voice.playOnAwake=false;voice.spatialBlend=0;voice.dopplerLevel=0;voice.priority=190;voice.volume=0;}tint=new MaterialPropertyBlock();nextBurst=Time.time+1.5f+Mathf.Repeat(transform.position.x+transform.position.z,3);MobilePreferences.Changed+=Comfort;Comfort();}
  void Comfort(){if(systems==null)return;for(int i=0;i<systems.Length;i++){var e=systems[i].emission;e.rateOverTimeMultiplier=rates[i]*(MobilePreferences.Current.reducedFlash?.5f:1);}if(voice&&MobilePreferences.Current.sfx<=0){voice.volume=0;voice.Stop();}}
  void Update(){var g=SurvivalGame.Instance;if(!g||!g.PlayerHealth)return;
   if(!g.RunStarted||g.Dead||g.Completed){Clear();return;}
   if(g.Paused){if(!frozen){frozen=true;foreach(var p in systems)if(p.isPlaying)p.Pause(false);if(voice)voice.Pause();}return;}
   if(frozen){frozen=false;if(active)foreach(var p in systems)if(p.isPaused)p.Play(false);if(voice&&MobilePreferences.Current.sfx>0)voice.UnPause();}
   float distance=Vector3.Distance(g.PlayerPosition,transform.position);bool near=global||distance<(active?range+2:range);
   if(near!=active){if(!near){Clear();return;}active=true;if(!intermittent)foreach(var p in systems)p.Play(false);}
   if(!active)return;
   if(g.ShotsFired!=shots||g.VisualEffects.ExplosionsPlayed!=blasts){shots=g.ShotsFired;blasts=g.VisualEffects.ExplosionsPlayed;duckUntil=Time.time+.5f;}
   float falloff=global?1:Mathf.Pow(Mathf.Clamp01(1-Mathf.Max(0,distance-3)/Mathf.Max(1,range-3)),2);
   float target=gain*falloff*MobilePreferences.Current.sfx*(Time.time<duckUntil?.42f:1);
   if(voice){voice.volume=MobilePreferences.Current.sfx<=0?0:Mathf.MoveTowards(voice.volume,target,Time.unscaledDeltaTime*.5f);voice.panStereo=global?0:Mathf.Clamp(Vector3.Dot(transform.position-g.PlayerPosition,Camera.main.transform.right)/range,-.65f,.65f);if(!intermittent&&target>.0005f&&!voice.isPlaying)voice.Play();if(target<=.0005f&&voice.volume<=.0005f)voice.Stop();}
   if(intermittent&&Time.time>=nextBurst){nextBurst=Time.time+Random.Range(4.5f,8);BurstCount++;foreach(var p in systems){p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);p.Play(false);}if(voice&&target>.0005f){voice.volume=target;voice.Play();}}
   if(beacon){float pulse=MobilePreferences.Current.reducedFlash?.35f:.5f+.25f*Mathf.Sin(Time.time*1.6f+transform.position.x);tint.SetColor("_BaseColor",new Color(1,.32f,.04f,1)*pulse);beacon.SetPropertyBlock(tint);}
  }
  void Clear(){if(active||frozen){foreach(var p in systems)p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}active=false;frozen=false;if(voice){voice.Stop();voice.volume=0;}}
  void OnDisable(){if(systems!=null){foreach(var p in systems)p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}if(voice)voice.Stop();active=false;frozen=false;}
  void OnDestroy(){MobilePreferences.Changed-=Comfort;}
 }
}
