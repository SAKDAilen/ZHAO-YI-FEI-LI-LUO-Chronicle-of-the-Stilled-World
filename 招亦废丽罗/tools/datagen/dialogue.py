# -*- coding: utf-8 -*-
"""native/dialogue.py — 人物对话树系统（v3.1）

特性：
  * 分支对话树：节点文本 + 最多 4 个选项，选项可带条件（旗标 / 物品 / 任务阶段 / 击杀数）。
  * 选项效果：设置旗标、给予物品、治疗、给予经验、开启/推进/完成任务、教诗句、开始交易。
  * 多轮记忆：同一 NPC 再次交谈会走不同开场（条件分支）。
  * 多语言：text 可为 str 或 {"zh": ..., "en": ...}；语言为 en 且缺翻译时回退中文。
  * 任务系统：QUEST_DEFS 定义任务与阶段，HUD 可显示当前目标。

数据全部是纯 Python dict，方便扩写；运行时由 DialogueRunner 解释执行。
"""

# ---------------------------------------------------------------- 物品名
ITEM_NAMES = {
    "crystal": ("晶尘", "crystal dust"),
    "part": ("齿轮零件", "gear part"),
    "fragment": ("楔片", "wedge shard"),
    "memory": ("记忆碎片", "memory shard"),
    "relic": ("古遗物", "relic"),
    "ember": ("余烬", "ember"),
}

# ---------------------------------------------------------------- 任务定义
QUEST_DEFS = {
    "q_intro": {
        "name": ("锈钟的低语", "Whisper of the Rusted Bell"),
        "stages": [
            "与钟漏交谈，弄清这个停滞的世界",
            "收集 3 枚晶尘（击碎野生晶簇）",
            "把晶尘带回给钟漏",
        ],
        "reward": "钟漏教你第一句剑诀",
    },
    "q_echo": {
        "name": ("回声的三次矛盾", "Three Contradictions"),
        "stages": [
            "在野外找到回声（会随机出现在营地附近）",
            "倾听它三段互相矛盾的叙述",
            "自己判断：哪一段是真的？",
        ],
        "reward": "记忆碎片 · 前代文明的残片",
    },
    "q_watch": {
        "name": ("守夜人的委托", "The Watchman's Charge"),
        "stages": [
            "替守夜人清理 5 个游荡的构装体",
            "回到营地找守夜人复命",
        ],
        "reward": "守夜人的护符（最大生命 +20）",
    },
    "q_shrines": {
        "name": ("五楔之谜", "Riddle of Five Wedges"),
        "stages": [
            "寻找并触碰散落荒野的五座楔之祭坛",
            "五座祭坛全部共鸣",
        ],
        "reward": "法则之楔 · 凝时（R 键）",
    },
}


def tr(value, lang="zh"):
    """取多语言文本。"""
    if isinstance(value, dict):
        return value.get(lang) or value.get("zh") or next(iter(value.values()))
    return value


def _has_flag(flags, name):
    return bool(flags.get(name))


def check_cond(cond, ctx):
    """条件求值。ctx 至少包含 flags / inventory / quests / kills / deaths。"""
    if not cond:
        return True
    flags = ctx.get("flags", {})
    inv = ctx.get("inventory", {})
    quests = ctx.get("quests", {})
    kills = ctx.get("kills", 0)
    deaths = ctx.get("deaths", 0)
    for key, val in cond.items():
        if key == "flag":
            if bool(flags.get(val)) != True:  # noqa: E712
                return False
        elif key == "no_flag":
            if flags.get(val):
                return False
        elif key == "item":
            need = cond.get("min", 1)
            if inv.get(val, 0) < need:
                return False
        elif key == "no_item":
            if inv.get(val, 0) > 0:
                return False
        elif key == "quest":
            stage = quests.get(val, -1)
            need = cond.get("min", 0)
            if stage < need:
                return False
            if "max" in cond and stage > cond["max"]:
                return False
        elif key == "kills":
            if kills < val:
                return False
        elif key == "deaths":
            if deaths < val:
                return False
        elif key == "min":
            pass
        elif key == "max":
            pass
        else:
            return False
    return True


