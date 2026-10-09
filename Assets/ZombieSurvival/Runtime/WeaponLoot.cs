using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class WeaponLoot : MonoBehaviour {
  sealed class Slot {public GameObject root;public Transform visual;public LineRenderer ring;public Vector3 position;public float nextRespawn;public bool placed;public WeaponKind kind;}
  Slot[] slots;
  SurvivalGame game;NavMeshPath path;Material marker;
  public int Collected {get;private set;}
  public int ActiveCount {get{int n=0;foreach(var s in slots)if(s!=null&&s.root.activeSelf)n++;return n;}}
  public void Initialize(SurvivalGame owner,PackVisuals pack){
   game=owner;slots=new Slot[game.Chapter.number>=2?3:0];path=new NavMeshPath();if(!pack.shotgunCrate)throw new System.InvalidOperationException("Shotgun crate missing");
   marker=new Material(Resources.Load<Material>("DeadDistrict/FX"));marker.SetColor("_BaseColor",Color.white);
   for(int i=0;i<slots.Length;i++){
    var kind=game.Chapter.number>=3&&i==1?WeaponKind.Launcher:WeaponKind.Shotgun;
    var o=new GameObject(kind==WeaponKind.Launcher?"Pooled launcher crate":"Pooled shotgun crate "+i);o.transform.SetParent(transform,false);
    var s=new Slot{root=o,kind=kind,visual=Instantiate(kind==WeaponKind.Launcher?pack.launcherCrate:pack.shotgunCrate,o.transform).transform};slots[i]=s;s.visual.localScale*=1.12f;
    s.ring=BlockVisuals.Ring("Weapon marker",o.transform,.95f,kind==WeaponKind.Launcher?new Color(.25f,.9f,1):new Color(1,.72f,.22f),marker);s.ring.widthMultiplier=.075f;o.SetActive(false);
   }
   if(slots.Length>0&&!game.CombatValidation){foreach(var site in game.Chapter.weaponSites!=null&&game.Chapter.weaponSites.Length>0?game.Chapter.weaponSites:new[]{new Vector3(4,0,1),new Vector3(12,0,6),new Vector3(-12,0,-6)})SpawnNear(site);}
  }
  void SpawnNear(Vector3 desired){
   if(TrySpawn(desired))return;
   for(int radius=1;radius<=3;radius++)for(int i=0;i<8;i++){
    float angle=i*Mathf.PI/4;if(TrySpawn(desired+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius))return;
   }
   Debug.LogWarning("No reachable weapon crate site near "+desired);
  }
  public bool TrySpawn(Vector3 desired){
   if(game.Dead||game.Paused||Mathf.Abs(desired.x)>game.config.arenaHalfSize-2||Mathf.Abs(desired.z)>game.config.arenaHalfSize-2)return false;
   Slot free=null;foreach(var s in slots)if(!s.placed){free=s;break;}if(free==null)return false;
   if(!NavMesh.SamplePosition(desired,out var nav,.7f,NavMesh.AllAreas)||Mathf.Abs(nav.position.y-desired.y)>.6f)return false;
   if(Physics.CheckCapsule(nav.position+Vector3.up*.18f,nav.position+Vector3.up*.85f,.4f,1<<8))return false;
   if(!NavMesh.CalculatePath(game.PlayerPosition,nav.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)return false;
   foreach(var s in slots)if(s.placed&&(s.position-nav.position).sqrMagnitude<2)return false;
   free.placed=true;free.position=nav.position;free.root.transform.position=nav.position;free.root.SetActive(true);return true;
  }
  public void Tick(float now){
   if(game.Dead||game.Paused)return;
   foreach(var s in slots){
    if(!s.placed)continue;
    if(!s.root.activeSelf){if(now>=s.nextRespawn)s.root.SetActive(true);else continue;}
    s.visual.localPosition=Vector3.up*(.045f+Mathf.Sin(now*2.3f)*.045f);s.ring.widthMultiplier=.085f+Mathf.Sin(now*3)*.01f;
    if((game.PlayerPosition-s.position).sqrMagnitude<=1.3f*1.3f&&!Physics.Linecast(game.PlayerPosition+Vector3.up*.65f,s.position+Vector3.up*.65f,1<<8)&&(s.kind==WeaponKind.Launcher?game.CollectLauncher():game.CollectShotgun())){
     Collected++;s.root.SetActive(false);s.nextRespawn=now+75;
    }
   }
  }
  public int AppendHints(LootHint[] hints,int count){foreach(var s in slots){if(s==null||!s.root.activeSelf)continue;if(count>=hints.Length)break;hints[count++]=new LootHint{source=s.root,position=s.position,weapon=true,weaponKind=s.kind};}return count;}

  public void Clear(){foreach(var s in slots)if(s!=null){s.root.SetActive(false);s.placed=false;}}
  void OnDestroy(){if(marker)Destroy(marker);}
 }
}
