using UnityEngine;
using UnityEngine.EventSystems;
namespace DeadDistrict {
 // Keep the grenade gesture independent of the left movement finger.
 public sealed class GrenadeAimInput : MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler {
  SurvivalGame game;int pointer=int.MinValue;Vector2 origin;
  public void Bind(SurvivalGame owner){game=owner;}
  Vector2 Local(PointerEventData e){RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out var p);return p;}
  public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue||!game||!game.BeginGrenadeAim())return;pointer=e.pointerId;origin=Local(e);e.useDragThreshold=false;}
  public void OnDrag(PointerEventData e){if(pointer!=e.pointerId||!game)return;float radius=95f;game.SetGrenadeAim(Vector2.ClampMagnitude((Local(e)-origin)/radius,1));}
  public void OnPointerUp(PointerEventData e){if(pointer!=e.pointerId)return;OnDrag(e);pointer=int.MinValue;if(game)game.EndGrenadeAim(true);}
  public void Cancel(){pointer=int.MinValue;if(game)game.EndGrenadeAim(false);}
  void OnDisable(){Cancel();}
  void OnApplicationFocus(bool focused){if(!focused)Cancel();}
 }
}
