using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class StoneAttackSetup
{
    public static string Build()
    {
        const string path="Assets/animetions/Stone_Attack_Ani.fbx";
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        var clips=importer.clipAnimations;
        if(clips.Length==0)clips=importer.defaultClipAnimations;
        foreach(var clip in clips){clip.loopTime=false;clip.loopPose=false;}
        importer.clipAnimations=clips;importer.SaveAndReimport();
        AnimationClip attack=null,walk=null;
        foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if(asset is AnimationClip c&&!c.name.StartsWith("__preview__")){attack=c;break;}
        foreach(var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/animetions/Stone_walking_Ani.fbx"))
            if(asset is AnimationClip c&&!c.name.StartsWith("__preview__")){walk=c;break;}
        if(attack==null||walk==null)throw new Exception("Stone animation clips missing");
        // The attack FBX has Hips at its root; the prefab's walking rig has Armature/Hips.
        var riggedAttack = new AnimationClip { name = "Stone Attack Rigged", frameRate = attack.frameRate };
        foreach (var binding in AnimationUtility.GetCurveBindings(attack))
        {
            var mapped = binding;
            mapped.path = string.IsNullOrEmpty(binding.path) ? "Armature" : "Armature/" + binding.path;
            AnimationUtility.SetEditorCurve(riggedAttack, mapped, AnimationUtility.GetEditorCurve(attack, binding));
        }
        riggedAttack.EnsureQuaternionContinuity();
        const string riggedPath = "Assets/animetions/Stone_Attack_Rigged.anim";
        var existingAttack = AssetDatabase.LoadAssetAtPath<AnimationClip>(riggedPath);
        if (existingAttack == null) { AssetDatabase.CreateAsset(riggedAttack, riggedPath); attack = riggedAttack; }
        else
        {
            EditorUtility.CopySerialized(riggedAttack, existingAttack);
            UnityEngine.Object.DestroyImmediate(riggedAttack);
            attack = existingAttack;
        }
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/animetors/Stone_Animetor.controller");
        var machine=controller.layers[0].stateMachine;
        foreach(var state in machine.states)machine.RemoveState(state.state);
        foreach(var transition in machine.anyStateTransitions)machine.RemoveAnyStateTransition(transition);
        controller.parameters=new[]{
            new AnimatorControllerParameter{name="Speed",type=AnimatorControllerParameterType.Float},
            new AnimatorControllerParameter{name="Attack",type=AnimatorControllerParameterType.Trigger}
        };
        var idle=machine.AddState("Idle");idle.motion=walk;idle.speed=0;
        var walking=machine.AddState("Walk");walking.motion=walk;
        var attacking=machine.AddState("Attack");attacking.motion=attack;
        var startWalking=idle.AddTransition(walking);startWalking.hasExitTime=false;startWalking.duration=.15f;startWalking.AddCondition(AnimatorConditionMode.Greater,.1f,"Speed");
        var stopWalking=walking.AddTransition(idle);stopWalking.hasExitTime=false;stopWalking.duration=.15f;stopWalking.AddCondition(AnimatorConditionMode.Less,.1f,"Speed");
        var startAttack=machine.AddAnyStateTransition(attacking);startAttack.hasExitTime=false;startAttack.duration=.1f;startAttack.canTransitionToSelf=false;startAttack.AddCondition(AnimatorConditionMode.If,0,"Attack");
        var finishAttack=attacking.AddTransition(idle);finishAttack.hasExitTime=true;finishAttack.exitTime=1;finishAttack.duration=.1f;
        machine.defaultState=idle;EditorUtility.SetDirty(controller);
        const string prefabPath="Assets/Resources/Stone_Prefab.prefab";
        var root=PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var animator=root.GetComponentInChildren<Animator>(true);
            if(animator==null)throw new Exception("Stone rigged Animator missing");
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
            var enemy=root.GetComponent<Enemy>();if(enemy==null)enemy=root.AddComponent<Enemy>();
            enemy.attackWindup=attack.length*.5f;
            enemy.attackRecovery=attack.length-enemy.attackWindup+.15f;
            enemy.attackCooldown=attack.length+.5f;
            PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        EnemyDeathSetup.Build("Stone");
        Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        return "Stone Attack configured; duration="+attack.length+" seconds, loop="+attack.isLooping;
    }
}
