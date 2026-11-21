# -*- coding: utf-8 -*-
"""招亦废丽罗 · 角色动画与相机数据生成器（v2.4）

以纯 Python（无第三方依赖）确定性生成青铜旅人的：
  1. 步态曲线 —— 空闲/行走/奔跑/冲刺/后退/侧移/游泳 七种循环，每通道 32 采样点；
  2. 三段剑击弧线 —— Catmull-Rom 关键帧插值，每通道 24 采样点；
  3. 一次性姿态 —— 跳跃（蹬地→收腿）/ 落地吸收 / 受击退缩 / 原地转向；
  4. 空闲微动作覆盖层 —— 64 点慢循环（深慢呼吸/重心轮换/头部游移/手部小动作）；
  5. 相机剖面 —— 模式距离/FOV、俯仰补偿曲线、平滑参数、肩部偏移；
  6. 披风布料参数 —— verlet 网格与物理常数。

生成物 js/charrig_data.js 为 @generated 文件，勿手改；改曲线请改本文件后重跑：
    python tools/charrig.py
"""
import json
import math
import os

N_GAIT = 32      # 步态循环采样点数
N_SWING = 24     # 剑击弧线采样点数
N_ONE = 24       # 一次性姿态采样点数（受击退缩 16 点）
N_IDLE2 = 64     # 空闲微动作覆盖层采样点数
N_PITCH = 19     # 相机俯仰补偿曲线采样点数
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "js", "charrig_data.js")


def r4(x):
    return round(float(x), 4)


# ---------------------------------------------------------------- 步态曲线
def gait(name, hip, arm, knee0, knee1, lift, bob, lean, sway, roll, shift,
         arm0, arm1, bob_style="walk", lift_pow=0.85):
    """由参数化谐波模型合成一个完整步态循环（相位 0..2π）。

    hip: 髋摆幅(rad)；arm: 臂摆幅；knee0/knee1: 膝屈基准/摆幅；
    lift: 足抬升幅度；bob: 骨盆垂直浮动(m)；lean: 躯干前倾；
    sway: 肩部反扭；roll: 躯干侧倾；shift: 骨盆侧移；
    arm0/arm1: 肘屈基准/摆幅。
    """
    d = {}
    for i in range(N_GAIT):
        ph = 2.0 * math.pi * i / N_GAIT
        # 摆动腿相位：腿在身体后方时抬脚屈膝
        back = max(0.0, -math.sin(ph)) ** lift_pow
        stance = max(0.0, math.sin(ph)) ** 0.9
        d.setdefault("hipL", []).append(r4(hip * math.sin(ph)))
        d.setdefault("hipR", []).append(r4(hip * math.sin(ph + math.pi)))
        d.setdefault("kneeL", []).append(r4(knee0 + knee1 * back))
        d.setdefault("kneeR", []).append(r4(knee0 + knee1 * max(0.0, math.sin(ph)) ** lift_pow))
        d.setdefault("footL", []).append(r4(lift * back))
        d.setdefault("footR", []).append(r4(lift * max(0.0, math.sin(ph)) ** lift_pow))
        d.setdefault("ankleL", []).append(r4(0.28 * back - 0.12 * stance))
        d.setdefault("ankleR", []).append(r4(0.28 * max(0.0, math.sin(ph)) ** lift_pow - 0.12 * max(0.0, -math.sin(ph)) ** 0.9))
        # 手臂与对侧腿反相
        d.setdefault("armL", []).append(r4(arm * math.sin(ph + math.pi)))
        d.setdefault("armR", []).append(r4(arm * math.sin(ph)))
        d.setdefault("elbowL", []).append(r4(arm0 + arm1 * max(0.0, math.sin(ph)) ** 1.1))
        d.setdefault("elbowR", []).append(r4(arm0 + arm1 * max(0.0, math.sin(ph + math.pi)) ** 1.1))
        # 躯干
        if bob_style == "run":
            b = bob * math.sin(ph + math.pi / 2.0)   # 跑：腾空抬升
        else:
            b = bob * (1.0 - abs(math.sin(ph)))       # 走：双支撑下压
        d.setdefault("bob", []).append(r4(b))
        d.setdefault("lean", []).append(r4(lean + 0.03 * math.sin(2 * ph)))
        d.setdefault("sway", []).append(r4(sway * math.sin(ph + math.pi / 2.0)))
        d.setdefault("roll", []).append(r4(roll * math.sin(ph)))
        d.setdefault("shift", []).append(r4(shift * math.sin(2 * ph)))
    return {"name": name, "n": N_GAIT, "channels": d}


