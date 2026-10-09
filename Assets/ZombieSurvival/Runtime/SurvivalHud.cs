using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace DeadDistrict {
 public sealed partial class SurvivalHud : MonoBehaviour {
  SurvivalGame game;RectTransform safe;Text stats,ammo,status,tips,deathStats,pauseLabel;Image healthFill,hit;GameObject death,pause;
  RectTransform topHud,pauseButton;readonly Vector3[] corners=new Vector3[4];
  Button interactButton;ActionButtonView interactView,grenadeView,weaponView,rollView;
  Button grenadeButton,weaponButton,rollButton,reloadButton;ActionButtonView reloadView;float nextRefresh;Rect lastSafe;
  static Color Ink=new Color(.055f,.09f,.11f,.9f), Cyan=new Color(.4f,.86f,.91f), Warm=new Color(1,.7f,.34f);
  Font font;Sprite circle;Text musicLabel,soundLabel,pickupToast,supplyName;
  public void Bind(SurvivalGame owner) {
   game=owner;font=Resources.Load<Font>("DeadDistrict/Chinese");if(!font)throw new System.InvalidOperationException("Chinese font missing");
   var tex=new Texture2D(96,96,TextureFormat.RGBA32,false);var pixels=new Color[96*96];for(int y=0;y<96;y++)for(int x=0;x<96;x++){float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(48,48));pixels[y*96+x]=new Color(1,1,1,Mathf.Clamp01(48-d));}tex.SetPixels(pixels);tex.Apply();circle=Sprite.Create(tex,new Rect(0,0,96,96),new Vector2(.5f,.5f));
   var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
   var scale=gameObject.AddComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1600,900);scale.matchWidthOrHeight=.5f;
   gameObject.AddComponent<GraphicRaycaster>();
   if(!FindFirstObjectByType<EventSystem>()){var e=new GameObject("EventSystem");e.AddComponent<EventSystem>();e.AddComponent<StandaloneInputModule>();}
   hit=Panel("Damage vignette",transform,new Color(.8f,.13f,.06f,0));Stretch(hit.rectTransform);hit.raycastTarget=false;
   safe=new GameObject("Safe area",typeof(RectTransform)).GetComponent<RectTransform>();safe.SetParent(transform,false);UpdateSafeArea();
   var top=Panel("HUD",safe,new Color(.035f,.075f,.09f,.55f));top.raycastTarget=false;topHud=top.rectTransform;Rect(topHud,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-20),new Vector2(288,85));
   stats=Label("Stats",top.transform,"",20,new Vector2(14,-5),new Vector2(132,36),Color.white);
   Label("Health caption",top.transform,"生命",13,new Vector2(14,-39),new Vector2(36,24),Cyan);
   var back=Panel("Health track",top.transform,new Color(.25f,.32f,.34f,.65f));Rect(back.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(54,-49),new Vector2(220,7));back.raycastTarget=false;
   healthFill=Panel("Health",back.transform,Cyan);Stretch(healthFill.rectTransform);healthFill.raycastTarget=false;
   pickupToast=Label("Pickup toast",safe,"",23,new Vector2(0,110),new Vector2(850,40),Warm,new Vector2(.5f,0));pickupToast.alignment=TextAnchor.MiddleCenter;
   supplyName=Label("Nearest supply",safe,"",18,Vector2.zero,new Vector2(400,44),Color.white,new Vector2(.5f,.5f));supplyName.alignment=TextAnchor.MiddleCenter;
   ammo=Label("Rifle",safe,"",20,new Vector2(-30,-32),new Vector2(410,42),Warm,new Vector2(1,1));ammo.alignment=TextAnchor.MiddleRight;
   status=Label("Director",safe,"",24,new Vector2(0,-30),new Vector2(550,70),Color.white,new Vector2(.5f,1));status.alignment=TextAnchor.MiddleCenter;
   tips=Label("Help",safe,Application.isMobilePlatform?"保持移动，突破包围\n左侧移动 · 右下翻滚 / 手雷 · 上方切枪 / 互动":"保持移动，突破包围\nWASD 移动 · Shift 翻滚 · Q 换枪 · G 投雷 · ESC 暂停",18,new Vector2(0,25),new Vector2(660,64),new Color(.75f,.83f,.85f),new Vector2(.5f,0));tips.alignment=TextAnchor.MiddleCenter;
   floatingZone=Panel("Floating movement area",safe,Color.clear);floatingZone.rectTransform.anchorMin=Vector2.zero;floatingZone.rectTransform.anchorMax=new Vector2(.44f,.64f);floatingZone.rectTransform.offsetMin=floatingZone.rectTransform.offsetMax=Vector2.zero;
   var joy=Panel("Movement joystick",safe,new Color(.1f,.19f,.22f,.7f));Rect(joy.rectTransform,new Vector2(0,0),new Vector2(.5f,.5f),new Vector2(142,150),new Vector2(190,190));
   joy.sprite=circle;var input=joy.gameObject.AddComponent<SurvivalInput>();var knob=Panel("Knob",joy.transform,new Color(.4f,.86f,.91f,.8f));Rect(knob.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(65,65));knob.sprite=circle;knob.raycastTarget=false;input.knob=knob.rectTransform;movementInput=input;floatingZone.gameObject.AddComponent<FloatingJoystickPad>().input=input;
   Label("Joystick caption",safe,"移动",16,new Vector2(56,38),new Vector2(190,24),Cyan,new Vector2(0,0)).alignment=TextAnchor.MiddleCenter;
   grenadeButton=IconButton("Grenade",new Vector2(-120,135),112,()=>{},out grenadeView);grenadeButton.gameObject.AddComponent<GrenadeAimInput>().Bind(game);
   weaponButton=IconButton("Switch weapon",new Vector2(-120,267),112,()=>game.SwitchWeapon(),out weaponView);
   rollButton=IconButton("Dodge",new Vector2(-252,135),112,()=>game.TryRoll(),out rollView);
   reloadButton=IconButton("Reload",new Vector2(-252,267),112,()=>game.TryReload(),out reloadView);
   interactButton=IconButton("Interact",new Vector2(-384,267),112,()=>game.World.Interact(),out interactView);interactButton.gameObject.SetActive(false);
   pauseButton=MakeButton("Pause",safe,"暂停",new Vector2(-75,-100),new Vector2(90,44),new Vector2(1,1),()=>game.TogglePause()).GetComponent<RectTransform>();
   death=Overlay("Death");Label("Title",death.transform,"你已被感染者包围",42,new Vector2(0,110),new Vector2(850,60),Warm,new Vector2(.5f,.5f)).alignment=TextAnchor.MiddleCenter;
   deathStats=Label("Result",death.transform,"",28,new Vector2(0,15),new Vector2(850,100),Color.white,new Vector2(.5f,.5f));deathStats.alignment=TextAnchor.MiddleCenter;
   MakeButton("Restart",death.transform,Application.isMobilePlatform?"重新开始":"重新开始 [R]",new Vector2(0,-105),new Vector2(310,70),new Vector2(.5f,.5f),()=>game.Restart());death.SetActive(false);
   pause=Overlay("Pause overlay");pauseLabel=Label("Paused",pause.transform,"已暂停",42,new Vector2(0,75),new Vector2(500,70),Color.white,new Vector2(.5f,.5f));pauseLabel.alignment=TextAnchor.MiddleCenter;
   MakeButton("Resume",pause.transform,"继续游戏",new Vector2(0,-35),new Vector2(270,70),new Vector2(.5f,.5f),()=>game.TogglePause());var music=MakeButton("Music",pause.transform,"",new Vector2(-170,-135),new Vector2(260,62),new Vector2(.5f,.5f),()=>{game.Sound.ToggleMusic();UpdateSoundLabels();});musicLabel=music.GetComponentInChildren<Text>();
   var sfx=MakeButton("Sound",pause.transform,"",new Vector2(170,-135),new Vector2(260,62),new Vector2(.5f,.5f),()=>{game.Sound.ToggleSfx();UpdateSoundLabels();});soundLabel=sfx.GetComponentInChildren<Text>();UpdateSoundLabels();pause.SetActive(false);BindGrowth();BindWaveAnnouncement();BindMobileShell();
  }
  GameObject Overlay(string name){var p=Panel(name,transform,new Color(.025f,.055f,.07f,.94f));Stretch(p.rectTransform);return p.gameObject;}
  Image Panel(string name,Transform parent,Color color){var o=new GameObject(name,typeof(RectTransform),typeof(Image));o.transform.SetParent(parent,false);var image=o.GetComponent<Image>();image.color=color;return image;}
  Text Label(string name,Transform parent,string value,int size,Vector2 pos,Vector2 dimensions,Color color,Vector2? anchor=null){
   var o=new GameObject(name,typeof(RectTransform),typeof(Text));o.transform.SetParent(parent,false);var t=o.GetComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.raycastTarget=false;
   Vector2 a=anchor??new Vector2(0,1);Rect(t.rectTransform,a,a.x==.5f?new Vector2(.5f,a.y):a,pos,dimensions);return t;
  }
  Button IconButton(string name,Vector2 pos,float diameter,UnityEngine.Events.UnityAction action,out ActionButtonView view){var p=Panel(name,safe,Ink);Rect(p.rectTransform,new Vector2(1,0),new Vector2(.5f,.5f),pos,Vector2.one*diameter);var b=p.gameObject.AddComponent<Button>();b.onClick.AddListener(action);view=p.gameObject.AddComponent<ActionButtonView>();view.Initialize(b,circle,font);return b;}
  Button MakeButton(string name,Transform parent,string value,Vector2 pos,Vector2 size,Vector2 anchor,UnityEngine.Events.UnityAction action) {
   var p=Panel(name,parent,new Color(.17f,.3f,.33f,.96f));Rect(p.rectTransform,anchor,new Vector2(.5f,.5f),pos,size);
   var b=p.gameObject.AddComponent<Button>();b.onClick.AddListener(action);var text=Label("Caption",p.transform,value,22,Vector2.zero,size,Color.white,new Vector2(.5f,.5f));text.alignment=TextAnchor.MiddleCenter;return b;
  }
  static void Rect(RectTransform r,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size){r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=pos;r.sizeDelta=size;}
  static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
  void UpdateSafeArea(){lastSafe=Screen.safeArea;safe.anchorMin=lastSafe.position/new Vector2(Screen.width,Screen.height);safe.anchorMax=(lastSafe.position+lastSafe.size)/new Vector2(Screen.width,Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;}
  public void Refresh() {
   if(Screen.safeArea!=lastSafe)UpdateSafeArea();
   // Action progress follows the render frame; text and other HUD data stay throttled.
   reloadView.Refresh(ActionSymbol.Reload,game.CanReload,progress:game.Reloading?game.ReloadProgress:1,active:game.Reloading);
   rollView.Refresh(ActionSymbol.Roll,game.CanRoll,"",1-game.RollCooldownRemaining/game.config.rollCooldown,game.Rolling);
   if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.1f;
   RefreshGrowth();RefreshWaveAnnouncement();RefreshChapterHud();
   pickupToast.text=Time.time<game.PickupMessageUntil?game.PickupMessage:"";
   var nearby=game.World.Nearest();interactButton.gameObject.SetActive(nearby!=null);
   if(nearby){var symbol=nearby.Kind==WorldPropKind.Door?(nearby.Open?ActionSymbol.DoorClosed:ActionSymbol.DoorOpen):nearby.Kind==WorldPropKind.Explosive?ActionSymbol.Explosive:ActionSymbol.Crate;interactView.Refresh(symbol,!game.Rolling&&!game.IsGrenadeAiming&&!game.Paused&&!game.Dead&&!nearby.Moving);}
   weaponView.Refresh(game.NextWeaponSymbol,game.HasAlternateWeapon&&!game.Dead&&!game.Paused&&!game.Rolling&&!game.IsGrenadeAiming,locked:!game.HasAlternateWeapon);
   stats.text=$"击杀 {game.Kills}";
   healthFill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(game.PlayerHealth.CurrentHealth/game.PlayerHealth.MaximumHealth),1);
   ammo.text=game.Reloading?$"换弹中  {Mathf.RoundToInt(game.ReloadProgress*100)}%":$"{game.WeaponName} {game.Ammo:00} / {game.MagazineCapacity}  ·  自动射击";
   status.text=waveAnnouncement.activeSelf?"":$"第 {game.Wave} / {game.Chapter.Count} 波";status.color=Color.white;
   grenadeView.Refresh(ActionSymbol.Grenade,game.Grenades>0&&!game.Dead&&!game.Paused&&!game.Rolling&&!game.GrenadeInFlight,game.Grenades.ToString(),active:game.IsGrenadeAiming);
   tips.text=game.IsGrenadeAiming?"拖动控制方向和距离 · 拉回起点取消":"";
  }
  public bool TargetCovered(Vector3 feet,Vector3 head){var a=Camera.main.WorldToScreenPoint(feet);var b=Camera.main.WorldToScreenPoint(head);float padding=Screen.height*.35f/(Camera.main.orthographicSize*2);var bounds=UnityEngine.Rect.MinMaxRect(Mathf.Min(a.x,b.x)-padding,Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x)+padding,Mathf.Max(a.y,b.y));return (growthPointButton&&growthPointButton.gameObject.activeSelf&&Covered(growthPointButton.GetComponent<RectTransform>(),bounds))||(interactButton&&interactButton.gameObject.activeSelf&&Covered(interactButton.GetComponent<RectTransform>(),bounds))||Covered(reloadButton.GetComponent<RectTransform>(),bounds)||Covered(topHud,bounds)||Covered(pauseButton,bounds)||Covered(grenadeButton.GetComponent<RectTransform>(),bounds)||Covered(weaponButton.GetComponent<RectTransform>(),bounds)||Covered(rollButton.GetComponent<RectTransform>(),bounds);}
  bool Covered(RectTransform panel,UnityEngine.Rect bounds){panel.GetWorldCorners(corners);var a=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var b=RectTransformUtility.WorldToScreenPoint(null,corners[2]);return UnityEngine.Rect.MinMaxRect(a.x,a.y,b.x,b.y).Overlaps(bounds);}
  void OnDestroy(){MobilePreferences.Changed-=ApplyMobilePreferences;MobilePreferences.Flush();if(circle){Destroy(circle.texture);Destroy(circle);}}
  public void SetHitAlpha(float alpha){if(hit){Color c=hit.color;c.a=alpha;hit.color=c;}}
  public void ShowDeath(){HideGrowth();Refresh();deathStats.text=$"存活 {Mathf.FloorToInt(game.Elapsed)} 秒   ·   击杀 {game.Kills}\n挑战至第 {game.Wave} / {game.Chapter.Count} 波   ·   等级 {game.Growth.Level}";death.SetActive(true);}
  void UpdateSoundLabels(){musicLabel.text="音乐："+(game.Sound.MusicEnabled?"开启":"关闭");soundLabel.text="音效："+(game.Sound.SfxEnabled?"开启":"关闭");}
  public void ShowPause(bool value){if(value)ShowShell("pause");else CloseShell();}
 }
}
