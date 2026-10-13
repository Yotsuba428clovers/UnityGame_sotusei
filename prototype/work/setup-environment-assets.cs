var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath("Assets/materials/textures/flat_tex.png");
importer.wrapMode=UnityEngine.TextureWrapMode.Repeat;
importer.filterMode=UnityEngine.FilterMode.Trilinear;
importer.anisoLevel=8;importer.mipmapEnabled=true;
importer.SaveAndReimport();
if(!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources/Environment"))UnityEditor.AssetDatabase.CreateFolder("Assets/Resources","Environment");
var ground=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Resources/Environment/GrassStoneGround.mat");
if(ground==null)
{
    ground=new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));
    UnityEditor.AssetDatabase.CreateAsset(ground,"Assets/Resources/Environment/GrassStoneGround.mat");
}
ground.SetTexture("_BaseMap",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/materials/textures/flat_tex.png"));
ground.SetColor("_BaseColor",new UnityEngine.Color(.92f,.95f,.89f));
ground.SetFloat("_Smoothness",.12f);ground.SetFloat("_Metallic",0);
ground.mainTextureScale=UnityEngine.Vector2.one*37.5f;
UnityEditor.EditorUtility.SetDirty(ground);
var shader=UnityEngine.Shader.Find("Prototype/Soft Day Sky");
if(shader==null||!shader.isSupported)throw new System.Exception("Sky shader unavailable");
foreach(var message in UnityEditor.ShaderUtil.GetShaderMessages(shader))
    if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)throw new System.Exception(message.message);
var sky=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Resources/Environment/DaylightSky.mat");
if(sky==null)
{
    sky=new UnityEngine.Material(shader);
    UnityEditor.AssetDatabase.CreateAsset(sky,"Assets/Resources/Environment/DaylightSky.mat");
}
UnityEditor.EditorUtility.SetDirty(sky);UnityEditor.AssetDatabase.SaveAssets();
return "Ground uses flat_tex, Repeat, mipmaps and 8x anisotropy; sky and ground materials saved";
