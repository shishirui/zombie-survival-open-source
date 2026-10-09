using UnityEngine;
namespace DeadDistrict {
 public sealed class RewardFeedback:MonoBehaviour {
  PackBurst earned,applied,heal;readonly PackBurst[] pickups=new PackBurst[2];int nextPickup;
  public int PickupsPlayed {get;private set;}
  public void Initialize(){earned=PackBurst.Create("GrowthEarn",transform);applied=PackBurst.Create("GrowthApply",transform);heal=PackBurst.Create("HealPickup",transform);for(int i=0;i<pickups.Length;i++)pickups[i]=PackBurst.Create("SupplyPickup",transform);}
  public void Pickup(Vector3 origin,Vector3 player,Color color,bool medical){PickupsPlayed++;if(medical)heal.Play(player+Vector3.up*.08f,1,Color.white,1.15f);else pickups[nextPickup++%pickups.Length].Play(player+Vector3.up*.5f,1,color,1);}
  public void Earn(Vector3 player){earned.Play(player+Vector3.up*.06f,1,Color.white,.95f);}
  public void Apply(Vector3 player){earned.Stop();applied.Play(player+Vector3.up*.06f,1,Color.white,1);}
  void LateUpdate(){var g=SurvivalGame.Instance;if(!g)return;if(g.Dead||g.Completed){earned.Stop();applied.Stop();heal.Stop();foreach(var p in pickups)p.Stop();return;}earned.Position=applied.Position=heal.Position=g.PlayerPosition+Vector3.up*.06f;foreach(var p in pickups)p.Position=g.PlayerPosition+Vector3.up*.5f;}
 }
 public sealed partial class SurvivalGame {public RewardFeedback Feedback {get;private set;}}
}
