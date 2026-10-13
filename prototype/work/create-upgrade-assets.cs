if(!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))UnityEditor.AssetDatabase.CreateFolder("Assets","Resources");
if(!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources/RogueliteUpgrades"))UnityEditor.AssetDatabase.CreateFolder("Assets/Resources","RogueliteUpgrades");
var go=new UnityEngine.GameObject("Temporary upgrade catalog");
var manager=go.AddComponent<UpgradeManager>();
manager.Initialize(null);
foreach(var upgrade in manager.catalog)
{
    string path="Assets/Resources/RogueliteUpgrades/"+upgrade.effect+".asset";
    if(UnityEditor.AssetDatabase.LoadAssetAtPath<UpgradeData>(path)!=null)continue;
    var asset=UnityEngine.Object.Instantiate(upgrade);
    asset.name=upgrade.title;
    UnityEditor.AssetDatabase.CreateAsset(asset,path);
}
UnityEngine.Object.DestroyImmediate(go);
UnityEditor.AssetDatabase.SaveAssets();
string backup=UnityEditor.SessionState.GetString("RogueliteTest.SaveBackup","");
if(backup=="")UnityEngine.PlayerPrefs.DeleteKey("Prototype.Roguelite.Records.v1");
else UnityEngine.PlayerPrefs.SetString("Prototype.Roguelite.Records.v1",backup);
UnityEngine.PlayerPrefs.Save();
return "16 editable upgrade assets created; test records restored";
