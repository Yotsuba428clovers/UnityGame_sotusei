using System.Collections.Generic;
using UnityEngine;

public sealed class UpgradeManager : MonoBehaviour
{
    public UpgradeData[] catalog;
    public readonly List<UpgradeData> Choices = new List<UpgradeData>();
    PlayerBuild build;
    bool ownsCatalog;
    public bool IsChoosing { get; private set; }
    public event System.Action Chosen;

    public void Initialize(PlayerBuild target)
    {
        build = target;
        if(catalog==null || catalog.Length==0)catalog=Resources.LoadAll<UpgradeData>("RogueliteUpgrades");
        if (catalog != null && catalog.Length > 0) return;
        ownsCatalog = true;
        string[] titles = { "追撃の刃", "回避の達人", "反撃の構え", "破砕撃", "連撃の極意", "炎刃", "爆炎", "疾風", "風刃", "氷結刃", "氷砕", "雷撃", "雷迅", "剣の加護", "魔力循環", "鋭利な刃" };
        string[] descriptions = { "4段目に追加範囲攻撃", "ジャスト回避で最大HPの3%回復", "ジャスト回避後の次の攻撃+50%", "強攻撃のブレイク値+30%", "10Hitごとに攻撃力+3%（最大30%）", "通常攻撃に炎ダメージ+20%", "炎上した敵の撃破時に爆発", "通常攻撃速度+15%", "フィニッシュに風の範囲斬撃", "通常攻撃3Hitで凍結", "凍結中の敵への強攻撃+50%", "命中時20%で最大3体へ雷撃", "ジャスト回避で周囲に雷撃", "最大HP+20%・増加分回復", "属性ゲージ獲得量+20%", "通常・強攻撃力+10%" };
        catalog = new UpgradeData[titles.Length];
        for (int i = 0; i < catalog.Length; i++)
        {
            var data = ScriptableObject.CreateInstance<UpgradeData>();
            data.effect = (UpgradeEffect)i; data.title = titles[i]; data.description = descriptions[i];
            data.rarity = i >= 13 ? UpgradeRarity.Common : (i == 0 || i == 6 || i == 12 ? UpgradeRarity.Epic : UpgradeRarity.Rare);
            data.maxStacks = i >= 13 ? 5 : 1;
            catalog[i] = data;
        }
    }

    public bool Offer()
    {
        if (IsChoosing || build == null) return false;
        Choices.Clear();
        var available = new List<UpgradeData>();
        foreach (var data in catalog)
            if (data != null && build.Stacks(data.effect) < data.maxStacks) available.Add(data);
        while (Choices.Count < 3 && available.Count > 0)
        {
            float total = 0;
            foreach (var data in available) total += Weight(data, available);
            float roll = Random.value * total;
            var selected = available[available.Count - 1];
            foreach (var data in available) { roll -= Weight(data, available); if (roll <= 0) { selected = data; break; } }
            Choices.Add(selected); available.Remove(selected);
        }
        IsChoosing = Choices.Count > 0;
        if(IsChoosing)Time.timeScale=0;
        return IsChoosing;
    }

    static float Weight(UpgradeData data, List<UpgradeData> pool)
    {
        int count = 0;
        foreach (var item in pool) if (item.rarity == data.rarity) count++;
        return (data.rarity == UpgradeRarity.Common ? 60f : data.rarity == UpgradeRarity.Rare ? 30f : 10f) / count;
    }

    public void Choose(int index)
    {
        if (!IsChoosing || index < 0 || index >= Choices.Count || SettingsMenu.IsOpen) return;
        build.Apply(Choices[index]);
        IsChoosing = false; Time.timeScale=1; Choices.Clear(); Chosen?.Invoke();
    }

    void OnDestroy() { if (ownsCatalog && catalog != null) foreach (var data in catalog) if (data != null) Destroy(data); }
}
