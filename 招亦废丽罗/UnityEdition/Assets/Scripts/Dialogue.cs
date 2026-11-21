// Dialogue.cs —— 对话树运行器（条件判定 / 效果应用），数据来自 dialogue.json
using System.Collections.Generic;
using UnityEngine;

public class DialogueContext
{
    public Dictionary<string, bool> flags = new Dictionary<string, bool>();
    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    public Dictionary<string, int> quests = new Dictionary<string, int>();
    public int kills, deaths;

    public bool Flag(string k)
    {
        bool v;
        return flags.TryGetValue(k, out v) && v;
    }

    public int Item(string k)
    {
        int v;
        return inventory.TryGetValue(k, out v) ? v : 0;
    }

    public int Quest(string k)
    {
        int v;
        return quests.TryGetValue(k, out v) ? v : 0;
    }
}

public class DialogueRunner
{
    public NpcDef npc;
    public DialogueContext ctx;
    public NodeDef node;
    public bool shopping;
    public string lastFx = "";

    public DialogueRunner(NpcDef def, DialogueContext c)
    {
        npc = def;
        ctx = c;
        string startId;
        BranchDef branch = PickBranch(out startId);
        string nodeId = startId;
        if (branch == null && npc.branches.Count > 0) nodeId = npc.branches[0].id;
        Enter(nodeId);
        if (branch != null) ApplyEffects(branch.onEnter);
    }

    BranchDef PickBranch(out string id)
    {
        id = npc.greet;
        for (int k = 0; k < npc.branches.Count; k++)
        {
            var b = npc.branches[k];
            if (CheckCond(b.cond))
            {
                id = b.id;
                return b;
            }
        }
        return null;
    }

    public void Enter(string nodeId)
    {
        if (nodeId == null) return;
        NodeDef n;
        if (!npc.nodes.TryGetValue(nodeId, out n)) return;
        node = n;
        ApplyEffects(n.onEnter);
    }

    bool CheckCond(Dictionary<string, object> cond)
    {
        if (cond == null || cond.Count == 0) return true;
        foreach (var kv in cond)
        {
            string key = kv.Key;
            object v = kv.Value;
            switch (key)
            {
                case "flag": if (!ctx.Flag(MiniJson.Str(v))) return false; break;
                case "no_flag": if (ctx.Flag(MiniJson.Str(v))) return false; break;
                case "item": if (ctx.Item(MiniJson.Str(v)) < 1) return false; break;
                case "min_item": break; // 与 item 搭配，由下面统一处理
                case "no_item": if (ctx.Item(MiniJson.Str(v)) > 0) return false; break;
                case "quest": if (ctx.Quest(MiniJson.Str(v)) < 1) return false; break;
                case "min": break;
                case "max": break;
                case "kills": if (ctx.kills < MiniJson.I(v)) return false; break;
                case "deaths": if (ctx.deaths < MiniJson.I(v)) return false; break;
            }
        }
        // item + min_item
        if (cond.ContainsKey("item"))
        {
            int need = cond.ContainsKey("min_item") ? MiniJson.I(cond["min_item"], 1) : 1;
            if (ctx.Item(MiniJson.Str(cond["item"])) < need) return false;
        }
        if (cond.ContainsKey("quest"))
        {
            int need = cond.ContainsKey("min") ? MiniJson.I(cond["min"], 1) : 1;
            int mx = cond.ContainsKey("max") ? MiniJson.I(cond["max"], 9999) : 9999;
            int st = ctx.Quest(MiniJson.Str(cond["quest"]));
            if (st < need || st > mx) return false;
        }
        return true;
    }

    public string Text()
    {
        return node == null ? "" : node.text;
    }

    public List<ChoiceDef> Choices()
    {
        var outList = new List<ChoiceDef>();
        if (node == null) return outList;
        for (int k = 0; k < node.choices.Count; k++)
        {
            var c = node.choices[k];
            if (c.effect != null && c.effect.ContainsKey("cond_skip")) continue;
            outList.Add(c);
        }
        if (outList.Count == 0)
        {
            var leave = new ChoiceDef();
            leave.text = "离开";
            leave.next = null;
            leave.effect = new Dictionary<string, object>();
            leave.effect["end"] = true;
            outList.Add(leave);
        }
        return outList;
    }

    /// 选择：返回 true 表示对话继续，false 表示结束
    public bool Select(int index)
    {
        var list = Choices();
        if (index < 0 || index >= list.Count) return false;
        var c = list[index];
        ApplyEffects(c.effect);
        if (c.next == null || (c.effect != null && c.effect.ContainsKey("end"))) return false;
        Enter(c.next);
        return true;
    }

    public void ApplyEffects(Dictionary<string, object> fx)
    {
        if (fx == null) return;
        foreach (var kv in fx)
        {
            string key = kv.Key;
            object v = kv.Value;
            switch (key)
            {
                case "set_flag": ctx.flags[MiniJson.Str(v)] = true; break;
                case "clear_flag": ctx.flags[MiniJson.Str(v)] = false; break;
                case "give":
                    foreach (var item in MiniJson.Dict(v))
                        ctx.inventory[item.Key] = ctx.Item(item.Key) + MiniJson.I(item.Value, 1);
                    lastFx = "获得物品";
                    break;
                case "take_item":
                    foreach (var item in MiniJson.Dict(v))
                        ctx.inventory[item.Key] = Mathf.Max(0, ctx.Item(item.Key) - MiniJson.I(item.Value, 1));
                    break;
                case "heal":
                    if (GameManager.I != null) GameManager.I.HealPlayer(MiniJson.F(v));
                    break;
                case "stamina":
                    if (GameManager.I != null) GameManager.I.RestoreStamina(MiniJson.F(v));
                    break;
                case "max_hp":
                    if (GameManager.I != null) GameManager.I.AddMaxHp(MiniJson.I(v));
                    break;
                case "damage_bonus":
                    if (GameManager.I != null) GameManager.I.AddDamageBonus(MiniJson.I(v));
                    break;
                case "teach_verse":
                    if (GameManager.I != null) GameManager.I.TeachVerse(MiniJson.Str(v));
                    break;
                case "start_quest":
                    ctx.quests[MiniJson.Str(v)] = Mathf.Max(1, ctx.Quest(MiniJson.Str(v)));
                    break;
                case "advance_quest":
                    {
                        string q = MiniJson.Str(v);
                        int stage = fx.ContainsKey("min_stage") ? MiniJson.I(fx["min_stage"], 1) : 1;
                        ctx.quests[q] = Mathf.Max(ctx.Quest(q), stage);
                    }
                    break;
                case "complete_quest":
                    ctx.quests[MiniJson.Str(v)] = 99;
                    break;
                case "open_shop":
                    shopping = true;
                    break;
            }
        }
    }

    public static string QuestLine(DialogueContext ctx)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kv in ctx.quests)
        {
            if (kv.Value >= 99) continue;
            string name;
            string[] stages;
            name = GameData.Quests.ContainsKey(kv.Key) ? kv.Key : kv.Key;
            if (GameData.Quests.TryGetValue(kv.Key, out stages) && stages.Length > 0)
                name = stages[Mathf.Clamp(kv.Value - 1, 0, stages.Length - 1)];
            sb.Append("◆ ").Append(name).Append("   ");
        }
        return sb.ToString();
    }
}
