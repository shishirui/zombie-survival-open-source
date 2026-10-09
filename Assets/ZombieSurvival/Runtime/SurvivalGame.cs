using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
namespace DeadDistrict {
 public sealed partial class SurvivalGame : MonoBehaviour {
  public SurvivalConfig config;
  public SurvivalHud hud;
  public SurvivalHealth PlayerHealth {get;private set;}
  public Vector3 PlayerPosition=>player.position;
  public IReadOnlyList<SurvivalEnemy> Enemies=>enemies;
  public int Kills {get;private set;}
  public int Ammo {get=>CurrentState.ammo;private set=>CurrentState.ammo=value;}
  public int Grenades {get;private set;}
  public int Wave {get;private set;}
  public int ActiveCount {get;private set;}
  public float Elapsed {get;private set;}
  public float NextHorde {get;private set;}
  public const float WaveBreakSeconds=5;
  public const float GameplayCameraSize=7.7f;
  public float ReloadProgress=>reloadEnd>0?Mathf.Clamp01(1-(reloadEnd-Time.time)/WeaponReloadSeconds):0;
  public bool Reloading=>reloadEnd>0;
  public bool Dead {get;private set;}
  public bool Paused {get;private set;}
  public int ShotsFired {get;private set;}
  float lastCombatShot=-10;
  public int Reloads {get;private set;}
  public int GrenadesThrown {get;private set;}
  public int GrenadeKills {get;private set;}
  public int SpawnViolations {get;private set;}
  public float MinSpawnDistance {get;private set;}=999;
  public int PeakAlive {get;private set;}
  public int PoolReuses {get;private set;}
  public bool AutoTest;
  public PackEffects VisualEffects {get;private set;}
  PackVisuals pack;LoftActor playerActor;float cameraKick;float[] targetVisibleSince,targetLastVisible;bool[] targetSightConfirmed;int[] visibleSpawn;
  public bool CombatValidation;
  public SurvivalSupplies Supplies {get;private set;}
  public WeaponLoot WeaponCrates {get;private set;}
  public EnemyHealthBars EnemyBars {get;private set;}
  public RunGrowth Growth {get;private set;}
  public WorldInteractions World {get;private set;}
  public RagdollCorpses Corpses {get;private set;}
  public string PickupMessage {get;private set;}="";
  public float PickupMessageUntil {get;private set;}
  string lastPickup="";
  public void NotifyPickup(string message){PickupMessage=Time.time<PickupMessageUntil&&lastPickup!=message?lastPickup+" · "+message:message;lastPickup=message;PickupMessageUntil=Time.time+1.6f;}
  public bool CanUseSupply(SupplyKind kind){if(Dead||Paused)return false;switch(kind){case SupplyKind.Medkit:return PlayerHealth.CurrentHealth<config.playerHealth;case SupplyKind.Ammo:return Ammo<MagazineCapacity||Reloading;case SupplyKind.Grenade:return Grenades<config.maxGrenades;case SupplyKind.Armor:return true;default:return false;}}
  public bool ApplySupply(SupplyKind kind){if(!CanUseSupply(kind))return false;switch(kind){case SupplyKind.Medkit:NotifyPickup("生命 +"+Mathf.CeilToInt(PlayerHealth.Heal(30)));break;case SupplyKind.Ammo:Ammo=MagazineCapacity;reloadEnd=0;Sound.CancelReload();NotifyPickup("弹匣已补满");break;case SupplyKind.Grenade:Grenades=Mathf.Min(config.maxGrenades,Grenades+1);nextGrenade=Time.time+config.grenadeRecharge;NotifyPickup("手雷 +1");break;case SupplyKind.Armor:PlayerHealth.GrantArmor();NotifyPickup("护甲 25 · 12 秒");break;}Feedback.Pickup(PlayerPosition,PlayerPosition,SurvivalSupplies.Tint(kind),kind==SupplyKind.Medkit);return true;}
  const int EnemyLayer=24, ObstacleLayer=8;
  readonly List<SurvivalEnemy> enemies=new List<SurvivalEnemy>();
  NavMeshPath spawnPath;readonly Vector3[] spawnCorners=new Vector3[64];
  Transform player,model;CharacterController controller;Camera cam;SurvivalEnemy target;
  LineRenderer targetRing;Material lineMat; Transform muzzle;Light flash;
  public SurvivalAudio Sound {get;private set;}

