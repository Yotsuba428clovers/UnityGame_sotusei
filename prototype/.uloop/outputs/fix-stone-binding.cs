var source=System.Linq.Enumerable.First(System.Linq.Enumerable.OfType<UnityEngine.AnimationClip>(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/animetions/Stone_Attack_Ani.fbx")),c=>!c.name.StartsWith("__"));
var clip=new UnityEngine.AnimationClip(); clip.name="Stone Attack Rigged";clip.frameRate=source.frameRate;
foreach(var binding in UnityEditor.AnimationUtility.GetCurveBindings(source)) {var mapped=binding;mapped.path=string.IsNullOrEmpty(binding.path)?"Armature":"Armature/"+binding.path;UnityEditor.AnimationUtility.SetEditorCurve(clip,mapped,UnityEditor.AnimationUtility.GetEditorCurve(source,binding));}
clip.EnsureQuaternionContinuity();
const string dest="Assets/animetions/Stone_Attack_Rigged.anim";
var existing=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(dest);
if(existing==null){UnityEditor.AssetDatabase.CreateAsset(clip,dest);existing=clip;}else{UnityEditor.EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);}
var controller=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/animetors/Stone_Animetor.controller");
var state=System.Linq.Enumerable.First(controller.layers[0].stateMachine.states,s=>s.state.name=="Attack").state;state.motion=existing;UnityEditor.EditorUtility.SetDirty(state);UnityEditor.AssetDatabase.SaveAssets();
var root=UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Prefabs/Stone_Prefab.prefab");
try {var model=root.GetComponentInChildren<UnityEngine.Animator>().gameObject;var bindings=UnityEditor.AnimationUtility.GetCurveBindings(existing);int missing=0;foreach(var b in bindings)if(model.transform.Find(b.path)==null)missing++;if(missing>0)throw new System.Exception("Missing bindings="+missing);return "Remapped curves="+bindings.Length+" missing="+missing+" length="+existing.length;}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
