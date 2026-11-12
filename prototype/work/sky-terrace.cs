using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
const string scenePath="Assets/Scenes/battleScene.unity";
const string folder="Assets/materials/BattleScenery";
if(EditorApplication.isPlaying)throw new System.Exception("Stop Play Mode first.");
var original=SceneManager.GetActiveScene();
var scene=SceneManager.GetSceneByPath(scenePath);
bool additive=!scene.IsValid()||!scene.isLoaded;
if(additive)scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
if(scene.isDirty)throw new System.Exception("Battle scene has unsaved edits.");
SceneManager.SetActiveScene(scene);
var root=System.Array.Find(scene.GetRootGameObjects(),g=>g.name=="Battle Scenery");
if(root==null)throw new System.Exception("Existing Battle Scenery not found.");
if(root.transform.Find("Sky Citadel")!=null)throw new System.Exception("Citadel already exists.");

const int width=2048,height=1024;
var hdr=new Texture2D(width,height,TextureFormat.RGBAHalf,false,true);
var pixels=new Color[width*height];
var sun=new Vector3(.38f,.74f,-.55f).normalized;
var zenith=new Color(.07f,.29f,.72f);
var horizon=new Color(.47f,.74f,.92f);
for(int y=0;y<height;y++)
{
    float latitude=((y+.5f)/height-.5f)*Mathf.PI;
    float dy=Mathf.Sin(latitude), horizontal=Mathf.Cos(latitude);
    for(int x=0;x<width;x++)
    {
        float phi=(.5f-(x+.5f)/width)*Mathf.PI*2f;
        var direction=new Vector3(Mathf.Cos(phi)*horizontal,dy,Mathf.Sin(phi)*horizontal);
        float elevation=Mathf.Max(0,dy);
        var sky=Color.Lerp(horizon,zenith,Mathf.Pow(elevation,.46f));
        if(dy>0)
        {
            float scale=2.6f/(dy+.31f);
            float u=direction.x*scale+23.7f,v=direction.z*scale+61.2f;
            float n=Mathf.PerlinNoise(u,v)*.59f+Mathf.PerlinNoise(u*2.17f+7.1f,v*2.17f)*.27f+Mathf.PerlinNoise(u*4.63f,v*4.63f+13)*.14f;
            float cloud=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.54f,.66f,n))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.015f,.12f,dy));
            sky=Color.Lerp(sky,Color.Lerp(new Color(.62f,.76f,.9f),new Color(1.12f,1.14f,1.16f),Mathf.InverseLerp(.53f,.73f,n)),cloud);
        }
        float glow=Mathf.Max(0,Vector3.Dot(direction,sun));
        sky+=new Color(1f,.88f,.66f)*(Mathf.Pow(glow,2800)*11f+Mathf.Pow(glow,24)*.12f);
        sky.a=1;pixels[y*width+x]=sky;
    }
}
hdr.SetPixels(pixels);hdr.Apply();
var exrPath=folder+"/Sky Terrace HDRI.exr";
System.IO.File.WriteAllBytes(exrPath,hdr.EncodeToEXR(Texture2D.EXRFlags.CompressZIP));
Object.DestroyImmediate(hdr);
AssetDatabase.ImportAsset(exrPath,ImportAssetOptions.ForceSynchronousImport);
var importer=(TextureImporter)AssetImporter.GetAtPath(exrPath);
importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.maxTextureSize=2048;
importer.textureCompression=TextureImporterCompression.Uncompressed;
importer.wrapModeU=TextureWrapMode.Repeat;importer.wrapModeV=TextureWrapMode.Clamp;
importer.SaveAndReimport();
var panoramic=Shader.Find("Skybox/Panoramic");
if(panoramic==null)throw new System.Exception("Panoramic sky shader not found.");
var skyMat=new Material(panoramic);
skyMat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(exrPath));
skyMat.SetFloat("_Exposure",1);skyMat.SetFloat("_Mapping",1);
AssetDatabase.CreateAsset(skyMat,folder+"/Sky Terrace HDRI.mat");
RenderSettings.skybox=skyMat;

