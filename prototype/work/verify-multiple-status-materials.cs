UnityEngine.Time.timeScale=0;
var root=new UnityEngine.GameObject("Two material status test");root.transform.position=UnityEngine.Vector3.one*1000;
var mesh=new UnityEngine.Mesh();
mesh.vertices=new[]{new UnityEngine.Vector3(-1,0,0),new UnityEngine.Vector3(1,0,0),new UnityEngine.Vector3(-1,2,0),new UnityEngine.Vector3(1,2,0)};
mesh.subMeshCount=2;mesh.SetTriangles(new[]{0,2,1},0);mesh.SetTriangles(new[]{1,2,3},1);mesh.RecalculateNormals();mesh.RecalculateBounds();
root.AddComponent<UnityEngine.MeshFilter>().sharedMesh=mesh;
var renderer=root.AddComponent<UnityEngine.MeshRenderer>();
var sourceA=new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));sourceA.color=UnityEngine.Color.white;
var sourceB=new UnityEngine.Material(sourceA);sourceB.color=new UnityEngine.Color(.2f,.25f,.3f);
renderer.sharedMaterials=new[]{sourceA,sourceB};
var enemy=root.AddComponent<Enemy>();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
typeof(Enemy).GetField("<Hp>k__BackingField",flags).SetValue(enemy,100f);
var visual=root.AddComponent<EnemyStatusVisual>();
try
{
    enemy.TakeHit(0,Element.Fire,UnityEngine.Vector3.zero);visual.RefreshNow();
    foreach(var material in renderer.sharedMaterials)
    {
        var color=material.GetColor("_BaseColor");
        if(color.r<color.g*3)throw new System.Exception("Second material slot not tinted");
        if(!material.IsKeywordEnabled("_EMISSION"))throw new System.Exception("Emission not enabled");
    }
    if(sourceA.color!=UnityEngine.Color.white||sourceB.color!=new UnityEngine.Color(.2f,.25f,.3f))throw new System.Exception("Shared source changed");
    typeof(Enemy).GetField("burnUntil",flags).SetValue(enemy,0f);
    visual.RefreshNow();
    if(renderer.sharedMaterials[0].color!=sourceA.color||renderer.sharedMaterials[1].color!=sourceB.color)throw new System.Exception("Slots not restored");
    foreach(var material in renderer.sharedMaterials)if(material.IsKeywordEnabled("_EMISSION"))throw new System.Exception("Emission keyword not restored");
    return "PASS: two submesh/material slots, dark material tint, emission restore, shared source unchanged";
}
finally
{
    UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(mesh);
    UnityEngine.Object.DestroyImmediate(sourceA);UnityEngine.Object.DestroyImmediate(sourceB);
}
