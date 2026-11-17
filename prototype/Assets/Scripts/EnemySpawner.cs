using UnityEngine;

// Spawns the next group after the current one is defeated.
public class EnemySpawner : MonoBehaviour
{
    public const int WaveCount = 4;
    public const int EnemiesPerWave = GameManager.KillGoal / WaveCount;
    public Transform player;
    public GameObject enemyPrefab;
    public GameObject bossPrefab;
    public int Wave { get; private set; }

    const int Columns = 5;
    Vector3 stageOrigin;
    Quaternion stageRotation;
    float ordinaryHeight;
    float nextWaveAt = -1f;
    bool bossReleased;

    void Start()
    {
        if (player == null) return;
        stageOrigin = player.position;
        stageRotation = Quaternion.Euler(0f, player.eulerAngles.y, 0f);
        ordinaryHeight = BodyHeight(player, 1.65f);
        SpawnWave();
    }

    void Update()
    {
        var game = GameManager.Instance;
        if (player == null || game == null || game.State != GameManager.GameState.Playing ||
            bossReleased || SettingsMenu.BlocksGameplay || game.Kills < Wave * EnemiesPerWave) return;

        if (nextWaveAt < 0f) nextWaveAt = Time.time + .55f;
        if (Time.time < nextWaveAt) return;
        nextWaveAt = -1f;
        if (Wave < WaveCount) SpawnWave();
        else SpawnBoss();
    }

    void SpawnWave()
    {
        Wave++;
        GameManager.Instance?.ShowBanner($"WAVE {Wave} / {WaveCount}", 1.1f);
        for (int index = 0; index < EnemiesPerWave; index++)
        {
            int row = index / Columns;
            float lane = index % Columns - (Columns - 1) * .5f;
            float lateral = Wave == 2 ? -3.5f : Wave == 3 ? 3.5f : 0f;
            var offset = new Vector3(lateral + lane * 1.5f + (row % 2) * .55f, 0f, 6f + row * 1.45f);
            Spawn(enemyPrefab, offset, false);
        }
    }

    void SpawnBoss()
    {
        bossReleased = true;
        Spawn(bossPrefab, new Vector3(0f, 0f, 10f), true);
        GameManager.Instance?.ShowBanner("FINAL BOSS", 1.65f);
        CameraFollow.Shake(.2f, .4f);
    }

    void Spawn(GameObject prefab, Vector3 offset, bool boss)
    {
        Vector3 position = stageOrigin + stageRotation * offset;
        if (Physics.Raycast(position + Vector3.up * 2.5f, Vector3.down, out var floor, 5f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
            floor.collider.GetComponentInParent<Enemy>() == null &&
            floor.collider.GetComponentInParent<PlayerController>() == null)
            position.y = floor.point.y;

        Vector3 facing = Vector3.ProjectOnPlane(player.position - position, Vector3.up);
        Quaternion rotation = facing.sqrMagnitude > .01f ? Quaternion.LookRotation(facing) : stageRotation;
        GameObject actor = prefab != null ? Instantiate(prefab, position, rotation) : CreateDefault(position);
        if (!actor.TryGetComponent(out Enemy enemy)) enemy = actor.AddComponent<Enemy>();
        enemy.isBoss = boss;
        if (boss)
        {
            if (prefab == null)
            {
                actor.transform.localScale *= 2.2f;
                enemy.maxHp = 1200f;
                enemy.attackStyle = Enemy.AttackStyle.GroundSlam;
                enemy.attackRange = 4f;
                enemy.attackDamage = 28f;
                enemy.attackWindup = .8f;
            }
            return;
        }

        float height = BodyHeight(actor.transform, 2f);
        actor.transform.localScale *= Mathf.Clamp(ordinaryHeight / height, .35f, 2f);
        enemy.maxHp = 48f;
    }

    static float BodyHeight(Transform actor, float fallback)
    {
        var meshes = actor.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (meshes.Length == 0) return fallback;
        Bounds bounds = meshes[0].bounds;
        foreach (var mesh in meshes) bounds.Encapsulate(mesh.bounds);
        return bounds.size.y > .2f ? bounds.size.y : fallback;
    }

    // 仮モデル(足元が原点のカプセル)
    GameObject CreateDefault(Vector3 pos)
    {
        var root = new GameObject("Enemy");
        root.transform.position = pos;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(body.GetComponent<Collider>());
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = Vector3.up;
        body.GetComponent<Renderer>().material.color = new Color(0.55f, 0.55f, 0.6f);

        // 向きが分かる「鼻」
        var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(nose.GetComponent<Collider>());
        nose.transform.SetParent(root.transform, false);
        nose.transform.localPosition = new Vector3(0f, 1.5f, 0.45f);
        nose.transform.localScale = Vector3.one * 0.3f;
        nose.GetComponent<Renderer>().material.color = Color.black;

        var col = root.AddComponent<CapsuleCollider>();
        col.center = Vector3.up;
        col.height = 2f;
        col.radius = 0.5f;
        return root;
    }
}
