using System;
using UnityEngine;

// A dedicated key keeps existing settings untouched. Only records persist.
public static class SaveManager
{
    const string Key = "Prototype.Musou.Stage01.Records.v1";
    [Serializable]
    public sealed class Records
    {
        public int runs, clears, bestKills, bestCombo;
        public float bestClearSeconds;
        public RunResult lastResult;
    }

    public static Records Load()
    {
        try
        {
            return JsonUtility.FromJson<Records>(PlayerPrefs.GetString(Key, "{}")) ?? new Records();
        }
        catch (ArgumentException)
        {
            Debug.LogWarning("Run records could not be read. Starting fresh records.");
            return new Records();
        }
    }

    public static void Record(RunResult result)
    {
        var data = Load();
        data.runs++;
        if (result.cleared)
        {
            data.clears++;
            if (data.bestClearSeconds <= 0 || result.seconds < data.bestClearSeconds)
                data.bestClearSeconds = result.seconds;
        }
        data.bestKills = Mathf.Max(data.bestKills, result.kills);
        data.bestCombo = Mathf.Max(data.bestCombo, result.maxCombo);
        data.lastResult = result;
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
}
