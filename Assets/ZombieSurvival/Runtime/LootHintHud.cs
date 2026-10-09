using UnityEngine;
namespace DeadDistrict {
 public struct LootHint {
  public WeaponKind weaponKind;
  public GameObject source;public Vector3 position;public SupplyKind kind;public bool weapon;
  public float Range=>weapon?5.5f:4.5f;
  public Vector3 Anchor=>position+Vector3.up*(weapon?1.55f:1.35f);
 }
 public sealed partial class SurvivalHud {
  readonly LootHint[] lootHints=new LootHint[9];GameObject hintSource,pendingHint;float pendingHintSince,hintLastVisible;
  GameObject captionSource;SupplyKind captionKind;bool captionWeapon;WeaponKind captionWeaponKind;
  bool HintInView(LootHint hint,Camera camera,bool retain){
   float range=hint.Range+(retain?.8f:0);if((hint.position-game.PlayerPosition).sqrMagnitude>range*range)return false;
   var view=camera.WorldToViewportPoint(hint.position);float x=retain?.075f:.1f,y=retain?.17f:.2f;
   return view.z>camera.nearClipPlane&&view.x>x&&view.x<1-x&&view.y>y&&view.y<1-y;
  }
  bool HintClear(LootHint hint)=>!Physics.Linecast(game.PlayerPosition+Vector3.up*.65f,hint.position+Vector3.up*.65f,1<<8);
  // Called after the game's final camera movement, every rendered frame, separately from the 10 Hz counters.
  public void RefreshLootHint(Camera camera){
   if(!supplyName||!game.World)return;
   if(game.Dead||game.Paused||game.Growth.Choosing){supplyName.enabled=false;pendingHint=null;return;}
   if(Screen.safeArea!=lastSafe)UpdateSafeArea();
   int count=game.Supplies.AppendHints(lootHints,0);count=game.WeaponCrates.AppendHints(lootHints,count);
   int current=-1,best=-1;float bestDistance=float.MaxValue;
   for(int i=0;i<count;i++){
    var hint=lootHints[i];bool clear=HintClear(hint);
    if(hint.source==hintSource&&HintInView(hint,camera,true)){
     if(clear)hintLastVisible=Time.time;
     // A tiny line-of-sight grace avoids flickering at a cover edge; removed items never get this grace.
     if(clear||Time.time-hintLastVisible<.12f)current=i;
    }
    if(!clear||!HintInView(hint,camera,false))continue;
    float distance=Vector3.Distance(hint.position,game.PlayerPosition);if(distance<bestDistance){best=i;bestDistance=distance;}
   }
   int chosen=current;
   if(current<0){chosen=best;pendingHint=null;}
   else if(best>=0&&best!=current&&bestDistance+.7f<Vector3.Distance(lootHints[current].position,game.PlayerPosition)){
    if(pendingHint!=lootHints[best].source){pendingHint=lootHints[best].source;pendingHintSince=Time.time;}
    else if(Time.time-pendingHintSince>=.18f){chosen=best;pendingHint=null;}
   }else pendingHint=null;
   if(chosen<0){hintSource=null;captionSource=null;supplyName.enabled=false;return;}
   var selected=lootHints[chosen];if(hintSource!=selected.source)hintLastVisible=Time.time;hintSource=selected.source;
   if(captionSource!=selected.source||captionKind!=selected.kind||captionWeapon!=selected.weapon||captionWeaponKind!=selected.weaponKind){
    string text=selected.weapon?(selected.weaponKind==WeaponKind.Launcher?"榴弹发射器箱":"霰弹枪箱"):SurvivalSupplies.Name(selected.kind);
    if(supplyName.text!=text)supplyName.text=text;captionSource=selected.source;captionKind=selected.kind;captionWeapon=selected.weapon;captionWeaponKind=selected.weaponKind;
   }
   var point=camera.WorldToScreenPoint(selected.Anchor);point.x=Mathf.Round(point.x);point.y=Mathf.Round(point.y);
   RectTransformUtility.ScreenPointToLocalPointInRectangle(safe,point,null,out var local);supplyName.rectTransform.anchoredPosition=local;supplyName.enabled=true;
  }
 }
}
