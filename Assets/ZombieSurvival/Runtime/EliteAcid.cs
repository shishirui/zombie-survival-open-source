using UnityEngine;
using UnityEngine.AI;
namespace DeadDistrict {
 // One fixed volley of at most seven pools. All effects are created once and reused.
 public sealed class EliteAcid:MonoBehaviour {
  public enum Stage {Hunt,Windup,Flight,Recovery}
  public Stage Phase {get;private set;}
  public int Volleys {get;private set;}
  public int Hits {get;private set;}
  public int TargetCount {get;private set;}
  public int ActivePools {get;private set;}
  public float DamageMultiplier=>Phase==Stage.Recovery?1.45f:1;
  public string Caption=>Phase==Stage.Windup||Phase==Stage.Flight?"避开腐蚀落点":Phase==Stage.Recovery?"远离酸液 · 趁机攻击":"保持移动";
  public Vector3 TargetAt(int index)=>targets[index];
  public const int MaxTargets=7;
  readonly Vector3[] targets=new Vector3[MaxTargets];readonly LineRenderer[] rings=new LineRenderer[MaxTargets],progress=new LineRenderer[MaxTargets];readonly GameObject[] drops=new GameObject[MaxTargets],pools=new GameObject[MaxTargets],fills=new GameObject[MaxTargets];
  SurvivalGame game;SurvivalEnemy enemy;LineRenderer halo;Material liquid,groundLiquid,fillLiquid;Mesh poolMesh;float phaseAt,nextVolley,nextPath,nextDamage;Vector3 launch;bool landed;PackBurst[] missiles,splashes,mists,bubbles;
  public void Initialize(SurvivalGame owner,SurvivalEnemy actor){
   game=owner;enemy=actor;Phase=Stage.Hunt;Volleys=Hits=TargetCount=ActivePools=0;landed=false;nextVolley=Time.time+1;nextPath=0;enemy.Actor.SetEliteTint(new Color(.46f,.64f,.18f));
   if(!halo){var material=Resources.Load<Material>("DeadDistrict/AcidWarning");liquid=new Material(material);groundLiquid=new Material(Resources.Load<Material>("DeadDistrict/AcidPool"));fillLiquid=new Material(material);liquid.SetColor("_BaseColor",new Color(.32f,.67f,.09f,.32f));
    halo=BlockVisuals.Ring("Acid elite identity",transform,1,new Color(.7f,.93f,.2f,.8f),material);
    for(int i=0;i<MaxTargets;i++){
     rings[i]=BlockVisuals.Ring("Acid danger boundary "+i,null,1,new Color(1,.5f,.13f,.95f),material);rings[i].widthMultiplier=.11f;
     progress[i]=BlockVisuals.Ring("Acid warning progress "+i,null,1,new Color(.65f,.95f,.22f,.75f),material);progress[i].widthMultiplier=.07f;
     drops[i]=BlockVisuals.Shape("Acid projectile "+i,PrimitiveType.Sphere,null,Vector3.zero,Vector3.one*.5f,liquid);
     pools[i]=BlockVisuals.Shape("Acid pool "+i,PrimitiveType.Quad,null,Vector3.zero,Vector3.one,groundLiquid);pools[i].transform.rotation=Quaternion.Euler(90,i*73,0);
     if(!poolMesh){poolMesh=Instantiate(pools[i].GetComponent<MeshFilter>().sharedMesh);var uv=poolMesh.uv;for(int k=0;k<uv.Length;k++)uv[k]=uv[k]/3+Vector2.one/3;poolMesh.uv=uv;}pools[i].GetComponent<MeshFilter>().sharedMesh=poolMesh;
     fills[i]=BlockVisuals.Shape("Acid liquid surface "+i,PrimitiveType.Cylinder,null,Vector3.zero,Vector3.one,fillLiquid);
     foreach(var o in new[]{drops[i],pools[i],fills[i]}){var r=o.GetComponent<Renderer>();r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;}
    }
   }
   if(missiles==null){missiles=new PackBurst[MaxTargets];splashes=new PackBurst[MaxTargets];mists=new PackBurst[MaxTargets];bubbles=new PackBurst[MaxTargets];for(int i=0;i<MaxTargets;i++){missiles[i]=PackBurst.Create("AcidMissile",transform.parent);splashes[i]=PackBurst.Create("AcidSplash",transform.parent);mists[i]=PackBurst.Create("AcidMist",transform.parent);bubbles[i]=PackBurst.Create("AcidBubbles",transform.parent);}}Hide();halo.gameObject.SetActive(true);
  }
  bool Clear(Vector3 a,Vector3 b)=>!Physics.Linecast(a+Vector3.up*.8f,b+Vector3.up*.8f,1<<8);
  bool Near(Vector3 p,Vector3 center,float radius){var d=p-center;d.y=0;return d.sqrMagnitude<=radius*radius&&Clear(center,p);}
  bool Pick(Vector3 desired){
   if(!NavMesh.SamplePosition(desired,out var p,.7f,NavMesh.AllAreas)||Mathf.Abs(p.position.y-game.PlayerPosition.y)>.6f||Physics.CheckCapsule(p.position+Vector3.up*.4f,p.position+Vector3.up*1.5f,.55f,1<<8)||!Clear(transform.position,p.position))return false;
   for(int i=0;i<TargetCount;i++)if(Vector3.Distance(targets[i],p.position)<game.Chapter.acidRadius*1.8f)return false;targets[TargetCount++]=p.position;return true;
  }
  void Damage(float amount){float before=game.PlayerHealth.CurrentHealth;game.PlayerHealth.Damage(amount,gameObject,0,.28f,(game.PlayerPosition-transform.position).normalized);if(game.PlayerHealth.CurrentHealth<before){Hits++;game.DamageFlash();}}
  public void Tick(float now){
   if(!enemy.Alive||game.Dead||game.Completed||game.Paused)return;var cfg=game.Chapter;float comfort=MobilePreferences.Current.reducedFlash?.4f:1;
   if(Phase==Stage.Windup){enemy.Agent.isStopped=true;enemy.Actor.MoveInfected(0,0);float t=Mathf.Clamp01((now-phaseAt)/cfg.eliteWindup);enemy.Actor.SetEliteWindup(.7f*t);
    for(int i=0;i<TargetCount;i++)progress[i].transform.localScale=Vector3.one*cfg.acidRadius*Mathf.Lerp(.15f,1,t);
    if(t>=1){Phase=Stage.Flight;phaseAt=now;launch=enemy.AimPoint+Vector3.up*.5f;Volleys++;enemy.Actor.SetEliteWindup(0);enemy.Actor.Attack();game.Sound.AcidSpit();for(int i=0;i<TargetCount;i++){progress[i].gameObject.SetActive(false);drops[i].transform.position=launch;missiles[i].Play(launch,1,Color.white,cfg.acidFlightTime+.1f);}}return;
   }
   if(Phase==Stage.Flight){float t=Mathf.Clamp01((now-phaseAt)/cfg.acidFlightTime);for(int i=0;i<TargetCount;i++){var to=Vector3.Lerp(launch,targets[i]+Vector3.up*.08f,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*2);drops[i].transform.position=to;missiles[i].Position=to;}
    if(t>=1){Phase=Stage.Recovery;phaseAt=now;landed=true;ActivePools=TargetCount;nextDamage=now+cfg.acidTickInterval;game.Sound.AcidLand();bool hit=false;
     for(int i=0;i<TargetCount;i++){drops[i].SetActive(false);missiles[i].Stop();splashes[i].Play(targets[i]+Vector3.up*.08f,cfg.acidRadius*.58f,Color.white,1.1f);mists[i].Play(targets[i]+Vector3.up*.7f,cfg.acidRadius*.55f,Color.white,cfg.acidLifetime);bubbles[i].Play(targets[i]+Vector3.up*.06f,cfg.acidRadius*.7f,Color.white,cfg.acidLifetime);pools[i].transform.position=targets[i]+Vector3.up*.025f;pools[i].transform.localScale=Vector3.one*(cfg.acidRadius*2);pools[i].SetActive(true);fills[i].transform.position=targets[i]+Vector3.up*.015f;fills[i].transform.localScale=new Vector3(cfg.acidRadius*2,.006f,cfg.acidRadius*2);fills[i].SetActive(true);rings[i].startColor=rings[i].endColor=new Color(.67f,.94f,.2f,.9f);if(Near(game.PlayerPosition,targets[i],cfg.acidRadius))hit=true;}
     if(hit)Damage(cfg.eliteDamage);
    }return;
   }
   if(Phase==Stage.Recovery){enemy.Agent.isStopped=true;enemy.Actor.MoveInfected(0,0);float age=now-phaseAt;
    if(landed&&age<cfg.acidLifetime){fillLiquid.SetColor("_BaseColor",new Color(.32f,.55f,.05f,.19f*comfort*Mathf.Clamp01((cfg.acidLifetime-age)/.4f)));groundLiquid.SetColor("_BaseColor",new Color(.44f,.69f,.1f,.65f*comfort*Mathf.Clamp01((cfg.acidLifetime-age)/.4f)));for(int i=0;i<TargetCount;i++){float a=Mathf.Clamp01((cfg.acidLifetime-age)/.4f);rings[i].startColor=rings[i].endColor=new Color(.63f,.85f,.2f,.72f*a);}if(now>=nextDamage){nextDamage=now+cfg.acidTickInterval;for(int i=0;i<TargetCount;i++)if(Near(game.PlayerPosition,targets[i],cfg.acidRadius)){Damage(cfg.acidTickDamage);break;}}}
    else if(landed){Hide();landed=false;}
    if(age<Mathf.Max(cfg.eliteRecovery,cfg.acidLifetime))return;Phase=Stage.Hunt;nextVolley=now+cfg.eliteCooldown;enemy.Agent.isStopped=false;
   }
   var delta=game.PlayerPosition-transform.position;delta.y=0;
   if(now>=nextVolley&&delta.magnitude<=cfg.eliteRange&&Clear(transform.position,game.PlayerPosition)){
    TargetCount=0;Pick(game.PlayerPosition);var side=Vector3.Cross(Vector3.up,delta.sqrMagnitude>.01f?delta.normalized:Vector3.forward);// Six outer targets rotate between volleys, with a walkable gap between pools.
    for(int i=0;i<6;i++)Pick(game.PlayerPosition+Quaternion.AngleAxis(i*60+(Volleys%2)*30,Vector3.up)*side*(cfg.acidRadius*2+1.1f));
    if(TargetCount==0){nextVolley=now+.5f;return;}enemy.Agent.ResetPath();enemy.Agent.isStopped=true;Phase=Stage.Windup;phaseAt=now;liquid.SetColor("_BaseColor",new Color(.46f,.76f,.12f,.8f*comfort));groundLiquid.SetColor("_BaseColor",new Color(.44f,.69f,.1f,.65f*comfort));fillLiquid.SetColor("_BaseColor",new Color(.32f,.55f,.05f,.19f*comfort));
    if(delta.sqrMagnitude>.01f)enemy.Model.rotation=Quaternion.LookRotation(delta);
    for(int i=0;i<TargetCount;i++){rings[i].transform.position=progress[i].transform.position=targets[i];rings[i].transform.localScale=Vector3.one*cfg.acidRadius;progress[i].transform.localScale=Vector3.one*.1f;rings[i].startColor=rings[i].endColor=new Color(1,.5f,.13f,.95f);rings[i].gameObject.SetActive(true);progress[i].gameObject.SetActive(true);}return;
   }
   enemy.Agent.isStopped=false;if(now>=nextPath){nextPath=now+game.config.repathInterval;enemy.Agent.SetDestination(game.PlayerPosition);}
   if(enemy.Agent.velocity.sqrMagnitude>.02f)enemy.Model.rotation=Quaternion.Slerp(enemy.Model.rotation,Quaternion.LookRotation(enemy.Agent.velocity),Time.deltaTime*6);enemy.Actor.MoveInfected(enemy.Agent.velocity.magnitude,Mathf.Clamp01(enemy.Agent.velocity.magnitude/Mathf.Max(.1f,enemy.Agent.speed)));
  }
  void Hide(){for(int i=0;i<MaxTargets;i++){if(missiles!=null){missiles[i].Stop();splashes[i].Stop();mists[i].Stop();bubbles[i].Stop();}if(rings[i])rings[i].gameObject.SetActive(false);if(progress[i])progress[i].gameObject.SetActive(false);if(drops[i])drops[i].SetActive(false);if(pools[i])pools[i].SetActive(false);if(fills[i])fills[i].SetActive(false);}ActivePools=0;}
  public void Cancel(){Hide();if(halo)halo.gameObject.SetActive(false);if(enemy&&enemy.Actor)enemy.Actor.SetEliteWindup(0);Phase=Stage.Hunt;landed=false;}
  void OnDisable(){Cancel();}
  void OnDestroy(){for(int i=0;i<MaxTargets;i++){if(missiles!=null){missiles[i].Dispose();splashes[i].Dispose();mists[i].Dispose();bubbles[i].Dispose();}if(rings[i])Destroy(rings[i].gameObject);if(progress[i])Destroy(progress[i].gameObject);if(drops[i])Destroy(drops[i]);if(pools[i])Destroy(pools[i]);if(fills[i])Destroy(fills[i]);}if(liquid)Destroy(liquid);if(groundLiquid)Destroy(groundLiquid);if(fillLiquid)Destroy(fillLiquid);if(poolMesh)Destroy(poolMesh);}
 }
}
