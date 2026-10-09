using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public enum WorldPropKind { SupplyCrate, Explosive, Door }
 public sealed class WorldProp : MonoBehaviour {
  public WorldPropKind Kind; public SupplyKind Contents; public Transform DoorPivot;
  public bool Broken {get;private set;} public bool Open {get;private set;} public bool Moving {get;private set;}
  public float Health {get;private set;} public int Uses {get;private set;}
  WorldInteractions owner; Collider solid;NavMeshObstacle obstacle;float angle,nextDoorAttack;Renderer[] surfaces;MaterialPropertyBlock flash;float flashUntil;
  public Vector3 AimPoint=>transform.position+Vector3.up*(Kind==WorldPropKind.Door?1.2f:.65f);
  public void Initialize(WorldInteractions world){owner=world;Health=Kind==WorldPropKind.Door?180:52;solid=GetComponent<Collider>();obstacle=GetComponent<NavMeshObstacle>();surfaces=GetComponentsInChildren<Renderer>();flash=new MaterialPropertyBlock();}
  public bool Toggle(){if(Broken||Moving||Kind!=WorldPropKind.Door)return false;if(Open&&Occupied()){owner.Game.NotifyPickup("门口有人，暂时无法关门");return false;}Open=!Open;Moving=true;Uses++;solid.enabled=false;obstacle.enabled=false;owner.Game.Sound.Mechanism();return true;}
  bool Occupied()=>Physics.CheckBox(transform.position+Vector3.up, new Vector3(1.55f,1.05f,.75f),transform.rotation,(1<<24)|(1<<26),QueryTriggerInteraction.Ignore);
  void Update(){if(!owner||owner.Game.Paused||owner.Game.Dead)return;
   if(!Broken&&Kind==WorldPropKind.Door&&!Open&&!Moving&&Time.time>=nextDoorAttack&&(owner.Game.PlayerPosition-transform.position).sqrMagnitude<144){nextDoorAttack=Time.time+.7f;foreach(var z in owner.Game.Enemies){if(!z.Alive||z.KnockedBack||(z.transform.position-transform.position).sqrMagnitude>2.3f*2.3f)continue;float a=Vector3.Dot(z.transform.position-transform.position,transform.forward),b=Vector3.Dot(owner.Game.PlayerPosition-transform.position,transform.forward);if(a*b>=0)continue;z.Actor.Attack();Hit(z.Kind==EnemyKind.Brute?36:18);owner.Game.Sound.Hit();owner.Game.VisualEffects.Impact(AimPoint,-transform.forward);break;}}
   if(Moving){angle=Mathf.MoveTowards(angle,Open?100:0,Time.deltaTime*220);DoorPivot.localRotation=Quaternion.Euler(0,angle,0);if(Mathf.Abs(angle-(Open?100:0))<.01f){Moving=false;if(!Open&&Occupied()){Open=true;Moving=true;}else{solid.enabled=!Open;obstacle.enabled=!Open;}}}
   if(flashUntil>0&&Time.time>=flashUntil){flashUntil=0;foreach(var r in surfaces)r.SetPropertyBlock(null);}
  }
  public void Hit(float damage){if(Broken||owner.Game.Dead||owner.Game.Paused||damage<=0)return;Health-=damage;flash.SetColor("_BaseColor",new Color(2,1.25f,.35f));foreach(var r in surfaces)r.SetPropertyBlock(flash);flashUntil=Time.time+.09f;if(Health>0)return;
   Broken=true;solid.enabled=false;obstacle.enabled=false;foreach(var r in surfaces)r.enabled=false;owner.Destroyed(this);
  }
 }
}
