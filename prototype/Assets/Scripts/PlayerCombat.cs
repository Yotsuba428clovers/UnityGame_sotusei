using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 戦闘の中心。通常4連コンボ / 強攻撃 / 必殺技 / 魔剣の属性切り替え
[RequireComponent(typeof(PlayerController), typeof(PlayerInputs))]
public class PlayerCombat : MonoBehaviour
{
    [Header("ステータス")]
    public float maxHp = 500f;
    public float maxGauge = 100f;
    public float gaugePerHit = 3f;

    [Header("通常攻撃(コンボ)")]
    public float lightDamage = 20f;
    public int comboLength = 4;
    public float comboWindow = 0.7f;
    public float lightRange = 3.2f;
    public float lightAngle = 150f;
    public float lightKnockback = 0.5f;   // ノックバック距離(m)

    [Header("強攻撃")]
    public float heavyDamage = 55f;
    public float heavyRange = 4.5f;
    public float heavyAngle = 220f;
    public float heavyKnockback = 3f;
    public float heavyRecovery = 0.7f;

    [Header("必殺技(ゲージMAXでB)")]
    public float specialDamage = 160f;
    public float specialRadius = 11f;
    public float specialKnockback = 8f;

    [Header("属性切り替え")]
    public float switchBonusTime = 2f;          // 切り替え直後のボーナス時間
    public float switchBonusMultiplier = 1.5f;  // その間のダメージ倍率
    public Renderer[] swordRenderers;           // 属性色に光らせる魔剣

    [Header("補助")]
    public float aimAssistRange = 6f;

    [Header("エフェクト(QuickSetupの設定が引き継がれます)")]
    public ElementFx[] elementFx;     // 0=炎 1=氷 2=雷 3=風
    public GameObject enemyDeathFx;
    public float fxScale = 1f;
    public Vector3 fxOffset = new Vector3(0f, 1f, 1.2f);   // 斬撃を出す位置(自分から見て前方)
    public Vector3 fxRotation = new Vector3(0f, 0f, 90f);
    public Vector3 specialRotation = new Vector3(0f, 0f, 90f);   // 必殺技の向きの補正
    public Vector3 switchRotation = new Vector3(0f, 0f, 90f);    // 属性切り替えの向きの補正// 斬撃の向きの補正(オイラー角)

    public Element CurrentElement { get; private set; } = Element.Fire;
    public float Hp { get; private set; }
    public float Gauge { get; private set; }
    public bool IsDead { get; private set; }
    public float SwitchBonusRemaining { get { return Mathf.Max(0f, switchBonusUntil - Time.time); } }

