using UnityEngine;
var root=GameObject.Find("dai - Central arena");
var f=root.GetComponentInChildren<MeshFilter>();
var c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;
Physics.SyncTransforms();
var heights=new System.Collections.Generic.List<string>();
for(int z=-8;z<=16;z+=4){var row=new System.Collections.Generic.List<string>();for(int x=-12;x<=12;x+=4){RaycastHit hit;row.Add(c.Raycast(new Ray(new Vector3(x,4,z),Vector3.down),out hit,8)?hit.point.y.ToString("F3"):"-");} heights.Add(z+":"+string.Join(",",row));}
Object.Destroy(c);
var follow=Camera.main.GetComponent<CameraFollow>();follow.enabled=false;
Camera.main.transform.SetPositionAndRotation(new Vector3(0,9,-25),Quaternion.Euler(10,0,0));
return string.Join("\n",heights);
