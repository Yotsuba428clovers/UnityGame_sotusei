UnityEngine.Time.timeScale=0;
var gm=GameManager.Instance;
gm.GetComponent<EnemyWaveManager>().enabled=false;
if(typeof(Enemy).GetField("weakElement")!=null||typeof(Enemy).GetField("weakMultiplier")!=null)throw new System.Exception("Enemy attributes remain");
var enemies=Enemy.All.ToArray();
if(enemies.Length<4)throw new System.Exception("Need 4 spawned model enemies");
for(int i=4;i<enemies.Length;i++)enemies[i].gameObject.SetActive(false);
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var properties=new[]{"_BaseColor","_Color","_1st_ShadeColor","_2nd_ShadeColor"};
int testedSlots=0;
for(int i=0;i<4;i++)
{
    var enemy=enemies[i];
    typeof(Enemy).GetField("<Hp>k__BackingField",flags).SetValue(enemy,10000f);
    var visual=enemy.GetComponent<EnemyStatusVisual>();
    if(visual==null)throw new System.Exception("Status visual missing");
    var original=new System.Collections.Generic.Dictionary<UnityEngine.Material,System.Collections.Generic.Dictionary<string,UnityEngine.Color>>();
    foreach(var renderer in enemy.GetComponentsInChildren<UnityEngine.Renderer>())
        foreach(var mat in renderer.sharedMaterials)
        {
            if(mat==null||original.ContainsKey(mat))continue;
            var values=new System.Collections.Generic.Dictionary<string,UnityEngine.Color>();
            foreach(var property in properties)if(mat.HasProperty(property))values[property]=mat.GetColor(property);
            if(values.Count>0)original[mat]=values;
        }
    for(int element=0;element<4;element++)
    {
        float hp=enemy.Hp;enemy.TakeHit(10,(Element)element,UnityEngine.Vector3.right);
        if(UnityEngine.Mathf.Abs(hp-enemy.Hp-10)>.001f)throw new System.Exception("Attribute damage multiplier remains");
    }
    visual.RefreshNow();
    if(!visual.StatusText.Contains("炎上")||!visual.StatusText.Contains("感電"))throw new System.Exception("Combined status names missing");
    foreach(var timer in new[]{"burnUntil","slowUntil","shockUntil","windUntil","frozenUntil","breakUntil","flashUntil"})typeof(Enemy).GetField(timer,flags).SetValue(enemy,0f);
    visual.RefreshNow();
    if(visual.StatusText!="")throw new System.Exception("Status names not cleared");
    foreach(var slot in original)
    {
        testedSlots++;
        foreach(var color in slot.Value)if(slot.Key.GetColor(color.Key)!=color.Value)throw new System.Exception("Original material color not restored");
    }
    enemy.TakeHit(0,(Element)i,UnityEngine.Vector3.right);
    if(i==1)enemy.Freeze(5);
    visual.RefreshNow();
    foreach(var slot in original)
        foreach(var color in slot.Value)if(slot.Key.GetColor(color.Key)==color.Value)throw new System.Exception("Material slot did not receive status tint");
    enemy.transform.position=new UnityEngine.Vector3((i-1.5f)*2.2f,0,4);
    enemy.transform.rotation=UnityEngine.Quaternion.Euler(0,180,0);
    var animator=enemy.GetComponentInChildren<UnityEngine.Animator>();if(animator!=null)animator.enabled=false;
}
var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
player.GetComponent<PlayerController>().Locked=true;
var camera=UnityEngine.Camera.main;
camera.GetComponent<CameraFollow>().enabled=false;
camera.transform.position=new UnityEngine.Vector3(0,3,-7);
camera.transform.LookAt(new UnityEngine.Vector3(0,1.3f,4));
return "PASS: enemy attributes removed, equal damage for all 4 elements, all material slots tinted/restored, combined status names; slots="+testedSlots;
