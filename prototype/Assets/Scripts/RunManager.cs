using UnityEngine;
using UnityEngine.SceneManagement;

// Scene-local: reloading the battle reconstructs all actors and transient effects.
[RequireComponent(typeof(RunResultManager))]
public sealed class RunManager : MonoBehaviour
{
    public enum RunState { Preparing, Playing, Cleared, Failed, Restarting }
    public RunState State { get; private set; }
    public PlayerBuild Build { get; private set; }
    RunResultManager results;
    GameManager game;

    void Start()
    {
        game = GetComponent<GameManager>();
        results = GetComponent<RunResultManager>();
        var player = FindFirstObjectByType<PlayerCombat>();
        if (game == null || player == null)
        {
            Debug.LogError("RunManager requires GameManager and PlayerCombat.", this);
            return;
        }
        Build = player.GetComponent<PlayerBuild>();
        if (Build == null) Build = player.gameObject.AddComponent<PlayerBuild>();
        Build.ResetBuild();
        var upgrades = GetComponent<UpgradeManager>();
        if(upgrades==null)upgrades=gameObject.AddComponent<UpgradeManager>();
        upgrades.Initialize(Build);
        var ui=GetComponent<RewardUI>();
        if(ui==null)ui=gameObject.AddComponent<RewardUI>();
        ui.Initialize(upgrades);
        var spawner = GetComponent<EnemySpawner>();
        if (spawner != null)
        {
            var waves=GetComponent<EnemyWaveManager>();
            if(waves==null)waves=gameObject.AddComponent<EnemyWaveManager>();
            waves.Initialize(spawner,upgrades);
        }
        State = RunState.Playing;
        Debug.Log("Run started: full health, empty gauge, no upgrades.", this);
    }

    public static bool RewardOpen => GameManager.Instance != null &&
        GameManager.Instance.GetComponent<UpgradeManager>() != null &&
        GameManager.Instance.GetComponent<UpgradeManager>().IsChoosing;

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
