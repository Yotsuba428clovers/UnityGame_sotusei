using System;
using UnityEngine;

[Serializable]
public sealed class RunResult
{
    public bool cleared;
    public int kills;
    public int maxCombo;
    public float seconds;
}

public sealed class RunResultManager : MonoBehaviour
{
    public RunResult LastResult { get; private set; }

    public void Record(bool cleared, int kills, int maxCombo, float seconds)
    {
        if (LastResult != null) return;
        LastResult = new RunResult { cleared = cleared, kills = kills,
            maxCombo = maxCombo, seconds = seconds };
        SaveManager.Record(LastResult);
        Debug.Log($"Run ended: clear={cleared}, kills={kills}, maxCombo={maxCombo}, seconds={seconds:F1}", this);
    }
}
