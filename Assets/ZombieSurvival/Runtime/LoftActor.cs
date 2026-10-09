using UnityEngine;
namespace DeadDistrict {
 public sealed class LoftActor : MonoBehaviour {
  public bool Quadruped;
  Animator animator;ParticleSystem[] gunParticles;Color[] launcherMuzzleColors;Transform rifle,shotgun,launcher;bool rolling;
  sealed class FlashSurface {public Renderer renderer;public int index;public Color color,defaultColor;public MaterialPropertyBlock original,flash;}
  FlashSurface[] surfaces;Transform body;Vector3 bodyPosition,recoil;Quaternion bodyRotation;float hitAt=-10;
  public int HitReactionsPlayed {get;private set;}
  public bool HitReactionActive=>Time.time-hitAt<.18f;
  static readonly int BaseColor=Shader.PropertyToID("_BaseColor");
  public Transform Muzzle {get;private set;}
  public Transform Body=>body;
  EnemyKind enemyKind;bool infected;float gaitPhase,movement,attackUntil;Quaternion[] gaitOriginal;Transform[] gaitBones;
  WeaponKind equippedWeapon;float eliteWindup;
  public void SetEliteWindup(float value){eliteWindup=Mathf.Clamp01(value);}
  public Transform Bone(int index)=>animator&&animator.isHuman?animator.GetBoneTransform((HumanBodyBones)index):null;
  public void CopyAppearance(Renderer[] destinations){foreach(var r in destinations)foreach(var s in surfaces)if(r.name==s.renderer.name&&s.index<r.sharedMaterials.Length){r.SetPropertyBlock(s.original,s.index);if(r is SkinnedMeshRenderer dst&&s.renderer is SkinnedMeshRenderer src)dst.sharedMesh=src.sharedMesh;}}
  public void SetKind(EnemyKind kind){
   enemyKind=kind;infected=true;eliteWindup=0;gaitPhase=Random.value*Mathf.PI*2;movement=0;attackUntil=0;

   if(animator){animator.SetBool("Fast",kind==EnemyKind.Runner);animator.speed=1;animator.Play(kind==EnemyKind.Runner?"Purchased running":"Locomotion",0,Random.value);}
   if(gaitBones==null&&animator&&animator.isHuman){gaitBones=new[]{animator.GetBoneTransform(HumanBodyBones.Spine),animator.GetBoneTransform(HumanBodyBones.Head),animator.GetBoneTransform(HumanBodyBones.LeftUpperArm),animator.GetBoneTransform(HumanBodyBones.RightUpperArm),animator.GetBoneTransform(HumanBodyBones.LeftLowerArm),animator.GetBoneTransform(HumanBodyBones.RightLowerArm)};gaitOriginal=new Quaternion[gaitBones.Length];}
   foreach(var s in surfaces){bool skin=s.defaultColor.g>.48f&&s.defaultColor.r>.4f&&s.defaultColor.b<.42f;
    s.color=Quadruped||kind==EnemyKind.Normal||skin?s.defaultColor:Color.Lerp(s.defaultColor,kind==EnemyKind.Runner?new Color(.78f,.22f,.07f):new Color(.34f,.25f,.48f),.8f);
    s.original.SetColor(BaseColor,s.color);s.renderer.SetPropertyBlock(s.original,s.index);
   }
  }
  public void SetEliteTint(Color tint){foreach(var s in surfaces){s.color=Color.Lerp(s.color,tint,.72f);s.original.SetColor(BaseColor,s.color);s.renderer.SetPropertyBlock(s.original,s.index);}}
  public void SetRolling(bool rolling){
   this.rolling=rolling;
   if(animator){animator.SetBool("Rolling",rolling);if(animator.layerCount>1)animator.SetLayerWeight(1,rolling?0:1);}
   if(rolling){if(gunParticles!=null)foreach(var ps in gunParticles)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);if(rifle)rifle.gameObject.SetActive(false);if(shotgun)shotgun.gameObject.SetActive(false);if(launcher)launcher.gameObject.SetActive(false);}
   else if(rifle&&shotgun)EquipWeapon(equippedWeapon);
  }
  public void Prepare(){
   animator=GetComponentInChildren<Animator>();
   if(animator){animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;}
   body=animator?animator.transform:null;if(body){bodyPosition=body.localPosition;bodyRotation=body.localRotation;}
   var list=new System.Collections.Generic.List<FlashSurface>();
   foreach(var r in GetComponentsInChildren<SkinnedMeshRenderer>())for(int i=0;i<r.sharedMaterials.Length;i++){
    var m=r.sharedMaterials[i];if(!m||!m.HasProperty(BaseColor))continue;
    var original=new MaterialPropertyBlock();r.GetPropertyBlock(original,i);var flash=new MaterialPropertyBlock();r.GetPropertyBlock(flash,i);
    list.Add(new FlashSurface{renderer=r,index=i,color=m.GetColor(BaseColor),defaultColor=m.GetColor(BaseColor),original=original,flash=flash});
   }surfaces=list.ToArray();
   var gun=transform.Find("Weapon");rifle=gun;
   if(gun){gunParticles=gun.GetComponentsInChildren<ParticleSystem>(true);foreach(var p in gunParticles)p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);Muzzle=Find(gun,"Epic Muzzle")??Find(gun,"MuzzleFlare");}
  }
  public void AddShotgun(GameObject prefab){if(!prefab)throw new System.InvalidOperationException("Shotgun model missing");shotgun=Instantiate(prefab,transform).transform;shotgun.name="Shotgun weapon";shotgun.gameObject.SetActive(false);}
  public void AddLauncher(GameObject prefab){if(!prefab)throw new System.InvalidOperationException("Launcher model missing");launcher=Instantiate(prefab,transform).transform;launcher.name="Grenade launcher";launcherMuzzleColors=System.Array.ConvertAll(launcher.GetComponentsInChildren<ParticleSystem>(true),x=>x.main.startColor.color);launcher.gameObject.SetActive(false);}
  public bool LauncherVisible=>launcher&&launcher.gameObject.activeSelf&&!rifle.gameObject.activeSelf&&!shotgun.gameObject.activeSelf;
  public void EquipWeapon(WeaponKind kind){
   equippedWeapon=kind;
   if(gunParticles!=null)foreach(var p in gunParticles)p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
   rifle.gameObject.SetActive(kind==WeaponKind.Rifle);shotgun.gameObject.SetActive(kind==WeaponKind.Shotgun);if(launcher)launcher.gameObject.SetActive(kind==WeaponKind.Launcher);
   var gun=kind==WeaponKind.Launcher?launcher:kind==WeaponKind.Shotgun?shotgun:rifle;gunParticles=gun.GetComponentsInChildren<ParticleSystem>(true);Muzzle=Find(gun,"Epic Muzzle")??Find(gun,"MuzzleFlare");
  }
  public bool ShotgunVisible=>shotgun&&shotgun.gameObject.activeSelf&&rifle&&!rifle.gameObject.activeSelf;
  static Transform Find(Transform root,string name){foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
  public void Move(float speed){if(animator)animator.SetFloat("Speed",speed,.12f,Time.deltaTime);}
  public void MoveInfected(float velocity,float ratio){movement=ratio;Move(ratio);if(animator){float stride=Quadruped?5.4f:enemyKind==EnemyKind.Runner?4.3f:enemyKind==EnemyKind.Brute?2.8f*1.28f:2.8f;float pace=Mathf.Clamp(velocity/stride,.22f,1.6f);animator.speed=Time.time<attackUntil?1:ratio>.05f?pace:1;gaitPhase+=Time.deltaTime*pace*(Quadruped?Mathf.PI*2/.62f:6);}}
  public void Attack(){attackUntil=Time.time+(Quadruped?.3f:.65f);if(animator&&!Quadruped)animator.SetTrigger("Attack");}
  public void Hit(Vector3 direction){HitReactionsPlayed++;hitAt=Time.time;direction.y=0;recoil=transform.InverseTransformDirection(direction.normalized);}
  public void ClearHit(){hitAt=-10;if(surfaces!=null)foreach(var s in surfaces)s.renderer.SetPropertyBlock(s.original,s.index);if(body){body.localPosition=bodyPosition;body.localRotation=bodyRotation;}}
  void LateUpdate(){
   UpdateWeaponHold();UpdateInfectedPose();
   if(Quadruped&&body&&!HitReactionActive){float bite=Mathf.Sin(Mathf.Clamp01((attackUntil-Time.time)/.3f)*Mathf.PI);float bound=Mathf.Max(0,movement)*.055f*(1-Mathf.Cos(gaitPhase));body.localPosition=bodyPosition+Vector3.forward*bite*.18f+Vector3.up*bound;body.localRotation=Quaternion.AngleAxis(Mathf.Sin(gaitPhase)*movement*4,Vector3.right)*bodyRotation;}
   if(!HitReactionActive)return;
   float t=Mathf.Clamp01((Time.time-hitAt)/.18f),pulse=Mathf.Sin(t*Mathf.PI);
   if(body){body.localPosition=bodyPosition+recoil*(pulse*.21f);body.localRotation=bodyRotation*Quaternion.AngleAxis(pulse*18,Vector3.Cross(Vector3.up,recoil));}
   foreach(var s in surfaces){float flash=Mathf.Clamp01(1-(Time.time-hitAt)/.13f);s.flash.SetColor(BaseColor,Color.Lerp(s.color,new Color(1,.98f,.72f,s.color.a),flash*(MobilePreferences.Current.reducedFlash?.25f:.85f)));s.renderer.SetPropertyBlock(s.flash,s.index);}
   if(t>=.94f)ClearHit();
  }
  // Preserve the purchased aiming pose. Attach the gun to its animated trigger hand,
  // and adapt only the support arm to the current weapon's foregrip.
  void UpdateWeaponHold(){
   if(!rifle||rolling||!animator||!animator.isHuman)return;
   var gun=equippedWeapon==WeaponKind.Launcher?launcher:equippedWeapon==WeaponKind.Shotgun?shotgun:rifle;if(!gun)return;
   var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);var elbow=animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);if(!hand||!elbow)return;
   gun.localRotation=Quaternion.identity;gun.localScale=Vector3.one*1.1f;
   gun.localPosition=transform.InverseTransformPoint(hand.position)-new Vector3(0,.05f,-.16f)*1.1f;
   var support=equippedWeapon==WeaponKind.Shotgun?new Vector3(-.02f,.12f,.13f):new Vector3(-.02f,.12f,.10f);
   FitArm(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,gun.TransformPoint(support),elbow.position);
  }
  void FitArm(HumanBodyBones upperId,HumanBodyBones lowerId,HumanBodyBones handId,Vector3 target,Vector3 elbowHint){
   var upper=animator.GetBoneTransform(upperId);var lower=animator.GetBoneTransform(lowerId);var hand=animator.GetBoneTransform(handId);if(!upper||!lower||!hand)return;
   Quaternion wrist=hand.rotation;
   float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,hand.position);
   var axis=(target-upper.position).normalized;float distance=Mathf.Clamp(Vector3.Distance(upper.position,target),Mathf.Abs(a-b)+.001f,a+b-.001f);
   var bend=Vector3.ProjectOnPlane(elbowHint-upper.position,axis).normalized;
   float along=(a*a-b*b+distance*distance)/(2*distance),height=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
   var elbow=upper.position+axis*along+bend*height;
   upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
   lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,target-lower.position)*lower.rotation;
   // Keep the purchased palm/curl orientation rather than pointing the fingers along the barrel.
   hand.rotation=wrist;
  }
  void Update(){RestoreGait();if(hitAt>-1&&Time.time-hitAt>=.18f)ClearHit();}
  bool gaitApplied;
  void RestoreGait(){if(!gaitApplied||gaitBones==null)return;for(int i=0;i<gaitBones.Length;i++)if(gaitBones[i])gaitBones[i].localRotation=gaitOriginal[i];gaitApplied=false;}
  void UpdateInfectedPose(){if(!infected||gaitBones==null||!animator||!animator.enabled||Time.time<attackUntil)return;
   for(int i=0;i<gaitBones.Length;i++)if(gaitBones[i])gaitOriginal[i]=gaitBones[i].localRotation;gaitApplied=true;
   float sway=Mathf.Sin(gaitPhase)*movement,lean=enemyKind==EnemyKind.Runner?20:enemyKind==EnemyKind.Brute?13:16;
   var spine=gaitBones[0];if(spine)spine.rotation=Quaternion.AngleAxis(lean,transform.right)*Quaternion.AngleAxis(sway*(enemyKind==EnemyKind.Brute?2.5f:4),transform.forward)*spine.rotation;
   if(gaitBones[1])gaitBones[1].rotation=Quaternion.AngleAxis(-7,transform.right)*Quaternion.AngleAxis(5+sway*2,transform.forward)*gaitBones[1].rotation;
   // Fit the upper arms toward a low asymmetric reach, retaining the purchased leg motion.
   PoseArm(2,4,HumanBodyBones.LeftHand,-.2f,Mathf.Lerp(-.32f,1.15f,eliteWindup),Mathf.Lerp(.72f,.25f,eliteWindup),Mathf.Lerp(.6f,1,eliteWindup));
   PoseArm(3,5,HumanBodyBones.RightHand,.25f,Mathf.Lerp(-.52f,1.15f,eliteWindup),Mathf.Lerp(.52f,.25f,eliteWindup),Mathf.Lerp(.65f,1,eliteWindup));
  }
  void PoseArm(int upper,int lower,HumanBodyBones handId,float x,float y,float z,float weight){var a=gaitBones[upper];var elbow=gaitBones[lower];var hand=animator.GetBoneTransform(handId);if(!a||!elbow||!hand)return;var dir=transform.TransformDirection(new Vector3(x,y,z)).normalized;a.rotation=Quaternion.Slerp(a.rotation,Quaternion.FromToRotation(elbow.position-a.position,dir)*a.rotation,weight);var fore=transform.TransformDirection(new Vector3(x*.3f,y*.3f,z)).normalized;elbow.rotation=Quaternion.Slerp(elbow.rotation,Quaternion.FromToRotation(hand.position-elbow.position,fore)*elbow.rotation,.6f);}
  public void ResetPose(){RestoreGait();ClearHit();if(animator){animator.Rebind();animator.Update(0);}}
  public void Shoot(){if(gunParticles!=null)for(int i=0;i<gunParticles.Length;i++){var p=gunParticles[i];p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);if(equippedWeapon==WeaponKind.Launcher&&launcherMuzzleColors!=null){var main=p.main;main.startColor=launcherMuzzleColors[i]*(MobilePreferences.Current.reducedFlash?.42f:1);}p.Play(false);}}
 }
}
