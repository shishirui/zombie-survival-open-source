using UnityEngine;
namespace DeadDistrict {
 // Reuses purchased particle hierarchies; every instance has a bounded lifetime and particle budget.
 public sealed class PackBurst:MonoBehaviour {
  ParticleSystem[] systems;ParticleSystem.MinMaxGradient[] colors;Vector3 baseScale;float until;
  public int ParticleCount {get{int n=0;foreach(var p in systems)n+=p.particleCount;return n;}}
  public Vector3 Position {set{transform.position=value;}}
  public static PackBurst Create(string resource,Transform parent){var source=Resources.Load<GameObject>("DeadDistrict/Feedback/"+resource);if(!source)throw new System.InvalidOperationException("Missing feedback "+resource);var root=Instantiate(source,parent);root.name="Pooled "+resource;var b=root.AddComponent<PackBurst>();b.systems=root.GetComponentsInChildren<ParticleSystem>(true);b.colors=System.Array.ConvertAll(b.systems,p=>p.main.startColor);b.baseScale=root.transform.localScale;b.Stop();return b;}
  static Gradient TintGradient(Gradient src,Color tint){var keys=src.colorKeys;var alpha=src.alphaKeys;for(int i=0;i<keys.Length;i++)keys[i].color*=tint;for(int i=0;i<alpha.Length;i++)alpha[i].alpha*=tint.a;var g=new Gradient();g.SetKeys(keys,alpha);g.mode=src.mode;return g;}
  static ParticleSystem.MinMaxGradient Tint(ParticleSystem.MinMaxGradient src,Color tint){switch(src.mode){case ParticleSystemGradientMode.Color:return new ParticleSystem.MinMaxGradient(src.color*tint);case ParticleSystemGradientMode.TwoColors:return new ParticleSystem.MinMaxGradient(src.colorMin*tint,src.colorMax*tint);case ParticleSystemGradientMode.TwoGradients:return new ParticleSystem.MinMaxGradient(TintGradient(src.gradientMin,tint),TintGradient(src.gradientMax,tint));default:return new ParticleSystem.MinMaxGradient(TintGradient(src.gradient,tint));}}
  public void Play(Vector3 position,float scale,Color tint,float duration){Stop();transform.SetPositionAndRotation(position,Quaternion.identity);transform.localScale=baseScale*scale;gameObject.SetActive(true);if(MobilePreferences.Current.reducedFlash){tint.r*=.5f;tint.g*=.5f;tint.b*=.5f;tint.a*=.65f;}for(int i=0;i<systems.Length;i++){var m=systems[i].main;m.startColor=Tint(colors[i],tint);systems[i].Play(false);}until=Time.time+duration;}
  void Update(){if(Time.time>=until)Stop();}
  public void Stop(){if(!this)return;if(systems!=null)foreach(var p in systems)if(p)p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);gameObject.SetActive(false);}
  public void Dispose(){if(this)Destroy(gameObject);}
 }
}
