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
    public bool Locked { get; set; }

    CharacterController cc;
    PlayerInputs input;
    Animator anim;
    bool hasSpeedParam;
    float vy, dodgeEnd, nextDodge, slowUntil, slowMul = 1f;
    Vector3 dodgeDir;
    float dodgeSpeedNow;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputs>();
    }

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        hasSpeedParam = AnimHelper.Has(anim, "Speed", AnimatorControllerParameterType.Float);
    }

    // 攻撃中などに移動を遅くする
    public void SlowFor(float duration, float multiplier)
    {
        slowUntil = Time.time + duration;
        slowMul = multiplier;
    }

    // 回避(無敵ダッシュ)を開始。通常の回避も、雷の強攻撃ダッシュもこれを使う
    public void BeginDodge(Vector3 dir, float speed, float duration)
    {
        IsDodging = true;
        dodgeEnd = Time.time + duration;
        dodgeSpeedNow = speed;
        dodgeDir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
        FaceDirection(dodgeDir);
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
        Vector3 dir = Locked ? Vector3.zero : InputDirection();

        if (!Locked && input.Dodge && !IsDodging && Time.time >= nextDodge)
        {
            nextDodge = Time.time + dodgeCooldown;
            BeginDodge(dir, dodgeSpeed, dodgeDuration);
        }

        Vector3 vel = Vector3.zero;
        if (IsDodging)
        {
            if (Time.time >= dodgeEnd)
            {
                IsDodging = false;
                SetIgnoreEnemies(false);
            }
            vel = dodgeDir * dodgeSpeedNow;
        }
        else if (!Locked)
        {
            float mul = Time.time < slowUntil ? slowMul : 1f;
            vel = dir * moveSpeed * mul;
            if (dir.sqrMagnitude > 0.01f && mul > 0.2f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
        }

        if (cc.isGrounded && vy < 0f) vy = -2f;
        vy += gravity * Time.deltaTime;
        vel.y = vy;
        cc.Move(vel * Time.deltaTime);

        if (hasSpeedParam) anim.SetFloat("Speed", new Vector3(vel.x, 0f, vel.z).magnitude);
    }
}
