# -*- coding: utf-8 -*-
"""C# 成员一致性检查（按类块提取 public 成员，按文件作用域校验访问）。"""
import os
import re

BASE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(BASE, "..", "..", "UnityEdition", "Assets", "Scripts"))

CHECKS = {
    'GameManager': ('GameManager.cs', {
        'Hud.cs': ['g.'],
        'Player.cs': ['g.'],
        'Enemies.cs': ['g.'],
    }),
    'PlayerController': ('Player.cs', {'GameManager.cs': ['player.'], 'CameraRig.cs': ['p.']}),
    'CameraRig': ('CameraRig.cs', {'GameManager.cs': ['camRig.']}),
    'TerrainStreamer': ('Terrain.cs', {'GameManager.cs': ['terrain.']}),
    'Population': ('Interactables.cs', {'GameManager.cs': ['pop.']}),
    'EnemyManager': ('Enemies.cs', {'GameManager.cs': ['enemies.']}),
    'Env': ('Env.cs', {'GameManager.cs': ['env.'], 'Hud.cs': ['g.env.']}),
    'Hud': ('Hud.cs', {'GameManager.cs': ['hud.']}),
    'DialogueRunner': ('Dialogue.cs', {'GameManager.cs': ['talkRunner.'], 'Hud.cs': ['g.talkRunner.']}),
    'DialogueContext': ('Dialogue.cs', {'GameManager.cs': ['ctx.']}),
    'EnemyRig': ('Character.cs', {'Enemies.cs': ['e.'], 'GameManager.cs': ['e.']}),
    'InteractObj': ('Interactables.cs', {'GameManager.cs': ['o.']}),
    'NpcUnit': ('Interactables.cs', {'GameManager.cs': ['npc.']}),
    'Rig': ('Character.cs', {'GameManager.cs': ['player.rig.'], 'Player.cs': ['rig.']}),
    'GameData': ('Data.cs', {'GameManager.cs': ['GameData.'], 'Terrain.cs': ['GameData.'],
                             'Interactables.cs': ['GameData.'], 'Player.cs': ['GameData.']}),
    'Mats': ('Geometry.cs', {'Interactables.cs': ['Mats.'], 'Terrain.cs': ['Mats.'], 'GameManager.cs': ['Mats.']}),
    'Geo': ('Geometry.cs', {'Interactables.cs': ['Geo.'], 'Terrain.cs': ['Geo.']}),
    'Make': ('Geometry.cs', {'Interactables.cs': ['Make.'], 'Terrain.cs': ['Make.'],
                             'GameManager.cs': ['Make.'], 'Character.cs': ['Make.']}),
    'MiniJson': ('Data.cs', {'GameManager.cs': ['MiniJson.'], 'Dialogue.cs': ['MiniJson.']}),
    'Part': ('Geometry.cs', {'Interactables.cs': ['o.glows', 'p.renderer']}),
}

DECL_LINE = re.compile(
    r"^\s*public\s+(?:static\s+|readonly\s+|const\s+|extern\s+|unsafe\s+|partial\s+)*"
    r"(?:[\w<>\[\],\.\?]+)\s+(.+)$")


def split_top(s):
    parts, buf, depth = [], '', 0
    for ch in s:
        if ch in '(<[{':
            depth += 1
        elif ch in ')>]}':
            depth -= 1
        if ch == ',' and depth == 0:
            parts.append(buf)
            buf = ''
        else:
            buf += ch
    parts.append(buf)
    return parts


def members_of(block):
    names = set()
    for line in block.split('\n'):
        line = line.strip()
        if not line.startswith('public'):
            continue
        rest = re.sub(r"^public\s+(?:(?:static|readonly|const|extern|unsafe|partial|override|virtual|abstract)\s+)*", "", line)
        par = rest.find('(')
        eq = rest.find('=')
        if par >= 0 and (eq < 0 or par < eq):
            nm = re.match(r"[A-Za-z_][\w<>\[\],\.\? ]*?\s+([A-Za-z_]\w*)\s*\(", rest)
            if nm:
                names.add(nm.group(1))
            continue
        br = rest.find('{')
        if br >= 0:
            ids = re.findall(r"[A-Za-z_]\w*", rest[:br])
            if ids:
                names.add(ids[-1])
            continue
        body = rest.split(';')[0]
        for seg in split_top(body):
            pre = seg.split('=')[0]
            ids = re.findall(r"[A-Za-z_]\w*", pre)
            if ids:
                names.add(ids[-1])
    return names
    return names


def class_blocks(text):
    """返回 {类名: 类体内文本}"""
    out = {}
    for m in re.finditer(r"(?:public\s+|internal\s+)?(?:static\s+|sealed\s+|abstract\s+)*class\s+(\w+)", text):
        name = m.group(1)
        i = text.find('{', m.end())
        if i < 0:
            continue
        depth = 0
        j = i
        while j < len(text):
            if text[j] == '{':
                depth += 1
            elif text[j] == '}':
                depth -= 1
                if depth == 0:
                    break
            j += 1
        out.setdefault(name, text[i:j + 1])
    return out


all_blocks = {}
for nm in os.listdir(ROOT):
    if nm.endswith('.cs'):
        for k, v in class_blocks(open(os.path.join(ROOT, nm), encoding='utf-8').read()).items():
            all_blocks.setdefault(k, '')
            all_blocks[k] += v

problems = []
for cls, (decl_file, uses) in CHECKS.items():
    block = all_blocks.get(cls)
    if not block:
        problems.append(f"[缺失类] {cls} 未在 {decl_file} 中声明")
        continue
    decls = members_of(block)
    decls |= {'Count', 'Length'}
    for fname, prefixes in uses.items():
        fpath = os.path.join(ROOT, fname)
        if not os.path.exists(fpath):
            problems.append(f"[缺失文件] {fname}")
            continue
        text = open(fpath, encoding='utf-8').read()
        for prefix in prefixes:
            pat = r"(?<![A-Za-z0-9_])" + re.escape(prefix) + r"([A-Za-z_]\w*)"
            for m in re.finditer(pat, text):
                name = m.group(1)
                if name in decls:
                    continue
                problems.append(f"{fname}: {prefix}{name}  (未在 {cls} 中找到)")

for p in problems:
    print(p)
print(f"\n{len(problems)} potential issues")
