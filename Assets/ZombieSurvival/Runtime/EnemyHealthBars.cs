using UnityEngine;
using UnityEngine.UI;
namespace DeadDistrict {
 // A fixed screen-space pool follows visible bodies without revealing enemies through walls.
 public sealed class EnemyHealthBars : MonoBehaviour {
  sealed class Bar {public RectTransform root;public Image fill,chip;public Text kind;public int spawn;public float shown=1;public bool visible;}
  Bar[] bars;SurvivalGame game;Camera cam;RectTransform canvasRect;float nextVisibility;
  public int VisibleCount {get;private set;}
  public float FillFor(int slot)=>bars[slot].fill.rectTransform.anchorMax.x;
  public bool VisibleFor(int slot)=>bars[slot].root.gameObject.activeSelf;
  public void Initialize(SurvivalGame owner){
   game=owner;cam=Camera.main;var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=-1;
   var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
   canvasRect=GetComponent<RectTransform>();bars=new Bar[game.Enemies.Count];
   for(int i=0;i<bars.Length;i++){
    var background=Image("Zombie health "+i,transform,new Color(.035f,.055f,.035f,.94f));var r=background.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(54,8);
    var chip=Image("Recent damage",r,new Color(1,.75f,.24f));var fill=Image("Remaining health",r,new Color(.85f,.22f,.13f));
    foreach(var image in new[]{chip,fill}){image.rectTransform.anchorMin=Vector2.zero;image.rectTransform.anchorMax=Vector2.one;image.rectTransform.offsetMin=new Vector2(2,2);image.rectTransform.offsetMax=new Vector2(-2,-2);}
    var label=new GameObject("Infected kind",typeof(RectTransform),typeof(Text));label.transform.SetParent(r,false);var text=label.GetComponent<Text>();text.font=Resources.Load<Font>("DeadDistrict/Chinese");text.fontSize=13;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(.5f,1);text.rectTransform.pivot=new Vector2(.5f,0);text.rectTransform.sizeDelta=new Vector2(100,19);text.rectTransform.anchoredPosition=new Vector2(0,3);
    bars[i]=new Bar{root=r,fill=fill,chip=chip,kind=text};r.gameObject.SetActive(false);
   }
  }
  static Image Image(string name,Transform parent,Color color){var o=new GameObject(name,typeof(RectTransform),typeof(Image));o.transform.SetParent(parent,false);var image=o.GetComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
  void LateUpdate(){
   if(bars==null)return;bool check=Time.unscaledTime>=nextVisibility;if(check)nextVisibility=Time.unscaledTime+.1f;VisibleCount=0;
   for(int i=0;i<bars.Length;i++){
    var z=game.Enemies[i];var b=bars[i];
    if(!z.Alive||game.Dead||game.Paused){b.visible=false;b.root.gameObject.SetActive(false);continue;}
    Vector3 body=z.AimPoint,head=z.transform.position+Vector3.up*(z.VisualHeight+.2f);var view=cam.WorldToViewportPoint(head);
    if(check||b.spawn!=z.SpawnCount)b.visible=view.z>0&&view.x>.025f&&view.x<.975f&&view.y>.08f&&view.y<.92f&&!Physics.Linecast(cam.transform.position,body,1<<8)&&(!game.hud||!game.hud.TargetCovered(z.transform.position,head));
    float fraction=Mathf.Clamp01(z.Health.CurrentHealth/z.Health.MaximumHealth);
    if(b.spawn!=z.SpawnCount){b.spawn=z.SpawnCount;b.shown=fraction;
     b.root.sizeDelta=new Vector2(z.IsElite?92:z.Kind==EnemyKind.Brute?72:54,8);b.fill.color=z.Kind==EnemyKind.Brute?new Color(.76f,.46f,1):z.Kind==EnemyKind.Runner?new Color(1,.62f,.18f):new Color(.85f,.22f,.13f);b.kind.color=b.fill.color;b.kind.text=z.IsElite?game.Chapter.eliteName:z.Kind==EnemyKind.Normal?"":z.Kind==EnemyKind.Brute?"重装":"感染犬";
    }
    b.shown=Mathf.MoveTowards(b.shown,fraction,Time.deltaTime*1.8f);
    b.fill.rectTransform.anchorMax=new Vector2(fraction,1);b.chip.rectTransform.anchorMax=new Vector2(b.shown,1);
    b.root.gameObject.SetActive(b.visible);if(!b.visible)continue;VisibleCount++;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,cam.WorldToScreenPoint(head),null,out var local);b.root.anchoredPosition=local;
   }
  }
 }
}
