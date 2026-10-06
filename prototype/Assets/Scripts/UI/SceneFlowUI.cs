using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SceneFlowUI : MonoBehaviour
{
    public bool titleScreen;
    public CanvasGroup content;
    public CanvasGroup curtain;
    public RectTransform emblem;
    public CanvasGroup help;
    public Button primaryButton;
    public Text status;
    bool transitioning;
    Vector2 origin;

    void Start()
    {
        Time.timeScale = 1f;
        origin = content.GetComponent<RectTransform>().anchoredPosition;
        if (primaryButton != null) primaryButton.Select();
        StartCoroutine(Reveal());
    }

    IEnumerator Reveal()
    {
        curtain.blocksRaycasts = true;
        content.alpha = 0;
        for (float t = 0; t < 1.1f; t += Time.unscaledDeltaTime)
        {
            float p = Mathf.Clamp01(t / 1.1f);
            float ease = 1 - Mathf.Pow(1 - p, 3);
            content.alpha = ease;
            content.GetComponent<RectTransform>().anchoredPosition = origin + Vector2.down * (1 - ease) * 28;
            curtain.alpha = 1 - Mathf.Clamp01(t / .55f);
            yield return null;
        }
        content.alpha = 1;
        content.GetComponent<RectTransform>().anchoredPosition = origin;
        curtain.alpha = 0;
        curtain.blocksRaycasts = false;
    }

    void Update()
    {
        if (emblem != null)
        {
            emblem.localEulerAngles = new Vector3(0, 0, -Time.unscaledTime * 3);
            emblem.localScale = Vector3.one * (1 + Mathf.Sin(Time.unscaledTime * .8f) * .015f);
        }
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && help != null)
            ToggleHelp();
    }

    public void StartBattle() { if (!transitioning) StartCoroutine(ChangeScene("battleScene")); }
    public void ReturnToTitle() { if (!transitioning) StartCoroutine(ChangeScene("TitleScene")); }
    public void ToggleHelp()
    {
        if (transitioning || help == null) return;
        bool show = !help.gameObject.activeSelf;
        help.gameObject.SetActive(show);
        content.interactable = !show;
        if (show)
        {
            var close = help.GetComponentInChildren<Button>();
            if (close != null) close.Select();
            StartCoroutine(ShowHelp());
        }
        else if (primaryButton != null) primaryButton.Select();
    }
    IEnumerator ShowHelp()
    {
        help.alpha = 0;
        for (float t = 0; t < .25f && help.gameObject.activeSelf; t += Time.unscaledDeltaTime)
        { help.alpha = Mathf.Clamp01(t / .25f); yield return null; }
        help.alpha = 1;
    }
    IEnumerator ChangeScene(string scene)
    {
        if (!Application.CanStreamedLevelBeLoaded(scene))
        { if (status != null) status.text = "SCENE UNAVAILABLE — CHECK BUILD SETTINGS"; yield break; }
        transitioning = true;
        content.interactable = false;
        curtain.blocksRaycasts = true;
        if (status != null) status.text = "PREPARING THE ARENA…";
        for (float t = 0; t < .5f; t += Time.unscaledDeltaTime)
        { curtain.alpha = Mathf.SmoothStep(0, 1, t / .5f); yield return null; }
        curtain.alpha = 1;
        Time.timeScale = 1;
        var operation = SceneManager.LoadSceneAsync(scene);
        while (!operation.isDone) yield return null;
    }
}
