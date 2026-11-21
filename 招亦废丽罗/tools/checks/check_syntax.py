# -*- coding: utf-8 -*-
"""粗略 C# 结构检查：括号配对、字符串/注释剥离、语句级明显问题。"""
import os
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "UnityEdition", "Assets")
ROOT = os.path.abspath(ROOT)


def strip_code(text):
    out = []
    i, n = 0, len(text)
    state = None  # None | 'line' | 'block' | 'str' | 'char' | 'verbatim'
    while i < n:
        c = text[i]
        if state is None:
            if c == '/' and i + 1 < n and text[i + 1] == '/':
                state = 'line'; i += 2; continue
            if c == '/' and i + 1 < n and text[i + 1] == '*':
                state = 'block'; i += 2; continue
            if c == '"':
                if i > 0 and text[i - 1] == '@':
                    state = 'verbatim'
                else:
                    state = 'str'
                out.append('"'); i += 1; continue
            if c == "'":
                state = 'char'; out.append("'"); i += 1; continue
            out.append(c); i += 1; continue
        if state == 'line':
            if c == '\n':
                state = None; out.append('\n')
            i += 1; continue
        if state == 'block':
            if c == '*' and i + 1 < n and text[i + 1] == '/':
                state = None; i += 2; continue
            i += 1; continue
        if state == 'str':
            if c == '\\':
                i += 2; continue
            if c == '"':
                state = None
            i += 1; continue
        if state == 'verbatim':
            if c == '"' and i + 1 < n and text[i + 1] == '"':
                i += 2; continue
            if c == '"':
                state = None
            i += 1; continue
        if state == 'char':
            if c == '\\':
                i += 2; continue
            if c == "'":
                state = None
            i += 1; continue
    return ''.join(out)


def check(path):
    text = open(path, encoding='utf-8').read()
    code = strip_code(text)
    stack = []
    pairs = {')': '(', ']': '[', '}': '{'}
    line = 1
    for ch in code:
        if ch == '\n':
            line += 1
        elif ch in '([{':
            stack.append((ch, line))
        elif ch in ')]}':
            if not stack or stack[-1][0] != pairs[ch]:
                return f"unbalanced '{ch}' at line {line}"
            stack.pop()
    if stack:
        return f"unclosed '{stack[-1][0]}' opened at line {stack[-1][1]}"
    return None


bad = 0
files = []
for base, _, names in os.walk(ROOT):
    for nm in names:
        if nm.endswith('.cs') or nm.endswith('.shader'):
            files.append(os.path.join(base, nm))
for f in sorted(files):
    err = check(f)
    rel = os.path.relpath(f, os.path.dirname(ROOT))
    if err:
        bad += 1
        print(f"FAIL {rel}: {err}")
    else:
        print(f"OK   {rel}")
print(f"\n{len(files)} files, {bad} problems")
sys.exit(1 if bad else 0)
