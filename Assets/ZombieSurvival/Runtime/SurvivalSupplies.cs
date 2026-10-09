using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public enum SupplyKind {Medkit,Ammo,Grenade,Armor}
 public sealed class SurvivalSupplies : MonoBehaviour {
  public const float VisualScale=1.2f;
  sealed class Slot {public GameObject root;public Transform[] models;public LineRenderer ring;public SupplyKind kind;public Vector3 position;public float expires;}
  readonly Slot[] slots=new Slot[6];
  SurvivalGame game;NavMeshPath path;GameObject aura;Material ringMaterial;int burstIndex,routeIndex;
  static readonly Vector3[] DefaultRoutePoints={new Vector3(8,0,6),new Vector3(-8,0,-6),new Vector3(16,0,-8),new Vector3(-16,0,8)};
  Vector3[] RoutePoints;
  float nextDrop,nextRoute;ParticleSystem[] auraParticles;
  public int Collected {get;private set;}
  public int Dropped {get;private set;}
  public int ActiveCount {get{int n=0;foreach(var s in slots)if(s!=null&&s.root.activeSelf)n++;return n;}}
  public int PoolCapacity=>slots.Length;
  public static string Name(SupplyKind k){switch(k){case SupplyKind.Medkit:return "急救包";case SupplyKind.Ammo:return "补弹箱";case SupplyKind.Grenade:return "手雷补给";default:return "临时护甲";}}
  public static Color Tint(SupplyKind k){switch(k){case SupplyKind.Medkit:return new Color(.25f,1,.65f);case SupplyKind.Ammo:return new Color(1,.82f,.2f);case SupplyKind.Grenade:return new Color(1,.45f,.15f);default:return new Color(.25f,.65f,1);}}
  public void Initialize(SurvivalGame owner,PackVisuals pack){
   game=owner;RoutePoints=game.Chapter.supplyRoutes!=null&&game.Chapter.supplyRoutes.Length>0?game.Chapter.supplyRoutes:DefaultRoutePoints;path=new NavMeshPath();ringMaterial=new Material(Resources.Load<Material>("DeadDistrict/FX"));ringMaterial.SetColor("_BaseColor",Color.white);
   if(pack.supplies==null||pack.supplies.Length!=4)throw new System.InvalidOperationException("Supply prefabs missing");
   for(int i=0;i<slots.Length;i++){
    var root=new GameObject("Pooled supply "+i);root.transform.SetParent(transform,false);var s=new Slot{root=root,models=new Transform[4]};slots[i]=s;
    for(int k=0;k<4;k++){s.models[k]=Instantiate(pack.supplies[k],root.transform).transform;s.models[k].gameObject.SetActive(false);}
    s.ring=BlockVisuals.Ring("Supply marker",root.transform,.78f,Color.white,ringMaterial);s.ring.widthMultiplier=.075f;root.SetActive(false);
   }
   aura=Instantiate(pack.armorAura,transform);aura.name="Armor aura";auraParticles=aura.GetComponentsInChildren<ParticleSystem>(true);Stop(auraParticles);aura.SetActive(false);
   nextDrop=Time.time+8;nextRoute=Time.time+18;
   if(!game.CombatValidation){TrySpawn(SupplyKind.Medkit,new Vector3(3,0,3));TrySpawn(SupplyKind.Ammo,new Vector3(-3,0,5));TrySpawn(SupplyKind.Grenade,new Vector3(6,0,-3));TrySpawn(SupplyKind.Armor,new Vector3(-5,0,-4));}
  }
  static void Stop(ParticleSystem[] particles){foreach(var p in particles)p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}
  public bool TrySpawn(SupplyKind kind,Vector3 desired){
   if(game.Dead||(int)kind<0||(int)kind>3)return false;Slot free=null;foreach(var s in slots)if(!s.root.activeSelf){free=s;break;}if(free==null)return false;
   if(Mathf.Abs(desired.x)>game.config.arenaHalfSize-2||Mathf.Abs(desired.z)>game.config.arenaHalfSize-2)return false;
   if(!NavMesh.SamplePosition(desired,out var sample,.7f,NavMesh.AllAreas)||Mathf.Abs(sample.position.y-desired.y)>.6f)return false;
   if(Physics.CheckCapsule(sample.position+Vector3.up*.18f,sample.position+Vector3.up*.85f,.4f,1<<8))return false;
   if(!NavMesh.CalculatePath(game.PlayerPosition,sample.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)return false;
   foreach(var s in slots)if(s.root.activeSelf&&(s.position-sample.position).sqrMagnitude<1)return false;
   free.kind=kind;free.position=sample.position;free.expires=Time.time+25;free.root.transform.position=sample.position;free.root.SetActive(true);
   for(int k=0;k<4;k++){free.models[k].localScale=Vector3.one*VisualScale;free.models[k].gameObject.SetActive(k==(int)kind);}
   free.ring.startColor=free.ring.endColor=Tint(kind);return true;
  }
  public bool TryPlaceRestSupply(SupplyKind kind,Vector3 desired){
   // Make room for guaranteed wave-six supplies only when the pool is full.
   bool full=true;Slot oldest=slots[0];foreach(var s in slots){if(!s.root.activeSelf)full=false;if(s.expires<oldest.expires)oldest=s;}
   if(full)oldest.root.SetActive(false);
   return TrySpawn(kind,desired);
  }
  public void OnKill(Vector3 position){if(game.CombatValidation||Time.time<nextDrop||Random.value>=.03f)return;var kind=Choose();if(TrySpawn(kind,position)){Dropped++;nextDrop=Time.time+8;}}
  SupplyKind Choose(){float roll=Random.value;if(game.PlayerHealth.CurrentHealth<50&&roll<.55f)return SupplyKind.Medkit;return (SupplyKind)Random.Range(0,4);}
  public void Tick(float now){
   if(game.Dead||game.Paused)return;
   if(!game.CombatValidation&&now>=nextRoute){nextRoute=now+18;TrySpawn(Choose(),RoutePoints[routeIndex++%RoutePoints.Length]);}
   foreach(var s in slots){
    if(!s.root.activeSelf)continue;if(now>=s.expires){s.root.SetActive(false);continue;}
    var model=s.models[(int)s.kind];float fade=Mathf.Clamp01((s.expires-now)/2);model.localScale=Vector3.one*(VisualScale*Mathf.Lerp(.1f,1,fade));model.localPosition=Vector3.up*(.22f+Mathf.Sin(now*2+s.position.x)*.1f);model.localRotation=Quaternion.Euler(0,now*28,0);var tint=Tint(s.kind);tint.a=fade;s.ring.startColor=s.ring.endColor=tint;
    if((game.PlayerPosition-s.position).sqrMagnitude>1.3f*1.3f)continue;
    if(Physics.Linecast(game.PlayerPosition+Vector3.up*.65f,s.position+Vector3.up*.65f,1<<8))continue;
    if(game.ApplySupply(s.kind)){Collected++;PlayPickup(s.kind,s.position);s.root.SetActive(false);}
   }
   bool shield=game.PlayerHealth.Armor>0;if(shield&&!aura.activeSelf){aura.SetActive(true);foreach(var p in auraParticles)p.Play(false);}else if(!shield&&aura.activeSelf){Stop(auraParticles);aura.SetActive(false);}if(shield)aura.transform.position=game.PlayerPosition+Vector3.up*.45f;
  }
  void PlayPickup(SupplyKind kind,Vector3 position){game.Sound.Pickup(kind==SupplyKind.Medkit);}
  public int AppendHints(LootHint[] hints,int count){foreach(var s in slots){if(s==null||!s.root.activeSelf)continue;if(count>=hints.Length)break;hints[count++]=new LootHint{source=s.root,position=s.position,kind=s.kind};}return count;}

  public void Clear(){foreach(var s in slots)if(s!=null)s.root.SetActive(false);if(aura){Stop(auraParticles);aura.SetActive(false);}}
  void OnDestroy(){if(ringMaterial)Destroy(ringMaterial);}
 }
}
