using UnityEngine;
using UnityEngine.UI;
namespace DeadDistrict {
 public sealed partial class SurvivalHud {
  GameObject growthPanel;Text experienceLabel,growthTitle,growthHint,growthSummary;Image experienceFill;Button growthPointButton;Text growthPointText;RectTransform growthContent;ActionGlyph growthRing;
  readonly Button[] growthCards=new Button[3];readonly Text[] cardTitles=new Text[3],cardDescriptions=new Text[3],cardRanks=new Text[3];readonly ActionGlyph[] cardIcons=new ActionGlyph[3];
  void BindGrowth(){
   experienceLabel=Label("Experience",topHud,"",20,new Vector2(148,-5),new Vector2(126,36),Warm);experienceLabel.alignment=TextAnchor.UpperRight;
   Label("Experience caption",topHud,"经验",13,new Vector2(14,-56),new Vector2(36,24),Warm);
   var track=Panel("Experience track",topHud,new Color(.25f,.27f,.23f,.65f));Rect(track.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(54,-66),new Vector2(220,5));track.raycastTarget=false;experienceFill=Panel("Experience fill",track.transform,Warm);Stretch(experienceFill.rectTransform);experienceFill.raycastTarget=false;
   var point=Panel("Spend growth points",safe,Ink);Rect(point.rectTransform,new Vector2(1,1),new Vector2(.5f,.5f),new Vector2(-86,-211),new Vector2(112,112));point.sprite=circle;
   growthPointButton=point.gameObject.AddComponent<Button>();growthPointButton.targetGraphic=point;growthPointButton.onClick.AddListener(()=>game.Growth.TryOpen());
   var pointColors=growthPointButton.colors;pointColors.pressedColor=new Color(.4f,.75f,.85f);pointColors.disabledColor=new Color(.6f,.6f,.6f,.7f);growthPointButton.colors=pointColors;
   var border=new GameObject("Growth point ring",typeof(RectTransform),typeof(CanvasRenderer),typeof(ActionGlyph));border.transform.SetParent(point.transform,false);Stretch(border.GetComponent<RectTransform>());growthRing=border.GetComponent<ActionGlyph>();growthRing.raycastTarget=false;growthRing.Set(ActionSymbol.Ring,Warm);
   var pointArt=new GameObject("Growth point icon",typeof(RectTransform),typeof(CanvasRenderer),typeof(ActionGlyph));pointArt.transform.SetParent(point.transform,false);Rect(pointArt.GetComponent<RectTransform>(),new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,9),new Vector2(64,64));var glyph=pointArt.GetComponent<ActionGlyph>();glyph.raycastTarget=false;glyph.Set(ActionSymbol.Upgrade,Warm);
   var badge=Panel("Growth point count plate",point.transform,new Color(.045f,.085f,.11f,.98f));badge.sprite=circle;badge.raycastTarget=false;Rect(badge.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-32),new Vector2(40,24));
   growthPointText=Label("Growth point count",badge.transform,"",18,Vector2.zero,new Vector2(40,24),Warm,new Vector2(.5f,.5f));growthPointText.alignment=TextAnchor.MiddleCenter;growthPointText.fontStyle=FontStyle.Bold;growthPointText.resizeTextForBestFit=true;growthPointText.resizeTextMinSize=12;growthPointText.resizeTextMaxSize=18;

