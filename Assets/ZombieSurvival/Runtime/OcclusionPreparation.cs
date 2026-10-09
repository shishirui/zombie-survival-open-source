using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace DeadDistrict {
 // Exercise the actual transparent meshes/materials before combat, including cover outside
 // the opening camera view. Metal needs the vertex layout and render-target state, so a
 // ShaderVariantCollection.WarmUp alone is insufficient. No runtime material copies.
 public static class OcclusionPreparation {
  public static int PreparedSurfaces {get;private set;}
  public static double Milliseconds {get;private set;}
  public static bool Succeeded {get;private set;}
  public static void Prepare(Camera camera){
   PreparedSurfaces=0;Milliseconds=0;Succeeded=false;
   if(!camera||!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline))return;
   var watch=Stopwatch.StartNew();var root=new GameObject("Temporary occlusion preparation");
   RenderTexture target=null;var oldTarget=camera.targetTexture;bool oldCulling=camera.useOcclusionCulling;
   try{
    var request=new UniversalRenderPipeline.SingleCameraRequest();
    if(!RenderPipeline.SupportsRenderRequest(camera,request))return;
    var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",new Color(1,1,1,.5f));
    foreach(var cover in UnityEngine.Object.FindObjectsByType<StreetOcclusion>(FindObjectsSortMode.None)){
     foreach(var surface in cover.surfaces){
      if(!surface.renderer||!surface.renderer.enabled||!surface.renderer.gameObject.activeInHierarchy)continue;
      var filter=surface.renderer.GetComponent<MeshFilter>();
      if(!filter||!filter.sharedMesh||surface.translucent==null||surface.translucent.Length==0)continue;
      var mesh=filter.sharedMesh;
      var proxy=new GameObject("Warm transparent cover",typeof(MeshFilter),typeof(MeshRenderer));proxy.transform.SetParent(root.transform,false);
      proxy.layer=surface.renderer.gameObject.layer;proxy.GetComponent<MeshFilter>().sharedMesh=mesh;
      // Place every real mesh in the main camera's frustum; alpha stays nonzero so the
      // driver cannot skip the transparent draw. The image is never presented on screen.
      float scale=.35f/Mathf.Max(.01f,mesh.bounds.size.magnitude);
      proxy.transform.rotation=surface.renderer.transform.rotation;proxy.transform.localScale=Vector3.one*scale;
      proxy.transform.position=camera.transform.position+camera.transform.forward*(camera.nearClipPlane+2)-proxy.transform.TransformVector(mesh.bounds.center);
      var renderer=proxy.GetComponent<MeshRenderer>();renderer.sharedMaterials=surface.translucent;
      renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=surface.renderer.receiveShadows;
      renderer.lightProbeUsage=surface.renderer.lightProbeUsage;renderer.reflectionProbeUsage=surface.renderer.reflectionProbeUsage;
      renderer.renderingLayerMask=surface.renderer.renderingLayerMask;renderer.SetPropertyBlock(block);PreparedSurfaces++;
     }
    }
    // Match the URP game color/depth formats and MSAA, not a generic ARGB32 thumbnail.
    // A different attachment format would leave Metal's gameplay PSO cold.
    bool hdr=camera.allowHDR&&pipeline.supportsHDR;
    var format=SystemInfo.GetGraphicsFormat(hdr?DefaultFormat.HDR:DefaultFormat.LDR);
    if(hdr&&pipeline.hdrColorBufferPrecision!=HDRColorBufferPrecision._64Bits&&SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32,GraphicsFormatUsage.Blend))format=GraphicsFormat.B10G11R11_UFloatPack32;
    var descriptor=new RenderTextureDescriptor(Mathf.Max(32,camera.pixelWidth),Mathf.Max(32,camera.pixelHeight)){
     graphicsFormat=format,depthStencilFormat=SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil),
     msaaSamples=camera.allowMSAA?pipeline.msaaSampleCount:1,sRGB=QualitySettings.activeColorSpace==ColorSpace.Linear,
     useDynamicScale=camera.allowDynamicResolution
    };
    descriptor.msaaSamples=SystemInfo.GetRenderTextureSupportedMSAASampleCount(descriptor);
    target=RenderTexture.GetTemporary(descriptor);request.destination=target;camera.useOcclusionCulling=false;
    RenderPipeline.SubmitRenderRequest(camera,request);
    Succeeded=true;
   }catch(Exception e){UnityEngine.Debug.LogWarning("OCCLUSION_PREPARE_FAILED "+e.Message);}
   finally{
    camera.targetTexture=oldTarget;camera.useOcclusionCulling=oldCulling;
    root.SetActive(false);UnityEngine.Object.Destroy(root);
    if(target)RenderTexture.ReleaseTemporary(target);
    watch.Stop();Milliseconds=watch.Elapsed.TotalMilliseconds;
    UnityEngine.Debug.Log("OCCLUSION_PREPARE success="+Succeeded+" surfaces="+PreparedSurfaces+" ms="+Milliseconds.ToString("F1"));
   }
  }
 }
}
