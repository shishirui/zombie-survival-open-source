using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace DeadDistrict {
 public sealed partial class SurvivalHud {
  GameObject shell;RectTransform shellSafe,shellContent;string shellPage="",returnPage="home",helpReturn="home";
  GameObject homePage,settingsPage,helpPage,pausePage,confirmPage,upgradesPage;Button continueRun;
  Slider musicSlider,sfxSlider;Text musicValue,sfxValue;Button[] joystickOptions,sizeOptions,shakeOptions,flashOptions;
  readonly List<RectTransform> shellLayouts=new List<RectTransform>();
  SurvivalInput movementInput;Image floatingZone;bool firstHelp;
  public string MobilePage=>shellPage;
  Text ShellText(string name,Transform parent,string text,Vector2 pos,Vector2 size,int fontSize=24,Color? tint=null){var t=Label(name,parent,text,fontSize,pos,size,tint??Color.white,new Vector2(.5f,.5f));t.alignment=TextAnchor.MiddleCenter;return t;}
  Button ShellButton(string name,Transform parent,string text,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action,bool primary=false){var b=MakeButton(name,parent,text,pos,size,new Vector2(.5f,.5f),action);b.GetComponent<Image>().color=primary?new Color(.14f,.46f,.5f,1):new Color(.13f,.21f,.25f,1);return b;}
  GameObject ShellPage(string name){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(shellSafe,false);Rect(r,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(1240,700));shellLayouts.Add(r);return r.gameObject;}
  void Title(Transform parent,string title,string subtitle){ShellText(title+" title",parent,title,new Vector2(0,280),new Vector2(1100,70),38,Warm);ShellText(title+" subtitle",parent,subtitle,new Vector2(0,225),new Vector2(1120,40),21,new Color(.66f,.79f,.82f));}
  void BindMobileShell(){
   var bg=Panel("Mobile shell",transform,new Color(.025f,.052f,.07f,.97f));Stretch(bg.rectTransform);shell=bg.gameObject;
   shellSafe=new GameObject("Menu safe area",typeof(RectTransform)).GetComponent<RectTransform>();shellSafe.SetParent(shell.transform,false);
   homePage=ShellPage("Home page");
   continueRun=ShellButton("Continue current run",homePage.transform,"继续本局",new Vector2(0,59),new Vector2(430,68),()=>game.ResumeRun(),true);
   ShellButton("Start new run",homePage.transform,"开始新的一局",new Vector2(0,-26),new Vector2(430,68),RequestNewRun,true);
   ShellButton("Home settings",homePage.transform,"设置",Vector2.zero,new Vector2(430,68),()=>OpenSettings("home"));
   pausePage=ShellPage("Mobile pause page");Title(pausePage.transform,"已暂停","准备好后继续战斗");
   ShellButton("Mobile resume",pausePage.transform,"继续游戏",new Vector2(0,100),new Vector2(430,68),()=>game.ResumeRun(),true);
   ShellButton("Pause settings",pausePage.transform,"设置",new Vector2(-111,10),new Vector2(208,60),()=>OpenSettings("pause"));
   ShellButton("Pause help",pausePage.transform,"操作说明",new Vector2(111,10),new Vector2(208,60),()=>OpenHelp("pause"));
   ShellButton("Pause home",pausePage.transform,"返回主菜单",new Vector2(0,-165),new Vector2(430,60),OpenHome);
   ShellText("Run retained",pausePage.transform,"本局保留，返回菜单后可以继续",new Vector2(0,-250),new Vector2(900,40),20,new Color(.65f,.77f,.8f));
   ShellButton("Pause upgrades",pausePage.transform,"本局强化",new Vector2(0,-70),new Vector2(430,60),()=>{RefreshGrowth();ShowShell("upgrades");});
   upgradesPage=ShellPage("Run upgrades page");Title(upgradesPage.transform,"本局强化","当前升级仅在这一局有效");growthSummary.transform.SetParent(upgradesPage.transform,false);Rect(growthSummary.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,0),new Vector2(1080,370));growthSummary.fontSize=25;growthSummary.lineSpacing=1.5f;ShellButton("Upgrades back",upgradesPage.transform,"返回",new Vector2(0,-305),new Vector2(430,60),()=>ShowShell("pause"));
   BuildSettingsPage();BuildHelpPage();BindChapterHud();
   confirmPage=ShellPage("Confirm new run");Title(confirmPage.transform,"开始新的一局？","当前这一局的战斗进度将结束");
   ShellButton("Keep current run",confirmPage.transform,"保留本局",new Vector2(-180,-20),new Vector2(320,70),OpenHome,true);
   ShellButton("Confirm restart",confirmPage.transform,"开始新局",new Vector2(180,-20),new Vector2(320,70),()=>{CloseShell();BeginRequestedChapter();});
   ShellButton("Death home",death.transform,"返回主菜单",new Vector2(0,-195),new Vector2(310,60),OpenHome);
   MobilePreferences.Changed+=ApplyMobilePreferences;ApplyMobilePreferences();shell.SetActive(false);UpdateMenuSafeArea();
  }
  void BuildSettingsPage(){
   settingsPage=ShellPage("Settings page");var p=settingsPage.transform;Title(p,"设置","按你的习惯调整，设置会自动保存");
   musicValue=ShellText("Music value",p,"",new Vector2(-315,160),new Vector2(550,42),24);
   musicSlider=VolumeSlider("Music volume",p,new Vector2(-315,110),v=>{var d=MobilePreferences.Current;d.music=v;MobilePreferences.Set(d);});
   sfxValue=ShellText("Sound value",p,"",new Vector2(-315,30),new Vector2(550,42),24);
   sfxSlider=VolumeSlider("Sound volume",p,new Vector2(-315,-20),v=>{var d=MobilePreferences.Current;d.sfx=v;MobilePreferences.Set(d);});
   ShellText("Shake label",p,"镜头震动",new Vector2(-315,-100),new Vector2(550,42));
   shakeOptions=Choices("Shake",p,new Vector2(-315,-150),new[]{"关闭","轻微","标准"},i=>{var d=MobilePreferences.Current;d.shake=i*.5f;MobilePreferences.Set(d);});
   ShellText("Joystick label",p,"移动摇杆",new Vector2(315,160),new Vector2(550,42));
   joystickOptions=Choices("Joystick",p,new Vector2(315,110),new[]{"固定","浮动"},i=>{var d=MobilePreferences.Current;d.joystick=i;MobilePreferences.Set(d);});
   ShellText("Button size label",p,"操作按钮大小",new Vector2(315,30),new Vector2(550,42));
   sizeOptions=Choices("Button size",p,new Vector2(315,-20),new[]{"紧凑","标准","放大"},i=>{var d=MobilePreferences.Current;d.buttons=i;MobilePreferences.Set(d);});
   ShellText("Flash label",p,"战斗闪光",new Vector2(315,-100),new Vector2(550,42));
   flashOptions=Choices("Flash",p,new Vector2(315,-150),new[]{"标准","减弱"},i=>{var d=MobilePreferences.Current;d.reducedFlash=i==1;MobilePreferences.Set(d);});
   ShellText("Comfort note",p,"减弱闪光保留攻击范围与危险提示",new Vector2(0,-230),new Vector2(1100,38),20,new Color(.61f,.76f,.79f));
   ShellButton("Settings back",p,"返回",new Vector2(-425,-305),new Vector2(290,58),()=>{MobilePreferences.Flush();ShowShell(returnPage);});
   ShellButton("Reset mobile settings",p,"恢复默认",new Vector2(0,-305),new Vector2(290,58),()=>MobilePreferences.Reset());
   ShellButton("Settings help",p,"操作说明",new Vector2(425,-305),new Vector2(290,58),()=>OpenHelp("settings"));
  }
  Button[] Choices(string name,Transform parent,Vector2 center,string[] titles,Action<int> choose){var buttons=new Button[titles.Length];float width=titles.Length==2?230:150;for(int i=0;i<titles.Length;i++){int index=i;buttons[i]=ShellButton(name+" "+i,parent,titles[i],center+Vector2.right*((i-(titles.Length-1)*.5f)*(width+12)),new Vector2(width,56),()=>choose(index));}return buttons;}
  Slider VolumeSlider(string name,Transform parent,Vector2 pos,UnityEngine.Events.UnityAction<float> change){
   var bg=Panel(name,parent,new Color(1,1,1,.015f));Rect(bg.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),pos,new Vector2(490,56));var slider=bg.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;
   var track=Panel("Track",bg.transform,new Color(.2f,.3f,.34f));track.raycastTarget=false;Rect(track.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(470,8));
   var fill=Panel("Fill",track.transform,Cyan);fill.raycastTarget=false;Stretch(fill.rectTransform);slider.fillRect=fill.rectTransform;
   var area=new GameObject("Handle area",typeof(RectTransform)).GetComponent<RectTransform>();area.SetParent(bg.transform,false);Rect(area,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(470,56));
   var handle=Panel("Handle",area,Cyan);handle.sprite=circle;handle.raycastTarget=false;Rect(handle.rectTransform,new Vector2(0,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(30,-26));slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.direction=Slider.Direction.LeftToRight;slider.onValueChanged.AddListener(change);return slider;
  }
  void BuildHelpPage(){
   helpPage=ShellPage("Help page");var p=helpPage.transform;Title(p,"操作说明","熟悉这些操作，就可以开始探索");
   HelpCard(p,"移动与射击","左侧摇杆移动\n发现可攻击的敌人后自动开火\n弹匣打空后自动换弹",new Vector2(-310,90));
   HelpCard(p,"手雷","按住右下手雷并拖动\n调整方向与距离，松手投掷\n拉回按下的位置可以取消",new Vector2(310,90));
   HelpCard(p,"翻滚与换弹","翻滚用于躲避包围\n换弹按钮可以提前补满弹匣\n圆环恢复后就能再次使用",new Vector2(-310,-115));
   HelpCard(p,"切枪与设置","获得新武器后可以切枪\n暂停菜单可以调整声音和操作\n探索地图，寻找更多生存机会",new Vector2(310,-115));
   var viewport=Panel("Help viewport",p,new Color(1,1,1,.001f));Rect(viewport.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-35),new Vector2(1200,470));viewport.gameObject.AddComponent<RectMask2D>();
   var content=new GameObject("Help document",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(viewport.transform,false);content.anchorMin=content.anchorMax=content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(1200,1400);content.anchoredPosition=Vector2.zero;
   foreach(string cardName in new[]{"移动与射击 card","手雷 card","翻滚与换弹 card","切枪与设置 card"}){var card=p.Find(cardName).GetComponent<RectTransform>();var pos=card.anchoredPosition;card.SetParent(content,false);Rect(card,new Vector2(.5f,1),new Vector2(.5f,.5f),pos-Vector2.up*200,new Vector2(560,175));}
   var license=Resources.Load<TextAsset>("DeadDistrict/ActionIcons/Attribution");
   var notes=ShellText("Manual resource notes",content,"图标资源许可\n\n"+license.text,Vector2.zero,new Vector2(1120,940),20,new Color(.59f,.72f,.75f));notes.alignment=TextAnchor.UpperLeft;notes.lineSpacing=1.15f;Rect(notes.rectTransform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-490),new Vector2(1120,940));float noteHeight=notes.preferredHeight;notes.rectTransform.sizeDelta=new Vector2(1120,noteHeight);content.sizeDelta=new Vector2(1200,490+noteHeight+24);
   var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport.rectTransform;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;scroll.decelerationRate=.1f;
   ShellButton("Help back",p,"返回",new Vector2(0,-305),new Vector2(430,60),()=>{if(firstHelp){firstHelp=false;var d=MobilePreferences.Current;d.helpSeen=true;MobilePreferences.Set(d);MobilePreferences.Flush();BeginRequestedChapter();}else ShowShell(helpReturn);},true);
  }
  void HelpCard(Transform parent,string title,string description,Vector2 center){var card=Panel(title+" card",parent,new Color(.09f,.15f,.19f,.9f));card.raycastTarget=false;Rect(card.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),center,new Vector2(560,175));ShellText(title+" heading",card.transform,title,new Vector2(0,55),new Vector2(520,40),25,Cyan);ShellText(title+" description",card.transform,description,new Vector2(0,-22),new Vector2(520,105),23);}
  void RequestNewRun(){RequestChapter(game.Chapter.number-1);}
  int requestedChapter;
  void BeginRequestedChapter(){game.BeginChapter(requestedChapter);}
  void RequestChapter(int index){
   if(!ChapterCatalog.Unlocked(index))return;requestedChapter=index;
   if(game.RunStarted&&!game.Dead&&!game.Completed){ShowShell("confirm");return;}
   if(!MobilePreferences.Current.helpSeen){OpenHelp("home");firstHelp=true;helpPage.transform.Find("Help back").GetComponentInChildren<Text>().text="跳过说明，开始游戏";}else BeginRequestedChapter();
  }

  public void OpenHome(){game.PauseForShell();ShowShell("home");}
  public void OpenSettings(string from){returnPage=from;ShowShell("settings");}
  void OpenHelp(string from){var scroll=helpPage.GetComponentInChildren<ScrollRect>(true);scroll.StopMovement();scroll.verticalNormalizedPosition=1;helpReturn=from;firstHelp=false;helpPage.transform.Find("Help back").GetComponentInChildren<Text>().text="返回";ShowShell("help");}
  void ShowShell(string page){shellPage=page;shell.SetActive(true);safe.gameObject.SetActive(false);pause.SetActive(false);death.SetActive(false);homePage.SetActive(page=="home");pausePage.SetActive(page=="pause");settingsPage.SetActive(page=="settings");helpPage.SetActive(page=="help");confirmPage.SetActive(page=="confirm");upgradesPage.SetActive(page=="upgrades");victoryPage.SetActive(page=="victory");chaptersPage.SetActive(page=="chapters");if(page=="chapters")RefreshChapterSelection();continueRun.gameObject.SetActive(game.RunStarted&&!game.Dead&&!game.Completed);LayoutHome();ApplyMobilePreferences();UpdateMenuSafeArea();}
  public void CloseShell(){MobilePreferences.Flush();shellPage="";shell.SetActive(false);safe.gameObject.SetActive(true);death.SetActive(false);pause.SetActive(false);Refresh();}
  void LayoutHome(){
   var rows=continueRun.gameObject.activeSelf?new[]{"Choose chapter","Continue current run","Start new run","Home settings"}:new[]{"Choose chapter","Start new run","Home settings"};
   for(int i=0;i<rows.Length;i++){var r=homePage.transform.Find(rows[i]).GetComponent<RectTransform>();r.anchoredPosition=new Vector2(0,((rows.Length-1)*.5f-i)*88);r.sizeDelta=new Vector2(430,68);}
  }
  public bool HandleShellBack(){if(!shell||!shell.activeSelf)return false;if(shellPage=="settings"){MobilePreferences.Flush();ShowShell(returnPage);}else if(shellPage=="help"){firstHelp=false;ShowShell(helpReturn);}else if(shellPage=="upgrades")ShowShell("pause");else if(shellPage=="chapters"||shellPage=="victory"||shellPage=="confirm")ShowShell("home");else if(shellPage=="pause")game.ResumeRun();return true;}
  void Select(Button[] buttons,int selected){if(buttons==null)return;for(int i=0;i<buttons.Length;i++){buttons[i].GetComponent<Image>().color=i==selected?new Color(.17f,.49f,.52f):new Color(.13f,.21f,.25f);buttons[i].GetComponentInChildren<Text>().color=i==selected?Color.white:new Color(.68f,.79f,.82f);}}
  void ApplyMobilePreferences(){
   var d=MobilePreferences.Current;if(musicSlider){musicSlider.SetValueWithoutNotify(d.music);sfxSlider.SetValueWithoutNotify(d.sfx);musicValue.text="音乐音量   "+Mathf.RoundToInt(d.music*100)+"%";sfxValue.text="音效音量   "+Mathf.RoundToInt(d.sfx*100)+"%";Select(joystickOptions,d.joystick);Select(sizeOptions,d.buttons);Select(shakeOptions,Mathf.RoundToInt(d.shake*2));Select(flashOptions,d.reducedFlash?1:0);}
   if(movementInput){movementInput.SetFloating(d.joystick==1);floatingZone.raycastTarget=d.joystick==1;}
   float scale=MobilePreferences.ButtonScale,diameter=112*scale,x=64+diameter*.5f,y=79+diameter*.5f,gap=diameter+20;
   PlaceAction(grenadeButton,new Vector2(-x,y),scale);PlaceAction(rollButton,new Vector2(-x-gap,y),scale);PlaceAction(weaponButton,new Vector2(-x,y+gap),scale);PlaceAction(reloadButton,new Vector2(-x-gap,y+gap),scale);PlaceAction(interactButton,new Vector2(-x-gap*2,y+gap),scale);
  }
  void PlaceAction(Button button,Vector2 pos,float scale){if(!button)return;var r=button.GetComponent<RectTransform>();r.anchoredPosition=pos;r.localScale=Vector3.one*scale;}
  void UpdateMenuSafeArea(){if(!shellSafe)return;var area=Screen.safeArea;shellSafe.anchorMin=area.position/new Vector2(Screen.width,Screen.height);shellSafe.anchorMax=(area.position+area.size)/new Vector2(Screen.width,Screen.height);shellSafe.offsetMin=shellSafe.offsetMax=Vector2.zero;Canvas.ForceUpdateCanvases();float scale=Mathf.Min(1,shellSafe.rect.width/1280,shellSafe.rect.height/740);foreach(var r in shellLayouts)r.localScale=Vector3.one*scale;}
  void Update(){MobilePreferences.Tick();TickGrowthButton();if(Screen.safeArea!=lastSafe){UpdateSafeArea();UpdateMenuSafeArea();}}
  void OnApplicationPause(bool paused){if(paused)MobilePreferences.Flush();}
 }
}