# ---------------------------------------------------------------- 对话树
# 节点格式：{"text": 文本, "speaker": 可选, "choices": [{"text","next","cond","effect"}],
#            "on_enter": {效果}, "auto": 自动跳转节点}
DIALOGUES = {
    # ------------------------------------------------------------ 钟漏
    "zhonglou": {
        "name": ("钟漏", "Bellglass"),
        "title": ("构装体向导", "Construct Guide"),
        "model": "clockwork",
        "greet": "first",
        "branches": [
            # 首次见面
            {"id": "first", "cond": {"no_flag": "met_zhonglou"},
             "text": "……又醒了。你已经是第……不，我数不清了。\n我是钟漏，一口走不动的钟。你身上的青铜在响，旅人。",
             "on_enter": {"set_flag": "met_zhonglou", "start_quest": "q_intro"}},
            # 带够晶尘
            {"id": "crystal_done", "cond": {"flag": "met_zhonglou", "quest": "q_intro", "min": 2,
                                            "item": "crystal", "min_item": 3},
             "text": "晶尘在掌心发烫……拿去吧。一句剑诀，换你记得我一次。"},
            # 死亡之后
            {"id": "after_death", "cond": {"flag": "met_zhonglou", "deaths": 1},
             "text": "你死过。我看见沙漏里多了一粒沙——那是你忘掉的东西。"},
            # 常态
            {"id": "again", "cond": {"flag": "met_zhonglou"},
             "text": "旅人，又见面了。我腹中的沙还是不落。要我再说说这个世界吗？"},
        ],
        "nodes": {
            "start": {
                "text": "（钟漏腹部的沙漏里，沙粒悬在半空，像被谁按下了暂停。）",
                "choices": [
                    {"text": "这是什么地方？", "next": "world"},
                    {"text": "你为什么走不动？", "next": "clock"},
                    {"text": "五根楔子是什么？", "next": "wedge"},
                    {"text": "（打量它的裂痕）", "next": "crack"},
                ],
            },
            "world": {
                "text": "东方一片青铜林，北方一汪泪湖，西方熔炉塌成峡谷，南方山会回声。\n中间那座峰上，插着五根楔子——世界的规则被钉死了，所以时间也不走了。",
                "choices": [
                    {"text": "那要怎么让时间重新走？", "next": "howto"},
                    {"text": "你说'又醒了'是什么意思？", "next": "loop"},
                    {"text": "（沉默）", "next": "start"},
                ],
            },
            "howto": {
                "text": "拔出楔子。一根一根。\n但每根楔子都有守楔人。他们不是恶人，只是……害怕。",
                "choices": [
                    {"text": "你怕吗？", "next": "fear"},
                    {"text": "我该先拔哪一根？", "next": "order"},
                ],
            },
            "order": {
                "text": "先别急着去。你连剑都握不稳。\n去打碎野外那些晶簇，把晶尘捡回来——我能用它替你磨一磨剑上的锈。",
                "on_enter": {"advance_quest": "q_intro", "min_stage": 1},
                "choices": [
                    {"text": "我去。", "next": None, "effect": {"end": True}},
                    {"text": "还有别的能帮我的吗？", "next": "help"},
                ],
            },
            "help": {
                "text": "南边营地里有守夜人，他缺人手；野外的回声知道得比我多，但它的话……你要自己分辨。\n再见，旅人。别死太多次，我会记不住的。",
                "choices": [{"text": "再见。", "next": None, "effect": {"end": True}}],
            },
            "clock": {
                "text": "我是第一代守楔人做的。他给我装了沙漏，说：只要沙还在流，你就还活着。\n后来他把楔子插进山里，沙就停了。我猜，他也把自己停住了。",
                "choices": [
                    {"text": "第一代守楔人是谁？", "next": "first_gen"},
                    {"text": "我能帮你把沙漏修好吗？", "next": "fix"},
                ],
            },
            "first_gen": {
                "text": "名字被磨掉了。我只记得他说话很轻，像怕吵醒什么。\n……有时候我怀疑，我记着的不是他，是别人塞给我的。",
                "choices": [{"text": "（记下这件事）", "next": "start"}],
            },
            "fix": {
                "text": "修不好。沙漏不是坏的，是世界不肯让它落。\n你若真拔了楔子，也许它自己会动。到那时候，我可能就不认得你了。",
                "choices": [{"text": "……", "next": "start"}],
            },
            "wedge": {
                "text": "重力、时间、能量、共鸣、法则。五根钉子，钉住五条规矩。\n我的沙漏归'时间'那根管；泪湖底的记忆归'记忆'——不，那是第二根。",
                "choices": [
                    {"text": "你刚才说错了？", "next": "slip"},
                    {"text": "守楔人有多强？", "next": "keeper"},
                ],
            },
            "slip": {
                "text": "……我说错了吗？\n（它沉默了很久，沙漏里的沙纹丝不动。）\n别信我。有时候我的话是别人写好的。",
                "on_enter": {"set_flag": "zhonglou_doubt"},
                "choices": [{"text": "我会自己判断。", "next": "start"}],
            },
            "keeper": {
                "text": "枢在青铜林，忆在泪湖，熵在熔炉，谐在山中，律在峰顶。\n前四个还肯说话，最后那个……不谈也罢。",
                "choices": [{"text": "明白了。", "next": "start"}],
            },
            "crack": {
                "text": "（裂缝里能看到细密的齿轮，底下压着一行被反复刻写又磨掉的字：\n'不要让他们再重启一次'）",
                "on_enter": {"set_flag": "saw_crack", "give": {"fragment": 1}},
                "choices": [
                    {"text": "这行字是谁刻的？", "next": "crack2"},
                    {"text": "（收下从裂缝里掉出的楔片）", "next": "start"},
                ],
            },
            "crack2": {
                "text": "我不知道。也许是上一次的我。\n拿好那块楔片——它不属于我，属于'还没发生的事'。",
                "choices": [{"text": "（离开）", "next": None, "effect": {"end": True}}],
            },
            "fear": {
                "text": "怕。我怕沙落尽的那一刻，发现自己从来没有活过。\n可我也怕一直这样停着。旅人，你替我把这个怕带出去吧。",
                "choices": [{"text": "我记住了。", "next": "start"}],
            },
            "loop": {
                "text": "世界重启过。不止一次。\n每次重启，都有一些东西被删掉。我第一次醒来的时候，记得七个人；现在只记得你一个。",
                "choices": [
                    {"text": "谁重启的？", "next": "who"},
                    {"text": "（不想再听）", "next": "start"},
                ],
            },
            "who": {
                "text": "守楔人。他们说：宁可停住，也不要再变坏。\n……可停住的东西，也会慢慢烂掉。你看我。",
                "choices": [{"text": "我会去问他们。", "next": "start"}],
            },
            # 交付任务
            "crystal_done": {
                "text": "（钟漏接过晶尘，用指节把它们碾成细粉，洒在剑上。）\n听好了——'刃起于息，止于息'。剑不是砍出去的，是叹出去的。",
                "on_enter": {"take_item": {"crystal": 3}, "set_flag": "verse_learned",
                             "complete_quest": "q_intro", "max_hp": 10, "teach_verse": "blade_breath"},
                "choices": [
                    {"text": "（试着叹出一剑）", "next": None, "effect": {"end": True}},
                ],
            },
            "after_death": {
                "text": "你死过。我看见沙漏里多了一粒沙——那是你忘掉的东西。\n别怕。忘掉一点，才能继续走。",
                "choices": [{"text": "（点头）", "next": "start"}],
            },
            "again": {
                "text": "旅人，又见面了。沙还是没落。\n要听我再说说这个世界吗？还是……你带了晶尘来？",
                "choices": [
                    {"text": "说说五根楔子。", "next": "wedge"},
                    {"text": "我带了晶尘。", "next": "crystal_done",
                     "cond": {"item": "crystal", "min": 3}},
                    {"text": "只是路过。", "next": None, "effect": {"end": True}},
                ],
            },
        },
    },

    # ------------------------------------------------------------ 回声
    "huisheng": {
        "name": ("回声", "Echo"),
        "title": ("古代灵 · 声音", "Ancient Spirit"),
        "model": "wisp",
        "greet": "first",
        "branches": [
            {"id": "first", "cond": {"no_flag": "met_echo"},
             "text": "（声音从三个方向同时响起，像三个人在说话）\n……你终于来了。不，你早就来过了。"},
            {"id": "again", "cond": {"flag": "met_echo"},
             "text": "（回声比上次近了些，声音却更远）\n你又来了。或者，我还没走。"},
        ],
        "nodes": {
            "start": {
                "text": "我只有声音，没有身体。所以我说的话你只能听，不能摸。\n想听哪一段？",
                "choices": [
                    {"text": "第一段：世界为什么停了。", "next": "tale1"},
                    {"text": "第二段：守楔人的理由。", "next": "tale2"},
                    {"text": "第三段：关于你自己。", "next": "tale3"},
                    {"text": "（安静地听）", "next": "quiet"},
                ],
            },
            "tale1": {
                "text": "版本一：世界停住，是因为守楔人把规则钉死了。是真的。",
                "on_enter": {"set_flag": "echo_1"},
                "choices": [{"text": "那版本二呢？", "next": "tale1b"}],
            },
            "tale1b": {
                "text": "版本二：世界从来没有动过，是你们这些'醒着的人'产生了动的错觉。\n……也是真的。",
                "on_enter": {"set_flag": "echo_1b"},
                "choices": [{"text": "不可能两个都是真的。", "next": "contradiction"}],
            },
            "contradiction": {
                "text": "（笑声从四个方向传来）\n当然可以。记忆被割裂的时候，两边就会各自长成真的。",
                "choices": [{"text": "第三段。", "next": "tale3"}],
            },
            "tale2": {
                "text": "枢怕变化，忆怕遗忘，熵怕耗尽，谐怕失衡，律……律什么都不怕，所以它最像坏掉的东西。",
                "on_enter": {"set_flag": "echo_2"},
                "choices": [
                    {"text": "它们是前代重启的幸存者？", "next": "survivor"},
                    {"text": "第一段是什么来着？", "next": "tale1"},
                ],
            },
            "survivor": {
                "text": "（沉默）\n……你怎么知道这个词。是钟漏说的，还是你自己梦见的？",
                "choices": [{"text": "钟漏的裂缝里有字。", "next": "survivor2"}],
            },
            "survivor2": {
                "text": "那就说得通了。有人不希望你想起来。\n（声音散开）我这具'身体'，其实就是那个人的记忆残渣。",
                "on_enter": {"set_flag": "echo_truth", "give": {"memory": 1}},
                "choices": [{"text": "（收下记忆碎片）", "next": "start"}],
            },
            "tale3": {
                "text": "你身上有青铜的响动，脚下没有影子——这是'还没被写进去'的人的样子。",
                "on_enter": {"set_flag": "echo_3"},
                "choices": [
                    {"text": "你是说我不属于这个世界？", "next": "notbelong"},
                    {"text": "（低头看自己的影子）", "next": "shadow"},
                ],
            },
            "notbelong": {
                "text": "你属于，也不属于。每次重启都需要一个'外人'来拔楔——上一次的那个，现在是我的声音。",
                "choices": [{"text": "那这一次呢？", "next": "this_time"}],
            },
            "this_time": {
                "text": "这一次，轮到你决定：拔掉所有楔子，还是……留一根，让世界继续停着。\n（声音忽然很近）别急。你还有很多天可以想。",
                "on_enter": {"set_flag": "knows_choice", "complete_quest": "q_echo"},
                "choices": [{"text": "（记住这句话）", "next": None, "effect": {"end": True}}],
            },
            "shadow": {
                "text": "影子是有的，只是淡。等你被这个世界记住，它就会变黑。\n……被记住，不一定是好事。",
                "choices": [{"text": "（退后一步）", "next": "start"}],
            },
            "quiet": {
                "text": "（安静。风停了，草不动，连你的呼吸也变成了回声的一部分。）\n好。愿意听的人，我多说一点。",
                "on_enter": {"heal": 15, "stamina": 100},
                "choices": [{"text": "（睁眼）", "next": "start"}],
            },
        },
    },

    # ------------------------------------------------------------ 守夜人
    "shouye": {
        "name": ("砌", "Qi"),
        "title": ("守夜人", "Night Watchman"),
        "model": "watcher",
        "greet": "first",
        "branches": [
            {"id": "first", "cond": {"no_flag": "met_watch"},
             "text": "站住。……哦，是会动的。难得。\n我是砌，就在这道火边守着。你要么帮我个忙，要么走远点。"},
            {"id": "done", "cond": {"flag": "met_watch", "quest": "q_watch", "min": 1, "kills": 5},
             "text": "你身上有铁锈味，也有血味。干得不错。"},
            {"id": "again", "cond": {"flag": "met_watch"},
             "text": "还没清完？那些会走路的破铜烂铁，见一个拆一个。"},
        ],
        "nodes": {
            "start": {
                "text": "这一带夜里不安全。构装体在游荡，像是被什么吸引了。\n你要是能清掉五只，我把护符给你。",
                "on_enter": {"start_quest": "q_watch", "set_flag": "met_watch"},
                "choices": [
                    {"text": "接了。", "next": "accept"},
                    {"text": "护符有什么用？", "next": "charm"},
                    {"text": "你为什么守在这里？", "next": "why"},
                ],
            },
            "accept": {
                "text": "好。别逞强。死了……死了也还在原地，这就是最烦的地方。",
                "choices": [{"text": "（出发）", "next": None, "effect": {"end": True}}],
            },
            "charm": {
                "text": "让血厚一点。就一点。\n这世道，多一点就是多一条命。",
                "choices": [{"text": "成交。", "next": "accept"}],
            },
            "why": {
                "text": "火需要人看着，不然会灭。世界也一样——总得有人守着不让它彻底冷掉。\n我守了多久？不知道。我数到四千多天就不数了。",
                "choices": [{"text": "（沉默）", "next": "start"}],
            },
            "charm_give": {
                "text": "（他把护符按进你掌心，护符很烫）\n拿着。别谢我，谢那些没撑下来的。",
                "on_enter": {"complete_quest": "q_watch", "max_hp": 20, "give": {"ember": 2},
                             "set_flag": "watch_done"},
                "choices": [{"text": "（收下）", "next": None, "effect": {"end": True}}],
            },
        },
    },

    # ------------------------------------------------------------ 行商·陶
    "tao": {
        "name": ("陶", "Tao"),
        "title": ("行商 · 拾荒者", "Wandering Peddler"),
        "model": "merchant",
        "greet": "first",
        "branches": [
            {"id": "first", "cond": {"no_flag": "met_tao"},
             "text": "来啦？别踩我的货。你看这满地的破铜烂铁，在我眼里都是钱。\n晶尘、楔片、记忆碎片——都收。"},
            {"id": "again", "cond": {"flag": "met_tao"},
             "text": "又见面了。今天带钱（晶尘）了吗？"},
        ],
        "nodes": {
            "start": {
                "text": "我这儿有三样好东西，看你出得起什么。",
                "on_enter": {"set_flag": "met_tao"},
                "choices": [
                    {"text": "3 晶尘 → 换 1 份余烬（恢复体力）", "next": "buy_ember",
                     "cond": {"item": "crystal", "min": 3}},
                    {"text": "5 晶尘 → 换 1 枚楔片", "next": "buy_fragment",
                     "cond": {"item": "crystal", "min": 5}},
                    {"text": "2 楔片 → 磨亮剑刃（攻击 +4）", "next": "sharpen",
                     "cond": {"item": "fragment", "min": 2}},
                    {"text": "只是看看。", "next": None, "effect": {"end": True}},
                ],
            },
            "buy_ember": {
                "text": "（把一小罐余烬推给你）拿好，别洒了。烫。",
                "on_enter": {"take_item": {"crystal": 3}, "give": {"ember": 1}, "heal": 8},
                "choices": [{"text": "再看看别的。", "next": "start"}],
            },
            "buy_fragment": {
                "text": "楔片？你从哪儿弄来的……算了，生意不问来路。",
                "on_enter": {"take_item": {"crystal": 5}, "give": {"fragment": 1}},
                "choices": [{"text": "（收好）", "next": "start"}],
            },
            "sharpen": {
                "text": "（他拿楔片在剑脊上刮了几下，火星四溅）\n好剑。可惜握剑的手还在抖。",
                "on_enter": {"take_item": {"fragment": 2}, "damage_bonus": 4},
                "choices": [{"text": "（活动了一下手腕）", "next": "start"}],
            },
        },
    },
}


