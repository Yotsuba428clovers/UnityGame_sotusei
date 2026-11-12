using UnityEngine;

public static class ArenaGeometry
{
    static readonly RaycastHit[] Hits = new RaycastHit[64];

    public static bool ClearPath(Vector3 from, Vector3 to)
    {
        Vector3 offset = to - from;
        float distance = offset.magnitude;
        return distance < .01f || ClearDistance(from + Vector3.up * .8f, offset / distance, distance) >= distance;
    }

    public static float ClearDistance(Vector3 origin, Vector3 direction, float distance, float radius = 0f)
    {
        int count = radius > 0f
            ? Physics.SphereCastNonAlloc(origin, radius, direction, Hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            : Physics.RaycastNonAlloc(origin, direction, Hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float clear = distance;
        for (int i = 0; i < count; i++)
        {
            Collider collider = Hits[i].collider;
            if (collider.GetComponentInParent<Enemy>() != null || collider.GetComponentInParent<PlayerController>() != null)
                continue;
            clear = Mathf.Min(clear, Hits[i].distance);
        }
        return clear;
    }
}
