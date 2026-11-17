using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Enemy))]
public sealed class EnemyStatusVisual : MonoBehaviour
{
    [Range(0,1)] public float multiplyStrength=.9f;
    [Range(0,1)] public float screenStrength=.2f;
    [Range(0,1)] public float emissionStrength=.18f;
    static readonly string[] ColorProperties={"_BaseColor","_Color","_1st_ShadeColor","_2nd_ShadeColor"};
    sealed class Slot
    {
        public Material material;
        public readonly Dictionary<string,Color> colors=new Dictionary<string,Color>();
        public Color emission, toonEmission;
        public Texture emissionMap, toonEmissionMap;
        public bool emissionKeyword, simpleKeyword, animationKeyword;
    }
    readonly List<Slot> slots=new List<Slot>();
    readonly List<Renderer> models=new List<Renderer>();
    Enemy enemy;
    GameObject status;
    Text label;
    string lastText;
    bool hasPreviousColor;
    Color previousColor;

    void Awake()
    {
        enemy=GetComponent<Enemy>();
        foreach(var renderer in GetComponentsInChildren<Renderer>(true))
        {
            if(!(renderer is SkinnedMeshRenderer)&&!(renderer is MeshRenderer))continue;
            if(renderer.GetComponent<TextMesh>()!=null)continue;
            models.Add(renderer);
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                if(materials[i]==null)continue;
                var material=new Material(materials[i]);materials[i]=material;
                var slot=new Slot {material=material};
                foreach(var property in ColorProperties)
                    if(material.HasProperty(property))slot.colors[property]=material.GetColor(property);
                if(material.HasProperty("_EmissionColor"))slot.emission=material.GetColor("_EmissionColor");
                if(material.HasProperty("_Emissive_Color"))slot.toonEmission=material.GetColor("_Emissive_Color");
                if(material.HasProperty("_EmissionMap"))slot.emissionMap=material.GetTexture("_EmissionMap");
                if(material.HasProperty("_Emissive_Tex"))slot.toonEmissionMap=material.GetTexture("_Emissive_Tex");
                slot.emissionKeyword=material.IsKeywordEnabled("_EMISSION");
                slot.simpleKeyword=material.IsKeywordEnabled("_EMISSIVE_SIMPLE");
                slot.animationKeyword=material.IsKeywordEnabled("_EMISSIVE_ANIMATION");
                slots.Add(slot);
            }
            renderer.sharedMaterials=materials;
        }
        BuildLabel();
    }
    void BuildLabel()
    {
        status=new GameObject("Active Status",typeof(RectTransform),typeof(Canvas),typeof(Image));
        status.transform.SetParent(transform,false);
        status.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        status.GetComponent<RectTransform>().sizeDelta=new Vector2(420,70);
        var backdrop=status.GetComponent<Image>();backdrop.color=new Color(.015f,.02f,.03f,.78f);backdrop.raycastTarget=false;
        status.transform.localScale=Vector3.one*(.008f/Mathf.Max(.01f,transform.lossyScale.x));
        var text=new GameObject("Status names",typeof(RectTransform),typeof(Text),typeof(Outline));
        text.transform.SetParent(status.transform,false);
        var rect=text.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
        label=text.GetComponent<Text>();label.font=UiLanguage.JapaneseFont;label.fontSize=46;
        label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
        label.resizeTextForBestFit=true;label.resizeTextMinSize=24;label.resizeTextMaxSize=46;
        var outline=text.GetComponent<Outline>();outline.effectColor=new Color(0,0,0,.95f);outline.effectDistance=new Vector2(1,-1);
        status.SetActive(false);
    }
    public Color CurrentTint { get; private set; }
    public string StatusText => label!=null?label.text:"";
    public void RefreshNow()
    {
        bool burn=!enemy.IsDead&&enemy.IsBurning;
        bool ice=!enemy.IsDead&&(enemy.IsFrozen||enemy.IsChilled);
        bool shock=!enemy.IsDead&&enemy.IsShocked;
        bool wind=!enemy.IsDead&&enemy.IsWinded;
        bool broken=!enemy.IsDead&&enemy.IsBroken;
        bool active=burn||ice||shock||wind||broken;
        Color tint=Color.clear;
        if(burn)tint=Tint(Element.Fire);
        if(ice)tint=Tint(Element.Ice);
        if(wind)tint=Tint(Element.Wind);
        if(shock)tint=Tint(Element.Lightning);
        // Keep the most recent active effect visible, but freezing always stays blue.
        var latest=enemy.LastAppliedElement;
        if((latest==Element.Fire&&burn)||(latest==Element.Ice&&ice)||(latest==Element.Lightning&&shock)||(latest==Element.Wind&&wind))tint=Tint(latest);
        if(enemy.IsFrozen&&!enemy.IsDead)tint=Tint(Element.Ice);
        if(broken&&!burn&&!ice&&!shock&&!wind)tint=new Color(.65f,.72f,.8f);
        CurrentTint=tint;
        bool hitFlash = enemy.HitFlash;
        Color visualTint = hitFlash ? Color.Lerp(active ? tint : Color.white, Color.white, .45f) : tint;
        if(!hasPreviousColor||visualTint!=previousColor)
        {
            foreach(var slot in slots)Paint(slot,visualTint,active || hitFlash);
            previousColor=visualTint;hasPreviousColor=true;
        }
        var names=new List<string>();
        if(burn)names.Add(UiLanguage.Pick("炎上","BURNING"));
        if(ice)names.Add(enemy.IsFrozen?UiLanguage.Pick("凍結","FROZEN"):UiLanguage.Pick("冷却","CHILLED"));
        if(shock)names.Add(UiLanguage.Pick("感電","SHOCKED"));
        if(wind)names.Add(UiLanguage.Pick("吹き飛ばし","KNOCKBACK"));
        if(broken)names.Add(UiLanguage.Pick("ブレイク","BREAK"));
        string content=string.Join(" / ",names);
        if(content!=lastText)
        {
            label.text=content;lastText=content;
            status.GetComponent<RectTransform>().sizeDelta=new Vector2(Mathf.Clamp(label.preferredWidth+36,130,480),70);
        }
        status.SetActive(active);
        if(active)
        {
            float top=transform.position.y+1.8f;
            foreach(var model in models)if(model!=null&&model.enabled)top=Mathf.Max(top,model.bounds.max.y);
            status.transform.position=new Vector3(transform.position.x,top+.25f,transform.position.z);
            if(Camera.main!=null)status.transform.rotation=Camera.main.transform.rotation;
            label.color=Color.Lerp(tint,Color.white,.8f);
        }
    }
    static Color Tint(Element element)
    {
        switch(element)
        {
            case Element.Fire:return new Color(1,.035f,.01f);
            case Element.Ice:return new Color(.04f,.55f,1);
            case Element.Lightning:return new Color(1,.78f,.015f);
            default:return new Color(.02f,1,.22f);
        }
    }
    void Paint(Slot slot,Color tint,bool active)
    {
        var material=slot.material;
        foreach(var original in slot.colors)
        {
            var color=original.Value;
            if(active)
            {
                // Strong multiply tint retains texture detail; screen lifts dark surfaces.
                var multiplied=color*Color.Lerp(Color.white,tint,multiplyStrength);
                var screen=Color.white-(Color.white-multiplied)*(Color.white-tint*screenStrength);
                color=new Color(screen.r,screen.g,screen.b,original.Value.a);
            }
            material.SetColor(original.Key,color);
        }
        if(material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor",active?slot.emission+tint*emissionStrength:slot.emission);
            if(material.HasProperty("_EmissionMap"))material.SetTexture("_EmissionMap",active?Texture2D.whiteTexture:slot.emissionMap);
            SetKeyword(material,"_EMISSION",active||slot.emissionKeyword);
        }
        if(material.HasProperty("_Emissive_Color"))
        {
            material.SetColor("_Emissive_Color",active?slot.toonEmission+tint*emissionStrength:slot.toonEmission);
            if(material.HasProperty("_Emissive_Tex"))material.SetTexture("_Emissive_Tex",active?Texture2D.whiteTexture:slot.toonEmissionMap);
            SetKeyword(material,"_EMISSIVE_SIMPLE",active||slot.simpleKeyword);
            SetKeyword(material,"_EMISSIVE_ANIMATION",!active&&slot.animationKeyword);
        }
    }
    static void SetKeyword(Material material,string keyword,bool enabled)
    { if(enabled)material.EnableKeyword(keyword);else material.DisableKeyword(keyword); }
    void LateUpdate() { RefreshNow(); }
    void OnValidate() { hasPreviousColor=false; }
    void OnDisable()
    {
        foreach(var slot in slots)Paint(slot,Color.clear,false);
        hasPreviousColor=false;
        if(status!=null)status.SetActive(false);
    }
    void OnDestroy() { foreach(var slot in slots)if(slot.material!=null)Destroy(slot.material); }
}
