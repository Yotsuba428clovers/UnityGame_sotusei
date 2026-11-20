using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 雑魚・ボス共通。固有属性・弱点・耐性は持たず、攻撃属性による状態異常だけを受ける。
// 炎:吹き飛ばし 氷:鈍足 雷:スタン 風:引き寄せ
// 氷の必殺技: 凍結(動けず攻撃不可・ノックバックなし)
public class Enemy : MonoBehaviour
{
    public enum AttackStyle { Melee, GroundSlam }
    public AttackStyle attackStyle;
    [Min(.5f)] public float slamRadius=3.6f;
    public bool showAttackWarning=true;
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
    public float attackRecovery = 0.4f;   // ダメージ判定の後、動き出すまでの硬直(攻撃モーションの後半の長さ)

    [Header("攻撃エフェクト")]
    [Tooltip("攻撃が振り下ろされる瞬間に生成するPrefab。未設定なら表示しません。")]
    public GameObject attackFx;
    [Tooltip("生成位置の基準。未設定なら敵自身。手や武器のTransformを指定できます。")]
    public Transform attackFxOrigin;
    [Tooltip("基準Transformからのローカル位置オフセット。")]
    public Vector3 attackFxOffset = new Vector3(0f, 1f, 1f);
    [Tooltip("基準Transformからの回転オフセット（度）。")]
    public Vector3 attackFxRotation = Vector3.zero;
    [Min(0.01f)] public float attackFxScale = 1f;

    [Header("ボス")]
    public bool isBoss;

    [Header("やられ演出")]
    public AnimationClip deathAnimation;
    public float deathLaunchSpeed = 14f;   // 吹っ飛ぶ水平速度
    public float deathLaunchUp = 9f;       // 上方向の初速
    public float deathSpin = 720f;         // 回転速度(度/秒)
    public float deathTime = 0.9f;         // 消えるまでの時間

