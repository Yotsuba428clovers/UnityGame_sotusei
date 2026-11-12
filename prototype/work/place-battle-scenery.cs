using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
const string path="Assets/Scenes/battleScene.unity";
const string folder="Assets/materials/BattleScenery";
const string rootName="Battle Scenery";
if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode before placement.");
var previous=SceneManager.GetActiveScene();
var scene=SceneManager.GetSceneByPath(path);
bool opened=!scene.IsValid()||!scene.isLoaded;
if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
if(scene.isDirty)throw new System.Exception("Battle scene has unsaved edits.");
if(System.Array.Exists(scene.GetRootGameObjects(), item=>item.name==rootName))throw new System.Exception("Scenery already exists.");
var assets=new System.Collections.Generic.Dictionary<string,GameObject>();
var materials=new System.Collections.Generic.Dictionary<string,Material>();
var shader=Shader.Find("Universal Render Pipeline/Lit");
if(shader==null)throw new System.Exception("URP Lit is unavailable.");
if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/materials","BattleScenery");
foreach(var id in new[]{"dai","a","b","c","d","e"})
{
    assets[id]=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/models/"+id+".fbx");
    if(assets[id]==null)throw new System.Exception("Missing FBX "+id);
    var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/materials/textures/"+id+".jpg");
    if(texture==null&&id!="e")throw new System.Exception("Missing texture "+id);
    var materialPath=folder+"/"+id+".mat";
    var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
    if(mat==null)
    {
        mat=new Material(shader);
        mat.SetColor("_BaseColor",texture!=null?Color.white:new Color(.65f,.67f,.56f));
        mat.SetTexture("_BaseMap",texture);
        mat.SetFloat("_Smoothness",.14f);
        AssetDatabase.CreateAsset(mat,materialPath);
    }
    materials[id]=mat;
}
var root=new GameObject(rootName);
SceneManager.MoveGameObjectToScene(root,scene);
System.Action<Transform,string,Vector3,float,float> place=(parent,id,position,scale,yaw)=>
{
    var instance=(GameObject)PrefabUtility.InstantiatePrefab(assets[id],scene);
    instance.transform.SetParent(parent,false);
    instance.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
    instance.transform.localScale=Vector3.one*scale;
    float bottom=float.PositiveInfinity;
    foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))
    {
        var slots=renderer.sharedMaterials;
        for(int i=0;i<slots.Length;i++)slots[i]=materials[id];
        renderer.sharedMaterials=slots;
        bottom=Mathf.Min(bottom,renderer.bounds.min.y);
    }
    if(float.IsInfinity(bottom))throw new System.Exception("No mesh for "+id);
    instance.transform.position+=Vector3.up*(position.y-bottom);
    instance.isStatic=true;
};
for(int i=0;i<6;i++)
{
    var group=new GameObject(i==0?"Main ruins":"Perimeter ruins "+i);
    group.transform.SetParent(root.transform,false);
    float angle=i*60f;
    var turn=Quaternion.Euler(0,angle,0);
    var center=turn*new Vector3(0,0,i==0?43:46+i%2*5);
    if(i==0||i==3)place(group.transform,"dai",center,17f,angle+18);
    float top=i==0||i==3?2.2f:0f;
    place(group.transform,"d",center+turn*new Vector3(0,top,1.4f),8f,angle+180);
    place(group.transform,"b",center+turn*new Vector3(-7.1f,0,-.5f),9f,angle+15);
    place(group.transform,"b",center+turn*new Vector3(7.3f,0,1.7f),7f,angle-26);
    place(group.transform,"e",center+turn*new Vector3(12f,0,5),9f,angle+22);
    place(group.transform,"a",center+turn*new Vector3(-11f,0,2),4.2f,angle+38);
    place(group.transform,"c",center+turn*new Vector3(5.3f,top,-4.4f),2.1f,angle+18);
}
AssetDatabase.SaveAssets();
if(!EditorSceneManager.SaveScene(scene))throw new System.Exception("Could not save battle scene.");
if(opened)EditorSceneManager.CloseScene(scene,true);
if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
return "Saved 38 scenery pieces in battleScene and six materials.";
