using MoreMountains.TopDownEngine;
using UnityEngine;
using System.Collections.Generic;
namespace DeadDistrict {
 // Use the purchased v4.2 health/damage contract, with immediate pool-safe death.
 public sealed class SurvivalHealth : Health {
  float armor,armorUntil,dodgeUntil,growthUntil;
  public bool GrowthProtected=>Time.time<growthUntil;
  public void BeginGrowthProtection(float duration){growthUntil=Time.time+duration;}
  public bool DodgeInvulnerable=>Time.time<dodgeUntil;
  public void BeginDodge(float duration){dodgeUntil=Time.time+duration;}
  public void ClearDodge(){dodgeUntil=0;}
  public float Armor {get{ExpireArmor();return armor;}}
  public float ArmorSeconds=>Armor>0?Mathf.Max(0,armorUntil-Time.time):0;
  public void GrantArmor(){armor=Mathf.Max(Armor,25);armorUntil=Time.time+12;}
  public void ClearArmor(){armor=0;armorUntil=0;}
  void ExpireArmor(){if(Time.time>=armorUntil)ClearArmor();}
  public float Heal(float amount){float before=CurrentHealth;SetHealth(Mathf.Clamp(CurrentHealth+amount,0,MaximumHealth));return CurrentHealth-before;}
  public override void Damage(float damage,GameObject instigator,float flickerDuration,float invincibilityDuration,Vector3 damageDirection,List<TypedDamage> typedDamages=null){
   if(DodgeInvulnerable||GrowthProtected||!CanTakeDamageThisFrame())return;
   float absorbed=Mathf.Min(Armor,Mathf.Max(0,damage));armor-=absorbed;
   // Base Damage also handles a fully absorbed hit, preserving the existing invulnerability window.
   base.Damage(damage-absorbed,instigator,flickerDuration,invincibilityDuration,damageDirection,typedDamages);
  }
  public void Configure(float value) {
   InitialHealth=MaximumHealth=value; DestroyOnDeath=false;
   DisableModelOnDeath=false; DisableControllerOnDeath=false; DisableCollisionsOnDeath=false;
   ResetHealthOnEnable=true; RespawnAtInitialLocation=false;
  }
  public override void Kill() { ClearArmor();ClearDodge();growthUntil=0;if(CurrentHealth>0) SetHealth(0); OnDeath?.Invoke(); }
  public void ResetForSpawn() { ClearArmor();ClearDodge();growthUntil=0;LastDamage=0;LastDamageDirection=Vector3.zero;DamageEnabled(); ResetHealthToMaxHealth(); }
 }
}
