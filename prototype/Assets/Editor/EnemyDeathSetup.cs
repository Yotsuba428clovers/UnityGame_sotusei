using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class EnemyDeathSetup
{
    public static string BuildAll()
    {
        return Build("Skeleton") + "\n" + Build("Stone");
    }

    public static string Build(string name)
    {
        string sourcePath = "Assets/animetions/" + name + "_Dead_Ani.fbx";
        var source = AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        if (source == null) throw new Exception(name + " death animation missing");
        var clip = new AnimationClip { name = name + " Death Rigged", frameRate = source.frameRate };
        foreach (var binding in AnimationUtility.GetCurveBindings(source))
        {
            var mapped = binding;
            mapped.path = string.IsNullOrEmpty(binding.path) ? "Armature" : "Armature/" + binding.path;
            AnimationUtility.SetEditorCurve(clip, mapped, AnimationUtility.GetEditorCurve(source, binding));
        }
        clip.EnsureQuaternionContinuity();
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        string clipPath = "Assets/animetions/" + name + "_Dead_Rigged.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existing == null) AssetDatabase.CreateAsset(clip, clipPath);
        else
        {
            EditorUtility.CopySerialized(clip, existing);
            UnityEngine.Object.DestroyImmediate(clip);
            clip = existing;
        }

        string prefabPath = "Assets/Prefabs/" + name + "_Prefab.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null) throw new Exception(name + " Animator missing");
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                if (animator.transform.Find(binding.path) == null)
                    throw new Exception(name + " death bone missing: " + binding.path);
            var controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller == null) throw new Exception(name + " AnimatorController missing");
            var machine = controller.layers[0].stateMachine;
            var death = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Die");
            if (death == null) death = machine.AddState("Die");
            death.motion = clip;
            death.speed = 1f;
            death.speedParameterActive = false;
            foreach (var transition in death.transitions) death.RemoveTransition(transition);
            EditorUtility.SetDirty(death);
            EditorUtility.SetDirty(controller);
            var enemy = root.GetComponent<Enemy>();
            if (enemy == null) throw new Exception(name + " Enemy missing");
            enemy.deathAnimation = clip;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        return name + " death configured: " + clip.length + " seconds, loop=" + clip.isLooping;
    }
}
