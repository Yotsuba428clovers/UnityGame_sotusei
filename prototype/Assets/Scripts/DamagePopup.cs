using UnityEngine;

// ダメージ数字(弱点は黄色で大きく表示)
public class DamagePopup : MonoBehaviour
{
    // 変更後
    public static Font CustomFont;   // 指定があればこのフォントを使う
    static Font osFont;
    TextMesh tm;
    float t;
    const float Life = 0.8f;

    public static void Spawn(Vector3 pos, float damage, Color color, bool weak)
    {
        Font font = CustomFont;
        if (font == null)
        {
            if (osFont == null) osFont = Font.CreateDynamicFontFromOSFont("Arial", 32);
            font = osFont;
        }

        var go = new GameObject("DamagePopup");
        go.transform.position = pos + new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f));

        var tm = go.AddComponent<TextMesh>();
        tm.font = font;
        tm.text = Mathf.RoundToInt(damage).ToString() + (weak ? "!" : "");
        tm.fontSize = weak ? 64 : 44;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = color;
        go.GetComponent<MeshRenderer>().material = font.material;

        var p = go.AddComponent<DamagePopup>();
        p.tm = tm;
    }

    void LateUpdate()
    {
        t += Time.unscaledDeltaTime;
        transform.position += Vector3.up * 1.5f * Time.unscaledDeltaTime;
        if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;

        Color c = tm.color;
        c.a = Mathf.Clamp01(1f - (t / Life - 0.5f) * 2f);
        tm.color = c;
        if (t >= Life) Destroy(gameObject);
    }
}
