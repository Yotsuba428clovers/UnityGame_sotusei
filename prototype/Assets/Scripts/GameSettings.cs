using UnityEngine;

public static class GameSettings
{
    const string Prefix = "ElementShift.Settings.";
    public static float Bgm => PlayerPrefs.GetFloat(Prefix + "Bgm", 1f);
    public static float Effects => PlayerPrefs.GetFloat(Prefix + "Effects", 1f);
    public static float Voice => PlayerPrefs.GetFloat(Prefix + "Voice", 1f);
    public static float Sensitivity => PlayerPrefs.GetFloat(Prefix + "Sensitivity", 140f);
    public static bool Vibration => PlayerPrefs.GetInt(Prefix + "Vibration", 1) != 0;
    public static bool FirstPerson(bool fallback) => PlayerPrefs.GetInt(Prefix + "FirstPerson", fallback ? 1 : 0) != 0;
    public static void SetFloat(string key, float value) { PlayerPrefs.SetFloat(Prefix + key, value); }
    public static void SetInt(string key, int value) { PlayerPrefs.SetInt(Prefix + key, value); }
    public static int GetInt(string key, int fallback) => PlayerPrefs.GetInt(Prefix + key, fallback);

    public static void ApplyGraphics()
    {
        if (PlayerPrefs.HasKey(Prefix + "Quality"))
            QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Quality"), 0, QualitySettings.names.Length - 1), true);
        if (PlayerPrefs.HasKey(Prefix + "Width"))
            Screen.SetResolution(PlayerPrefs.GetInt(Prefix + "Width"), PlayerPrefs.GetInt(Prefix + "Height"),
                PlayerPrefs.GetInt(Prefix + "Fullscreen", 1) != 0 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }
}
