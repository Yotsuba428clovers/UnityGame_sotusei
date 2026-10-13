using UnityEngine;

// Uses the existing prefab/fallback spawning path, replacing its endless Update loop.
public sealed class EnemyWaveManager : MonoBehaviour
{
    [Min(1)] public int normalWaves = 4;
    public int Wave { get; private set; }
    public bool AwaitingReward { get; private set; }
    public bool Active { get; private set; }
    EnemySpawner spawner;
    UpgradeManager upgrades;
    int remaining;
    float nextSpawn;

    public void Initialize(EnemySpawner source, UpgradeManager rewards)
    {
        spawner = source; upgrades = rewards;
        spawner.enabled = false;
        upgrades.Chosen += Advance;
        Active = true; Advance();
    }

    void Advance()
    {
        AwaitingReward = false; Wave++;
        if (Wave > normalWaves)
        {
            spawner.SpawnEnemy(true);
            GameManager.Instance.ShowBanner("FINAL BOSS", 3);
        }
        else
        {
            remaining = Mathf.Max(3, spawner.startingEnemies) + (Wave - 1) * 4;
            nextSpawn = Time.time;
            GameManager.Instance.ShowBanner("WAVE " + Wave, 2);
        }
    }

    void Update()
    {
        if (!Active || AwaitingReward || SettingsMenu.BlocksGameplay || GameManager.Instance.State != GameManager.GameState.Playing) return;
        if (Wave > normalWaves) return;
        if (remaining > 0 && Time.time >= nextSpawn)
        {
            nextSpawn = Time.time + Mathf.Max(.1f, spawner.spawnInterval);
            for (int i = 0; i < Mathf.Max(1, spawner.batchSize) && remaining > 0 && Enemy.All.Count < Mathf.Max(1, spawner.maxAlive); i++)
            { spawner.SpawnEnemy(false); remaining--; }
        }
        if (remaining == 0 && Enemy.All.Count == 0)
        {
            AwaitingReward = true;
            if (!upgrades.Offer()) Advance();
        }
    }

    void OnDestroy() { if (upgrades != null) upgrades.Chosen -= Advance; }
}
