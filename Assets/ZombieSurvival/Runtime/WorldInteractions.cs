using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class WorldInteractions : MonoBehaviour {
  public SurvivalGame Game {get;private set;}
  public readonly List<WorldProp> Props=new List<WorldProp>();
  public int Explosions {get;private set;} public int CratesBroken {get;private set;} public int SuppliesReleased {get;private set;}
  sealed class Pending { public Vector3 point; public SupplyKind kind; }
  readonly List<Pending> pending=new List<Pending>();float nextSupply;LineRenderer danger;Material dangerMaterial;
  public void Initialize(SurvivalGame game,PackVisuals pack){Game=game;dangerMaterial=new Material(Resources.Load<Material>("DeadDistrict/FX"));danger=BlockVisuals.Ring("Explosive danger radius",transform,4.5f,new Color(1,.35f,.1f,.65f),dangerMaterial);danger.widthMultiplier=.045f;danger.gameObject.SetActive(false);
   if(game.Chapter.streetRooms)foreach(int side in new[]{-1,1})foreach(int z in new[]{-24,0,24})Spawn(pack.worldDoor,new Vector3(side*24.5f,0,z),side<0?90:-90);
   Vector3[] boxes=game.Chapter.worldCrates!=null&&game.Chapter.worldCrates.Length>0?game.Chapter.worldCrates:new Vector3[]{new Vector3(7,0,9),new Vector3(-9,0,-11),new Vector3(29,0,0),new Vector3(-29,0,0),new Vector3(29,0,24),new Vector3(-29,0,-24)};
   for(int i=0;i<boxes.Length;i++){var box=Spawn(pack.worldSupplyCrate,boxes[i],i*37);box.Contents=(SupplyKind)(i%4);}
   foreach(var pos in game.Chapter.worldExplosives!=null&&game.Chapter.worldExplosives.Length>0?game.Chapter.worldExplosives:new[]{new Vector3(-7,0,11),new Vector3(-9,0,13),new Vector3(7,0,-13),new Vector3(10,0,24)})Spawn(pack.worldExplosive,pos,0);
  }
  public WorldProp Spawn(GameObject prefab,Vector3 point,float yaw){var p=Instantiate(prefab,point,Quaternion.Euler(0,yaw,0),transform).GetComponent<WorldProp>();p.Initialize(this);Props.Add(p);return p;}
  // The six rooms built by VisualUpgrade extend 13 m inward and 7.5 m either side of their entrance.
  // Include a small doorway/wall margin so sampling cannot place a body across the threshold.
  public WorldProp RoomAt(Vector3 point){foreach(var p in Props){if(p.Kind!=WorldPropKind.Door)continue;var local=p.transform.InverseTransformPoint(point);if(local.z<.4f&&local.z>-13.4f&&Mathf.Abs(local.x)<7.9f)return p;}return null;}
  public Vector3 EnemyApproachPoint {get{var room=RoomAt(Game.PlayerPosition);return room?room.transform.position+room.transform.forward*2.6f:Game.PlayerPosition;}}
  public bool Visible(WorldProp p,float range){if(!p||p.Broken)return false;var from=Game.PlayerPosition+Vector3.up*1.1f;if((p.AimPoint-from).sqrMagnitude>range*range)return false;var v=Camera.main.WorldToViewportPoint(p.AimPoint);if(v.z<=0||v.x<.07f||v.x>.93f||v.y<.2f||v.y>.8f||Game.hud.TargetCovered(p.transform.position,p.AimPoint))return false;
   if(Physics.Linecast(from,p.AimPoint,out var hit,1<<8)&&hit.collider.GetComponent<WorldProp>()!=p)return false;
   return !Physics.Linecast(Camera.main.transform.position,p.AimPoint,out hit,1<<8)||hit.collider.GetComponent<WorldProp>()==p;
  }
  public WorldProp Nearest(){WorldProp selected=null;float best=float.MaxValue;foreach(var p in Props){if(p.Kind!=WorldPropKind.Door||!Visible(p,2.8f))continue;float distance=(p.transform.position-Game.PlayerPosition).sqrMagnitude;if(distance<best){selected=p;best=distance;}}return selected;}
  public string Caption {get{var p=Nearest();return !p?"":p.Moving?"门正在移动":p.Open?"关门":"开门";}}
  public bool Interact(){if(Game.Dead||Game.Paused||Game.Rolling||Game.IsGrenadeAiming)return false;var p=Nearest();return p&&p.Toggle();}
  void OnDestroy(){if(dangerMaterial)Destroy(dangerMaterial);}
  public void Tick(){var nearest=Nearest();bool show=nearest&&nearest.Kind==WorldPropKind.Explosive&&!Game.Dead;danger.gameObject.SetActive(show);if(show)danger.transform.position=nearest.transform.position;
   if(Game.Dead||Game.Paused)return;if(Time.time<nextSupply)return;nextSupply=Time.time+.3f;
   for(int i=pending.Count-1;i>=0;i--){var reward=pending[i];bool spawned=false;for(int n=0;n<9&&!spawned;n++){var point=reward.point+(n==0?Vector3.zero:new Vector3(Mathf.Cos(n*Mathf.PI/4),0,Mathf.Sin(n*Mathf.PI/4))*1.4f);spawned=Game.Supplies.TrySpawn(reward.kind,point);}if(spawned){pending.RemoveAt(i);SuppliesReleased++;}}
  }
  public void Destroyed(WorldProp p){Game.VisualEffects.Impact(p.AimPoint,Vector3.up);if(p.Kind==WorldPropKind.SupplyCrate){CratesBroken++;pending.Add(new Pending{point=p.transform.position,kind=p.Contents});Game.Sound.BreakCrate();Game.NotifyPickup("补给箱已击碎");}else if(p.Kind==WorldPropKind.Explosive){Explosions++;Blast(p.transform.position,4.5f,180,true);}else Game.Sound.BreakCrate();}
  public void DamageProps(Vector3 point,float radius,float damage){foreach(var p in Props){if(p.Broken||(p.transform.position-point).sqrMagnitude>radius*radius)continue;if(Physics.Linecast(point+Vector3.up*.7f,p.AimPoint,out var hit,1<<8)&&hit.collider.GetComponent<WorldProp>()!=p)continue;p.Hit(damage);}}
  public void Blast(Vector3 point,float radius,float damage,bool hurtPlayer){Game.VisualEffects.Explosion(point);Game.Sound.Explosion();foreach(var z in Game.Enemies)if(z.Alive&&(z.transform.position-point).sqrMagnitude<=radius*radius)z.TakeBlast(damage,point);
   if(hurtPlayer&&(Game.PlayerPosition-point).sqrMagnitude<radius*radius&&!Physics.Linecast(point+Vector3.up,Game.PlayerPosition+Vector3.up,1<<8))Game.PlayerHealth.Damage(18,gameObject,0,0,(Game.PlayerPosition-point).normalized);
   DamageProps(point,radius,damage);
  }
 }
}
