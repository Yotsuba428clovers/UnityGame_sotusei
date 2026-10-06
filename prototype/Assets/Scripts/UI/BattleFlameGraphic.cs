using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class BattleFlameGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r=rectTransform.rect;
        Vector2[] outline={new Vector2(0,-.45f),new Vector2(-.33f,-.26f),new Vector2(-.35f,.02f),new Vector2(-.12f,.30f),new Vector2(-.1f,.03f),new Vector2(.12f,.49f),new Vector2(.20f,.11f),new Vector2(.34f,-.06f),new Vector2(.28f,-.30f)};
        vh.AddVert(r.center,color,Vector2.zero);
        foreach(var p in outline)vh.AddVert(r.center+Vector2.Scale(p,r.size),color,Vector2.zero);
        for(int i=0;i<outline.Length;i++)vh.AddTriangle(0,i+1,(i+1)%outline.Length+1);
    }
}
