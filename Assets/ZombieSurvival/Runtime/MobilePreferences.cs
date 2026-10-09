using System;
using UnityEngine;
namespace DeadDistrict {
 // Device preferences only. Run/checkpoint persistence is a separate feature.
 public static class MobilePreferences {
  [Serializable] public struct Data {public float music,sfx,shake;public int joystick,buttons;public bool reducedFlash,helpSeen;}
  const string Key="DeadDistrict.Mobile.v1";
  static Data data;static bool loaded,dirty;static float changedAt;
  public static event Action Changed;
  public static Data Defaults=>new Data{music=1,sfx=1,shake=1,joystick=0,buttons=1};
  public static Data Current {get{Ensure();return data;}}
  public static float ButtonScale=>Current.buttons==0?.9f:Current.buttons==2?1.15f:1;
  static void Ensure(){if(!loaded)Reload();}
  public static void Reload(){data=Defaults;string json=PlayerPrefs.GetString(Key,"");if(json!=""){try{data=JsonUtility.FromJson<Data>(json);}catch{data=Defaults;}}else{data.music=PlayerPrefs.GetInt("Music",1);data.sfx=PlayerPrefs.GetInt("Sfx",1);}data=Clamp(data);loaded=true;dirty=false;}
  static Data Clamp(Data d){d.music=Finite(d.music,1);d.sfx=Finite(d.sfx,1);d.shake=Finite(d.shake,1);d.joystick=Mathf.Clamp(d.joystick,0,1);d.buttons=Mathf.Clamp(d.buttons,0,2);return d;}
  static float Finite(float n,float fallback)=>float.IsNaN(n)||float.IsInfinity(n)?fallback:Mathf.Clamp01(n);
  public static void Set(Data value){data=Clamp(value);loaded=true;dirty=true;changedAt=Time.unscaledTime;Changed?.Invoke();}
  public static void Reset(){var d=Defaults;d.helpSeen=Current.helpSeen;Set(d);Flush();}
  public static void Tick(){if(dirty&&Time.unscaledTime-changedAt>.4f)Flush();}
  public static void Flush(){if(!dirty)return;PlayerPrefs.SetString(Key,JsonUtility.ToJson(data));PlayerPrefs.SetInt("Music",data.music>0?1:0);PlayerPrefs.SetInt("Sfx",data.sfx>0?1:0);PlayerPrefs.Save();dirty=false;}
 }
}
