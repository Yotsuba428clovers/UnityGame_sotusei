using UnityEngine;

// 画面表示(IMGUI方式なのでCanvas等の設定は不要)
public class HUD : MonoBehaviour
{
    const float H = 1080f;   // 仮想解像度の高さ
    PlayerCombat player;
    public Font font;   // 空なら標準フォント
    GUIStyle style;
    bool logged;

    static void Box(Rect r, Color c)
    {
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, c, 0f, 0f);
    }

    static void Bar(Rect r, float t, Color fill)
    {
        Box(r, new Color(0f, 0f, 0f, 0.6f));
        Box(new Rect(r.x + 3f, r.y + 3f, (r.width - 6f) * Mathf.Clamp01(t), r.height - 6f), fill);
    }

    void Label(Rect r, string s, int size, Color c, TextAnchor a)
    {
        style.fontSize = size;
        style.alignment = a;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), s, style);
        style.normal.textColor = c;
        GUI.Label(r, s, style);
    }

    void OnGUI()
    {
        if (style == null) style = new GUIStyle(GUI.skin.label);
        style.font = font;   // 毎回代入(nullなら標準フォント)
        if (!logged) { logged = true; Debug.Log("[HUD] font = " + (font != null ? font.name : "NULL")); }
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerCombat>();
            if (player == null) return;
        }
        var gm = GameManager.Instance;
        if (gm == null) return;

        float s = Screen.height / H;
        float W = Screen.width / s;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));

        // HP / 必殺ゲージ
        float hpT = player.Hp / player.maxHp;
        Bar(new Rect(40, 40, 520, 30), hpT, Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.3f, 0.9f, 0.4f), hpT));
        Label(new Rect(48, 40, 200, 30), "HP", 22, Color.white, TextAnchor.MiddleLeft);
        bool ready = player.Gauge >= player.maxGauge;
        Bar(new Rect(40, 78, 520, 20), player.Gauge / player.maxGauge,
            ready ? new Color(1f, 0.85f, 0.2f) : ElementUtil.GetColor(player.CurrentElement));
        Label(new Rect(48, 74, 500, 28), ready ? "SPECIAL READY  (B)" : "SPECIAL", 18, Color.white, TextAnchor.MiddleLeft);

        // 属性セレクタ
        for (int i = 0; i < 4; i++)
        {
            Element e = (Element)i;
            bool cur = e == player.CurrentElement;
            Color c = ElementUtil.GetColor(e);
            Rect r = new Rect(40 + i * 130, H - 120, 120, 56);
            Box(r, cur ? c : new Color(c.r * 0.3f, c.g * 0.3f, c.b * 0.3f, 0.8f));
            Label(r, ElementUtil.GetLabel(e), cur ? 26 : 20, cur ? Color.white : new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleCenter);
        }
        float bonus = player.SwitchBonusRemaining;
        if (bonus > 0f)
        {
            Bar(new Rect(40, H - 54, 510, 14), bonus / player.switchBonusTime, Color.white);
            Label(new Rect(40, H - 84, 510, 28), "SWITCH BONUS x" + player.switchBonusMultiplier.ToString("0.0"), 20, Color.white, TextAnchor.MiddleLeft);
        }

        // 撃破数 / ボスHP
        if (Enemy.Boss != null && !Enemy.Boss.IsDead)
        {
            Label(new Rect(W / 2 - 400, 20, 800, 30), "BOSS", 24, Color.white, TextAnchor.MiddleCenter);
            Bar(new Rect(W / 2 - 400, 54, 800, 26), Enemy.Boss.Hp / Enemy.Boss.maxHp, new Color(0.8f, 0.2f, 0.9f));
        }
        else
        {
            string k = gm.Kills < gm.killsToBoss ? "KILLS " + gm.Kills + " / " + gm.killsToBoss : "KILLS " + gm.Kills;
            Label(new Rect(W - 460, 40, 420, 40), k, 32, Color.white, TextAnchor.MiddleRight);
        }

        // コンボ
        if (gm.Combo > 1 && gm.ComboActive)
            Label(new Rect(W - 460, 300, 420, 90), gm.Combo + " HITS", 84, new Color(1f, 0.9f, 0.3f), TextAnchor.MiddleRight);

        // バナー
        if (gm.BannerRemaining > 0f)
            Label(new Rect(0, H * 0.3f, W, 120), gm.BannerText, 72, Color.white, TextAnchor.MiddleCenter);

        if (gm.State != GameManager.GameState.Playing)
            Label(new Rect(0, H * 0.3f + 120, W, 60),
                  "MAX COMBO " + gm.MaxCombo + "   -   Press START / R to restart", 32, Color.white, TextAnchor.MiddleCenter);

        // 操作ヒント
        Label(new Rect(W - 900, H - 50, 860, 30),
              "X:弱攻撃  Y:教攻撃  A:回避  B:必殺技  LB/RB:属性切り替え", 20, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleRight);


    }
}
