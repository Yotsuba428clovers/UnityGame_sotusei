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

    static readonly Timing Light1 = new Timing(24, 7, 11, 13, 20, 17);
    static readonly Timing Light2 = new Timing(25, 8, 12, 14, 21, 18);
    static readonly Timing Light3 = new Timing(31, 11, 16, 0, 0, 22);
    static readonly Timing Strong = new Timing(33, 12, 17, 0, 0, 24);
    static readonly Timing Special = new Timing(30, 10.5f, 11.5f, 0, 0, 30);

    public float maxHp = 500f;
    public float maxGauge = 100f;
    public float gaugePerHit = 4f;
    public float damageInvincibility = 0.75f;
    public float lightDamage = 20f;
    public float heavyDamage = 55f;
    public float specialDamage = 160f;
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
    CombatPulse warning;
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

        if (input.SelectedElement is Element selected && selected != CurrentElement)
        {
            CurrentElement = selected;
            if (move == Move.None) ApplySwordColor();
            PlayEffect(FxKind.Switch, selected, transform.position, transform.forward, 0f);
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
            queueUntil = Time.time + .18f;
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

        if (next == Move.Light2) controller.StepForward(.55f);
        attackOrigin = transform.position;
        float duration = timing.Duration / (30f * speed);
        controller.SlowFor(duration + (IsLight || next == Move.Special ? 0f : .04f),
            next == Move.Special ? 0f : .18f);
        if (next != Move.Special) return;

        Gauge = 0f;
        float windup = timing.HitStart / 30f;
        invincibleUntil = Mathf.Max(invincibleUntil, duration + Time.time);
        CombatPresentation.Special(attackElement, attackOrigin);
        Vector3 center = attackElement == Element.Wind ? ForwardFocus(attackOrigin, 3f) : attackOrigin;
        float radius = attackElement == Element.Fire ? 6f : attackElement == Element.Ice ? 6f : 5f;
        float angle = attackElement == Element.Ice ? 100f : attackElement == Element.Lightning ? 120f : 360f;
        warning = CombatPulse.Create(center, ElementUtil.GetColor(attackElement), radius, windup, true,
            attackForward, angle);
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
        if (warning != null) Destroy(warning.gameObject);
        warning = null;
        if (move != Move.None) controller.SlowFor(0f, 1f);
        move = Move.None;
        impactPauseUntil = 0f;
        queued = Command.None;
        ApplySwordColor();
    }

    void BeginImpact()
    {
        if (warning != null) Destroy(warning.gameObject);
        warning = null;
        Vector3 center = transform.position;
        if (move == Move.Special)
        {
            if (attackElement == Element.Wind)
            {
                center = ForwardFocus(attackOrigin, 3f);
                windBuffUntil = Time.time + 5f;
            }
            else center = attackOrigin;

            PlayEffect(FxKind.Special, attackElement, center, attackForward, 0f);
            float radius = attackElement == Element.Fire ? 6f : attackElement == Element.Ice ? 6f : 5f;
            CombatPulse.Create(center, ElementUtil.GetColor(attackElement), radius, .42f, false, attackForward,
                attackElement == Element.Ice ? 100f : attackElement == Element.Lightning ? 120f : 360f);
            CameraFollow.Shake(.22f, .22f);
            Rumble.Play(.6f, .7f, .24f);
            return;
        }

        CombatPresentation.Sound(0, center);
        if (IsLight)
        {
            // Neutral slash; the actual sword movement is driven by the attack pose.
            CombatPulse.Create(center, Color.white, move == Move.Light3 ? 3.5f : 3f, .17f, false,
                attackForward, move == Move.Light3 ? 100f : 155f);
            return;
        }
        PlayEffect(FxKind.Heavy, attackElement, center, attackForward, 3.7f);
        if (move == Move.Branch && attackElement == Element.Fire)
            CombatPulse.Create(ForwardFocus(center, 3f), ElementUtil.GetColor(attackElement), 2.2f, .28f);
    }

    void ResolveImpact()
    {
        candidates.Clear();
        candidates.AddRange(Enemy.All);
        Vector3 source = move == Move.Special ? attackOrigin : transform.position;
        if (IsLight)
        {
            float range = move == Move.Light3 ? 3.5f : 3f;
            float angle = move == Move.Light3 ? 100f : 155f;
            foreach (var enemy in candidates)
            {
                if (!CanHit(enemy) || !InSector(enemy, source, range, angle) ||
                    !ArenaGeometry.ClearPath(source, enemy.transform.position)) continue;
                bool launch = move == Move.Light3;
                float push = move == Move.Light2 ? .45f : .12f;
                Hit(enemy, lightDamage * (launch ? 1.65f : 1f), null,
                    AwayFrom(enemy, source) * push);
                if (launch) enemy.Launch(.85f);
                else enemy.Interrupt(.18f);
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
            if (!CanHit(enemy) || FlatDistanceSquared(source, enemy.transform.position) > 64f ||
                !ArenaGeometry.ClearPath(source, enemy.transform.position)) continue;
            switch (attackElement)
            {
                case Element.Fire:
                    bool blast = branch && FlatDistanceSquared(enemy.transform.position, focus) <= 2.2f * 2.2f &&
                        ArenaGeometry.ClearPath(focus, enemy.transform.position);
                    if (!blast && !InSector(enemy, source, special ? 6f : 4f, special ? 360f : 125f)) continue;
                    Hit(enemy, special ? specialDamage : heavyDamage * (branch ? 1.25f : 1f),
                        Element.Fire, AwayFrom(enemy, blast ? focus : source) * (special ? 3.2f : branch ? 2f : .75f));
                    enemy.Interrupt(special || branch ? .45f : .22f);
                    break;
                case Element.Wind:
                    float radius = special ? 5f : branch ? 3.5f : 2.3f;
                    if (FlatDistanceSquared(enemy.transform.position, focus) > radius * radius ||
                        !ArenaGeometry.ClearPath(focus, enemy.transform.position)) continue;
                    Hit(enemy, special ? heavyDamage : heavyDamage * .65f, Element.Wind, Vector3.zero);
                    enemy.PullToward(focus, special ? 4f : branch ? 2.5f : 1.2f);
                    break;
                case Element.Ice:
                    Vector3 offset = enemy.transform.position - source;
                    float forward = Vector3.Dot(offset, attackForward);
                    float lateral = Vector3.Dot(offset, Vector3.Cross(Vector3.up, attackForward));
                    bool inRange = branch ? forward >= 0f && forward <= 5f && Mathf.Abs(lateral) <= 1.4f :
                        InSector(enemy, source, special ? 6f : 3.3f, special ? 100f : 140f);
                    if (!inRange) continue;
                    Hit(enemy, heavyDamage * (special ? .65f : .75f), Element.Ice, Vector3.zero);
                    if (branch || special) enemy.Freeze(special ? 2f : 1.5f);
                    break;
            }
        }
    }

    void ChainLightning(Vector3 source)
    {
        if (hits > 0) return;
        bool special = move == Move.Special;
        Enemy current = NearestTarget(source, special ? 5f : 4f, true);
        int remaining = special ? 6 : move == Move.Branch ? 4 : 2;
        Vector3 previous = source;
        while (current != null && remaining-- > 0)
        {
            Vector3 point = current.transform.position;
            CombatPresentation.LightningArc(previous + Vector3.up, point + Vector3.up, ElementUtil.GetColor(Element.Lightning));
            Hit(current, heavyDamage * (hits == 0 ? 1f : .65f), Element.Lightning, Vector3.zero);
            previous = point;
            current = NearestTarget(point, 3f, false);
        }
    }

    Enemy NearestTarget(Vector3 origin, float range, bool first)
    {
        Enemy nearest = null;
        float nearestDistance = range * range;
        foreach (var enemy in candidates)
        {
            if (!CanHit(enemy) || (first && !InSector(enemy, origin, range, 120f))) continue;
            float distance = FlatDistanceSquared(origin, enemy.transform.position);
            if (distance > nearestDistance || (nearest != null &&
                Mathf.Approximately(distance, nearestDistance) && enemy.GetInstanceID() >= nearest.GetInstanceID())) continue;
            if (!ArenaGeometry.ClearPath(origin, enemy.transform.position)) continue;
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

    bool InSector(Enemy enemy, Vector3 origin, float range, float angle)
    {
        Vector3 offset = enemy.transform.position - origin;
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
        if (!hitSet.Add(enemy) || !enemy.TakeHit(damage, element, knockback)) return;
        bool first = hits == 0;
        hits++;
        if (move != Move.Special && hits <= 5) AddGauge(gaugePerHit);
        GameManager.Instance?.AddCombo(1);

        Color color = element.HasValue ? ElementUtil.GetColor(element.Value) : Color.white;
        if (hits <= 6) CombatPresentation.Impact(enemy.transform.position + Vector3.up * .2f, color, !IsLight);
        if (element.HasValue && hits <= 5)
        {
            GameObject effect = GetFx(FxKind.Hit, element.Value);
            if (effect != null)
                Fx.Play(effect, enemy.transform.position + Vector3.up, Quaternion.identity, fxScale);
        }
        if (first && move != Move.Special)
        {
            if (!IsLight)
            {
                CameraFollow.Shake(.14f, .11f);
                impactPauseUntil = Time.unscaledTime + .04f;
            }
            Rumble.Play(.12f, IsLight ? .26f : .55f, .09f);
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

    void PlayEffect(FxKind kind, Element element, Vector3 center, Vector3 forward, float fallbackRadius)
    {
        GameObject prefab = GetFx(kind, element);
        if (prefab == null)
        {
            if (fallbackRadius > 0f)
                CombatPulse.Create(center, ElementUtil.GetColor(element), fallbackRadius, .2f, false, forward, 125f);
            return;
        }
        Quaternion facing = Quaternion.LookRotation(forward);
        Vector3 offset = kind == FxKind.Heavy ? facing * fxOffset : Vector3.up * .1f;
        Vector3 rotation = kind == FxKind.Special ? specialRotation : kind == FxKind.Switch ? switchRotation : fxRotation;
        Fx.Play(prefab, center + offset, facing * Quaternion.Euler(rotation), fxScale);
    }
}
