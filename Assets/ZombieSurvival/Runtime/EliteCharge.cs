using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class EliteCharge:MonoBehaviour {
  public enum Stage {Hunt,Windup,Charging,Recovery}
  public Stage Phase {get;private set;}
  public Vector3 LockedDirection {get;private set;}
  public Vector3 StartPoint {get;private set;}
  public Vector3 EndPoint {get;private set;}
  public int Charges {get;private set;}
  public int Hits {get;private set;}
  public int WallHits {get;private set;}
  public float DamageMultiplier=>Phase==Stage.Recovery?1.45f:1;
  public string Caption=>Phase==Stage.Windup?"向两侧躲避":Phase==Stage.Charging?"冲撞中":Phase==Stage.Recovery?"硬直 · 趁机攻击":"保持移动";
  SurvivalGame game;SurvivalEnemy enemy;LineRenderer warning,progress,halo;float phaseAt,nextCharge,nextPath,recovery;bool hitThisCharge,endsAtWall,followup;
  LineRenderer Line(string name,Material mat,Color color){var l=new GameObject(name).AddComponent<LineRenderer>();l.sharedMaterial=mat;l.useWorldSpace=true;l.widthMultiplier=.09f;l.startColor=l.endColor=color;l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;l.receiveShadows=false;return l;}
  public void Initialize(SurvivalGame owner,SurvivalEnemy actor){game=owner;enemy=actor;Phase=Stage.Hunt;Charges=Hits=WallHits=0;nextCharge=Time.time+1;nextPath=0;hitThisCharge=false;followup=false;
   if(!warning){var mat=Resources.Load<Material>("DeadDistrict/FX");warning=Line("Charge danger lane",mat,new Color(1,.22f,.1f,.9f));warning.loop=true;progress=Line("Charge windup progress",mat,new Color(1,.65f,.17f,.65f));progress.positionCount=2;halo=BlockVisuals.Ring("Charge elite identity",transform,.9f,new Color(1,.4f,.14f,.7f),mat);}
   Hide();halo.gameObject.SetActive(true);
  }
  bool Clear(Vector3 a,Vector3 b)=>!Physics.Linecast(a+Vector3.up*.8f,b+Vector3.up*.8f,1<<8);
  public void Tick(float now){
   if(!enemy.Alive||game.Dead||game.Completed||game.Paused)return;
   var cfg=game.Chapter;
   if(Phase==Stage.Windup){enemy.Agent.isStopped=true;enemy.Actor.MoveInfected(0,0);float t=Mathf.Clamp01((now-phaseAt)/cfg.eliteWindup);enemy.Actor.SetEliteWindup(.65f*Mathf.SmoothStep(0,1,t));progress.SetPosition(1,Vector3.Lerp(StartPoint,EndPoint,t)+Vector3.up*.08f);
    if(t>=1){Phase=Stage.Charging;phaseAt=now;Charges++;hitThisCharge=false;enemy.Actor.SetEliteWindup(0);progress.gameObject.SetActive(false);}return;}
   if(Phase==Stage.Charging){
    var from=transform.position;float remaining=Vector3.Dot(EndPoint-from,LockedDirection);float distance=Mathf.Min(Mathf.Max(0,remaining),cfg.chargeSpeed*Mathf.Min(Time.deltaTime,.1f));bool blocked=false;
    if(Physics.SphereCast(from+Vector3.up*.9f,.6f,LockedDirection,out var wall,distance+.08f,1<<8,QueryTriggerInteraction.Ignore)){distance=Mathf.Max(0,wall.distance-.08f);blocked=true;}
    var to=from+LockedDirection*distance;if(NavMesh.Raycast(from,to,out var nav,NavMesh.AllAreas)){to=nav.position;blocked=true;}
    if(!hitThisCharge&&DistanceToSegment(game.PlayerPosition,from,to)<cfg.chargeWidth*.5f+.34f&&Clear(from,game.PlayerPosition)){
     hitThisCharge=true;float before=game.PlayerHealth.CurrentHealth;game.PlayerHealth.Damage(cfg.eliteDamage,gameObject,0,.3f,LockedDirection);if(game.PlayerHealth.CurrentHealth<before){Hits++;game.DamageFlash();}
    }
    enemy.Agent.Move(to-from);enemy.Actor.MoveInfected(cfg.chargeSpeed,1);enemy.Model.rotation=Quaternion.LookRotation(LockedDirection);
    if(blocked||remaining<=distance+.1f||now-phaseAt>cfg.chargeDistance/cfg.chargeSpeed+.5f)Finish(now,blocked||endsAtWall);return;
   }
   if(Phase==Stage.Recovery){enemy.Agent.isStopped=true;enemy.Actor.MoveInfected(0,0);if(now-phaseAt<recovery)return;Phase=Stage.Hunt;nextCharge=now+(followup?.25f:cfg.eliteCooldown);enemy.Agent.isStopped=false;}
   var delta=game.PlayerPosition-transform.position;delta.y=0;
   if(now>=nextCharge&&delta.magnitude<=cfg.eliteRange&&Clear(transform.position,game.PlayerPosition)){
    StartPoint=transform.position;LockedDirection=delta.sqrMagnitude>.01f?delta.normalized:enemy.Model.forward;LockedDirection=new Vector3(LockedDirection.x,0,LockedDirection.z).normalized;float range=cfg.chargeDistance;endsAtWall=false;
    if(Physics.SphereCast(StartPoint+Vector3.up*.9f,.6f,LockedDirection,out var hit,range,1<<8,QueryTriggerInteraction.Ignore)){range=Mathf.Max(0,hit.distance-.08f);endsAtWall=true;}
    EndPoint=StartPoint+LockedDirection*range;if(NavMesh.Raycast(StartPoint,EndPoint,out var nav,NavMesh.AllAreas)){EndPoint=nav.position;endsAtWall=true;}
    if((EndPoint-StartPoint).sqrMagnitude<1){nextCharge=now+.5f;return;}
    Phase=Stage.Windup;phaseAt=now;enemy.Agent.ResetPath();enemy.Agent.isStopped=true;enemy.Model.rotation=Quaternion.LookRotation(LockedDirection);ShowLane();return;
   }
   enemy.Agent.isStopped=false;if(now>=nextPath){nextPath=now+game.config.repathInterval;enemy.Agent.SetDestination(game.PlayerPosition);}
   if(enemy.Agent.velocity.sqrMagnitude>.03f)enemy.Model.rotation=Quaternion.Slerp(enemy.Model.rotation,Quaternion.LookRotation(enemy.Agent.velocity),Time.deltaTime*6);
   enemy.Actor.MoveInfected(enemy.Agent.velocity.magnitude,Mathf.Clamp01(enemy.Agent.velocity.magnitude/Mathf.Max(.1f,enemy.Agent.speed)));
  }
  void ShowLane(){var side=Vector3.Cross(Vector3.up,LockedDirection)*game.Chapter.chargeWidth*.5f;var a=StartPoint+Vector3.up*.08f;var b=EndPoint+Vector3.up*.08f;var neck=b-LockedDirection*Mathf.Min(1,(b-a).magnitude*.3f);warning.positionCount=7;warning.SetPositions(new[]{a-side,neck-side,neck-side*1.6f,b,neck+side*1.6f,neck+side,a+side});warning.gameObject.SetActive(true);progress.SetPosition(0,a);progress.SetPosition(1,a);progress.gameObject.SetActive(true);}
  static float DistanceToSegment(Vector3 p,Vector3 a,Vector3 b){p.y=a.y=b.y=0;var d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude)));}
  void Finish(float now,bool wall){Phase=Stage.Recovery;phaseAt=now;followup=!wall&&!followup&&enemy.Health.CurrentHealth<=game.Chapter.eliteHealth*.5f;recovery=wall?1.7f:followup?.6f:game.Chapter.eliteRecovery;Hide();enemy.Actor.SetEliteWindup(0);enemy.Actor.MoveInfected(0,0);if(wall){WallHits++;game.VisualEffects.Impact(transform.position+LockedDirection*.7f,Vector3.up);game.Sound.BreakCrate();}}
  void Hide(){if(warning)warning.gameObject.SetActive(false);if(progress)progress.gameObject.SetActive(false);}
  public void Cancel(){Hide();if(halo)halo.gameObject.SetActive(false);if(enemy&&enemy.Actor)enemy.Actor.SetEliteWindup(0);Phase=Stage.Hunt;}
  void OnDisable(){Cancel();}
  void OnDestroy(){if(warning)Destroy(warning.gameObject);if(progress)Destroy(progress.gameObject);}
 }
}