  public bool IsGrenadeAiming {get;private set;}
  public bool GrenadeAimValid=>IsGrenadeAiming&&grenadeStick.magnitude>.16f;
  public bool GrenadeInFlight=>grenadeAt>=0;
  public Vector3 GrenadeLandingPoint=>grenadeEnd;
  public Vector3 GrenadeAimPoint {get{var r=cam.transform.right;var f=cam.transform.forward;r.y=f.y=0;r.Normalize();f.Normalize();var delta=r*grenadeStick.x+f*grenadeStick.y;float range=Mathf.Lerp(2,12,Mathf.InverseLerp(.16f,1,grenadeStick.magnitude));return player.position+delta.normalized*range;}}
  Vector2 grenadeStick;LineRenderer aimTrajectory,aimLanding;
  Transform grenade;LineRenderer blast;Vector3 grenadeStart,grenadeEnd;float grenadeAt=-1,blastUntil;
  float nextScan,nextTrickle,nextGrenade,hitUntil;
  float nextShot {get=>CurrentState.nextShot;set=>CurrentState.nextShot=value;}
  float reloadEnd {get=>CurrentState.reloadEnd;set=>CurrentState.reloadEnd=value;}
  bool ready;int pendingHorde,hordeSpawnIndex;float nextHordeSpawn,hordeAngle;
  Vector3 cameraVelocity,lastMove=Vector3.forward;
  public static SurvivalGame Instance;
  void Awake(){config=Instantiate(config);Chapter=ChapterCatalog.Current();config.arenaHalfSize=Chapter.arenaHalfSize;if(!Chapter)throw new InvalidOperationException("Chapter configuration missing");config.maxAlive=Mathf.Clamp(Chapter.maxAlive,20,ChapterDefinition.MaximumActiveEnemies);Instance=this;Time.timeScale=1;Physics.IgnoreLayerCollision(24,24,true);var args=Environment.GetCommandLineArgs();AutoTest=Array.IndexOf(args,"-survival-smoke")>=0||Array.IndexOf(args,"-survival-capture")>=0;}
  void OnDestroy(){if(config)Destroy(config);Physics.IgnoreLayerCollision(26,24,false);if(EnemyBars)Destroy(EnemyBars.gameObject);BlockVisuals.ReleaseRuntimeMaterials();if(lineMat)Destroy(lineMat);}
  void Start() {
   spawnPath=new NavMeshPath();
   Application.targetFrameRate=60;Application.runInBackground=true;Screen.sleepTimeout=SleepTimeout.NeverSleep;
   UnityEngine.Random.InitState(AutoTest?81273:Environment.TickCount);
   player=new GameObject("Survivor").transform;player.gameObject.layer=26;Physics.IgnoreLayerCollision(26,24,false);
   controller=player.gameObject.AddComponent<CharacterController>();controller.radius=.34f;controller.height=1.8f;controller.center=new Vector3(0,.9f,0);controller.stepOffset=.2f;
   PlayerHealth=player.gameObject.AddComponent<SurvivalHealth>();PlayerHealth.Configure(config.playerHealth);PlayerHealth.ResetForSpawn();PlayerHealth.OnDeath+=EndRun;
   pack=Resources.Load<PackVisuals>("DeadDistrict/PackVisuals");
   if(!pack)throw new InvalidOperationException("Pack visuals not prepared");
   var actor=Instantiate(pack.survivor,player);model=actor.transform;playerActor=actor.GetComponent<LoftActor>();playerActor.Prepare();
   muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(model,false);muzzle.localPosition=new Vector3(.17f,1.7f,1.5f);
   if(playerActor.Muzzle)muzzle.position=playerActor.Muzzle.position;
   flash=muzzle.gameObject.AddComponent<Light>();flash.color=new Color(1,.67f,.25f);flash.range=3;flash.intensity=0;flash.shadows=LightShadows.None;
   cam=Camera.main;cam.orthographic=true;cam.orthographicSize=GameplayCameraSize;cam.transform.position=player.position+new Vector3(-14,21,-14);cam.transform.rotation=Quaternion.Euler(48,45,0);
   lineMat=new Material(Resources.Load<Material>("DeadDistrict/FX"));lineMat.SetColor("_BaseColor",Color.white);
   BlockVisuals.Ring("Survivor marker",player,.6f,new Color(.3f,.8f,.95f),lineMat);
   targetRing=BlockVisuals.Ring("Selected target",null,.55f,new Color(1,.4f,.12f),lineMat);targetRing.gameObject.SetActive(false);
   targetVisibleSince=new float[config.maxAlive];targetLastVisible=new float[config.maxAlive];targetSightConfirmed=new bool[config.maxAlive];visibleSpawn=new int[config.maxAlive];for(int i=0;i<config.maxAlive;i++)targetVisibleSince[i]=-1;
   for(int i=0;i<config.maxAlive;i++) {
    var g=new GameObject("Pooled zombie "+i);g.layer=EnemyLayer;
    var z=g.AddComponent<SurvivalEnemy>();
    z.Initialize(this,i,pack.infected[i%pack.infected.Length]);
    g.SetActive(false);enemies.Add(z);
   }
   CreateEffects();
   VisualEffects=gameObject.AddComponent<PackEffects>();VisualEffects.Initialize(pack);
   InitializeWeapons();gameObject.AddComponent<MobileComfort>().Initialize(this);InitializeDodge();Corpses=gameObject.AddComponent<RagdollCorpses>();Corpses.Initialize(this,pack);Grenades=config.maxGrenades;Ammo=MagazineCapacity;NextHorde=float.PositiveInfinity;nextTrickle=Time.time+.5f;nextGrenade=Time.time+config.grenadeRecharge;
   Sound=gameObject.AddComponent<SurvivalAudio>();Sound.Initialize();
   Supplies=gameObject.AddComponent<SurvivalSupplies>();Supplies.Initialize(this,pack);
   WeaponCrates=gameObject.AddComponent<WeaponLoot>();WeaponCrates.Initialize(this,pack);
   Feedback=gameObject.AddComponent<RewardFeedback>();Feedback.Initialize();
   GrowthEffects=player.gameObject.AddComponent<PlayerGrowthEffects>();GrowthEffects.Initialize(lineMat);Growth=new RunGrowth(this);
   if(hud)hud.Bind(this);
   World=gameObject.AddComponent<WorldInteractions>();World.Initialize(this,pack);
   EnemyBars=new GameObject("Enemy health bar pool").AddComponent<EnemyHealthBars>();EnemyBars.Initialize(this);
   OcclusionPreparation.Prepare(cam);
   ready=true;InitializeMobileFlow();Debug.Log("DEAD_DISTRICT_READY pool="+enemies.Count+" pipeline="+GraphicsSettings.currentRenderPipeline?.name);
  }
  void CreateEffects() {
   grenade=Instantiate(pack.grenade).transform;grenade.gameObject.SetActive(false);
   blast=BlockVisuals.Ring("Pooled blast",transform,1,new Color(1,.64f,.18f),lineMat);blast.gameObject.SetActive(false);
   aimLanding=BlockVisuals.Ring("Manual grenade blast preview",transform,1,new Color(1,.72f,.3f,.65f),lineMat);aimLanding.widthMultiplier=.05f;aimLanding.gameObject.SetActive(false);
   var arc=new GameObject("Manual grenade trajectory");arc.transform.SetParent(transform,false);aimTrajectory=arc.AddComponent<LineRenderer>();aimTrajectory.sharedMaterial=lineMat;aimTrajectory.useWorldSpace=true;aimTrajectory.positionCount=32;aimTrajectory.widthMultiplier=.075f;aimTrajectory.startColor=new Color(1,.8f,.45f,.9f);aimTrajectory.endColor=new Color(1,.6f,.2f,.6f);aimTrajectory.shadowCastingMode=ShadowCastingMode.Off;aimTrajectory.receiveShadows=false;arc.SetActive(false);
  }
  void Update() {
   if(Completed){if(Input.GetKeyDown(KeyCode.Escape))hud.HandleShellBack();return;}
   if(Input.GetKeyDown(KeyCode.Escape)&&!Growth.Choosing){if(!hud.HandleShellBack()&&!Dead)TogglePause();}
   if(Dead) {UpdateEffects(Time.time);if(Input.GetKeyDown(KeyCode.R))Restart();return;}
   if(Growth.Choosing){Growth.TickChoice();return;}
   if(Paused)return;
   Growth.TickPending();if(Paused)return;
   float now=Time.time;Elapsed+=Time.deltaTime;
   if(Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift))TryRoll();
   MovePlayer();
   Supplies.Tick(now);WeaponCrates.Tick(now);World.Tick();
   if(Input.GetKeyDown(KeyCode.E))World.Interact();
   if(Input.GetKeyDown(KeyCode.Q))SwitchWeapon();
   if(Input.GetKeyDown(KeyCode.R))TryReload();
   if(now>=nextScan){AcquireTarget();nextScan=now+(target!=null?.12f:.05f);}
   AimAndShoot(now);
   if(!CombatValidation)SpawnDirector(now);
   if(Completed)return;
   foreach(var z in enemies)if(z.Alive)z.Tick(now);
   if(Input.GetKeyDown(KeyCode.G)||SurvivalInput.GrenadeRequested){SurvivalInput.GrenadeRequested=false;ThrowGrenade();}
   if(now>=nextGrenade){if(Grenades<config.maxGrenades)Grenades++;nextGrenade=now+config.grenadeRecharge;}
   UpdateEffects(now);
   Sound.SetPressure(ActiveCount>=20||pendingHorde>0||(!float.IsPositiveInfinity(NextHorde)&&NextHorde-Elapsed<5));
   if(hud)hud.Refresh();
  }
  void LateUpdate() {
   if(!player)return;if(Paused){if(hud)hud.RefreshLootHint(cam);return;}
   Vector3 desired=player.position+new Vector3(-14,21,-14);
   cam.transform.position=Vector3.SmoothDamp(cam.transform.position,desired,ref cameraVelocity,.16f);
   UpdateGrenadePreview();
   if(cameraKick>0){cam.transform.position+=new Vector3(Mathf.Sin(Time.time*71),Mathf.Cos(Time.time*89),0)*cameraKick*MobilePreferences.Current.shake;cameraKick=Mathf.MoveTowards(cameraKick,0,Time.deltaTime*1.6f);}
   if(hud)hud.RefreshLootHint(cam);
  }
  void MovePlayer() {
   if(Rolling){MoveDodge();return;}
   Vector2 input=CombatValidation?Vector2.zero:AutoTest?TestMove():SurvivalInput.ReadMove();
   Vector3 right=cam.transform.right,forward=cam.transform.forward;right.y=forward.y=0;right.Normalize();forward.Normalize();
   Vector3 dir=right*input.x+forward*input.y;
   controller.Move((dir*config.moveSpeed+Vector3.down*12)*Time.deltaTime);
   if(dir.sqrMagnitude>.02f)lastMove=dir;
   playerActor.Move(Mathf.Clamp01(dir.magnitude));Sound.SetWalking(dir.magnitude>.2f);
  }
  Vector2 TestMove() {
   if(Elapsed>60)return Vector2.zero;
   return new Vector2(Mathf.Cos(Elapsed*.3f),Mathf.Sin(Elapsed*.3f));
  }
  public bool CanAutoTarget(SurvivalEnemy z) {
   if(z==null||!z.Alive)return false;
   Vector3 from=player.position+Vector3.up*1.1f,to=z.AimPoint;
   if((to-from).sqrMagnitude>WeaponRange*WeaponRange)return false;
   // Keep the body within the readable play area, clear of screen edges and HUD.
   if(!WithinFiringView(z.transform.position+Vector3.up*.15f)||!WithinFiringView(z.transform.position+Vector3.up*z.VisualHeight))return false;
   if(hud&&hud.TargetCovered(z.transform.position+Vector3.up*.15f,z.transform.position+Vector3.up*z.VisualHeight))return false;
   if(Physics.Linecast(cam.transform.position,to,1<<ObstacleLayer))return false;
   return !Physics.Linecast(from,to,1<<ObstacleLayer);
  }
  bool WithinFiringView(Vector3 point){var v=cam.WorldToViewportPoint(point);return v.z>cam.nearClipPlane&&v.x>.06f&&v.x<.94f&&v.y>.18f&&v.y<.82f;}
  bool ObserveTarget(SurvivalEnemy z,float now){
   int slot=z.Slot;
   if(visibleSpawn[slot]!=z.SpawnCount){visibleSpawn[slot]=z.SpawnCount;targetVisibleSince[slot]=-1;targetLastVisible[slot]=-10;targetSightConfirmed[slot]=false;}
   bool visible=CanAutoTarget(z);
   // Only a previously confirmed target keeps its lock through a short obstruction.
   // Every shot still requires current on-screen visibility and clear cover checks.
   if(!visible){if(!z.Alive||!targetSightConfirmed[slot]||now-targetLastVisible[slot]>.25f){targetVisibleSince[slot]=-1;targetSightConfirmed[slot]=false;}return false;}
   if(now-targetLastVisible[slot]>.25f){targetVisibleSince[slot]=-1;targetSightConfirmed[slot]=false;}
   if(targetVisibleSince[slot]<0)targetVisibleSince[slot]=now;
   targetLastVisible[slot]=now;if(now-targetVisibleSince[slot]>=.22f)targetSightConfirmed[slot]=true;
   return true;
  }
  void AcquireTarget() {
   float best=float.MaxValue,nearest=float.MaxValue;SurvivalEnemy selected=null,waiting=null;
   foreach(var z in enemies) {
    if(!ObserveTarget(z,Time.time))continue;
    float d=(z.transform.position-player.position).sqrMagnitude;
    if(d<nearest){nearest=d;waiting=z;}
    // Keep a live target unless another is substantially closer; prefer opponents already seen.
    float score=d*(z==target?.55f:1);
    if(targetSightConfirmed[z.Slot]&&score<best){best=score;selected=z;}
   }target=selected?selected:waiting;
  }
  void AimAndShoot(float now) {
   if(Rolling){targetRing.gameObject.SetActive(false);return;}
   if(target!=null&&!CanAutoTarget(target)){target=null;AcquireTarget();nextScan=now+(target!=null?.12f:.05f);}
   Vector3 aim=CanAutoTarget(target)?target.transform.position-player.position:model.forward;aim.y=0;
   if(aim.sqrMagnitude>.01f)model.rotation=Quaternion.Slerp(model.rotation,Quaternion.LookRotation(aim),Time.deltaTime*20);
   targetRing.gameObject.SetActive(target!=null&&target.Alive);
   if(targetRing.gameObject.activeSelf)targetRing.transform.position=target.transform.position;
   if(reloadEnd>0) {if(now>=reloadEnd){Ammo=MagazineCapacity;reloadEnd=0;Sound.CancelReload();Reloads++;}else return;}
   if(Ammo<=0){TryReload();return;}
   if(now<nextShot||!CanAutoTarget(target)||targetVisibleSince[target.Slot]<0||(!targetSightConfirmed[target.Slot]&&now-lastCombatShot>Mathf.Max(.25f,1/WeaponFireRate+.08f)&&now-targetVisibleSince[target.Slot]<.22f))return;
   if(CurrentWeapon==WeaponKind.Launcher&&!Launcher.CanLaunch)return;
   targetSightConfirmed[target.Slot]=true;
   // Align the muzzle on a scheduled shot so turning cannot add a pause between victims.
   model.rotation=Quaternion.LookRotation(aim);
   nextShot=Mathf.Max(nextShot+1/WeaponFireRate,now+.5f/WeaponFireRate);Ammo--;ShotsFired++;lastCombatShot=now;
   FireWeapon();
  }

