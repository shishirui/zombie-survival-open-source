using System;
using UnityEngine;
namespace DeadDistrict {
 // Fade closed buildings and large cover without changing collision or navigation.
 public sealed class StreetOcclusion : MonoBehaviour {
  [Serializable] public sealed class Surface {
   public MeshRenderer renderer;
   public Material[] opaque, translucent;
  }
  public Surface[] surfaces;
  public BoxCollider obstacle;
  public float Opacity {get;private set;}=1;
  public bool ObscuringPlayer {get;private set;}
  MaterialPropertyBlock block;
  bool ghost;
  static readonly int BaseColor=Shader.PropertyToID("_BaseColor");
  void Awake(){block=new MaterialPropertyBlock();}
  bool Blocks(Vector3 from,Vector3 to){
   var delta=to-from;var bounds=obstacle.bounds;bounds.Expand(ObscuringPlayer?.65f:.35f);
   return bounds.IntersectRay(new Ray(from,delta.normalized),out float distance)&&distance<delta.magnitude;
  }
  void LateUpdate(){
   var game=SurvivalGame.Instance;var camera=Camera.main;
   if(!game||!game.PlayerHealth||!camera||!obstacle)return;
   Vector3 head=game.PlayerPosition+Vector3.up*1.35f,foot=game.PlayerPosition+Vector3.up*.3f;
   ObscuringPlayer=Blocks(camera.transform.position,head)||Blocks(camera.transform.position,foot);
   float target=ObscuringPlayer?.15f:1;
   float next=Mathf.MoveTowards(Opacity,target,Time.unscaledDeltaTime*3.4f);
   if(Mathf.Approximately(Opacity,next))return;
   Opacity=next;bool nextGhost=Opacity<.999f;
   if(nextGhost!=ghost){ghost=nextGhost;foreach(var s in surfaces)s.renderer.sharedMaterials=ghost?s.translucent:s.opaque;}
   foreach(var s in surfaces){
    if(ghost){block.Clear();block.SetColor(BaseColor,new Color(1,1,1,Opacity));s.renderer.SetPropertyBlock(block);}
    else s.renderer.SetPropertyBlock(null);
   }
  }
  void OnDisable(){
   if(surfaces!=null)foreach(var s in surfaces)if(s.renderer){s.renderer.sharedMaterials=s.opaque;s.renderer.SetPropertyBlock(null);}
   ghost=false;Opacity=1;ObscuringPlayer=false;
  }
 }
}
