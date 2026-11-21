// DataSmoke.cs —— 在没有 Unity 的环境里，用自写 UnityEngine 桩 + 真实 StreamingAssets 数据
// 验证 Data.cs 的 JSON 解析与世界数学（高度/区域/湖泊）是否正确。
// 由 tools/checks/data_smoke.ps1 编译运行。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class DataSmoke
{
    static int errs = 0;

    static void Check(string name, bool ok, string detail = "")
    {
        Console.WriteLine((ok ? "PASS  " : "FAIL  ") + name + (detail.Length > 0 ? "  " + detail : ""));
        if (!ok) errs++;
    }

    public static int Main(string[] args)
    {
        string dir = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        Application.streamingAssetsPathOverride = dir;
        Application.persistentDataPathOverride = Path.Combine(Path.GetTempPath(), "zhaoyi_smoke");

        try
        {
            GameData.Load();
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL  GameData.Load 抛异常: " + e);
            return 1;
        }

        Console.WriteLine("version = " + GameData.Version);
        Check("版本 v2.4", GameData.Version == "v2.4", GameData.Version);
        Check("步态 7 套", GameData.Gaits.Count == 7, "count=" + GameData.Gaits.Count);
        Check("剑击 3 段", GameData.Swings.Count == 3, "count=" + GameData.Swings.Count);
        Check("一次性姿态 4 套", GameData.OneShots.Count == 4, "count=" + GameData.OneShots.Count);
        Check("idle2 通道", GameData.Idle2.Count == 8, "count=" + GameData.Idle2.Count);
        Check("相机俯仰曲线", GameData.Cam.pitchCurve != null && GameData.Cam.pitchCurve.Length >= 10,
              "len=" + (GameData.Cam.pitchCurve == null ? 0 : GameData.Cam.pitchCurve.Length));
        Check("披风网格", GameData.Cape.rows == 6 && GameData.Cape.cols == 5,
              GameData.Cape.rows + "x" + GameData.Cape.cols);
        Check("对话 NPC 4 位", GameData.Npcs.Count == 4, "count=" + GameData.Npcs.Count);
        Check("碑文 8 段", GameData.SteleTexts.Length == 8, "count=" + GameData.SteleTexts.Length);
        Check("楔坛 5 座", GameData.Shrines.Count == 5, "count=" + GameData.Shrines.Count);
        Check("出生点 (470,470)", Mathf.Abs(GameData.Spawn.x - 470f) < 0.01f && Mathf.Abs(GameData.Spawn.y - 470f) < 0.01f,
              GameData.Spawn.ToString());

        // 世界数学抽查
        float hSpawn = GameData.HeightAt(470f, 470f);
        Check("出生点高度 >100", hSpawn > 100f && hSpawn < 200f, "h=" + hSpawn.ToString("0.00"));
        float hPeak = GameData.HeightAt(0f, 0f);
        Check("楔之峰顶 128.5", Mathf.Abs(hPeak - 128.5f) < 1.5f, "h=" + hPeak.ToString("0.00"));
        bool lake = GameData.InLake(0f, -540f);
        Check("泪湖判定", lake, "inLake=" + lake);
        Check("泪湖水位 7", Mathf.Abs(GameData.WaterAt(0f, -540f) - 7f) < 0.01f, GameData.WaterAt(0f, -540f).ToString("0.0"));
        var c1 = GameData.RegionColor(GameData.RegionAt(470f, 470f));
        Check("出生点属于青铜林", GameData.RegionAt(470f, 470f) == "forest", GameData.RegionAt(470f, 470f));
        Check("区域配色非零", c1.r + c1.g + c1.b > 0.05f, c1.ToString());

        // 数据完整性抽查
        var npc = GameData.Npcs["zhonglou"];
        Check("钟漏分支/节点", npc.branches.Count >= 3 && npc.nodes.Count >= 10,
              "branches=" + npc.branches.Count + " nodes=" + npc.nodes.Count);
        var gait = GameData.Gaits["walk"];
        Check("走路通道 17 个", gait.ch.Count == 17, "channels=" + gait.ch.Count);
        Check("走路采样有效", !float.IsNaN(gait.Sample("hipL", 0.3f)) && Mathf.Abs(gait.Sample("hipL", 0.3f)) < 3f,
              "hipL(0.3)=" + gait.Sample("hipL", 0.3f).ToString("0.000"));

        Console.WriteLine();
        if (errs == 0) { Console.WriteLine("[OK] 数据冒烟测试全部通过"); return 0; }
        Console.WriteLine("[FAIL] " + errs + " 项失败");
        return 1;
    }
}