GAITS = {
    "idle":   gait("idle", 0.0, 0.035, 0.10, 0.0, 0.0, 0.005, 0.055, 0.012, 0.008, 0.0, 0.42, 0.0, bob_style="walk"),
    "walk":   gait("walk", 0.50, 0.62, 0.12, 1.00, 1.0, 0.034, 0.10, 0.10, 0.045, 0.020, 0.50, 0.55),
    "run":    gait("run", 0.88, 1.05, 0.26, 1.42, 1.0, 0.052, 0.20, 0.15, 0.060, 0.028, 1.10, 0.60, bob_style="run"),
    "sprint": gait("sprint", 1.18, 1.30, 0.40, 1.80, 1.0, 0.062, 0.34, 0.19, 0.080, 0.038, 1.45, 0.65, bob_style="run"),
    # v2.4 方向步态：后退（小幅碎步 + 后倾）与侧移（重心侧换 + 交叉步）
    "back":   gait("back", 0.42, 0.50, 0.25, 0.85, 0.8, 0.026, -0.10, 0.08, 0.040, 0.018, 0.55, 0.50, bob_style="walk"),
    "strafe": gait("strafe", 0.35, 0.42, 0.20, 0.70, 0.7, 0.024, 0.02, 0.12, 0.060, 0.022, 0.50, 0.45, bob_style="walk"),
}


def swim_gait():
    """自由泳循环：双臂交替大划水 + 双腿小幅打水 + 滚肩 + 身体起伏。"""
    d = {}
    for i in range(N_GAIT):
        ph = 2.0 * math.pi * i / N_GAIT
        s = math.sin(ph)
        k = max(0.0, s) ** 0.9
        ko = max(0.0, -s) ** 0.9
        d.setdefault("hipL", []).append(r4(0.35 * s))
        d.setdefault("hipR", []).append(r4(-0.35 * s))
        d.setdefault("kneeL", []).append(r4(0.35 + 0.45 * k))
        d.setdefault("kneeR", []).append(r4(0.35 + 0.45 * ko))
        d.setdefault("footL", []).append(r4(0.35 * k))
        d.setdefault("footR", []).append(r4(0.35 * ko))
        d.setdefault("ankleL", []).append(r4(0.25 * k))
        d.setdefault("ankleR", []).append(r4(0.25 * ko))
        d.setdefault("armL", []).append(r4(1.5 * s))
        d.setdefault("armR", []).append(r4(-1.5 * s))
        d.setdefault("elbowL", []).append(r4(0.5 + 0.9 * k))
        d.setdefault("elbowR", []).append(r4(0.5 + 0.9 * ko))
        d.setdefault("bob", []).append(r4(0.06 * math.sin(2 * ph + math.pi / 2.0)))
        d.setdefault("lean", []).append(r4(-0.20))   # 出水仰头（动画端重映射）
        d.setdefault("sway", []).append(r4(0.18 * s))
        d.setdefault("roll", []).append(r4(0.10 * math.sin(ph + math.pi / 2.0)))
        d.setdefault("shift", []).append(r4(0.0))
    return {"name": "swim", "n": N_GAIT, "channels": d}


GAITS["swim"] = swim_gait()