    public float Hp { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsFrozen => Time.time < frozenUntil;
    public bool IsBurning => Time.time < burnUntil;
    public bool IsChilled => Time.time < slowUntil;
    public bool IsShocked => Time.time < shockUntil;
    public bool IsWinded => Time.time < windUntil;
    public bool IsBroken => Time.time < breakUntil;
    public bool HitFlash => Time.time < flashUntil;
    public Element LastAppliedElement { get; private set; }
    public float BreakRemaining { get; private set; } = 100f;
    public void DamageBreak(float amount)
    {
        if(!isBoss||IsDead)return;
        BreakRemaining-=amount;
        if(BreakRemaining<=0)
        {
            BreakRemaining=100;breakUntil=Time.time+3f;stunUntil=Mathf.Max(stunUntil,breakUntil);
            CancelAttack();knockVel=Vector3.zero;
            GameManager.Instance?.ShowBanner("BREAK!",2);
        }
    }
    public float Radius { get { return bodyRadius * transform.localScale.x; } }

    PlayerCombat player;
    Vector3 knockVel;
    static int effectFrame = -1, damagePopups, deathEffects;
    float airTime, airHeight, airOffset;
    float impactPauseUntil;
    float nextAttack, stunUntil, slowUntil, burnUntil, flashUntil, frozenUntil;
    float shockUntil, windUntil, breakUntil;
    Coroutine attack;
    Vector3 lastHitDir;   // 最後に受けた攻撃の方向
    Collider[] cols;
    Animator anim;
    bool hasSpeedParam;
    CombatPulse activeWarning;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); if (Boss == this) Boss = null; CancelAttack(); }
    void ClearWarning(){if(activeWarning!=null)Destroy(activeWarning.gameObject);activeWarning=null;}
    void CancelAttack()
    {
        if (attack != null) StopCoroutine(attack);
        attack = null;
        ClearWarning();
    }

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
        cols = GetComponentsInChildren<Collider>();
        if (isBoss) Boss = this;
        player = FindFirstObjectByType<PlayerCombat>();
        anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.applyRootMotion = false;   // モデルだけが動いて、当たり判定が置いていかれるのを防ぐ
        hasSpeedParam = AnimHelper.Has(anim, "Speed", AnimatorControllerParameterType.Float);

        if(GetComponent<EnemyStatusVisual>()==null)gameObject.AddComponent<EnemyStatusVisual>();
    }

    void Update()
    {
        if (SettingsMenu.BlocksGameplay || (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing)) return;
        if (IsDead) return;

        if (Time.unscaledTime < impactPauseUntil)
        {
            if (anim != null) anim.speed = 0f;
            return;
        }
        if (anim != null) anim.speed = IsFrozen || IsBroken ? 0f : (IsChilled ? 0.55f : 1f);
        if (player == null || player.IsDead)
        {
            if (hasSpeedParam) anim.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            return;
        }

        Vector3 to = player.transform.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        Vector3 dir = dist > 0.01f ? to / dist : transform.forward;
        bool stunned = Time.time < stunUntil || airTime > 0f;

        Vector3 move = IsFrozen ? Vector3.zero : knockVel + Separation();
        knockVel = Vector3.MoveTowards(knockVel, Vector3.zero, 25f * Time.deltaTime);

        float walkSpeed = 0f;
        if (!stunned && attack == null)
        {
            float speed = moveSpeed * (Time.time < slowUntil ? 0.45f : 1f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
            if (dist > attackRange * 0.8f)
            {
                move += dir * speed;
                walkSpeed = speed;
            }
            if (dist <= attackRange && Time.time >= nextAttack &&
                ArenaGeometry.ClearPath(transform.position, player.transform.position)) attack = StartCoroutine(AttackRoutine());
        }
        if (hasSpeedParam) anim.SetFloat("Speed", walkSpeed, 0.1f, Time.deltaTime);
        Vector3 position = transform.position;
        Vector3 step = move * Time.deltaTime;
        if (step.sqrMagnitude > .000001f)
        {
            float distance = step.magnitude;
            Vector3 heading = step / distance;
            position += heading * ArenaGeometry.ClearDistance(position + Vector3.up * (.75f - airOffset),
                heading, distance, Mathf.Min(.45f, Radius));
        }
        if (step.sqrMagnitude > .000001f && ArenaGeometry.SampleMeadowFloor(position, out var meadow))
            position.y = meadow.point.y + airOffset;
        if (airTime > 0f && !IsFrozen)
        {
            airTime = Mathf.Max(0f, airTime - Time.deltaTime);
            float progress = 1f - airTime / .45f;
            position.y -= airOffset;
            airOffset = 4f * airHeight * progress * (1f - progress);
            position.y += airOffset;
        }
        transform.position = position;
    }
    // 敵同士が重なりすぎないように
    Vector3 Separation()
    {
        Vector3 push = Vector3.zero;
        Vector3 position = transform.position;
        float radius = Radius;
        for (int i = 0; i < All.Count; i++)
        {
            Enemy o = All[i];
            if (o == this) continue;
            Vector3 d = position - o.transform.position;
            d.y = 0f;
            float min = radius + o.Radius;
            float squared = d.sqrMagnitude;
            if (squared >= min * min || squared <= .000001f) continue;
            float distance = Mathf.Sqrt(squared);
            push += d * ((min - distance) / distance);
        }
        return push * 4f;
    }

    IEnumerator AttackRoutine()
    {
        Vector3 impactPosition=transform.position;
        nextAttack = Time.time + attackCooldown + Random.Range(0f, 0.5f);
        AnimHelper.Trigger(anim, "Attack");
        CombatPresentation.Sound(2,transform.position);
        if(showAttackWarning)
            activeWarning=CombatPulse.Create(attackStyle==AttackStyle.GroundSlam?impactPosition:impactPosition+transform.forward*attackRange*.5f,
                new Color(1,.32f,.13f),attackStyle==AttackStyle.GroundSlam?slamRadius:attackRange*.65f,attackWindup,true);
        yield return new WaitForSeconds(attackWindup);   // 攻撃モーションの「当たる瞬間」まで
        ClearWarning();
        if (!IsDead && player != null)
        {
            if (attackFx != null)
            {
                Transform origin = attackFxOrigin != null ? attackFxOrigin : transform;
                Fx.Play(attackFx, origin.TransformPoint(attackFxOffset),
                    origin.rotation * Quaternion.Euler(attackFxRotation), Mathf.Max(0.01f, attackFxScale));
            }
            bool slam=attackStyle==AttackStyle.GroundSlam;
            CombatPresentation.Sound(slam?3:0,transform.position);
            CombatPulse.Create(slam?impactPosition:transform.position+transform.forward,new Color(1,.58f,.23f),slam?slamRadius:1.2f,slam?.5f:.22f);
            if(slam && Camera.main!=null && Vector3.Distance(Camera.main.transform.position,impactPosition)<20)
                CameraFollow.Shake(.16f,.22f);
            Vector3 d = player.transform.position - (slam?impactPosition:transform.position);
            d.y = 0f;
            if (d.magnitude <= (slam?slamRadius:attackRange+0.6f) &&
                ArenaGeometry.ClearPath(transform.position, player.transform.position)) player.TakeDamage(attackDamage);
        }
        yield return new WaitForSeconds(attackRecovery);   // モーションの残りが終わるまで動かない
        attack = null;
    }

    // 凍結: 動けず、攻撃もできない(ボスは凍結時間が短い)
    public void Freeze(float duration)
    {
        if (IsDead) return;
        frozenUntil = Time.time + (isBoss ? duration * 0.3f : duration);
        stunUntil = Mathf.Max(stunUntil, frozenUntil);
        CancelAttack();
        knockVel = Vector3.zero;
        windUntil=0;
    }

    public void Interrupt(float duration)
    {
        if (IsDead) return;
        stunUntil = Mathf.Max(stunUntil, Time.time + duration);
        CancelAttack();
    }

    public void PauseOnHit(float until)
    {
        if (!IsDead) impactPauseUntil = Mathf.Max(impactPauseUntil, until);
    }

    public void Launch(float height)
    {
        if (IsDead || IsFrozen || isBoss || airTime > 0f) return;
        airTime = .45f;
        airHeight = height;
        Interrupt(airTime);
    }

    public void PullToward(Vector3 destination, float maxDistance)
    {
        if (IsDead || IsFrozen || isBoss) return;
        Vector3 offset = Vector3.ProjectOnPlane(destination - transform.position, Vector3.up);
        float distance = Mathf.Min(offset.magnitude, maxDistance);
        float speed = Mathf.Sqrt(2f * 25f * distance);
        knockVel = offset.normalized * speed;
        Interrupt(speed / 25f);
    }

    // A large enemy can be in sword range even when its root is several metres away.
    public Vector3 AttackTargetFrom(Vector3 origin)
    {
        if (!isBoss || cols == null) return transform.position;

        Vector3 nearest = transform.position;
        float bestDistance = float.PositiveInfinity;
        foreach (var body in cols)
        {
            if (body == null || !body.enabled || body.isTrigger || !body.gameObject.activeInHierarchy)
                continue;
            Bounds bounds = body.bounds;
            var sample = new Vector3(origin.x, bounds.center.y, origin.z);
            Vector3 surface = body.ClosestPoint(sample);
            surface.y = transform.position.y;
            float distance = (surface - origin).sqrMagnitude;
            if (distance >= bestDistance) continue;
            nearest = surface;
            bestDistance = distance;
        }
        return nearest;
    }

    // プレイヤーの回避中に、当たり判定を無視する
    public void IgnoreCollision(Collider other, bool ignore)
    {
        if (cols == null) cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols)
            if (c != null) Physics.IgnoreCollision(other, c, ignore);
    }

    // 戻り値 = 生存中の敵が命中を受け付けたか。属性による弱点倍率はない。
    public bool TakeHit(float damage, Element? element, Vector3 knockback)
    {
        if (IsDead) return false;

        Vector3 flat = knockback;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.0001f) lastHitDir = flat.normalized;

        float bossResist = isBoss ? 0.25f : 1f;

        // knockbackは「飛ぶ距離(m)」として扱い、初速に変換
        float dist = knockback.magnitude * bossResist;
        if (Time.time < frozenUntil) dist = 0f;   // 凍結中は吹っ飛ばない
        flashUntil = Time.time + 0.06f;
        if (element.HasValue) LastAppliedElement = element.Value;

        switch (element)
        {
            case Element.Fire:
                burnUntil = Time.time + .65f;
                break;
            case Element.Ice:
                slowUntil = Time.time + 3f;
                break;
            case Element.Lightning:
                shockUntil=Time.time+(isBoss?.2f:.6f);
                if (!isBoss) Interrupt(shockUntil - Time.time);
                break;
            case Element.Wind:
                dist *= 1.8f;
                windUntil=Time.time+Mathf.Max(isBoss?.1f:.25f,Mathf.Sqrt(2f*25f*dist)/25f);
                if (!isBoss) Interrupt(.25f);
                break;
        }
        if (dist > 0.01f) knockVel = knockback.normalized * Mathf.Sqrt(2f * 25f * dist);

        ApplyDamage(damage);
        return true;
    }

    static void RefreshEffectBudget()
    {
        if (effectFrame == Time.frameCount) return;
        effectFrame = Time.frameCount;
        damagePopups = deathEffects = 0;
    }

    void ApplyDamage(float dmg)
    {
        Hp -= dmg;
        RefreshEffectBudget();
        if (damagePopups++ < 12) DamagePopup.Spawn(transform.position + Vector3.up * (2.2f * transform.localScale.y), dmg,
                          Color.white, false);
        if (Hp <= 0f) Die();
    }

    void Die()
    {
        if (IsDead) return;
        IsDead = true;
        CancelAttack();
        All.Remove(this);
        if (Boss == this) Boss = null;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        CoinDrop.Spawn(transform.position - Vector3.up * airOffset, player);
        if (player != null && deathEffects++ < 10)
            Fx.Play(player.enemyDeathFx, transform.position + Vector3.up * transform.localScale.y,
                    Quaternion.identity, player.fxScale * transform.localScale.x);
        if (GameManager.Instance != null) GameManager.Instance.OnEnemyKilled(this);
        StartCoroutine(DeathRoutine());
    }

    // やられ演出: 最後に受けた攻撃の方向へ吹っ飛び、回転しながら縮んで消える
    IEnumerator DeathRoutine()
    {
        Vector3 s = transform.localScale;
        if (deathAnimation != null && anim != null && anim.HasState(0, Animator.StringToHash("Base Layer.Die")))
        {
            anim.speed = 1f;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var parameter in anim.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger) anim.ResetTrigger(parameter.nameHash);
            anim.Play("Base Layer.Die", 0, 0f);
            yield return new WaitForSeconds(deathAnimation.length + .25f);
            const float fadeTime = .35f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                transform.localScale = s * (1f - Mathf.Clamp01(elapsed / fadeTime));
                yield return null;
            }
            Destroy(gameObject);
            yield break;
        }
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

}

