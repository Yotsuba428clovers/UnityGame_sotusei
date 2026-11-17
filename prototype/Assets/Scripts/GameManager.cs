using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// ゲーム進行(撃破数・コンボ・クリア/ゲームオーバー・リスタート)
public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Cleared, GameOver }

    public static GameManager Instance { get; private set; }

    public const int KillGoal = 100;
    public float victoryDuration = 1.25f;
    public float resultSlideDuration = .55f;
    public bool ResultVisible { get; private set; }
    public float ClearTime { get; private set; }
    float runSeconds;

    public GameState State { get; private set; } = GameState.Playing;
    public int Kills { get; private set; }
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }
    public string BannerText { get; private set; } = "";
    public float BannerRemaining { get { return Mathf.Max(0f, bannerUntil - Time.unscaledTime); } }
    public bool ComboActive { get { return Time.time < comboExpire; } }

    float comboExpire, bannerUntil;
    RunManager run;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        run = GetComponent<RunManager>();
        if (run == null) run = gameObject.AddComponent<RunManager>();
    }

    public void AddCombo(int n)
    {
        if (!ComboActive) Combo = 0;
        Combo += n;
        MaxCombo = Mathf.Max(MaxCombo, Combo);
        comboExpire = Time.time + 2.5f;
    }

    public void ShowBanner(string text, float seconds)
    {
        BannerText = text;
        bannerUntil = Time.unscaledTime + seconds;
    }

    public void OnEnemyKilled(Enemy e)
    {
        if (State != GameState.Playing) return;
        if (!e.isBoss)
        {
            Kills++;
            return;
        }
        if (Kills >= KillGoal)
        {
            State = GameState.Cleared;
            ClearTime = runSeconds;
            ShowBanner("STAGE CLEAR", 1.05f);
            StartCoroutine(VictoryRoutine(e));
        }
    }

    IEnumerator VictoryRoutine(Enemy boss)
    {
        // Let the last attack finish tallying its hits before recording the result.
        yield return null;
        run.Finish(true);
        var player = FindFirstObjectByType<PlayerCombat>();
        if (player == null) { ResultVisible = true; yield break; }
        player.EndCombat();
        var camera = Camera.main;
        var follow = camera != null ? camera.GetComponent<CameraFollow>() : null;
        while (boss != null) yield return null;
        player.CelebrateVictory();
        if (follow != null) follow.BeginVictory(player.transform);
        yield return WaitPresentation(victoryDuration);
        float elapsed = 0;
        while (elapsed < resultSlideDuration)
        {
            if (SettingsMenu.IsOpen) { yield return null; continue; }
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(.01f, resultSlideDuration)));
            if (follow != null) follow.SetResultFraming(progress);
            yield return null;
        }
        if (follow != null) follow.SetResultFraming(1f);
        ResultVisible = true;
    }

    public void GameOver()
    {
        if (State != GameState.Playing) return;
        State = GameState.GameOver;
        ClearTime = runSeconds;
        run.Finish(false);
        var player = FindFirstObjectByType<PlayerCombat>();
        if (player != null) player.EndCombat();
        ResultVisible = true;
    }

    IEnumerator WaitPresentation(float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            if (!SettingsMenu.IsOpen) elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    public void RestartBattle()
    {
        run.RestartRun();
    }

    public void ReturnToTitle()
    {
        if (SettingsMenu.BlocksGameplay) return;
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (SettingsMenu.BlocksGameplay) return;
        if (State == GameState.Playing) runSeconds += Time.deltaTime;
        if (State == GameState.Playing) return;
        if (State == GameState.Cleared && !ResultVisible) return;
        bool key = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        if (key) RestartBattle();
    }
}
