using UnityEngine;

// 右スティックで回り込めるTPSカメラ + 画面揺れ
public class CameraFollow : MonoBehaviour
{
    public Transform target;
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
            pitch = Mathf.Clamp(pitch - l.y * sensitivity * dt, minPitch, maxPitch);
        }

        Vector3 focus = target.position + Vector3.up * height;
        if (!init) { smoothed = focus; init = true; }
        smoothed = Vector3.Lerp(smoothed, focus, 1f - Mathf.Exp(-followSmooth * dt));

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pos = smoothed - rot * Vector3.forward * distance;

        if (shakeTime > 0f)
        {
            shakeTime -= dt;
            pos += Random.insideUnitSphere * shakeMag;
            if (shakeTime <= 0f) shakeMag = 0f;
        }
        transform.SetPositionAndRotation(pos, rot);
    }
    void OnDestroy(){if(instance==this)instance=null;}
}
