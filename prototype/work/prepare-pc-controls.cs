var gm=GameManager.Instance;gm.GetComponent<EnemyWaveManager>().enabled=false;
var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
player.GetComponent<PlayerController>().EndBattle();
var enemies=Enemy.All.ToArray();
for(int i=0;i<enemies.Length;i++)
{
    enemies[i].enabled=false;
    if(i>0)enemies[i].gameObject.SetActive(false);
}
var target=enemies[0];target.name="PC Input Target";
target.transform.position=player.transform.position+player.transform.forward*2;
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
typeof(Enemy).GetField("<Hp>k__BackingField",flags).SetValue(target,10000f);
typeof(PlayerCombat).GetField("invincibleUntil",flags).SetValue(player,float.PositiveInfinity);
var input=player.GetComponent<PlayerInputs>();
foreach(var pair in new[]{new[]{"attack","<Keyboard>/f"},new[]{"heavy","<Keyboard>/g"},new[]{"special","<Keyboard>/z"}})
{
    var action=(UnityEngine.InputSystem.InputAction)typeof(PlayerInputs).GetField(pair[0],flags).GetValue(input);
    bool found=false;foreach(var binding in action.bindings)if(binding.path==pair[1])found=true;
    if(!found)throw new System.Exception("Missing new keyboard binding: "+pair[0]);
}
var gameView=UnityEditor.EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor"));gameView.Focus();
return "Prepared invulnerable player and target; F/G/Z bindings verified";
