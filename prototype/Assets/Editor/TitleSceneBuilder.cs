using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class TitleSceneBuilder
{
    static readonly Color Navy = new Color(.025f, .045f, .09f);
    static readonly Color Ink = new Color(.055f, .085f, .13f);
    static readonly Color Mint = new Color(.55f, .95f, .79f);
    static readonly Color Ivory = new Color(.94f, .93f, .86f);
    static readonly Color Muted = new Color(.48f, .60f, .65f);
    static Font font;

    static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false); r.sizeDelta = size; r.anchoredPosition = position;
        return r;
    }
    static Image Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var r = Rect(name, parent, position, size);
        var i = r.gameObject.AddComponent<Image>(); i.color = color; i.raycastTarget = false;
        return i;
    }
    static Text Label(string name, Transform parent, string text, Vector2 pos, Vector2 size, int fontSize, Color color, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var r = Rect(name, parent, pos, size);
        var t = r.gameObject.AddComponent<Text>();
        t.font = font; t.text = text; t.fontSize = fontSize; t.color = color;
        t.alignment = align; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }
    static Button Button(string name, Transform parent, string label, Vector2 pos, Vector2 size, bool primary, UnityAction action)
    {
        var i = Panel(name, parent, pos, size, primary ? Mint : Ink); i.raycastTarget = true;
        var button = i.gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None;
        var motion = i.gameObject.AddComponent<MenuButtonMotion>();
        motion.normal = i.color; motion.highlighted = primary ? new Color(.72f, 1, .9f) : new Color(.12f, .20f, .25f);
        Label("Label", i.transform, label, Vector2.zero, size - new Vector2(32, 0), primary ? 22 : 18, primary ? Navy : Ivory);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }
    static CanvasGroup Group(string name, Transform parent)
    {
        return Rect(name, parent, Vector2.zero, new Vector2(1600, 900)).gameObject.AddComponent<CanvasGroup>();
    }
    static SceneFlowUI Canvas(bool title)
    {
        var go = new GameObject(title ? "Title UI" : "Battle Navigation", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        go.GetComponent<Canvas>().sortingOrder = title ? 0 : 50;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var flow = go.AddComponent<SceneFlowUI>(); flow.titleScreen = title;
        flow.content = Group("Content", go.transform);
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        return flow;
    }
    static void Curtain(SceneFlowUI flow)
    {
        var i = Panel("Transition Fade", flow.transform, Vector2.zero, new Vector2(1600, 900), Navy);
        i.rectTransform.anchorMin = Vector2.zero; i.rectTransform.anchorMax = Vector2.one; i.rectTransform.sizeDelta = Vector2.zero;
        i.raycastTarget = true;
        flow.curtain = i.gameObject.AddComponent<CanvasGroup>(); flow.curtain.alpha = 0; flow.curtain.blocksRaycasts = false;
    }

    public static string Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        // Retain the previous editor scene (including unsaved Cube) in memory.
        var title = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(title);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camera.tag = "MainCamera";
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camera.GetComponent<Camera>().backgroundColor = Navy;
        var flow = Canvas(true); var p = flow.content.transform;
        var bg = Panel("Background", p, Vector2.zero, new Vector2(1600, 900), Navy);
        bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one; bg.rectTransform.sizeDelta = Vector2.zero;
        Panel("Right Field", p, new Vector2(440, 0), new Vector2(720, 900), Ink);
        for (int x = 100; x < 790; x += 80)
            Panel("Grid Vertical", p, new Vector2(x, 0), new Vector2(1, 900), new Color(.13f, .20f, .23f, .35f));
        for (int y = -400; y <= 400; y += 80)
            Panel("Grid Horizontal", p, new Vector2(440, y), new Vector2(720, 1), new Color(.13f, .20f, .23f, .35f));
        Label("Brand", p, "E / A     ELEMENTAL ARCHIVE", new Vector2(-350, 372), new Vector2(700, 36), 16, Mint);
        Label("Edition", p, "ACTION RPG  /  PROTOTYPE 01", new Vector2(450, 372), new Vector2(520, 36), 14, Muted, TextAnchor.MiddleRight);
        Panel("Top Rule", p, new Vector2(0, 330), new Vector2(1410, 1), new Color(.3f, .45f, .47f, .5f));
        Label("Chapter", p, "FOUR ELEMENTS. ONE BLADE.", new Vector2(-350, 214), new Vector2(700, 36), 17, Mint);
        Label("Title", p, "ELEMENT\nSHIFT", new Vector2(-350, 77), new Vector2(700, 240), 98, Ivory);
        Label("Description", p, "Change your element. Find your rhythm.\nEnter the arena and make every strike count.", new Vector2(-350, -94), new Vector2(700, 70), 20, Muted);
        flow.primaryButton = Button("Start Battle", p, "ENTER THE ARENA                                      →", new Vector2(-414, -216), new Vector2(570, 72), true, flow.StartBattle);
        Button("How To Play", p, "HOW TO PLAY                                              +", new Vector2(-414, -303), new Vector2(570, 60), false, flow.ToggleHelp);
        var emblem = Rect("Orbiting Element Emblem", p, new Vector2(420, 24), new Vector2(460, 460)); flow.emblem = emblem;
        for (int ring = 0; ring < 3; ring++)
        {
            float side = 330 + ring * 64;
            var frame = Rect("Diamond " + ring, emblem, Vector2.zero, new Vector2(side, side)); frame.localEulerAngles = new Vector3(0, 0, 45);
            Color c = ring == 0 ? Mint : new Color(.2f, .37f, .38f, .65f);
            Panel("Top", frame, new Vector2(0, side/2), new Vector2(side, 2), c);
            Panel("Bottom", frame, new Vector2(0, -side/2), new Vector2(side, 2), c);
            Panel("Left", frame, new Vector2(-side/2, 0), new Vector2(2, side), c);
            Panel("Right", frame, new Vector2(side/2, 0), new Vector2(2, side), c);
        }
        var blade = Panel("Blade", p, new Vector2(420, 30), new Vector2(18, 250), Ivory); blade.rectTransform.localEulerAngles = new Vector3(0,0,-25);
        var guard = Panel("Crossguard", p, new Vector2(370, -76), new Vector2(102, 12), Mint); guard.rectTransform.localEulerAngles = new Vector3(0,0,-25);
        Label("Emblem Caption", p, "THE ELEMENTAL CYCLE", new Vector2(420,-281), new Vector2(500,40), 17, Ivory, TextAnchor.MiddleCenter);
        Color[] colors = { new Color(1,.49f,.33f), new Color(.48f,.8f,1), new Color(.86f,.7f,1), Mint };
        string[] names = { "01  FIRE", "02  ICE", "03  THUNDER", "04  WIND" };
        for (int n=0; n<4; n++)
        {
            float x = 225 + n*133;
            Panel(names[n]+" Dot", p, new Vector2(x,-337), new Vector2(5,5), colors[n]);
            Label(names[n], p, names[n], new Vector2(x+37,-337), new Vector2(120,30), 11, colors[n], TextAnchor.MiddleCenter);
        }
        Panel("Footer Rule", p, new Vector2(0,-386), new Vector2(1410,1), new Color(.3f,.45f,.47f,.5f));
        flow.status = Label("Status", p, "READY WHEN YOU ARE", new Vector2(-350,-418), new Vector2(700,30), 12, Muted);
        Label("Navigation Hint", p, "MOUSE / KEYBOARD / CONTROLLER", new Vector2(440,-418), new Vector2(540,30), 12, Muted, TextAnchor.MiddleRight);
        flow.help = Group("How To Play Overlay", flow.transform);
        var shade = Panel("Backdrop", flow.help.transform, Vector2.zero, new Vector2(1600,900), new Color(0,0,0,.9f)); shade.raycastTarget = true;
        Panel("Help Card", flow.help.transform, Vector2.zero, new Vector2(860,590), Ink);
        Label("Heading", flow.help.transform, "MASTER THE ELEMENTS", new Vector2(0,205), new Vector2(720,80), 34, Ivory);
        Label("Controls", flow.help.transform, UiLanguage.EnglishControls, new Vector2(0,-5), new Vector2(720,285), 21, Muted);
        Button("Close Help", flow.help.transform, "BACK TO TITLE                                          →", new Vector2(0,-205), new Vector2(720,64), true, flow.ToggleHelp);
        flow.help.gameObject.SetActive(false);
        Curtain(flow);
        EditorSceneManager.SaveScene(title, "Assets/Scenes/TitleScene.unity");
        var battle = EditorSceneManager.OpenScene("Assets/Scenes/battleScene.unity", OpenSceneMode.Additive);
        SceneManager.SetActiveScene(battle);
        bool hasSetup = false;
        foreach (var root in battle.GetRootGameObjects())
            if (root.GetComponentInChildren<QuickSetup>(true) != null) hasSetup = true;
        if (!hasSetup)
        {
            var setup = new GameObject("Battle Setup").AddComponent<QuickSetup>();
            setup.hudFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/fonts/ReggaeOne-Regular.ttf");
        }
        var navigation = Canvas(false);
        navigation.primaryButton = Button("Return To Title", navigation.content.transform, "← TITLE", new Vector2(661,348), new Vector2(180,48), false, navigation.ReturnToTitle);
        Curtain(navigation);
        EditorSceneManager.SaveScene(battle);
        EditorSceneManager.CloseScene(battle, true);
        SceneManager.SetActiveScene(title);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/TitleScene.unity", true), new EditorBuildSettingsScene("Assets/Scenes/battleScene.unity", true) };
        Selection.activeGameObject = flow.gameObject;
        return "Created TitleScene, enabled battle setup and transitions, registered both build scenes.";
    }
}
