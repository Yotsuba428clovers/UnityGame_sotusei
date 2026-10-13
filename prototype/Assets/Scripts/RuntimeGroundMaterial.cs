using UnityEngine;

// The shared asset remains editable; only this per-scene tiling instance is destroyed.
public sealed class RuntimeGroundMaterial : MonoBehaviour
{
    public Material ownedMaterial;
    void OnDestroy() { if(ownedMaterial!=null)Destroy(ownedMaterial); }
}
