using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BattlePolishSetup
{
    const string Vfx="Assets/AssetStore/Epic Toon FX/Prefabs/Combat/Sword/";
    static GameObject Load(string path){return AssetDatabase.LoadAssetAtPath<GameObject>(Vfx+path+".prefab");}
    public static string Apply()
    {
        var stone=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Stone_Prefab.prefab");
        try
        {
            var e=stone.GetComponent<Enemy>();e.attackStyle=Enemy.AttackStyle.GroundSlam;
            e.showAttackWarning=true;e.slamRadius=6;e.attackRange=5;e.moveSpeed=1.8f;e.attackDamage=32;e.maxHp=1800;
            PrefabUtility.SaveAsPrefabAsset(stone,"Assets/Prefabs/Stone_Prefab.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(stone);}
        var baseStone=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Stone_Prefab.prefab");
        var raider=(GameObject)PrefabUtility.InstantiatePrefab(baseStone);
        try
        {
            raider.name="Stone Raider";raider.transform.localScale*=.5f;
            var e=raider.GetComponent<Enemy>();e.maxHp=180;e.moveSpeed=2.2f;e.attackRange=3.5f;e.slamRadius=4;e.attackDamage=22;
            PrefabUtility.SaveAsPrefabAsset(raider,"Assets/Prefabs/Stone_Raider.prefab");
        }
        finally{Object.DestroyImmediate(raider);}
        var skeleton=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Skeleton_Prefab.prefab");
        try
        {
            var e=skeleton.GetComponent<Enemy>();e.attackStyle=Enemy.AttackStyle.Melee;e.moveSpeed=3.5f;e.attackRange=1.9f;e.showAttackWarning=true;
            if(e.attackFx==null){e.attackFx=Load("Slash/SwordSlashMini/SwordSlashMiniWhite");e.attackFxOffset=new Vector3(0,1,1);e.attackFxRotation=new Vector3(0,0,90);}
            PrefabUtility.SaveAsPrefabAsset(skeleton,"Assets/Prefabs/Skeleton_Prefab.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(skeleton);}
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/battleScene.unity");
        bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/battleScene.unity",OpenSceneMode.Additive);
        foreach(var root in scene.GetRootGameObjects())
        {
            var setup=root.GetComponentInChildren<QuickSetup>(true);if(setup==null)continue;
            setup.enhanceBattlePresentation=true;
            if(setup.elementFx==null||setup.elementFx.Length<4)setup.elementFx=new ElementFx[4];
            string[] colors={"Red","Blue","Yellow","Green"};
            for(int i=0;i<4;i++)
            {
                if(setup.elementFx[i]==null)setup.elementFx[i]=new ElementFx{name=((Element)i).ToString()};
                var f=setup.elementFx[i];string color=colors[i];
                if(f.slash==null)f.slash=Load("Slash/SwordSlashThin/SwordSlashThin"+color);
                if(f.heavy==null)f.heavy=Load("Slash/SwordSlashThick/SwordSlashThick"+(i==2?"White":color));
                if(f.hit==null)f.hit=Load("Hit/SwordHitMini/SwordHitMini"+color);
                if(f.special==null)f.special=Load("Whirlwind/SwordWhirlwind"+(i==2?"White":color));
            }
            EditorUtility.SetDirty(setup);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        if(opened)EditorSceneManager.CloseScene(scene,true);
        AssetDatabase.SaveAssets();
        return "Battle polish saved: attack warnings, VFX, arena, sound and special presentation.";
    }
}
