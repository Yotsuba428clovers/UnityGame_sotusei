using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// Explicit gamepad navigation avoids battle EventSystem navigation being disabled.
public sealed class RewardUI : MonoBehaviour
{
    UpgradeManager upgrades;
    GameObject panel;
    Text heading, details, footer;
    readonly Button[] buttons = new Button[3];
    readonly Text[] labels = new Text[3];
    int selected;
    bool wasOpen;
    Font uiFont;
    public void Initialize(UpgradeManager source)
    {
        upgrades = source;
        var hud=GetComponent<HUD>();
        uiFont=hud!=null&&hud.font!=null?hud.font:Font.CreateDynamicFontFromOSFont(new[]{"Yu Gothic","Meiryo","Arial"},25);
        if(EventSystem.current==null)
        {
            var events=new GameObject("Run EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform,false);
        }
        var canvas = new GameObject("Run choices", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(transform, false);
        var c = canvas.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 60;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900);
        panel = new GameObject("Panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(canvas.transform, false);
        var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(.025f,.035f,.06f,1f);
        heading = Label(panel.transform, new Vector2(0,260), new Vector2(1350,90), 42);
        details = Label(panel.transform, new Vector2(0,150), new Vector2(1350,100), 24);
        footer = Label(panel.transform, new Vector2(0,-270), new Vector2(1350,80), 23);
        for (int i=0;i<3;i++)
        {
            int index=i;
            var go = new GameObject("Choice " + i, typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(panel.transform,false);
            var r=go.GetComponent<RectTransform>(); r.sizeDelta=new Vector2(430,260); r.anchoredPosition=new Vector2((i-1)*470,-50);
            buttons[i]=go.GetComponent<Button>(); labels[i]=Label(go.transform,Vector2.zero,new Vector2(390,240),25);
            buttons[i].onClick.AddListener(()=>Activate(index));
        }
        panel.SetActive(false);
    }
    Text Label(Transform parent,Vector2 position,Vector2 size,int fontSize)
    {
        var go=new GameObject("Text",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
        var r=go.GetComponent<RectTransform>();r.sizeDelta=size;r.anchoredPosition=position;
        var t=go.GetComponent<Text>(); t.font=uiFont;t.fontSize=fontSize;t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;t.raycastTarget=false; LocalizedLabel.Bind(t); return t;
    }
    void Activate(int index)
    {
        if(SettingsMenu.BlocksGameplay)return;
        if(upgrades.IsChoosing) upgrades.Choose(index);
        else if(GameManager.Instance.State!=GameManager.GameState.Playing)
        {
            if(index==0)GameManager.Instance.RestartBattle();
            else if(index==1) { Time.timeScale=1; SceneManager.LoadScene("TitleScene"); }
        }
    }
    void Update()
    {
        if(upgrades==null)return;
        var gm=GameManager.Instance;
        bool result=gm!=null&&(gm.State==GameManager.GameState.GameOver || gm.ResultVisible);
        bool open=upgrades.IsChoosing||result;
        panel.SetActive(open);
        if(!open){wasOpen=false;return;}
        if(!wasOpen){selected=0;wasOpen=true;}
        if(SettingsMenu.BlocksGameplay)return;
        int count=upgrades.IsChoosing?upgrades.Choices.Count:2;
        var pad=Gamepad.current;var key=Keyboard.current;
        if((key!=null&&key.rightArrowKey.wasPressedThisFrame)||(pad!=null&&pad.dpad.right.wasPressedThisFrame))selected=(selected+1)%count;
        if((key!=null&&key.leftArrowKey.wasPressedThisFrame)||(pad!=null&&pad.dpad.left.wasPressedThisFrame))selected=(selected+count-1)%count;
        heading.text=upgrades.IsChoosing?UiLanguage.Pick("強化を1つ選択","Choose an upgrade"):(gm.State==GameManager.GameState.Cleared?"RUN CLEAR":"RUN FAILED");
        details.text=upgrades.IsChoosing?UiLanguage.Pick("獲得した強化は今回のランだけ有効です","Upgrades last for this run only"):$"TIME {System.TimeSpan.FromSeconds(gm.ClearTime):mm\\:ss}   KILLS {gm.Kills}   MAX COMBO {gm.MaxCombo}";
        if(upgrades.IsChoosing)footer.text=UiLanguage.Pick("← → / 十字キーで選択　Enter / ×で決定　1・2・3でも選択可能","Arrow keys / D-pad: select   Enter / Cross: confirm   1 / 2 / 3: quick select");
        else
        {
            var records=SaveManager.Load();
            footer.text=UiLanguage.English?$"Personal best: {records.bestKills} kills / {records.bestCombo} hits   Clears: {records.clears} / {records.runs} runs\nUpgrades do not carry over.":$"自己ベスト: {records.bestKills}撃破 / {records.bestCombo}Hit　クリア {records.clears} / {records.runs}ラン\n能力強化は持ち越しません。";
        }
        for(int i=0;i<3;i++)
        {
            buttons[i].gameObject.SetActive(i<count);
            if(i>=count)continue;
            buttons[i].GetComponent<RectTransform>().anchoredPosition=new Vector2((i-(count-1)*.5f)*470,-50);
            buttons[i].GetComponent<Image>().color=i==selected?new Color(.38f,.30f,.13f):new Color(.09f,.12f,.18f);
            labels[i].text=upgrades.IsChoosing?$"{i+1}. {UiLanguage.UpgradeName(upgrades.Choices[i])}\n{upgrades.Choices[i].rarity}\n\n{UiLanguage.UpgradeDescription(upgrades.Choices[i])}":(i==0?UiLanguage.Pick("再挑戦","Play again"):UiLanguage.Pick("タイトルへ","Back to title"));
        }
        if(key!=null&&upgrades.IsChoosing)
        {
            if(key.digit1Key.wasPressedThisFrame){Activate(0);return;}
            if(key.digit2Key.wasPressedThisFrame){Activate(1);return;}
            if(key.digit3Key.wasPressedThisFrame){Activate(2);return;}
        }
        if((key!=null&&key.enterKey.wasPressedThisFrame)||(pad!=null&&pad.buttonSouth.wasPressedThisFrame))Activate(selected);
    }
}
