var lines=new System.Collections.Generic.List<string>();
foreach(var id in new[]{"dai","a","b","c","d","e"}) {
var path="Assets/models/"+id+".fbx";var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
if(asset==null){lines.Add(id+" missing");continue;}
var go=UnityEngine.Object.Instantiate(asset);
try {foreach(var r in go.GetComponentsInChildren<UnityEngine.Renderer>(true)) {var mats=System.Array.ConvertAll(r.sharedMaterials,m=>m==null?"null":m.name+":"+(m.mainTexture==null?"-":UnityEditor.AssetDatabase.GetAssetPath(m.mainTexture))); lines.Add(id+" "+r.name+" "+r.bounds+" mats="+string.Join(",",mats));}
foreach(var t in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)) if(t is UnityEngine.Texture) lines.Add(id+" embedded "+t.name);
} finally {UnityEngine.Object.DestroyImmediate(go);}
}
return string.Join("\n",lines);
