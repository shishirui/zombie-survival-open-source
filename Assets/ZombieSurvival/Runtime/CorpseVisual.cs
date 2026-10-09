using UnityEngine;
namespace DeadDistrict {
 public sealed class CorpseVisual : MonoBehaviour {
  public Transform[] bones;
  public Rigidbody[] bodies;
  public Renderer[] surfaces;
  public Rigidbody hips;Transform animatedModel;CharacterJoint[] joints;Collider[] colliders;readonly System.Collections.Generic.List<Collider> adjacentA=new System.Collections.Generic.List<Collider>(),adjacentB=new System.Collections.Generic.List<Collider>();
  public Collider[] Colliders=>colliders??(colliders=GetComponentsInChildren<Collider>(true));
  public void ConfigurePhysics(){
   joints=GetComponentsInChildren<CharacterJoint>(true);
   foreach(var rb in bodies){rb.solverIterations=12;rb.solverVelocityIterations=4;rb.maxDepenetrationVelocity=2;rb.maxAngularVelocity=6;rb.linearDamping=.6f;rb.angularDamping=1.6f;}
   foreach(var j in joints){j.enableProjection=true;j.projectionDistance=.025f;j.projectionAngle=10;j.enableCollision=false;}
   // Adjacent limbs may overlap at their shared joint; other body parts keep self collision.
   foreach(var a in Colliders)foreach(var b in Colliders)if(a!=b){bool adjacent=a.attachedRigidbody==b.attachedRigidbody;foreach(var j in joints)if((j.GetComponent<Rigidbody>()==a.attachedRigidbody&&j.connectedBody==b.attachedRigidbody)||(j.GetComponent<Rigidbody>()==b.attachedRigidbody&&j.connectedBody==a.attachedRigidbody))adjacent=true;if(adjacent){adjacentA.Add(a);adjacentB.Add(b);}}
  }
  public bool Grounded{get{float scale=transform.lossyScale.x;var head=bones[(int)HumanBodyBones.Head];return Physics.Raycast(hips.position+Vector3.up*.05f,Vector3.down,.65f*scale,(1<<0)|(1<<8))&&Mathf.Abs(head.position.y-hips.position.y)<.6f*scale;}}
  public bool Resting{get{if(!Grounded)return false;foreach(var rb in bodies)if(rb.linearVelocity.sqrMagnitude>.04f||rb.angularVelocity.sqrMagnitude>.12f)return false;return true;}}
  public void Freeze(){foreach(var rb in bodies){if(!rb.isKinematic){rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;}rb.isKinematic=true;rb.detectCollisions=false;rb.interpolation=RigidbodyInterpolation.None;}}
  public void CopyPose(LoftActor source){
   if(!animatedModel)animatedModel=GetComponentInChildren<Animator>(true).transform;
   animatedModel.localPosition=source.Body.localPosition;animatedModel.localRotation=source.Body.localRotation;animatedModel.localScale=source.Body.localScale;
   for(int i=0;i<bones.Length;i++)if(bones[i]){var b=source.Bone(i);if(b){bones[i].localPosition=b.localPosition;bones[i].localRotation=b.localRotation;bones[i].localScale=b.localScale;}}
   // Refit anchors to this model and its current animated pose before releasing physics.
   // Copying anchors from a different rig caused a visible snap and stretched limbs on death.
   foreach(var j in joints)if(j.connectedBody){j.autoConfigureConnectedAnchor=false;j.connectedAnchor=j.connectedBody.transform.InverseTransformPoint(j.transform.TransformPoint(j.anchor));}
   source.CopyAppearance(surfaces);
  }
  public void Launch(Vector3 impulse){
   Vector3 horizontal=Vector3.ProjectOnPlane(impulse,Vector3.up);bool blast=impulse.y>1.5f;
   Vector3 velocity=Vector3.ClampMagnitude(horizontal,blast?4.5f:2.4f)+Vector3.up*Mathf.Min(impulse.y,blast?2.1f:.25f);
   Vector3 direction=horizontal.sqrMagnitude>.01f?horizontal.normalized:transform.forward;
   Vector3 spin=Vector3.Cross(Vector3.up,direction)*(blast?2.6f:2.1f);Vector3 pivot=hips.worldCenterOfMass;
   foreach(var rb in bodies){rb.isKinematic=false;rb.detectCollisions=true;rb.linearVelocity=velocity+Vector3.Cross(spin,rb.worldCenterOfMass-pivot);rb.angularVelocity=spin;rb.WakeUp();}
   for(int i=0;i<adjacentA.Count;i++)Physics.IgnoreCollision(adjacentA[i],adjacentB[i],true);
  }
 }
}
