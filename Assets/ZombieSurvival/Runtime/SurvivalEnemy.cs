using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public enum EnemyKind { Normal, Runner, Brute }
 public sealed class SurvivalEnemy : MonoBehaviour {
  public SurvivalHealth Health {get;private set;}
  public NavMeshAgent Agent {get;private set;}
  public bool Alive=>gameObject.activeSelf&&Health.CurrentHealth>0;
  public EnemyKind Kind {get;private set;}
  public bool IsElite {get;private set;}
  public EliteSlam EliteAttack {get;private set;}
  public EliteCharge ChargeAttack {get;private set;}
  public EliteAcid AcidAttack {get;private set;}
  public float EliteDamageMultiplier=>game.Chapter.acidElite?AcidAttack.DamageMultiplier:game.Chapter.chargeElite?ChargeAttack.DamageMultiplier:EliteAttack.DamageMultiplier;
  public string EliteCaption=>game.Chapter.acidElite?AcidAttack.Caption:game.Chapter.chargeElite?ChargeAttack.Caption:EliteAttack.Phase==EliteSlam.Stage.Windup?"避开红圈":EliteAttack.Phase==EliteSlam.Stage.Recovery?"趁机攻击":"保持移动";
  public void CancelEliteAttack(){if(EliteAttack)EliteAttack.Cancel();if(ChargeAttack)ChargeAttack.Cancel();if(AcidAttack)AcidAttack.Cancel();}
  public float SizeMultiplier=>IsElite?1.62f:Kind==EnemyKind.Brute?1.28f:Kind==EnemyKind.Runner?.92f:1;
  public float VisualHeight=>IsElite?3.2f:Kind==EnemyKind.Runner?1.12f:Kind==EnemyKind.Brute?2.55f:2.05f;
  public Vector3 AimPoint=>transform.position+Vector3.up*(IsElite?1.65f:Kind==EnemyKind.Runner?.62f:Kind==EnemyKind.Brute?1.25f:1.1f);
  public string KindName=>IsElite?game.Chapter.eliteName:Kind==EnemyKind.Brute?"重装感染者":Kind==EnemyKind.Runner?"感染犬":"感染者";
  public Vector3 DeathImpulse {get;private set;}
  public LoftActor Actor=>actor;
  public Transform Model=>model;
  public int Slot;
  public int SpawnCount {get;private set;}
  public float ContactDamage {get;private set;}
  public bool KnockedBack=>Alive&&Time.time<knockUntil;
  SurvivalGame game;Transform model,humanModel,dogModel,bruteModel;LoftActor actor,humanActor,dogActor,bruteActor;CapsuleCollider capsule;float nextPath,nextAttack,phase,knockUntil,knockAt;Vector3 knockVelocity;
  public void Initialize(SurvivalGame owner,int slot,GameObject visual){
   game=owner;Slot=slot;phase=slot*1.37f;Health=gameObject.AddComponent<SurvivalHealth>();Health.Configure(game.config.enemyHealth);Health.OnDeath+=()=>game.EnemyKilled(this);
   capsule=gameObject.AddComponent<CapsuleCollider>();capsule.center=new Vector3(0,.85f,0);capsule.radius=.35f;capsule.height=1.7f;
   Agent=gameObject.AddComponent<NavMeshAgent>();Agent.radius=.32f;Agent.height=1.8f;Agent.acceleration=18;Agent.angularSpeed=500;Agent.updateRotation=false;Agent.stoppingDistance=.35f;Agent.obstacleAvoidanceType=ObstacleAvoidanceType.LowQualityObstacleAvoidance;Agent.avoidancePriority=25+slot%50;Agent.enabled=false;
   model=Instantiate(visual,transform).transform;actor=model.GetComponent<LoftActor>();actor.Prepare();humanModel=model;humanActor=actor;
  }
  public void Spawn(Vector3 position,float speed,EnemyKind kind=EnemyKind.Normal,bool elite=false){
   CancelEliteAttack();IsElite=elite;Kind=kind;SpawnCount++;transform.position=position;gameObject.SetActive(true);knockUntil=0;DeathImpulse=Vector3.zero;
   var pressure=new HordePressure(game.Elapsed,game.config);
   Health.Configure((kind==EnemyKind.Brute?game.config.bruteHealth:kind==EnemyKind.Runner?game.config.runnerHealth:game.config.enemyHealth)*game.EnemyHealthScale);Health.ResetForSpawn();if(IsElite){Health.Configure(game.Chapter.eliteHealth);Health.ResetForSpawn();}
   ContactDamage=(kind==EnemyKind.Brute?16:kind==EnemyKind.Runner?6:game.config.attackDamage)*game.EnemyDamageScale;
   if(kind==EnemyKind.Runner&&!dogModel){var prefab=Resources.Load<GameObject>("DeadDistrict/InfectedHound");if(!prefab)throw new System.InvalidOperationException("Infected hound missing");dogModel=Instantiate(prefab,transform).transform;dogActor=dogModel.GetComponent<LoftActor>();dogActor.Prepare();}
   if(kind==EnemyKind.Brute&&!bruteModel){bruteModel=Instantiate(Resources.Load<GameObject>("DeadDistrict/Brutes/Brute-"+(Slot%2)),transform).transform;bruteActor=bruteModel.GetComponent<LoftActor>();bruteActor.Prepare();}
   humanModel.gameObject.SetActive(kind==EnemyKind.Normal);if(bruteModel)bruteModel.gameObject.SetActive(kind==EnemyKind.Brute);if(dogModel)dogModel.gameObject.SetActive(kind==EnemyKind.Runner);model=kind==EnemyKind.Runner?dogModel:kind==EnemyKind.Brute?bruteModel:humanModel;actor=kind==EnemyKind.Runner?dogActor:kind==EnemyKind.Brute?bruteActor:humanActor;
   model.localPosition=Vector3.zero;model.localScale=IsElite?new Vector3(1.86f,1.62f,1.86f):kind==EnemyKind.Brute?Vector3.one*SizeMultiplier:Vector3.one;actor.ResetPose();actor.SetKind(kind);
   capsule.center=Vector3.up*(kind==EnemyKind.Runner?.55f:.85f*SizeMultiplier);capsule.radius=IsElite?.60f:kind==EnemyKind.Brute?.52f:kind==EnemyKind.Runner?.4f:.35f;capsule.height=kind==EnemyKind.Runner?1.1f:1.7f*SizeMultiplier;
   Agent.radius=IsElite?.56f:kind==EnemyKind.Brute?.48f:kind==EnemyKind.Runner?.37f:.32f;Agent.height=kind==EnemyKind.Runner?1.12f:1.8f*SizeMultiplier;Agent.enabled=true;Agent.Warp(position);Agent.isStopped=false;
   if(game.ChapterRunning)pressure=new HordePressure(0,game.config);
   Agent.speed=speed==0?0:kind==EnemyKind.Brute?(game.config.bruteSpeed+pressure.bruteSpeed)*.78f:kind==EnemyKind.Runner?game.config.runnerSpeed+pressure.runnerSpeed:(speed+pressure.normalSpeed)*.8f;
   if(IsElite){Agent.speed=1.9f;if(game.Chapter.acidElite){if(!AcidAttack)AcidAttack=gameObject.AddComponent<EliteAcid>();AcidAttack.Initialize(game,this);}else if(game.Chapter.chargeElite){if(!ChargeAttack)ChargeAttack=gameObject.AddComponent<EliteCharge>();ChargeAttack.Initialize(game,this);}else{if(!EliteAttack)EliteAttack=gameObject.AddComponent<EliteSlam>();EliteAttack.Initialize(game,this);}}
   nextPath=Time.time+Slot%11*.031f;nextAttack=Time.time+.8f;capsule.enabled=true;
  }
  public int HitReactionsPlayed=>actor.HitReactionsPlayed;
  public bool HitReactionActive=>actor.HitReactionActive;
  public void ReactToHit(Vector3 direction){actor.Hit(direction);}
  public void TakeBullet(float damage,Vector3 direction,bool shotgun){if(!Alive)return;DeathImpulse=direction.normalized*(shotgun?3.4f:1.8f)+Vector3.up*.7f;Health.Damage(damage*(IsElite?EliteDamageMultiplier:1),game.gameObject,0,0,direction);}
  public void TakeBlast(float damage,Vector3 point){
   if(!Alive||Physics.Linecast(point+Vector3.up*.65f,AimPoint,1<<8))return;
   Vector3 direction=transform.position-point;direction.y=0;if(direction.sqrMagnitude<.01f)direction=model.forward;direction.Normalize();
   float resistance=Kind==EnemyKind.Brute?.45f:1;DeathImpulse=direction*6*resistance+Vector3.up*2.5f;Health.Damage(damage*(IsElite?EliteDamageMultiplier:1),game.gameObject,0,0,direction);
   if(Alive&&!IsElite){knockAt=Time.time;knockUntil=Time.time+.38f;knockVelocity=direction*7*resistance;Agent.ResetPath();Agent.isStopped=true;nextAttack=Time.time+.7f;actor.Hit(direction);}
  }
  public void Retire(){CancelEliteAttack();Agent.enabled=false;knockUntil=0;actor.ClearHit();gameObject.SetActive(false);}
  public void Tick(float now){
   if(!Alive||!Agent.enabled||!Agent.isOnNavMesh)return;
   if(IsElite){if(game.Chapter.acidElite)AcidAttack.Tick(now);else if(game.Chapter.chargeElite)ChargeAttack.Tick(now);else EliteAttack.Tick(now);return;}
   if(KnockedBack){
    Vector3 delta=knockVelocity*Time.deltaTime*(1-Mathf.Clamp01((now-knockAt)/.38f));float distance=delta.magnitude;
    if(distance>0){var dir=delta/distance;if(Physics.SphereCast(transform.position+Vector3.up,capsule.radius,dir,out var blocked,distance+.03f,1<<8))delta=dir*Mathf.Max(0,blocked.distance-.03f);
     if(NavMesh.Raycast(transform.position,transform.position+delta,out var nav,Agent.areaMask))delta=Vector3.ClampMagnitude(nav.position-transform.position,delta.magnitude);
     Agent.Move(delta);
    }actor.MoveInfected(0,0);return;
   }
   if(Agent.isStopped){Agent.isStopped=false;nextPath=0;}
   var toPlayer=game.PlayerPosition-transform.position;toPlayer.y=0;
   if(now>=nextPath){nextPath=now+game.config.repathInterval;Vector3 dest=game.PlayerPosition+new Vector3(Mathf.Cos(phase),0,Mathf.Sin(phase))*.65f;Agent.SetDestination(dest);}
   float reach=Kind==EnemyKind.Brute?1.8f:1.65f;
   if(toPlayer.sqrMagnitude<reach&&now>=nextAttack&&!Physics.Linecast(AimPoint,game.PlayerPosition+Vector3.up*1.1f,1<<8)){
    nextAttack=now+(Kind==EnemyKind.Runner?.75f:Kind==EnemyKind.Brute?1.35f:game.config.attackInterval);
    game.PlayerHealth.Damage(ContactDamage,gameObject,0,.22f,toPlayer.normalized);if(!game.PlayerHealth.DodgeInvulnerable&&!game.PlayerHealth.GrowthProtected)game.DamageFlash();actor.Attack();
   }
   Vector3 facing=Agent.velocity.sqrMagnitude>.03f?Agent.velocity:toPlayer;facing.y=0;if(facing.sqrMagnitude>.01f)model.rotation=Quaternion.Slerp(model.rotation,Quaternion.LookRotation(facing),Time.deltaTime*9);
   actor.MoveInfected(Agent.velocity.magnitude,Mathf.Clamp01(Agent.velocity.magnitude/Mathf.Max(.1f,Agent.speed)));
  }
 }
}
