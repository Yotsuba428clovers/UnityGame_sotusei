using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class StonePrefabSetup
{
    public static string Build()
    {
        const string animationPath = "Assets/animetions/Stone_walking_Ani.fbx";
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

        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/models/Stone.fbx");
        var sourceFilter = source.GetComponentInChildren<MeshFilter>();
        var rigAsset = AssetDatabase.LoadAssetAtPath<GameObject>(animationPath);
        var skin = rigAsset.GetComponentInChildren<SkinnedMeshRenderer>();
        if (sourceFilter == null || skin == null) throw new Exception("Stone mesh or walking skeleton missing");
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
        mesh.name = "Stone Rigged"; mesh.vertices = vertices; mesh.normals = normals;
        mesh.boneWeights = weights; mesh.bindposes = target.bindposes;
        mesh.RecalculateBounds(); mesh.RecalculateTangents();
        const string meshPath = "Assets/models/Stone_Rigged.asset";
        var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existingMesh != null) { EditorUtility.CopySerialized(mesh, existingMesh); UnityEngine.Object.DestroyImmediate(mesh); mesh = existingMesh; }
        else AssetDatabase.CreateAsset(mesh, meshPath);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/animetors/Stone_Animetor.controller");
        var machine = controller.layers[0].stateMachine;
        var state = machine.states[0].state;
        state.name = "Stone Walking"; state.motion = walk; state.speed = 1;
        state.speedParameterActive = false;
        foreach (var transition in state.transitions) state.RemoveTransition(transition);
        machine.defaultState = state;
        EditorUtility.SetDirty(controller);

        const string prefabPath = "Assets/Prefabs/Stone_Prefab.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            for (int i=root.transform.childCount-1;i>=0;i--) UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            var oldAnimator = root.GetComponent<Animator>();
            if (oldAnimator != null) UnityEngine.Object.DestroyImmediate(oldAnimator);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(rigAsset, root.transform);
            model.name = "Stone Animated Model";
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
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        StoneAttackSetup.Build();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        return "Stone_Prefab updated. Vertices="+vertices.Length+", maximum skin transfer distance="+maxDistance+", loop="+walk.isLooping;
    }
}
