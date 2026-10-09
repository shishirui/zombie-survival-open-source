using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
namespace DeadDistrict.Editor {
 public static class CombatRevision {
  const string Folder="Assets/ZombieSurvival/Launcher/Polish/";
  static ParticleSystem Smoke(GameObject parent,string name,Material material,int count,float life,float size,Color tint){
   var go=new GameObject(name);go.transform.SetParent(parent.transform,false);var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var m=ps.main;m.duration=1.5f;m.loop=false;m.playOnAwake=false;m.useUnscaledTime=false;m.stopAction=ParticleSystemStopAction.None;m.simulationSpace=ParticleSystemSimulationSpace.World;m.scalingMode=ParticleSystemScalingMode.Hierarchy;m.maxParticles=count;m.startLifetime=new ParticleSystem.MinMaxCurve(life*.8f,life);m.startSize=new ParticleSystem.MinMaxCurve(size*.7f,size);m.startSpeed=new ParticleSystem.MinMaxCurve(.3f,1.1f);m.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);m.startColor=tint;
   var em=ps.emission;em.rateOverTime=0;em.rateOverDistance=0;em.SetBursts(new[]{new ParticleSystem.Burst(.035f,(short)count)});var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.38f;
   var sizeOver=ps.sizeOverLifetime;sizeOver.enabled=true;sizeOver.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.5f),new Keyframe(.35f,1),new Keyframe(1,1.8f)));
   var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.8f,.8f,.8f),1)},new[]{new GradientAlphaKey(.45f,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(.7f,.45f),new GradientAlphaKey(0,1)});color.color=gradient;
   var vel=ps.velocityOverLifetime;vel.enabled=true;vel.space=ParticleSystemSimulationSpace.World;vel.x=.08f;vel.y=.45f;vel.z=-.06f;var rotation=ps.rotationOverLifetime;rotation.enabled=true;rotation.z=.55f;
   var sheet=ps.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=2;sheet.numTilesY=2;sheet.frameOverTime=0;sheet.startFrame=new ParticleSystem.MinMaxCurve(0,4);
   var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return ps;
  }
  public static void ApplyEffects(){
   var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/ZombieSurvival/PackVisuals/Materials/TankExplosionSmokeMaterial_FX_21.mat");if(!source)throw new Exception("TopDown smoke material missing");
   string path=Folder+"LauncherSmoke.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}else mat.CopyPropertiesFromMaterial(source);mat.SetColor("_BaseColor",Color.white);EditorUtility.SetDirty(mat);
   var trailRoot=new GameObject("Launcher trailing smoke");var trail=Smoke(trailRoot,"Smoke emitter",mat,48,.45f,.5f,new Color(.9f,.87f,.8f,.72f));var main=trail.main;main.loop=true;main.startSpeed=.08f;var em=trail.emission;em.SetBursts(new ParticleSystem.Burst[0]);em.rateOverDistance=5;var shape=trail.shape;shape.radius=.035f;var size=trail.sizeOverLifetime;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.5f,1,2.4f));var prefab=PrefabUtility.SaveAsPrefabAsset(trail.gameObject,"Assets/ZombieSurvival/Resources/DeadDistrict/LauncherTrailSmoke.prefab");UnityEngine.Object.DestroyImmediate(trailRoot);
   var pack=Resources.Load<PackVisuals>("DeadDistrict/PackVisuals");var blast=new GameObject("LauncherBlast");
   // Reuse the grenade's fire atlas and animation curve, without the old spinning flat fireball.
   var body=GrenadeLayer(pack,blast,pack.explosion.name,8,1,false);body.name="Grenade flame clouds";var bm=body.main;bm.startSpeed=new ParticleSystem.MinMaxCurve(2,4);bm.startSize=new ParticleSystem.MinMaxCurve(.95f,1.4f);bm.startLifetime=new ParticleSystem.MinMaxCurve(.24f,.42f);bm.startColor=new Color(1,.66f,.24f,.85f);
   var bodyColor=body.colorOverLifetime;var warm=new Gradient();warm.SetKeys(new[]{new GradientColorKey(new Color(1,.9f,.6f),0),new GradientColorKey(new Color(.7f,.26f,.055f),1)},new[]{new GradientAlphaKey(.85f,0),new GradientAlphaKey(.6f,.45f),new GradientAlphaKey(0,1)});bodyColor.color=warm;
   var fire=GrenadeLayer(pack,blast,"Fire",1,.8f,true);var fm=fire.main;fm.startColor=new Color(1,.6f,.22f,.68f);
   var glow=GrenadeLayer(pack,blast,"Glow",1,.7f,true);var gm=glow.main;gm.startColor=new Color(1,1,1,.32f);
   var sparks=GrenadeLayer(pack,blast,"Sparks",5,1,false);var sm=sparks.main;sm.startSpeed=new ParticleSystem.MinMaxCurve(3,7);sm.startLifetime=new ParticleSystem.MinMaxCurve(.18f,.32f);
   Smoke(blast,"Rolling blast smoke",mat,16,1.6f,2.1f,new Color(.78f,.76f,.72f,.88f));var dust=Smoke(blast,"Ground dust puff",mat,6,.85f,1.65f,new Color(.8f,.74f,.66f,.6f));var dm=dust.main;dm.startSpeed=new ParticleSystem.MinMaxCurve(1.8f,3);var dv=dust.velocityOverLifetime;dv.y=.08f;
   pack.launcherExplosion=PrefabUtility.SaveAsPrefabAsset(blast,Folder+"LauncherBlast.prefab");UnityEngine.Object.DestroyImmediate(blast);EditorUtility.SetDirty(pack);
   int cap=pack.launcherExplosion.GetComponentsInChildren<ParticleSystem>(true).Sum(x=>x.main.maxParticles);if(cap>40)throw new Exception("Blast particle cap exceeded: "+cap);
   File.WriteAllText(Folder+"SmokeSources.txt","Smoke: existing TopDown LoftSmoke.tga through TankExplosionSmokeMaterial_FX_21, adapted URP alpha material.\nFlame clouds, fire, glow and sparks: current EpicGrenade prefab, preserving grenade fire texture and flipbook timing. Fire has no rotation over lifetime or velocity alignment.\n6 unchanged world-space trail emitters, 48 particles each.\n4 blast slots, "+cap+" particles each, 1.85 s lifetime; no new realtime lights. Original purchased files and grenade prefab unchanged.\n");Debug.Log("LAUNCHER_SMOKE_READY blastCap="+cap);
  }
  static ParticleSystem GrenadeLayer(PackVisuals pack,GameObject parent,string name,int count,float scale,bool stable){
   var source=pack.explosion.GetComponentsInChildren<ParticleSystem>(true).First(x=>x.name==name);var ps=UnityEngine.Object.Instantiate(source,parent.transform,false);foreach(Transform child in ps.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);ps.name="Grenade "+name;ps.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);ps.transform.localScale=Vector3.one*scale;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var m=ps.main;m.loop=false;m.playOnAwake=false;m.useUnscaledTime=false;m.stopAction=ParticleSystemStopAction.None;m.simulationSpace=ParticleSystemSimulationSpace.World;m.scalingMode=ParticleSystemScalingMode.Hierarchy;m.maxParticles=count;
   var em=ps.emission;em.rateOverTime=0;em.rateOverDistance=0;em.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
   var sub=ps.subEmitters;sub.enabled=false;var collision=ps.collision;collision.enabled=false;var lights=ps.lights;lights.enabled=false;
   if(stable){m.startRotation3D=false;m.startRotation=0;m.startSpeed=0;var rotation=ps.rotationOverLifetime;rotation.enabled=false;var bySpeed=ps.rotationBySpeed;bySpeed.enabled=false;var shape=ps.shape;shape.enabled=false;var velocity=ps.velocityOverLifetime;velocity.enabled=false;var force=ps.forceOverLifetime;force.enabled=false;var noise=ps.noise;noise.enabled=false;var r=ps.GetComponent<ParticleSystemRenderer>();r.renderMode=ParticleSystemRenderMode.Billboard;r.alignment=ParticleSystemRenderSpace.View;r.allowRoll=false;}
   return ps;
  }
  public static void ApplyImpactAndBuild(){ApplyEffects();PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.iOS.buildNumber="53";AssetDatabase.SaveAssets();PrototypeSetup.Build();}

  public static void ApplyHound(){
   const string folder="Assets/ZombieSurvival/Art/InfectedHound/";var source=AssetDatabase.LoadAllAssetsAtPath(folder+"Dog.fbx").OfType<AnimationClip>().First(c=>c.name.Contains("Walking")&&!c.name.StartsWith("__preview__"));
   var run=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"HoundRun.anim");if(!run){run=new AnimationClip();AssetDatabase.CreateAsset(run,folder+"HoundRun.anim");}run.ClearCurves();run.name="Hound Bound Run";run.frameRate=60;
   foreach(var binding in AnimationUtility.GetCurveBindings(source)){
    var curve=AnimationUtility.GetEditorCurve(source,binding);bool phaseShift=binding.path.Contains("Bone.011")||binding.path.Contains("Bone.017")||binding.path.Contains("IKBackRight")||binding.path.Contains("IKFrontRight");float shift=phaseShift?.42f:0;
    var keys=new Keyframe[39];for(int i=0;i<keys.Length;i++){float normalized=i/(float)(keys.Length-1);keys[i]=new Keyframe(normalized*.62f,curve.Evaluate(Mathf.Repeat(normalized+shift,1)*source.length));}var retimed=new AnimationCurve(keys);for(int i=0;i<keys.Length;i++){AnimationUtility.SetKeyLeftTangentMode(retimed,i,AnimationUtility.TangentMode.Auto);AnimationUtility.SetKeyRightTangentMode(retimed,i,AnimationUtility.TangentMode.Auto);}AnimationUtility.SetEditorCurve(run,binding,retimed);
   }run.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(run);settings.loopTime=true;settings.loopBlend=true;settings.keepOriginalPositionY=true;settings.keepOriginalPositionXZ=true;settings.keepOriginalOrientation=true;AnimationUtility.SetAnimationClipSettings(run,settings);EditorUtility.SetDirty(run);
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+"Hound.controller");var state=controller.layers[0].stateMachine.states.First(s=>s.state.name=="Purchased running").state;var tree=AssetDatabase.LoadAssetAtPath<BlendTree>(folder+"HoundRunBlend.asset");if(!tree){tree=new BlendTree();AssetDatabase.CreateAsset(tree,folder+"HoundRunBlend.asset");}tree.name="Bound run locomotion";tree.blendParameter="Speed";tree.useAutomaticThresholds=false;var idle=AssetDatabase.LoadAllAssetsAtPath(folder+"Dog.fbx").OfType<AnimationClip>().First(c=>c.name.Contains("Idle")&&!c.name.StartsWith("__preview__"));tree.children=new[]{new ChildMotion{motion=idle,threshold=0,timeScale=1},new ChildMotion{motion=run,threshold=.15f,timeScale=1}};state.motion=tree;EditorUtility.SetDirty(tree);EditorUtility.SetDirty(state);EditorUtility.SetDirty(controller);
   Debug.Log("HOUND_RUN_READY cycle=.62 speed=5.4 originalWalking=1.6667");
  }
  public static void ApplyAndBuild(){ApplyEffects();ApplyHound();PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.iOS.buildNumber="52";AssetDatabase.SaveAssets();PrototypeSetup.Build();}
 }
}
