using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace DeadDistrict.Editor {
 public static class FeedbackAssets {
  const string Folder="Assets/ZombieSurvival/Resources/DeadDistrict/Feedback/";
  static Material Convert(Material source){if(!source)throw new Exception("Missing particle material");string path=Folder+"Materials/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(m,path);}var tex=source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.HasProperty("_MainTex")?source.GetTexture("_MainTex"):null;m.SetTexture("_BaseMap",tex);m.SetColor("_BaseColor",Color.white);bool add=source.name.Contains("_ADD");m.SetFloat("_Surface",1);m.SetFloat("_Blend",add?2:0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)(add?BlendMode.One:BlendMode.OneMinusSrcAlpha));m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;EditorUtility.SetDirty(m);return m;}
  static void Create(string name,string source,float scale,bool loop,int cap,float life){
   string path="Assets/Epic Toon FX/Prefabs/"+source+".prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)throw new Exception(path);var root=new GameObject(name);var model=UnityEngine.Object.Instantiate(prefab,root.transform);model.transform.localPosition=Vector3.zero;VisualUpgrade.Strip(root);root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);root.transform.localScale=Vector3.one*scale;
   foreach(var r in root.GetComponentsInChildren<ParticleSystemRenderer>(true)){r.sharedMaterials=r.sharedMaterials.Where(m=>m).Select(Convert).ToArray();r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
   foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true)){
    ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);if(name=="AcidSplash"&&ps.name=="Smoke"){ps.gameObject.SetActive(false);}var m=ps.main;m.playOnAwake=false;m.loop=loop;m.stopAction=ParticleSystemStopAction.None;m.scalingMode=ParticleSystemScalingMode.Hierarchy;m.simulationSpace=name.StartsWith("Acid")?ParticleSystemSimulationSpace.World:ParticleSystemSimulationSpace.Local;m.maxParticles=cap;m.startLifetimeMultiplier=Mathf.Min(m.startLifetimeMultiplier,life);m.startDelay=0;
    var emit=ps.emission;emit.rateOverTimeMultiplier=Mathf.Min(emit.rateOverTimeMultiplier,loop?14:32);for(int i=0;i<emit.burstCount;i++){var b=emit.GetBurst(i);b.count=new ParticleSystem.MinMaxCurve(Mathf.Min(cap,b.count.constantMax));emit.SetBurst(i,b);}
    var trails=ps.trails;trails.enabled=false;var col=ps.collision;col.enabled=false;var light=ps.lights;light.enabled=false;var sub=ps.subEmitters;sub.enabled=false;
    if(name=="AcidSplash"&&ps.transform==model.transform){ps.GetComponent<ParticleSystemRenderer>().renderMode=ParticleSystemRenderMode.HorizontalBillboard;m.startRotation3D=false;m.startRotation=0;}
    if(name=="AcidMist"){m.startSpeed=new ParticleSystem.MinMaxCurve(.12f,.25f);m.startColor=new Color(.38f,.65f,.08f,.3f);m.startSize=new ParticleSystem.MinMaxCurve(.7f,1.1f);emit.SetBursts(new ParticleSystem.Burst[0]);emit.rateOverTime=ps.name=="Droplets"?0:9;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.75f;shape.radiusThickness=1;}
    if(name=="AcidBubbles"){m.startSpeedMultiplier*=.6f;m.startColor=new Color(.48f,.82f,.12f,.6f);}
   }
   PrefabUtility.SaveAsPrefabAsset(root,Folder+name+".prefab");UnityEngine.Object.DestroyImmediate(root);
  }
  public static void Prepare(){Directory.CreateDirectory(Folder+"Materials");AssetDatabase.Refresh();
   Create("AcidMissile","Combat/Missiles/Liquid/LiquidMissileAcid",.72f,true,18,.35f);
   Create("AcidSplash","Combat/Explosions/LiquidExplosion/LiquidExplosionAcid",1,false,24,.85f);
   Create("AcidMist","Combat/Explosions (Misc)/PoisonExplosionSoft",1,true,12,1.15f);
   Create("AcidBubbles","Environment/Water/Boiling/AcidBoiling",1,true,14,.8f);
   Create("GrowthEarn","Interactive/Level Up/Nova/LevelupNovaYellow",.65f,false,22,.9f);
   Create("GrowthApply","Interactive/Level Up/Cylinder/LevelupCylinderBlue",.78f,false,24,.95f);
   Create("HealPickup","Interactive/Healing/HealNova",.58f,false,20,1.1f);
   Create("SupplyPickup","Interactive/Loot/ItemSparkleBurst/ItemSparkleBurstYellow",.9f,false,26,.85f);
   AssetDatabase.SaveAssets();Debug.Log("FEEDBACK_ASSETS_PASS eight purchased effect presets");
  }
  public static void PrepareAndBuild(){Prepare();PrototypeSetup.Build();}
 }
}
