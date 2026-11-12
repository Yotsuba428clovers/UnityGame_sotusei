using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class SettingsMenu : MonoBehaviour
{
    static SettingsMenu instance;
    static int closedFrame = -1;
    public static bool IsOpen => instance != null && instance.opened;
    public static bool BlocksGameplay => IsOpen || Time.frameCount == closedFrame;
    bool opened, previousNavigation, previousAudioPause;
    float previousTimeScale;
    GameObject popup, previousSelection;
    Selectable firstControl;
    readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    int resolutionIndex;
    bool fullscreen;
    Text resolutionText, modeText, qualityText, viewText, vibrationText, languageText;
    CameraFollow follow;
    static readonly Color Gold = new Color(.88f, .8f, .5f);
    static readonly Color Ink = new Color(.035f, .045f, .07f, .98f);

    void Start()
    {
        instance = this;
        closedFrame = -1;
        follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        Rumble.Enabled = GameSettings.Vibration;
        GameSettings.ApplyGraphics();
        foreach (var resolution in Screen.resolutions)
        {
            var size = new Vector2Int(resolution.width, resolution.height);
            if (!resolutions.Contains(size)) resolutions.Add(size);
        }
        if (resolutions.Count == 0) { resolutions.Add(new Vector2Int(1280, 720)); resolutions.Add(new Vector2Int(1920, 1080)); }
        var saved = new Vector2Int(GameSettings.GetInt("Width", Screen.currentResolution.width), GameSettings.GetInt("Height", Screen.currentResolution.height));
        if (!resolutions.Contains(saved) && saved.x > 0 && saved.y > 0) resolutions.Add(saved);
        resolutionIndex = Mathf.Max(0, resolutions.IndexOf(saved));
        fullscreen = GameSettings.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) != 0;
        Build();
    }

    RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
    }

    Text Label(string name, Transform parent, string content, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var text = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content; text.fontSize = fontSize; text.color = color; text.alignment = alignment;
        text.raycastTarget = false; LocalizedLabel.Bind(text); return text;
    }

    Image Box(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
        image.color = color; return image;
    }

    Button Button(string name, Transform parent, string label, Vector2 position, Vector2 size, Action action, out Text text)
    {
        var image = Box(name, parent, position, size, new Color(.12f, .15f, .19f));
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(1f, .9f, .6f); colors.selectedColor = colors.highlightedColor; button.colors = colors;
        text = Label(name + " Label", image.transform, label, Vector2.zero, size - new Vector2(20, 2), 23, Gold, TextAnchor.MiddleCenter);
        button.onClick.AddListener(() => action()); return button;
    }

    void SliderRow(Transform parent, string label, float y, float min, float max, float value, Action<float> change, bool percentage)
    {
        Label(label, parent, label, new Vector2(-305, y), new Vector2(230, 38), 23, Color.white);
        var rect = Rect(label + " Slider", parent, new Vector2(70, y), new Vector2(440, 34));
        rect.gameObject.AddComponent<Image>().color = Color.clear;
        var slider = rect.gameObject.AddComponent<Slider>(); slider.minValue = min; slider.maxValue = max;
        Box("Track", rect, Vector2.zero, new Vector2(440, 6), new Color(.2f, .23f, .28f)).raycastTarget = false;
        var fillArea = Rect("Fill Area", rect, Vector2.zero, new Vector2(422, 6));
        var fill = Box("Fill", fillArea, Vector2.zero, new Vector2(422, 6), Gold); slider.fillRect = fill.rectTransform;
        fill.rectTransform.sizeDelta = Vector2.zero;
        var handleArea = Rect("Handle Area", rect, Vector2.zero, new Vector2(422, 34));
        var handle = Box("Handle", handleArea, Vector2.zero, new Vector2(18, 30), Color.white);
        handle.rectTransform.sizeDelta = new Vector2(18, 0);
        slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
        var amount = Label(label + " Value", parent, "", new Vector2(355, y), new Vector2(85, 38), 22, Gold, TextAnchor.MiddleRight);
        slider.SetValueWithoutNotify(value);
        amount.text = percentage ? Mathf.RoundToInt(value * 100) + "%" : Mathf.RoundToInt(value).ToString();
        slider.onValueChanged.AddListener(v => { amount.text = percentage ? Mathf.RoundToInt(v * 100) + "%" : Mathf.RoundToInt(v).ToString(); change(v); });
        if (firstControl == null) firstControl = slider;
    }

    void Choice(Transform parent, string label, float y, string value, Action action, out Text text)
    {
        Label(label, parent, label, new Vector2(-305, y), new Vector2(230, 38), 23, Color.white);
        Button(label + " Choice", parent, value, new Vector2(100, y), new Vector2(550, 40), action, out text);
    }

    void Build()
    {
        popup = new GameObject("Settings Popup", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        popup.transform.SetParent(transform, false);
        var canvas = popup.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = popup.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var shade = Box("Settings Backdrop", popup.transform, Vector2.zero, new Vector2(10000, 10000), new Color(0, 0, 0, .72f));
        var panel = Box("Settings Panel", popup.transform, Vector2.zero, new Vector2(960, 820), Ink).rectTransform;
        Label("Settings Heading", panel, "SETTINGS", new Vector2(-305, 355), new Vector2(230, 60), 38, Gold);
        Label("Settings Hint", panel, "OPTIONS / MENU / ESC  ·  CLOSE", new Vector2(155, 355), new Vector2(440, 50), 17, Gold, TextAnchor.MiddleRight);
        Label("Audio Section", panel, "AUDIO", new Vector2(-300, 300), new Vector2(240, 34), 20, Gold);
        SliderRow(panel, "BGM", 250, 0, 1, GameSettings.Bgm, v => GameSettings.SetFloat("Bgm", v), true);
        SliderRow(panel, "SOUND EFFECTS", 200, 0, 1, GameSettings.Effects, v => GameSettings.SetFloat("Effects", v), true);
        SliderRow(panel, "VOICE", 150, 0, 1, GameSettings.Voice, v => GameSettings.SetFloat("Voice", v), true);
        Label("Graphics Section", panel, "GRAPHICS", new Vector2(-300, 100), new Vector2(240, 34), 20, Gold);
        Choice(panel, "RESOLUTION", 55, ResolutionLabel(), () => { resolutionIndex = (resolutionIndex + 1) % resolutions.Count; resolutionText.text = ResolutionLabel(); ApplyDisplay(); }, out resolutionText);
        Choice(panel, "DISPLAY MODE", 5, fullscreen ? "FULLSCREEN  >" : "WINDOWED  >", () => { fullscreen = !fullscreen; modeText.text = fullscreen ? "FULLSCREEN  >" : "WINDOWED  >"; ApplyDisplay(); }, out modeText);
        Choice(panel, "QUALITY", -45, QualitySettings.names[QualitySettings.GetQualityLevel()] + "  >", () => { int quality = (QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length; GameSettings.SetInt("Quality", quality); QualitySettings.SetQualityLevel(quality, true); qualityText.text = QualitySettings.names[quality] + "  >"; }, out qualityText);
        Choice(panel, "CAMERA", -95, CameraLabel(), () => { bool firstPerson=!(follow!=null?follow.firstPerson:GameSettings.FirstPerson(true)); GameSettings.SetInt("FirstPerson",firstPerson?1:0); if(follow!=null)follow.SetFirstPerson(firstPerson); viewText.text=CameraLabel(); }, out viewText);
        Label("Controls Section", panel, "CONTROLS", new Vector2(-300, -145), new Vector2(240, 34), 20, Gold);
        SliderRow(panel, "LOOK SENSITIVITY", -190, 40, 300, GameSettings.Sensitivity, v => { GameSettings.SetFloat("Sensitivity", v); if (follow != null) follow.sensitivity = v; }, false);
        Choice(panel, "VIBRATION", -240, Rumble.Enabled ? "ON  >" : "OFF  >", () => { Rumble.Enabled = !Rumble.Enabled; GameSettings.SetInt("Vibration", Rumble.Enabled ? 1 : 0); vibrationText.text = Rumble.Enabled ? "ON  >" : "OFF  >"; }, out vibrationText);
        Choice(panel, "LANGUAGE", -285, UiLanguage.Pick("日本語  >", "English  >"), () => { UiLanguage.SetEnglish(!UiLanguage.English); languageText.text=UiLanguage.Pick("日本語  >", "English  >"); }, out languageText);
        Text unused;
        Button("Resume", panel, "CLOSE / RESUME", new Vector2(-225, -335), new Vector2(370, 55), Close, out unused);
        var restart=Button("Restart Battle", panel, "RESTART BATTLE", new Vector2(225, -335), new Vector2(370, 55), () => { Close(); if (GameManager.Instance != null) GameManager.Instance.RestartBattle(); }, out unused);
        restart.gameObject.SetActive(GameManager.Instance!=null);
        if(GameManager.Instance==null)panel.Find("Resume").GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-335);
        popup.SetActive(false);
    }

    string ResolutionLabel() => resolutions[resolutionIndex].x + " x " + resolutions[resolutionIndex].y + "  >";
    string CameraLabel() => (follow!=null?follow.firstPerson:GameSettings.FirstPerson(true))?"FIRST PERSON  >":"THIRD PERSON  >";
    void ApplyDisplay()
    {
        var size = resolutions[resolutionIndex];
        GameSettings.SetInt("Width", size.x); GameSettings.SetInt("Height", size.y); GameSettings.SetInt("Fullscreen", fullscreen ? 1 : 0);
        Screen.SetResolution(size.x, size.y, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    void Update()
    {
        var pad = Gamepad.current;
        bool toggle = (pad != null && pad.startButton.wasPressedThisFrame) || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);
        var flow=GetComponent<SceneFlowUI>();
        if(!opened && flow!=null && flow.help!=null && flow.help.gameObject.activeSelf &&
            ((Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)||(pad!=null&&pad.buttonEast.wasPressedThisFrame)))
        { flow.ToggleHelp();return; }
        if (toggle) { if (opened) Close(); else Open(); }
        else if (opened && pad != null && pad.buttonEast.wasPressedThisFrame) Close();
    }

    public void Open()
    {
        if (opened || popup == null) return;
        previousTimeScale = Time.timeScale; previousAudioPause = AudioListener.pause;
        opened = true; Time.timeScale = 0; AudioListener.pause = true; popup.SetActive(true);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        if (EventSystem.current != null)
        {
            previousNavigation = EventSystem.current.sendNavigationEvents; previousSelection = EventSystem.current.currentSelectedGameObject;
            EventSystem.current.sendNavigationEvents = true; firstControl.Select();
        }
    }

    public void Close()
    {
        if (!opened) return;
        opened = false; closedFrame = Time.frameCount;
        Time.timeScale = previousTimeScale; AudioListener.pause = previousAudioPause; popup.SetActive(false);
        if (EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = previousNavigation;
            EventSystem.current.SetSelectedGameObject(previousSelection);
        }
        PlayerPrefs.Save();
    }

    void OnDisable() { Close(); }
    void OnDestroy() { if (instance == this) instance = null; }
}
