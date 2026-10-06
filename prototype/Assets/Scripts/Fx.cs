using UnityEngine;

// 属性ごとのエフェクトPrefab(Epic Toon FXなどを割り当てる)
[System.Serializable]
public class ElementFx
{
    public string name;
    [Tooltip("通常攻撃の斬撃")] public GameObject slash;
    [Tooltip("強攻撃・コンボ最後の一撃(空なら斬撃を使用)")] public GameObject heavy;
    [Tooltip("必殺技(足元から発生。空なら強攻撃を使用)")] public GameObject special;
    [Tooltip("属性切り替えの瞬間")] public GameObject switchFx;
    [Tooltip("敵に当たった時")] public GameObject hit;
}

public static class Fx
{
    // Prefabを生成し、パーティクルの長さに合わせて自動で消す
    public static GameObject Play(GameObject prefab, Vector3 pos, Quaternion rot, float scale = 1f)
    {
        if (prefab == null) return null;
        var go = Object.Instantiate(prefab, pos, rot);
        go.transform.localScale *= scale;

        float life = 0f;
        bool any = false;
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
        {
            any = true;
            var m = ps.main;
            life = Mathf.Max(life, m.duration + m.startLifetime.constantMax);
        }
        Object.Destroy(go, any ? Mathf.Clamp(life, 1.5f, 8f) + 0.5f : 3f);
        return go;
    }
}
