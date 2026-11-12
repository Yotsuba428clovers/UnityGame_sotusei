using UnityEngine;

// 移動・回避。カメラ基準の移動。
[RequireComponent(typeof(CharacterController), typeof(PlayerInputs))]
public class PlayerController : MonoBehaviour
{
    [Header("移動")]
    public float moveSpeed = 7f;
    public float turnSpeed = 14f;
    public float gravity = -25f;

    [Header("回避")]
    public float dodgeSpeed = 17f;
    public float dodgeDuration = 0.25f;
    public float dodgeCooldown = 0.45f;

    public Transform cameraTransform;

    public bool IsDodging { get; private set; }
    public float DodgeStarted { get; private set; }
    public float DodgeProgress => Mathf.Clamp01((Time.time - DodgeStarted) / Mathf.Max(.01f, dodgeDuration));
    public bool Locked { get; set; }
    public bool IsStaggered { get { return Time.time < staggerUntil; } }
    float staggerUntil;

    public void StaggerFor(float duration)
    {
        staggerUntil = Mathf.Max(staggerUntil, Time.time + duration);
    }

    public void EndBattle()
    {
        Locked = true;
        IsDodging = false;
        SetIgnoreEnemies(false);
    }

    CharacterController cc;
    PlayerInputs input;
    Animator anim;
    PlayerCombat combat;
    bool hasSpeedParam;
    float vy, dodgeEnd, nextDodge, slowUntil, slowMul = 1f;
    Vector3 dodgeDir;
    float dodgeSpeedNow;
    public bool DodgeReady => !Locked && !IsStaggered && !IsDodging && Time.time >= nextDodge;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputs>();
    }

    void Start()
    {
        combat = GetComponent<PlayerCombat>();
        anim = GetComponentInChildren<Animator>();
        hasSpeedParam = AnimHelper.Has(anim, "Speed", AnimatorControllerParameterType.Float);
    }

    // 攻撃中などに移動を遅くする
    public void SlowFor(float duration, float multiplier)
    {
        slowUntil = Time.time + duration;
        slowMul = multiplier;
    }

    public void StepForward(float distance)
    {
        cc.Move(transform.forward * distance);
    }

    void BeginDodge(Vector3 direction)
    {
        IsDodging = true;
        DodgeStarted = Time.time;
        dodgeEnd = Time.time + dodgeDuration;
        dodgeSpeedNow = dodgeSpeed;
        bool directed = direction.sqrMagnitude > .01f;
        dodgeDir = directed ? direction.normalized : -transform.forward;
        if (directed) FaceDirection(dodgeDir);
        SetIgnoreEnemies(true);
        AnimHelper.Trigger(anim, "Dodge");
    }

    // 回避中は敵をすり抜ける
    void SetIgnoreEnemies(bool ignore)
    {
        foreach (var e in Enemy.All) e.IgnoreCollision(cc, ignore);
    }

    public void FaceDirection(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
    }

    public Vector3 InputDirection()
    {
        Vector2 m = input.Move;
        if (m.sqrMagnitude < 0.0004f) return Vector3.zero;
        Transform cam = cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : null);
        Vector3 f = cam != null ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 r = cam != null ? Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized : Vector3.right;
        Vector3 d = f * m.y + r * m.x;
        return d.sqrMagnitude > 1f ? d.normalized : d;
    }

    void Update()
    {
        if (SettingsMenu.BlocksGameplay || !Application.isFocused) return;
        bool blocked = Locked || IsStaggered;
        Vector3 dir = blocked ? Vector3.zero : InputDirection();

        if (DodgeReady && input.Dodge && (combat == null || combat.TryCancelForDodge()))
        {
            nextDodge = Time.time + dodgeCooldown;
            BeginDodge(dir);
        }

        Vector3 vel = Vector3.zero;
        if (IsDodging)
        {
            if (Time.time >= dodgeEnd)
            {
                IsDodging = false;
                SetIgnoreEnemies(false);
            }
            else vel = dodgeDir * dodgeSpeedNow;
        }
        else if (!blocked)
        {
            float mul = combat != null && combat.IsAttackPaused ? 0f : Time.time < slowUntil ? slowMul : 1f;
            vel = dir * moveSpeed * mul;
            if (dir.sqrMagnitude > 0.01f && mul > 0.2f)
            {
                var follow = cameraTransform != null ? cameraTransform.GetComponent<CameraFollow>() : null;
                Vector3 facing = follow != null && follow.firstPerson ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up) : dir;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing), turnSpeed * Time.deltaTime);
            }
        }

        if (cc.isGrounded && vy < 0f) vy = -2f;
        vy += gravity * Time.deltaTime;
        vel.y = vy;
        cc.Move(vel * Time.deltaTime);

        if (hasSpeedParam) anim.SetFloat("Speed", new Vector3(vel.x, 0f, vel.z).magnitude);
    }
}