    PlayerController controller;
    PlayerInputs input;
    Animator anim;
    readonly Collider[] buffer = new Collider[256];
    readonly HashSet<Enemy> hitSet = new HashSet<Enemy>();
    int combo, bufferedType;
    float lastAttackTime, nextActionTime, switchBonusUntil, invincibleUntil, bufferedUntil;
    static bool hitStopping;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        input = GetComponent<PlayerInputs>();
        Hp = maxHp;
        hitStopping = false;
    }

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        ApplySwordColor();
    }

    void Update()
    {
        if (IsDead) return;

        if (input.NextElement) SwitchElement(1);
        if (input.PrevElement) SwitchElement(-1);

        // 先行入力(0.25秒)
        if (input.Attack) Buffer(1);
        if (input.Heavy) Buffer(2);
        if (input.Special) Buffer(3);

        if (controller.IsDodging || Time.time < nextActionTime || Time.time > bufferedUntil) return;
        int type = bufferedType;
        bufferedType = 0;
        bufferedUntil = 0f;
        if (type == 1) LightAttack();
        else if (type == 2) HeavyAttack();
        else if (type == 3 && Gauge >= maxGauge) StartCoroutine(SpecialRoutine());
    }

    void Buffer(int t) { bufferedType = t; bufferedUntil = Time.time + 0.25f; }

    // ---------- 属性 ----------
    void SwitchElement(int step)
    {
        CurrentElement = ElementUtil.Next(CurrentElement, step);
        switchBonusUntil = Time.time + switchBonusTime;
        ApplySwordColor();
        PlayOrFlash(FxKind.Switch, false, 2.2f, 0.25f);
        Rumble.Play(0f, 0.25f, 0.06f);
        AnimHelper.Trigger(anim, "Switch");
    }

    public void ApplySwordColor()
    {
        if (swordRenderers == null) return;
        Color c = ElementUtil.GetColor(CurrentElement);
        foreach (var r in swordRenderers)
        {
            if (r == null) continue;
            Material m = r.material;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 2f); }
        }
    }

    // ---------- 攻撃 ----------
    void LightAttack()
    {
        if (Time.time - lastAttackTime > comboWindow) combo = 0;
        combo++;
        bool finisher = combo >= comboLength;
        AimAssist();
        AnimHelper.Trigger(anim, "Attack" + combo);

        float range = finisher ? lightRange * 1.3f : lightRange;
        PlayOrFlash(finisher ? FxKind.Heavy : FxKind.Slash, true, range, 0.1f);
        DoHit(range, lightAngle, lightDamage * (finisher ? 2f : 1f),
              lightKnockback * (finisher ? 4f : 1f), finisher, true);

        lastAttackTime = Time.time;
        nextActionTime = Time.time + (finisher ? 0.55f : 0.25f);
        controller.SlowFor(finisher ? 0.5f : 0.25f, 0.25f);
        if (finisher) combo = 0;
    }

    void HeavyAttack()
    {
        combo = 0;
        AimAssist();
        AnimHelper.Trigger(anim, "Heavy");
        nextActionTime = Time.time + heavyRecovery;
        controller.SlowFor(heavyRecovery, 0.15f);
        StartCoroutine(HeavyRoutine());
    }

    IEnumerator HeavyRoutine()
    {
        yield return new WaitForSeconds(0.18f);
        if (IsDead) yield break;
        PlayOrFlash(FxKind.Heavy, true, heavyRange, 0.15f);
        DoHit(heavyRange, heavyAngle, heavyDamage, heavyKnockback, true, true);
    }

    IEnumerator SpecialRoutine()
    {
        Gauge = 0f;
        nextActionTime = Time.time + 1.1f;
        invincibleUntil = Time.time + 1.1f;
        controller.SlowFor(1f, 0f);
        Rumble.Play(0.3f, 0.1f, 0.35f);
        AnimHelper.Trigger(anim, "Special");
        if (GameManager.Instance != null)
            GameManager.Instance.ShowBanner(ElementUtil.GetLabel(CurrentElement) + " BLADE - SPECIAL!", 1.2f);

        yield return new WaitForSeconds(0.35f);
        if (IsDead) yield break;
        PlayOrFlash(FxKind.Special, false, specialRadius, 0.35f);
        DoHit(specialRadius, 360f, specialDamage, specialKnockback, true, false);
        CameraFollow.Shake(0.5f, 0.4f);
        Rumble.Play(1f, 1f, 0.5f);
    }

    // 範囲内の敵にダメージ。戻り値=ヒット数
    int DoHit(float range, float angle, float damage, float knockback, bool big, bool gainGauge)
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up, range, buffer);
        hitSet.Clear();
        int hits = 0;
        float mult = SwitchBonusRemaining > 0f ? switchBonusMultiplier : 1f;
        GameObject hitPrefab = GetFx(FxKind.Hit);

        for (int i = 0; i < count; i++)
        {
            Enemy e = buffer[i].GetComponentInParent<Enemy>();
            if (e == null || e.IsDead || !hitSet.Add(e)) continue;

            Vector3 to = e.transform.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (angle < 359f && dist > e.Radius + 0.8f && Vector3.Angle(transform.forward, to) > angle * 0.5f) continue;

            Vector3 dir = dist > 0.01f ? to / dist : transform.forward;
            e.TakeHit(damage * mult, CurrentElement, dir * knockback);
            hits++;
            if (hitPrefab != null && hits <= 12)   // 大量ヒット時の負荷対策で上限あり
                Fx.Play(hitPrefab, e.transform.position + Vector3.up * e.transform.localScale.y, Quaternion.identity, fxScale);
        }

        if (hits > 0)
        {
            if (gainGauge) Gauge = Mathf.Min(maxGauge, Gauge + gaugePerHit * hits);
            if (GameManager.Instance != null) GameManager.Instance.AddCombo(hits);
            CameraFollow.Shake(big ? 0.25f : 0.07f, 0.12f);
            Rumble.Play(big ? 0.5f : 0.15f, big ? 0.7f : 0.3f, big ? 0.18f : 0.08f);
            if (!hitStopping) StartCoroutine(HitStop(big ? 0.09f : 0.04f));
        }
        return hits;
    }

    // ヒットストップ(一瞬止めて手応えを出す)
    IEnumerator HitStop(float duration)
    {
        hitStopping = true;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        hitStopping = false;
    }

    // 近くの敵の方へ自動で向く
    void AimAssist()
    {
        Vector3 want = controller.InputDirection();
        if (want.sqrMagnitude < 0.01f) want = transform.forward;
        Enemy best = null;
        float bestScore = float.MaxValue;
        foreach (var e in Enemy.All)
        {
            if (e.IsDead) continue;
            Vector3 to = e.transform.position - transform.position;
            to.y = 0f;
            float d = to.magnitude;
            if (d > aimAssistRange) continue;
            float ang = Vector3.Angle(want, to);
            if (ang > 100f) continue;
            float score = d + ang * 0.05f;
            if (score < bestScore) { bestScore = score; best = e; }
        }
        if (best != null) controller.FaceDirection(best.transform.position - transform.position);
    }

    // ---------- 被ダメージ ----------
    public void TakeDamage(float dmg)
    {
        if (IsDead || Time.time < invincibleUntil) return;
        if (controller.IsDodging)
        {
            Gauge = Mathf.Min(maxGauge, Gauge + 8f);   // ジャスト回避でゲージ回復
            Rumble.Play(0.1f, 0.5f, 0.1f);
            return;
        }
        Hp -= dmg;
        invincibleUntil = Time.time + 0.25f;
        CameraFollow.Shake(0.2f, 0.15f);
        Rumble.Play(0.8f, 0.4f, 0.25f);
        if (Hp <= 0f) Die();
    }

    void Die()
    {
        IsDead = true;
        Hp = 0f;
        controller.Locked = true;
        Rumble.Play(1f, 1f, 0.6f);
        AnimHelper.Trigger(anim, "Die");
        if (GameManager.Instance != null) GameManager.Instance.GameOver();
    }

    // ---------- エフェクト ----------
    enum FxKind { Slash, Heavy, Special, Switch, Hit }

    GameObject GetFx(FxKind kind)
    {
        int i = (int)CurrentElement;
        if (elementFx == null || i >= elementFx.Length || elementFx[i] == null) return null;
        ElementFx f = elementFx[i];
        switch (kind)
        {
            case FxKind.Slash: return f.slash;
            case FxKind.Heavy: return f.heavy != null ? f.heavy : f.slash;
            case FxKind.Special: return f.special != null ? f.special : (f.heavy != null ? f.heavy : f.slash);
            case FxKind.Switch: return f.switchFx;
            default: return f.hit;
        }
    }

    // 登録済みならPrefabのエフェクト、未登録なら仮の半透明の球を出す
    void PlayOrFlash(FxKind kind, bool front, float flashRadius, float flashLife)
    {
        GameObject prefab = GetFx(kind);
        if (prefab == null)
        {
            SpawnFlash(flashRadius, ElementUtil.GetColor(CurrentElement), flashLife);
            return;
        }
        Vector3 pos = transform.position + (front ? transform.TransformVector(fxOffset) : Vector3.up * 0.1f);
        Vector3 euler = fxRotation;                                  // 通常攻撃・強攻撃
        if (kind == FxKind.Special) euler = specialRotation;         // 必殺技
        else if (kind == FxKind.Switch) euler = switchRotation;      // 属性切り替え
        Fx.Play(prefab, pos, transform.rotation * Quaternion.Euler(euler), fxScale);
    }

    // 簡易エフェクト(後で自作VFXに差し替えOK)
    void SpawnFlash(float radius, Color c, float life)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        go.transform.position = transform.position + Vector3.up;
        go.transform.localScale = new Vector3(radius * 2f, 0.3f, radius * 2f);
        var r = go.GetComponent<Renderer>();
        var m = new Material(Shader.Find("Sprites/Default"));
        m.color = new Color(c.r, c.g, c.b, 0.35f);
        r.material = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Destroy(go, life);
    }
}
