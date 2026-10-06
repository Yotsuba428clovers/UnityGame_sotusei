using UnityEngine;

// 魔剣の属性
public enum Element { Fire, Ice, Lightning, Wind }

public static class ElementUtil
{
    public static Color GetColor(Element e)
    {
        switch (e)
        {
            case Element.Fire: return new Color(1f, 0.35f, 0.1f);
            case Element.Ice: return new Color(0.4f, 0.8f, 1f);
            case Element.Lightning: return new Color(1f, 0.9f, 0.2f);
            default: return new Color(0.5f, 1f, 0.6f);
        }
    }

    public static string GetLabel(Element e)
    {
        switch (e)
        {
            case Element.Fire: return "FIRE";
            case Element.Ice: return "ICE";
            case Element.Lightning: return "THUNDER";
            default: return "WIND";
        }
    }

    public static Element Next(Element e, int step) { return (Element)((((int)e + step) % 4 + 4) % 4); }

    // 炎<->氷、雷<->風 が反属性(弱点属性の反対は耐性になる)
    public static Element Opposite(Element e) { return (Element)((int)e ^ 1); }
}

// Animatorにパラメーターがある時だけ呼ぶ(自作モデルを後から差し替えやすくするため)
public static class AnimHelper
{
    public static bool Has(Animator a, string name, AnimatorControllerParameterType type)
    {
        if (a == null || a.runtimeAnimatorController == null) return false;
        foreach (var p in a.parameters) if (p.name == name && p.type == type) return true;
        return false;
    }

    public static void Trigger(Animator a, string name)
    {
        if (Has(a, name, AnimatorControllerParameterType.Trigger)) a.SetTrigger(name);
    }
}
