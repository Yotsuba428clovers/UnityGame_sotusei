var report = new System.Text.StringBuilder();
report.AppendLine("Playing="+UnityEditor.EditorApplication.isPlaying+" Scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
foreach(var path in new[]{"Assets/animetions/Stone_Attack_Ani.fbx","Assets/animetions/Stone_walking_Ani.fbx"}) {
report.AppendLine(path);
foreach(var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)) if(a is UnityEngine.AnimationClip c && !c.name.StartsWith("__")) {
report.AppendLine(c.name+" length="+c.length);
foreach(var b in UnityEditor.AnimationUtility.GetCurveBindings(c)) report.AppendLine(b.path+" "+b.propertyName);
}}
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Stone_Prefab.prefab");
foreach(var t in prefab.GetComponentsInChildren<UnityEngine.Transform>(true)) report.AppendLine("PREFAB "+UnityEditor.AnimationUtility.CalculateTransformPath(t,prefab.transform)+" rot="+t.localEulerAngles+" pos="+t.localPosition);
return report.ToString();
