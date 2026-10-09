using System;
using System.Linq;
using UnityEngine;
namespace DeadDistrict {
 public sealed partial class SurvivalGame {
  static bool startAfterReload;
  public bool RunStarted {get;private set;}
  void InitializeMobileFlow(){
   bool validation=RuntimeLaunch.Arguments().Any(a=>a!="-mobile-ux-smoke"&&(a.EndsWith("-smoke")||a.EndsWith("-capture")));
   if(validation||startAfterReload){startAfterReload=false;RunStarted=true;if(!CombatValidation)StartChapter();}
   else {PauseForShell();hud.OpenHome();}
  }
  public void PauseForShell(){
   EndGrenadeAim(false);CancelDodge();foreach(var input in FindObjectsByType<SurvivalInput>(FindObjectsSortMode.None))input.Clear();
   SurvivalInput.Move=Vector2.zero;SurvivalInput.GrenadeRequested=false;Paused=true;Time.timeScale=0;if(Sound)Sound.Pause(true);
  }
  public void BeginRun(){
   if(RunStarted||Dead){Restart();return;}
   RunStarted=true;Paused=false;Time.timeScale=1;Sound.Pause(false);hud.CloseShell();
   nextTrickle=Time.time+.5f;nextGrenade=Time.time+config.grenadeRecharge;if(!CombatValidation)StartChapter();
  }
  public void ResumeRun(){if(!RunStarted||Dead||Completed)return;Paused=false;Time.timeScale=1;Sound.Pause(false);SurvivalInput.Move=Vector2.zero;hud.CloseShell();}
 }
}
