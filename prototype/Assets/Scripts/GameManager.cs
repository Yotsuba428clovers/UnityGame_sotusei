using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// ゲーム進行(撃破数・コンボ・クリア/ゲームオーバー・リスタート)
public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Cleared, GameOver }

    public static GameManager Instance { get; private set; }

    public int killsToBoss = 50;

    public GameState State { get; private set; } = GameState.Playing;
    public int Kills { get; private set; }
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }
    public string BannerText { get; private set; } = "";
    public float BannerRemaining { get { return Mathf.Max(0f, bannerUntil - Time.unscaledTime); } }
    public bool ComboActive { get { return Time.time < comboExpire; } }

    float comboExpire, bannerUntil;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
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
        Kills++;
        if (e.isBoss && State == GameState.Playing)
        {
            State = GameState.Cleared;
            ShowBanner("STAGE CLEAR!", 999f);
            Rumble.Play(0.8f, 0.8f, 0.7f);
        }
    }

    public void GameOver()
    {
        if (State != GameState.Playing) return;
        State = GameState.GameOver;
        ShowBanner("GAME OVER", 999f);
    }

    void Update()
    {
        if (State == GameState.Playing) return;
        bool pad = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
        bool key = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        if (pad || key) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
