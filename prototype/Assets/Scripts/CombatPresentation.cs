using UnityEngine;
using UnityEngine.UI;

// Shared presentation and time effects so hitstop cannot overwrite a special's slow motion.
public class CombatPresentation : MonoBehaviour
{
    public AudioClip swingSound, hitSound, warningSound, slamSound, specialSound;
    [Range(0,1)] public float soundVolume=.35f;
    public static CombatPresentation Instance { get; private set; }
    public Material effectMaterial { get; private set; }
    AudioSource audioSource, ambience;
    AudioClip[] generated;
    float hitUntil, slowUntil, flashAlpha, nextSound;
    float nextWarning,nextSlam;
    Color flashColor;
    Image flash;
    Image damageFlash;
    float damageAlpha;
    bool ownsTime;
    void Awake()
    {
        Instance=this;
        effectMaterial=new Material(Shader.Find("Sprites/Default"));
        audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;
        generated=new[]{Tone("Blade",.16f,520,90,.65f),Tone("Impact",.22f,135,45,.55f),Tone("Warning",.28f,340,520,.12f),Tone("Slam",.65f,85,25,.48f),Tone("Special",.9f,190,50,.32f),Tone("Arena ambience",8,48,48,.045f)};
        ambience=gameObject.AddComponent<AudioSource>();ambience.clip=generated[5];ambience.loop=true;ambience.volume=.12f;ambience.Play();
        var go=new GameObject("Combat Screen Flash",typeof(Canvas));go.transform.SetParent(transform,false);
        var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=15;
        var image=new GameObject("Flash",typeof(RectTransform),typeof(Image));image.transform.SetParent(go.transform,false);
        flash=image.GetComponent<Image>();flash.raycastTarget=false;
        var r=flash.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=Vector2.zero;flash.color=Color.clear;
        var damage=new GameObject("Damage Flash",typeof(RectTransform),typeof(Image));
        damage.transform.SetParent(go.transform,false);
        damageFlash=damage.GetComponent<Image>();damageFlash.raycastTarget=false;
        var dr=damageFlash.rectTransform;dr.anchorMin=Vector2.zero;dr.anchorMax=Vector2.one;dr.sizeDelta=Vector2.zero;
        damageFlash.color=Color.clear;
    }
    static AudioClip Tone(string name,float seconds,float start,float end,float noise)
    {
        const int rate=22050;int length=Mathf.CeilToInt(seconds*rate);var samples=new float[length];
        var random=new System.Random(name.Length*123);float phase=0,filtered=0;
        for(int i=0;i<length;i++)
        {
            float p=i/(float)length;phase+=2*Mathf.PI*Mathf.Lerp(start,end,p)/rate;
            filtered=Mathf.Lerp(filtered,(float)random.NextDouble()*2-1,.18f);
            float envelope=name=="Arena ambience"?.6f:Mathf.Min(1,p*40)*Mathf.Pow(1-p,2);
            samples[i]=(Mathf.Sin(phase)*(1-noise)+filtered*noise)*envelope*.45f;
        }
        var clip=AudioClip.Create(name,length,1,rate,false);clip.SetData(samples,0);return clip;
    }
    public static void Sound(int kind,Vector3 position)
    {
        var self=Instance;if(self==null)return;
        if(kind<2 && Time.unscaledTime<self.nextSound)return;
        if(kind==2 && Time.unscaledTime<self.nextWarning)return;
        if(kind==3 && Time.unscaledTime<self.nextSlam)return;
        if(kind<2)self.nextSound=Time.unscaledTime+.055f;
        if(kind==2)self.nextWarning=Time.unscaledTime+.12f;
        if(kind==3)self.nextSlam=Time.unscaledTime+.1f;
        AudioClip[] overrides={self.swingSound,self.hitSound,self.warningSound,self.slamSound,self.specialSound};
        var clip=overrides[kind]!=null?overrides[kind]:self.generated[kind];
        float distance=Camera.main!=null?Vector3.Distance(Camera.main.transform.position,position):0;
        float volume=self.soundVolume*Mathf.Clamp01(1-distance/42f);
        self.audioSource.PlayOneShot(clip,volume);
    }
    public static void HitStop(float seconds)
    {if(Instance!=null)Instance.hitUntil=Mathf.Max(Instance.hitUntil,Time.unscaledTime+seconds);}
    public static void PlayerDamage()
    {
        var self=Instance;if(self==null)return;
        HitStop(.08f);
        self.damageAlpha=.24f;
        self.damageFlash.color=new Color(.85f,.015f,.025f,self.damageAlpha);
    }
    public static void Special(Element element,Vector3 position)
    {
        var self=Instance;if(self==null)return;
        self.slowUntil=Time.unscaledTime+.38f;
        self.flashColor=ElementUtil.GetColor(element);self.flashAlpha=.12f;
        CameraFollow.Punch(9,.7f);Sound(4,position);
    }
    public static void Impact(Vector3 position,Color color,bool heavy)
    {
        CombatPulse.Create(position,color,heavy?1.6f:.65f,heavy?.35f:.19f);
        if(Instance==null)return;
        Sound(1,position);
    }
    void Update()
    {
        float scale=Time.unscaledTime<hitUntil?.035f:(Time.unscaledTime<slowUntil?.32f:1);
        if(scale<1){Time.timeScale=scale;ownsTime=true;}
        else if(ownsTime){Time.timeScale=1;ownsTime=false;}
        flashAlpha=Mathf.MoveTowards(flashAlpha,0,Time.unscaledDeltaTime*.45f);
        flash.color=new Color(flashColor.r,flashColor.g,flashColor.b,flashAlpha);
        damageAlpha=Mathf.MoveTowards(damageAlpha,0,Time.unscaledDeltaTime*.8f);
        damageFlash.color=new Color(.85f,.015f,.025f,damageAlpha);
    }
    void OnDestroy()
    {
        if(Instance==this){Instance=null;if(ownsTime)Time.timeScale=1;}
        if(effectMaterial!=null)Destroy(effectMaterial);
        if(generated!=null)foreach(var clip in generated)Destroy(clip);
    }
}
