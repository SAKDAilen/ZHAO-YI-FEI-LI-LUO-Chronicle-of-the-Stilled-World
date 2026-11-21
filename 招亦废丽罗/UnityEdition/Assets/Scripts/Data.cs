// Data.cs —— JSON 解析、共享数据加载（动画曲线 / 对话树 / 碑文）、世界高度与区域配色
// 数据与网页版、原生版完全同源：tools/export_unity.py 导出到 StreamingAssets。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// ---------------------------------------------------------------- 极简 JSON
public static class MiniJson
{
    public static object Parse(string text)
    {
        var r = new Reader(text);
        return r.Value();
    }

    class Reader
    {
        readonly string s;
        int i;

        public Reader(string t) { s = t; }

        void Skip()
        {
            while (i < s.Length)
            {
                char c = s[i];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n') i++;
                else break;
            }
        }

        public object Value()
        {
            Skip();
            if (i >= s.Length) return null;
            char c = s[i];
            if (c == '{') return Obj();
            if (c == '[') return Arr();
            if (c == '"') return Str();
            if (c == 't') { i += 4; return true; }
            if (c == 'f') { i += 5; return false; }
            if (c == 'n') { i += 4; return null; }
            return Num();
        }

        Dictionary<string, object> Obj()
        {
            var d = new Dictionary<string, object>();
            i++; // {
            while (true)
            {
                Skip();
                if (i >= s.Length) return d;
                if (s[i] == '}') { i++; return d; }
                string k = Str();
                Skip();
                if (i < s.Length && s[i] == ':') i++;
                d[k] = Value();
                Skip();
                if (i < s.Length && s[i] == ',') i++;
            }
        }

        List<object> Arr()
        {
            var a = new List<object>();
            i++; // [
            while (true)
            {
                Skip();
                if (i >= s.Length) return a;
                if (s[i] == ']') { i++; return a; }
                a.Add(Value());
                Skip();
                if (i < s.Length && s[i] == ',') i++;
            }
        }

