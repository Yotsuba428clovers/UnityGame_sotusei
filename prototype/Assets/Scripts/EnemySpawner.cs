using UnityEngine;

// One stage with three fixed formations: a cluster, a line and a spread.
public class EnemySpawner : MonoBehaviour
{
    public Transform player;
    public GameObject enemyPrefab;

    static readonly Vector2[] Formation =
    {
        new Vector2(-1.5f, 6f), new Vector2(0f, 6f), new Vector2(1.5f, 6f),
        new Vector2(-1.5f, 7.6f), new Vector2(0f, 7.6f), new Vector2(1.5f, 7.6f),
        new Vector2(-0.75f, 9.2f), new Vector2(0.75f, 9.2f),
        new Vector2(-8f, 12f), new Vector2(-5.3f, 12f), new Vector2(-2.6f, 12f),
        new Vector2(0f, 12f), new Vector2(2.6f, 12f), new Vector2(5.3f, 12f), new Vector2(8f, 12f),
        new Vector2(-8f, 4f), new Vector2(-6f, 8f), new Vector2(7f, 5f),
        new Vector2(6f, 9f), new Vector2(0f, 16f)
    };

    void Start()
    {
        if (player == null) return;
        foreach (Vector2 slot in Formation)
        {
            Vector3 offset = new Vector3(slot.x, 0f, slot.y);
            Vector3 position = player.position + player.rotation * offset;
            if (Physics.Raycast(position + Vector3.up * 3f, Vector3.down, out var floor, 6f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                position.y = floor.point.y;

            Quaternion facing = Quaternion.LookRotation(-(player.rotation * offset));
            GameObject actor = enemyPrefab != null ? Instantiate(enemyPrefab, position, facing) : CreateDefault(position);
            if (!actor.TryGetComponent(out Enemy enemy)) enemy = actor.AddComponent<Enemy>();
            enemy.isBoss = false;
        }
        GameManager.Instance?.ShowBanner("DEFEAT 20 ENEMIES", 1.8f);
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
