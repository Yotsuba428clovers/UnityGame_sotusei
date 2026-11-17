using System.Collections.Generic;
using UnityEngine;

// 攻撃の経過フレームが判定・連携・回避の受付を管理する。
[RequireComponent(typeof(PlayerController), typeof(PlayerInputs))]
public class PlayerCombat : MonoBehaviour
{
    enum Move { None, Light1, Light2, Light3, Strong, Branch, Special }
    enum Command { None, Light, Strong, Special }
    enum FxKind { Heavy, Special, Switch, Hit }

    readonly struct Timing
    {
        public readonly float Duration, HitStart, HitEnd, ChainStart, ChainEnd, DodgeStart;
        public Timing(float duration, float hitStart, float hitEnd, float chainStart, float chainEnd, float dodgeStart)
        {
            Duration = duration;
            HitStart = hitStart;
            HitEnd = hitEnd;
            ChainStart = chainStart;
            ChainEnd = chainEnd;
            DodgeStart = dodgeStart;
        }
    }

    static readonly Timing Light1 = new Timing(13, 3, 7, 7, 12, 9);
    static readonly Timing Light2 = new Timing(14, 4, 8, 8, 13, 10);
    static readonly Timing Light3 = new Timing(18, 5, 10, 0, 0, 12);
    static readonly Timing Strong = new Timing(21, 6, 11, 0, 0, 14);
    static readonly Timing Special = new Timing(26, 8f, 9f, 0, 0, 26);

    public float maxHp = 500f;
    public float maxGauge = 100f;
    public float gaugePerHit = 4f;
    public float damageInvincibility = 0.75f;
    public float lightDamage = 25f;
    public float heavyDamage = 65f;
    public float specialDamage = 160f;
    const float FireSpecialRadius = 10f;
    const float IceSpecialRadius = 8f;
    const float WindSpecialRadius = 7f;
    const float LightningSpecialRange = 7f;
    public Renderer[] swordRenderers;
    public ElementFx[] elementFx;
    public GameObject enemyDeathFx;
    public float fxScale = 1f;
    public Vector3 fxOffset = new Vector3(0f, 1f, 1.2f);
    public Vector3 fxRotation = new Vector3(0f, 0f, 90f);
    public Vector3 specialRotation = new Vector3(0f, 0f, 90f);
    public Vector3 switchRotation = new Vector3(0f, 0f, 90f);

    public Element CurrentElement { get; private set; } = Element.Fire;
    public float Hp { get; private set; }
    public float Gauge { get; private set; }
    public bool IsDead { get; private set; }
    public float WindBuffRemaining => Mathf.Max(0f, windBuffUntil - Time.time);
    public float AttackSpeed => WindBuffRemaining > 0f ? 1.25f : 1f;
    public bool IsAttackPaused => Time.unscaledTime < impactPauseUntil;

