# 招亦废丽罗 · 律令之楔
**ZHAO YI FEI LI LUO — Chronicle of the Stilled World**

**v3.3 · Unity 引擎版**（Unity 2022.3 LTS · Built-in Render Pipeline · C#）
东方废土 × 生物机械美学的 3D 开放世界动作游戏垂直切片：程序化生成的 8192×8192 米世界、青铜旅人角色与顶级相机、三型敌人、四 NPC 完整对话树与任务、五楔之坛、昼夜/天气/能量潮汐、存档。**整个工程不包含任何 HTML 内容**，双击打开即可在 Unity 中游玩。

---

## 一、如何运行

**前置条件**：安装 [Unity Hub](https://unity.com/download) 与任意 **Unity 2022.3 LTS**（推荐 `2022.3.62f1`，其它 2022.3.x 亦可）。

| 方式 | 操作 |
| --- | --- |
| 一键启动（推荐） | 双击仓库根目录 **`启动Unity版.bat`**：自动查找本机 Unity Editor 并用该工程打开；未安装时给出安装指引 |
| 手动打开 | Unity Hub → `Add` → `Add project from disk` → 选择 **`UnityEdition`** 文件夹 → 打开 → 点 **Play** |
| 重新导出数据 | `python tools/export_unity.py`（把动画曲线/对话树/世界文本写入 `UnityEdition/Assets/StreamingAssets/`） |

> **首次打开工程时**，编辑器脚本 `Assets/Editor/ProjectSetup.cs` 会自动：
> 1. 设置产品名、1280×720 分辨率、Linear 色彩空间；
> 2. 生成 `Assets/Scenes/Main.unity` 并加入 Build Settings（避免手写场景文件）；
> 3. 运行时由 `GameManager` 的 `[RuntimeInitializeOnLoadMethod]` 自举——即使场景为空也能直接运行，场景内所有内容均由代码程序化创建。

---

## 二、操作说明

| 按键 | 功能 |
| --- | --- |
| **W A S D** | 移动（方向以相机为基准，镜头旋转即改变前进方向） |
| **Shift** | 冲刺（消耗耐力，相机会自动转回角色背后） |
| **空格** | 跳跃（落地有缓冲姿态与镜头震动） |
| **Q** | 翻滚（0.5 秒无敌帧） |
| **鼠标左键** | 三段连击剑击（命中窗口 0.22–0.62，扇形 65°，命中判定带顿挫与剑光粒子） |
| **鼠标移动 / 滚轮** | 旋转视角 / 缩放镜头（0.5×–1.9×） |
| **R** | 律令（能量满且已解锁法则之坛后，10 秒伤害 ×1.5） |
| **E** | 交互：篝火 / 碑文 / 晶簇 / 遗物匣 / 楔坛 / 灵脉之泉 / NPC 对话 |
| **Tab** | 锁定最近敌人（镜头自动跟随目标） |
| **1 – 6** | 对话选项 |
| **F1 / F5 / F9** | 显示/隐藏统计面板 / 存档 / 读档（另有 60 秒自动存档） |
| **Esc** | 关闭对话 / 释放鼠标 |

---

## 三、游戏内容

- **世界**：8192×8192 米确定性高度场（Python 世界生成器同源算法复刻），五大区域（平原/青铜林/回音山/熔炉/泪湖）与四大外域（锈沙海/冰晶冻原/烬海荒原/沧溟裂谷），泪湖水面、楔之峰 128.5 米平台。
- **青铜旅人**：约 90 个程序化部件（青铜甲、战裙、肩甲尖刺、面具眼缝、背甲水晶、发光符文、长剑），7 套步态 + 跳跃/落地/受击/转向一次性姿态 + 足部 IK + 披风 Verlet 布料，全部由 `charrig.json` 曲线数据驱动。
- **相机**：四种模式自动切换（探索/战斗/瞄准/冲刺）、俯仰补偿、肩部偏移、地形碰撞采样、冲刺自动跟随、动态 FOV、震屏。
- **敌人**：构造机偶（追击/挥击）、飞翼幽魂（盘旋/俯冲）、熔岩兽（冲撞/高伤）三型，随击杀数提升等级。
- **对话与任务**：钟漏 / 回声 / 守夜人 / 行商四位 NPC，分支对话树、条件判定、任务链（锈钟的低语、回声的三次矛盾、守夜人的委托、五楔之谜）、商店交易、奖励（血量/耐力/伤害/诗句）。
- **世界交互**：篝火（回血并更新重生点）、八块碑文（世界叙事）、晶簇（拾取晶尘）、遗物匣（随机战利品）、五座楔坛（永久属性加成，法则之坛解锁「律令」）、灵脉之泉（恢复）。
- **活世界**：24 小时伪时间循环（夜/黎明/白昼/黄昏四时段调色）、三层天气（晴/阴/雨/雾/尘暴/晶尘雨）、能量潮汐与能量状态、黄昏雾与太阳方位随时间变化。
- **存档**：F5/F9 手动 + 60 秒自动，保存位置/属性/任务/背包/旗标。

---

## 四、技术实现

| 层次 | 实现 |
| --- | --- |
| 引擎 | Unity 2022.3 LTS，Built-in Render Pipeline |
| 世界 | `Data.cs` 复刻 Python 生成器的确定性噪声与高度函数（`mulberry32` + 值噪声 + fbm），`Terrain.cs` 按玩家位置流式生成 64 米区块（33×33 顶点、程序化顶点色、法线差分、道具合并网格） |
| 角色 | `Character.cs` 程序化层级骨架（骨盆/躯干/胸/颈/头/肩/肘/腕/髋/膝/踝），动画由 `charrig.json` 曲线采样（步态混合、方向感知、一次性姿态叠加、相位同步脚步） |
| 相机 | `CameraRig.cs` 独立相机系统（模式/俯仰补偿/碰撞/跟随/肩偏移/FOV/震屏） |
| 渲染 | CPU：网格生成、动画、动画层混合；GPU：自写 ShaderLab（`VertexColorLit` 顶点色光照、`Sky` 渐变天空、`Water` 波纹水面），方向光实时阴影、雾 |
| UI | `Hud.cs` IMGUI + 系统中文字体（Microsoft YaHei），血/耐力/律令条、时钟、天气、任务、对话面板、伤害数字 |
| 数据 | `MiniJson` 自写解析器；`StreamingAssets/*.json` 与 Python 生成器同源 |
| 启动 | `ProjectSetup.cs`（编辑器自动建场景）+ `GameManager.Boot()` 运行时自举，零手工场景配置 |

**CPU + GPU 分工**：CPU 负责确定性世界生成、区块流式、骨骼动画曲线采样、战斗与对话逻辑、存档；GPU 负责顶点着色光照、雾、天空渐变、半透明水面与 HUD 合成。

---

## 五、工程目录

```
UnityEdition/
├─ Assets/
│  ├─ Scripts/
│  │  ├─ Data.cs          世界数学 + JSON 数据 + MiniJson
│  │  ├─ Geometry.cs      图元/合并/材质/披风 Verlet
│  │  ├─ Character.cs     青铜旅人 + 三型敌人模型与动画
│  │  ├─ Terrain.cs       地形流式、道具、泪湖、阴影贴片
│  │  ├─ CameraRig.cs     四模式相机
│  │  ├─ Player.cs        移动/跳跃/游泳/翻滚/剑击/律令
│  │  ├─ Dialogue.cs      对话树运行器（分支/条件/效果）
│  │  ├─ Interactables.cs 交互物、NPC、世界布局
│  │  ├─ Enemies.cs       敌人 AI 与刷怪
│  │  ├─ Env.cs           昼夜/天气/潮汐/能量
│  │  ├─ Hud.cs           IMGUI 中文界面
│  │  └─ GameManager.cs   引导、战斗判定、交互、存档
│  ├─ Shaders/            VertexColorLit / Sky / Water
│  ├─ Editor/ProjectSetup.cs
│  └─ StreamingAssets/    charrig.json / dialogue.json / lore.json
├─ Packages/manifest.json
└─ ProjectSettings/ProjectVersion.txt   (2022.3.62f1)
```

```
tools/
├─ export_unity.py        导出数据到 StreamingAssets
├─ datagen/               自包含数据源（charrig.py / dialogue.py / lore.py）
└─ checks/                无 Unity 环境下的自检工具（静态检查 + 真编译 + 数据冒烟）
   └─ stubcheck/          自写 UnityEngine / UnityEditor 桩（仅供编译校验）
```

---

## 六、数据管线

角色动画、对话树、碑文与楔坛数据都由 Python 数据源生成，导出为 JSON 后由 Unity 运行时读取（同一套数据可被其它运行时复用）：

```bash
python tools/export_unity.py
```

修改 `tools/datagen/charrig.py` 中的步态/剑击曲线后重新导出，即可在 Unity 中看到新的动作手感，无需改动 C#。

---

## 七、自检工具（无需 Unity Editor）

本工程在**没有安装 Unity 编辑器的机器**上完成，因此附带一整套离线自检工具：先用自写的 `UnityEngine` / `UnityEditor` 桩将**全部 C# 脚本用 Roslyn 真编译**，再用桩运行时加载真实 `StreamingAssets` 数据做冒烟测试。

```bash
# 1) 括号/结构静态检查
python tools/checks/check_syntax.py
python tools/checks/check_members.py

# 2) 真编译：12 个运行时脚本 + 编辑器脚本，对自写 Unity 桩编译（0 error 即通过）
powershell -NoProfile -ExecutionPolicy Bypass -File tools/checks/compile_check.ps1

# 3) 数据冒烟：加载 charrig/dialogue/lore 三个 JSON，校验解析与世界数学
powershell -NoProfile -ExecutionPolicy Bypass -File tools/checks/data_smoke.ps1
```

当前结果：

| 检查 | 结果 |
| --- | --- |
| 括号/结构 | 16 个 `.cs`/`.shader` 文件 **0 问题** |
| 跨文件成员引用 | 仅 2 条 `catch (Exception e)` 误报（非真实错误） |
| Roslyn 真编译（运行时 12 + 编辑器 1） | **0 error** |
| 数据冒烟（19 项断言） | **全部 PASS**（步态 7 套 / 剑击 3 段 / 一次性姿态 4 套 / 相机曲线 19 点 / 披风 6×5 / 4 NPC / 8 碑文 / 5 楔坛 / 峰顶 128.4 m / 泪湖判定与水位 7 / 出生点 470,470 属青铜林 / JSON 采样有效） |

> 说明：桩编译能验证语法、类型、跨文件接口与 API 用法正确性，但无法替代在 Unity 中实机渲染；打开工程后如有个别运行期问题，请优先查看 Console 的第一个报错。（第 2、3 项校验需要 .NET 8 SDK 与 Roslyn，**仅开发自检使用，游玩不需要**。）

---

## 八、常见问题

- **首次打开提示少量警告**：Unity 首次导入会生成 `.meta`/`Library`，属正常现象；如提示某脚本找不到，请确认 `Assets/StreamingAssets/` 下三个 JSON 存在（可运行 `python tools/export_unity.py` 重新生成）。
- **没有 Main 场景**：`ProjectSetup.cs` 会在编辑器加载后自动创建；即使没有场景，直接按 Play 也会由 `GameManager.Boot()` 生成全部内容。
- **中文字体**：HUD 使用系统「Microsoft YaHei」，Windows 自带；若在其它系统运行，会自动回退 Arial。
- **性能**：地形区块流式生成、每区块合并道具网格、笔刷化顶点色着色，均针对集显优化；如需更高帧率可降低 `TerrainStreamer.radius`。
- **输入**：使用 Unity 旧版 Input Manager（默认启用），无需安装 Input System 包。

---

## 设计文档

完整世界观、区域、楔子能力、诗句与叙事设定见 **`生成指令.txt`**。