        string Str()
        {
            var sb = new StringBuilder();
            Skip();
            if (i < s.Length && s[i] == '"') i++;
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') break;
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        if (i + 4 <= s.Length)
                        {
                            int code = int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            sb.Append((char)code);
                            i += 4;
                        }
                        break;
                    default: sb.Append(e); break;
                }
            }
            return sb.ToString();
        }

        object Num()
        {
            int start = i;
            while (i < s.Length)
            {
                char c = s[i];
                if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E') i++;
                else break;
            }
            double d;
            double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out d);
            return d;
        }
    }

    // ---- 便捷访问
    public static Dictionary<string, object> Dict(object o)
    {
        return o as Dictionary<string, object> ?? new Dictionary<string, object>();
    }

    public static List<object> List(object o)
    {
        return o as List<object> ?? new List<object>();
    }

    public static string Str(object o, string def = "")
    {
        if (o == null) return def;
        return o as string ?? Convert.ToString(o, CultureInfo.InvariantCulture);
    }

    public static float F(object o, float def = 0f)
    {
        if (o == null) return def;
        if (o is double) return (float)(double)o;
        if (o is bool) return (bool)o ? 1f : 0f;
        float v;
        if (float.TryParse(Str(o), NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
        return def;
    }

    public static int I(object o, int def = 0)
    {
        return Mathf.RoundToInt(F(o, def));
    }

    public static bool B(object o, bool def = false)
    {
        if (o == null) return def;
        if (o is bool) return (bool)o;
        if (o is double) return (double)o != 0.0;
        return def;
    }

    public static Dictionary<string, object> Sub(Dictionary<string, object> d, string key)
    {
        object o;
        if (d != null && d.TryGetValue(key, out o)) return Dict(o);
        return new Dictionary<string, object>();
    }

    public static float[] Floats(object o)
    {
        var list = List(o);
        var arr = new float[list.Count];
        for (int k = 0; k < list.Count; k++) arr[k] = F(list[k]);
        return arr;
    }
}

// ---------------------------------------------------------------- 动画数据
public class GaitData
{
    public string name;
    public int n;
    public Dictionary<string, float[]> ch = new Dictionary<string, float[]>();

    public float Sample(string key, float phaseRad)
    {
        float[] arr;
        if (!ch.TryGetValue(key, out arr) || arr.Length == 0) return 0f;
        float t = (phaseRad / (2f * Mathf.PI)) * n;
        t -= Mathf.Floor(t / n) * n;
        int i0 = Mathf.FloorToInt(t);
        int i1 = (i0 + 1) % n;
        float f = t - i0;
        return Mathf.Lerp(arr[i0 % n], arr[i1 % n], f);
    }
}

public class SwingArc
{
    public int idx;
    public int n;
    public float[] armX, armY, armZ, elbow, torso, lunge, off;

    public float Pick(float[] arr, float t01)
    {
        if (arr == null || arr.Length == 0) return 0f;
        float t = Mathf.Clamp01(t01) * (n - 1);
        int i0 = Mathf.FloorToInt(t);
        int i1 = Mathf.Min(n - 1, i0 + 1);
        return Mathf.Lerp(arr[i0], arr[i1], t - i0);
    }
}

public class OneShot
{
    public string name;
    public int n;
    public Dictionary<string, float[]> ch = new Dictionary<string, float[]>();

    public float At(string key, float t01)
    {
        float[] arr;
        if (!ch.TryGetValue(key, out arr) || arr.Length == 0) return 0f;
        float t = Mathf.Clamp01(t01) * (n - 1);
        int i0 = Mathf.FloorToInt(t);
        int i1 = Mathf.Min(n - 1, i0 + 1);
        return Mathf.Lerp(arr[i0], arr[i1], t - i0);
    }
}

public class CamCfg
{
    public float dist = 6.6f, distMin = 1.5f, distMax = 13f, height = 1.62f, fov = 62f;
    public float pitchMin = -0.75f, pitchMax = 1.05f;
    public float[] pitchCurve = new float[19];
    public float smoothPos = 10f, smoothLook = 16f, smoothDist = 6.5f, smoothDistHit = 12f, smoothFov = 8f;
    public float shoulderX = 0.55f, shoulderFade = 0.55f;
    public float collideMargin = 0.32f, lookAhead = 1.4f;
    public float autoDelay = 0.7f, autoSpeed = 3.2f;
    public float zoomStep = 0.14f, zoomMin = 0.5f, zoomMax = 1.9f;
    public Dictionary<string, float[]> modeDist = new Dictionary<string, float[]>();
    public Dictionary<string, float[]> modeFov = new Dictionary<string, float[]>();
}

public class CapeCfg
{
    public int rows = 6, cols = 5, iter = 2;
    public float w = 0.52f, len = 1.02f, gravity = 9.8f, damping = 0.965f, stiffness = 0.18f, wind = 0.35f;
}

public class BranchDef
{
    public string id;
    public Dictionary<string, object> cond;
    public string text;
    public Dictionary<string, object> onEnter;
}

public class ChoiceDef
{
    public string text;
    public string next;      // 可为 null
    public Dictionary<string, object> effect;
}

public class NodeDef
{
    public string id;
    public string text;
    public Dictionary<string, object> onEnter;
    public List<ChoiceDef> choices = new List<ChoiceDef>();
}

public class NpcDef
{
    public string key, name, title, model, greet;
    public List<BranchDef> branches = new List<BranchDef>();
    public Dictionary<string, NodeDef> nodes = new Dictionary<string, NodeDef>();
}

public class ShrineDef
{
    public string id, name, desc;
    public Dictionary<string, object> bonus = new Dictionary<string, object>();
}

// ---------------------------------------------------------------- 全局数据
public static class GameData
{
    // ---- 世界常量（与 native/world.py 一致）
    public const float SIZE = 8192f;
    public const float WORLD_HALF = SIZE * 0.5f;
    public const float CHUNK = 64f;
    public const int CHUNK_SEGS = 32;
    public static Vector2 Spawn = new Vector2(470f, 470f);

    public static string Version = "v1.0";
    public static Dictionary<string, GaitData> Gaits = new Dictionary<string, GaitData>();
    public static List<SwingArc> Swings = new List<SwingArc>();
    public static Dictionary<string, OneShot> OneShots = new Dictionary<string, OneShot>();
    public static Dictionary<string, float[]> Idle2 = new Dictionary<string, float[]>();
    public static int Idle2N = 64;
    public static CamCfg Cam = new CamCfg();
    public static CapeCfg Cape = new CapeCfg();
    public static Dictionary<string, NpcDef> Npcs = new Dictionary<string, NpcDef>();
    public static Dictionary<string, string[]> Quests = new Dictionary<string, string[]>();
    public static Dictionary<string, string> ItemNames = new Dictionary<string, string>();
    public static string[] SteleTexts = new string[0];
    public static List<ShrineDef> Shrines = new List<ShrineDef>();

    static bool loaded;

    public static void Load()
    {
        if (loaded) return;
        loaded = true;
        string dir = Path.Combine(Application.streamingAssetsPath, "");
        LoadCharRig(Path.Combine(dir, "charrig.json"));
        LoadDialogue(Path.Combine(dir, "dialogue.json"));
        LoadLore(Path.Combine(dir, "lore.json"));
        InitPerm();
        Debug.Log("[数据] 已加载：" + Gaits.Count + " 步态 / " + Swings.Count + " 剑弧 / "
                  + Npcs.Count + " NPC / " + Shrines.Count + " 楔坛");
    }

    static string ReadAll(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("[数据] 缺少文件：" + path + "（请先运行 python tools/export_unity.py）");
            return "{}";
        }
        return File.ReadAllText(path, Encoding.UTF8);
    }

    static void LoadCharRig(string path)
    {
        var root = MiniJson.Dict(MiniJson.Parse(ReadAll(path)));
        Version = MiniJson.Str(root.ContainsKey("version") ? root["version"] : null, "v2.4");

        foreach (var kv in MiniJson.Sub(root, "gait"))
        {
            var gd = MiniJson.Dict(kv.Value);
            var g = new GaitData();
            g.name = MiniJson.Str(gd.ContainsKey("name") ? gd["name"] : null, kv.Key);
            g.n = Mathf.Max(1, MiniJson.I(gd.ContainsKey("n") ? gd["n"] : null, 32));
            foreach (var chkv in MiniJson.Sub(gd, "channels"))
                g.ch[chkv.Key] = MiniJson.Floats(chkv.Value);
            Gaits[kv.Key] = g;
            if (!Gaits.ContainsKey(g.name)) Gaits[g.name] = g;
        }

        var swings = MiniJson.List(root.ContainsKey("swing") ? root["swing"] : null);
        for (int k = 0; k < swings.Count; k++)
        {
            var sd = MiniJson.Dict(swings[k]);
            var sw = new SwingArc();
            sw.idx = k;
            sw.n = Mathf.Max(1, MiniJson.I(sd.ContainsKey("n") ? sd["n"] : null, 24));
            sw.armX = MiniJson.Floats(sd.ContainsKey("armX") ? sd["armX"] : null);
            sw.armY = MiniJson.Floats(sd.ContainsKey("armY") ? sd["armY"] : null);
            sw.armZ = MiniJson.Floats(sd.ContainsKey("armZ") ? sd["armZ"] : null);
            sw.elbow = MiniJson.Floats(sd.ContainsKey("elbow") ? sd["elbow"] : null);
            sw.torso = MiniJson.Floats(sd.ContainsKey("torso") ? sd["torso"] : null);
            sw.lunge = MiniJson.Floats(sd.ContainsKey("lunge") ? sd["lunge"] : null);
            sw.off = MiniJson.Floats(sd.ContainsKey("off") ? sd["off"] : null);
            Swings.Add(sw);
        }

        foreach (var kv in MiniJson.Sub(root, "oneshot"))
        {
            var od = MiniJson.Dict(kv.Value);
            var one = new OneShot();
            one.name = kv.Key;
            one.n = Mathf.Max(1, MiniJson.I(od.ContainsKey("n") ? od["n"] : null, 24));
            foreach (var chkv in MiniJson.Sub(od, "channels"))
                one.ch[chkv.Key] = MiniJson.Floats(chkv.Value);
            OneShots[kv.Key] = one;
        }

        var idle2 = MiniJson.Sub(root, "idle2");
        Idle2N = Mathf.Max(1, MiniJson.I(idle2.ContainsKey("n") ? idle2["n"] : null, 64));
        foreach (var kv in MiniJson.Sub(idle2, "channels"))
            Idle2[kv.Key] = MiniJson.Floats(kv.Value);

        var cam = MiniJson.Sub(root, "cam");
        var baseD = MiniJson.Sub(cam, "base");
        Cam.dist = MiniJson.F(baseD.ContainsKey("dist") ? baseD["dist"] : null, 6.6f);
        Cam.distMin = MiniJson.F(baseD.ContainsKey("min") ? baseD["min"] : null, 1.5f);
        Cam.distMax = MiniJson.F(baseD.ContainsKey("max") ? baseD["max"] : null, 13f);
        Cam.height = MiniJson.F(baseD.ContainsKey("height") ? baseD["height"] : null, 1.62f);
        Cam.fov = MiniJson.F(baseD.ContainsKey("fov") ? baseD["fov"] : null, 62f);
        Cam.pitchMin = MiniJson.F(cam.ContainsKey("pitchMin") ? cam["pitchMin"] : null, -0.75f);
        Cam.pitchMax = MiniJson.F(cam.ContainsKey("pitchMax") ? cam["pitchMax"] : null, 1.05f);
        Cam.pitchCurve = MiniJson.Floats(cam.ContainsKey("pitchCurve") ? cam["pitchCurve"] : null);
        var smooth = MiniJson.Sub(cam, "smooth");
        Cam.smoothPos = MiniJson.F(smooth.ContainsKey("pos") ? smooth["pos"] : null, 10f);
        Cam.smoothLook = MiniJson.F(smooth.ContainsKey("look") ? smooth["look"] : null, 16f);
        Cam.smoothDist = MiniJson.F(smooth.ContainsKey("dist") ? smooth["dist"] : null, 6.5f);
        Cam.smoothDistHit = MiniJson.F(smooth.ContainsKey("distHit") ? smooth["distHit"] : null, 12f);
        Cam.smoothFov = MiniJson.F(smooth.ContainsKey("fov") ? smooth["fov"] : null, 8f);
        var shoulder = MiniJson.Sub(cam, "shoulder");
        Cam.shoulderX = MiniJson.F(shoulder.ContainsKey("x") ? shoulder["x"] : null, 0.55f);
        Cam.shoulderFade = MiniJson.F(shoulder.ContainsKey("fade") ? shoulder["fade"] : null, 0.55f);
        var collide = MiniJson.Sub(cam, "collide");
        Cam.collideMargin = MiniJson.F(collide.ContainsKey("margin") ? collide["margin"] : null, 0.32f);
        Cam.lookAhead = MiniJson.F(cam.ContainsKey("lookAhead") ? cam["lookAhead"] : null, 1.4f);
        var auto = MiniJson.Sub(cam, "autoFollow");
        Cam.autoDelay = MiniJson.F(auto.ContainsKey("delay") ? auto["delay"] : null, 0.7f);
        Cam.autoSpeed = MiniJson.F(auto.ContainsKey("speed") ? auto["speed"] : null, 3.2f);
        var zoom = MiniJson.Sub(cam, "zoom");
        Cam.zoomStep = MiniJson.F(zoom.ContainsKey("step") ? zoom["step"] : null, 0.14f);
        Cam.zoomMin = MiniJson.F(zoom.ContainsKey("min") ? zoom["min"] : null, 0.5f);
        Cam.zoomMax = MiniJson.F(zoom.ContainsKey("max") ? zoom["max"] : null, 1.9f);
        foreach (var kv in MiniJson.Sub(cam, "modes"))
        {
            var md = MiniJson.Dict(kv.Value);
            if (md.ContainsKey("dist"))
                Cam.modeDist[kv.Key] = new float[] { MiniJson.F(md["dist"]), MiniJson.F(md.ContainsKey("fov") ? md["fov"] : null, 62f) };
        }
        if (Cam.modeDist.Count == 0)
        {
            Cam.modeDist["explore"] = new float[] { 6.6f, 62f };
            Cam.modeDist["combat"] = new float[] { 5.4f, 61f };
            Cam.modeDist["aim"] = new float[] { 4.8f, 44f };
            Cam.modeDist["sprint"] = new float[] { 7.4f, 70f };
        }
        if (Cam.pitchCurve.Length == 0) Cam.pitchCurve = new float[] { 1f };

        var cape = MiniJson.Sub(root, "cape");
        Cape.rows = MiniJson.I(cape.ContainsKey("rows") ? cape["rows"] : null, 6);
        Cape.cols = MiniJson.I(cape.ContainsKey("cols") ? cape["cols"] : null, 5);
        Cape.w = MiniJson.F(cape.ContainsKey("w") ? cape["w"] : null, 0.52f);
        Cape.len = MiniJson.F(cape.ContainsKey("len") ? cape["len"] : null, 1.02f);
        Cape.gravity = MiniJson.F(cape.ContainsKey("gravity") ? cape["gravity"] : null, 9.8f);
        Cape.damping = MiniJson.F(cape.ContainsKey("damping") ? cape["damping"] : null, 0.965f);
        Cape.stiffness = MiniJson.F(cape.ContainsKey("stiffness") ? cape["stiffness"] : null, 0.18f);
        Cape.wind = MiniJson.F(cape.ContainsKey("wind") ? cape["wind"] : null, 0.35f);
        Cape.iter = MiniJson.I(cape.ContainsKey("iter") ? cape["iter"] : null, 2);
    }

    static void LoadDialogue(string path)
    {
        var root = MiniJson.Dict(MiniJson.Parse(ReadAll(path)));
        foreach (var kv in MiniJson.Sub(root, "npcs"))
        {
            var nd = MiniJson.Dict(kv.Value);
            var npc = new NpcDef();
            npc.key = kv.Key;
            npc.name = MiniJson.Str(nd.ContainsKey("name") ? nd["name"] : null, kv.Key);
            npc.title = MiniJson.Str(nd.ContainsKey("title") ? nd["title"] : null, "");
            npc.model = MiniJson.Str(nd.ContainsKey("model") ? nd["model"] : null, "clockwork");
            npc.greet = MiniJson.Str(nd.ContainsKey("greet") ? nd["greet"] : null, "");

            foreach (object o in MiniJson.List(nd.ContainsKey("branches") ? nd["branches"] : null))
            {
                var bd = MiniJson.Dict(o);
                var b = new BranchDef();
                b.id = MiniJson.Str(bd.ContainsKey("id") ? bd["id"] : null, "");
                b.cond = MiniJson.Sub(bd, "cond");
                b.text = MiniJson.Str(bd.ContainsKey("text") ? bd["text"] : null, "");
                b.onEnter = MiniJson.Sub(bd, "on_enter");
                npc.branches.Add(b);
            }

            foreach (var nkv in MiniJson.Sub(nd, "nodes"))
            {
                var nodeD = MiniJson.Dict(nkv.Value);
                var node = new NodeDef();
                node.id = nkv.Key;
                node.text = MiniJson.Str(nodeD.ContainsKey("text") ? nodeD["text"] : null, "");
                node.onEnter = MiniJson.Sub(nodeD, "on_enter");
                foreach (object o in MiniJson.List(nodeD.ContainsKey("choices") ? nodeD["choices"] : null))
                {
                    var cd = MiniJson.Dict(o);
                    var c = new ChoiceDef();
                    c.text = MiniJson.Str(cd.ContainsKey("text") ? cd["text"] : null, "");
                    object nx;
                    c.next = cd.TryGetValue("next", out nx) && nx != null ? MiniJson.Str(nx) : null;
                    c.effect = MiniJson.Sub(cd, "effect");
                    node.choices.Add(c);
                }
                npc.nodes[nkv.Key] = node;
            }
            Npcs[kv.Key] = npc;
        }

        foreach (var kv in MiniJson.Sub(root, "quests"))
        {
            var qd = MiniJson.Dict(kv.Value);
            var stages = new List<string>();
            foreach (object o in MiniJson.List(qd.ContainsKey("stages") ? qd["stages"] : null))
                stages.Add(MiniJson.Str(o));
            Quests[kv.Key] = stages.ToArray();
        }

        foreach (var kv in MiniJson.Sub(root, "items"))
            ItemNames[kv.Key] = MiniJson.Str(kv.Value, kv.Key);
    }

    static void LoadLore(string path)
    {
        var root = MiniJson.Dict(MiniJson.Parse(ReadAll(path)));
        var st = new List<string>();
        foreach (object o in MiniJson.List(root.ContainsKey("stele") ? root["stele"] : null))
            st.Add(MiniJson.Str(o));
        SteleTexts = st.ToArray();

        foreach (object o in MiniJson.List(root.ContainsKey("shrines") ? root["shrines"] : null))
        {
            var sd = MiniJson.Dict(o);
            var s = new ShrineDef();
            s.id = MiniJson.Str(sd.ContainsKey("id") ? sd["id"] : null, "");
            s.name = MiniJson.Str(sd.ContainsKey("name") ? sd["name"] : null, "");
            s.desc = MiniJson.Str(sd.ContainsKey("desc") ? sd["desc"] : null, "");
            s.bonus = MiniJson.Sub(sd, "bonus");
            Shrines.Add(s);
        }

        var spawn = MiniJson.List(root.ContainsKey("spawn") ? root["spawn"] : null);
        if (spawn.Count >= 2) Spawn = new Vector2(MiniJson.F(spawn[0]), MiniJson.F(spawn[1]));
    }

    // ------------------------------------------------------------ 世界数学
    static int[] PERM;

    static void InitPerm()
    {
        var p = new int[256];
        for (int k = 0; k < 256; k++) p[k] = k;
        var rng = new System.Random(0x5EED2026);
        for (int k = 255; k >= 0; k--)
        {
            int j = rng.Next(k + 1);
            int tmp = p[k]; p[k] = p[j]; p[j] = tmp;
        }
        PERM = new int[512];
        for (int k = 0; k < 512; k++) PERM[k] = p[k & 255];
    }

    static float Hash2(int ix, int iz)
    {
        return PERM[(PERM[ix & 255] + iz) & 255] / 255f;
    }

    public static float Noise2(float x, float z)
    {
        int ix = Mathf.FloorToInt(x);
        int iz = Mathf.FloorToInt(z);
        float fx = x - ix, fz = z - iz;
        float ux = fx * fx * (3f - 2f * fx);
        float uz = fz * fz * (3f - 2f * fz);
        float a = Hash2(ix, iz);
        float b = Hash2(ix + 1, iz);
        float c = Hash2(ix, iz + 1);
        float d = Hash2(ix + 1, iz + 1);
        float l1 = a + (b - a) * ux;
        float l2 = c + (d - c) * ux;
        return (l1 + (l2 - l1) * uz) * 2f - 1f;
    }

    public static float Fbm(float x, float z, int oct, float lac, float gain)
    {
        float amp = 1f, freq = 1f, sum = 0f, norm = 0f;
        for (int k = 0; k < oct; k++)
        {
            sum += amp * Noise2(x * freq, z * freq);
            norm += amp;
            amp *= gain;
            freq *= lac;
        }
        return sum / Mathf.Max(1e-6f, norm);
    }

    public static float Smoother(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    public static float LakeEllipse(float x, float z)
    {
        return Mathf.Sqrt(x * x + ((z + 540f) / 1.35f) * ((z + 540f) / 1.35f));
    }

    static float DirWin(float aRad, float centerDeg, float halfDeg)
    {
        float d = (aRad * 57.2957795f - centerDeg) % 360f;
        if (d < 0f) d += 360f;
        if (d > 180f) d = 360f - d;
        return Smoother((halfDeg - d) / 12f);
    }

    public static float HeightAt(float x, float z)
    {
        float r = Mathf.Sqrt(x * x + z * z);
        float edge = Smoother((3900f - r) / 220f);
        float h = 4f * edge - 16f * (1f - edge);

        float le = LakeEllipse(x, z);
        float lake = Smoother((200f - le) / 130f);
        h = h * (1f - lake) + (-14f) * lake;
        h += 10f * Mathf.Exp(-((le - 330f) * (le - 330f)) / (2f * 95f * 95f)) * edge;

        float pk = Mathf.Exp(-(r * r) / (2f * 265f * 265f)) * (1f - lake);
        h += 126f * pk * edge;

        float flat = Smoother((r - 62f) / 55f);
        h = 128.5f * flat + h * (1f - flat);

        float er = Mathf.Sqrt(x * x + (z - 640f) * (z - 640f));
        h += 62f * Mathf.Exp(-((er - 235f) * (er - 235f)) / (2f * 105f * 105f)) * edge;

        float fr = Mathf.Sqrt((x + 620f) * (x + 620f) + z * z);
        h += 36f * Mathf.Exp(-((fr - 175f) * (fr - 175f)) / (2f * 95f * 95f)) * edge;
        h -= 16f * Mathf.Exp(-(fr * fr) / (2f * 85f * 85f)) * edge;
        h += 27f * Mathf.Exp(-((x - 620f) * (x - 620f) + z * z) / (2f * 290f * 290f)) * edge;

        h += 15f * Fbm(x * 0.0042f + 37f, z * 0.0042f, 3, 2f, 0.5f) * edge;
        h += 17f * Fbm(x * 0.008f, z * 0.008f, 4, 2f, 0.5f) * edge;

        float a = Mathf.Atan2(z, x);
        float band = Smoother((r - 900f) / 260f) * Smoother((3700f - r) / 400f);
        float des = band * DirWin(a, 0f, 46f);
        h += 10f * des + 6f * des * Fbm(x * 0.005f + 31f, z * 0.005f, 3, 2f, 0.5f);
        float tun = band * DirWin(a, 270f, 46f);
        h += 9f * tun + 5f * tun * Fbm(x * 0.004f + 77f, z * 0.004f, 3, 2f, 0.5f);
        float tr = Mathf.Sqrt(x * x + (z + 1350f) * (z + 1350f));
        h += 20f * Mathf.Exp(-(tr * tr) / (2f * 430f * 430f)) * tun;
        float tr2 = Mathf.Sqrt(x * x + (z + 2700f) * (z + 2700f));
        h += 26f * Mathf.Exp(-(tr2 * tr2) / (2f * 380f * 380f)) * tun;
        float ash = band * DirWin(a, 180f, 41f);
        h += 7f * ash + 4f * ash * Fbm(x * 0.006f + 13f, z * 0.006f, 3, 2f, 0.5f);
        float ar = Mathf.Sqrt((x + 1350f) * (x + 1350f) + z * z);
        h += 24f * Mathf.Exp(-(ar * ar) / (2f * 340f * 340f)) * ash;
        float ar2 = Mathf.Sqrt((x + 2700f) * (x + 2700f) + z * z);
        h += 22f * Mathf.Exp(-(ar2 * ar2) / (2f * 320f * 320f)) * ash;
        float can = DirWin(a, 90f, 40f) * Smoother((r - 850f) / 200f);
        h += 8f * can;
        h -= 30f * Mathf.Exp(-((z - 1180f) * (z - 1180f)) / (2f * 55f * 55f)) * can;
        h -= 26f * Mathf.Exp(-((z - 2600f) * (z - 2600f)) / (2f * 50f * 50f)) * can;
        return h;
    }

    public static string RegionAt(float x, float z)
    {
        float r = Mathf.Sqrt(x * x + z * z);
        if (r < 190f) return "peak";
        if (LakeEllipse(x, z) < 340f && z < -220f) return "lake";
        float a = Mathf.Atan2(z, x) * 57.2957795f;
        a = (a + 360f) % 360f;
        if (a > 45f && a < 135f && r < 900f) return "echo";
        if (a > 135f && a < 225f && r < 900f) return "forge";
        if (r > 330f && r < 560f && a > 225f && a < 315f) return "lake";
        if (r > 320f && r < 860f && (a <= 45f || a >= 315f)) return "forest";
        if (r > 900f && r < 3800f)
        {
            if (a <= 46f || a >= 318f) return "desert";
            if (a > 220f && a < 316f) return "tundra";
            if (a > 136f && a < 218f) return "ash";
            if (a > 50f && a < 130f) return "canyon";
        }
        return "plains";
    }

    public static Color RegionColor(string region)
    {
        switch (region)
        {
            case "forest": return new Color(0.13f, 0.22f, 0.12f);
            case "echo": return new Color(0.30f, 0.26f, 0.21f);
            case "forge": return new Color(0.32f, 0.20f, 0.13f);
            case "desert": return new Color(0.48f, 0.38f, 0.22f);
            case "tundra": return new Color(0.50f, 0.58f, 0.62f);
            case "ash": return new Color(0.26f, 0.24f, 0.22f);
            case "canyon": return new Color(0.20f, 0.19f, 0.26f);
            case "lake": return new Color(0.14f, 0.21f, 0.26f);
            case "peak": return new Color(0.35f, 0.30f, 0.25f);
            default: return new Color(0.26f, 0.30f, 0.17f);
        }
    }

    public static bool InLake(float x, float z)
    {
        return LakeEllipse(x, z) < 205f;
    }

    public static float WaterAt(float x, float z)
    {
        return InLake(x, z) ? 7f : 0f;
    }
}
