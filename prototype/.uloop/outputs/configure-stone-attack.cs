if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Stop Play Mode before editing prefab assets");
var path = "Assets/animetions/Stone_Attack_Ani.fbx";
var importer = (UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(path);
var clips = importer.clipAnimations;
if (clips.Length == 0) clips = importer.defaultClipAnimations;
foreach (var clip in clips) { clip.loopTime = false; clip.loopPose = false; }
importer.clipAnimations = clips;
importer.SaveAndReimport();
var attack = System.Linq.Enumerable.FirstOrDefault(System.Linq.Enumerable.OfType<UnityEngine.AnimationClip>(UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)), c => !c.name.StartsWith("__preview__"));
if (attack == null) throw new System.Exception("Attack clip missing");
var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/animetors/Stone_Animetor.controller");
var state = System.Linq.Enumerable.First(controller.layers[0].stateMachine.states, s => s.state.name == "Attack").state;
state.motion = attack;
UnityEditor.EditorUtility.SetDirty(state);
UnityEditor.EditorUtility.SetDirty(controller);
var prefabPath = "Assets/Prefabs/Stone_Prefab.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try {
var animator = root.GetComponentInChildren<UnityEngine.Animator>(true);
animator.runtimeAnimatorController = controller;
animator.applyRootMotion = false;
var enemy = root.GetComponent<Enemy>();
enemy.attackWindup = attack.length * .65f;
enemy.attackRecovery = attack.length - enemy.attackWindup + .15f;
enemy.attackCooldown = attack.length + .5f;
UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
} finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
UnityEditor.AssetDatabase.SaveAssets();
return "Attack=" + attack.name + ", duration=" + attack.length + ", loop=" + attack.isLooping + ", path=" + UnityEditor.AssetDatabase.GetAssetPath(state.motion);
