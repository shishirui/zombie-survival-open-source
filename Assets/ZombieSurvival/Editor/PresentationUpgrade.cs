using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.AI.Navigation;
namespace DeadDistrict.Editor {
 public static class PresentationUpgrade {
  const string Base="Assets/ZombieSurvival/";
  const string Epic="Assets/Epic Toon FX/";
  const string Loft="Assets/TopDownEngine/Demos/Loft3D/";
  public static void Apply(PackVisuals pack){
   pack.bullet=Projectile();
   pack.impact=Effect("EpicImpact","Prefabs/Combat/Explosions/BulletExplosion/BulletExplosionFire.prefab",.45f,false);
   pack.enemyImpact=Effect("EpicEnemyImpact","Prefabs/Combat/Blood/Green/GreenBloodSplatDirectional.prefab",.95f,false);
   GrenadeEffectsUpgrade.Apply();
   pack.death=Effect("EpicDeath","Prefabs/Combat/Blood/Green/GreenBloodSplatCritical.prefab",.85f,false);
   var muzzle=Effect("EpicMuzzle","Prefabs/Combat/Muzzleflash/BulletMuzzle/BulletMuzzleFire.prefab",.42f,false);
   // Preserve the purchased rifle's shells, replace its original muzzle particles.
   var actor=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(pack.survivor));
   foreach(var ps in actor.GetComponentsInChildren<ParticleSystem>(true).Where(x=>x.name.Contains("Muzzle")).ToArray())if(ps)UnityEngine.Object.DestroyImmediate(ps.gameObject);
   var gun=actor.transform.Find("Weapon");var g=UnityEngine.Object.Instantiate(muzzle,gun);g.name="Epic Muzzle";g.transform.localPosition=new Vector3(0,.18f,.78f);g.transform.localRotation=Quaternion.identity;
   pack.survivor=PrefabUtility.SaveAsPrefabAsset(actor,AssetDatabase.GetAssetPath(pack.survivor));PrefabUtility.UnloadPrefabContents(actor);EditorUtility.SetDirty(pack);AssetDatabase.SaveAssets();
   SupplyAssets.Prepare(pack);WeaponAssets.Prepare(pack);CharacterAssets.Prepare(pack);WorldAssets.Prepare(pack);PrepareAudio();Decorate();Check(pack);PlayerSettings.bundleVersion=MobileBuild.Version;PlayerSettings.productName="末日街区";EditorUtility.SetDirty(pack);
  }
  internal static GameObject Effect(string name,string path,float scale,bool loop){
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(Epic+path);if(!source)throw new Exception("Missing Epic effect "+path);
   VisualUpgrade.reused.Add(Epic+path);var g=UnityEngine.Object.Instantiate(source);VisualUpgrade.Strip(g);g.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);g.transform.localScale=Vector3.one*scale;
   VisualUpgrade.ConvertRenderers(g);VisualUpgrade.LimitParticles(g);
   foreach(var p in g.GetComponentsInChildren<ParticleSystem>(true)){var m=p.main;m.loop=loop;m.startLifetimeMultiplier=Mathf.Min(m.startLifetimeMultiplier,loop?2:1.8f);m.maxParticles=loop?35:90;m.simulationSpace=ParticleSystemSimulationSpace.World;}
   if(name=="EpicEnemyImpact"||name=="EpicDeath"){
    bool critical=name=="EpicDeath";int i=0;
    foreach(var ps in g.GetComponentsInChildren<ParticleSystem>(true)){
     var main=ps.main;main.maxParticles=72;main.startLifetime=new ParticleSystem.MinMaxCurve(critical?.45f:.32f,critical?.85f:.65f);main.startSpeed=new ParticleSystem.MinMaxCurve(3,critical?9:8);main.startSize=new ParticleSystem.MinMaxCurve(critical?.16f:.12f,critical?.4f:.3f);
     var shape=ps.shape;if(shape.enabled){shape.angle=critical?75:45;shape.radius=.12f;}
     var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)(i++==0?(critical?52:38):26))});
    }
    if(!critical)StarImpact(g);
    ImpactPulse(g,"Hot impact core",critical?2.0f:1.25f,.18f,new Color(3,2.7f,1.2f));
    ImpactPulse(g,"Spreading impact halo",critical?3.1f:2.0f,.3f,new Color(.55f,1.6f,.22f,.55f));
   }
   return VisualUpgrade.Save(g,name);
  }
  static void StarImpact(GameObject parent){
   string path=Epic+"Prefabs/Combat/Sword/Hit/SwordHitMini/SwordHitMiniYellow.prefab";var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new Exception("Missing star impact");VisualUpgrade.reused.Add(path);
   var g=UnityEngine.Object.Instantiate(source,parent.transform);g.name="Pack star-shaped bullet impact";g.transform.localPosition=Vector3.zero;g.transform.localRotation=Quaternion.identity;g.transform.localScale=Vector3.one*.55f;VisualUpgrade.Strip(g);VisualUpgrade.ConvertRenderers(g);VisualUpgrade.LimitParticles(g);
   foreach(var ps in g.GetComponentsInChildren<ParticleSystem>(true)){var main=ps.main;main.loop=false;main.maxParticles=24;main.startLifetime=new ParticleSystem.MinMaxCurve(.12f,.28f);main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)(ps.name=="Glow"?1:ps.name=="Sparks"?12:6))});}
  }
  static void ImpactPulse(GameObject parent,string name,float size,float lifetime,Color color){
   var o=new GameObject(name);o.transform.SetParent(parent.transform,false);var ps=o.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=ps.main;main.loop=false;main.playOnAwake=false;main.duration=lifetime;main.startLifetime=lifetime;main.startSpeed=0;main.startSize=size;main.startColor=color;main.maxParticles=1;main.simulationSpace=ParticleSystemSimulationSpace.World;
   var shape=ps.shape;shape.enabled=false;var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,1)});
   var sizeOver=ps.sizeOverLifetime;sizeOver.enabled=true;sizeOver.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.25f),new Keyframe(.35f,1),new Keyframe(1,1.1f)));
   var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=gradient;
   var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Base+"Resources/DeadDistrict/BulletGlow.mat");renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
  }
  static GameObject Projectile(){
   string path="Prefabs/Combat/Missiles/Bullet/BulletSmallFire.prefab";
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(Epic+path);if(!source)throw new Exception("Missing Epic projectile");VisualUpgrade.reused.Add(Epic+path);
   var g=UnityEngine.Object.Instantiate(source);VisualUpgrade.Strip(g);g.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);g.transform.localScale=Vector3.one*.4f;VisualUpgrade.ConvertRenderers(g);VisualUpgrade.LimitParticles(g);
   foreach(var ps in g.GetComponentsInChildren<ParticleSystem>(true)){var main=ps.main;main.loop=true;main.maxParticles=24;main.startLifetime=new ParticleSystem.MinMaxCurve(.06f,.14f);main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=ps.emission;emission.rateOverTimeMultiplier=Mathf.Min(80,emission.rateOverTimeMultiplier);}
   var bullet=VisualUpgrade.Save(g,"EpicBullet");
   string mp=Base+"Resources/DeadDistrict/BulletGlow.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(mat,mp);}
   var original=AssetDatabase.LoadAssetAtPath<Material>(Epic+"Materials/Basics/glow1_ADD.mat");mat.SetTexture("_BaseMap",original.GetTexture("_MainTex"));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",2);mat.SetFloat("_ZWrite",0);mat.SetFloat("_Cull",0);mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(float)BlendMode.One);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;EditorUtility.SetDirty(mat);
   return bullet;
  }
  static void PrepareAudio(){
   string folder=Base+"Resources/DeadDistrict/Audio/";Directory.CreateDirectory(folder);
   var entries=new[]{
    new[]{"Pickup",Loft+"Sounds/LoftCoin.wav"},new[]{"Music",Loft+"Sounds/BackgroundMusic1.wav"},new[]{"Steps",Loft+"Sounds/LoftWalk.wav"},
    new[]{"BattleMusic","Assets/TopDownEngine/Demos/Explodudes/Sounds/ExplodudesSong.wav"},
    new[]{"Dodge",Loft+"Sounds/LoftSwoosh.wav"},new[]{"Shotgun",Loft+"Sounds/LoftShotgun.wav"},new[]{"ShotgunReload",Loft+"Sounds/LoftMechanism.wav"},
    new[]{"Rifle1",Loft+"Sounds/LoftGun1.wav"},new[]{"Rifle2",Epic+"Sound/etfx_shoot_pistol02.wav"},new[]{"Rifle3",Epic+"Sound/etfx_shoot_pistol03.wav"},
    new[]{"Hit1",Loft+"Sounds/LoftHit1.wav"},new[]{"Hit2",Loft+"Sounds/LoftHit2.wav"},new[]{"Hit3",Loft+"Sounds/LoftHit3.wav"},
    new[]{"ShotgunReloadSequence",Base+"Art/Audio/ShotgunReloadSequence.wav"},new[]{"Reload",Loft+"Sounds/LoftReload.wav"},new[]{"Explosion",Base+"Art/Audio/GrenadeLayered.wav"},
    new[]{"Death",Loft+"Sounds/LoftAIDeath.wav"},new[]{"Hurt",Loft+"Sounds/LoftDeath.wav"},new[]{"Horde",Loft+"Sounds/LoftTom.wav"}};
   foreach(var e in entries){File.Copy(e[1],folder+e[0]+".wav",true);VisualUpgrade.reused.Add(e[1]);}
   foreach(var input in new[]{Epic+"Sound/etfx_explosion_grenade.wav",Epic+"Sound/etfx_explosion_rocket.wav",Epic+"Sound/etfx_explosion_nuke.wav",Loft+"Sounds/LoftExplosion.wav"})VisualUpgrade.reused.Add(input);
   AssetDatabase.Refresh();
   foreach(var e in entries){var imp=AssetImporter.GetAtPath(folder+e[0]+".wav") as AudioImporter;var settings=imp.defaultSampleSettings;settings.loadType=e[0].Contains("Music")?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.75f;imp.defaultSampleSettings=settings;imp.forceToMono=!e[0].Contains("Music");imp.SaveAndReimport();}
  }
  static Material Surface(string name,Color color,bool cracks=false){
   Directory.CreateDirectory(Base+"Art");string file=Base+"Art/"+name+".png";var tex=new Texture2D(256,256,TextureFormat.RGB24,false);var rng=new System.Random(name.GetHashCode());var pixels=new Color[256*256];
   for(int y=0;y<256;y++)for(int x=0;x<256;x++){float grain=(float)rng.NextDouble()*.10f-.05f;float n=Mathf.PerlinNoise(x*.035f,y*.035f)*.09f;bool joint=cracks&&(x%64<1||y%64<1);pixels[y*256+x]=new Color(.72f+grain+n,.72f+grain+n,.72f+grain+n)*(joint?.62f:1);}
   tex.SetPixels(pixels);tex.Apply();File.WriteAllBytes(file,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(file);var importer=(TextureImporter)AssetImporter.GetAtPath(file);importer.wrapMode=TextureWrapMode.Repeat;importer.maxTextureSize=256;importer.SaveAndReimport();
   string path=Base+"Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",color);mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(file));mat.SetFloat("_Smoothness",.08f);EditorUtility.SetDirty(mat);return mat;
  }
  static void SetSurface(Renderer r,Material mat,Vector2 tiling){r.sharedMaterial=mat;mat.SetTextureScale("_BaseMap",tiling);}
  static void Decorate(){
   var scene=EditorSceneManager.GetActiveScene();var arena=GameObject.Find("Abandoned district");var parent=GameObject.Find("Loft3D visual dressing").transform;
   var asphalt=Surface("District asphalt",new Color(.31f,.35f,.38f));SetSurface(GameObject.Find("Ground").GetComponent<Renderer>(),asphalt,new Vector2(18,18));
   var pavement=Surface("District paving",new Color(.54f,.58f,.57f),true);pavement.SetTextureScale("_BaseMap",new Vector2(3,12));
   var tile=Surface("District interior",new Color(.58f,.49f,.39f),true);tile.SetTextureScale("_BaseMap",new Vector2(4,4));
   foreach(var r in arena.GetComponentsInChildren<Renderer>(true)){
    if(r.name=="Raised sidewalk")r.sharedMaterial=pavement;
    if(r.name=="Room floor")r.sharedMaterial=tile;
    foreach(var m in r.sharedMaterials)if(m){if(m.name.Contains("WallMaterialBlue")){m.SetColor("_BaseColor",new Color(.29f,.42f,.45f));EditorUtility.SetDirty(m);}if(m.name.Contains("orangeSteel")){m.SetColor("_BaseColor",new Color(.34f,.40f,.35f));EditorUtility.SetDirty(m);}}
   }
   // Furnish each block as a different place rather than repeating the same office.
   for(int side=-1;side<=1;side+=2){
    var center=new Vector3(side*31,0,0);
    Prop(parent,"LoftBed",center+new Vector3(0,0,1),3.2f,90);
    Prop(parent,"LoftKitchenFridge",center+new Vector3(side*4,0,5.2f),2.4f,-side*90);
    Prop(parent,"LoftKitchenSink",center+new Vector3(side*4,0,1.8f),2.2f,-side*90);
    Prop(parent,"LoftKitchenCabinet",center+new Vector3(side*4,0,-1),2.2f,-side*90);
    Prop(parent,"LoftKitchenStove",center+new Vector3(side*4,0,-4.5f),2.2f,-side*90);
    Prop(parent,"LoftCoffeeMachine",new Vector3(side*20,0,14),1.8f,90);
    Prop(parent,"LoftTable",new Vector3(side*19.5f,0,10),2.1f,0);
    Prop(parent,"LoftBarChair",new Vector3(side*19.5f,0,8),1.4f,0);
    Prop(parent,"LoftLaptop",center+new Vector3(side*2.2f,1.0f,3.5f),.9f,-side*90);
   }
   // A checkpoint with planted islands breaks up the formerly empty intersection.
   var curb=BlockVisuals.MakeMaterial("Planter concrete",new Color(.30f,.35f,.34f));
   for(int side=-1;side<=1;side+=2){
    var pos=new Vector3(side*11,0,side*3);BlockVisuals.Shape("Checkpoint planter",PrimitiveType.Cube,parent,pos+Vector3.up*.32f,new Vector3(2.5f,.64f,3.3f),curb,true,8);
    Prop(parent,"LoftPlant",pos+Vector3.up*.6f,2.4f,0);
    Prop(parent,"LoftLamp",new Vector3(side*15,0,0),3.4f,0);
   }
   var hazard=BlockVisuals.MakeMaterial("Checkpoint hazard",new Color(.83f,.61f,.23f));
   foreach(var barricade in arena.GetComponentsInChildren<Transform>().Where(t=>t.name=="Concrete barricade")){
    for(int j=-2;j<=2;j++){var stripe=BlockVisuals.Shape("Barricade warning stripe",PrimitiveType.Cube,parent,barricade.position+new Vector3(j*.32f,.02f,-.465f),new Vector3(.11f,1.15f,.025f),hazard);stripe.transform.rotation=Quaternion.Euler(0,0,-24);}
   }
   Sign(parent,"隔离区",new Vector3(8.5f,2.1f,6),new Color(.98f,.72f,.28f));Sign(parent,"物资补给",new Vector3(-19,2.3f,16),new Color(.45f,.83f,.81f));
   var smoke=Effect("EpicSmoke","Prefabs/Environment/Smoke/Dark/SmokeDarkSoft.prefab",.6f,true);
   var fire=Effect("EpicFire","Prefabs/Environment/Fire/Cartoon/Torch/ToonFireTorchRed.prefab",.45f,true);
   for(int i=0;i<2;i++){
    Vector3 pos=i==0?new Vector3(10,1.0f,19):new Vector3(-14,1,-21);
    foreach(var source in new[]{smoke,fire}){var fx=UnityEngine.Object.Instantiate(source,parent);fx.transform.position=pos;foreach(var ps in fx.GetComponentsInChildren<ParticleSystem>(true)){var m=ps.main;m.playOnAwake=true;}}
   }
   var light=GameObject.Find("Late afternoon").GetComponent<Light>();light.transform.rotation=Quaternion.Euler(48,-35,0);light.color=new Color(1,.84f,.65f);light.intensity=1.3f;
   RenderSettings.ambientLight=new Color(.48f,.57f,.65f);RenderSettings.fogStartDistance=50;RenderSettings.fogEndDistance=95;
   var camera=Camera.main;camera.orthographic=true;camera.orthographicSize=9.5f;camera.transform.SetPositionAndRotation(new Vector3(-14,21,-14),Quaternion.Euler(48,45,0));
   WorldAssets.Decorate(parent);
   var surface=arena.GetComponent<NavMeshSurface>();surface.BuildNavMesh();string path=Base+"Settings/DistrictNavMesh-V3.asset";AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(surface.navMeshData,path);
   foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{Base+"PackVisuals/Materials"})){
    var matPath=AssetDatabase.GUIDToAssetPath(guid);var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);var label=Path.GetFileNameWithoutExtension(matPath);
    if(label.Contains("LoftWallMaterialBlue"))mat.SetColor("_BaseColor",new Color(.29f,.42f,.45f));
    else if(label.Contains("LoftWallTopMaterial"))mat.SetColor("_BaseColor",new Color(.65f,.66f,.60f));
    else if(label.Contains("orangeSteel"))mat.SetColor("_BaseColor",new Color(.34f,.40f,.35f));
    else continue;EditorUtility.SetDirty(mat);
   }
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Debug.Log("PRESENTATION_SCENE_READY diagonal=45 pitch=48");
  }
  static void Prop(Transform parent,string name,Vector3 position,float size,float yaw){VisualUpgrade.Prop(parent,"Prefabs/Props/LoftFurniture/"+name+".prefab",position,size,yaw);}
  static void Sign(Transform parent,string text,Vector3 position,Color color){var g=new GameObject("Street sign "+text);g.transform.SetParent(parent,false);g.transform.position=position;g.transform.rotation=Quaternion.Euler(48,45,0);var tm=g.AddComponent<TextMesh>();tm.text=text;tm.font=Resources.Load<Font>("DeadDistrict/Chinese");tm.fontSize=48;tm.characterSize=.055f;tm.anchor=TextAnchor.MiddleCenter;tm.color=color;g.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;}
  static void Check(PackVisuals pack){
   var font=Resources.Load<Font>("DeadDistrict/Chinese");foreach(char c in "末日街区生存移动击杀生命步枪自动射击手雷换弹尸潮感染者暂停继续游戏重新开始音乐开启关闭物资补给霰弹枪箱切换寻找获得近距清怪翻滚冷却就绪疾行者重装感染者")if(!font.HasCharacter(c))throw new Exception("Missing Chinese glyph "+c);
   foreach(var prefab in new[]{pack.survivor,pack.impact,pack.enemyImpact,pack.explosion,pack.death,pack.bullet,pack.shotgunWeapon,pack.shotgunCrate}){if(!prefab)throw new Exception("Pack prefab reference missing after preparation");foreach(var r in prefab.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)if(m&&(!m.shader||!m.shader.name.StartsWith("Universal Render Pipeline")))throw new Exception("Unconverted material "+m.name);}
   Debug.Log("PRESENTATION_ASSET_CHECK_PASS Chinese font and URP effects");
  }
 }
}
