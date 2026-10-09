using System.Collections.Generic;
using UnityEngine;
namespace DeadDistrict {
 public static class ActionIconLibrary {
  static readonly Dictionary<ActionSymbol,Sprite> Cache=new Dictionary<ActionSymbol,Sprite>();
  public static Sprite Get(ActionSymbol symbol){
   if(Cache.TryGetValue(symbol,out var sprite)&&sprite)return sprite;
   string name;
   switch(symbol){
    case ActionSymbol.Grenade:name="grenade";break;
    case ActionSymbol.Roll:name="roll";break;
    case ActionSymbol.Reload:name="reload";break;
    case ActionSymbol.Launcher:case ActionSymbol.Rifle:name="rifle";break;
    case ActionSymbol.Shotgun:name="shotgun";break;
    case ActionSymbol.DoorOpen:case ActionSymbol.DoorClosed:name="door";break;
    case ActionSymbol.Lock:name="lock";break;
    default:return null;
   }
   sprite=Resources.Load<Sprite>("DeadDistrict/ActionIcons/"+name);Cache[symbol]=sprite;return sprite;
  }
 }
}