   growthSummary=Label("Run upgrades",pause.transform,"",19,new Vector2(0,-265),new Vector2(1060,120),Cyan,new Vector2(.5f,.5f));growthSummary.alignment=TextAnchor.MiddleCenter;
   growthPanel=Overlay("Upgrade choices");growthContent=new GameObject("Growth safe content",typeof(RectTransform)).GetComponent<RectTransform>();growthContent.SetParent(growthPanel.transform,false);Rect(growthContent,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(1200,700));growthTitle=Label("Upgrade title",growthContent,"",40,new Vector2(0,295),new Vector2(1100,70),Warm,new Vector2(.5f,.5f));growthTitle.alignment=TextAnchor.MiddleCenter;
   growthHint=Label("Upgrade help",growthContent,"选择一项强化 · 战斗已暂停",22,new Vector2(0,235),new Vector2(1100,50),Color.white,new Vector2(.5f,.5f));growthHint.alignment=TextAnchor.MiddleCenter;
   for(int i=0;i<3;i++){
    var card=Panel("Upgrade card "+i,growthContent,new Color(.10f,.19f,.23f));Rect(card.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2((i-1)*378,-18),new Vector2(352,412));
    var b=card.gameObject.AddComponent<Button>();b.targetGraphic=card;growthCards[i]=b;var colors=b.colors;colors.highlightedColor=new Color(1.2f,1.3f,1.4f);colors.pressedColor=new Color(.55f,.82f,.9f);colors.disabledColor=Color.white;b.colors=colors;
    cardRanks[i]=Label("Upgrade rank",card.transform,"",16,new Vector2(22,-15),new Vector2(308,30),Cyan);
    var art=new GameObject("Upgrade icon",typeof(RectTransform),typeof(CanvasRenderer),typeof(ActionGlyph));art.transform.SetParent(card.transform,false);var r=art.GetComponent<RectTransform>();Rect(r,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-47),new Vector2(92,92));cardIcons[i]=art.GetComponent<ActionGlyph>();cardIcons[i].raycastTarget=false;
    cardTitles[i]=Label("Upgrade name",card.transform,"",27,new Vector2(0,-146),new Vector2(310,46),Color.white,new Vector2(.5f,1));cardTitles[i].alignment=TextAnchor.MiddleCenter;
    cardDescriptions[i]=Label("Upgrade details",card.transform,"",20,new Vector2(23,-203),new Vector2(306,132),new Color(.77f,.86f,.87f));cardDescriptions[i].alignment=TextAnchor.UpperCenter;
    var select=Panel("Choose highlight",card.transform,new Color(.17f,.37f,.40f));Rect(select.rectTransform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,20),new Vector2(308,48));select.raycastTarget=false;
    var text=Label("Choose caption",select.transform,Application.isMobilePlatform?"选择强化":"选择强化 ["+(i+1)+"]",20,Vector2.zero,new Vector2(308,48),Color.white,new Vector2(.5f,.5f));text.alignment=TextAnchor.MiddleCenter;
   }
   var footer=Label("Upgrade duration",growthContent,"每次消耗 1 点 · 未使用的强化点会保留",19,new Vector2(0,-245),new Vector2(1100,60),Cyan,new Vector2(.5f,.5f));footer.alignment=TextAnchor.MiddleCenter;MakeButton("Defer growth",growthContent,"返回战斗",new Vector2(0,-310),new Vector2(350,60),new Vector2(.5f,.5f),()=>game.Growth.Defer());growthPanel.SetActive(false);RefreshGrowth();
  }
  public void RefreshGrowth(){if(!experienceLabel||game.Growth==null)return;var g=game.Growth;experienceLabel.text=$"等级 {g.Level}";experienceFill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01((float)g.Experience/g.Required),1);growthSummary.text=g.Summary();growthPointButton.gameObject.SetActive(g.Points>0&&!game.Dead&&!game.Completed&&!g.Choosing);growthPointButton.interactable=!game.Paused&&!game.Dead;growthPointText.text=g.Points.ToString();}
  public void ShowGrowth(int offerId){nextRefresh=0;Refresh();var g=game.Growth;growthPanel.SetActive(true);growthPanel.transform.SetAsLastSibling();LayoutGrowth();growthTitle.text="选择强化 · 剩余 "+g.Points+" 点";for(int i=0;i<3;i++){var k=g.Offers[i];int index=i;cardTitles[i].text=RunGrowth.Title(k);cardDescriptions[i].text=g.Description(k);cardRanks[i].text="强化 "+(g.Rank(k)+1)+(RunGrowth.Cap(k)>0?" / "+RunGrowth.Cap(k):"");cardIcons[i].Set(RunGrowth.Symbol(k),Warm);growthCards[i].onClick.RemoveAllListeners();growthCards[i].onClick.AddListener(()=>g.Choose(index,offerId));growthCards[i].interactable=false;}RefreshGrowth();}
  public void RefreshGrowthChoice(){if(!growthPanel)return;for(int i=0;i<3;i++)growthCards[i].interactable=game.Growth.ChoiceReady&&!game.Dead;}
  void LayoutGrowth(){if(!growthContent)return;var canvas=GetComponent<Canvas>();float factor=canvas.scaleFactor;var area=Screen.safeArea;growthContent.position=new Vector3(area.center.x,area.center.y,0);growthContent.localScale=Vector3.one*Mathf.Min(1,Mathf.Min(area.width/factor/1240,area.height/factor/740));}
  void TickGrowthButton(){
   if(growthPointButton&&growthPointButton.gameObject.activeSelf){
    // A slow continuous border pulse signals available points without flashing the whole HUD.
    float pulse=.5f-.5f*Mathf.Cos(Time.unscaledTime*Mathf.PI);
    bool reduced=MobilePreferences.Current.reducedFlash;
    float alpha=reduced?Mathf.Lerp(.5f,.65f,pulse):Mathf.Lerp(.3f,1,pulse);
    var tint=game.Growth.OpenRequested?Cyan:Warm;growthRing.Set(ActionSymbol.Ring,new Color(tint.r,tint.g,tint.b,alpha));
    growthPointButton.image.color=Color.Lerp(new Color(.12f,.27f,.29f,.96f),new Color(.23f,.34f,.27f,.96f),pulse*(reduced?.15f:.5f));
   }
   if(growthPanel&&growthPanel.activeSelf)LayoutGrowth();
  }
  public void HideGrowth(){if(growthPanel)growthPanel.SetActive(false);}
 }
}
