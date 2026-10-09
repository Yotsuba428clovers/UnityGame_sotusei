var spawner=UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();spawner.enabled=false;
foreach(var e in UnityEngine.Object.FindObjectsByType<Enemy>(UnityEngine.FindObjectsSortMode.None))e.gameObject.SetActive(false);
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(EnemySpawner).GetField("spawnCount",flags).SetValue(spawner,0);
var spawn=typeof(EnemySpawner).GetMethod("Spawn",flags);var report=new System.Text.StringBuilder();int stone=0,skeleton=0;
for(int i=1;i<=30;i++){var before=new System.Collections.Generic.HashSet<Enemy>(Enemy.All);spawn.Invoke(spawner,new object[]{false});var enemy=System.Linq.Enumerable.First(Enemy.All,e=>!before.Contains(e));if(enemy.name.StartsWith("Stone")){stone++;report.AppendLine("Stone spawn index="+i);}else skeleton++;enemy.gameObject.SetActive(false);UnityEngine.Object.Destroy(enemy.gameObject);}
report.AppendLine("Normal spawns=30 Stone="+stone+" Skeleton="+skeleton+" alternate="+spawner.additionalEnemyPrefabs[0].name);
return report.ToString();