var main=root.transform.Find("Main ruins");
var rear=root.transform.Find("Perimeter ruins 3");
var dais=main.Find("dai");
if(dais==null)throw new System.Exception("Main dais missing.");
dais.SetParent(root.transform,true);
dais.name="dai - Central arena";
dais.SetPositionAndRotation(new Vector3(0,0,4),Quaternion.identity);
dais.localScale=new Vector3(34,1.6f,34);
var daisRenderer=dais.GetComponentInChildren<Renderer>();
dais.position+=new Vector3(-daisRenderer.bounds.center.x,.105f-daisRenderer.bounds.max.y,4-daisRenderer.bounds.center.z);
var rearDais=rear.Find("dai");
if(rearDais!=null)rearDais.gameObject.SetActive(false);
var oldMain=new System.Collections.Generic.List<Transform>();
foreach(Transform item in main)oldMain.Add(item);
var layout=new[]{new Vector3(-17,0,21),new Vector3(-14,0,24),new Vector3(15,0,25),new Vector3(18,0,23),new Vector3(-17,0,15),new Vector3(17,0,16)};
for(int i=0;i<oldMain.Count;i++)
{
    var item=oldMain[i];item.position=layout[i];
    if(item.name=="e")item.rotation=Quaternion.Euler(0,-16,0);
    var r=item.GetComponentInChildren<Renderer>();item.position-=Vector3.up*r.bounds.min.y;
}
foreach(Transform item in rear)
{
    if(item==rearDais)continue;
    var r=item.GetComponentInChildren<Renderer>();item.position-=Vector3.up*r.bounds.min.y;
}
var lit=Shader.Find("Universal Render Pipeline/Lit");
System.Func<string,Color,Material> makeMat=(name,color)=>
{
    var mat=new Material(lit);mat.color=color;mat.SetFloat("_Smoothness",.12f);
    AssetDatabase.CreateAsset(mat,folder+"/"+name+".mat");return mat;
};
var stone=makeMat("Citadel Limestone",new Color(.51f,.65f,.77f));
var roof=makeMat("Citadel Blue",new Color(.16f,.34f,.56f));
var cliff=makeMat("Floating Slate",new Color(.25f,.45f,.60f));
var grass=makeMat("Floating Meadow",new Color(.40f,.57f,.33f));
var wall=makeMat("Terrace Limestone",new Color(.64f,.63f,.52f));
var beacon=makeMat("Citadel Beacon",new Color(.38f,.75f,1f));
beacon.EnableKeyword("_EMISSION");beacon.SetColor("_EmissionColor",new Color(.25f,.8f,1f)*3f);
var citadel=new GameObject("Sky Citadel").transform;citadel.SetParent(root.transform,false);
var parapets=new GameObject("Terrace parapets").transform;parapets.SetParent(root.transform,false);
System.Action<Transform,string,PrimitiveType,Vector3,Vector3,Material> piece=(parent,name,type,pos,scale,mat)=>
{
    var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);
    go.transform.localPosition=pos;go.transform.localScale=scale;
    go.GetComponent<Renderer>().sharedMaterial=mat;
    Object.DestroyImmediate(go.GetComponent<Collider>());go.isStatic=true;
};
for(int sign=-1;sign<=1;sign+=2)
{
    for(int i=0;i<7;i++)
    {
        float x=sign*(7+i*1.85f);
        piece(parapets,"Weathered block",PrimitiveType.Cube,new Vector3(x,.58f,27),new Vector3(1.8f,1.16f,1.25f),wall);
        piece(parapets,"Coping",PrimitiveType.Cube,new Vector3(x,1.22f,27),new Vector3(1.82f,.22f,1.48f),wall);
    }
}
var cone=new Mesh();cone.name="Faceted taper";
var verts=new Vector3[14];verts[12]=Vector3.up;verts[13]=Vector3.zero;
var tris=new int[72];
for(int i=0;i<12;i++){float a=i*Mathf.PI/6;verts[i]=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));int j=(i+1)%12;int t=i*6;tris[t]=i;tris[t+1]=12;tris[t+2]=j;tris[t+3]=i;tris[t+4]=j;tris[t+5]=13;}
cone.vertices=verts;cone.triangles=tris;cone.RecalculateNormals();cone.RecalculateBounds();
AssetDatabase.CreateAsset(cone,folder+"/Faceted Taper.asset");
System.Action<Transform,string,Vector3,Vector3,Material,bool> taper=(parent,name,pos,scale,mat,inverted)=>
{
    var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
    go.transform.localPosition=pos;go.transform.localScale=scale;
    if(inverted)go.transform.localRotation=Quaternion.Euler(180,0,0);
    go.GetComponent<MeshFilter>().sharedMesh=cone;go.GetComponent<MeshRenderer>().sharedMaterial=mat;go.isStatic=true;
};
citadel.localPosition=new Vector3(29,21,174);
piece(citadel,"Meadow crown",PrimitiveType.Cylinder,new Vector3(0,0,0),new Vector3(56,2.8f,34),grass);
taper(citadel,"Floating bedrock",new Vector3(0,-2,0),new Vector3(28,34,17),cliff,true);
for(int i=0;i<7;i++)taper(citadel,"Hanging crag",new Vector3(-23+i*7.6f,-2,-5+i%3*3),new Vector3(6,14+i%4*4,7),cliff,true);
piece(citadel,"Keep",PrimitiveType.Cube,new Vector3(0,10,1),new Vector3(24,15,13),stone);
for(int i=0;i<7;i++)
{
    float x=(i-3)*6f,z=i%2==0?-3:6;
    float h=19+(3-Mathf.Abs(i-3))*5;
    float diameter=i==3?8:5.3f;
    piece(citadel,"Tower",PrimitiveType.Cylinder,new Vector3(x,2+h/2,z),new Vector3(diameter,h/2,diameter),stone);
    piece(citadel,"Tower cornice",PrimitiveType.Cylinder,new Vector3(x,h+2,z),new Vector3(diameter*1.2f,.75f,diameter*1.2f),stone);
    taper(citadel,"Blue spire",new Vector3(x,h+2.6f,z),new Vector3(diameter*.59f,i==3?15:8,diameter*.59f),roof,false);
    for(int j=0;j<3;j++)piece(citadel,"Lancet",PrimitiveType.Cube,new Vector3(x,7+j*4,z-diameter*.493f),new Vector3(.65f,2.1f,.26f),roof);
}
taper(citadel,"Sunlit finial",new Vector3(0,53,6),new Vector3(.8f,7,.8f),beacon,false);
for(int i=0;i<3;i++)
{
    var isle=new GameObject("Satellite island "+(i+1)).transform;isle.SetParent(root.transform,false);
    isle.localPosition=new Vector3(new[]{-72f,-37f,91f}[i],new[]{14f,32f,23f}[i],new[]{143f,187f,191f}[i]);
    float r=new[]{16f,7f,11f}[i];
    piece(isle,"Green crown",PrimitiveType.Cylinder,Vector3.zero,new Vector3(r*2,1.6f,r*1.3f),grass);
    taper(isle,"Slate underside",new Vector3(0,-1.4f,0),new Vector3(r,r*1.35f,r*.65f),cliff,true);
    piece(isle,"Ruined tower",PrimitiveType.Cylinder,new Vector3(0,5,0),new Vector3(3,4,3),stone);
    taper(isle,"Spire",new Vector3(0,9,0),new Vector3(1.8f,4.5f,1.8f),roof,false);
}
AssetDatabase.SaveAssets();
if(!EditorSceneManager.SaveScene(scene))throw new System.Exception("Could not save battle scene.");
if(additive)EditorSceneManager.CloseScene(scene,true);
if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);
return "Central dai, framing ruins and parapets, floating citadel, and 2048x1024 HDR EXR saved.";
