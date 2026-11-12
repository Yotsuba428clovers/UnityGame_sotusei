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
    Material sky;
    Material oldSky;
    AmbientMode oldAmbientMode;
    Color oldSkyAmbient,oldEquatorAmbient,oldGroundAmbient;
    Light oldSun;
    void Start()
    {
        oldFog=RenderSettings.fog;oldFogMode=RenderSettings.fogMode;oldFogColor=RenderSettings.fogColor;oldFogDensity=RenderSettings.fogDensity;oldAmbient=RenderSettings.ambientLight;
        oldSky=RenderSettings.skybox;oldSun=RenderSettings.sun;oldAmbientMode=RenderSettings.ambientMode;
        oldSkyAmbient=RenderSettings.ambientSkyColor;oldEquatorAmbient=RenderSettings.ambientEquatorColor;oldGroundAmbient=RenderSettings.ambientGroundColor;
        stone=new Material(Shader.Find("Universal Render Pipeline/Lit"));stone.color=new Color(.44f,.47f,.41f);stone.SetFloat("_Smoothness",.15f);
        seam=new Material(stone);seam.color=new Color(.10f,.13f,.16f);
        tile=new Material(stone);tile.color=new Color(.20f,.24f,.27f);
        trim=new Material(Shader.Find("Universal Render Pipeline/Lit"));trim.color=new Color(.16f,.40f,.39f);trim.EnableKeyword("_EMISSION");trim.SetColor("_EmissionColor",new Color(.14f,.75f,.65f)*1.5f);
        var ground=GameObject.Find("Ground");if(ground!=null&&ground.GetComponent<RuntimeGroundMaterial>()==null)ground.GetComponent<Renderer>().sharedMaterial=stone;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.68f,.84f,.92f);RenderSettings.fogDensity=.006f;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.66f,.78f,.91f);
        RenderSettings.ambientEquatorColor=new Color(.62f,.68f,.69f);
        RenderSettings.ambientGroundColor=new Color(.34f,.38f,.28f);
        Light light=null;
        foreach(var candidate in FindObjectsByType<Light>(FindObjectsSortMode.None))if(candidate.type==LightType.Directional){light=candidate;break;}
        if(light!=null){light.color=new Color(1,.95f,.82f);light.intensity=1.15f;light.transform.rotation=Quaternion.Euler(48,-35,0);light.shadows=LightShadows.Soft;RenderSettings.sun=light;}
        // A scene-authored HDR panorama takes precedence over the prototype sky.
        var skyAsset=oldSky!=null && oldSky.shader.name=="Skybox/Panoramic"
            ?oldSky:Resources.Load<Material>("Environment/DaylightSky");
        if(skyAsset==oldSky && oldSky!=null)RenderSettings.fogDensity=.0047f;
        if(skyAsset!=null)
        {
            sky=new Material(skyAsset);
            if(light!=null&&sky.HasProperty("_SunDirection"))sky.SetVector("_SunDirection",-light.transform.forward);
            RenderSettings.skybox=sky;
        }
        // Keep the prototype pillars only in scenes without authored scenery.
        bool hasScenery=System.Array.Exists(gameObject.scene.GetRootGameObjects(),
            root=>root.name=="Battle Scenery" && root.activeInHierarchy && root.transform.childCount>0);
        for(int i=0;!hasScenery && i<16;i++)
        {
            float a=i*Mathf.PI*2/16;var pos=new Vector3(Mathf.Cos(a)*32,0,Mathf.Sin(a)*32);
            float h=4+(i%3)*1.4f;
            Block("Ruined pillar",pos+Vector3.up*h*.5f,new Vector3(1.7f,h,1.7f),stone);
            Block("Pillar cap",pos+Vector3.up*h,new Vector3(2.2f,.35f,2.2f),stone);
            Block("Rune",pos+Vector3.up*(h*.65f),new Vector3(1.78f,.13f,1.78f),trim);
        }
        var volume=gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=10;
        profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;
        var bloom=profile.Add<Bloom>();bloom.intensity.Override(.35f);bloom.threshold.Override(1.1f);
        var vignette=profile.Add<Vignette>();vignette.intensity.Override(.22f);vignette.smoothness.Override(.45f);
        var color=profile.Add<ColorAdjustments>();color.contrast.Override(8);color.saturation.Override(5);
        if(Camera.main!=null){var data=Camera.main.GetComponent<UniversalAdditionalCameraData>();if(data!=null)data.renderPostProcessing=true;Camera.main.clearFlags=CameraClearFlags.Skybox;Camera.main.backgroundColor=RenderSettings.fogColor;}
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
            RenderSettings.skybox=oldSky;RenderSettings.sun=oldSun;RenderSettings.ambientMode=oldAmbientMode;
            RenderSettings.ambientSkyColor=oldSkyAmbient;RenderSettings.ambientEquatorColor=oldEquatorAmbient;RenderSettings.ambientGroundColor=oldGroundAmbient;
            Destroy(stone);
        }
        if(trim!=null)Destroy(trim);if(seam!=null)Destroy(seam);if(tile!=null)Destroy(tile);
        if(sky!=null)Destroy(sky);
        if(profile!=null){foreach(var component in profile.components)Destroy(component);Destroy(profile);}
    }
}
