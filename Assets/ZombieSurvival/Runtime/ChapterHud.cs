using UnityEngine;
using UnityEngine.UI;
namespace DeadDistrict {
 public sealed partial class SurvivalHud {
  GameObject victoryPage,bossPanel,chaptersPage;Button nextChapterButton;Button[] chapterButtons;Text[] chapterLabels;Text victoryStats,bossCaption;Image bossFill;
  static string RunTime(float seconds)=>Mathf.FloorToInt(seconds/60)+" 分 "+Mathf.FloorToInt(seconds%60)+" 秒";
  void BindChapterHud(){
   victoryPage=ShellPage("Chapter victory");Title(victoryPage.transform,game.Chapter.VictoryTitle,"第 "+game.Chapter.number+" 关 · "+game.Chapter.title+" · "+game.Chapter.Count+" 波挑战完成");
   victoryStats=ShellText("Victory stats",victoryPage.transform,"",new Vector2(0,70),new Vector2(1050,170),28);victoryStats.lineSpacing=1.5f;
   ShellText("Victory encouragement",victoryPage.transform,"你突破了尸潮，击败了"+game.Chapter.eliteName,new Vector2(0,-65),new Vector2(1000,45),22,new Color(.65f,.8f,.8f));
   ShellButton("Replay chapter",victoryPage.transform,"再挑战一次",new Vector2(-200,-190),new Vector2(360,68),()=>game.Restart(),true);
   ShellButton("Victory home",victoryPage.transform,"返回主菜单",new Vector2(200,-190),new Vector2(360,68),OpenHome);
   BindChapterSelection();
   nextChapterButton=ShellButton("Next chapter",victoryPage.transform,"前往下一关",new Vector2(0,-280),new Vector2(430,60),()=>game.BeginChapter(game.Chapter.number),true);
   var panel=Panel("Elite health",safe,new Color(.03f,.05f,.065f,.8f));bossPanel=panel.gameObject;panel.raycastTarget=false;
   Rect(panel.rectTransform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-100),new Vector2(390,54));
   bossCaption=Label("Elite caption",panel.transform,game.Chapter.eliteName,20,new Vector2(0,8),new Vector2(380,30),Warm,new Vector2(.5f,.5f));bossCaption.alignment=TextAnchor.MiddleCenter;
   var track=Panel("Elite health track",panel.transform,new Color(.19f,.1f,.08f));track.raycastTarget=false;Rect(track.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-15),new Vector2(360,7));
   bossFill=Panel("Elite health fill",track.transform,new Color(.94f,.4f,.17f));bossFill.raycastTarget=false;Stretch(bossFill.rectTransform);bossPanel.SetActive(false);victoryPage.SetActive(false);
  }
  void RefreshChapterHud(){if(!bossPanel)return;var e=game.Elite;bool visible=e&&e.Alive&&e.IsElite&&!game.Dead&&!game.Completed&&!game.Paused;bossPanel.SetActive(visible);if(!visible)return;
   bossFill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(e.Health.CurrentHealth/e.Health.MaximumHealth),1);
   bossCaption.text=game.Chapter.eliteName+" · "+e.EliteCaption;
  }
  void BindChapterSelection(){
   requestedChapter=game.Chapter.number-1;
   ShellButton("Choose chapter",homePage.transform,"选择关卡",new Vector2(0,144),new Vector2(430,68),()=>ShowShell("chapters"));
   chaptersPage=ShellPage("Chapter selection");Title(chaptersPage.transform,"选择关卡","通关后解锁下一关 · 每关独立成长");chapterButtons=new Button[ChapterCatalog.Count];chapterLabels=new Text[ChapterCatalog.Count];
   for(int i=0;i<ChapterCatalog.Count;i++){int index=i;var c=ChapterCatalog.Get(i);var panel=Panel("Chapter card "+i,chaptersPage.transform,new Color(.075f,.14f,.18f,.95f));panel.raycastTarget=false;Rect(panel.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2((i-(ChapterCatalog.Count-1)*.5f)*400,0),new Vector2(380,330));
    ShellText("Chapter name "+i,panel.transform,"第 "+c.number+" 关\n"+c.title,new Vector2(0,104),new Vector2(350,85),28,Warm);
    ShellText("Chapter description "+i,panel.transform,c.Description,new Vector2(0,25),new Vector2(350,75),22);
    chapterLabels[i]=ShellText("Chapter progress "+i,panel.transform,"",new Vector2(0,-42),new Vector2(350,50),20,Cyan);
    chapterButtons[i]=ShellButton("Select chapter "+i,panel.transform,"进入关卡",new Vector2(0,-112),new Vector2(310,62),()=>RequestChapter(index),true);
   }
   ShellButton("Chapter selection back",chaptersPage.transform,"返回主菜单",new Vector2(0,-280),new Vector2(430,60),OpenHome);chaptersPage.SetActive(false);
  }
  void RefreshChapterSelection(){for(int i=0;i<ChapterCatalog.Count;i++){var c=ChapterCatalog.Get(i);bool unlocked=ChapterCatalog.Unlocked(i);var record=ChapterProgress.Read(c.id);chapterButtons[i].interactable=unlocked;chapterButtons[i].GetComponentInChildren<Text>().text=unlocked?"进入关卡":"尚未解锁";chapterLabels[i].text=!unlocked?"通关上一关后解锁":record.clears>0?"已通关 · 最快 "+RunTime(record.bestSeconds):c.Count+" 波挑战 · 尚未通关";}}
  public void ShowVictory(){HideGrowth();nextChapterButton.gameObject.SetActive(ChapterCatalog.Unlocked(game.Chapter.number));var r=ChapterProgress.Read(game.Chapter.id);victoryStats.text="击杀 "+game.Kills+"   ·   等级 "+game.Growth.Level+"\n通关用时  "+RunTime(game.Elapsed)+(r.clears>0?"\n最快纪录  "+RunTime(r.bestSeconds):"");ShowShell("victory");}
 }
}
