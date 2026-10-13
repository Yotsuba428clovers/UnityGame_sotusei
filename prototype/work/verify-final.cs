var gm=GameManager.Instance;
var rewards=gm.GetComponent<UpgradeManager>();
var build=gm.GetComponent<RunManager>().Build;
if(rewards.catalog.Length!=16)throw new System.Exception("Catalog missing");
foreach(var asset in rewards.catalog)if(!UnityEditor.AssetDatabase.Contains(asset))throw new System.Exception("Upgrade not loaded from asset");
foreach(var asset in rewards.catalog)build.Apply(asset);
var enemy=Enemy.All.Count>0?Enemy.All[0]:null;
if(enemy!=null)
{
    build.OnHit(enemy,0,false,true);build.OnHit(enemy,0,false,true);build.OnHit(enemy,0,false,true);
    if(!enemy.IsFrozen)throw new System.Exception("Ice accumulation failed");
}
build.JustDodge(-50);
if(build.BeginAttack()<1.5f)throw new System.Exception("Counter failed");
if(!rewards.Offer() || !RunManager.RewardOpen || UnityEngine.Time.timeScale!=0)throw new System.Exception("Reward pause failed");
return "16 persistent assets loaded; freeze and counter verified; reward UI open";
