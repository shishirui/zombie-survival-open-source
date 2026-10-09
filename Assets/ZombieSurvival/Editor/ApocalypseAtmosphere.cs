using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace DeadDistrict.Editor {
 public static class ApocalypseAtmosphere {
  const string Source="Assets/PolygonApocalypse/Prefabs/";
  const string Output="Assets/ZombieSurvival/Apocalypse/Atmosphere/";
  const string Scene="Assets/ZombieSurvival/Scenes/DeadDistrict.unity";
  const string RootName="Apocalypse atmosphere";
  [MenuItem("Dead District/Apply Apocalypse atmosphere (batch 3)")]
  public static void Apply(){
   Directory.CreateDirectory(Output);AssetDatabase.Refresh();var scene=EditorSceneManager.OpenScene(Scene);
   var old=GameObject.Find(RootName);if(old)UnityEngine.Object.DestroyImmediate(old);
   var root=new GameObject(RootName).transform;
   var cars=GameObject.Find("Apocalypse district dressing").GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Apocalypse SM_Prop_Car")).OrderBy(t=>t.position.z).ToArray();
   if(cars.Length<2)throw new Exception("Street wrecks missing");
   // Only two wrecks smoulder. Effects stay inside the existing obstacle footprint.
   foreach(var car in new[]{cars[0],cars[cars.Length-1]}){
    var site=new GameObject("Wreck embers").transform;site.SetParent(root,false);site.position=car.position+Vector3.up*.65f;
    Effect(site,"FX_Fire_01",0);Effect(site,"FX_Smoke_Small_01",1);site.gameObject.AddComponent<DistrictAtmosphere>();
   }
   foreach(var pos in new[]{new Vector3(-19,.1f,-12),new Vector3(19,.1f,12)}){
    var site=new GameObject("Curb wind dust").transform;site.SetParent(root,false);site.position=pos;Effect(site,"FX_Dust_Wind_01",2);site.gameObject.AddComponent<DistrictAtmosphere>();
   }
   var light=GameObject.Find("Late afternoon").GetComponent<Light>();light.color=new Color(1,.95f,.86f);light.intensity=1.35f;
   RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.52f,.58f,.63f);
   var volume=GameObject.Find("Atmosphere").GetComponent<Volume>();var profile=volume.sharedProfile;
   if(profile.TryGet<ColorAdjustments>(out var color)){color.postExposure.Override(.24f);color.contrast.Override(9);color.saturation.Override(-3);EditorUtility.SetDirty(color);}
   // Preserve existing bloom and grenade/muzzle settings.
   Pickups();PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.iOS.buildNumber="39";
   EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Validate();
  }
  static Material Convert(Material source,bool particle){
   string path=Output+source.name+(particle?"-particle":"-lit")+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!material){material=new Material(Shader.Find(particle?"Universal Render Pipeline/Particles/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
   material.SetTexture("_BaseMap",source.HasProperty("_MainTex")?source.GetTexture("_MainTex"):null);material.SetColor("_BaseColor",Color.white);
   material.SetFloat("_Cull",particle?0:2);material.enableInstancing=true;
   if(particle){
    bool add=source.shader.name.Contains("Additive")||source.name.Contains("Fire")||source.name.Contains("Ember");
    material.SetFloat("_Surface",1);material.SetFloat("_Blend",add?2:0);material.SetFloat("_ZWrite",0);
    material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)(add?BlendMode.One:BlendMode.OneMinusSrcAlpha));
    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
   }else {material.SetFloat("_Smoothness",.13f);material.SetFloat("_Metallic",0);}
   EditorUtility.SetDirty(material);return material;
  }
  static void Effect(Transform parent,string name,int kind){
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"FX/Prefabbed/"+name+".prefab");if(!source)throw new Exception("Missing "+name);
   var go=UnityEngine.Object.Instantiate(source,parent);go.name=name;VisualUpgrade.Strip(go,true);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;go.transform.localScale=Vector3.one;
   foreach(var ps in go.GetComponentsInChildren<ParticleSystem>(true)){
    ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);bool ember=ps.name.Contains("Ember");
    var main=ps.main;main.loop=true;main.playOnAwake=false;main.prewarm=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.cullingMode=ParticleSystemCullingMode.Pause;main.useUnscaledTime=false;
    main.maxParticles=kind==0?(ember?8:24):kind==1?10:12;main.startLifetime=kind==0?1.2f:kind==1?2.8f:3.5f;main.startSpeed=0;main.gravityModifier=0;
    main.startSize=kind==0?(ember?.035f:.5f):kind==1?.55f:.7f;main.startSize3D=false;
    main.startColor=kind==0?new Color(1,.62f,.22f,ember?.65f:.75f):kind==1?new Color(.26f,.29f,.31f,.24f):new Color(.60f,.53f,.42f,.12f);
    var emission=ps.emission;emission.enabled=true;emission.rateOverTime=kind==0?(ember?3:12):kind==1?2.5f:2;emission.rateOverDistance=0;emission.SetBursts(new ParticleSystem.Burst[0]);
    var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.position=Vector3.zero;shape.rotation=Vector3.zero;shape.scale=kind==2?new Vector3(3,.1f,1):new Vector3(.55f,.15f,.55f);
    var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=kind==2?.65f:.12f;velocity.y=kind==0?(ember?.8f:.5f):kind==1?.4f:.025f;velocity.z=.08f;velocity.orbitalX=velocity.orbitalY=velocity.orbitalZ=0;velocity.radial=0;velocity.speedModifier=1;
    var force=ps.forceOverLifetime;force.enabled=false;var noise=ps.noise;noise.enabled=false;
    var collision=ps.collision;collision.enabled=false;var lights=ps.lights;lights.enabled=false;var trails=ps.trails;trails.enabled=false;var sub=ps.subEmitters;sub.enabled=false;
    var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,kind==0?.7f:.6f,1,kind==0?.1f:1.8f));
    var tint=ps.colorOverLifetime;tint.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.2f),new GradientAlphaKey(0,1)});tint.color=gradient;
    var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterials=renderer.sharedMaterials.Where(m=>m!=null).Select(m=>Convert(m,true)).ToArray();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.maxParticleSize=.08f;
   }
  }
  static GameObject Model(string path,Transform parent,float size){
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source+path+".prefab");if(!source)throw new Exception("Missing "+path);
   var model=UnityEngine.Object.Instantiate(source,parent);VisualUpgrade.Strip(model,false);foreach(var a in model.GetComponentsInChildren<Animator>())UnityEngine.Object.DestroyImmediate(a);
   model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
   foreach(var r in model.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>Convert(m,false)).ToArray();
   var b=Bounds(model);model.transform.localScale*=size/Mathf.Max(b.size.x,b.size.y,b.size.z);b=Bounds(model);model.transform.localPosition-=new Vector3(b.center.x,b.min.y,b.center.z);return model;
  }
  static Bounds Bounds(GameObject g){var rs=g.GetComponentsInChildren<MeshRenderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
  static Material Accent(string name,Color color){var path=Output+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.15f);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;}
  static GameObject Save(GameObject go,string name){var prefab=PrefabUtility.SaveAsPrefabAsset(go,Output+name+".prefab");UnityEngine.Object.DestroyImmediate(go);return prefab;}
  static void Pickups(){
   var pack=Resources.Load<PackVisuals>("DeadDistrict/PackVisuals");
   var white=Accent("Medical ivory",new Color(.91f,.94f,.85f));var gold=Accent("Ammo brass",new Color(.95f,.66f,.18f));
   var root=new GameObject("Apocalypse medical case");Model("Props/SM_Prop_MedicalBox_01",root.transform,.95f);float top=Bounds(root).max.y;
   BlockVisuals.Shape("Medical cross long",PrimitiveType.Cube,root.transform,new Vector3(0,top+.012f,0),new Vector3(.12f,.018f,.36f),white);
   BlockVisuals.Shape("Medical cross wide",PrimitiveType.Cube,root.transform,new Vector3(0,top+.014f,0),new Vector3(.36f,.018f,.12f),white);pack.supplies[0]=Save(root,"MedicalCase");
   root=new GameObject("Apocalypse ammo case");Model("Item/SM_Item_Ammo_Crate_01",root.transform,.95f);top=Bounds(root).max.y;
   for(int i=0;i<3;i++){var bullet=BlockVisuals.Shape("Ammo lid round",PrimitiveType.Capsule,root.transform,new Vector3((i-1)*.17f,top+.04f,0),new Vector3(.065f,.16f,.065f),gold);bullet.transform.localRotation=Quaternion.Euler(90,0,0);}
   pack.supplies[1]=Save(root,"AmmoCase");root=new GameObject("Apocalypse grenade supply");Model("Weapons/Misc/SM_Wep_Grenade_01",root.transform,.7f);pack.supplies[2]=Save(root,"GrenadeSupply");
   // Keep the existing blue armour silhouette and its matching blue ground ring.
   root=new GameObject("Apocalypse shotgun case");Model("Props/SM_Prop_Ammo_Box_01",root.transform,1.55f);top=Bounds(root).max.y;
   var gun=Model("Weapons/Guns/SM_Wep_Shotgun_01",root.transform,1.3f);gun.transform.localRotation=Quaternion.Euler(0,90,90);var b=Bounds(gun);gun.transform.position+=new Vector3(-b.center.x,top+.05f-b.min.y,-b.center.z);
   BlockVisuals.Shape("Weapon case brass band",PrimitiveType.Cube,root.transform,new Vector3(-.48f,top+.015f,0),new Vector3(.1f,.025f,.38f),gold);
   pack.shotgunCrate=Save(root,"ShotgunCase");EditorUtility.SetDirty(pack);
  }
  public static void Validate(){
   var root=GameObject.Find(RootName);if(!root||root.GetComponentsInChildren<DistrictAtmosphere>().Length!=4)throw new Exception("Ambient sites missing");
   int budget=0;foreach(var p in root.GetComponentsInChildren<ParticleSystem>()){budget+=p.main.maxParticles;if(p.collision.enabled||p.lights.enabled||p.main.useUnscaledTime)throw new Exception("Unbounded atmosphere feature");}
   if(budget>108||root.GetComponentsInChildren<Light>().Length>0||root.GetComponentsInChildren<Collider>().Length>0)throw new Exception("Atmosphere budget or collision");
   var pack=Resources.Load<PackVisuals>("DeadDistrict/PackVisuals");foreach(var prefab in pack.supplies.Concat(new[]{pack.shotgunCrate}))foreach(var renderer in prefab.GetComponentsInChildren<Renderer>())foreach(var m in renderer.sharedMaterials)if(!m||!m.shader.name.StartsWith("Universal Render Pipeline"))throw new Exception("Pickup shader missing");
   Debug.Log("APOCALYPSE_ATMOSPHERE_PASS sites=4 particleBudget="+budget+" newPickupModels=4");
  }
  public static void ApplyAndBuild(){Apply();PrototypeSetup.Build();}
 }
}
