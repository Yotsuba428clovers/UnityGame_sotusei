using UnityEngine;
using UnityEngine.UI;

// Retains the authored text, so language changes never translate an already translated string.
[RequireComponent(typeof(Text))]
public sealed class LocalizedLabel : MonoBehaviour
{
    Text label;
    Font originalFont;
    string source, rendered, cachedSource, cachedResult;
    bool cachedEnglish;
    public static void Bind(Text text)
    {
        if(text.GetComponent<LocalizedLabel>()==null)text.gameObject.AddComponent<LocalizedLabel>();
    }
    public static void BindTree(Transform root)
    {
        foreach(var text in root.GetComponentsInChildren<Text>(true))Bind(text);
    }
    void Awake()
    {
        label=GetComponent<Text>();originalFont=label.font;source=label.text;
        label.resizeTextForBestFit=true;label.resizeTextMinSize=Mathf.Min(14,label.fontSize);label.resizeTextMaxSize=label.fontSize;
        Refresh();
    }
    void OnEnable() { UiLanguage.Changed+=Refresh; Refresh(); }
    void OnDisable() { UiLanguage.Changed-=Refresh; }
    public void SetSource(string value) { source=value;cachedSource=null;Refresh(); }
    void LateUpdate()
    {
        if(label.text!=rendered){source=label.text;Refresh();}
    }
    public void Refresh()
    {
        if(label==null)return;
        // Some labels receive their value just before their panel is hidden.
        // Capture that write on reactivation or a language change as well as in LateUpdate.
        if(label.text!=rendered)source=label.text;
        if(cachedSource!=source || cachedEnglish!=UiLanguage.English)
        {
            cachedSource=source;cachedEnglish=UiLanguage.English;cachedResult=UiLanguage.Translate(source);
        }
        rendered=cachedResult;label.text=rendered;
        label.font=UiLanguage.English?originalFont:UiLanguage.JapaneseFont;
    }
}
