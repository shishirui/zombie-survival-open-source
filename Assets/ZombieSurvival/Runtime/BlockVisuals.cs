using UnityEngine;
using UnityEngine.Rendering;
namespace DeadDistrict {
 public static class BlockVisuals {
  static readonly System.Collections.Generic.List<Material> runtimeMaterials=new System.Collections.Generic.List<Material>();
  public static void ReleaseRuntimeMaterials(){foreach(var m in runtimeMaterials)if(m)Object.Destroy(m);runtimeMaterials.Clear();}
  public static Material MakeMaterial(string name, Color color) {
   var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
   var m=new Material(shader){name=name};m.SetColor("_BaseColor",color);m.color=color;
   if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.15f);
   if(Application.isPlaying)runtimeMaterials.Add(m);
#if UNITY_EDITOR
   else {System.IO.Directory.CreateDirectory("Assets/ZombieSurvival/Materials");UnityEditor.AssetDatabase.CreateAsset(m,UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/ZombieSurvival/Materials/"+name+".mat"));}
#endif
   return m;
  }
  public static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 size, Material mat, bool solid=false, int layer=0) {
   var g=GameObject.CreatePrimitive(type);g.name=name;g.layer=layer;
   g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;
   g.GetComponent<Renderer>().sharedMaterial=mat;
   if(!solid) {var c=g.GetComponent<Collider>(); c.enabled=false; Object.DestroyImmediate(c);}
   return g;
  }
  public static Transform Human(Transform parent, Material clothes, Material skin, Material dark, bool rifle) {
   var root=new GameObject("Model").transform;root.SetParent(parent,false);
   Shape("Torso",PrimitiveType.Cube,root,new Vector3(0,1.05f,0),new Vector3(.65f,.75f,.4f),clothes);
   Shape("Head",PrimitiveType.Sphere,root,new Vector3(0,1.65f,.025f),Vector3.one*.42f,skin);
   Shape("Left boot",PrimitiveType.Cube,root,new Vector3(-.19f,.3f,0),new Vector3(.24f,.6f,.3f),dark);
   Shape("Right boot",PrimitiveType.Cube,root,new Vector3(.19f,.3f,0),new Vector3(.24f,.6f,.3f),dark);
   Shape("Left arm",PrimitiveType.Cube,root,new Vector3(-.42f,1.05f,.25f),new Vector3(.18f,.2f,.65f),skin);
   Shape("Right arm",PrimitiveType.Cube,root,new Vector3(.42f,1.05f,.25f),new Vector3(.18f,.2f,.65f),skin);
   if(rifle) {
    Shape("Rifle receiver",PrimitiveType.Cube,root,new Vector3(.3f,1.15f,.55f),new Vector3(.18f,.2f,.7f),dark);
    Shape("Rifle barrel",PrimitiveType.Cube,root,new Vector3(.3f,1.16f,1.02f),new Vector3(.075f,.075f,.45f),dark);
    Shape("Backpack",PrimitiveType.Cube,root,new Vector3(0,1.05f,-.3f),new Vector3(.5f,.6f,.24f),dark);
   } return root;
  }
  public static LineRenderer Ring(string name, Transform parent, float radius, Color color, Material mat) {
   var g=new GameObject(name);g.transform.SetParent(parent,false);var l=g.AddComponent<LineRenderer>();
   l.sharedMaterial=mat;l.useWorldSpace=false;l.loop=true;l.positionCount=40;l.widthMultiplier=.055f;
   l.startColor=l.endColor=color;l.shadowCastingMode=ShadowCastingMode.Off;
   for(int i=0;i<40;i++){float a=i*Mathf.PI*2/40;l.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,.055f,Mathf.Sin(a)*radius));} return l;
  }
 }
}
