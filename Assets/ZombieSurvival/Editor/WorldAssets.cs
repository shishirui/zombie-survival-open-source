using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
namespace DeadDistrict.Editor {
 public static class WorldAssets {
  const string Loft="Assets/TopDownEngine/Demos/Loft3D/";
  static GameObject Source(string path){VisualUpgrade.reused.Add(path);var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new Exception("World asset missing "+path);return source;}
  static GameObject Model(GameObject source,Transform parent,Vector3 size){var m=UnityEngine.Object.Instantiate(source,parent);VisualUpgrade.Strip(m,false);foreach(var a in m.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);m.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);m.transform.localScale=Vector3.one;VisualUpgrade.ConvertRenderers(m);Fit(m,size);return m;}
  static Bounds Bounds(GameObject g){var rr=g.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)).ToArray();if(rr.Length==0)throw new Exception("World model has no renderers "+g.name);var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
  static void Fit(GameObject g,Vector3 size){var b=Bounds(g);g.transform.localScale=Vector3.Scale(g.transform.localScale,new Vector3(size.x/Mathf.Max(.01f,b.size.x),size.y/Mathf.Max(.01f,b.size.y),size.z/Mathf.Max(.01f,b.size.z)));b=Bounds(g);g.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);}
  static WorldProp Physical(GameObject root,WorldPropKind kind,Vector3 size){root.layer=8;var p=root.AddComponent<WorldProp>();p.Kind=kind;var c=root.AddComponent<BoxCollider>();c.size=size;c.center=Vector3.up*size.y*.5f;var nav=root.AddComponent<NavMeshObstacle>();nav.shape=NavMeshObstacleShape.Box;nav.center=c.center;nav.size=size;nav.carving=true;nav.carveOnlyStationary=true;nav.carvingTimeToStationary=.1f;return p;}
  public static void Prepare(PackVisuals pack){
   var root=new GameObject("Breakable supply crate");Model(Source("Assets/TopDownEngine/Demos/Explodudes/Prefabs/Level/ExplodudesCrate.prefab"),root.transform,new Vector3(1.3f,1.15f,1.3f));CrateMaterial(root);Physical(root,WorldPropKind.SupplyCrate,new Vector3(1.3f,1.15f,1.3f));pack.worldSupplyCrate=VisualUpgrade.Save(root,"WorldSupplyCrate");
   root=new GameObject("Explosive blue crate");Model(Source(Loft+"Prefabs/Props/LoftBlueExplosive1x1.prefab"),root.transform,new Vector3(1.35f,1.2f,1.35f));ExplosiveMaterial(root);Physical(root,WorldPropKind.Explosive,new Vector3(1.35f,1.2f,1.35f));pack.worldExplosive=VisualUpgrade.Save(root,"WorldExplosive");
   var purchased=Source(Loft+"Prefabs/Props/LoftDoor.prefab");var meshes=purchased.GetComponentsInChildren<MeshRenderer>(true);var door=meshes.First(r=>r.name=="Door");var frame=meshes.First(r=>r.name=="Doorway");
   root=new GameObject("Interactive loft door");var p=Physical(root,WorldPropKind.Door,new Vector3(2.8f,2.4f,.22f));
   Model(frame.gameObject,root.transform,new Vector3(3.2f,2.8f,.32f));var leaf=Model(door.gameObject,root.transform,new Vector3(2.8f,2.4f,.18f));var pivot=new GameObject("Door hinge").transform;pivot.SetParent(root.transform,false);pivot.localPosition=new Vector3(-1.4f,0,0);leaf.transform.SetParent(pivot,true);p.DoorPivot=pivot;
   pack.worldDoor=VisualUpgrade.Save(root,"WorldDoor");EditorUtility.SetDirty(pack);AssetDatabase.SaveAssets();Debug.Log("WORLD_ASSETS_READY purchased crate/explosive/hinged door");
  }
  static Material ColorMaterial(string name,Color color){string path="Assets/ZombieSurvival/PackVisuals/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.22f);EditorUtility.SetDirty(m);return m;}
  static void ExplosiveMaterial(GameObject root){var blue=ColorMaterial("ExplosiveBlue",new Color(.04f,.24f,.42f));var band=ColorMaterial("ExplosiveSteel",new Color(.13f,.17f,.19f));var warning=ColorMaterial("ExplosiveWarning",new Color(.95f,.62f,.12f));foreach(var r in root.GetComponentsInChildren<Renderer>())r.sharedMaterial=blue;
   foreach(float x in new[]{-.44f,.44f}){BlockVisuals.Shape("Steel band top",PrimitiveType.Cube,root.transform,new Vector3(x,1.215f,0),new Vector3(.1f,.035f,1.4f),band);foreach(float z in new[]{-.685f,.685f})BlockVisuals.Shape("Steel band side",PrimitiveType.Cube,root.transform,new Vector3(x,.6f,z),new Vector3(.1f,1.2f,.025f),band);}
   foreach(float z in new[]{-.70f,.70f})foreach(float angle in new[]{-40f,40f}){var bar=BlockVisuals.Shape("Explosive warning cross",PrimitiveType.Cube,root.transform,new Vector3(0,.6f,z),new Vector3(.1f,.7f,.025f),warning);bar.transform.localRotation=Quaternion.Euler(0,0,angle);}
  }
  static void CrateMaterial(GameObject root){
   const string texture="Assets/TopDownEngine/Demos/Explodudes/Textures/ExplodudesCrate.png";VisualUpgrade.reused.Add(texture);
   const string path="Assets/ZombieSurvival/PackVisuals/Materials/WorldCrateWood.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));mat.SetColor("_BaseColor",new Color(1,.7f,.43f));mat.SetFloat("_Smoothness",.12f);mat.SetFloat("_Metallic",0);EditorUtility.SetDirty(mat);foreach(var r in root.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;
  }
  static void Prop(Transform parent,string name,Vector3 point,float size,float yaw=0,bool solid=false){var g=VisualUpgrade.Prop(parent,"Prefabs/Props/LoftFurniture/"+name+".prefab",point,size,yaw);if(solid&&!g.GetComponent<Collider>()){var rr=g.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);var box=g.AddComponent<BoxCollider>();box.center=g.transform.InverseTransformPoint(b.center);var local=g.transform.InverseTransformVector(b.size);box.size=new Vector3(Mathf.Abs(local.x),Mathf.Abs(local.y),Mathf.Abs(local.z));g.layer=8;}}
  public static void Decorate(Transform parent){
   // Rebuild room interiors coherently; keep a three-metre entrance-to-reward aisle.
   foreach(var t in parent.Cast<Transform>().ToArray())if(t.name.StartsWith("Pack prop")&&Mathf.Abs(t.position.x)>25)UnityEngine.Object.DestroyImmediate(t.gameObject);
   foreach(int side in new[]{-1,1})foreach(int z in new[]{-24,0,24}){
    Vector3 c=new Vector3(side*31,0,z);Func<float,float,Vector3> at=(x,y)=>c+new Vector3(side*x,0,y);
    if(z==0){
     Prop(parent,"LoftBed",at(2,3.7f),3,side*90,true);Prop(parent,"LoftTvCabinet",at(-2,-4.8f),2.3f,0,true);Prop(parent,"LoftTV",at(-2,-4.8f)+Vector3.up*.85f,1.2f);
     Prop(parent,"LoftSofaLong",at(2,-4.8f),3,90,true);Prop(parent,"LoftLampSmall",at(3.5f,5.7f),1.3f);
     Prop(parent,"LoftKitchenCabinet",at(4,0),2.2f,-side*90,true);Prop(parent,"LoftMicrowave",at(4,0)+Vector3.up*1.1f,.75f,-side*90);
    }else if(z>0){
     Prop(parent,"LoftDeskCorner",at(3,3.8f),3,-side*90,true);Prop(parent,"LoftDeskChair",at(1.2f,3.8f),1.3f,90);
     Prop(parent,"LoftLaptop",at(3,3.8f)+Vector3.up*1.05f,.85f,90);Prop(parent,"LoftBookCase",at(4,-4),3,-side*90,true);
     Prop(parent,"LoftSofaCorner",at(-1,-4.5f),3.4f,0,true);Prop(parent,"LoftCoffeeTable",at(-1,-2.4f),1.4f);
     Prop(parent,"LoftSpeaker",at(4,0),1.5f,90,true);
    }else{
     Prop(parent,"LoftKitchenBar",at(2,3.7f),3.4f,0,true);Prop(parent,"LoftCoffeeMachine",at(2,3.7f)+Vector3.up*1.05f,.85f);
     Prop(parent,"LoftKitchenCabinetUpper",at(4,4)+Vector3.up*1.7f,2.4f,-side*90);
     Prop(parent,"LoftTable",at(0,-4.1f),2.1f,0,true);Prop(parent,"LoftChair",at(-1.7f,-4.1f),1.2f,90);Prop(parent,"LoftChair",at(1.7f,-4.1f),1.2f,-90);
     Prop(parent,"LoftBlender",at(2.8f,3.7f)+Vector3.up*1.05f,.55f);Prop(parent,"LoftKitchenFridge",at(4,-3.7f),2.4f,-side*90,true);
    }
    Prop(parent,"LoftPlant",at(4.5f,6),1.8f);
    var label=new GameObject("Location sign");label.transform.SetParent(parent,false);label.transform.position=new Vector3(side*23.5f,2.5f,z+2);label.transform.rotation=Quaternion.Euler(48,45,0);var text=label.AddComponent<TextMesh>();text.font=Resources.Load<Font>("DeadDistrict/Chinese");text.text=z==0?"居民住宅":z>0?"办公区":"便利商店";text.fontSize=48;text.characterSize=.05f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.5f,.9f,.85f);label.GetComponent<Renderer>().sharedMaterial=text.font.material;
   }
   Debug.Log("WORLD_ROOMS_READY six furnished rooms, clear reward aisles");
  }
 }
}
