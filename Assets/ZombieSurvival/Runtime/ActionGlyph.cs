using UnityEngine;
using UnityEngine.UI;
namespace DeadDistrict {
 public enum ActionSymbol { Grenade, Roll, Rifle, Shotgun, DoorOpen, DoorClosed, Crate, Explosive, Lock, Ring, Health, Reload, Launcher, Upgrade }
 // Resolution-independent HUD artwork: all strokes share the same weight at any phone scale.
 public sealed class ActionGlyph:MaskableGraphic {
  public ActionSymbol Symbol; public float Progress=1;
  VertexHelper mesh;
  public void Set(ActionSymbol symbol,Color tint,float progress=1){if(Symbol==symbol&&color==tint&&Mathf.Approximately(Progress,progress))return;Symbol=symbol;color=tint;Progress=progress;SetVerticesDirty();}
  Vector2 Point(float x,float y)=>new Vector2(x*rectTransform.rect.width/80,y*rectTransform.rect.height/80)+rectTransform.rect.center;
  void Quad(Vector2 a,Vector2 b,Vector2 c,Vector2 d){int n=mesh.currentVertCount;mesh.AddVert(a,color,Vector2.zero);mesh.AddVert(b,color,Vector2.zero);mesh.AddVert(c,color,Vector2.zero);mesh.AddVert(d,color,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);}
  void Line(float ax,float ay,float bx,float by,float width=4){var a=Point(ax,ay);var b=Point(bx,by);var normal=new Vector2(-(b-a).y,(b-a).x).normalized*width*rectTransform.rect.width/160;Quad(a-normal,a+normal,b+normal,b-normal);}
  void Path(float width,params float[] xy){for(int i=0;i<xy.Length-2;i+=2)Line(xy[i],xy[i+1],xy[i+2],xy[i+3],width);}
  void Box(float x,float y,float w,float h){Quad(Point(x,y),Point(x,y+h),Point(x+w,y+h),Point(x+w,y));}
  void Arc(float x,float y,float radius,float from,float degrees,float width=4){int segments=Mathf.Max(1,Mathf.CeilToInt(Mathf.Abs(degrees)/8));for(int i=0;i<segments;i++){float a=(from+degrees*i/segments)*Mathf.Deg2Rad,b=(from+degrees*(i+1)/segments)*Mathf.Deg2Rad;Line(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius,width);}}
  void Arrow(float x,float y,bool right){float d=right?1:-1;Path(3,x-9*d,y,x+9*d,y,x+3*d,y+6);Line(x+9*d,y,x+3*d,y-6,3);}
  protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();mesh=vh;
   switch(Symbol){
    case ActionSymbol.Ring:Arc(0,0,37,90,-360*Mathf.Clamp01(Progress),2);break;
    case ActionSymbol.Grenade:
     Path(4,-13,-25,-21,-15,-21,3,-12,16,9,16,18,4,18,-15,10,-25,-13,-25);Path(4,-7,17,-7,24,8,24,22,11,26,-9);Arc(15,25,5,0,360,3);Line(-19,-3,16,-3,3);Line(-17,-14,15,-14,3);Line(-3,13,-3,-23,3);break;
    case ActionSymbol.Roll:
     Arc(0,0,29,42,275,3);Path(3,18,-26,24,-19,14,-17);Arc(10,14,6,0,360,4);Path(5,4,7,-9,3,-13,-7,1,-10,12,-18,22,-18);Path(5,-9,3,-1,-1,10,3);Path(5,-12,-6,-19,-13,-14,-19,-5,-19);break;
    case ActionSymbol.Rifle:case ActionSymbol.Shotgun:
     Box(-18,-1,37,9);Box(17,2,17,4);Path(5,-17,2,-29,-5,-29,6,-19,8);Path(5,-3,-1,-8,-14,-2,-14,3,-2);if(Symbol==ActionSymbol.Rifle){Path(6,8,-2,11,-14,18,-12,16,-2);Box(8,9,6,5);}else{Box(4,-5,19,3);Box(25,-1,9,3);}Arrow(-13,24,true);Arrow(15,-25,false);break;
    case ActionSymbol.DoorOpen:case ActionSymbol.DoorClosed:
     Path(4,-22,-27,-22,27,16,27,16,-27);if(Symbol==ActionSymbol.DoorOpen){Path(4,-20,25,2,16,2,-33,-20,-25);Box(-5,-7,3,5);Arrow(24,0,true);}else{Path(3,-18,-25,11,-25,11,22,-18,22);Box(4,-4,3,5);Arrow(25,0,false);}break;
    case ActionSymbol.Crate:
     Path(4,-24,-18,-24,13,0,25,24,13,24,-18,0,-29,-24,-18);Path(3,-24,13,0,1,24,13);Path(3,0,1,0,-29);Path(3,-12,19,12,7,12,-23);break;
    case ActionSymbol.Explosive:
     Path(4,0,27,5,13,21,20,15,5,29,0,14,-6,20,-22,5,-15,0,-30,-6,-15,-23,-21,-15,-6,-30,0,-15,6,-22,21,-6,14,0,27);Box(-2,1,4,12);Box(-2,-9,4,4);break;
    case ActionSymbol.Reload:
     Path(4,-11,-20,-11,17,9,17,14,-17,9,-22,-11,-20);Line(-5,8,7,8,3);Line(-5,-1,8,-1,3);Line(-5,-10,9,-10,3);Arc(0,0,31,35,270,3);Path(4,8,-31,18,-30,15,-20);break;
    case ActionSymbol.Upgrade:Path(7,-22,4,0,26,22,4);Path(7,-22,-17,0,5,22,-17);break;
    case ActionSymbol.Health:Box(-8,-28,16,56);Box(-28,-8,56,16);break;
    case ActionSymbol.Lock:Path(6,-14,2,-14,13,-9,20,9,20,14,13,14,2);Box(-21,-25,42,29);break;
   }
  }
 }
}
