using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SkeletonPrefabSetup
{
    public static string Build()
    {
        const string animationPath = "Assets/animetions/Skeleton_walking_Ani.fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(animationPath);
        var clips = importer.clipAnimations;
        if (clips.Length == 0) clips = importer.defaultClipAnimations;
        foreach (var c in clips) { c.loopTime = true; c.loopPose = true; }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        AnimationClip walk = null;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(animationPath))
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) { walk = clip; break; }
        if (walk == null) throw new Exception("Walking clip missing");
        AnimationClip sourceAttack = null;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/animetions/Skeleton_Attack_Ani.fbx"))
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) { sourceAttack = clip; break; }
        if (sourceAttack == null) throw new Exception("Attack clip missing");
        // The attack export omits the Armature prefix present in the walking rig.
        var attack = UnityEngine.Object.Instantiate(sourceAttack);
        attack.name = "Skeleton Attack";
        foreach (var binding in AnimationUtility.GetCurveBindings(sourceAttack))
        {
            var mapped = binding;
            mapped.path = string.IsNullOrEmpty(binding.path) ? "Armature" : "Armature/" + binding.path;
            AnimationUtility.SetEditorCurve(attack, binding, null);
            AnimationUtility.SetEditorCurve(attack, mapped, AnimationUtility.GetEditorCurve(sourceAttack, binding));
        }
        var clipSettings = AnimationUtility.GetAnimationClipSettings(attack);
        clipSettings.loopTime = false; clipSettings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(attack, clipSettings);
        const string attackPath = "Assets/animetions/Skeleton_Attack_Rigged.anim";
        var oldAttack = AssetDatabase.LoadAssetAtPath<AnimationClip>(attackPath);
        if (oldAttack != null) { EditorUtility.CopySerialized(attack, oldAttack); UnityEngine.Object.DestroyImmediate(attack); attack = oldAttack; }
        else AssetDatabase.CreateAsset(attack, attackPath);

        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/models/Skeleton.fbx");
        var sourceFilter = source.GetComponentInChildren<MeshFilter>();
        var rigAsset = AssetDatabase.LoadAssetAtPath<GameObject>(animationPath);
        var skin = rigAsset.GetComponentInChildren<SkinnedMeshRenderer>();
        if (sourceFilter == null || skin == null) throw new Exception("Skeleton mesh or walking skeleton missing");
        var target = skin.sharedMesh;
        var vertices = sourceFilter.sharedMesh.vertices;
        var normals = sourceFilter.sharedMesh.normals;
        var worldBounds = sourceFilter.GetComponent<Renderer>().bounds;
        var bounds = target.bounds;
        var grid = new Dictionary<Vector3Int, List<int>>();
        var targetVertices = target.vertices;
        const float cell = .035f;
        for (int i = 0; i < targetVertices.Length; i++)
        {
            var key = Vector3Int.FloorToInt(targetVertices[i] / cell);
            if (!grid.TryGetValue(key, out var indices)) grid[key] = indices = new List<int>();
            indices.Add(i);
        }
        var sourceWeights = target.boneWeights;
        var weights = new BoneWeight[vertices.Length];
        float maxDistance = 0;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = sourceFilter.transform.TransformPoint(vertices[i]);
            v = Vector3.Scale(v - worldBounds.center, new Vector3(bounds.size.x/worldBounds.size.x, bounds.size.y/worldBounds.size.y, bounds.size.z/worldBounds.size.z)) + bounds.center;
            vertices[i] = v;
            if (normals.Length == vertices.Length) normals[i] = sourceFilter.transform.TransformDirection(normals[i]).normalized;
            var key = Vector3Int.FloorToInt(v / cell);
            float distance = float.MaxValue; int nearest = -1;
            for (int x=-1;x<=1;x++) for (int y=-1;y<=1;y++) for (int z=-1;z<=1;z++)
            {
                if (!grid.TryGetValue(key + new Vector3Int(x,y,z), out var indices)) continue;
                foreach (int j in indices) { float d = (v-targetVertices[j]).sqrMagnitude; if (d < distance) { distance=d; nearest=j; } }
            }
            if (nearest < 0) throw new Exception("Mesh shapes do not match at vertex " + i);
            maxDistance = Mathf.Max(maxDistance, Mathf.Sqrt(distance));
            weights[i] = sourceWeights[nearest];
        }
        if (maxDistance > .05f) throw new Exception("Mesh correspondence too inaccurate: " + maxDistance);
        var mesh = UnityEngine.Object.Instantiate(sourceFilter.sharedMesh);
        mesh.name = "Skeleton Rigged"; mesh.vertices = vertices; mesh.normals = normals;
        mesh.boneWeights = weights; mesh.bindposes = target.bindposes;
        mesh.RecalculateBounds(); mesh.RecalculateTangents();
        const string meshPath = "Assets/models/Skeleton_Rigged.asset";
        var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existingMesh != null) { EditorUtility.CopySerialized(mesh, existingMesh); UnityEngine.Object.DestroyImmediate(mesh); mesh = existingMesh; }
        else AssetDatabase.CreateAsset(mesh, meshPath);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/animetors/Skeleton_Animetor.controller");
        var machine = controller.layers[0].stateMachine;
        foreach (var state in machine.states) machine.RemoveState(state.state);
        foreach (var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
        controller.parameters = new[] {
            new AnimatorControllerParameter { name="Speed", type=AnimatorControllerParameterType.Float },
            new AnimatorControllerParameter { name="Attack", type=AnimatorControllerParameterType.Trigger }
        };
        var idleState = machine.AddState("Idle"); idleState.motion = walk; idleState.speed = 0;
        var walkState = machine.AddState("Walk"); walkState.motion = walk;
        var attackState = machine.AddState("Attack"); attackState.motion = attack;
        var toWalk = idleState.AddTransition(walkState); toWalk.hasExitTime = false; toWalk.duration = .12f; toWalk.AddCondition(AnimatorConditionMode.Greater, .1f, "Speed");
        var toIdle = walkState.AddTransition(idleState); toIdle.hasExitTime = false; toIdle.duration = .12f; toIdle.AddCondition(AnimatorConditionMode.Less, .1f, "Speed");
        var toAttack = machine.AddAnyStateTransition(attackState); toAttack.hasExitTime = false; toAttack.duration = .08f; toAttack.canTransitionToSelf = false; toAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        var afterAttack = attackState.AddTransition(idleState); afterAttack.hasExitTime = true; afterAttack.exitTime = 1; afterAttack.duration = .08f;
        machine.defaultState = idleState;
        EditorUtility.SetDirty(controller);

        const string prefabPath = "Assets/Prefabs/Skeleton_Prefab.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            for (int i=root.transform.childCount-1;i>=0;i--) UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            var oldAnimator = root.GetComponent<Animator>();
            if (oldAnimator != null) UnityEngine.Object.DestroyImmediate(oldAnimator);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(rigAsset, root.transform);
            model.name = "Skeleton Animated Model";
            // Make the copied rig editable while preserving the source FBX unchanged.
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.sharedMaterials = sourceFilter.GetComponent<Renderer>().sharedMaterials;
            renderer.localBounds = skin.localBounds;
            var animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var enemy = root.GetComponent<Enemy>();
            if (enemy == null) enemy = root.AddComponent<Enemy>();
            enemy.attackRange = 1.7f;
            enemy.attackWindup = attack.length * .45f;
            enemy.attackRecovery = attack.length - enemy.attackWindup + .12f;
            enemy.attackCooldown = attack.length + .4f;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        EnemyDeathSetup.Build("Skeleton");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        return "Skeleton_Prefab updated. Vertices="+vertices.Length+", maximum skin transfer distance="+maxDistance+", loop="+walk.isLooping;
    }
}

