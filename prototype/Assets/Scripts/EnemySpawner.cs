using UnityEngine;

// プレイヤーの周囲に敵を湧かせる。撃破数が killsToBoss に達するとボス出現。
// enemyPrefab / bossPrefab が空なら、カプセルの仮モデルを自動生成する。
public class EnemySpawner : MonoBehaviour
{
    public Transform player;
    public GameObject enemyPrefab;
    public GameObject bossPrefab;
    public GameObject[] additionalEnemyPrefabs;

    public int maxAlive = 45;
    public float spawnInterval = 0.35f;
    public int batchSize = 3;
    public int startingEnemies = 15;
    public float minRadius = 14f;
    public float maxRadius = 24f;

    float nextSpawn;
    bool bossSpawned;
    int spawnCount;

    void Start()
    {
        for (int i = 0; i < startingEnemies; i++) Spawn(false);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameManager.GameState.Playing || player == null) return;

        if (!bossSpawned && gm.Kills >= gm.killsToBoss)
        {
            bossSpawned = true;
            Spawn(true);
            gm.ShowBanner("BOSS APPEARS!", 3f);
            return;
        }

        if (Time.time < nextSpawn) return;
        nextSpawn = Time.time + spawnInterval;
        int limit = bossSpawned ? maxAlive / 2 : maxAlive;
        for (int i = 0; i < batchSize; i++)
            if (Enemy.All.Count < limit) Spawn(false);
    }

    void Spawn(bool boss)
    {
        Vector2 c = Random.insideUnitCircle.normalized * (boss ? 16f : Random.Range(minRadius, maxRadius));
        Vector3 pos = new Vector3(player.position.x + c.x, player.position.y, player.position.z + c.y);

        GameObject prefab = boss ? bossPrefab : enemyPrefab;
        if(!boss && additionalEnemyPrefabs!=null && additionalEnemyPrefabs.Length>0 && ++spawnCount%3==0)
        {
            var alternate=additionalEnemyPrefabs[Random.Range(0,additionalEnemyPrefabs.Length)];
            if(alternate!=null)prefab=alternate;
        }
        GameObject go = prefab != null ? Instantiate(prefab, pos, Quaternion.identity) : CreateDefault(pos, boss);

        Enemy e = go.GetComponent<Enemy>();
        bool fresh = e == null;
        if (fresh) e = go.AddComponent<Enemy>();
        e.weakElement = (Element)Random.Range(0, 4);
        e.isBoss = boss;
        if (fresh && boss)
        {
            e.maxHp = 1800f;
            e.moveSpeed = 2.6f;
            e.attackRange = 4f;
            e.attackDamage = 30f;
            e.attackCooldown = 2.5f;
            e.attackWindup = 0.9f;
        }
    }

    // 仮モデル(足元が原点のカプセル)
    GameObject CreateDefault(Vector3 pos, bool boss)
    {
        var root = new GameObject(boss ? "Boss" : "Enemy");
        root.transform.position = pos;
        root.transform.localScale = Vector3.one * (boss ? 2.4f : 1f);

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(body.GetComponent<Collider>());
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = Vector3.up;
        body.GetComponent<Renderer>().material.color = boss ? new Color(0.35f, 0.1f, 0.4f) : new Color(0.55f, 0.55f, 0.6f);

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
