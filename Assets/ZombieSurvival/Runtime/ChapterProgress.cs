using System;
using UnityEngine;
namespace DeadDistrict {
 public static class ChapterProgress {
  [Serializable] public struct Record {public int clears,bestKills;public float bestSeconds;}
  internal static bool IsolatedValidation;
  static readonly System.Collections.Generic.Dictionary<string,Record> isolated=new System.Collections.Generic.Dictionary<string,Record>();
  static string Key(string id)=>"DeadDistrict.Chapter."+id+".v1";
  public static Record Read(string id){if(IsolatedValidation)return isolated.TryGetValue(id,out var test)?test:new Record();try{return JsonUtility.FromJson<Record>(PlayerPrefs.GetString(Key(id),"{}"));}catch{return new Record();}}
  public static void Complete(string id,float seconds,int kills){var r=Read(id);r.clears++;r.bestKills=Mathf.Max(r.bestKills,kills);if(r.bestSeconds<=0||seconds<r.bestSeconds)r.bestSeconds=seconds;if(IsolatedValidation){isolated[id]=r;return;}PlayerPrefs.SetString(Key(id),JsonUtility.ToJson(r));PlayerPrefs.Save();}
 }
}
