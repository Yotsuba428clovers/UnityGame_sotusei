using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public static class UiLanguage
{
    public static bool English => GameSettings.GetInt("Language", 0) == 1;
    public static event Action Changed;
    static Font japaneseFont;
    public static Font JapaneseFont => japaneseFont != null ? japaneseFont :
        japaneseFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Arial" }, 24);
    public static string Pick(string japanese, string english) => English ? english : japanese;
    public static void SetEnglish(bool english)
    {
        GameSettings.SetInt("Language", english ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    static readonly Dictionary<string, string> Japanese = new Dictionary<string, string>
    {
        {"SETTINGS", "設定"}, {"LANGUAGE", "言語"}, {"AUDIO", "音量"},
        {"SOUND EFFECTS", "効果音"}, {"VOICE", "ボイス"}, {"GRAPHICS", "画面設定"},
        {"RESOLUTION", "解像度"}, {"DISPLAY MODE", "表示モード"}, {"QUALITY", "画質"},
        {"CAMERA", "カメラ"}, {"CONTROLS", "操作設定"}, {"LOOK SENSITIVITY", "カメラ感度"},
        {"VIBRATION", "振動"}, {"FULLSCREEN  >", "フルスクリーン  >"}, {"WINDOWED  >", "ウィンドウ  >"},
        {"FIRST PERSON  >", "一人称  >"}, {"THIRD PERSON  >", "三人称  >"},
        {"ON  >", "オン  >"}, {"OFF  >", "オフ  >"}, {"CLOSE / RESUME", "閉じる / 戻る"},
        {"RESTART BATTLE", "最初からやり直す"}, {"OPTIONS / MENU / ESC  ·  CLOSE", "OPTIONS / Esc：閉じる"},
        {"ENTER THE ARENA                                      →", "戦闘を開始                                      →"},
        {"HOW TO PLAY                                              +", "操作方法                                      +"},
        {"HOW TO PLAY", "操作方法"},
        {"BACK TO TITLE                                          →", "タイトルへ戻る                                      →"},
        {"← TITLE", "← タイトル"}, {"ELEMENT\nSHIFT", "エレメント\nシフト"},
        {"E / A     ELEMENTAL ARCHIVE", "E / A     属性の記録"},
        {"ACTION RPG  /  PROTOTYPE 01", "アクションRPG  /  試作版01"},
        {"FOUR ELEMENTS. ONE BLADE.", "四つの属性、一振りの魔剣。"},
        {"Change your element. Find your rhythm.\nEnter the arena and make every strike count.", "属性を変え、自分だけの戦い方を。\n魔剣を手に、戦場へ踏み出そう。"},
        {"THE ELEMENTAL CYCLE", "巡る四つの属性"}, {"READY WHEN YOU ARE", "準備ができたら、戦闘開始"},
        {"MOUSE / KEYBOARD / CONTROLLER", "マウス / キーボード / コントローラー対応"},
        {"MASTER THE ELEMENTS", "操作方法"}, {"PREPARING THE ARENA…", "戦闘の準備中…"},
        {"FLAME BURST!", "煉獄斬！"}, {"FROZEN WORLD!", "絶氷領域！"},
        {"DEFEAT 20 ENEMIES", "敵を20体撃破せよ"},
        {"PLAY AGAIN  /  R", "再挑戦  /  R"}, {"BACK TO TITLE", "タイトルへ戻る"},
        {"SCENE UNAVAILABLE — CHECK BUILD SETTINGS", "シーンが未登録です。ビルド設定を確認してください。"},
        {"MOVE       WASD / Left stick\nCAMERA   Arrow keys / Right stick\nATTACK    J / X     ·     HEAVY    K / Y\nDODGE      Space / A     ·     SPECIAL   L / B\nELEMENT  Q, E / LB, RB\n\nChain attacks. Switch elements. Defeat the boss.",
         "移動：WASD / 左スティック\n視点：矢印キー / 右スティック\n通常攻撃：J / □　強攻撃：K / △\n回避：Space / ×　必殺技：L / ○\n属性切替：Q・E / L1・R1\n設定：Esc / OPTIONS\n報酬選択：← → / 十字キー、Enter / ×で決定\n再挑戦：リザルトのボタン / R\n\n連続攻撃と属性を使い分け、ボスを倒そう。"},
        {"Z / B  ·  ULT", "Z / ○：ULT"}, {"Q / E   ·   LB / RB", "Q / E：属性切替　L1 / R1"},
        {"MOUSE: LOOK    F: ATTACK    G: HEAVY    SPACE: DODGE", "マウス：視点　F：攻撃　G：強攻撃　Space：回避"},
        {"R  ·  PLAY AGAIN     OPTIONS  ·  SETTINGS", "R：再挑戦　OPTIONS：設定"},
        {"Low  >", "低  >"}, {"Medium  >", "中  >"}, {"High  >", "高  >"},
        {"Very Low  >", "最低  >"}, {"Very High  >", "最高  >"}, {"Ultra  >", "最高品質  >"},
        {"Very Low", "最低"}, {"Low", "低"}, {"Medium", "中"}, {"High", "高"}, {"Very High", "最高"}, {"Ultra", "最高品質"},
        {"High Fidelity  >", "高品質  >"}, {"Balanced  >", "標準  >"}, {"Performant  >", "軽量  >"}
    };
    static readonly KeyValuePair<string,string>[] Tokens =
    {
        new KeyValuePair<string,string>("THUNDER MODE", "雷強化"), new KeyValuePair<string,string>("WIND MODE", "風強化"),
        new KeyValuePair<string,string>("MAX COMBO", "最大コンボ"), new KeyValuePair<string,string>("ENEMIES DEFEATED", "撃破数"),
        new KeyValuePair<string,string>("HP REMAINING", "残りHP"), new KeyValuePair<string,string>("SPECIAL READY", "必殺技準備完了"),
        new KeyValuePair<string,string>("SPECIAL", "必殺技"), new KeyValuePair<string,string>("CHARGING", "蓄積中"),
        new KeyValuePair<string,string>("ELEMENT SHIFT", "属性切替"), new KeyValuePair<string,string>("SWITCH BONUS", "切替ボーナス"),
        new KeyValuePair<string,string>("KEEP THE RHYTHM", "コンボをつなごう"), new KeyValuePair<string,string>("STAGE CLEAR", "ステージクリア"),
        new KeyValuePair<string,string>("RUN CLEAR", "ランクリア"), new KeyValuePair<string,string>("RUN FAILED", "ラン終了"),
        new KeyValuePair<string,string>("GAME OVER", "ゲームオーバー"), new KeyValuePair<string,string>("FINAL BOSS", "最終ボス"),
        new KeyValuePair<string,string>("BOSS APPEARS!", "ボス出現！"), new KeyValuePair<string,string>("B O S S", "ボス"),
        new KeyValuePair<string,string>("BREAK", "ブレイク"), new KeyValuePair<string,string>("SYNERGY", "属性相乗効果"),
        new KeyValuePair<string,string>("FIRE", "炎"), new KeyValuePair<string,string>("ICE", "氷"),
        new KeyValuePair<string,string>("THUNDER", "雷"), new KeyValuePair<string,string>("WIND", "風"),
        new KeyValuePair<string,string>("WAVE", "ウェーブ"), new KeyValuePair<string,string>("KILLS", "撃破数"),
        new KeyValuePair<string,string>("TIME", "時間"), new KeyValuePair<string,string>("Common", "コモン"),
        new KeyValuePair<string,string>("HIT", "ヒット"),
        new KeyValuePair<string,string>("Rare", "レア"), new KeyValuePair<string,string>("Epic", "エピック"),
        new KeyValuePair<string,string>("R TO RESTART / OPTIONS FOR SETTINGS", "R：再挑戦 / OPTIONS：設定")
    };
    public static string Translate(string source)
    {
        // Support the existing serialized help text as well as rebuilt title scenes.
        if(source!=null&&(source.StartsWith("MOVE       WASD")||source==EnglishControls))return Pick(JapaneseControls,EnglishControls);
        if(English || string.IsNullOrEmpty(source))return source;
        if(Japanese.TryGetValue(source,out var translated))return translated;
        foreach(var token in Tokens)source=Regex.Replace(source,@"\b"+Regex.Escape(token.Key)+@"\b",token.Value);
        return source;
    }
    static readonly string[] UpgradeNames = { "Follow-up Blade", "Dodge Master", "Counter Stance", "Shattering Strike", "Combo Mastery", "Flame Blade", "Inferno", "Swift Wind", "Wind Blade", "Frost Blade", "Ice Shatter", "Chain Lightning", "Thunder Dodge", "Sword's Blessing", "Mana Cycle", "Keen Edge" };
    static readonly string[] UpgradeDescriptions =
    {
        "Add a shockwave to the fourth combo hit", "Perfect dodge restores 3% max HP",
        "After a perfect dodge, next attack deals +50% damage", "Heavy attacks deal +30% break damage",
        "+3% damage per 10 combo hits (max +30%)", "Normal attacks add 20% fire damage",
        "Burning enemies explode on defeat", "+15% normal attack speed", "Finishers release a wind slash",
        "Three normal hits build up a freeze", "Heavy attacks add 50% damage to frozen enemies",
        "20% chance to strike up to 3 nearby enemies", "Perfect dodge strikes nearby enemies with lightning",
        "+20% max HP and heal the increase", "+20% gauge gained on hits", "+10% normal and heavy attack damage"
    };
    public static string UpgradeName(UpgradeData data) => English ? UpgradeNames[(int)data.effect] : data.title;
    public static string UpgradeDescription(UpgradeData data) => English ? UpgradeDescriptions[(int)data.effect] : data.description;
    public const string JapaneseControls = "移動：WASD / 左スティック　視点：マウス / 右スティック\n弱攻撃：左クリック / □　強攻撃：右クリック / △\n回避：Space / ×　必殺技：F / R1\n属性：炎・風・氷・雷 → 1・2・3・4 / 十字キー ↑ → ↓ ←\n設定：Esc / OPTIONS　再挑戦：R\n\n弱は3連撃。弱2段目から強攻撃で属性技へ派生。\nゲージ100で必殺技。敵を20体倒すとクリア。";
    public const string EnglishControls = "Move: WASD / Left stick   Look: Mouse / Right stick\nLight: Left click / Square   Strong: Right click / Triangle\nDodge: Space / Cross   Special: F / R1\nFire, Wind, Ice, Thunder: 1, 2, 3, 4 / D-pad ↑ → ↓ ←\nSettings: Esc / OPTIONS   Retry: R\n\nChain three light attacks or branch from light 2 into strong.\nUse a special at 100 charge. Defeat 20 enemies to clear.";
}

