using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
const string path="Assets/Scenes/battleScene.unity";
if(EditorApplication.isPlaying)throw new System.Exception("Still playing");
var old=SceneManager.GetSceneByPath(path);
if(old.IsValid()&&old.isLoaded&&old.isDirty)throw new System.Exception("Unsaved battle scene");
AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
if(old.IsValid()&&old.isLoaded&&SceneManager.sceneCount>1)EditorSceneManager.CloseScene(old,true);
var scene=EditorSceneManager.OpenScene(path,SceneManager.sceneCount==1&&SceneManager.GetActiveScene().path==path?OpenSceneMode.Single:OpenSceneMode.Additive);
foreach(var obj in scene.GetRootGameObjects())
{
    var q=obj.GetComponent<QuickSetup>();
    if(q!=null)
    {
        var so=new SerializedObject(q);
        so.FindProperty("firstPersonView").boolValue=false;
        so.FindProperty("thirdPersonStartPitch").floatValue=8;
        so.FindProperty("thirdPersonDistance").floatValue=11;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
var root=System.Array.Find(scene.GetRootGameObjects(),g=>g.name=="Battle Scenery");
var castle=root.transform.Find("Sky Citadel");
castle.localPosition=new Vector3(29,18,174);castle.localScale=new Vector3(1,.83f,1);
EditorSceneManager.MarkSceneDirty(scene);
if(!EditorSceneManager.SaveScene(scene))throw new System.Exception("Cannot save");
SceneManager.SetActiveScene(scene);
return "Editor-saved third person camera and adjusted citadel.";
