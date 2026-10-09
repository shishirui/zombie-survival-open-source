using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public partial class SurvivalGame {
  public int OpeningCount {get;private set;}
  // Populate the street before the first rendered frame. Later reinforcements retain offscreen spawning.
  void SeedOpeningEnemies(){
   for(int attempt=0;attempt<160&&OpeningCount<8;attempt++){
    float angle=attempt*2.399963f;float radius=11+(attempt%6);
    var point=player.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
    if(!NavMesh.SamplePosition(point,out var hit,.8f,NavMesh.AllAreas))continue;
    if(Vector3.Distance(player.position,hit.position)<10)continue;
    var view=cam.WorldToViewportPoint(hit.position+Vector3.up);
    if(view.z<=0||view.x<.12f||view.x>.88f||view.y<.12f||view.y>.88f)continue;
    if(Physics.Linecast(player.position+Vector3.up,hit.position+Vector3.up,1<<8))continue;
    // The closer camera selects different opening sites; use the same physical clearance as reinforcements.
    if(!CanSpawnEnemyAt(hit.position))continue;
    bool crowded=false;foreach(var z in enemies)if(z.Alive&&(z.transform.position-hit.position).sqrMagnitude<4){crowded=true;break;}if(crowded)continue;
    SurvivalEnemy free=null;foreach(var z in enemies)if(!z.gameObject.activeSelf){free=z;break;}if(free==null)break;
    free.Spawn(hit.position,config.enemySpeed,EnemyKind.Normal);OpeningCount++;ActiveCount++;PeakAlive=Mathf.Max(PeakAlive,ActiveCount);
   }
  }
 }
}
