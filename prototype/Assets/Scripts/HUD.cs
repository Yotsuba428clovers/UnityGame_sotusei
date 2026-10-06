using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public Font font;
    PlayerCombat player;
    Canvas canvas;
    RectTransform root, special;
    Image hpFill, hpTrail, bonusFill, bossFill;
    Text hpLabel, chargeLabel, specialLabel, comboLabel, bonusLabel, killsLabel, buffsLabel, banner, result, bossLabel;
    BattleRingGraphic chargeRing, readyGlow, innerRing;
    Image[] elements=new Image[4];
    RectTransform[] elementRects=new RectTransform[4];
    CanvasGroup comboGroup;
    float displayedHp=1, trailingHp=1, displayedCharge, comboScale=1;
    int previousCombo;
    bool wasReady;
    static readonly Color Gold=new Color(.88f,.80f,.50f);
    static readonly Color Lime=new Color(.74f,1,.22f);
    static readonly Color White=new Color(.97f,.95f,.87f);
    static readonly Color Dark=new Color(.035f,.045f,.07f,.78f);

    RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));
        var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=anchor;r.sizeDelta=size;r.anchoredPosition=pos;return r;
    }
    Image Box(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
    {
        var r=Rect(name,parent,anchor,pos,size);var image=r.gameObject.AddComponent<Image>();
        image.color=color;image.raycastTarget=false;return image;
    }
    Text Label(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,int fontSize,Color color,TextAnchor align=TextAnchor.MiddleCenter)
    {
        var r=Rect(name,parent,anchor,pos,size);var text=r.gameObject.AddComponent<Text>();
        text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.color=color;
        text.alignment=align;text.raycastTarget=false;text.verticalOverflow=VerticalWrapMode.Overflow;
        var shadow=r.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(1,-1);return text;
    }
    BattleRingGraphic Ring(string name,Transform parent,float diameter,Color color,float thickness)
    {
        var r=Rect(name,parent,new Vector2(.5f,.5f),Vector2.zero,Vector2.one*diameter);
        var ring=r.gameObject.AddComponent<BattleRingGraphic>();ring.color=color;ring.thickness=thickness;ring.raycastTarget=false;return ring;
    }
    Image Bar(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
    {
        var track=Box(name,parent,anchor,pos,size,new Color(.08f,.09f,.11f,.85f));
        var fill=Box("Fill",track.transform,new Vector2(0,.5f),Vector2.zero,size,color);
        fill.rectTransform.pivot=new Vector2(0,.5f);return fill;
    }
    static void SetBar(Image image,float amount,float width)
    {image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width*Mathf.Clamp01(amount));}

    void Start()
    {
        var go=new GameObject("Battle HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        go.transform.SetParent(transform,false);canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
        var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        root=go.GetComponent<RectTransform>();
        var hp=Rect("Health",root,new Vector2(0,1),new Vector2(50,-52),new Vector2(1040,48));hp.pivot=new Vector2(0,.5f);
        Box("HP label backing",hp,new Vector2(0,.5f),new Vector2(77,0),new Vector2(154,38),new Color(.34f,.30f,.17f,.65f));
        hpLabel=Label("HP",hp,new Vector2(0,.5f),new Vector2(77,0),new Vector2(148,40),22,White);
        hpFill=Bar("Health bar",hp,new Vector2(0,.5f),new Vector2(576,0),new Vector2(810,14),new Color(.4f,.96f,.19f));
        hpTrail=Box("Damage trail",hpFill.transform.parent,new Vector2(0,.5f),Vector2.zero,new Vector2(810,14),new Color(.85f,.56f,.29f));
        hpTrail.rectTransform.pivot=new Vector2(0,.5f);hpTrail.transform.SetAsFirstSibling();
        Box("Gold health rule",hp,new Vector2(0,.5f),new Vector2(576,-14),new Vector2(810,1),new Color(.88f,.8f,.5f,.45f));
        buffsLabel=Label("Active buffs",root,new Vector2(0,1),new Vector2(50,-99),new Vector2(900,36),18,Gold,TextAnchor.MiddleLeft);buffsLabel.rectTransform.pivot=new Vector2(0,.5f);

        special=Rect("Special attack",root,new Vector2(.5f,0),new Vector2(0,139),new Vector2(156,156));
        readyGlow=Ring("Ready aura",special,166,new Color(.74f,1,.22f,0),.05f);
        Ring("Outer gold rim",special,142,Gold,.015f);
        Ring("Dark disc",special,130,Dark,1);
        Ring("Charge track",special,130,new Color(.31f,.35f,.17f,.85f),.075f);
        chargeRing=Ring("Circular special charge",special,130,Lime,.075f);chargeRing.fill=0;
        innerRing=Ring("Inner element ring",special,101,new Color(.88f,.25f,.94f),.025f);
        var flameRect=Rect("Special flame",special,new Vector2(.5f,.5f),Vector2.zero,new Vector2(54,62));
        var flame=flameRect.gameObject.AddComponent<BattleFlameGraphic>();flame.color=Lime;flame.raycastTarget=false;
        chargeLabel=Label("Charge percent",special,new Vector2(.5f,.5f),new Vector2(0,94),new Vector2(160,34),25,White);
        specialLabel=Label("Special status",special,new Vector2(.5f,.5f),new Vector2(0,-100),new Vector2(290,30),17,Gold);
        Label("Special input",special,new Vector2(.5f,.5f),new Vector2(0,-129),new Vector2(260,26),14,White).text="B / L  ·  SPECIAL";

        var combo=Rect("Combo",root,new Vector2(0,0),new Vector2(62,154),new Vector2(270,170));combo.pivot=new Vector2(0,.5f);
        comboGroup=combo.gameObject.AddComponent<CanvasGroup>();comboGroup.alpha=0;
        Box("Combo panel",combo,new Vector2(.5f,.5f),Vector2.zero,new Vector2(270,170),new Color(.09f,.09f,.08f,.6f));
        Box("Combo gold edge",combo,new Vector2(0,.5f),Vector2.zero,new Vector2(2,170),Gold);
        comboLabel=Label("Hit count",combo,new Vector2(.5f,.5f),new Vector2(0,27),new Vector2(244,90),64,White,TextAnchor.MiddleLeft);
        bonusLabel=Label("Switch bonus",combo,new Vector2(.5f,.5f),new Vector2(0,-35),new Vector2(244,32),18,Gold,TextAnchor.MiddleLeft);
        bonusFill=Bar("Bonus duration",combo,new Vector2(.5f,.5f),new Vector2(0,-62),new Vector2(244,4),Lime);

        var selector=Rect("Elements",root,new Vector2(1,0),new Vector2(-240,155),new Vector2(400,155));
        Label("Element heading",selector,new Vector2(.5f,.5f),new Vector2(0,72),new Vector2(400,26),15,Gold).text="ELEMENT SHIFT";
        string[] names={"FIRE","ICE","THUNDER","WIND"};string[] icons={"F","I","T","W"};
        for(int i=0;i<4;i++)
        {
            var card=Box(names[i],selector,new Vector2(.5f,.5f),new Vector2(-144+i*96,0),new Vector2(82,92),Dark);
            elements[i]=card;elementRects[i]=card.rectTransform;
            Box("Element accent",card.transform,new Vector2(.5f,1),new Vector2(0,-1),new Vector2(82,3),ElementUtil.GetColor((Element)i));
            Label("Glyph",card.transform,new Vector2(.5f,.5f),new Vector2(0,6),new Vector2(80,56),36,ElementUtil.GetColor((Element)i)).text=icons[i];
            Label("Name",card.transform,new Vector2(.5f,.5f),new Vector2(0,-30),new Vector2(82,24),11,White).text=names[i];
        }
        Label("Element input",selector,new Vector2(.5f,.5f),new Vector2(0,-75),new Vector2(400,26),15,White).text="Q / E   ·   LB / RB";
        killsLabel=Label("Kills",root,new Vector2(1,1),new Vector2(-215,-111),new Vector2(350,40),22,White,TextAnchor.MiddleRight);
        bossLabel=Label("Boss",root,new Vector2(.5f,1),new Vector2(0,-111),new Vector2(600,28),18,Gold);
        bossFill=Bar("Boss health",root,new Vector2(.5f,1),new Vector2(0,-141),new Vector2(700,10),new Color(.85f,.25f,.5f));
        banner=Label("Battle banner",root,new Vector2(.5f,.5f),new Vector2(0,140),new Vector2(1300,100),64,White);
        result=Label("Battle result",root,new Vector2(.5f,.5f),new Vector2(0,58),new Vector2(1300,50),25,Gold);
        Label("Combat inputs",root,new Vector2(1,0),new Vector2(-335,29),new Vector2(590,27),14,new Color(.9f,.9f,.85f,.75f),TextAnchor.MiddleRight).text="J / X  ATTACK    K / Y  HEAVY    SPACE / A  DODGE";
    }
    void Update()
    {
        if(root==null)return;
        if(player==null)player=FindFirstObjectByType<PlayerCombat>();
        var gm=GameManager.Instance;canvas.enabled=player!=null&&gm!=null;if(!canvas.enabled)return;
        float dt=Time.unscaledDeltaTime,blend=1-Mathf.Exp(-10*dt);
        float hp=Mathf.Clamp01(player.Hp/Mathf.Max(1,player.maxHp));
        displayedHp=Mathf.Lerp(displayedHp,hp,blend);trailingHp=Mathf.MoveTowards(trailingHp,hp,dt*.25f);
        SetBar(hpFill,displayedHp,810);SetBar(hpTrail,Mathf.Max(displayedHp,trailingHp),810);
        hpLabel.text="HP  "+Mathf.CeilToInt(player.Hp)+" / "+Mathf.CeilToInt(player.maxHp);
        float gauge=Mathf.Clamp01(player.Gauge/Mathf.Max(1,player.maxGauge));
        displayedCharge=Mathf.MoveTowards(displayedCharge,gauge,dt*1.5f);chargeRing.fill=displayedCharge;
        bool ready=gauge>=.999f;if(ready&&!wasReady)special.localScale=Vector3.one*1.12f;wasReady=ready;
        float pulse=ready?1+Mathf.Sin(Time.unscaledTime*4)*.025f:1;
        special.localScale=Vector3.Lerp(special.localScale,Vector3.one*pulse,blend);
        readyGlow.color=new Color(Lime.r,Lime.g,Lime.b,ready?.25f+Mathf.Sin(Time.unscaledTime*4)*.15f:0);
        innerRing.color=Color.Lerp(innerRing.color,ElementUtil.GetColor(player.CurrentElement),blend);
        chargeLabel.text=Mathf.RoundToInt(gauge*100)+"%";specialLabel.text=ready?"SPECIAL READY":"CHARGING";specialLabel.color=ready?Lime:Gold;
        float targetAlpha=(gm.Combo>1&&gm.ComboActive)||player.SwitchBonusRemaining>0?1:0;
        comboGroup.alpha=Mathf.MoveTowards(comboGroup.alpha,targetAlpha,dt*5);
        if(gm.Combo!=previousCombo&&gm.Combo>1)comboScale=1.13f;
        previousCombo=gm.Combo;comboScale=Mathf.Lerp(comboScale,1,blend);comboLabel.rectTransform.localScale=Vector3.one*comboScale;
        comboLabel.text=gm.Combo+" <size=20>HIT</size>";
        bonusLabel.text=player.SwitchBonusRemaining>0?"SWITCH BONUS  "+Mathf.RoundToInt(player.switchBonusMultiplier*100)+"%":"KEEP THE RHYTHM";
        SetBar(bonusFill,player.SwitchBonusRemaining/Mathf.Max(.01f,player.switchBonusTime),244);
        for(int i=0;i<4;i++)
        {
            bool active=i==(int)player.CurrentElement;
            Color tint=active?Color.Lerp(Dark,ElementUtil.GetColor((Element)i),.3f):Dark;
            elements[i].color=Color.Lerp(elements[i].color,tint,blend);
            elementRects[i].localScale=Vector3.Lerp(elementRects[i].localScale,Vector3.one*(active?1.1f:1),blend);
        }
        killsLabel.text="KILLS  "+gm.Kills+" <color=#B8AB7C>/ "+gm.killsToBoss+"</color>";
        bool boss=Enemy.Boss!=null&&!Enemy.Boss.IsDead;bossLabel.gameObject.SetActive(boss);bossFill.transform.parent.gameObject.SetActive(boss);
        if(boss){bossLabel.text="B O S S";SetBar(bossFill,Enemy.Boss.Hp/Mathf.Max(1,Enemy.Boss.maxHp),700);}
        buffsLabel.text=(player.LightningBuffRemaining>0?"THUNDER MODE  "+player.LightningBuffRemaining.ToString("0.0")+"s   ":"")+(player.WindBuffRemaining>0?"WIND MODE  "+player.WindBuffRemaining.ToString("0.0")+"s":"");
        banner.text=gm.BannerRemaining>0?gm.BannerText:"";
        result.text=gm.State!=GameManager.GameState.Playing?"MAX COMBO  "+gm.MaxCombo+"    ·    START / R TO RESTART":"";
    }
}