    PlayerController controller;
    PlayerInputs input;
    Animator anim;
    AttackPose pose;
    Move move;
    Timing timing;
    Element attackElement;
    Command queued;
    float queueUntil, frame, speed, invincibleUntil, windBuffUntil, impactPauseUntil;
    int hits;
    Vector3 attackOrigin, attackForward;
    readonly List<Enemy> candidates = new List<Enemy>(20);
    readonly HashSet<Enemy> hitSet = new HashSet<Enemy>();

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        input = GetComponent<PlayerInputs>();
        Hp = maxHp;
    }

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.applyRootMotion = false;
        pose = new AttackPose(transform, anim, swordRenderers);
        foreach (var source in GetComponentsInChildren<AudioSource>())
            source.gameObject.AddComponent<AudioVolumeChannel>().Configure(source, AudioVolumeChannel.Bus.Voice);
        ApplySwordColor();
    }

    void Update()
    {
        pose?.Restore();
        if (SettingsMenu.BlocksGameplay || !Application.isFocused || IsDead || controller.IsStaggered ||
            (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing))
        {
            queued = Command.None;
            return;
        }

        int elementStep = input.ElementStep;
        if (elementStep != 0)
        {
            CurrentElement = ElementUtil.Next(CurrentElement, elementStep);
            if (move == Move.None) ApplySwordColor();
            PlayEffect(FxKind.Switch, CurrentElement, transform.position, transform.forward);
        }

        if (controller.IsDodging || (input.Dodge && controller.DodgeReady && CanDodge))
            return;

        Command command = input.Heavy ? Command.Strong : input.Attack ? Command.Light :
            input.Special ? Command.Special : Command.None;
        if (move == Move.None)
        {
            if (command == Command.Light) Begin(Move.Light1);
            else if (command == Command.Strong) Begin(Move.Strong);
            else if (command == Command.Special && Gauge >= maxGauge) Begin(Move.Special);
            return;
        }

        float previousFrame = frame;
        if (!IsAttackPaused) frame += Time.deltaTime * 30f * speed;
        bool chainInput = (move == Move.Light1 && command == Command.Light) ||
            (move == Move.Light2 && (command == Command.Light || command == Command.Strong));
        if (Time.time > queueUntil) queued = Command.None;
        if (chainInput && previousFrame < timing.ChainEnd && (queued != Command.Strong || command == Command.Strong))
        {
            queued = command;
            queueUntil = Time.time + .27f;
        }

        if (frame > previousFrame && previousFrame < timing.HitEnd && frame >= timing.HitStart)
        {
            if (previousFrame < timing.HitStart)
                BeginImpact();
            ResolveImpact();
        }

        if (queued != Command.None && frame >= timing.ChainStart && previousFrame < timing.ChainEnd)
        {
            Move next = move == Move.Light1 ? Move.Light2 : queued == Command.Strong ? Move.Branch : Move.Light3;
            Begin(next);
        }
        else if (frame >= timing.Duration)
        {
            FinishAttack();
        }
    }

    void LateUpdate()
    {
        if (controller.IsDodging)
        {
            pose?.Dodge(controller.DodgeProgress);
            return;
        }
        if (move == Move.None || IsDead)
        {
            pose?.Rest();
            return;
        }
        pose?.Apply(frame / timing.Duration, timing.HitStart / timing.Duration, attackForward,
            move == Move.Light2, move == Move.Light3, move == Move.Special,
            IsLight ? Color.white : ElementUtil.GetColor(attackElement));
    }

    void Begin(Move next)
    {
        FinishAttack();
        move = next;
        timing = next == Move.Light1 ? Light1 : next == Move.Light2 ? Light2 :
            next == Move.Light3 ? Light3 : next == Move.Special ? Special : Strong;
        speed = IsLight ? AttackSpeed : 1f;
        attackElement = CurrentElement;
        Vector3 direction = controller.InputDirection();
        controller.FaceDirection(direction);
        attackForward = transform.forward;
        frame = 0f;
        hits = 0;
        hitSet.Clear();

        if (next == Move.Light2) controller.StepForward(.9f);
        attackOrigin = transform.position;
        float duration = timing.Duration / (30f * speed);
        controller.SlowFor(duration + (IsLight || next == Move.Special ? 0f : .04f),
            next == Move.Special ? 0f : .18f);
        if (next != Move.Special) return;

        Gauge = 0f;
        invincibleUntil = Mathf.Max(invincibleUntil, duration + Time.time);
        CombatPresentation.Special(attackElement, attackOrigin);
    }

    bool IsLight => move == Move.Light1 || move == Move.Light2 || move == Move.Light3;
    bool CanDodge => move == Move.None || frame >= timing.DodgeStart;

    public bool TryCancelForDodge()
    {
        if (!CanDodge || IsDead) return false;
        FinishAttack();
        return true;
    }

    void FinishAttack()
    {
        if (move != Move.None) controller.SlowFor(0f, 1f);
        move = Move.None;
        impactPauseUntil = 0f;
        queued = Command.None;
        ApplySwordColor();
    }

    void BeginImpact()
    {
        Vector3 center = transform.position;
        if (move == Move.Special)
        {
            if (attackElement == Element.Wind)
            {
                center = ForwardFocus(attackOrigin, 3f);
                windBuffUntil = Time.time + 5f;
            }
            else center = attackOrigin;

            PlayEffect(FxKind.Special, attackElement, center, attackForward);

            CameraFollow.Shake(.34f, .25f);
            Rumble.Play(.6f, .7f, .24f);
            return;
        }

        CombatPresentation.Sound(0, center);
        if (!IsLight) PlayEffect(FxKind.Heavy, attackElement, center, attackForward);
    }

    void ResolveImpact()
    {
        candidates.Clear();
        candidates.AddRange(Enemy.All);
        Vector3 source = move == Move.Special ? attackOrigin : transform.position;
        if (IsLight)
        {
            float range = move == Move.Light3 ? 5.1f : 4f;
            float angle = move == Move.Light3 ? 145f : 175f;
            foreach (var enemy in candidates)
            {
                if (!CanHit(enemy)) continue;
                Vector3 target = enemy.AttackTargetFrom(source);
                if (!InSector(target, source, range, angle) || !ArenaGeometry.ClearPath(source, target)) continue;
                bool launch = move == Move.Light3;
                float push = launch ? 1.4f : move == Move.Light2 ? .28f : .10f;
                Hit(enemy, lightDamage * (launch ? 1.65f : 1f), null,
                    AwayFrom(enemy, source) * push);
                if (launch) enemy.Launch(1.25f);
                else if (!enemy.isBoss) enemy.Interrupt(.18f);
            }
            return;
        }

        if (attackElement == Element.Lightning)
        {
            ChainLightning(source);
            return;
        }

        bool special = move == Move.Special;
        bool branch = move == Move.Branch;
        Vector3 focus = ForwardFocus(source, special || branch ? 3f : 1.8f);
        foreach (var enemy in candidates)
        {
            if (!CanHit(enemy)) continue;
            Vector3 target = enemy.AttackTargetFrom(source);
            if (FlatDistanceSquared(source, target) > 100f || !ArenaGeometry.ClearPath(source, target)) continue;
            switch (attackElement)
            {
                case Element.Fire:
                    Vector3 blastTarget = branch ? enemy.AttackTargetFrom(focus) : target;
                    bool blast = branch && FlatDistanceSquared(blastTarget, focus) <= 2.2f * 2.2f &&
                        ArenaGeometry.ClearPath(focus, blastTarget);
                    if (!blast && !InSector(target, source, special ? FireSpecialRadius : 5f, special ? 360f : 155f)) continue;
                    Hit(enemy, special ? specialDamage : heavyDamage * (branch ? 1.25f : 1f),
                        Element.Fire, AwayFrom(enemy, blast ? focus : source) * (special ? 5f : branch ? 3.5f : 1.6f));
                    if (!enemy.isBoss) enemy.Interrupt(special || branch ? .45f : .22f);
                    break;
                case Element.Wind:
                    float radius = special ? WindSpecialRadius : branch ? 4.5f : 3.1f;
                    Vector3 windTarget = enemy.AttackTargetFrom(focus);
                    if (FlatDistanceSquared(windTarget, focus) > radius * radius ||
                        !ArenaGeometry.ClearPath(focus, windTarget)) continue;
                    Hit(enemy, special ? heavyDamage : heavyDamage * .65f, Element.Wind, Vector3.zero);
                    enemy.PullToward(focus, special ? 4f : branch ? 2.5f : 1.2f);
                    break;
                case Element.Ice:
                    Vector3 offset = target - source;
                    float forward = Vector3.Dot(offset, attackForward);
                    float lateral = Vector3.Dot(offset, Vector3.Cross(Vector3.up, attackForward));
                    bool inRange = branch ? forward >= 0f && forward <= 5f && Mathf.Abs(lateral) <= 1.4f :
                        InSector(target, source, special ? IceSpecialRadius : 4.1f, special ? 100f : 150f);
                    if (!inRange) continue;
                    Hit(enemy, heavyDamage * (special ? .65f : .60f), Element.Ice, Vector3.zero);
                    if (branch || special) enemy.Freeze(special ? 2f : 1.5f);
                    break;
            }
        }
    }

    void ChainLightning(Vector3 source)
    {
        if (hits > 0) return;
        bool special = move == Move.Special;
        Enemy current = NearestTarget(source, special ? LightningSpecialRange : 5f, true);
        int remaining = special ? 12 : move == Move.Branch ? 6 : 3;
        Vector3 previous = source;
        while (current != null && remaining-- > 0)
        {
            Vector3 point = current.AttackTargetFrom(previous);
            CombatPresentation.LightningArc(previous + Vector3.up, point + Vector3.up, ElementUtil.GetColor(Element.Lightning));
            Hit(current, special ? specialDamage : heavyDamage * (hits == 0 ? 1f : .75f), Element.Lightning, Vector3.zero);
            previous = point;
            current = NearestTarget(point, 3.6f, false);
        }
    }

    Enemy NearestTarget(Vector3 origin, float range, bool first)
    {
        Enemy nearest = null;
        float nearestDistance = range * range;
        foreach (var enemy in candidates)
        {
            if (!CanHit(enemy)) continue;
            Vector3 target = enemy.AttackTargetFrom(origin);
            if (first && !InSector(target, origin, range, 120f)) continue;
            float distance = FlatDistanceSquared(origin, target);
            if (distance > nearestDistance || (nearest != null &&
                Mathf.Approximately(distance, nearestDistance) && enemy.GetInstanceID() >= nearest.GetInstanceID())) continue;
            if (!ArenaGeometry.ClearPath(origin, target)) continue;
            nearest = enemy;
            nearestDistance = distance;
        }
        return nearest;
    }

    bool CanHit(Enemy enemy) => enemy != null && !enemy.IsDead && !hitSet.Contains(enemy);

    Vector3 ForwardFocus(Vector3 source, float distance)
    {
        float clear = ArenaGeometry.ClearDistance(source + Vector3.up * .8f, attackForward, distance, .15f);
        return source + attackForward * Mathf.Max(0f, clear - (clear < distance ? .12f : 0f));
    }

    bool InSector(Vector3 target, Vector3 origin, float range, float angle)
    {
        Vector3 offset = target - origin;
        offset.y = 0f;
        float distance = offset.sqrMagnitude;
        return distance <= range * range && (angle >= 360f || distance < .01f ||
            Vector3.Dot(attackForward, offset) >= Mathf.Sqrt(distance) * Mathf.Cos(angle * .5f * Mathf.Deg2Rad));
    }

    static float FlatDistanceSquared(Vector3 from, Vector3 to)
    {
        Vector3 offset = to - from;
        return offset.x * offset.x + offset.z * offset.z;
    }

    static Vector3 AwayFrom(Enemy enemy, Vector3 origin) =>
        Vector3.ProjectOnPlane(enemy.transform.position - origin, Vector3.up).normalized;

    void Hit(Enemy enemy, float damage, Element? element, Vector3 knockback)
    {
        if (!hitSet.Add(enemy)) return;
        Vector3 hitPosition = enemy.AttackTargetFrom(transform.position) + Vector3.up * .9f;
        if (!enemy.TakeHit(damage, element, knockback)) return;
        if (enemy.isBoss && !enemy.IsDead && !IsLight && !enemy.IsBroken)
            enemy.DamageBreak(move == Move.Special ? 45f : 25f);
        bool first = hits == 0;
        hits++;
        if (move != Move.Special && (hits <= 6 || enemy.isBoss)) AddGauge(enemy.isBoss ? gaugePerHit * 2f : gaugePerHit);
        GameManager.Instance?.AddCombo(1);

        bool special = move == Move.Special;
        bool finisher = move == Move.Light3;
        bool heavy = !IsLight || finisher;
        if (hits <= 8)
        {
            // One prominent burst per move; the rest use small contact flashes.
            bool accent = first && (heavy || enemy.IsDead);
            float scale = accent ? (special ? 1.2f : .82f) : heavy ? .78f : .60f;
            CombatPresentation.StrikeImpact(hitPosition, element, accent, scale * fxScale,
                GetFx(FxKind.Hit, element ?? Element.Lightning));
        }

        if (first)
        {
            CombatPresentation.StrikeSound(heavy || enemy.IsDead, heavy ? .95f : .72f);
            if (!special)
            {
                float pause = heavy ? .048f : enemy.IsDead ? .032f : .02f;
                impactPauseUntil = Time.unscaledTime + pause;
                if (heavy) CameraFollow.Shake(finisher ? .16f : .23f, .14f);
                Rumble.Play(.12f, heavy ? .58f : .26f, .09f);
            }
        }
        else if (hits == 4 && !special)
        {
            CameraFollow.Shake(heavy ? .24f : .11f, .11f);
            CombatPresentation.StrikeSound(true, .30f);
        }
        if (IsAttackPaused) enemy.PauseOnHit(impactPauseUntil);
    }

    public void TakeDamage(float damage)
    {
        if (SettingsMenu.BlocksGameplay || IsDead || damage <= 0f || Time.time < invincibleUntil ||
            (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing)) return;
        if (controller.IsDodging)
        {
            if (Time.time - controller.DodgeStarted <= .12f) CombatPresentation.JustDodge(transform.position);
            return;
        }

        FinishAttack();
        Hp = Mathf.Max(0f, Hp - damage);
        invincibleUntil = Time.time + damageInvincibility;
        CombatPresentation.PlayerDamage();
        CameraFollow.Shake(.2f, .15f);
        if (Hp <= 0f)
        {
            IsDead = true;
            controller.EndBattle();
            AnimHelper.Trigger(anim, "Die");
            GameManager.Instance?.GameOver();
            return;
        }

        controller.StaggerFor(.2f);
        if (anim != null && anim.HasState(0, Animator.StringToHash("Base Layer.Damage")))
            anim.CrossFadeInFixedTime("Base Layer.Damage", .04f, 0);
    }

    public void EndCombat()
    {
        FinishAttack();
        controller.EndBattle();
    }

    public void CelebrateVictory()
    {
        EndCombat();
        if (anim == null) return;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (anim.HasState(0, Animator.StringToHash("Base Layer.Win")))
            anim.CrossFadeInFixedTime("Base Layer.Win", .15f, 0, 0f);
    }

    public void ResetRunResources()
    {
        Hp = maxHp;
        Gauge = 0f;
        windBuffUntil = 0f;
        CurrentElement = Element.Fire;
        ApplySwordColor();
    }

    public void RestoreHealth(float amount) { if (!IsDead && amount > 0f) Hp = Mathf.Min(maxHp, Hp + amount); }
    public void AddGauge(float amount) { if (!IsDead) Gauge = Mathf.Clamp(Gauge + amount, 0f, maxGauge); }

    public void ApplySwordColor()
    {
        if (swordRenderers == null) return;
        Color color = ElementUtil.GetColor(move == Move.None ? CurrentElement : attackElement);
        foreach (var renderer in swordRenderers)
        {
            if (renderer == null) continue;
            Material material = renderer.material;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2f);
            }
        }
    }

    GameObject GetFx(FxKind kind, Element element)
    {
        int index = (int)element;
        if (elementFx == null || index >= elementFx.Length || elementFx[index] == null) return null;
        ElementFx effects = elementFx[index];
        switch (kind)
        {
            case FxKind.Heavy: return effects.heavy != null ? effects.heavy : effects.slash;
            case FxKind.Special: return effects.special != null ? effects.special : effects.heavy;
            case FxKind.Switch: return effects.switchFx;
            default: return effects.hit;
        }
    }

    void PlayEffect(FxKind kind, Element element, Vector3 center, Vector3 forward)
    {
        GameObject prefab = GetFx(kind, element);
        if (prefab == null) return;
        Quaternion facing = Quaternion.LookRotation(forward);
        Vector3 offset = kind == FxKind.Heavy ? facing * fxOffset : Vector3.up * .1f;
        Vector3 rotation = kind == FxKind.Special ? specialRotation : kind == FxKind.Switch ? switchRotation : fxRotation;
        Fx.Play(prefab, center + offset, facing * Quaternion.Euler(rotation), fxScale);
    }
}
