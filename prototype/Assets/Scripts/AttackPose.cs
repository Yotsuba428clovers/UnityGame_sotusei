using UnityEngine;

// Poses the current rig over its locomotion clip until authored attack clips are available.
sealed class AttackPose
{
    readonly Transform root, chest, arm, forearm, hand, sword;
    readonly TrailRenderer trail;
    Quaternion chestRest, armRest, forearmRest;
    bool posedChest, posedArm;

    public AttackPose(Transform root, Animator animator, Renderer[] swords)
    {
        this.root = root;
        if (animator != null && animator.isHuman)
        {
            chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            arm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            forearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }
        else if (animator != null)
        {
            foreach (Transform bone in animator.GetComponentsInChildren<Transform>())
            {
                switch (bone.name)
                {
                    case "Character1_Spine2": chest = bone; break;
                    case "Character1_RightArm": arm = bone; break;
                    case "Character1_RightForeArm": forearm = bone; break;
                    case "Character1_RightHand": hand = bone; break;
                }
            }
        }

        if (swords == null || swords.Length != 1 || swords[0] == null || swords[0].name != "MagicSword")
            return;

        // QuickSetup's blade is separate from the rig. Keep its original dimensions in world space.
        sword = swords[0].transform;
        var tip = new GameObject("Blade trail").transform;
        tip.SetParent(sword, false);
        tip.localPosition = Vector3.forward * .48f;
        if (CombatPresentation.Instance != null)
        {
            trail = tip.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = CombatPresentation.Instance.effectMaterial;
            trail.time = .16f;
            trail.minVertexDistance = .06f;
            trail.startWidth = .38f;
            trail.endWidth = 0f;
            trail.emitting = false;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    public void Restore()
    {
        if (posedChest) chest.localRotation = chestRest;
        if (posedArm)
        {
            arm.localRotation = armRest;
            forearm.localRotation = forearmRest;
        }
        posedChest = posedArm = false;
    }

    public void Rest()
    {
        if (trail != null) trail.emitting = false;
        if (sword == null || hand == null) return;
        SetSword(hand.position, (root.forward + root.up * .25f).normalized);
    }

    public void Dodge(float progress)
    {
        if (chest != null)
        {
            chestRest = chest.localRotation;
            chest.rotation = Quaternion.AngleAxis(25f * Mathf.Sin(progress * Mathf.PI), root.right) * chest.rotation;
            posedChest = true;
        }
        Rest();
    }

    public void Apply(float progress, float impact, Vector3 forward, bool reverse, bool rising, bool special, Color color)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        float swing = Mathf.SmoothStep(0f, 1f, (progress - impact + .13f) / .30f);
        float weight = Mathf.Min(Mathf.Clamp01(progress * 9f), Mathf.Clamp01((1f - progress) * 4f));
        float side = reverse ? -1f : 1f;
        float sweep = Mathf.Lerp(80f, -92f, swing) * side;
        float pitch = rising ? Mathf.Lerp(-58f, 72f, swing) :
            special ? Mathf.Lerp(78f, -10f, swing) : Mathf.Lerp(22f, -14f, swing);
        Vector3 horizontal = Quaternion.AngleAxis(sweep, Vector3.up) * forward;
        Vector3 blade = (horizontal * Mathf.Cos(pitch * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(pitch * Mathf.Deg2Rad)).normalized;

        if (chest != null)
        {
            chestRest = chest.localRotation;
            chest.rotation = Quaternion.AngleAxis(sweep * weight * .27f, Vector3.up) * chest.rotation;
            posedChest = true;
        }
        if (arm != null && forearm != null && hand != null)
        {
            armRest = arm.localRotation;
            forearmRest = forearm.localRotation;
            posedArm = true;
            Vector3 shoulder = arm.position;
            Vector3 reach = shoulder + horizontal * .43f + forward * .16f +
                Vector3.up * Mathf.Clamp(blade.y * .47f - .08f, -.38f, .40f);
            BendArm(Vector3.Lerp(hand.position, reach, weight), right);
        }
        if (sword != null)
        {
            Vector3 grip = hand != null ? hand.position : root.position + Vector3.up + horizontal * .40f;
            SetSword(grip, blade);
        }
        if (trail != null)
        {
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.emitting = swing > 0f && swing < 1f;
        }
    }

    void BendArm(Vector3 target, Vector3 right)
    {
        Vector3 shoulder = arm.position;
        float upper = Vector3.Distance(shoulder, forearm.position);
        float lower = Vector3.Distance(forearm.position, hand.position);
        Vector3 reach = target - shoulder;
        if (upper < .01f || lower < .01f || reach.sqrMagnitude < .0001f) return;

        float distance = Mathf.Clamp(reach.magnitude, Mathf.Abs(upper - lower) + .001f, upper + lower - .001f);
        Vector3 axis = reach.normalized;
        float along = (upper * upper - lower * lower + distance * distance) / (2f * distance);
        float bend = Mathf.Sqrt(Mathf.Max(0f, upper * upper - along * along));
        Vector3 pole = Vector3.ProjectOnPlane(right - Vector3.up, axis).normalized;
        Vector3 elbow = shoulder + axis * along + pole * bend;
        arm.rotation = Quaternion.FromToRotation(forearm.position - shoulder, elbow - shoulder) * arm.rotation;
        forearm.rotation = Quaternion.FromToRotation(hand.position - forearm.position,
            shoulder + axis * distance - forearm.position) * forearm.rotation;
    }

    void SetSword(Vector3 grip, Vector3 direction)
    {
        sword.SetPositionAndRotation(grip + direction * .56f, Quaternion.LookRotation(direction, root.up));
    }
}
