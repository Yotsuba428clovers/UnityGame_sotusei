using UnityEngine;

public sealed class CoinDrop : MonoBehaviour
{
    const float HopDuration = 0.40f;
    const float HopHeight = 1.05f;
    const float CoinSize = 1.00f;
    const float RestHeight = 0.40f;
    const float AbsorbHeight = 1.05f;

    static GameObject model;
    static Material coinMaterial;
    PlayerCombat player;
    float age;
    float absorbDuration;
    Vector3 origin;

    public static void Spawn(Vector3 position, PlayerCombat player)
    {
        if (model == null)
            model = Resources.Load<GameObject>("Drops/dropCoin");
        if (coinMaterial == null)
            coinMaterial = Resources.Load<Material>("Drops/DropCoin_Toon");
        if (model == null || coinMaterial == null)
            return;

        var root = new GameObject("DropCoin");
        var coin = root.AddComponent<CoinDrop>();
        coin.player = player;
        coin.origin = position + Vector3.up * RestHeight;
        coin.CreateVisual();
        root.transform.SetPositionAndRotation(coin.origin,
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        root.transform.localScale = Vector3.zero;
    }

    void CreateVisual()
    {
        var pivot = new GameObject("Visual").transform;
        pivot.SetParent(transform, false);
        var visual = Instantiate(model, pivot, false);
        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                materials[i] = coinMaterial;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Turn a flat import upright so the face is visible as the coin spins.
        Vector3 size = bounds.size;
        if (size.y < Mathf.Min(size.x, size.z) * 0.5f)
            pivot.localRotation = Quaternion.Euler(90f, 0f, 0f);
        else if (size.x < Mathf.Min(size.y, size.z) * 0.5f)
            pivot.localRotation = Quaternion.Euler(0f, 90f, 0f);

        float scale = CoinSize / Mathf.Max(Mathf.Max(size.x, size.y, size.z), 0.001f);
        pivot.localScale = Vector3.one * scale;
        pivot.localPosition = -(pivot.localRotation * bounds.center) * scale;
    }

    void LateUpdate()
    {
        age += Time.deltaTime;
        if (age < HopDuration)
        {
            float progress = age / HopDuration;
            float height = 4f * HopHeight * progress * (1f - progress);
            transform.position = origin + Vector3.up * height;
            transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, age / 0.08f);
            transform.Rotate(Vector3.up, 720f * Time.deltaTime, Space.World);
            return;
        }

        if (player == null || player.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 target = player.transform.position + Vector3.up * AbsorbHeight;
        if (absorbDuration == 0f)
            absorbDuration = Mathf.Clamp(Vector3.Distance(origin, target) / 22f, 0.22f, 0.38f);

        float flight = Mathf.Clamp01((age - HopDuration) / absorbDuration);
        transform.position = Vector3.Lerp(origin, target, flight * flight)
            + Vector3.up * (Mathf.Sin(flight * Mathf.PI) * 0.28f);
        transform.Rotate(Vector3.up, 1080f * Time.deltaTime, Space.World);
        transform.localScale = Vector3.one * (1f - Mathf.SmoothStep(0f, 1f, (flight - 0.5f) * 2f));
        if (flight >= 1f)
            Destroy(gameObject);
    }
}
