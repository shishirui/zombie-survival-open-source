using UnityEngine;
namespace DeadDistrict {
 public sealed partial class SurvivalGame {
  public bool Rolling {get;private set;}
  public int Rolls {get;private set;}
  public float RollCooldownRemaining=>Mathf.Max(0,nextRoll-Time.time);
  public bool CanRoll=>ready&&!Dead&&!Paused&&!Rolling&&!IsGrenadeAiming&&RollCooldownRemaining<=0;
  float rollAt,rollCovered,nextRoll;Vector3 rollDirection;TrailRenderer rollTrail;
  void InitializeDodge(){
   var o=new GameObject("Dodge speed trail");o.transform.SetParent(player,false);o.transform.localPosition=Vector3.up*.35f;
   rollTrail=o.AddComponent<TrailRenderer>();rollTrail.sharedMaterial=Resources.Load<Material>("DeadDistrict/BulletGlow");rollTrail.time=.16f;rollTrail.widthMultiplier=.25f;rollTrail.startColor=new Color(.25f,1.7f,2,.6f);rollTrail.endColor=new Color(.15f,1,1,0);rollTrail.minVertexDistance=.1f;rollTrail.emitting=false;rollTrail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  }
  public bool TryRoll(){
   if(!CanRoll)return false;
   Vector2 input=SurvivalInput.ReadMove();var r=cam.transform.right;var f=cam.transform.forward;r.y=f.y=0;r.Normalize();f.Normalize();
   rollDirection=input.sqrMagnitude>.02f?(r*input.x+f*input.y).normalized:lastMove.normalized;rollDirection.y=0;
   if(rollDirection.sqrMagnitude<.5f)rollDirection=model.forward;
   rollAt=Time.time;rollCovered=0;nextRoll=Time.time+config.rollCooldown;Rolling=true;Rolls++;
   PlayerHealth.BeginDodge(config.rollDuration);Physics.IgnoreLayerCollision(26,24,true);model.rotation=Quaternion.LookRotation(rollDirection);playerActor.SetRolling(true);rollTrail.Clear();rollTrail.emitting=true;Sound.Roll();Sound.SetWalking(false);return true;
  }
  void MoveDodge(){
   float t=Mathf.Clamp01((Time.time-rollAt)/config.rollDuration);float distance=config.rollDistance*(1-(1-t)*(1-t));
   controller.Move(rollDirection*(distance-rollCovered)+Vector3.down*12*Time.deltaTime);rollCovered=distance;
   if(t>=1)CancelDodge();
  }
  void CancelDodge(){Rolling=false;PlayerHealth?.ClearDodge();Physics.IgnoreLayerCollision(26,24,false);if(playerActor)playerActor.SetRolling(false);if(rollTrail)rollTrail.emitting=false;}
 }
}
