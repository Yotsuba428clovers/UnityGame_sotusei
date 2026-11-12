using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public Font font;
    PlayerCombat player;
    Canvas canvas;
    RectTransform root, special;
    Image hpFill, hpTrail, objectiveFill;
    Text hpLabel, chargeLabel, specialLabel, comboLabel, killsLabel, buffsLabel, banner;
    BattleRingGraphic chargeRing, readyGlow, innerRing;
    Image[] elements=new Image[4];
    RectTransform[] elementRects=new RectTransform[4];
    CanvasGroup comboGroup;
    float displayedHp=1, trailingHp=1, displayedCharge, comboScale=1;
    int previousCombo;
    bool wasReady;
    RectTransform clearPanel;
    CanvasGroup clearGroup;
    Text clearStats, resultHeading;
    Button retryButton;
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
        var shadow=r.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(1,-1);LocalizedLabel.Bind(text);return text;
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

    Button ResultButton(string name, Vector2 position, string label, UnityEngine.Events.UnityAction action)
    {
        var background = Box(name, clearPanel, new Vector2(.5f, .5f), position, new Vector2(245, 54),
            new Color(.24f, .22f, .14f, 1f));
        background.raycastTarget = true;
        var button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(action);
        Label("Label", background.transform, new Vector2(.5f, .5f), Vector2.zero,
            new Vector2(220, 50), 22, White).text = label;
        return button;
    }

    void Start()
    {
        var go=new GameObject("Battle HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        go.transform.SetParent(transform,false);canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
        var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        root=go.GetComponent<RectTransform>();
        var hp=Rect("Health",root,new Vector2(0,1),new Vector2(50,-52),new Vector2(1040,48));hp.pivot=new Vector2(0,.5f);
        Box("HP label backing",hp,new Vector2(0,.5f),new Vector2(77,0),new Vector2(154,38),new Color(.34f,.30f,.17f,.65f));
        hpLabel=Label("HP",hp,new Vector2(0,.5f),new Vector2(77,0),new Vector2(148,40),22,White);
        hpFill=Bar("Health bar",hp,new Vector2(0,.5f),new Vector2(324,0),new Vector2(320,14),new Color(.4f,.96f,.19f));
        hpTrail=Box("Damage trail",hpFill.transform.parent,new Vector2(0,.5f),Vector2.zero,new Vector2(320,14),new Color(.85f,.56f,.29f));
        hpTrail.rectTransform.pivot=new Vector2(0,.5f);hpTrail.transform.SetAsFirstSibling();
        Box("Gold health rule",hp,new Vector2(0,.5f),new Vector2(324,-14),new Vector2(320,1),new Color(.88f,.8f,.5f,.45f));
        buffsLabel=Label("Wind buff",root,new Vector2(0,1),new Vector2(160,-113),new Vector2(300,36),22,Gold,TextAnchor.MiddleLeft);
        buffsLabel.rectTransform.pivot=new Vector2(0,.5f);

        special=Rect("Special attack",root,new Vector2(0,1),new Vector2(101,-139),new Vector2(156,156));
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
        Label("Special input",special,new Vector2(.5f,.5f),new Vector2(0,-129),new Vector2(260,26),23,White).text="F / R1  ·  SPECIAL";

        var combo=Rect("Combo",root,new Vector2(0,0),new Vector2(50,99),new Vector2(190,90));combo.pivot=new Vector2(0,.5f);
        comboGroup=combo.gameObject.AddComponent<CanvasGroup>();comboGroup.alpha=0;
        Box("Combo panel",combo,new Vector2(.5f,.5f),Vector2.zero,new Vector2(190,90),new Color(.09f,.09f,.08f,.6f));
        Box("Combo gold edge",combo,new Vector2(0,.5f),Vector2.zero,new Vector2(2,90),Gold);
        comboLabel=Label("Hit count",combo,new Vector2(.5f,.5f),Vector2.zero,new Vector2(164,80),54,White,TextAnchor.MiddleLeft);

        var selector=Rect("Elements",root,new Vector2(1,0),new Vector2(-240,111),new Vector2(400,155));
        Element[] order = { Element.Fire, Element.Wind, Element.Ice, Element.Lightning };
        string[] icons = { "▲", "↻", "❄", "ϟ" };
        string[] directions = { "↑", "→", "↓", "←" };
        for(int i=0;i<4;i++)
        {
            Element element = order[i];
            int index = (int)element;
            string name = ElementUtil.GetLabel(element);
            var card=Box(name,selector,new Vector2(.5f,.5f),new Vector2(-144+i*96,0),new Vector2(82,100),Dark);
            elements[index]=card;elementRects[index]=card.rectTransform;
            Box("Element accent",card.transform,new Vector2(.5f,1),new Vector2(0,-1),new Vector2(82,3),ElementUtil.GetColor(element));
            Label("Glyph",card.transform,new Vector2(.5f,.5f),new Vector2(0,19),new Vector2(80,43),33,ElementUtil.GetColor(element)).text=icons[i];
            Label("Name",card.transform,new Vector2(.5f,.5f),new Vector2(0,-9),new Vector2(82,22),14,White).text=name;
            Label("Shortcut",card.transform,new Vector2(.5f,.5f),new Vector2(0,-34),new Vector2(82,22),16,White).text=(i+1)+" / "+directions[i];
        }
        var objective = Rect("Stage objective", root, new Vector2(.5f, 1f), new Vector2(0f, -50f), new Vector2(230f, 60f));
        Box("Objective backing", objective, new Vector2(.5f, .5f), Vector2.zero, new Vector2(230f, 60f), Dark);
        killsLabel=Label("K.O. count",objective,new Vector2(.5f,.5f),new Vector2(0,6),new Vector2(230,40),28,White);
        objectiveFill=Bar("Stage progress",objective,new Vector2(.5f,.5f),new Vector2(0,-17),new Vector2(178,5),Gold);
        banner=Label("Battle banner",root,new Vector2(.5f,.5f),new Vector2(0,140),new Vector2(1300,100),64,White);
        clearPanel=Rect("Stage results",root,new Vector2(.74f,.5f),Vector2.zero,new Vector2(600,480));
        clearGroup=clearPanel.gameObject.AddComponent<CanvasGroup>();clearGroup.alpha=0;clearGroup.blocksRaycasts=false;
        Box("Result backing",clearPanel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(600,480),new Color(.035f,.045f,.07f,.94f));
        Box("Gold rule",clearPanel,new Vector2(.5f,.5f),new Vector2(0,122),new Vector2(510,2),Gold);
        resultHeading=Label("Result heading",clearPanel,new Vector2(.5f,.5f),new Vector2(0,172),new Vector2(545,86),49,Gold);
        clearStats=Label("Stage stats",clearPanel,new Vector2(.5f,.5f),new Vector2(0,-5),new Vector2(460,190),24,White,TextAnchor.MiddleLeft);
        retryButton=ResultButton("Retry",new Vector2(-132,-164),"PLAY AGAIN  /  R",() => GameManager.Instance?.RestartBattle());
        ResultButton("Title",new Vector2(132,-164),"BACK TO TITLE",() => GameManager.Instance?.ReturnToTitle());
        clearPanel.gameObject.SetActive(false);
    }
    void Update()
    {
        if(root==null)return;
        if(player==null)player=FindFirstObjectByType<PlayerCombat>();
        var gm=GameManager.Instance;canvas.enabled=player!=null&&gm!=null;if(!canvas.enabled)return;
        if (gm.ResultVisible)
        {
            for(int i=0;i<root.childCount;i++)
            {
                var child=root.GetChild(i);
                if(child!=clearPanel)child.gameObject.SetActive(false);
            }
            if (!clearPanel.gameObject.activeSelf)
            {
                bool cleared = gm.State == GameManager.GameState.Cleared;
                clearPanel.anchorMin = clearPanel.anchorMax = new Vector2(cleared ? .74f : .5f, .5f);
                resultHeading.text = cleared ? "STAGE CLEAR" : "GAME OVER";
                clearPanel.gameObject.SetActive(true);
                clearGroup.blocksRaycasts = true;
                if (EventSystem.current != null && !SettingsMenu.IsOpen)
                {
                    EventSystem.current.sendNavigationEvents = true;
                    retryButton.Select();
                }
            }
            clearGroup.alpha=Mathf.MoveTowards(clearGroup.alpha,1f,Time.unscaledDeltaTime*5);
            clearStats.text="TIME                 "+System.TimeSpan.FromSeconds(gm.ClearTime).ToString(@"mm\:ss")
                +"\n\nENEMIES DEFEATED     "+gm.Kills+" / "+GameManager.KillGoal+"\n\nMAX COMBO            "+gm.MaxCombo;
            return;
        }
        float dt=Time.unscaledDeltaTime,blend=1-Mathf.Exp(-10*dt);
        float hp=Mathf.Clamp01(player.Hp/Mathf.Max(1,player.maxHp));
        displayedHp=Mathf.Lerp(displayedHp,hp,blend);trailingHp=Mathf.MoveTowards(trailingHp,hp,dt*.25f);
        SetBar(hpFill,displayedHp,320);SetBar(hpTrail,Mathf.Max(displayedHp,trailingHp),320);
        hpLabel.text="HP  "+Mathf.CeilToInt(player.Hp)+" / "+Mathf.CeilToInt(player.maxHp);
        float gauge=Mathf.Clamp01(player.Gauge/Mathf.Max(1,player.maxGauge));
        displayedCharge=Mathf.MoveTowards(displayedCharge,gauge,dt*1.5f);chargeRing.fill=displayedCharge;
        bool ready=gauge>=.999f;if(ready&&!wasReady)special.localScale=Vector3.one*1.12f;wasReady=ready;
        float pulse=ready?1+Mathf.Sin(Time.unscaledTime*4)*.025f:1;
        special.localScale=Vector3.Lerp(special.localScale,Vector3.one*(pulse*.66f),blend);
        readyGlow.color=new Color(Lime.r,Lime.g,Lime.b,ready?.25f+Mathf.Sin(Time.unscaledTime*4)*.15f:0);
        innerRing.color=Color.Lerp(innerRing.color,ElementUtil.GetColor(player.CurrentElement),blend);
        chargeLabel.text=Mathf.RoundToInt(gauge*100)+"%";specialLabel.text=ready?"SPECIAL READY":"CHARGING";specialLabel.color=ready?Lime:Gold;
        float targetAlpha=gm.Combo>1&&gm.ComboActive?1:0;
        comboGroup.alpha=Mathf.MoveTowards(comboGroup.alpha,targetAlpha,dt*5);
        if(gm.Combo!=previousCombo&&gm.Combo>1)comboScale=1.13f;
        previousCombo=gm.Combo;comboScale=Mathf.Lerp(comboScale,1,blend);comboLabel.rectTransform.localScale=Vector3.one*comboScale;
        comboLabel.text=gm.Combo+" <size=20>HIT</size>";
        for(int i=0;i<4;i++)
        {
            bool active=i==(int)player.CurrentElement;
            Color tint=active?Color.Lerp(Dark,ElementUtil.GetColor((Element)i),.3f):Dark;
            elements[i].color=Color.Lerp(elements[i].color,tint,blend);
            elementRects[i].localScale=Vector3.Lerp(elementRects[i].localScale,Vector3.one*(active?1.1f:1),blend);
        }
        killsLabel.text="K.O.  "+gm.Kills+" / "+GameManager.KillGoal;
        SetBar(objectiveFill,gm.Kills/(float)GameManager.KillGoal,178);
        buffsLabel.text=player.WindBuffRemaining>0?"↻  WIND MODE  "+player.WindBuffRemaining.ToString("0.0")+"s":"";
        banner.text=gm.BannerRemaining>0?gm.BannerText:"";
    }
}
