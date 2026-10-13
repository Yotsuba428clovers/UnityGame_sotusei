var gm=GameManager.Instance;
var build=gm.GetComponent<RunManager>().Build;
var catalog=gm.GetComponent<UpgradeManager>().catalog;
UnityEngine.Time.timeScale=0;
System.Action<UpgradeEffect> acquire = effect => { foreach(var item in catalog)if(item.effect==effect){build.Apply(item);break;} };
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var area=typeof(PlayerBuild).GetMethod("Area",flags);
var explosion=typeof(PlayerBuild).GetMethod("Explode",flags);
var root=new UnityEngine.GameObject("Synergy test enemy");
root.transform.position=new UnityEngine.Vector3(1000,0,1000);
var enemy=root.AddComponent<Enemy>();
enemy.maxHp=10000;
typeof(Enemy).GetField("<Hp>k__BackingField",flags).SetValue(enemy,10000f);
try
{
    build.ResetBuild();acquire(UpgradeEffect.FireBlade);acquire(UpgradeEffect.AttackSpeed);
    if(UnityEngine.Mathf.Abs(build.FireRangeMultiplier-1.3f)>.001f)throw new System.Exception("Fire/wind range");
    float hp=enemy.Hp;
    area.Invoke(build,new object[]{root.transform.position+UnityEngine.Vector3.right*3.5f,3f,10f,Element.Fire,256,null});
    if(enemy.Hp>=hp)throw new System.Exception("Expanded fire area did not hit");

    build.ResetBuild();acquire(UpgradeEffect.FireBlade);acquire(UpgradeEffect.ChainLightning);
    hp=enemy.Hp;
    explosion.Invoke(build,new object[]{root.transform.position,100f,null,false});
    float combined=hp-enemy.Hp;
    build.ResetBuild();acquire(UpgradeEffect.FireBlade);
    hp=enemy.Hp;
    explosion.Invoke(build,new object[]{root.transform.position,100f,null,false});
    if(combined<=hp-enemy.Hp)throw new System.Exception("Explosion lightning missing");

    build.ResetBuild();acquire(UpgradeEffect.FireBlade);acquire(UpgradeEffect.FreezeBlade);
    enemy.Freeze(10);hp=enemy.Hp;
    build.OnHit(enemy,100,false,false,true);
    if(enemy.Hp>=hp)throw new System.Exception("Frozen target explosion missing");
    hp=enemy.Hp;build.OnHit(enemy,100,false,false,true);
    if(enemy.Hp!=hp)throw new System.Exception("Frozen explosion cooldown failed");

    build.ResetBuild();acquire(UpgradeEffect.FreezeBlade);acquire(UpgradeEffect.ChainLightning);
    if(build.ElementDamage(enemy,100,Element.Lightning)!=150)throw new System.Exception("Frozen lightning multiplier");

    typeof(Enemy).GetField("frozenUntil",flags).SetValue(enemy,0f);
    build.ResetBuild();acquire(UpgradeEffect.FreezeBlade);acquire(UpgradeEffect.AttackSpeed);
    build.OnHit(enemy,0,false,true);build.OnHit(enemy,0,false,true);
    if(!enemy.IsFrozen)throw new System.Exception("Accelerated freeze missing");

    build.ResetBuild();acquire(UpgradeEffect.AttackSpeed);acquire(UpgradeEffect.ChainLightning);
    if(build.ChainChance!=.35f)throw new System.Exception("Wind lightning frequency");
    foreach(var data in catalog)build.Apply(data);
    if(build.SynergyText.Split('/').Length!=6)throw new System.Exception("HUD synergy list");
    build.ResetBuild();
    if(build.SynergyText!=""||build.FireRangeMultiplier!=1||build.ChainChance!=.2f)throw new System.Exception("Synergies not reset");
    return "PASS: 6 synergy effects, explosion cooldown, HUD list, and reset";
}
finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Time.timeScale=1; }
