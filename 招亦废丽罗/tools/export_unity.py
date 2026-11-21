# -*- coding: utf-8 -*-
"""tools/export_unity.py —— 把共享数据（角色动画曲线 / 对话树 / 世界文本）
导出为 Unity 工程可直接读取的 JSON（StreamingAssets）。

数据来源（全部自包含在 tools/datagen/，不依赖任何历史版本）：
  tools/datagen/charrig.py  -> charrig.json   （步态 / 剑击 / 一次性姿态 / 相机 / 披风）
  tools/datagen/dialogue.py -> dialogue.json  （四 NPC 对话树 + 任务 + 物品）
  tools/datagen/lore.py     -> lore.json      （碑文、五坛数据、出生点）

用法： python tools/export_unity.py
输出： UnityEdition/Assets/StreamingAssets/{charrig,dialogue,lore}.json
"""
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
OUT = os.path.join(REPO, "UnityEdition", "Assets", "StreamingAssets")

sys.path.insert(0, os.path.join(HERE, "datagen"))

import charrig  # noqa: E402

# ---------------------------------------------------------------- 角色曲线
data = charrig.DATA
charrig_out = {
    "version": data.get("version", "v2.4"),
    "gait": data["gait"],
    "swing": data["swing"],
    "oneshot": data["oneshot"],
    "idle2": data["idle2"],
    "cam": data["cam"],
    "cape": data["cape"],
}

# ---------------------------------------------------------------- 对话树
import dialogue as dlg  # noqa: E402


def pick_pair(val):
    """(zh, en) 二元组 → 中文。"""
    if isinstance(val, (list, tuple)) and len(val) == 2:
        return val[0]
    return val


npcs = {}
for key, node in dlg.DIALOGUES.items():
    npcs[key] = {
        "name": pick_pair(node.get("name")),
        "title": pick_pair(node.get("title")),
        "model": node.get("model", "clockwork"),
        "greet": node.get("greet", ""),
        "branches": node.get("branches", []),
        "nodes": node.get("nodes", {}),
    }

quests = {}
for qid, q in getattr(dlg, "QUEST_DEFS", {}).items():
    quests[qid] = {
        "name": pick_pair(q.get("name", qid)) if isinstance(q, dict) else str(q),
        "stages": q.get("stages", []) if isinstance(q, dict) else [],
    }

dialogue_out = {
    "npcs": npcs,
    "quests": quests,
    "items": dlg.ITEM_NAMES,
}

# ---------------------------------------------------------------- 文本 / 五坛
import lore as gamelore  # noqa: E402

lore_out = {
    "stele": gamelore.LORE_TEXTS,
    "shrines": [
        {"id": sid, "name": name, "desc": desc, "bonus": bonus}
        for (sid, name, desc, bonus) in gamelore.SHRINE_DATA
    ],
    "spawn": list(gamelore.SPAWN),
}

# ---------------------------------------------------------------- 写出
os.makedirs(OUT, exist_ok=True)
files = {
    "charrig.json": charrig_out,
    "dialogue.json": dialogue_out,
    "lore.json": lore_out,
}
for name, payload in files.items():
    path = os.path.join(OUT, name)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False, separators=(",", ":"))
    print("wrote %s (%d bytes)" % (path, os.path.getsize(path)))
print("done")
