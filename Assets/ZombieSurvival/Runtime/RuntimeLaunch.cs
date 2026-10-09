using System;
using System.Runtime.InteropServices;
namespace DeadDistrict {
 public static class RuntimeLaunch {
#if UNITY_IOS && !UNITY_EDITOR
  [DllImport("__Internal")] static extern IntPtr DeadDistrictLaunchArguments();
#endif
  public static string[] Arguments(){
#if UNITY_IOS && !UNITY_EDITOR
   var native=Marshal.PtrToStringUTF8(DeadDistrictLaunchArguments());if(!string.IsNullOrEmpty(native))return native.Split('\u001f');
#endif
   return Environment.GetCommandLineArgs();
  }
 }
}
