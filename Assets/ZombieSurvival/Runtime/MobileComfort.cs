using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace DeadDistrict {
 public sealed class MobileComfort:MonoBehaviour {
  sealed class Tint {public Renderer renderer;public int index;public MaterialPropertyBlock original,dim;}
  readonly List<Tint> tints=new List<Tint>();Bloom bloom;float bloomIntensity;bool? last;
  static readonly int Base=Shader.PropertyToID("_BaseColor"),TintColor=Shader.PropertyToID("_TintColor");
  public void Initialize(SurvivalGame game){
   foreach(var renderer in FindObjectsByType<ParticleSystemRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
    for(int i=0;i<renderer.sharedMaterials.Length;i++){var material=renderer.sharedMaterials[i];if(!material)continue;var original=new MaterialPropertyBlock();renderer.GetPropertyBlock(original,i);var dim=new MaterialPropertyBlock();renderer.GetPropertyBlock(dim,i);foreach(int property in new[]{Base,TintColor})if(material.HasProperty(property)){Color c=material.GetColor(property);dim.SetColor(property,new Color(c.r*.42f,c.g*.42f,c.b*.42f,c.a));}tints.Add(new Tint{renderer=renderer,index=i,original=original,dim=dim});}
   }
   foreach(var volume in FindObjectsByType<Volume>(FindObjectsSortMode.None))if(volume.isGlobal&&volume.profile.TryGet(out bloom)){bloomIntensity=bloom.intensity.value;break;}
   MobilePreferences.Changed+=Apply;Apply();
  }
  void Apply(){bool reduced=MobilePreferences.Current.reducedFlash;if(last==reduced)return;last=reduced;foreach(var tint in tints)if(tint.renderer)tint.renderer.SetPropertyBlock(reduced?tint.dim:tint.original,tint.index);if(bloom!=null)bloom.intensity.value=bloomIntensity*(reduced?.35f:1);}
  void OnDestroy(){MobilePreferences.Changed-=Apply;}
 }
}
