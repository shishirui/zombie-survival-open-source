using UnityEngine;
using UnityEngine.UI;
namespace DeadDistrict {
 public sealed partial class SurvivalHud {
  GameObject waveAnnouncement;Text waveTitle,waveSubtitle;
  void BindWaveAnnouncement(){
   var panel=Panel("Wave announcement",safe,new Color(.035f,.075f,.09f,.88f));waveAnnouncement=panel.gameObject;panel.raycastTarget=false;
   Rect(panel.rectTransform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-18),new Vector2(560,92));
   var edge=Panel("Wave accent",panel.transform,Warm);edge.raycastTarget=false;Rect(edge.rectTransform,new Vector2(0,.5f),new Vector2(0,.5f),Vector2.zero,new Vector2(5,92));
   waveTitle=Label("Wave title",panel.transform,"",30,new Vector2(0,14),new Vector2(540,44),Warm,new Vector2(.5f,.5f));waveTitle.alignment=TextAnchor.MiddleCenter;
   waveSubtitle=Label("Wave subtitle",panel.transform,"",19,new Vector2(0,-25),new Vector2(540,30),Color.white,new Vector2(.5f,.5f));waveSubtitle.alignment=TextAnchor.MiddleCenter;waveAnnouncement.SetActive(false);
  }
  void RefreshWaveAnnouncement(){
   if(!waveAnnouncement)return;
   float left=game.NextHorde-game.Elapsed;bool waiting=game.Wave<game.Chapter.Count&&left>=0&&left<=SurvivalGame.WaveBreakSeconds+.05f;
   waveAnnouncement.SetActive(!game.Dead&&!game.Paused&&waiting);
   if(waiting){var next=game.Chapter.waves[game.Wave];waveTitle.text="第 "+(game.Wave+1)+" 波即将来袭";waveSubtitle.text=next.warning+" · "+Mathf.CeilToInt(left)+" 秒后开始";}
  }
 }
}
