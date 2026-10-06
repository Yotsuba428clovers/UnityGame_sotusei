using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BattleEnvironment : MonoBehaviour
{
    Material stone,trim,seam,tile;
    VolumeProfile profile;
    bool oldFog;
    FogMode oldFogMode;
    Color oldFogColor,oldAmbient;
    float oldFogDensity;
    void Start()
    {
        oldFog=RenderSettings.fog;oldFogMode=RenderSettings.fogMode;oldFogColor=RenderSettings.fogColor;oldFogDensity=RenderSettings.fogDensity;oldAmbient=RenderSettings.ambientLight;
        stone=new Material(Shader.Find("Universal Render Pipeline/Lit"));stone.color=new Color(.16f,.20f,.24f);stone.SetFloat("_Smoothness",.22f);
        seam=new Material(stone);seam.color=new Color(.10f,.13f,.16f);
        tile=new Material(stone);tile.color=new Color(.20f,.24f,.27f);
        trim=new Material(Shader.Find("Universal Render Pipeline/Lit"));trim.color=new Color(.16f,.40f,.39f);trim.EnableKeyword("_EMISSION");trim.SetColor("_EmissionColor",new Color(.14f,.75f,.65f)*1.5f);
        var ground=GameObject.Find("Ground");if(ground!=null)ground.GetComponent<Renderer>().sharedMaterial=stone;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.08f,.15f,.20f);RenderSettings.fogDensity=.012f;
        RenderSettings.ambientLight=new Color(.32f,.41f,.48f);
        var light=FindFirstObjectByType<Light>();if(light!=null){light.color=new Color(.73f,.85f,1);light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(38,-35,0);light.shadows=LightShadows.Soft;}
        for(int i=0;i<16;i++)
        {
            float a=i*Mathf.PI*2/16;var pos=new Vector3(Mathf.Cos(a)*32,0,Mathf.Sin(a)*32);
            float h=4+(i%3)*1.4f;
            Block("Ruined pillar",pos+Vector3.up*h*.5f,new Vector3(1.7f,h,1.7f),stone);
            Block("Pillar cap",pos+Vector3.up*h,new Vector3(2.2f,.35f,2.2f),stone);
            Block("Rune",pos+Vector3.up*(h*.65f),new Vector3(1.78f,.13f,1.78f),trim);
        }
        for(int i=-10;i<=10;i++)
        {
            Block("Floor seam",new Vector3(i*3,.025f,0),new Vector3(.05f,.015f,60),seam);
            Block("Floor seam",new Vector3(0,.025f,i*3),new Vector3(60,.015f,.05f),seam);
        }
        for(int x=-5;x<5;x++)for(int z=-5;z<5;z++)if((x+z)%2==0)
            Block("Weathered floor tile",new Vector3(x*6+3,.013f,z*6+3),new Vector3(5.95f,.01f,5.95f),tile);
        var volume=gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=10;
        profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;
        var bloom=profile.Add<Bloom>();bloom.intensity.Override(.35f);bloom.threshold.Override(1.1f);
        var vignette=profile.Add<Vignette>();vignette.intensity.Override(.22f);vignette.smoothness.Override(.45f);
        var color=profile.Add<ColorAdjustments>();color.contrast.Override(8);color.saturation.Override(5);
        if(Camera.main!=null){var data=Camera.main.GetComponent<UniversalAdditionalCameraData>();if(data!=null)data.renderPostProcessing=true;Camera.main.clearFlags=CameraClearFlags.SolidColor;Camera.main.backgroundColor=RenderSettings.fogColor;}
    }
    void Block(string name,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(transform);go.transform.position=position;go.transform.localScale=scale;
        go.GetComponent<Renderer>().sharedMaterial=material;Destroy(go.GetComponent<Collider>());
    }
    void OnDestroy()
    {
        if(stone!=null)
        {
            RenderSettings.fog=oldFog;RenderSettings.fogMode=oldFogMode;RenderSettings.fogColor=oldFogColor;RenderSettings.fogDensity=oldFogDensity;RenderSettings.ambientLight=oldAmbient;
            Destroy(stone);
        }
        if(trim!=null)Destroy(trim);if(seam!=null)Destroy(seam);if(tile!=null)Destroy(tile);
        if(profile!=null){foreach(var component in profile.components)Destroy(component);Destroy(profile);}
    }
}
