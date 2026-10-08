var sb=new System.Text.StringBuilder();foreach(var name in new[]{"Skeleton","Stone"}){
var path="Assets/animetions/"+name+"_Dead_Ani.fbx";
foreach(var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))if(a is UnityEngine.AnimationClip c&&!c.name.StartsWith("__")){sb.AppendLine(name+" clip="+c.name+" length="+c.length+" loop="+c.isLooping);foreach(var b in UnityEditor.AnimationUtility.GetCurveBindings(c))if(b.propertyName=="m_LocalRotation.x")sb.AppendLine(b.path);}
var p=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/"+name+"_Prefab.prefab");var anim=p.GetComponentInChildren<UnityEngine.Animator>(true);sb.AppendLine("Animator="+UnityEditor.AnimationUtility.CalculateTransformPath(anim.transform,p.transform));foreach(var t in anim.GetComponentsInChildren<UnityEngine.Transform>(true))if(t.name=="Hips"||t.name=="Armature")sb.AppendLine(UnityEditor.AnimationUtility.CalculateTransformPath(t,anim.transform));}
return sb.ToString();
