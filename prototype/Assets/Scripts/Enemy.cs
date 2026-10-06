using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 雑魚・ボス共通の敵。頭上の球の色 = 弱点属性。
// 弱点属性 x1.5 / 反属性(耐性) x0.6
// 炎:継続ダメージ 氷:鈍足 雷:スタン 風:大きく吹き飛ばし
// 氷の必殺技: 凍結(動けず攻撃不可・ノックバックなし)
public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> All = new List<Enemy>();
    public static Enemy Boss;

    [Header("ステータス")]
    public float maxHp = 60f;
    public float moveSpeed = 3.2f;
    public float bodyRadius = 0.5f;

    [Header("攻撃")]
    public float attackRange = 1.7f;
    public float attackDamage = 8f;
    public float attackCooldown = 1.4f;
    public float attackWindup = 0.4f;

    [Header("属性")]
    public Element weakElement = Element.Fire;
    public float weakMultiplier = 1.5f;
    public float resistMultiplier = 0.6f;
    public bool isBoss;

    [Header("やられ演出")]
    public float deathLaunchSpeed = 14f;   // 吹っ飛ぶ水平速度
    public float deathLaunchUp = 9f;       // 上方向の初速
    public float deathSpin = 720f;         // 回転速度(度/秒)
    public float deathTime = 0.9f;         // 消えるまでの時間

    public float Hp { get; private set; }
    public bool IsDead { get; private set; }
    public float Radius { get { return bodyRadius * transform.localScale.x; } }

    PlayerCombat player;
    readonly List<Material> mats = new List<Material>();
    readonly List<Color> baseCols = new List<Color>();
    Vector3 knockVel;
    float nextAttack, stunUntil, slowUntil, burnUntil, nextBurnTick, flashUntil, frozenUntil;
    bool attacking, warn;
    int attackToken;
    Vector3 lastHitDir;   // 最後に受けた攻撃の方向
    Collider[] cols;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); if (Boss == this) Boss = null; }

    // Colliderが無いPrefabでも攻撃が当たるように、見た目の大きさに合わせて自動で付ける
    void EnsureCollider()
    {
        if (GetComponentInChildren<Collider>() != null) return;   // 既にあれば何もしない

        var rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);

        Vector3 s = transform.lossyScale;
        var col = gameObject.AddComponent<CapsuleCollider>();
        col.center = transform.InverseTransformPoint(b.center);
        col.radius = Mathf.Max(b.size.x, b.size.z) * 0.5f / Mathf.Max(0.0001f, Mathf.Max(s.x, s.z));
        col.height = Mathf.Max(b.size.y / Mathf.Max(0.0001f, s.y), col.radius * 2f);
    }
    void Start()
    {
        Hp = maxHp;
        EnsureCollider();
        if (isBoss) Boss = this;
        player = FindFirstObjectByType<PlayerCombat>();

        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            Material m = r.material;
            if (m.HasProperty("_BaseColor")) { mats.Add(m); baseCols.Add(m.GetColor("_BaseColor")); }
            else if (m.HasProperty("_Color")) { mats.Add(m); baseCols.Add(m.GetColor("_Color")); }
        }

        // 弱点属性マーカー(頭上の球)
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(marker.GetComponent<Collider>());
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        marker.transform.localScale = Vector3.one * 0.35f;
        var mm = new Material(Shader.Find("Sprites/Default"));
        mm.color = ElementUtil.GetColor(weakElement);
        marker.GetComponent<Renderer>().material = mm;
    }

    void Update()
    {
        if (IsDead) return;

        // 炎の継続ダメージ
        if (Time.time < burnUntil && Time.time >= nextBurnTick)
        {
            nextBurnTick = Time.time + 0.5f;
            ApplyDamage(isBoss ? 6f : 4f, Element.Fire, false, false);
            if (IsDead) return;
        }

        UpdateVisual();
        if (player == null || player.IsDead) return;

        Vector3 to = player.transform.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        Vector3 dir = dist > 0.01f ? to / dist : transform.forward;
        bool stunned = Time.time < stunUntil;

        Vector3 move = knockVel + Separation();
        knockVel = Vector3.MoveTowards(knockVel, Vector3.zero, 25f * Time.deltaTime);

        if (!stunned && !attacking)
        {
            float speed = moveSpeed * (Time.time < slowUntil ? 0.45f : 1f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
            if (dist > attackRange * 0.8f) move += dir * speed;
            if (dist <= attackRange && Time.time >= nextAttack) StartCoroutine(AttackRoutine());
        }
        transform.position += move * Time.deltaTime;
    }

    // 敵同士が重なりすぎないように
    Vector3 Separation()
    {
        Vector3 push = Vector3.zero;
        for (int i = 0; i < All.Count; i++)
        {
            Enemy o = All[i];
            if (o == this) continue;
            Vector3 d = transform.position - o.transform.position;
            d.y = 0f;
            float min = Radius + o.Radius;
            float m = d.magnitude;
            if (m < min && m > 0.001f) push += d / m * (min - m);
        }
        return push * 4f;
    }

    IEnumerator AttackRoutine()
    {
        attacking = true;
        int token = attackToken;
        nextAttack = Time.time + attackCooldown + Random.Range(0f, 0.5f);
        warn = true;
        yield return new WaitForSeconds(attackWindup);
        warn = false;
        if (!IsDead && token == attackToken && player != null)
        {
            Vector3 d = player.transform.position - transform.position;
            d.y = 0f;
            if (d.magnitude <= attackRange + 0.6f) player.TakeDamage(attackDamage);
        }
        attacking = false;
    }

    // 凍結: 動けず、攻撃もできない(ボスは凍結時間が短い)
    public void Freeze(float duration)
    {
        if (IsDead) return;
        frozenUntil = Time.time + (isBoss ? duration * 0.3f : duration);
        stunUntil = Mathf.Max(stunUntil, frozenUntil);
        attackToken++;            // 攻撃の溜めをキャンセル
        knockVel = Vector3.zero;
    }

    // プレイヤーの回避中に、当たり判定を無視する
    public void IgnoreCollision(Collider other, bool ignore)
    {
        if (cols == null) cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols)
            if (c != null) Physics.IgnoreCollision(other, c, ignore);
    }

    // 戻り値 = 弱点を突いたか
    public bool TakeHit(float damage, Element element, Vector3 knockback)
    {
        if (IsDead) return false;

        Vector3 flat = knockback;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.0001f) lastHitDir = flat.normalized;

        bool weak = element == weakElement;
        bool resist = element == ElementUtil.Opposite(weakElement);
        float mult = weak ? weakMultiplier : (resist ? resistMultiplier : 1f);
        float bossResist = isBoss ? 0.25f : 1f;

        // knockbackは「飛ぶ距離(m)」として扱い、初速に変換
        float dist = knockback.magnitude * bossResist;
        if (Time.time < frozenUntil) dist = 0f;   // 凍結中は吹っ飛ばない
        flashUntil = Time.time + 0.06f;

        switch (element)
        {
            case Element.Fire:
                burnUntil = Time.time + 3f;
                break;
            case Element.Ice:
                slowUntil = Time.time + 3f;
                break;
            case Element.Lightning:
                stunUntil = Mathf.Max(stunUntil, Time.time + (isBoss ? 0.2f : 0.6f));
                attackToken++;
                break;
            case Element.Wind:
                dist *= 1.8f;
                stunUntil = Mathf.Max(stunUntil, Time.time + (isBoss ? 0.1f : 0.25f));
                attackToken++;
                break;
        }
        if (dist > 0.01f) knockVel = knockback.normalized * Mathf.Sqrt(2f * 25f * dist);

        ApplyDamage(damage * mult, element, weak, true);
        return weak;
    }

    void ApplyDamage(float dmg, Element el, bool weak, bool popup)
    {
        Hp -= dmg;
        if (popup)
            DamagePopup.Spawn(transform.position + Vector3.up * (2.2f * transform.localScale.y), dmg,
                              weak ? Color.yellow : Color.white, weak);
        if (Hp <= 0f) Die();
    }

    void Die()
    {
        if (IsDead) return;
        IsDead = true;
        All.Remove(this);
        if (Boss == this) Boss = null;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        if (player != null)
            Fx.Play(player.enemyDeathFx, transform.position + Vector3.up * transform.localScale.y,
                    Quaternion.identity, player.fxScale * transform.localScale.x);
        if (GameManager.Instance != null) GameManager.Instance.OnEnemyKilled(this);
        StartCoroutine(DeathRoutine());
    }

    // やられ演出: 最後に受けた攻撃の方向へ吹っ飛び、回転しながら縮んで消える
    IEnumerator DeathRoutine()
    {
        Vector3 s = transform.localScale;
        float k = isBoss ? 0.3f : 1f;   // ボスは控えめに
        if (lastHitDir == Vector3.zero) lastHitDir = -transform.forward;

        Vector3 vel = lastHitDir * deathLaunchSpeed * k + Vector3.up * deathLaunchUp * k;
        Vector3 spinAxis = Vector3.Cross(Vector3.up, lastHitDir);   // 吹っ飛ぶ向きに倒れる回転軸
        if (spinAxis.sqrMagnitude < 0.001f) spinAxis = Vector3.right;
        spinAxis.Normalize();

        float groundY = transform.position.y;
        float t = 0f;
        while (t < deathTime)
        {
            float dt = Time.deltaTime;
            t += dt;

            vel.y -= 30f * dt;   // 重力
            transform.position += vel * dt;
            if (transform.position.y < groundY)
            {
                Vector3 p = transform.position;
                p.y = groundY;
                transform.position = p;
                vel.y = 0f;
            }
            transform.Rotate(spinAxis, deathSpin * k * dt, Space.World);

            // 最後の30%で縮んで消える
            float shrink = Mathf.Clamp01((t / deathTime - 0.7f) / 0.3f);
            transform.localScale = s * (1f - shrink);
            yield return null;
        }
        Destroy(gameObject);
    }

    void UpdateVisual()
    {
        for (int i = 0; i < mats.Count; i++)
        {
            Color c = baseCols[i];
            if (Time.time < burnUntil) c = Color.Lerp(c, ElementUtil.GetColor(Element.Fire), 0.5f);
            if (Time.time < slowUntil) c = Color.Lerp(c, ElementUtil.GetColor(Element.Ice), 0.5f);
            if (warn) c = Color.Lerp(c, Color.red, 0.6f);
            if (Time.time < frozenUntil) c = Color.Lerp(c, ElementUtil.GetColor(Element.Ice), 0.85f);
            if (Time.time < flashUntil) c = Color.white;
            if (mats[i].HasProperty("_BaseColor")) mats[i].SetColor("_BaseColor", c);
            if (mats[i].HasProperty("_Color")) mats[i].SetColor("_Color", c);
        }
    }
}
