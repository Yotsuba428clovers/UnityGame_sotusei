using UnityEngine;

// 右スティックで回り込めるTPSカメラ + 画面揺れ
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public bool firstPerson;
    public float eyeHeight = 1.65f;
    public float distance = 7.5f;
    public float height = 1.6f;
    public float sensitivity = 140f;
    public float minPitch = -10f;
    public float maxPitch = 55f;
    public float followSmooth = 12f;

    static CameraFollow instance;
    PlayerInputs input;
    float yaw, pitch = 18f, shakeTime, shakeMag;
    Vector3 smoothed;
    bool init;
    Camera view;
    float baseFov, punch, punchRemaining;
    bool victory;
    bool victoryPrelude;
    Vector3 victoryPosition;
    Vector3 victorySmoothedPosition;
    float resultOffset;
    Quaternion victoryRotation;
    Renderer[] playerRenderers;
    UnityEngine.Rendering.ShadowCastingMode[] originalShadows;

    void Start()
    {
        sensitivity = GameSettings.Sensitivity;
        SetFirstPerson(GameSettings.FirstPerson(firstPerson));
    }

    public void SetFirstPerson(bool enabled)
    {
        RestorePlayerModel();
        firstPerson = enabled;
        init = false;
        if (victory || victoryPrelude) return;
        if (!firstPerson)
        {
            baseFov = 60f;
            if (view != null) { view.fieldOfView = baseFov; view.nearClipPlane = .1f; }
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            return;
        }
        if (!firstPerson || target == null) return;
        yaw = target.eulerAngles.y;
        pitch = 0f;
        if (view != null) { baseFov = 75f; view.fieldOfView = baseFov; view.nearClipPlane = .05f; }
        playerRenderers = target.GetComponentsInChildren<Renderer>();
        originalShadows = new UnityEngine.Rendering.ShadowCastingMode[playerRenderers.Length];
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            originalShadows[i] = playerRenderers[i].shadowCastingMode;
            playerRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    void RestorePlayerModel()
    {
        if (playerRenderers == null) return;
        for (int i = 0; i < playerRenderers.Length; i++)
            if (playerRenderers[i] != null) playerRenderers[i].shadowCastingMode = originalShadows[i];
        playerRenderers = null;
    }

    public void BeginVictory(Transform player)
    {
        RestorePlayerModel();
        victoryPrelude = false;
        victory = true;
        victorySmoothedPosition = transform.position;
        resultOffset = 0f;
        shakeTime = punchRemaining = 0;
        Vector3 focus = player.position + Vector3.up * 1.1f;
        victoryPosition = player.position + player.forward * 4.6f + Vector3.up * 1.55f;
        victoryRotation = Quaternion.LookRotation(focus - victoryPosition);
    }

    public void HoldBossDefeat()
    {
        victoryPrelude = true;
        shakeTime = punchRemaining = 0;
    }

    public void SetResultFraming(float progress)
    {
        resultOffset = 1.2f * Mathf.Clamp01(progress);
    }

    void Awake() { instance = this; view=GetComponent<Camera>(); if(view!=null)baseFov=view.fieldOfView; }

    public static void Punch(float amount,float seconds)
    {if(instance!=null){instance.punch=amount;instance.punchRemaining=seconds;}}

    public static void Shake(float magnitude, float time)
    {
        if (instance == null) return;
        instance.shakeMag = Mathf.Max(instance.shakeMag, magnitude);
        instance.shakeTime = Mathf.Max(instance.shakeTime, time);
    }

    void LateUpdate()
    {
        if (SettingsMenu.IsOpen) return;
        if (victoryPrelude) return;
        if (victory)
        {
            float blend = 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime);
            victorySmoothedPosition = Vector3.Lerp(victorySmoothedPosition, victoryPosition, blend);
            transform.position = victorySmoothedPosition + victoryRotation * Vector3.right * resultOffset;
            transform.rotation = Quaternion.Slerp(transform.rotation, victoryRotation, blend);
            if (view != null) view.fieldOfView = Mathf.Lerp(view.fieldOfView, 38f, blend);
            return;
        }
        if (target == null) return;
        if (input == null) input = target.GetComponent<PlayerInputs>();

        float dt = Time.unscaledDeltaTime;
        if(view!=null)
        {
            punchRemaining=Mathf.Max(0,punchRemaining-dt);
            view.fieldOfView=Mathf.Lerp(view.fieldOfView,baseFov-(punchRemaining>0?punch:0),1-Mathf.Exp(-9*dt));
        }
        if (input != null)
        {
            Vector2 l = input.Look;
            yaw += l.x * sensitivity * dt;
            pitch = Mathf.Clamp(pitch - l.y * sensitivity * dt, firstPerson ? -75f : minPitch, firstPerson ? 75f : maxPitch);
        }

        Vector3 focus = target.position + Vector3.up * height;
        if (!init) { smoothed = focus; init = true; }
        smoothed = Vector3.Lerp(smoothed, focus, 1f - Mathf.Exp(-followSmooth * dt));

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pos = firstPerson ? target.position + Vector3.up * eyeHeight : smoothed - rot * Vector3.forward * distance;

        if (shakeTime > 0f)
        {
            shakeTime -= dt;
            pos += Random.insideUnitSphere * shakeMag;
            if (shakeTime <= 0f) shakeMag = 0f;
        }
        transform.SetPositionAndRotation(pos, rot);
    }
    void OnDestroy(){RestorePlayerModel();if(instance==this)instance=null;}
}
