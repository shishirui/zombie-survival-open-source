using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
namespace DeadDistrict.Editor {
 public static class VisualUpgrade {
  const string Loft="Assets/TopDownEngine/Demos/Loft3D/";
  const string Root="Assets/ZombieSurvival/PackVisuals/";
  static readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();
  internal static readonly List<string> reused=new List<string>();
  [MenuItem("Dead District/Rebuild pack visuals and scene")]
  public static void Prepare(){
   Directory.CreateDirectory(Root+"Materials");Directory.CreateDirectory(Root+"Prefabs");Directory.CreateDirectory(Root+"Animations");AssetDatabase.Refresh();
   var pack=AssetDatabase.LoadAssetAtPath<PackVisuals>("Assets/ZombieSurvival/Resources/DeadDistrict/PackVisuals.asset");
   if(!pack){pack=ScriptableObject.CreateInstance<PackVisuals>();AssetDatabase.CreateAsset(pack,"Assets/ZombieSurvival/Resources/DeadDistrict/PackVisuals.asset");}
   var playerController=Controller("Survivor",true);var enemyController=Controller("Infected",false);
   pack.survivor=Actor("Survivor","Prefabs/PlayableCharacters/LoftSuspenders.prefab",playerController,true,0);
   pack.infected=new[]{Actor("InfectedTie","Prefabs/PlayableCharacters/LoftTie.prefab",enemyController,false,0),Actor("InfectedSuit","Prefabs/PlayableCharacters/LoftSuit.prefab",enemyController,false,1)};
   pack.impact=Effect("Impact","Prefabs/Weapons/Projectiles/LoftBulletImpact.prefab",.2f);
   pack.explosion=Effect("Explosion","Prefabs/Weapons/Projectiles/LoftGrenadeExplosion.prefab",.65f);
   pack.death=Effect("Death","Prefabs/Props/LoftDeathVFX.prefab",.4f);
   pack.grenade=Grenade();pack.bullet=Bullet();EditorUtility.SetDirty(pack);
   DressScene();PresentationUpgrade.Apply(pack);ApocalypseUpgrade.Apply();ApocalypseCharacters.Apply();ApocalypseAtmosphere.Apply();AssetDatabase.SaveAssets();
   File.WriteAllText("/tmp/deaddistrict-reused-assets.txt",string.Join("\n",reused.Distinct()));
   Debug.Log("VISUAL_UPGRADE_READY reused="+reused.Distinct().Count()+" materials="+mats.Count);
  }
  static GameObject Load(string path){reused.Add(Loft+path);var g=AssetDatabase.LoadAssetAtPath<GameObject>(Loft+path);if(!g)throw new Exception("Missing pack asset: "+path);return g;}
  static Transform Named(GameObject g,string name){return g.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);}
  internal static void Strip(GameObject g,bool particles=true){
   foreach(var c in g.GetComponentsInChildren<MonoBehaviour>(true).Reverse())if(c)UnityEngine.Object.DestroyImmediate(c);
   foreach(var t in g.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
   foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
   foreach(var c in g.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(c);
   foreach(var c in g.GetComponentsInChildren<AudioSource>(true))UnityEngine.Object.DestroyImmediate(c);
   foreach(var c in g.GetComponentsInChildren<Light>(true))UnityEngine.Object.DestroyImmediate(c);
   if(!particles)foreach(var p in g.GetComponentsInChildren<ParticleSystem>(true))UnityEngine.Object.DestroyImmediate(p.gameObject);
  }
  static Material Convert(Material source,bool particle,Color? tint=null){
   if(!source)return null;
   string key=source.GetInstanceID()+"_"+particle+"_"+(tint.HasValue?tint.Value.ToString():"base");if(mats.TryGetValue(key,out var found))return found;
   var m=new Material(Shader.Find(particle?"Universal Render Pipeline/Particles/Unlit":"Universal Render Pipeline/Lit"));
   Color color=source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.HasProperty("_Color")?source.GetColor("_Color"):source.HasProperty("_TintColor")?source.GetColor("_TintColor"):Color.white;
   if(tint.HasValue)color=tint.Value;
   if(particle&&(source.shader.name=="Sprites/Default"||source.shader.name=="Particles/Standard Unlit"||source.HasProperty("_TintColor")))color=Color.white;
   var tex=source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.HasProperty("_MainTex")?source.GetTexture("_MainTex"):null;
   m.SetColor("_BaseColor",color);if(tex)m.SetTexture("_BaseMap",tex);
   if(!particle){m.SetFloat("_Smoothness",source.HasProperty("_Glossiness")?Mathf.Min(.35f,source.GetFloat("_Glossiness")):.18f);m.SetFloat("_Metallic",source.HasProperty("_Metallic")?source.GetFloat("_Metallic"):0);m.SetFloat("_Cull",0);}
   else {
    bool additive=(source.HasProperty("_DstBlend")&&Mathf.Approximately(source.GetFloat("_DstBlend"),1))||source.name.Contains("_ADD")||source.shader.name.Contains("Additive")||source.name.Contains("Muzzle")||source.name.Contains("Twirl");
    m.SetFloat("_Surface",1);m.SetFloat("_Blend",additive?2:0);m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",0);
    m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)(additive?BlendMode.One:BlendMode.OneMinusSrcAlpha));
    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=(int)RenderQueue.Transparent;
   }
   string filename=source.name+"_"+(particle?"FX":"Lit")+"_"+mats.Count+".mat";string p=Root+"Materials/"+filename;
   var old=AssetDatabase.LoadAssetAtPath<Material>(p);if(old){EditorUtility.CopySerialized(m,old);UnityEngine.Object.DestroyImmediate(m);m=old;EditorUtility.SetDirty(m);}else AssetDatabase.CreateAsset(m,p);
   mats[key]=m;return m;
  }
  internal static void ConvertRenderers(GameObject g,bool infected=false,bool player=false,int variant=0){
   foreach(var r in g.GetComponentsInChildren<Renderer>(true)){
    bool particle=r is ParticleSystemRenderer;var arr=r.sharedMaterials;
    for(int i=0;i<arr.Length;i++){
     Color? tint=null;
     if(arr[i]&&!particle&&(infected||player)){
      string n=arr[i].name.ToLowerInvariant();
      if(n.Contains("skin"))tint=infected?new Color(.49f,.57f,.34f):new Color(.78f,.57f,.38f);
      else if(r.name=="Shirt"||n.Contains("white"))tint=infected?(variant==0?new Color(.51f,.44f,.28f):new Color(.38f,.20f,.17f)):new Color(.15f,.46f,.54f);
      else if(r.name=="Pants"||n.Contains("gray"))tint=infected?new Color(.22f,.27f,.25f):new Color(.16f,.19f,.24f);
     }
     arr[i]=Convert(arr[i],particle,tint);
    }
    r.sharedMaterials=arr;if(particle&&arr.Length==0)r.enabled=false;if(!particle){r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
   }
  }
  internal static void LimitParticles(GameObject g,bool gun=false){foreach(var ps in g.GetComponentsInChildren<ParticleSystem>(true)){
   var main=ps.main;main.playOnAwake=false;main.loop=false;main.maxParticles=gun?80:120;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.stopAction=ParticleSystemStopAction.None;
   if(ps.name=="Shells")main.startLifetime=1.8f;
   if(gun&&ps.name=="MuzzleFlare")main.startLifetime=.065f;
   var trails=ps.trails;trails.enabled=false;
   var collision=ps.collision;collision.enabled=false;
  }}
  internal static GameObject Save(GameObject g,string name){g.name=name;var result=PrefabUtility.SaveAsPrefabAsset(g,Root+"Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(g);return result;}
  static GameObject Actor(string name,string source,RuntimeAnimatorController controller,bool player,int variant){
   var original=Load(source);var animator=original.GetComponentsInChildren<Animator>(true).First(a=>a.isHuman);
   var root=new GameObject(name);var model=UnityEngine.Object.Instantiate(animator.gameObject,root.transform);model.name="Animated model";
   Strip(model,false);if(!player){foreach(var t in model.GetComponentsInChildren<Transform>(true))if(t&&(t.name=="Sword"||t.name=="WeaponAttachmentContainer"||t.name=="WeaponAttachment"))UnityEngine.Object.DestroyImmediate(t.gameObject);}
   model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one*1.2f;
   var a=model.GetComponent<Animator>();a.runtimeAnimatorController=controller;a.applyRootMotion=false;
   ConvertRenderers(model,!player,player,variant);
   if(player){
    var weapon=UnityEngine.Object.Instantiate(Load("Prefabs/Weapons/Weapons/LoftAssaultRifle.prefab"),root.transform);weapon.name="Weapon";Strip(weapon);
    foreach(var wa in weapon.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(wa);
    weapon.transform.localPosition=new Vector3(.17f,1.45f,.46f);weapon.transform.localRotation=Quaternion.identity;weapon.transform.localScale=Vector3.one*1.35f;
    var feedback=Named(weapon,"ShootFeedback");if(feedback)UnityEngine.Object.DestroyImmediate(feedback.gameObject);
    ConvertRenderers(weapon);LimitParticles(weapon,true);
   }
   root.AddComponent<LoftActor>();return Save(root,name);
  }
  static AnimationClip Clip(string path){reused.Add(Loft+path);return AssetDatabase.LoadAllAssetsAtPath(Loft+path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));}
  static AnimatorController Controller(string name,bool survivor){
   string path=Root+"Animations/"+name+".controller";AssetDatabase.DeleteAsset(path);
   var c=AnimatorController.CreateAnimatorControllerAtPath(path);c.AddParameter("Speed",AnimatorControllerParameterType.Float);c.AddParameter("Attack",AnimatorControllerParameterType.Trigger);
   var idle=Clip(survivor?"Models/Characters/Suspenders/LoftSuspenders@RifleIdle.fbx":"Models/Characters/Tie/LoftTie@Idle.fbx");
   var walk=Clip(survivor?"Models/Characters/Suspenders/LoftSuspenders@Running.fbx":"Models/Characters/Tie/LoftTie@Walking.fbx");
   var tree=new BlendTree{name="Pack locomotion",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(tree,c);tree.AddChild(idle,0);tree.AddChild(walk,1);
   var sm=c.layers[0].stateMachine;var state=sm.AddState("Locomotion");state.motion=tree;sm.defaultState=state;
   if(survivor){
    c.AddParameter("Rolling",AnimatorControllerParameterType.Bool);
    var roll=sm.AddState("Purchased forward roll");var rollClip=Clip("Models/Characters/Suspenders/LoftSuspenders@SprintingForwardRoll.fbx");roll.motion=rollClip;roll.speed=rollClip.length/.42f;
    var enter=state.AddTransition(roll);enter.hasExitTime=false;enter.duration=.025f;enter.AddCondition(AnimatorConditionMode.If,0,"Rolling");
    var leave=roll.AddTransition(state);leave.hasExitTime=false;leave.duration=.07f;leave.AddCondition(AnimatorConditionMode.IfNot,0,"Rolling");
    var mask=new AvatarMask();for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
    foreach(var part in new[]{AvatarMaskBodyPart.Body,AvatarMaskBodyPart.Head,AvatarMaskBodyPart.LeftArm,AvatarMaskBodyPart.RightArm,AvatarMaskBodyPart.LeftFingers,AvatarMaskBodyPart.RightFingers})mask.SetHumanoidBodyPartActive(part,true);
    string mp=Root+"Animations/RifleUpperBody.mask";AssetDatabase.DeleteAsset(mp);AssetDatabase.CreateAsset(mask,mp);
    c.AddLayer("Rifle stance");var layers=c.layers;layers[1].defaultWeight=1;layers[1].avatarMask=mask;layers[1].blendingMode=AnimatorLayerBlendingMode.Override;c.layers=layers;
    var upper=layers[1].stateMachine.AddState("Purchased rifle pose");upper.motion=idle;layers[1].stateMachine.defaultState=upper;
   }else{
    c.AddParameter("Fast",AnimatorControllerParameterType.Bool);
    var runTree=new BlendTree{name="Runner locomotion",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(runTree,c);runTree.AddChild(idle,0);runTree.AddChild(Clip("Models/Characters/Tie/LoftTie@Running.fbx"),1);
    var running=sm.AddState("Purchased running");running.motion=runTree;
    var beginRun=state.AddTransition(running);beginRun.hasExitTime=false;beginRun.duration=.1f;beginRun.AddCondition(AnimatorConditionMode.If,0,"Fast");
    var endRun=running.AddTransition(state);endRun.hasExitTime=false;endRun.duration=.1f;endRun.AddCondition(AnimatorConditionMode.IfNot,0,"Fast");
    var attack=sm.AddState("Melee");attack.motion=Clip("Models/Characters/Suit/LoftSuit@StandingMeleeAttackHorizontal.fbx");attack.speed=1.8f;
    var t=sm.AddAnyStateTransition(attack);t.hasExitTime=false;t.duration=.1f;t.canTransitionToSelf=false;t.AddCondition(AnimatorConditionMode.If,0,"Attack");
    var back=attack.AddTransition(state);back.hasExitTime=true;back.exitTime=.85f;back.duration=.12f;
   }
   EditorUtility.SetDirty(c);return c;
  }
  static GameObject Effect(string name,string source,float scale){var g=UnityEngine.Object.Instantiate(Load(source));Strip(g);g.transform.localPosition=Vector3.zero;g.transform.localRotation=Quaternion.identity;g.transform.localScale*=scale;ConvertRenderers(g);LimitParticles(g);if(name=="Explosion"){var debris=Named(g,"PhysicsParticles");if(debris)UnityEngine.Object.DestroyImmediate(debris.gameObject);}return Save(g,name);}
  static GameObject Grenade(){var source=Load("Prefabs/Weapons/Projectiles/LoftGrenade.prefab");var model=source.GetComponentsInChildren<MeshRenderer>(true).First().gameObject;var g=UnityEngine.Object.Instantiate(model);Strip(g,false);g.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);g.transform.localScale=Vector3.one;ConvertRenderers(g);Fit(g,.3f);return Save(g,"Grenade");}
  static GameObject Bullet(){var source=Load("Prefabs/Weapons/Projectiles/LoftAssaultRifleBullet.prefab");var root=new GameObject("Bullet");var child=UnityEngine.Object.Instantiate(Named(source,"BulletParticles").gameObject,root.transform);Strip(child);child.transform.localPosition=Vector3.zero;child.transform.localRotation=Quaternion.identity;ConvertRenderers(root);foreach(var ps in root.GetComponentsInChildren<ParticleSystem>()){var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=24;var trails=ps.trails;trails.enabled=false;
   var collision=ps.collision;collision.enabled=false;}return Save(root,"Bullet");}
  static void Fit(GameObject g,float size){var rr=g.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)).ToArray();if(rr.Length==0)return;var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);float max=Mathf.Max(b.size.x,b.size.y,b.size.z);g.transform.localScale*=size/max;b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);g.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);}
  internal static GameObject Prop(Transform parent,string source,Vector3 position,float size,float yaw=0){var wrapper=new GameObject("Pack prop "+Path.GetFileNameWithoutExtension(source));wrapper.transform.SetParent(parent,false);var g=UnityEngine.Object.Instantiate(Load(source),wrapper.transform);Strip(g,false);foreach(var a in g.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);g.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);g.transform.localScale=Vector3.one;ConvertRenderers(g);Fit(g,size);bool solid=source.Contains("LoftSofa")||source.Contains("LoftDesk.prefab")||source.Contains("LoftBookCase")||source.Contains("LoftCoffeeTable")||source.Contains("LoftAmmoCrate");
   if(solid){var rr=g.GetComponentsInChildren<Renderer>();if(rr.Length>0){var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);wrapper.gameObject.layer=8;var box=wrapper.AddComponent<BoxCollider>();box.center=b.center;box.size=b.size;}}
   wrapper.transform.position=position;wrapper.transform.rotation=Quaternion.Euler(0,yaw,0);foreach(var t in wrapper.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic);return wrapper;}
  static void DressScene(){
   var scene=EditorSceneManager.OpenScene("Assets/ZombieSurvival/Scenes/DeadDistrict.unity");
   var previous=GameObject.Find("Loft3D visual dressing");if(previous)UnityEngine.Object.DestroyImmediate(previous);
   var dressing=new GameObject("Loft3D visual dressing").transform;dressing.SetParent(GameObject.Find("Abandoned district").transform,false);
   foreach(var g in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>()).ToArray()){
    if(g.name=="Abandoned building"){var renderer=g.GetComponent<Renderer>();if(renderer)renderer.enabled=false;var collider=g.GetComponent<Collider>();if(collider)collider.enabled=false;Cutaway(dressing,g.position);}
    if(g.name=="Roof"||g.name=="Window"){var renderer=g.GetComponent<Renderer>();if(renderer)renderer.enabled=false;}
    if(g.name=="Car chassis"){
     var r=g.GetComponent<Renderer>();if(r)r.enabled=false;
     Prop(dressing,"Prefabs/Props/LoftCar.prefab",new Vector3(g.position.x,0,g.position.z),4.4f,90);
    }
    if(g.name=="Car cabin"){var r=g.GetComponent<Renderer>();if(r)r.enabled=false;}
    if(g.name=="Shipping crate"){var r=g.GetComponent<Renderer>();if(r)r.enabled=false;Prop(dressing,"Models/WeaponCrate/LoftWeaponCrate.fbx",new Vector3(g.position.x,0,g.position.z),1.8f);}
   }
   var curb=BlockVisuals.MakeMaterial("Sidewalk slabs",new Color(.43f,.45f,.42f));var paint=BlockVisuals.MakeMaterial("Faded crosswalk",new Color(.62f,.60f,.48f));
   for(int side=-1;side<=1;side+=2){
    BlockVisuals.Shape("Raised sidewalk",PrimitiveType.Cube,dressing,new Vector3(side*20,-.015f,0),new Vector3(7,.08f,72),curb);
    for(int j=-5;j<=5;j++)BlockVisuals.Shape("Sidewalk seam",PrimitiveType.Cube,dressing,new Vector3(side*20,.029f,j*6),new Vector3(7,.006f,.05f),BlockVisuals.MakeMaterial("Slab seam "+side+" "+j,new Color(.26f,.29f,.28f)));
    for(int z=-24;z<=24;z+=24){
     // Door frames are generated with the interactive entrance.

     Prop(dressing,"Prefabs/Props/LoftFurniture/LoftPlant.prefab",new Vector3(side*20,0,z+4),2.1f);
     Prop(dressing,"Prefabs/Props/LoftFurniture/LoftLamp.prefab",new Vector3(side*18,0,z-5),2.7f);
    }
   }
   for(int i=0;i<7;i++)BlockVisuals.Shape("Crosswalk stripe",PrimitiveType.Cube,dressing,new Vector3(-5+i*1.7f,.021f,-5),new Vector3(.65f,.025f,3.2f),paint);
   Prop(dressing,"Prefabs/Props/LoftFurniture/LoftSofa.prefab",new Vector3(-18,0,-9),2.8f,90);
   Prop(dressing,"Prefabs/Props/LoftFurniture/LoftCoffeeTable.prefab",new Vector3(-16,0,-9),1.4f);
   Prop(dressing,"Prefabs/Props/LoftFurniture/LoftBookCase.prefab",new Vector3(-22,0,-12),2.8f,90);
   Prop(dressing,"Prefabs/Props/LoftFurniture/LoftDesk.prefab",new Vector3(19,0,12),2.8f,-90);
   Prop(dressing,"Prefabs/Props/LoftFurniture/LoftDeskChair.prefab",new Vector3(17.5f,0,12),1.3f,70);
   Prop(dressing,"Models/Ammo/LoftAmmoCrate.fbx",new Vector3(15,0,8),1.1f);
   var camera=Camera.main;camera.allowHDR=true;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
   var volumeObject=GameObject.Find("Atmosphere");if(volumeObject)UnityEngine.Object.DestroyImmediate(volumeObject);volumeObject=new GameObject("Atmosphere");var volume=volumeObject.AddComponent<Volume>();volume.isGlobal=true;
   volume.sharedProfile=AtmosphereProfile();
   QualitySettings.shadows=UnityEngine.ShadowQuality.All;
   var light=GameObject.Find("Late afternoon").GetComponent<Light>();light.intensity=1.35f;light.shadows=LightShadows.Soft;
   RenderSettings.ambientLight=new Color(.48f,.54f,.58f);
   var pipeline=GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;if(pipeline){var serialized=new SerializedObject(pipeline);serialized.FindProperty("m_SoftShadowsSupported").boolValue=true;serialized.ApplyModifiedProperties();pipeline.shadowDistance=42;EditorUtility.SetDirty(pipeline);}
   var surface=GameObject.Find("Abandoned district").GetComponent<NavMeshSurface>();surface.BuildNavMesh();string navPath="Assets/ZombieSurvival/Settings/DistrictNavMesh-V2.asset";AssetDatabase.DeleteAsset(navPath);AssetDatabase.CreateAsset(surface.navMeshData,navPath);
   EditorSceneManager.SaveScene(scene);
   Debug.Log("PACK_SCENE_DRESSING props="+dressing.childCount);
  }
  static void Wall(Transform parent,Vector3 position,Vector3 size){
   var wrapper=new GameObject("Loft wall module");wrapper.transform.SetParent(parent,false);
   var g=UnityEngine.Object.Instantiate(Load("Prefabs/Walls/WallThickLong.prefab"),wrapper.transform);Strip(g,false);g.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);g.transform.localScale=Vector3.one;ConvertRenderers(g);
   var rr=g.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);
   g.transform.localScale=Vector3.Scale(g.transform.localScale,new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z));b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);g.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
   wrapper.gameObject.layer=8;var box=wrapper.AddComponent<BoxCollider>();box.center=Vector3.up*size.y*.5f;box.size=size;wrapper.transform.position=position;
   foreach(var t in wrapper.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic);
  }
  static void Cutaway(Transform parent,Vector3 center){
   center.y=0;int side=center.x<0?-1:1;
   var floor=BlockVisuals.MakeMaterial("Exposed room floor",new Color(.42f,.46f,.43f));var seams=BlockVisuals.MakeMaterial("Interior tile seams",new Color(.34f,.38f,.35f));
   BlockVisuals.Shape("Room floor",PrimitiveType.Cube,parent,center+Vector3.up*.012f,new Vector3(13,.025f,15),floor);
   for(int i=-2;i<=2;i++)BlockVisuals.Shape("Interior tile joint",PrimitiveType.Cube,parent,center+new Vector3(i*2.5f,.028f,0),new Vector3(.035f,.008f,15),seams);
   for(int i=-2;i<=2;i++)BlockVisuals.Shape("Interior tile joint",PrimitiveType.Cube,parent,center+new Vector3(0,.028f,i*2.5f),new Vector3(13,.008f,.035f),seams);
   Wall(parent,center+new Vector3(side*6.5f,0,0),new Vector3(.4f,2.3f,15));
   Wall(parent,center+new Vector3(0,0,-7.5f),new Vector3(13,2.3f,.4f));Wall(parent,center+new Vector3(0,0,7.5f),new Vector3(13,2.3f,.4f));
   Wall(parent,center+new Vector3(-side*6.5f,0,-4.45f),new Vector3(.4f,1.5f,6.1f));Wall(parent,center+new Vector3(-side*6.5f,0,4.45f),new Vector3(.4f,1.5f,6.1f));
   Prop(parent,"Prefabs/Props/LoftFurniture/LoftDesk.prefab",center+new Vector3(side*2.2f,0,3.5f),2.6f,side*90);
   Prop(parent,"Prefabs/Props/LoftFurniture/LoftDeskChair.prefab",center+new Vector3(side*.5f,0,3.5f),1.3f,-side*90);
   Prop(parent,"Prefabs/Props/LoftFurniture/LoftBookCase.prefab",center+new Vector3(side*4.5f,0,-4),2.8f,-side*90);
   Prop(parent,"Prefabs/Props/LoftFurniture/LoftSofa.prefab",center+new Vector3(-side*2,0,-4),2.8f,side*90);
   Prop(parent,"Prefabs/Props/LoftFurniture/LoftCoffeeTable.prefab",center+new Vector3(-side*3,0,-1),1.5f);
   Prop(parent,"Prefabs/Props/LoftFurniture/LoftPlant.prefab",center+new Vector3(side*4.6f,0,5.5f),1.8f);
   Prop(parent,"Models/Ammo/LoftAmmoCrate.fbx",center+new Vector3(side*4.5f,0,0),1.1f);
  }
  static VolumeProfile AtmosphereProfile(){
   string path=Root+"Atmosphere.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
   if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
   profile.components.RemoveAll(c=>c==null);
   if(profile.components.Count==0){var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.16f);bloom.threshold.Override(1.1f);var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);var color=profile.Add<ColorAdjustments>(true);color.postExposure.Override(.15f);color.contrast.Override(8);color.saturation.Override(-5);}
   foreach(var component in profile.components){if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,profile);EditorUtility.SetDirty(component);}
   EditorUtility.SetDirty(profile);return profile;
  }
  public static void RepairAtmosphereAndBuild(){AtmosphereProfile();AssetDatabase.SaveAssets();PrototypeSetup.Build();}
  public static void PrepareAndBuild(){Prepare();PrototypeSetup.Build();}
  public static void PrepareAndExportIOS(){Prepare();MobileBuild.ExportIOS();}
 }
}
