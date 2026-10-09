using UnityEngine;
using UnityEngine.EventSystems;
namespace DeadDistrict {
 public sealed class FloatingJoystickPad:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler {
  public SurvivalInput input;
  public void OnPointerDown(PointerEventData e){if(input)input.OnPointerDown(e);}
  public void OnDrag(PointerEventData e){if(input)input.OnDrag(e);}
  public void OnPointerUp(PointerEventData e){if(input)input.OnPointerUp(e);}
  void OnDisable(){if(input)input.Clear();}
 }
}
