using UnityEngine;

public class CombatPulse : MonoBehaviour
{
    LineRenderer line;
    LineRenderer charge;
    float radius,life,elapsed;
    float angle;
    Color tint;
    bool warning;
    const int Points=80;
    Vector3[] positions=new Vector3[Points];
    public static CombatPulse Create(Vector3 position, Color color, float radius, float life,
        bool warning = false, Vector3 forward = default, float angle = 360f)
    {
        if(CombatPresentation.Instance==null)return null;
        var go=new GameObject(warning?"Attack warning":"Impact pulse");go.transform.position=position+Vector3.up*.055f;
        if (forward.sqrMagnitude > .001f) go.transform.rotation = Quaternion.LookRotation(forward);
        var pulse = go.AddComponent<CombatPulse>();
        pulse.radius = radius;
        pulse.life = Mathf.Max(.05f, life);
        pulse.tint = color;
        pulse.warning = warning;
        pulse.angle = angle;
        pulse.line=go.AddComponent<LineRenderer>();pulse.line.sharedMaterial=CombatPresentation.Instance.effectMaterial;
        pulse.line.useWorldSpace=false;pulse.line.loop=true;pulse.line.positionCount=Points;
        pulse.line.widthMultiplier=warning?.09f:.12f;pulse.line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        if(warning)
        {
            var inner=new GameObject("Warning charge");inner.transform.SetParent(go.transform,false);inner.transform.localPosition=Vector3.up*.01f;
            pulse.charge=inner.AddComponent<LineRenderer>();pulse.charge.sharedMaterial=pulse.line.sharedMaterial;
            pulse.charge.useWorldSpace=false;pulse.charge.loop=true;pulse.charge.positionCount=Points;pulse.charge.widthMultiplier=.05f;
        }
        pulse.Draw(warning?radius:.1f,1);return pulse;
    }
    void Draw(float size,float alpha)
    {
        bool sector = angle < 360f;
        int arcPoints = sector ? Points - 1 : Points;
        for (int i = 0; i < arcPoints; i++)
        {
            float fraction = i / (float)(sector ? arcPoints - 1 : arcPoints);
            float radians = (fraction - .5f) * angle * Mathf.Deg2Rad;
            positions[i] = new Vector3(Mathf.Sin(radians) * size, 0f, Mathf.Cos(radians) * size);
        }
        if (sector) positions[Points - 1] = Vector3.zero;
        line.SetPositions(positions);var c=new Color(tint.r,tint.g,tint.b,alpha);line.startColor=line.endColor=c;
    }
    void Update()
    {
        elapsed+=warning?Time.deltaTime:Time.unscaledDeltaTime;float p=Mathf.Clamp01(elapsed/life);
        if(warning)
        {
            line.widthMultiplier=.08f+p*.16f;Draw(radius,.45f+p*.55f);
            for(int i=0;i<Points;i++)positions[i]*=p;
            charge.SetPositions(positions);charge.startColor=charge.endColor=new Color(tint.r,tint.g,tint.b,.7f);
        }
        else Draw(Mathf.Lerp(.15f,radius,1-Mathf.Pow(1-p,2)),1-p);
        if(p>=1)Destroy(gameObject);
    }
}
