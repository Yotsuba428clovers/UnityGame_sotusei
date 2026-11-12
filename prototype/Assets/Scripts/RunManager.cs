using UnityEngine;
using UnityEngine.SceneManagement;

// Scene-local: reloading the battle reconstructs all actors and transient effects.
[RequireComponent(typeof(RunResultManager))]
public sealed class RunManager : MonoBehaviour
{
    public enum RunState { Preparing, Playing, Cleared, Failed, Restarting }
    public RunState State { get; private set; }
    RunResultManager results;
    GameManager game;

    void Awake()
    {
        game = GetComponent<GameManager>();
        results = GetComponent<RunResultManager>();
        State = RunState.Playing;
    }

    public void Finish(bool cleared)
    {
        if (State != RunState.Playing) return;
        State = cleared ? RunState.Cleared : RunState.Failed;
        results.Record(cleared, game.Kills, game.MaxCombo, game.ClearTime);
    }

    public void RestartRun()
    {
        if (State == RunState.Restarting) return;
        var scene = gameObject.scene;
        if (!Application.CanStreamedLevelBeLoaded(scene.path))
        {
            Debug.LogError("Add the battle scene to Build Settings before restarting.", this);
            return;
        }
        State = RunState.Restarting;
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene.path);
    }
}
