using UnityEngine;
using UnityEngine.EventSystems;
namespace DeadDistrict {
 public sealed class SurvivalInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler {
  public static Vector2 Move;
  public static bool GrenadeRequested;
  public RectTransform knob;
  int pointer=int.MinValue;bool floating;Vector2 home;bool homeSet;
  public bool Floating=>floating;
  public void SetFloating(bool value){if(!homeSet){home=((RectTransform)transform).anchoredPosition;homeSet=true;}floating=value;GetComponent<UnityEngine.UI.Image>().raycastTarget=!value;Clear();}
  void Recenter(PointerEventData e){var r=(RectTransform)transform;var parent=(RectTransform)r.parent;RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,e.position,e.pressEventCamera,out var p);p-=parent.rect.min;float radius=r.rect.width*.5f;r.anchoredPosition=new Vector2(Mathf.Clamp(p.x,radius+20,parent.rect.width*.44f-radius),Mathf.Clamp(p.y,radius+20,parent.rect.height*.64f-radius));}

  public void OnPointerDown(PointerEventData e) {if(pointer!=int.MinValue)return;pointer=e.pointerId;e.useDragThreshold=false;if(floating)Recenter(e);OnDrag(e);}
  public void OnDrag(PointerEventData e) {
   if(pointer!=e.pointerId)return;
   var r=(RectTransform)transform;Vector2 p;
   RectTransformUtility.ScreenPointToLocalPointInRectangle(r,e.position,e.pressEventCamera,out p);
   float radius=r.rect.width*.35f; var n=Vector2.ClampMagnitude(p/radius,1);
   Move=n.magnitude<.12f?Vector2.zero:n;knob.anchoredPosition=n*radius;
  }
  public void OnPointerUp(PointerEventData e) {if(e.pointerId==pointer)Clear();}
  void OnDisable(){Clear();}
  public void Clear(){pointer=int.MinValue;Move=Vector2.zero;if(knob)knob.anchoredPosition=Vector2.zero;if(homeSet)((RectTransform)transform).anchoredPosition=home;}
  public static Vector2 ReadMove() {
   Vector2 keys=new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),
    (Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0));
   return keys.sqrMagnitude>0?Vector2.ClampMagnitude(keys,1):Move;
  }
  void OnApplicationFocus(bool focus){if(!focus)Clear();}
 }
}
