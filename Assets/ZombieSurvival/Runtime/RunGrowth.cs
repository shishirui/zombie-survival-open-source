using System;
using System.Collections.Generic;
using UnityEngine;
namespace DeadDistrict {
 public enum UpgradeKind { Damage,FireRate,Reload,Vitality,Speed,Dodge,Grenade,Scatter,Medical }
 public sealed class RunGrowth {
  readonly SurvivalGame game;readonly int[] ranks=new int[9];readonly List<UpgradeKind> offers=new List<UpgradeKind>();readonly System.Random random=new System.Random();
  public bool Enabled=true;
  public bool Choosing {get;private set;}
  public int Level {get;private set;}=1;
  public int Experience {get;private set;}
  public int TotalExperience {get;private set;}
  public int Required=>Mathf.Min(2400,300+(Level-1)*150);
  public int OfferId {get;private set;}
  float readyAt;bool requested,appliedThisVisit;
  public int Points {get;private set;}
  public bool OpenRequested=>requested;
  public bool ChoiceReady=>Choosing&&Time.unscaledTime>=readyAt;
  public IReadOnlyList<UpgradeKind> Offers=>offers;
  public int Rank(UpgradeKind kind)=>ranks[(int)kind];
  public RunGrowth(SurvivalGame owner){game=owner;Enabled=!game.AutoTest&&!game.CombatValidation;}
  public static int Reward(EnemyKind kind)=>kind==EnemyKind.Brute?35:kind==EnemyKind.Runner?15:10;
  public void Award(EnemyKind kind){if(!Enabled||game.Dead)return;int amount=Reward(kind);Experience+=amount;TotalExperience+=amount;bool gained=false;while(Experience>=Required){Experience-=Required;Level++;Points++;gained=true;}if(gained)game.GrowthEffects?.Earn();}
  public static int Cap(UpgradeKind kind){switch(kind){case UpgradeKind.FireRate:case UpgradeKind.Reload:return 5;case UpgradeKind.Speed:case UpgradeKind.Dodge:case UpgradeKind.Grenade:return 4;case UpgradeKind.Scatter:return 3;default:return 0;}}
  public bool Eligible(UpgradeKind kind)=> (Cap(kind)==0||Rank(kind)<Cap(kind))&&(kind!=UpgradeKind.Scatter||game.ShotgunUnlocked);
  // Opening is exclusively player-requested. Earning XP never pauses combat.
  public bool TryOpen(){if(!Enabled||Choosing||game.Dead||game.Paused||Points<=0)return false;requested=true;TickPending();return true;}
  public void TickPending(){if(!requested||game.Paused||game.Dead)return;if(game.GrowthActionBusy)return;requested=false;appliedThisVisit=false;game.SetGrowthPause(true);OpenOffer();}
  void OpenOffer(){Choosing=true;if(offers.Count==0){var pool=new List<UpgradeKind>();foreach(UpgradeKind k in Enum.GetValues(typeof(UpgradeKind)))if(Eligible(k))pool.Add(k);for(int i=0;i<3;i++){int n=random.Next(pool.Count);offers.Add(pool[n]);pool.RemoveAt(n);}}OfferId++;readyAt=Time.unscaledTime+.3f;game.hud.ShowGrowth(OfferId);}
  public void TickChoice(){game.hud.RefreshGrowthChoice();if(Input.GetKeyDown(KeyCode.Escape)){Defer();return;}if(!ChoiceReady)return;for(int i=0;i<3;i++)if(Input.GetKeyDown(KeyCode.Alpha1+i)){Choose(i,OfferId);break;}}
  public bool Choose(int index,int offerId){if(!ChoiceReady||game.Dead||Points<=0||offerId!=OfferId||index<0||index>=offers.Count)return false;var kind=offers[index];if(!Eligible(kind))return false;Points--;ranks[(int)kind]++;Apply(kind);appliedThisVisit=true;game.Sound.Upgrade();offers.Clear();
   if(Points>0)OpenOffer();else Defer();game.hud.RefreshGrowth();return true;
  }
  public void Defer(){if(!Choosing)return;Choosing=false;game.hud.HideGrowth();game.SetGrowthPause(false);if(appliedThisVisit){game.PlayerHealth.BeginGrowthProtection(1);game.GrowthEffects?.Apply();}appliedThisVisit=false;game.hud.RefreshGrowth();}
  void Apply(UpgradeKind kind){var c=game.config;int r=Rank(kind);switch(kind){
   case UpgradeKind.Damage:float damage=(1+.2f*r)/(1+.2f*(r-1));c.bulletDamage*=damage;c.shotgunPelletDamage*=damage;c.launcherDamage*=damage;break;
   case UpgradeKind.FireRate:float rate=(1+.12f*r)/(1+.12f*(r-1));c.roundsPerSecond*=rate;c.shotgunRoundsPerSecond*=rate;c.launcherRoundsPerSecond*=rate;break;
   case UpgradeKind.Reload:float reload=(1-.1f*r)/(1-.1f*(r-1));c.reloadSeconds*=reload;c.shotgunReloadSeconds*=reload;c.launcherReloadSeconds*=reload;break;
   case UpgradeKind.Vitality:IncreaseHealth(20,20);break;
   case UpgradeKind.Medical:IncreaseHealth(5,35);break;
   case UpgradeKind.Speed:c.moveSpeed*=(1+.08f*r)/(1+.08f*(r-1));break;
   case UpgradeKind.Dodge:c.rollCooldown*=(1-.12f*r)/(1-.12f*(r-1));break;
   case UpgradeKind.Grenade:c.grenadeDamage*=(1+.25f*r)/(1+.25f*(r-1));c.grenadeRadius+=.35f;break;
   case UpgradeKind.Scatter:c.shotgunPellets+=2;break;
  }}
  void IncreaseHealth(float capacity,float heal){game.config.playerHealth+=capacity;game.PlayerHealth.MaximumHealth=game.config.playerHealth;game.PlayerHealth.Heal(heal);}
  public void Cancel(){Choosing=false;requested=false;offers.Clear();if(game.hud)game.hud.HideGrowth();}
  public static string Title(UpgradeKind k){switch(k){case UpgradeKind.Damage:return "强化弹药";case UpgradeKind.FireRate:return "高速击发";case UpgradeKind.Reload:return "快速装填";case UpgradeKind.Vitality:return "强健体魄";case UpgradeKind.Speed:return "轻盈步伐";case UpgradeKind.Dodge:return "敏捷翻滚";case UpgradeKind.Grenade:return "高爆手雷";case UpgradeKind.Scatter:return "密集弹幕";default:return "应急医疗";}}
  public string Description(UpgradeKind k){int next=Rank(k)+1;var c=game.config;switch(k){
   case UpgradeKind.Damage:return "所有枪械伤害\n+20% 基础伤害\n累计强化 +"+(next*20)+"%";
   case UpgradeKind.FireRate:return "所有枪械的射速\n+12% 基础射速\n累计强化 +"+(next*12)+"%";
   case UpgradeKind.Reload:return "所有枪械换弹更快\n-10% 基础换弹时间\n累计缩短 "+(next*10)+"%";
   case UpgradeKind.Vitality:return "生命上限 +20\n立即恢复 20 生命\n上限 "+Mathf.RoundToInt(c.playerHealth)+" → "+Mathf.RoundToInt(c.playerHealth+20);
   case UpgradeKind.Speed:return "移动速度\n+8% 基础速度\n累计强化 +"+(next*8)+"%";
   case UpgradeKind.Dodge:return "翻滚冷却更短\n-12% 基础冷却\n累计缩短 "+(next*12)+"%";
   case UpgradeKind.Grenade:return "手雷伤害 +25% 基础伤害\n爆炸半径 +0.35 米\n半径 "+(c.grenadeRadius+.35f).ToString("0.00")+" 米";
   case UpgradeKind.Scatter:return "每发霰弹额外 +2 弹丸\n弹药消耗仍为 1 发\n弹丸 "+c.shotgunPellets+" → "+(c.shotgunPellets+2);
   default:return "立即恢复 35 生命\n生命上限 +5\n仅在本局有效";
  }}
  public static ActionSymbol Symbol(UpgradeKind k){switch(k){case UpgradeKind.FireRate:case UpgradeKind.Damage:case UpgradeKind.Reload:return ActionSymbol.Rifle;case UpgradeKind.Scatter:return ActionSymbol.Shotgun;case UpgradeKind.Grenade:return ActionSymbol.Grenade;case UpgradeKind.Speed:case UpgradeKind.Dodge:return ActionSymbol.Roll;default:return ActionSymbol.Health;}}
  public string Summary(){var lines=new List<string>();foreach(UpgradeKind k in Enum.GetValues(typeof(UpgradeKind)))if(Rank(k)>0)lines.Add(Title(k)+" ×"+Rank(k));return lines.Count==0?"积攒强化点 · 点击右上方入口选择强化":string.Join("   ·   ",lines);}
 }
 public sealed partial class SurvivalGame {
  public bool GrowthActionBusy=>Rolling||IsGrenadeAiming||GrenadeInFlight||Time.time<growthActionUntil;
  float growthActionUntil;
  public PlayerGrowthEffects GrowthEffects {get;private set;}
  public void SetGrowthPause(bool value){EndGrenadeAim(false);CancelDodge();foreach(var input in FindObjectsByType<SurvivalInput>(FindObjectsSortMode.None))input.Clear();foreach(var pad in FindObjectsByType<GrenadeAimInput>(FindObjectsSortMode.None))pad.Cancel();SurvivalInput.GrenadeRequested=false;Paused=value;Time.timeScale=value?0:1;Sound.Pause(value);}
 }
}
