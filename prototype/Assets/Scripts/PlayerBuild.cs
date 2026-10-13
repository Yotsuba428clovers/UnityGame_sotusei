using System.Collections.Generic;
using UnityEngine;

// Runtime data only. Never serialize this component into the save file.
[RequireComponent(typeof(PlayerCombat))]
public sealed class PlayerBuild : MonoBehaviour
{
    readonly List<string> upgrades = new List<string>();
    public IReadOnlyList<string> Upgrades => upgrades.AsReadOnly();
    PlayerCombat combat;
    float baseHp, baseLight, baseHeavy, baseGaugeGain;
    readonly Dictionary<UpgradeEffect,int> stacks = new Dictionary<UpgradeEffect,int>();
    readonly Dictionary<Enemy,int> freezeHits = new Dictionary<Enemy,int>();
    bool counter;
    float lastDodge = -100f;
    readonly Dictionary<Enemy,float> frostExplosionUntil = new Dictionary<Enemy,float>();
    bool exploding;
    public bool OwnsFire => Has(UpgradeEffect.FireBlade) || Has(UpgradeEffect.Explosion);
    public bool OwnsIce => Has(UpgradeEffect.FreezeBlade) || Has(UpgradeEffect.IceShatter);
    public bool OwnsWind => Has(UpgradeEffect.AttackSpeed) || Has(UpgradeEffect.WindBlade);
    public bool OwnsLightning => Has(UpgradeEffect.ChainLightning) || Has(UpgradeEffect.DodgeLightning);
    public float ChainChance => OwnsWind && OwnsLightning ? .35f : .2f;
    public float FireRangeMultiplier => OwnsFire && OwnsWind ? 1.3f : 1f;
    public string SynergyText
    {
        get
        {
            var active=new List<string>();
            if(OwnsFire&&OwnsWind)active.Add("FIRE+WIND");
            if(OwnsFire&&OwnsLightning)active.Add("FIRE+THUNDER");
            if(OwnsFire&&OwnsIce)active.Add("FIRE+ICE");
            if(OwnsIce&&OwnsLightning)active.Add("ICE+THUNDER");
            if(OwnsIce&&OwnsWind)active.Add("ICE+WIND");
            if(OwnsWind&&OwnsLightning)active.Add("WIND+THUNDER");
            return string.Join(" / ",active);
        }
    }
    public float ElementDamage(Enemy enemy,float damage,Element element)
    {
        return element==Element.Lightning && OwnsIce && OwnsLightning && enemy.IsFrozen ? damage*1.5f : damage;
    }
    public int Stacks(UpgradeEffect effect) => stacks.TryGetValue(effect, out var count) ? count : 0;
    public bool Has(UpgradeEffect effect) => Stacks(effect) > 0;
    public float SpeedMultiplier => Has(UpgradeEffect.AttackSpeed) ? 1.15f : 1f;
    public void Apply(UpgradeData data)
    {
        if(data == null || Stacks(data.effect) >= data.maxStacks)return;
        stacks[data.effect]=Stacks(data.effect)+1;
        upgrades.Add(data.title);
        float previousMax=combat.maxHp;
        combat.maxHp=baseHp*(1+.2f*Stacks(UpgradeEffect.MaxHealth));
        combat.lightDamage=baseLight*(1+.1f*Stacks(UpgradeEffect.Damage));
        combat.heavyDamage=baseHeavy*(1+.1f*Stacks(UpgradeEffect.Damage));
        combat.gaugePerHit=baseGaugeGain*(1+.2f*Stacks(UpgradeEffect.GaugeGain));
        if(combat.maxHp>previousMax)combat.RestoreHealth(combat.maxHp-previousMax);
    }
    public float BeginAttack()
    {
        float multiplier=counter?1.5f:1f; counter=false;
        if(Has(UpgradeEffect.ComboPower) && GameManager.Instance != null)
            multiplier*=1+Mathf.Min(.3f,(GameManager.Instance.ComboActive ? GameManager.Instance.Combo/10 : 0)*.03f);
        return multiplier;
    }
    public void OnHit(Enemy enemy,float damage,bool heavy,bool normal,bool wasFrozen=false)
    {
        if(wasFrozen && OwnsFire && OwnsIce &&
            (!frostExplosionUntil.TryGetValue(enemy,out var until)||Time.time>=until))
        {
            if(!enemy.IsDead)frostExplosionUntil[enemy]=Time.time+.5f;
            Explode(enemy.transform.position,damage*.4f,enemy,true);
        }
        if(heavy)
        {
            enemy.DamageBreak(30f*(Has(UpgradeEffect.Shatter)?1.3f:1f));
            if(Has(UpgradeEffect.IceShatter)&&enemy.IsFrozen&&!enemy.IsDead)enemy.TakeHit(damage*.5f,Element.Ice,Vector3.zero);
        }
        if(!normal || enemy.IsDead)return;
        if(Has(UpgradeEffect.FireBlade))enemy.TakeHit(damage*.2f,Element.Fire,Vector3.zero);
        if(Has(UpgradeEffect.FreezeBlade)&&!enemy.IsDead)
        {
            int increment=OwnsIce&&OwnsWind&&combat.AttackSpeed>1f?2:1;
            int count=freezeHits.TryGetValue(enemy,out var value)?value+increment:increment;
            if(count>=3){enemy.Freeze(2);count=0;}
            freezeHits[enemy]=count;
        }
        if(Has(UpgradeEffect.ChainLightning)&&Random.value<ChainChance)Area(enemy.transform.position,5,damage*.4f,Element.Lightning,3,enemy);
    }
    public void Finisher()
    {
        if(Has(UpgradeEffect.Shockwave))Area(transform.position,5,combat.lightDamage*.6f,combat.CurrentElement);
        if(Has(UpgradeEffect.WindBlade))Area(transform.position+transform.forward*2,4,combat.lightDamage*.5f,Element.Wind);
    }
    public void FireBurst(Vector3 position,float radius,float damage)
    {
        if(OwnsFire&&OwnsLightning)Area(position,radius*FireRangeMultiplier,damage*.4f,Element.Lightning,3);
    }
    public void JustDodge(float start)
    {
        if(lastDodge==start)return;lastDodge=start;
        combat.AddGauge(8);
        if(Has(UpgradeEffect.DodgeHeal))combat.RestoreHealth(combat.maxHp*.03f);
        if(Has(UpgradeEffect.Counter))counter=true;
        if(Has(UpgradeEffect.DodgeLightning))Area(transform.position,5,combat.lightDamage,Element.Lightning);
        CombatPresentation.JustDodge(transform.position);
    }
    public void EnemyKilled(Enemy enemy)
    {
        freezeHits.Remove(enemy);
        frostExplosionUntil.Remove(enemy);
        if(Has(UpgradeEffect.Explosion)&&enemy.IsBurning)Explode(enemy.transform.position,combat.lightDamage*.75f,enemy);
    }
    void Explode(Vector3 position,float damage,Enemy source,bool includeSource=false)
    {
        // Secondary deaths never recursively spawn further explosions.
        if(exploding)return;
        exploding=true;
        try
        {
            Area(position,3,damage,Element.Fire,256,includeSource?null:source);
            if(OwnsFire&&OwnsLightning)Area(position,5,damage*.4f,Element.Lightning,3,source);
        }
        finally { exploding=false; }
    }
    void Area(Vector3 position,float radius,float damage,Element element,int limit=256,Enemy exclude=null)
    {
        if(element==Element.Fire)radius*=FireRangeMultiplier;
        CombatPulse.Create(position,ElementUtil.GetColor(element),radius,.3f);
        int hits=0;
        foreach(var enemy in Enemy.All.ToArray())
        {
            if(enemy==null||enemy==exclude||enemy.IsDead||(enemy.transform.position-position).sqrMagnitude>radius*radius)continue;
            enemy.TakeHit(ElementDamage(enemy,damage,element),element,Vector3.zero);
            if(++hits>=limit)break;
        }
    }

    void Awake()
    {
        combat = GetComponent<PlayerCombat>();
        baseHp = combat.maxHp;
        baseLight = combat.lightDamage;
        baseHeavy = combat.heavyDamage;
        baseGaugeGain = combat.gaugePerHit;
    }

    public void ResetBuild()
    {
        upgrades.Clear();
        stacks.Clear();freezeHits.Clear();frostExplosionUntil.Clear();counter=false;lastDodge=-100f;exploding=false;
        combat.maxHp = baseHp;
        combat.lightDamage = baseLight;
        combat.heavyDamage = baseHeavy;
        combat.gaugePerHit = baseGaugeGain;
        combat.ResetRunResources();
    }
}
