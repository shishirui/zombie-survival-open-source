using UnityEngine;
namespace DeadDistrict {
 public sealed class RagdollCorpses : MonoBehaviour {
  sealed class Slot {public GameObject root;public CorpseVisual[] models;public CorpseVisual active;public HoundCorpse dog;public bool isDog;public float frozeAt,expires,settledSince;public bool frozen;}
  readonly Slot[] slots=new Slot[8];int index;SurvivalGame game;
  public int Played {get;private set;}
  public int PoolCapacity=>slots.Length;
  public int ActiveCount {get{int n=0;foreach(var s in slots)if(s!=null&&s.root.activeSelf)n++;return n;}}
  public int SimulatedCount {get{int n=0;foreach(var s in slots)if(s!=null&&s.root.activeSelf&&!s.frozen)n++;return n;}}
  public Vector3 LastImpulse {get;private set;}
  public Vector3 LastHipsPosition=>slots[(index+slots.Length-1)%slots.Length].isDog?slots[(index+slots.Length-1)%slots.Length].dog.RestPosition:slots[(index+slots.Length-1)%slots.Length].active.hips.position;
  public void Initialize(SurvivalGame owner,PackVisuals pack){
   game=owner;if(pack.corpses==null||pack.corpses.Length!=pack.infected.Length||pack.corpses.Length<2)throw new System.InvalidOperationException("Purchased ragdoll presets missing");
   Physics.IgnoreLayerCollision(25,25,false);Physics.IgnoreLayerCollision(25,24,true);Physics.IgnoreLayerCollision(25,26,true);
   for(int i=0;i<slots.Length;i++){
    var root=new GameObject("Pooled infected corpse "+i);root.transform.SetParent(transform,false);var s=new Slot{root=root,models=new CorpseVisual[pack.corpses.Length]};slots[i]=s;
    for(int k=0;k<pack.corpses.Length;k++){s.models[k]=Instantiate(pack.corpses[k],root.transform).GetComponent<CorpseVisual>();s.models[k].ConfigurePhysics();s.models[k].Freeze();s.models[k].gameObject.SetActive(false);}root.SetActive(false);
   }

  }
  public void Drop(SurvivalEnemy z){
   var s=slots[index++%slots.Length];foreach(var v in s.models){v.Freeze();v.gameObject.SetActive(false);}
   if(s.dog)s.dog.gameObject.SetActive(false);s.isDog=z.Kind==EnemyKind.Runner;
   if(s.isDog){s.root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);s.root.transform.localScale=Vector3.one;s.root.SetActive(true);if(!s.dog){var obj=new GameObject("Resting infected hound");obj.transform.SetParent(s.root.transform,false);s.dog=obj.AddComponent<HoundCorpse>();}s.dog.Drop(z);s.frozen=true;s.expires=Time.time+5.5f;LastImpulse=z.DeathImpulse;Played++;return;}
   s.root.transform.SetPositionAndRotation(z.Model.position,z.Model.rotation);s.root.transform.localScale=z.Model.localScale;
   s.root.SetActive(true);s.active=s.models[z.Slot%(z.Kind==EnemyKind.Brute?2:s.models.Length)];s.active.gameObject.SetActive(true);s.active.CopyPose(z.Actor);Physics.SyncTransforms();
   Vector3 impulse=z.DeathImpulse;if(impulse.sqrMagnitude<.01f)impulse=z.Health.LastDamageDirection.normalized*1.8f+Vector3.up*.7f;
   LastImpulse=impulse;s.active.Launch(impulse);
   // Reapply after activation so reused corpse colliders cannot push one another apart.
   foreach(var other in slots)if(other!=s&&other.root.activeSelf&&!other.isDog)foreach(var a in s.active.Colliders)foreach(var b in other.active.Colliders)Physics.IgnoreCollision(a,b,true);
   s.frozen=false;s.settledSince=-1;s.frozeAt=Time.time+1.2f;s.expires=Time.time+5.5f;Played++;
  }
  void Update(){if(!game||game.Paused)return;foreach(var s in slots){if(!s.root.activeSelf)continue;if(s.isDog){if(Time.time>=s.expires)s.root.SetActive(false);continue;}
   if(!s.frozen){
    if(s.active.Resting){if(s.settledSince<0)s.settledSince=Time.time;}else s.settledSince=-1;
    bool settled=Time.time>=s.frozeAt&&s.settledSince>=0&&Time.time-s.settledSince>=.25f;
    bool bounded=Time.time>=s.frozeAt+2&&s.active.Grounded;
    if(settled||bounded){s.active.Freeze();s.frozen=true;}
   }
   if(!s.frozen&&Time.time>=s.expires-1){s.active.Freeze();s.frozen=true;}
   // Keep the settled skeleton fixed in place until the pooled model is hidden.
   if(Time.time>=s.expires){s.active.Freeze();s.root.SetActive(false);}
  }}
  public void FreezeAll(){foreach(var s in slots)if(s!=null&&s.root.activeSelf&&!s.isDog){s.active.Freeze();s.frozen=true;}}
 }
}
