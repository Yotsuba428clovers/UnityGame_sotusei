var gm=GameManager.Instance;
var run=gm.GetComponent<RunManager>();
var pc=run.Build.GetComponent<PlayerCombat>();
if(run.Build.Upgrades.Count!=0 || pc.Gauge!=0 || pc.CurrentElement!=Element.Fire || pc.maxHp!=500 || pc.lightDamage!=20 || gm.MaxCombo!=0)throw new System.Exception("Reset did not restore baseline");
var waves=gm.GetComponent<EnemyWaveManager>();
typeof(EnemyWaveManager).GetField("<Wave>k__BackingField",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(waves,waves.normalWaves);
typeof(EnemyWaveManager).GetField("remaining",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(waves,0);
foreach(var enemy in Enemy.All.ToArray())enemy.TakeHit(100000,Element.Fire,UnityEngine.Vector3.zero);
return "Reset verified: no upgrades, original stats, empty gauge, fire; final wave completed";
