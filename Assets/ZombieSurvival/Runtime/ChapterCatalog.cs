using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeadDistrict {
 public static class ChapterCatalog {
  public static readonly string[] ScenePaths={"Assets/ZombieSurvival/Scenes/DeadDistrict.unity","Assets/ZombieSurvival/Scenes/QuarantineCamp.unity","Assets/ZombieSurvival/Scenes/FreightDepot.unity"};
  static readonly string[] ResourcesNames={"CommercialStreet","QuarantineCamp","FreightDepot"};
  public static int Count=>ResourcesNames.Length;
  public static ChapterDefinition Get(int index)=>index>=0&&index<Count?Resources.Load<ChapterDefinition>("DeadDistrict/"+ResourcesNames[index]):null;
  public static ChapterDefinition Current(){for(int i=0;i<Count;i++){var c=Get(i);if(c&&c.sceneName==SceneManager.GetActiveScene().name)return c;}throw new System.InvalidOperationException("No chapter for current scene");}
  public static bool Unlocked(int index)=>index>=0&&index<Count&&(index==0||ChapterProgress.Read(Get(index-1).id).clears>0);
 }
 public sealed partial class SurvivalGame {
  public bool BeginChapter(int index){
   if(!ChapterCatalog.Unlocked(index))return false;var next=ChapterCatalog.Get(index);
   if(next==Chapter&&!RunStarted&&!Dead){BeginRun();return true;}
   Growth.Cancel();EndGrenadeAim(false);CancelDodge();SurvivalInput.Move=Vector2.zero;SurvivalInput.GrenadeRequested=false;MobilePreferences.Flush();startAfterReload=true;Time.timeScale=1;
   SceneManager.LoadScene(next.sceneName);return true;
  }
 }
}