# ---------------------------------------------------------------- 运行时
class DialogueRunner:
    """一次对话的运行时状态机。"""

    def __init__(self, npc_key, ctx, lang="zh"):
        self.npc_key = npc_key
        self.data = DIALOGUES[npc_key]
        self.ctx = ctx
        self.lang = lang
        self.node_id = self._pick_branch()
        self.node = self._resolve(self.node_id)
        self.done = False
        self.shop = False
        self.last_fx = {}
        self._enter(self.node)

    # -- 内部
    def _pick_branch(self):
        for b in self.data.get("branches", []):
            if check_cond(b.get("cond"), self.ctx):
                return b["id"]
        return "start"

    def _resolve(self, node_id):
        if not node_id:
            return None
        nodes = self.data["nodes"]
        if node_id in nodes:
            return dict(nodes[node_id], id=node_id)
        if node_id == "crystal_done" and "crystal_done" in nodes:
            return dict(nodes[node_id], id=node_id)
        # 分支 id 直接当节点用（若存在同名节点）
        return dict(nodes.get("start", {"text": "……"}), id="start")

    def _enter(self, node):
        if node and node.get("on_enter"):
            self.last_fx = apply_effects(node["on_enter"], self.ctx, self)

    # -- 公开
    def text(self):
        if not self.node:
            return ""
        pre = ""
        if "speaker" in self.node:
            pre = tr(self.node["speaker"], self.lang) + "："
        return pre + tr(self.node.get("text", ""), self.lang).replace("{name}", "")

    def choices(self):
        out = []
        for ch in (self.node or {}).get("choices", []):
            if check_cond(ch.get("cond"), self.ctx):
                out.append(ch)
        if not out:
            out = [{"text": "（离开）", "next": None, "effect": {"end": True}}]
        return out

    def select(self, i):
        """选择第 i 项，返回 True 表示对话继续。"""
        chs = self.choices()
        if not chs or i < 0 or i >= len(chs):
            return False
        ch = chs[i]
        self.last_fx = {}
        if ch.get("effect"):
            self.last_fx = apply_effects(ch["effect"], self.ctx, self)
        if self.done:
            return False
        nxt = ch.get("next")
        if nxt is None:
            self.done = True
            return False
        self.node_id = nxt
        self.node = self._resolve(nxt)
        self._enter(self.node)
        return True


