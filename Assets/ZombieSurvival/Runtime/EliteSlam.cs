using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 public sealed class EliteSlam:MonoBehaviour {
  public enum Stage {Hunt,Windup,Recovery}
  public Stage Phase {get;private set;}
  public Vector3 ImpactPoint {get;private set;}
  public int Slams {get;private set;}
  public int Hits {get;private set;}
  public float DamageMultiplier=>Phase==Stage.Recovery?1.45f:1;
  SurvivalGame game;SurvivalEnemy enemy;LineRenderer warning,charge,shock,halo;
  float phaseAt,nextSlam,nextPath,shockAt=-10;
  public void Initialize(SurvivalGame owner,SurvivalEnemy actor){
   game=owner;enemy=actor;Phase=Stage.Hunt;Slams=Hits=0;nextSlam=Time.time+1;nextPath=0;shockAt=-10;
   if(!warning){
    var material=Resources.Load<Material>("DeadDistrict/FX");
    warning=BlockVisuals.Ring("Elite danger boundary",null,1,new Color(1,.24f,.12f,.9f),material);warning.widthMultiplier=.11f;
    charge=BlockVisuals.Ring("Elite charge progress",null,1,new Color(1,.52f,.15f,.8f),material);charge.widthMultiplier=.065f;
    shock=BlockVisuals.Ring("Elite ground shock",null,1,new Color(1,.65f,.3f,.65f),material);shock.widthMultiplier=.12f;
    halo=BlockVisuals.Ring("Elite identity",transform,.9f,new Color(1,.43f,.16f,.7f),material);halo.widthMultiplier=.06f;
   }
   HideWarning();shock.gameObject.SetActive(false);halo.gameObject.SetActive(true);
  }
  bool Clear(Vector3 from,Vector3 to)=>!Physics.Linecast(from+Vector3.up*.7f,to+Vector3.up*.7f,1<<8);
  public void Tick(float now){
   if(!enemy.Alive)return;var cfg=game.Chapter;
   if(shock.gameObject.activeSelf){float t=(now-shockAt)/.4f;shock.transform.localScale=Vector3.one*Mathf.Lerp(.2f,cfg.eliteRadius,Mathf.Clamp01(t));var c=new Color(1,.61f,.24f,(1-t)*(MobilePreferences.Current.reducedFlash?.25f:.65f));shock.startColor=shock.endColor=c;if(t>=1)shock.gameObject.SetActive(false);}
   if(Phase==Stage.Windup){
    enemy.Agent.isStopped=true;enemy.Actor.MoveInfected(0,0);float t=Mathf.Clamp01((now-phaseAt)/cfg.eliteWindup);enemy.Actor.SetEliteWindup(Mathf.SmoothStep(0,1,t*2));charge.transform.localScale=Vector3.one*cfg.eliteRadius*Mathf.Lerp(.18f,1,t);
    if(now-phaseAt>=cfg.eliteWindup){
     HideWarning();enemy.Actor.SetEliteWindup(0);enemy.Actor.Attack();Slams++;Phase=Stage.Recovery;phaseAt=now;shockAt=now;shock.transform.position=ImpactPoint;shock.gameObject.SetActive(true);
     game.VisualEffects.Impact(ImpactPoint,Vector3.up);game.Sound.BreakCrate();
     var delta=game.PlayerPosition-ImpactPoint;delta.y=0;
     if(!game.Dead&&!game.Completed&&delta.sqrMagnitude<=cfg.eliteRadius*cfg.eliteRadius&&Clear(ImpactPoint,game.PlayerPosition)){
      float before=game.PlayerHealth.CurrentHealth;game.PlayerHealth.Damage(cfg.eliteDamage,gameObject,0,.25f,delta.normalized);if(game.PlayerHealth.CurrentHealth<before){Hits++;game.DamageFlash();}
     }
    }return;
   }
   if(Phase==Stage.Recovery){enemy.Agent.isStopped=true;enemy.Actor.MoveInfected(0,0);if(now-phaseAt<cfg.eliteRecovery)return;Phase=Stage.Hunt;nextSlam=now+cfg.eliteCooldown;enemy.Agent.isStopped=false;}
   var direction=game.PlayerPosition-transform.position;direction.y=0;
   if(now>=nextSlam&&direction.magnitude<=cfg.eliteRange&&Clear(transform.position,game.PlayerPosition)&&NavMesh.SamplePosition(game.PlayerPosition,out var hit,.6f,NavMesh.AllAreas)){
    ImpactPoint=hit.position;Phase=Stage.Windup;phaseAt=now;enemy.Agent.ResetPath();enemy.Agent.isStopped=true;
    if(direction.sqrMagnitude>.01f)enemy.Model.rotation=Quaternion.LookRotation(direction);
    warning.transform.position=charge.transform.position=ImpactPoint;warning.transform.localScale=Vector3.one*cfg.eliteRadius;charge.transform.localScale=Vector3.one*.1f;warning.gameObject.SetActive(true);charge.gameObject.SetActive(true);enemy.Actor.MoveInfected(0,0);return;
   }
   enemy.Agent.isStopped=false;if(now>=nextPath){nextPath=now+game.config.repathInterval;enemy.Agent.SetDestination(game.PlayerPosition);}
   if(enemy.Agent.velocity.sqrMagnitude>.02f)enemy.Model.rotation=Quaternion.Slerp(enemy.Model.rotation,Quaternion.LookRotation(enemy.Agent.velocity),Time.deltaTime*6);
   enemy.Actor.MoveInfected(enemy.Agent.velocity.magnitude,Mathf.Clamp01(enemy.Agent.velocity.magnitude/Mathf.Max(.1f,enemy.Agent.speed)));
  }
  void HideWarning(){if(warning)warning.gameObject.SetActive(false);if(charge)charge.gameObject.SetActive(false);}
  public void Cancel(){HideWarning();if(shock)shock.gameObject.SetActive(false);if(halo)halo.gameObject.SetActive(false);if(enemy&&enemy.Actor)enemy.Actor.SetEliteWindup(0);Phase=Stage.Hunt;}
  void OnDisable(){Cancel();}
  void OnDestroy(){foreach(var ring in new[]{warning,charge,shock})if(ring)Destroy(ring.gameObject);}
 }
}
