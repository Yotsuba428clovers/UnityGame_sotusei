using UnityEngine;

public enum UpgradeEffect { Shockwave, DodgeHeal, Counter, Shatter, ComboPower, FireBlade,
    Explosion, AttackSpeed, WindBlade, FreezeBlade, IceShatter, ChainLightning, DodgeLightning,
    MaxHealth, GaugeGain, Damage }
public enum UpgradeRarity { Common, Rare, Epic }

[CreateAssetMenu(menuName = "Roguelite/Upgrade")]
public sealed class UpgradeData : ScriptableObject
{
    public UpgradeEffect effect;
    public UpgradeRarity rarity;
    public string title;
    [TextArea] public string description;
    [Min(1)] public int maxStacks = 1;
}
