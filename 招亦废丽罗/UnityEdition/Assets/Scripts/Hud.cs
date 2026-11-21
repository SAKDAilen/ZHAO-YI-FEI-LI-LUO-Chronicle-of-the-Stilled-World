// Hud.cs —— IMGUI 界面（中文动态字体：微软雅黑）
using System.Collections.Generic;
using UnityEngine;

public class Hud
{
    public Font font;
    public Texture2D white;
    GUIStyle stTitle, stSub, stClock, stHint, stSmall, stMsg, stDmg, stPrompt, stDlgTitle, stDlgBody, stChoice, stBar;

    public void Init()
    {
        try { font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 20); } catch { }
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        white = new Texture2D(1, 1);
        white.SetPixel(0, 0, Color.white);
        white.Apply();

        stTitle = NewStyle(30, TextAnchor.MiddleCenter);
        stSub = NewStyle(15, TextAnchor.MiddleCenter);
        stClock = NewStyle(17, TextAnchor.UpperRight);
        stHint = NewStyle(14, TextAnchor.LowerLeft);
        stSmall = NewStyle(13, TextAnchor.UpperLeft);
        stMsg = NewStyle(16, TextAnchor.LowerRight);
        stDmg = NewStyle(18, TextAnchor.MiddleCenter);
        stPrompt = NewStyle(17, TextAnchor.MiddleCenter);
        stDlgTitle = NewStyle(19, TextAnchor.UpperLeft);
        stDlgBody = NewStyle(16, TextAnchor.UpperLeft);
        stChoice = NewStyle(16, TextAnchor.UpperLeft);
        stBar = NewStyle(13, TextAnchor.MiddleLeft);
    }

    GUIStyle NewStyle(int size, TextAnchor anchor)
    {
        var s = new GUIStyle(GUI.skin.label);
        s.font = font;
        s.fontSize = size;
        s.alignment = anchor;
        s.wordWrap = true;
        s.richText = true;
        s.normal.textColor = Color.white;
        s.padding = new RectOffset(0, 0, 0, 0);
        s.margin = new RectOffset(0, 0, 0, 0);
        return s;
    }

    void Label(Rect r, string text, GUIStyle style, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.Label(r, text, style);
        GUI.color = old;
    }

    void Rect2(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, white);
        GUI.color = old;
    }

    public void Draw(GameManager g)
    {
        float w = Screen.width, h = Screen.height;
        float sx = w / 1280f, sy = h / 720f;
        float S = Mathf.Min(sx, sy);
        int fs = Mathf.RoundToInt(16 * S);

        // 标题与版本
        Label(new Rect(0, 8, w, 40), "招亦废丽罗 · 律令之楔", stTitle, new Color(1f, 0.92f, 0.80f, 0.95f));
        Label(new Rect(0, 46, w, 22), "v3.3 · Unity 引擎版 · CPU+GPU", stSub, new Color(0.80f, 0.84f, 0.88f, 0.8f));

        // 时钟 / 环境
        string clock = g.env.ClockText() + "   " + g.env.PhaseName() + " · " + g.env.weather + "\n" +
                       "能量" + g.env.energyState + " · 潮汐 " + Mathf.RoundToInt(g.env.tide * 100f) + "%   击杀 " + g.kills + " · 死亡 " + g.ctx.deaths;
        Label(new Rect(w - 360 * S, 12, 340 * S, 60), clock, stClock, new Color(0.92f, 0.88f, 0.80f, 0.9f));

        // FPS / 状态
        Label(new Rect(18 * S, 12, 300 * S, 40),
            "FPS " + g.fps.ToString("0.0") + " · " + g.frameMs.ToString("0.0") + "ms\n" +
            "区块 " + g.terrain.Count + " · 敌人 " + g.enemies.list.Count, stSmall, new Color(0.75f, 0.82f, 0.88f, 0.75f));

        // 操作提示
        Label(new Rect(18 * S, h - 96 * S, 620 * S, 80 * S),
            "WASD 移动（依相机） · Shift 冲刺 · 空格 跳跃 · Q 翻滚\n" +
            "鼠标左键 挥剑 · E 交互 / 对话 · Tab 锁定 · R 律令\n" +
            "1-4 对话选项 · F5 存档 · F9 读档 · F1 统计 · Esc 释放鼠标", stHint, new Color(0.85f, 0.85f, 0.82f, 0.8f));

        // 底部状态条
        float bw = Mathf.Min(560 * S, w * 0.5f);
        float bx = (w - bw) / 2f;
        float by = h - 52 * S;
        Bar(new Rect(bx, by, bw, 16 * S), g.player.hp / g.player.maxHp, new Color(0.62f, 0.13f, 0.12f), new Color(0.95f, 0.30f, 0.24f));
        Bar(new Rect(bx, by + 20 * S, bw, 11 * S), g.player.stamina / g.player.maxStamina, new Color(0.45f, 0.36f, 0.10f), new Color(0.90f, 0.76f, 0.26f));
        Bar(new Rect(bx, by + 35 * S, bw, 10 * S), g.player.lawEnergy / 100f, new Color(0.10f, 0.30f, 0.36f), new Color(0.35f, 0.85f, 0.95f));
        Label(new Rect(bx + bw + 10 * S, by, 120 * S, 20 * S), Mathf.CeilToInt(g.player.hp) + " / " + Mathf.CeilToInt(g.player.maxHp), stBar, Color.white);

        // 任务行
        string quests = DialogueRunner.QuestLine(g.ctx);
        if (quests.Length > 0)
            Label(new Rect(20 * S, 62 * S, 520 * S, 24 * S), quests, stSmall, new Color(0.95f, 0.85f, 0.55f, 0.9f));

        // 交互提示
        if (!string.IsNullOrEmpty(g.interactPrompt) && !g.talking)
            Label(new Rect(0, h * 0.5f + 120 * S, w, 30 * S), g.interactPrompt, stPrompt, new Color(1f, 0.86f, 0.42f, 0.95f));

        // 消息
        float my = h - 120 * S;
        for (int k = g.messages.Count - 1; k >= 0; k--)
        {
            var m = g.messages[k];
            Label(new Rect(w - 700 * S, my, 680 * S, 26 * S), m.text, stMsg, new Color(m.color.r, m.color.g, m.color.b, Mathf.Clamp01(1.6f - m.age * 0.5f)));
            my -= 24 * S;
        }

        // 伤害数字
        var cam = g.cam;
        for (int k = 0; k < g.dmgNums.Count; k++)
        {
            var d = g.dmgNums[k];
            Vector3 sp = cam.WorldToScreenPoint(d.world);
            if (sp.z < 0f) continue;
            Label(new Rect(sp.x - 40, h - sp.y - 20, 80, 26), d.text, stDmg, new Color(d.color.r, d.color.g, d.color.b, Mathf.Clamp01(1.5f - d.age)));
        }

        // 对话面板
        if (g.talking && g.talkRunner != null)
        {
            float pw = Mathf.Min(760 * S, w * 0.72f);
            float ph = 250 * S;
            float px = (w - pw) / 2f;
            float py = h - ph - 80 * S;
            Rect2(new Rect(px, py, pw, ph), new Color(0.03f, 0.05f, 0.08f, 0.88f));
            Rect2(new Rect(px, py, pw, 2f), new Color(0.55f, 0.75f, 0.85f, 0.7f));
            string title = g.talkNpc != null ? (g.talkNpc.def.name + "  ·  " + g.talkNpc.def.title) : "";
            Label(new Rect(px + 18 * S, py + 10 * S, pw - 36 * S, 26 * S), title, stDlgTitle, new Color(0.95f, 0.85f, 0.60f));
            Label(new Rect(px + 18 * S, py + 40 * S, pw - 36 * S, ph - 120 * S), g.talkRunner.Text(), stDlgBody, new Color(0.92f, 0.93f, 0.92f));
            var choices = g.talkRunner.Choices();
            float cy = py + ph - 100 * S;
            for (int k = 0; k < choices.Count && k < 6; k++)
            {
                Label(new Rect(px + 18 * S, cy + k * 22 * S, pw - 36 * S, 22 * S), "<color=#9fd8ff>" + (k + 1) + ".</color> " + choices[k].text, stChoice,
                    new Color(0.82f, 0.88f, 0.92f));
            }
            Label(new Rect(px + 18 * S, py + ph - 26 * S, pw - 36 * S, 20 * S), "按 1-" + Mathf.Min(choices.Count, 6) + " 选择 · Esc 离开", stSmall, new Color(0.6f, 0.68f, 0.72f));
        }

        // 准星
        Rect2(new Rect(w / 2f - 1f, h / 2f - 1f, 2f, 2f), new Color(1f, 1f, 1f, 0.4f));

        // 引擎统计
        if (g.showStats)
        {
            int ii = 0;
            foreach (string line in g.StatsLines())
            {
                Label(new Rect(18 * S, 74 * S + ii * 18 * S, 420 * S, 20 * S), line, stSmall, new Color(0.7f, 0.78f, 0.84f, 0.7f));
                ii++;
            }
        }
    }

    void Bar(Rect r, float frac, Color bg, Color fg)
    {
        Rect2(r, new Color(0f, 0f, 0f, 0.55f));
        frac = Mathf.Clamp01(frac);
        Rect2(new Rect(r.x + 1, r.y + 1, (r.width - 2) * frac, r.height - 2), fg);
    }
}
