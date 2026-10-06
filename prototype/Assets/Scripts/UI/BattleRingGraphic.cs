using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class BattleRingGraphic : MaskableGraphic
{
    [Range(0,1)] public float fill=1;
    [Range(.01f,1)] public float thickness=.08f;
    float oldFill=-1;
    void Update(){if(oldFill!=fill){oldFill=fill;SetVerticesDirty();}}
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
        int segments=Mathf.Max(1,Mathf.CeilToInt(128*Mathf.Clamp01(fill)));
        for(int i=0;i<=segments;i++)
        {
            float angle=Mathf.PI*.5f-Mathf.PI*2*Mathf.Clamp01(fill)*i/segments;
            Vector2 dir=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            vh.AddVert(rectTransform.rect.center+dir*radius,color,Vector2.zero);
            vh.AddVert(rectTransform.rect.center+dir*radius*(1-thickness),color,Vector2.zero);
            if(i>0){int n=i*2;vh.AddTriangle(n-2,n,n-1);vh.AddTriangle(n,n+1,n-1);}
        }
    }
}