def apply_effects(fx, ctx, runner=None):
    """执行效果字典，返回本次产生的可展示事件（物品/数值变化）。"""
    events = {}
    flags = ctx.setdefault("flags", {})
    inv = ctx.setdefault("inventory", {})
    quests = ctx.setdefault("quests", {})
    if "set_flag" in fx:
        flags[fx["set_flag"]] = True
    if "clear_flag" in fx:
        flags.pop(fx["clear_flag"], None)
    if "give" in fx:
        got = {}
        for k, n in fx["give"].items():
            inv[k] = inv.get(k, 0) + int(n)
            got[k] = got.get(k, 0) + int(n)
        events["give"] = got
    if "take_item" in fx:
        for k, n in fx["take_item"].items():
            inv[k] = max(0, inv.get(k, 0) - int(n))
    if "heal" in fx:
        events["heal"] = int(fx["heal"])
    if "stamina" in fx:
        events["stamina"] = fx["stamina"]
    if "max_hp" in fx:
        events["max_hp"] = int(fx["max_hp"])
        flags["bonus_hp"] = int(flags.get("bonus_hp", 0)) + int(fx["max_hp"])
    if "damage_bonus" in fx:
        events["damage_bonus"] = int(fx["damage_bonus"])
        flags["damage_bonus"] = int(flags.get("damage_bonus", 0)) + int(fx["damage_bonus"])
    if "teach_verse" in fx:
        events["verse"] = fx["teach_verse"]
        flags.setdefault("verses", []).append(fx["teach_verse"])
    if "start_quest" in fx and fx["start_quest"] not in quests:
        quests[fx["start_quest"]] = 0
        events["quest_start"] = fx["start_quest"]
    if "advance_quest" in fx:
        q = fx["advance_quest"]
        cur = quests.get(q, -1)
        if cur >= 0:
            target = max(cur + 1, int(fx.get("min_stage", cur + 1)))
            quests[q] = target
            events["quest_advance"] = q
    if "complete_quest" in fx:
        q = fx["complete_quest"]
        defs = QUEST_DEFS.get(q)
        stages = len(defs["stages"]) if defs else 1
        quests[q] = stages - 1
        flags["done_" + q] = True
        events["quest_done"] = q
    if "end" in fx:
        if runner is not None:
            runner.done = True
    if "open_shop" in fx:
        events["shop"] = True
    return events


def quest_lines(ctx, lang="zh"):
    """HUD 显示：进行中的任务与当前目标。"""
    out = []
    quests = ctx.get("quests", {})
    for qid, stage in quests.items():
        d = QUEST_DEFS.get(qid)
        if not d:
            continue
        if ctx.get("flags", {}).get("done_" + qid):
            continue
        stages = d["stages"]
        idx = max(0, min(stage, len(stages) - 1))
        out.append((tr(d["name"], lang), stages[idx]))
    return out
