using UnityEngine;
namespace DeadDistrict {
 // Snapshot the four-legged pose. Settle as one solid body so no humanoid
 // ragdoll or live animator can stretch the dog after death.
 public sealed class HoundCorpse:MonoBehaviour {
  Mesh mesh;MeshRenderer surface;Vector3 origin;Quaternion facing;float born;float side;
  public Vector3 RestPosition=>transform.position;
  public void Drop(SurvivalEnemy enemy){
   if(!mesh){mesh=new Mesh{name="Pooled hound death pose"};gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;surface=gameObject.AddComponent<MeshRenderer>();}
   var source=enemy.Model.GetComponentInChildren<SkinnedMeshRenderer>();source.BakeMesh(mesh,true);
   var vertices=mesh.vertices;var matrix=enemy.Model.worldToLocalMatrix*source.transform.localToWorldMatrix;
   for(int i=0;i<vertices.Length;i++)vertices[i]=matrix.MultiplyPoint3x4(vertices[i])-Vector3.up*.5f;
   mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();surface.sharedMaterials=source.sharedMaterials;
   origin=enemy.Model.position;facing=enemy.Model.rotation;side=enemy.Slot%2==0?1:-1;born=Time.time;gameObject.SetActive(true);Settle(0);
  }
  void Update(){if(gameObject.activeSelf)Settle(Mathf.Clamp01((Time.time-born)/.32f));}
  void Settle(float t){float eased=t*t*(3-2*t);transform.rotation=facing*Quaternion.Euler(0,0,side*85*eased);transform.position=origin;float min=surface.bounds.min.y;transform.position+=Vector3.up*(origin.y-min+.015f);}
  void OnDestroy(){if(mesh)Destroy(mesh);}
 }
}
