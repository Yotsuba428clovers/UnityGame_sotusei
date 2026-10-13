using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// ゲーム進行(撃破数・コンボ・クリア/ゲームオーバー・リスタート)
public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Cleared, GameOver }

    public static GameManager Instance { get; private set; }

    public int killsToBoss = 50;
    public float victoryDuration = 4.3f;
    public float bossDefeatPause = .8f;
    public float resultSlideDuration = .85f;
    public bool ResultVisible { get; private set; }
    public float ClearTime { get; private set; }
    float battleStarted;
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
        battleStarted = Time.time;
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
        Kills++;
        if (e.isBoss && State == GameState.Playing)
        {
            State = GameState.Cleared;
            ClearTime = runSeconds;
            run.Finish(true);
            BannerText = "";
            foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                if (enemy == e) continue;
                enemy.gameObject.SetActive(false);
                Destroy(enemy.gameObject);
            }
            foreach (var spawner in FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) spawner.enabled = false;
            StartCoroutine(VictoryRoutine(e));
            Rumble.Play(0.8f, 0.8f, 0.7f);
        }
    }

    IEnumerator VictoryRoutine(Enemy boss)
    {
        var player = FindFirstObjectByType<PlayerCombat>();
        if (player == null) { ResultVisible = true; yield break; }
        player.EndCombat();
        var camera = Camera.main;
        var follow = camera != null ? camera.GetComponent<CameraFollow>() : null;
        if (follow != null) follow.HoldBossDefeat();
        while (boss != null) yield return null;
        yield return WaitPresentation(bossDefeatPause);
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
        foreach (var spawner in FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) spawner.enabled = false;
        ShowBanner("GAME OVER", 999f);
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

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (SettingsMenu.BlocksGameplay) return;
        if(State==GameState.Playing && !RunManager.RewardOpen)runSeconds+=Time.deltaTime;
        if (State == GameState.Playing) return;
        if (State == GameState.Cleared && !ResultVisible) return;
        bool key = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        if (key) RestartBattle();
    }
}
