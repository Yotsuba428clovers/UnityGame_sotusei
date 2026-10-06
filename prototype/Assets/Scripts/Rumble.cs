using UnityEngine;
using UnityEngine.InputSystem;

// コントローラーの振動。Rumble.Play(低周波, 高周波, 秒数) で呼ぶ。
// 低周波 = 重い「ドン」という振動 / 高周波 = 鋭い「ブルッ」という振動(各0〜1)
public class Rumble : MonoBehaviour
{
    public static bool Enabled = true;     // 振動のON/OFF
    public static float Strength = 1f;     // 全体の強さ(0〜1)

    static Rumble instance;
    float low, high, start, end;
    bool active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }

    public static void Play(float low, float high, float duration)
    {
        if (!Enabled || duration <= 0f || Gamepad.current == null) return;
        if (instance == null)
        {
            var go = new GameObject("Rumble");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Rumble>();
        }
        instance.Add(low * Strength, high * Strength, duration);
    }

    void Add(float l, float h, float d)
    {
        float now = Time.unscaledTime;
        // 再生中は、強い方の値を優先して重ねる
        l = Mathf.Max(l, Level(low, now));
        h = Mathf.Max(h, Level(high, now));
        low = Mathf.Clamp01(l);
        high = Mathf.Clamp01(h);
        end = Mathf.Max(now + d, active ? end : 0f);
        start = now;
        active = true;
    }

    float Level(float v, float now)
    {
        if (!active || now >= end || end <= start) return 0f;
        return v * (end - now) / (end - start);   // だんだん弱くなる
    }

    void Update()
    {
        if (!active) return;
        var pad = Gamepad.current;
        if (pad == null) { active = false; return; }

        float now = Time.unscaledTime;   // ヒットストップ中も止まらないように実時間を使う
        if (now >= end)
        {
            pad.SetMotorSpeeds(0f, 0f);
            active = false;
        }
        else
        {
            float f = (end - now) / (end - start);
            pad.SetMotorSpeeds(low * f, high * f);
        }
    }

    void Stop()
    {
        active = false;
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);
    }

    void OnDisable() { Stop(); }
    void OnApplicationQuit() { Stop(); }
    void OnApplicationFocus(bool focus) { if (!focus) Stop(); }
}
