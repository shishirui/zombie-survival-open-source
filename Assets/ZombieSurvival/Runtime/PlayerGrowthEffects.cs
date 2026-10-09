using UnityEngine;
namespace DeadDistrict {
 // A bounded set of reusable lines: no new objects on each level or selection.
 public sealed class PlayerGrowthEffects:MonoBehaviour {
  LineRenderer earned,shield,halo;readonly LineRenderer[] sparks=new LineRenderer[8];float earnAt=-100,applyAt=-100;
  public int EarnPulses {get;private set;}
  public int ApplyPulses {get;private set;}
  public void Initialize(Material material){
   earned=BlockVisuals.Ring("Earned growth point",transform,.7f,Color.white,material);
   shield=BlockVisuals.Ring("Growth protection",transform,.76f,Color.white,material);
   halo=BlockVisuals.Ring("Growth activation glow",transform,.62f,Color.white,material);
   for(int i=0;i<sparks.Length;i++){var o=new GameObject("Growth mote "+i);o.transform.SetParent(transform,false);var l=o.AddComponent<LineRenderer>();l.sharedMaterial=material;l.useWorldSpace=false;l.positionCount=2;l.widthMultiplier=.05f;l.numCapVertices=3;l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;sparks[i]=l;}
   Update();
  }
  public void Earn(){if(Time.time-earnAt<.5f)return;earnAt=Time.time;EarnPulses++;SurvivalGame.Instance.Feedback.Earn(transform.position);}
  public void Apply(){applyAt=Time.time;ApplyPulses++;SurvivalGame.Instance.Feedback.Apply(transform.position);}
  void Update(){if(!earned)return;float comfort=MobilePreferences.Current.reducedFlash?.35f:1;
   float t=(Time.time-earnAt)/.65f;bool earning=t>=0&&t<1;earned.gameObject.SetActive(earning);if(earning){earned.transform.localScale=Vector3.one*Mathf.Lerp(.7f,1.6f,t);earned.startColor=earned.endColor=new Color(.45f,.88f,.65f,(1-t)*.8f*comfort);}
   for(int i=0;i<sparks.Length;i++){var s=sparks[i];s.gameObject.SetActive(earning);if(!earning)continue;float a=i*Mathf.PI*.25f;var p=new Vector3(Mathf.Cos(a)*.6f,.15f+t*(.55f+(i%3)*.1f),Mathf.Sin(a)*.6f);s.SetPosition(0,p);s.SetPosition(1,p+Vector3.up*.065f);s.startColor=s.endColor=new Color(.55f,.9f,.6f,(1-t)*comfort);}
   float u=Time.time-applyAt;bool active=u>=0&&u<1;shield.gameObject.SetActive(active);halo.gameObject.SetActive(active);if(active){float alpha=(1-u)*comfort;shield.startColor=shield.endColor=new Color(.3f,.76f,.95f,alpha*.8f);shield.widthMultiplier=.07f;halo.transform.localPosition=Vector3.up*(.2f+u*1.35f);halo.startColor=halo.endColor=new Color(.48f,.9f,.95f,alpha*.42f);halo.widthMultiplier=.08f;}
  }
 }
}