  public bool CanSpawnEnemyAt(Vector3 point){
   // Random reinforcements enter from streets, including while a room is open.
   // Check after NavMesh sampling, which can otherwise snap an outdoor candidate inside.
   if(World.RoomAt(point)!=null||Physics.CheckCapsule(point+Vector3.up*.45f,point+Vector3.up*1.7f,.42f,1<<ObstacleLayer,QueryTriggerInteraction.Ignore))return false;
   // When the player shelters inside, validate the outdoor route to their entrance.
   // The existing pursuit and door-breaking system handles the last leg.
   if(!NavMesh.SamplePosition(World.EnemyApproachPoint,out var goal,.8f,NavMesh.AllAreas))return false;
   if(!NavMesh.CalculatePath(point,goal.position,NavMesh.AllAreas,spawnPath)||spawnPath.status!=NavMeshPathStatus.PathComplete)return false;
   int count=spawnPath.GetCornersNonAlloc(spawnCorners);if(count<1||count>=spawnCorners.Length)return false;
   // NavMesh obstacle carving settles asynchronously. Reject physical obstructions even during that gap.
   for(int i=1;i<count;i++){var delta=spawnCorners[i]-spawnCorners[i-1];float distance=delta.magnitude;if(distance>.01f&&Physics.SphereCast(spawnCorners[i-1]+Vector3.up*.9f,.42f,delta/distance,out _,distance,1<<ObstacleLayer,QueryTriggerInteraction.Ignore))return false;}
   return true;
  }
  public float MinimumReinforcementDistance=>Chapter.number>=2?12f:22f;
  public bool IsReinforcementOffscreen(Vector3 point){
   // The complete body must stay beyond a padded screen edge, including elites.
   var feet=cam.WorldToViewportPoint(point);var head=cam.WorldToViewportPoint(point+Vector3.up*3.3f);
   return feet.z<=0&&head.z<=0||Mathf.Max(feet.x,head.x)<-.12f||Mathf.Min(feet.x,head.x)>1.12f||Mathf.Max(feet.y,head.y)<-.15f||Mathf.Min(feet.y,head.y)>1.15f;
  }
  float NearbySpawnRadius(Vector3 direction,int attempt){
   float edge=MinimumReinforcementDistance;
   while(edge<34&&!IsReinforcementOffscreen(player.position+direction*edge))edge+=2;
   // Fall back a little farther out when a building blocks the nearest edge.
   return edge+UnityEngine.Random.Range(.6f,2.6f)+(attempt>=12?UnityEngine.Random.Range(2f,6f):0);
  }
  public bool TrySpawn(float angle) {
   if(ActiveCount>=config.maxAlive)return false;
   SurvivalEnemy free=null;foreach(var z in enemies)if(!z.gameObject.activeSelf){free=z;break;}if(free==null)return false;
   for(int i=0;i<20;i++) {
    float a=angle+UnityEngine.Random.Range(-.1f,.1f);var direction=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
    float radius=Chapter.number>=2?NearbySpawnRadius(direction,i):UnityEngine.Random.Range(24f,Elapsed<18&&i<10?28f:32f);
    Vector3 pos=player.position+direction*radius;
    if(Mathf.Abs(pos.x)>config.arenaHalfSize-2||Mathf.Abs(pos.z)>config.arenaHalfSize-2)continue;
    if(!NavMesh.SamplePosition(pos,out NavMeshHit hit,2,NavMesh.AllAreas))continue;
    Vector3 v=cam.WorldToViewportPoint(hit.position+Vector3.up*1.5f);
    if(Chapter.number>=2?!IsReinforcementOffscreen(hit.position):v.z>0&&v.x>-.12f&&v.x<1.12f&&v.y>-.15f&&v.y<1.15f)continue;
    float d=Vector3.Distance(hit.position,player.position);if(d<MinimumReinforcementDistance)continue;
    if(Chapter.number>=2&&Physics.CheckSphere(hit.position+Vector3.up*.8f,.65f,1<<EnemyLayer,QueryTriggerInteraction.Ignore))continue;
    if(!CanSpawnEnemyAt(hit.position))continue;
    if(directedElite&&Physics.CheckCapsule(hit.position+Vector3.up*.65f,hit.position+Vector3.up*2.5f,.64f,1<<ObstacleLayer))continue;
    MinSpawnDistance=Mathf.Min(MinSpawnDistance,d);
    if(d<MinimumReinforcementDistance)SpawnViolations++;
    if(free.SpawnCount>0)PoolReuses++;
    EnemyKind kind=directedKind??new HordePressure(Elapsed,config,Wave).ChooseKind(UnityEngine.Random.value);
    free.Spawn(hit.position,config.enemySpeed+UnityEngine.Random.Range(-.3f,.25f),kind,directedElite);if(directedElite)Elite=free;
    ActiveCount++;PeakAlive=Mathf.Max(PeakAlive,ActiveCount);return true;
   } return false;
  }
  public void EnemyKilled(SurvivalEnemy z) {if(!z.gameObject.activeSelf)return;Corpses.Drop(z);VisualEffects.Death(z.transform.position,Launcher&&Launcher.ApplyingBlast?.45f:1);Sound.Kill();Kills++;Growth.Award(z.Kind);ActiveCount=Mathf.Max(0,ActiveCount-1);Supplies.OnKill(z.transform.position);z.Retire();targetVisibleSince[z.Slot]=-1;targetSightConfirmed[z.Slot]=false;targetLastVisible[z.Slot]=-10;if(target==z){target=null;nextScan=0;}}
  public bool BeginGrenadeAim(){if(!ready||Dead||Paused||Rolling||Grenades<=0||GrenadeInFlight)return false;IsGrenadeAiming=true;grenadeStick=Vector2.zero;return true;}
  public void SetGrenadeAim(Vector2 stick){if(IsGrenadeAiming)grenadeStick=Vector2.ClampMagnitude(stick,1);}
  public void EndGrenadeAim(bool release){bool commit=release&&GrenadeAimValid;Vector3 point=commit?GrenadeAimPoint:Vector3.zero;IsGrenadeAiming=false;grenadeStick=Vector2.zero;if(aimLanding)aimLanding.gameObject.SetActive(false);if(aimTrajectory)aimTrajectory.gameObject.SetActive(false);if(commit)ThrowGrenadeAt(point);}
  void UpdateGrenadePreview(){bool show=GrenadeAimValid&&!Dead&&!Paused;aimLanding.gameObject.SetActive(show);aimTrajectory.gameObject.SetActive(show);if(!show)return;Vector3 from=player.position+Vector3.up*1.2f,to=GrenadeAimPoint;aimLanding.transform.position=to;aimLanding.transform.localScale=Vector3.one*config.grenadeRadius;for(int i=0;i<32;i++){float t=i/31f;aimTrajectory.SetPosition(i,Vector3.Lerp(from,to,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*3);}}
  // Keyboard shortcut and existing gameplay tests retain the quick-throw path.
  public void ThrowGrenade(){Vector3 dest=target&&target.Alive?target.transform.position:player.position+lastMove.normalized*8;EndGrenadeAim(false);ThrowGrenadeAt(dest);}
  void ThrowGrenadeAt(Vector3 destination){
   if(Dead||Paused||Rolling||Grenades<=0||GrenadeInFlight)return;
   Vector3 delta=destination-player.position;delta.y=0;delta=Vector3.ClampMagnitude(delta,12);
   Grenades--;GrenadesThrown++;grenadeStart=player.position+Vector3.up*1.2f;
   grenadeEnd=player.position+delta;grenadeAt=Time.time;grenade.position=grenadeStart;grenade.gameObject.SetActive(true);
  }
  void UpdateEffects(float now) {
   flash.intensity=Mathf.MoveTowards(flash.intensity,0,Time.deltaTime*35);
   if(grenadeAt>=0) {
    float t=(now-grenadeAt)/.9f;grenade.position=Vector3.Lerp(grenadeStart,grenadeEnd,t)+Vector3.up*Mathf.Sin(Mathf.Clamp01(t)*Mathf.PI)*3;
    if(t>=1) {
     grenadeAt=-1;growthActionUntil=now+1;grenade.gameObject.SetActive(false);blast.transform.position=grenadeEnd;blast.transform.localScale=Vector3.one;blast.gameObject.SetActive(true);blastUntil=now+.35f;
     VisualEffects.Explosion(grenadeEnd);cameraKick=.20f;
     int before=Kills;
     foreach(var z in enemies)if(!Dead&&z.Alive&&(z.transform.position-grenadeEnd).sqrMagnitude<=config.grenadeRadius*config.grenadeRadius)z.TakeBlast(config.grenadeDamage,grenadeEnd);
     World.DamageProps(grenadeEnd,config.grenadeRadius,config.grenadeDamage);
     GrenadeKills+=Kills-before;
     Sound.Explosion();
    }
   }
   if(blast.gameObject.activeSelf){float t=1-(blastUntil-now)/.35f;blast.transform.localScale=Vector3.one*Mathf.Lerp(.5f,config.grenadeRadius,t);if(now>=blastUntil)blast.gameObject.SetActive(false);}
   if(hud)hud.SetHitAlpha(Mathf.Clamp01((hitUntil-now)*2)*.2f*(MobilePreferences.Current.reducedFlash?.4f:1));
  }
  public void DamageFlash(){hitUntil=Time.time+.35f;Sound.Hurt();}
  void EndRun(){if(Completed||Dead)return;foreach(var e in enemies)e.CancelEliteAttack();Growth.Cancel();Time.timeScale=1;CancelDodge();Corpses.FreezeAll();EndGrenadeAim(false);Supplies.Clear();WeaponCrates.Clear();PlayerHealth.ClearArmor();PickupMessage="";PickupMessageUntil=0;Sound.EndRun();Dead=true;Paused=false;foreach(var z in enemies)if(z.Alive&&z.Agent.isOnNavMesh)z.Agent.isStopped=true;if(hud)hud.ShowDeath();Debug.Log("RUN_ENDED time="+Elapsed+" kills="+Kills);}
  public void TogglePause(){if(Completed||Dead||!ready||!RunStarted||Growth.Choosing)return;if(Paused)ResumeRun();else{PauseForShell();hud.ShowPause(true);}}
  void OnApplicationPause(bool pause){if(pause&&!Paused&&!Dead)TogglePause();}
  public void Restart(){startAfterReload=true;MobilePreferences.Flush();EndGrenadeAim(false);Time.timeScale=1;SurvivalInput.Move=Vector2.zero;SurvivalInput.GrenadeRequested=false;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
 }
}
