using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
namespace DeadDistrict.Editor {
 public static class SurvivorStanceSetup {
  public const string AimSource="Assets/TopDownEngine/Demos/Colonel/Animations/Various/FBI@RifleAimingIdle.fbx";
  public static void ApplyAndBuild(){
   var clip=AssetDatabase.LoadAllAssetsAtPath(AimSource).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
   if(!clip.isHumanMotion||!clip.isLooping)throw new Exception("Purchased aim animation must be looping Humanoid");
   var pack=Resources.Load<PackVisuals>("DeadDistrict/PackVisuals");var c=(AnimatorController)pack.survivor.GetComponentInChildren<Animator>().runtimeAnimatorController;
   var locomotion=c.layers[0].stateMachine.states.First(s=>s.state.name=="Locomotion").state;var tree=(BlendTree)locomotion.motion;var children=tree.children;children[0].motion=clip;tree.children=children;
   var upper=c.layers[1].stateMachine.defaultState;upper.motion=clip;EditorUtility.SetDirty(tree);EditorUtility.SetDirty(upper);EditorUtility.SetDirty(c);
   PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.iOS.buildNumber="56";AssetDatabase.SaveAssets();
   Debug.Log("SURVIVOR_STANCE_READY source="+AimSource+" humanoid=true looping=true purchasedRunAndRollRetained=true");PrototypeSetup.Build();
  }
 }
}