# ---------------------------------------------------------------- 剑击弧线
def catmull(p0, p1, p2, p3, t):
    t2, t3 = t * t, t * t * t
    return 0.5 * ((2 * p1) + (-p0 + p2) * t +
                  (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 +
                  (-p0 + 3 * p1 - 3 * p2 + p3) * t3)


def _sample_keys(kf, n):
    """关键帧 [(t, 值), ...] → n 点 Catmull-Rom 采样。"""
    ts = [k[0] for k in kf]
    vals = [k[1] for k in kf]
    out = []
    for i in range(n):
        t = i / (n - 1)
        seg = 0
        for s in range(len(ts) - 1):
            if ts[s] <= t <= ts[s + 1]:
                seg = s
                break
        p0 = vals[max(0, seg - 1)]
        p1 = vals[seg]
        p2 = vals[min(len(vals) - 1, seg + 1)]
        p3 = vals[min(len(vals) - 1, seg + 2)]
        tt = (t - ts[seg]) / max(1e-6, ts[seg + 1] - ts[seg])
        out.append(r4(catmull(p0, p1, p2, p3, tt)))
    return out


def swing_arc(idx, keys, elbow_keys, torso_keys, lunge_keys, off_keys):
    """keys: {'x':[(t,值)...], 'y':..., 'z':...}，首尾含 0 与 1。"""
    return {
        "idx": idx,
        "n": N_SWING,
        "armX": _sample_keys(keys["x"], N_SWING), "armY": _sample_keys(keys["y"], N_SWING), "armZ": _sample_keys(keys["z"], N_SWING),
        "elbow": _sample_keys(elbow_keys, N_SWING), "torso": _sample_keys(torso_keys, N_SWING),
        "lunge": _sample_keys(lunge_keys, N_SWING), "off": _sample_keys(off_keys, N_SWING),
    }


SWINGS = [
    swing_arc(0,  # 右上撩：低位起手 → 上撩 → 回落
              {"x": [(0, 0.30), (0.15, 0.55), (0.45, 1.95), (0.72, 2.60), (1.0, 2.30)],
               "y": [(0, -0.40), (0.15, -0.55), (0.45, 0.10), (0.72, -0.40), (1.0, -1.05)],
               "z": [(0, -0.35), (0.15, -0.30), (0.45, -0.10), (0.72, -0.28), (1.0, -0.45)]},
              [(0, 0.55), (0.15, 0.70), (0.45, 0.30), (0.72, 0.18), (1.0, 0.45)],
              [(0, 0.0), (0.2, 0.35), (0.5, -0.30), (0.8, -0.15), (1.0, 0.0)],
              [(0, 0.0), (0.2, 0.07), (0.5, 0.11), (0.8, 0.06), (1.0, 0.0)],
              [(0, 0.0), (0.3, -0.35), (0.6, -0.70), (1.0, -0.40)]),
    swing_arc(1,  # 反手横扫：右侧蓄力 → 横向大扫 → 回收
              {"x": [(0, 1.70), (0.18, 1.25), (0.55, 0.95), (0.85, 1.50), (1.0, 1.85)],
               "y": [(0, -1.00), (0.18, -0.65), (0.55, 0.95), (0.85, -0.60), (1.0, -1.15)],
               "z": [(0, 0.10), (0.18, -0.30), (0.55, -0.55), (0.85, -0.35), (1.0, -0.15)]},
              [(0, 0.20), (0.18, 0.30), (0.55, 0.55), (0.85, 0.30), (1.0, 0.20)],
              [(0, 0.30), (0.25, -0.30), (0.6, 0.55), (0.9, -0.20), (1.0, 0.15)],
              [(0, 0.0), (0.25, 0.10), (0.6, 0.13), (1.0, 0.0)],
              [(0, -0.60), (0.3, -0.30), (0.6, 0.10), (1.0, -0.35)]),
    swing_arc(2,  # 纵劈：高举蓄势 → 下劈 → 收势
              {"x": [(0, 0.55), (0.25, 2.85), (0.50, 2.35), (0.72, 0.50), (1.0, 0.65)],
               "y": [(0, -0.20), (0.25, -0.10), (0.50, -0.40), (0.72, -0.60), (1.0, -0.85)],
               "z": [(0, -0.40), (0.25, -0.62), (0.50, 0.18), (0.72, 0.30), (1.0, -0.20)]},
              [(0, 0.50), (0.25, 0.15), (0.50, 0.30), (0.72, 0.65), (1.0, 0.55)],
              [(0, 0.10), (0.3, 0.30), (0.55, -0.25), (0.8, -0.10), (1.0, 0.0)],
              [(0, 0.0), (0.25, -0.05), (0.5, 0.14), (0.75, 0.08), (1.0, 0.0)],
              [(0, -0.30), (0.3, -1.00), (0.6, -0.55), (1.0, -0.30)]),
]


# ---------------------------------------------------------------- 一次性姿态
def oneshot(name, keys, n=N_ONE):
    """keys: {通道: [(t, 值), ...]} → Catmull-Rom 采样曲线组。"""
    return {"name": name, "n": n, "channels": {ch: _sample_keys(kf, n) for ch, kf in keys.items()}}


ONES = {
    # 跳跃：蹬地伸展（前 30%）→ 空中收腿抱膝（后 70%）
    "jump": oneshot("jump", {
        "hipL": [(0, 0.20), (0.30, 0.55), (0.60, -0.10), (1.0, -0.50)],
        "hipR": [(0, 0.25), (0.30, 0.60), (0.60, -0.40), (1.0, -0.80)],
        "kneeL": [(0, 0.40), (0.30, 0.10), (0.60, 0.60), (1.0, 1.20)],
        "kneeR": [(0, 0.40), (0.30, 0.15), (0.60, 0.50), (1.0, 0.90)],
        "footL": [(0, 0.10), (0.30, 0.35), (0.60, 0.10), (1.0, 0.25)],
        "footR": [(0, 0.10), (0.30, 0.35), (0.60, 0.15), (1.0, 0.20)],
        "armL": [(0, -0.40), (0.35, -0.90), (0.70, 0.50), (1.0, 1.30)],
        "armR": [(0, -0.40), (0.35, -0.90), (0.70, 0.30), (1.0, 1.00)],
        "elbowL": [(0, 0.50), (0.35, 0.70), (0.70, 0.30), (1.0, 0.40)],
        "elbowR": [(0, 0.50), (0.35, 0.70), (0.70, 0.30), (1.0, 0.40)],
        "torso": [(0, 0.00), (0.35, -0.10), (0.70, 0.05), (1.0, 0.12)],
        "head": [(0, 0.00), (0.40, -0.12), (0.80, 0.06), (1.0, 0.00)],
        "sink": [(0, 0.00), (0.30, -0.02), (1.0, 0.00)],
    }),
    # 落地：屈髋屈膝缓冲 → 手臂展开平衡 → 恢复
    "land": oneshot("land", {
        "hipL": [(0, 0.50), (0.35, 0.65), (0.70, 0.10), (1.0, 0.00)],
        "hipR": [(0, 0.45), (0.35, 0.60), (0.70, 0.12), (1.0, 0.00)],
        "kneeL": [(0, 1.10), (0.35, 1.40), (0.70, 0.40), (1.0, 0.10)],
        "kneeR": [(0, 1.00), (0.35, 1.30), (0.70, 0.40), (1.0, 0.10)],
        "footL": [(0, 0.25), (0.40, 0.35), (0.80, 0.10), (1.0, 0.05)],
        "footR": [(0, 0.25), (0.40, 0.35), (0.80, 0.10), (1.0, 0.05)],
        "armL": [(0, 0.70), (0.35, 1.00), (0.70, 0.20), (1.0, 0.05)],
        "armR": [(0, 0.50), (0.35, 0.80), (0.70, 0.15), (1.0, 0.05)],
        "elbowL": [(0, 0.50), (0.40, 0.55), (0.80, 0.40), (1.0, 0.42)],
        "elbowR": [(0, 0.50), (0.40, 0.55), (0.80, 0.40), (1.0, 0.42)],
        "torso": [(0, -0.25), (0.40, -0.40), (0.80, -0.08), (1.0, -0.03)],
        "head": [(0, 0.12), (0.40, 0.22), (0.80, 0.05), (1.0, 0.00)],
        "sink": [(0, -0.10), (0.35, -0.13), (0.75, -0.02), (1.0, 0.00)],
    }),
    # 受击退缩：躯干/头向受击侧快扭 → 手臂外张 → 回弹
    "flinch": oneshot("flinch", {
        "torso": [(0, 0.00), (0.15, 0.22), (0.40, -0.10), (0.70, 0.03), (1.0, 0.00)],
        "head": [(0, 0.00), (0.15, 0.28), (0.45, -0.12), (0.75, 0.03), (1.0, 0.00)],
        "armL": [(0, 0.05), (0.20, 0.45), (0.55, -0.15), (1.0, 0.02)],
        "armR": [(0, 0.05), (0.20, 0.38), (0.55, -0.12), (1.0, 0.02)],
        "elbowL": [(0, 0.50), (0.30, 0.80), (0.70, 0.45), (1.0, 0.45)],
        "elbowR": [(0, 0.50), (0.30, 0.80), (0.70, 0.45), (1.0, 0.45)],
        "sink": [(0, 0.00), (0.20, -0.03), (0.60, 0.00), (1.0, 0.00)],
    }, 16),
    # 原地转向：单足点地换步 + 躯干/头先转
    "turn": oneshot("turn", {
        "hipL": [(0, 0.20), (0.40, 0.50), (0.70, 0.10), (1.0, 0.20)],
        "hipR": [(0, 0.35), (0.40, 0.15), (0.70, 0.45), (1.0, 0.35)],
        "footL": [(0, 0.05), (0.40, 0.45), (0.70, 0.05), (1.0, 0.05)],
        "footR": [(0, 0.40), (0.40, 0.10), (0.70, 0.50), (1.0, 0.40)],
        "kneeL": [(0, 0.20), (0.40, 0.50), (0.70, 0.15), (1.0, 0.20)],
        "kneeR": [(0, 0.35), (0.40, 0.20), (0.70, 0.50), (1.0, 0.35)],
        "armL": [(0, 0.10), (0.50, -0.15), (1.0, 0.10)],
        "armR": [(0, 0.10), (0.50, -0.15), (1.0, 0.10)],
        "torso": [(0, 0.00), (0.50, 0.12), (1.0, 0.00)],
        "head": [(0, 0.00), (0.35, 0.18), (0.75, 0.08), (1.0, 0.00)],
    }),
}


# ---------------------------------------------------------------- 空闲微动作覆盖层
def idle2_cycle():
    """64 点慢循环（约 8s 一圈）：深慢呼吸/重心轮换/头部游移/手部小动作。"""
    d = {}
    for i in range(N_IDLE2):
        ph = 2.0 * math.pi * i / N_IDLE2
        d.setdefault("breath", []).append(r4(0.5 * math.sin(2 * ph)))
        d.setdefault("weight", []).append(r4(math.sin(ph)))
        d.setdefault("headY", []).append(r4(0.6 * math.sin(ph + 1.2) + 0.25 * math.sin(2 * ph + 0.5)))
        d.setdefault("headX", []).append(r4(0.35 * math.sin(1.5 * ph + 2.0)))
        d.setdefault("armL", []).append(r4(0.5 * math.sin(ph + 2.4)))
        d.setdefault("armR", []).append(r4(0.5 * math.sin(ph + 2.4 + math.pi)))
        d.setdefault("handL", []).append(r4(0.6 * math.sin(2 * ph + 0.8)))
        d.setdefault("handR", []).append(r4(0.6 * math.sin(2 * ph + 1.5)))
    return {"n": N_IDLE2, "channels": d}


IDLE2 = idle2_cycle()

# ---------------------------------------------------------------- 相机剖面
def pitch_mult(p):
    """仰视拉远、俯视拉近（smoothstep 过渡）。"""
    if p <= -0.2:
        return 1.0
    if p >= 0.9:
        return 0.0
    u = (p + 0.2) / 1.1
    return 3 * u * u - 2 * u * u * u


CAM = {
    "base": {"dist": 6.6, "min": 1.5, "max": 13.0, "height": 1.62, "fov": 62},
    "modes": {
        "explore": {"dist": 6.6, "fov": 62},
        "combat":  {"dist": 5.4, "fov": 61},
        "aim":     {"dist": 4.8, "fov": 44},
        "sprint":  {"dist": 7.4, "fov": 70},
    },
    # 俯仰补偿曲线，采样 pitch -0.75..1.05（共 N_PITCH 点）
    "pitchMin": -0.75, "pitchMax": 1.05, "pitchN": N_PITCH,
    "pitchCurve": [r4(1.12 - 0.42 * pitch_mult(-0.75 + 1.8 * i / (N_PITCH - 1)))
                   for i in range(N_PITCH)],
    "smooth": {"pos": 10.0, "look": 16.0, "dist": 6.5, "distHit": 12.0, "fov": 8.0},
    "shoulder": {"x": 0.55, "fade": 0.55},
    "collide": {"margin": 0.32, "rays": 5, "min": 1.4},
    "lookAhead": 1.4,
    "autoFollow": {"delay": 0.7, "speed": 3.2},
    "zoom": {"step": 0.14, "min": 0.5, "max": 1.9},
}

# ---------------------------------------------------------------- 披风布料
CAPE = {
    "rows": 6, "cols": 5, "w": 0.52, "len": 1.02,
    "gravity": 9.8, "damping": 0.965, "stiffness": 0.18, "wind": 0.35, "iter": 2,
}

DATA = {
    "version": "v2.4",
    "gait": GAITS,
    "swing": SWINGS,
    "oneshot": ONES,
    "idle2": IDLE2,
    "cam": CAM,
    "cape": CAPE,
}


def main():
    js = "/* @generated by tools/charrig.py — 角色动画/相机/布料数据（勿手改，改曲线请改生成器后重跑） */\n"
    js += "(function () {\n  'use strict';\n  window.G = window.G || {};\n  window.G.CHARRIG = "
    js += json.dumps(DATA, ensure_ascii=False, separators=(",", ":"))
    js += ";\n})();\n"
    with open(OUT, "w", encoding="utf-8") as f:
        f.write(js)
    print(f"charrig_data.js 生成完成 → {OUT}  ({len(js)} 字节)")
    print(f"  步态 {len(GAITS)} 种 × {N_GAIT} 点；剑击 {len(SWINGS)} 段 × {N_SWING} 点；"
          f"一次性姿态 {len(ONES)} 组；空闲微动作 {N_IDLE2} 点；俯仰曲线 {N_PITCH} 点")


if __name__ == "__main__":
    main()
