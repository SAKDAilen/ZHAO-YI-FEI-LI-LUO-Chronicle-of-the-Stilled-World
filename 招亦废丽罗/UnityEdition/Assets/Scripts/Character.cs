// Character.cs —— 青铜旅人骨骼（~90 部件）+ 动画驱动 + 三型敌人
using System.Collections.Generic;
using UnityEngine;

public struct AnimState
{
    public float speed, accel, yaw, camYaw;
    public Vector3 vel;
    public bool onGround, swimming, dead, hasWeapon, bowAim;
    public string state;
    public float swingT;      // <0 表示没有剑击
    public int swingIdx;
    public float rollT, jumpT, landT, flinchT, turnRate;
}

public class Rig
{
    public Transform root;
    public Dictionary<string, Transform> refs = new Dictionary<string, Transform>();
    public List<Part> parts = new List<Part>();
    public CapeSim cape;
    public Transform capeRoot;
    public MeshRenderer capeRenderer;
    public float walkPhase, gaitW, carryW, deathT, idleT, swimPhase, landMax, time;
    public System.Action<int> stepCb;
    float prevSin;

    // ---- 调色板（与原生版一致）
    public static readonly Color BRONZE = new Color(0.54f, 0.32f, 0.13f);
    public static readonly Color BRONZE_D = new Color(0.26f, 0.16f, 0.09f);
    public static readonly Color BRONZE_B = new Color(0.80f, 0.58f, 0.26f);
    public static readonly Color RUST = new Color(0.40f, 0.21f, 0.11f);
    public static readonly Color CLOTH = new Color(0.16f, 0.14f, 0.12f);
    public static readonly Color CLOTH_D = new Color(0.09f, 0.08f, 0.07f);
    public static readonly Color LEATHER = new Color(0.26f, 0.17f, 0.10f);
    public static readonly Color GLOW = new Color(0.30f, 0.92f, 1.00f);
    public static readonly Color GLOW_GOLD = new Color(1.00f, 0.74f, 0.28f);
    public static readonly Color CRYSTAL = new Color(0.62f, 0.92f, 1.00f);

    Material M(Color c) { return Mats.Get(c, Color.black); }
    Material ME(Color c, Color e) { return Mats.Get(c, e); }

    Transform Add(string name, Transform parent, Vector3 pos)
    {
        var t = Make.Node(name, parent, pos);
        refs[name] = t;
        return t;
    }

    Part P(string name, string node, Mesh mesh, Color col, Vector3 pos, Vector3 euler, Vector3 scale)
    {
        var p = Make.PartOn(refs[node], name, mesh, M(col), pos, euler, scale);
        parts.Add(p);
        return p;
    }

    Part PE(string name, string node, Mesh mesh, Color col, Color emis, Vector3 pos, Vector3 euler, Vector3 scale)
    {
        var p = Make.PartOn(refs[node], name, mesh, ME(col, emis), pos, euler, scale);
        parts.Add(p);
        return p;
    }

    // ------------------------------------------------------------ 构建
    public static Rig BuildPlayer(Transform parent, Vector3 pos, float yaw)
    {
        var r = new Rig();
        r.root = Make.Node("青铜旅人", parent, pos);
        r.root.localRotation = Quaternion.Euler(0f, yaw * Mathf.Rad2Deg, 0f);

        var pelvis = r.Add("pelvis", r.root, new Vector3(0f, 1.00f, 0f));
        r.P("盆骨", "pelvis", Geo.Box(), BRONZE_D, Vector3.zero, Vector3.zero, new Vector3(0.36f, 0.16f, 0.26f));
        r.P("腰带", "pelvis", Geo.Box(), LEATHER, new Vector3(0f, 0.10f, 0f), Vector3.zero, new Vector3(0.40f, 0.06f, 0.29f));
        // 六片战裙
        for (int k = 0; k < 6; k++)
        {
            float a = -70f + k * 28f;
            float rad = a * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(rad) * 0.17f, -0.15f, Mathf.Cos(rad) * 0.13f);
            r.P("战裙" + k, "pelvis", Geo.Box(), k % 2 == 0 ? CLOTH : CLOTH_D, p,
                new Vector3(8f, -a, 0f), new Vector3(0.11f, 0.28f, 0.03f));
        }
        r.PE("腰饰", "pelvis", Geo.Octa(), GLOW_GOLD, new Color(0.7f, 0.45f, 0.12f),
            new Vector3(0.16f, -0.03f, -0.13f), Vector3.zero, new Vector3(0.05f, 0.07f, 0.05f));
        r.P("腰包", "pelvis", Geo.Box(), LEATHER, new Vector3(-0.19f, -0.02f, -0.02f), Vector3.zero,
            new Vector3(0.08f, 0.12f, 0.09f));

        var torso = r.Add("torso", pelvis, new Vector3(0f, 0.10f, 0f));
        r.P("腹甲", "torso", Geo.Box(), BRONZE, new Vector3(0f, 0.08f, 0f), Vector3.zero, new Vector3(0.30f, 0.16f, 0.24f));
        r.P("腹环1", "torso", Geo.Cylinder(10, false), BRONZE_D, new Vector3(0f, 0.02f, 0f), Vector3.zero,
            new Vector3(0.30f, 0.05f, 0.24f));
        r.P("腹环2", "torso", Geo.Cylinder(10, false), BRONZE_D, new Vector3(0f, 0.13f, 0f), Vector3.zero,
            new Vector3(0.31f, 0.05f, 0.25f));

        var chest = r.Add("chest", torso, new Vector3(0f, 0.12f, 0f));
        r.P("胸廓", "chest", Geo.Box(), BRONZE, new Vector3(0f, 0.17f, 0f), Vector3.zero, new Vector3(0.46f, 0.34f, 0.30f));
        r.P("胸甲", "chest", Geo.Box(), BRONZE_B, new Vector3(0f, 0.20f, -0.16f), Vector3.zero, new Vector3(0.40f, 0.22f, 0.05f));
        r.P("胸甲沿", "chest", Geo.Box(), BRONZE_D, new Vector3(0f, 0.09f, -0.16f), Vector3.zero, new Vector3(0.34f, 0.04f, 0.06f));
        r.P("领圈", "chest", Geo.Cylinder(12, true), BRONZE_D, new Vector3(0f, 0.36f, 0f), Vector3.zero,
            new Vector3(0.34f, 0.06f, 0.30f));
        r.PE("胸徽灵核", "chest", Geo.Sphere(12, 8), GLOW_GOLD, new Color(0.95f, 0.62f, 0.20f),
            new Vector3(0f, 0.21f, -0.18f), Vector3.zero, new Vector3(0.11f, 0.11f, 0.06f));
        for (int k = 0; k < 4; k++)
        {
            r.P("铆钉" + k, "chest", Geo.Sphere(6, 4), BRONZE_B,
                new Vector3((k % 2 == 0 ? -1f : 1f) * 0.17f, 0.20f, -0.185f), Vector3.zero, Vector3.one * 0.022f);
        }
        r.PE("背甲水晶", "chest", Geo.Octa(), CRYSTAL, new Color(0.35f, 0.85f, 1.0f),
            new Vector3(0f, 0.24f, 0.17f), new Vector3(0f, 0f, 0f), new Vector3(0.10f, 0.14f, 0.10f));
        r.P("背甲", "chest", Geo.Box(), BRONZE_D, new Vector3(0f, 0.14f, 0.16f), Vector3.zero, new Vector3(0.32f, 0.28f, 0.05f));
        for (int k = 0; k < 2; k++)
            r.P("排气口" + k, "chest", Geo.Box(), BRONZE_D, new Vector3((k == 0 ? -0.10f : 0.10f), 0.02f, 0.18f),
                Vector3.zero, new Vector3(0.07f, 0.03f, 0.04f));

        var neck = r.Add("neck", chest, new Vector3(0f, 0.28f, 0f));
        r.P("颈", "neck", Geo.Cylinder(10, true), BRONZE_D, new Vector3(0f, 0.06f, 0f), Vector3.zero,
            new Vector3(0.13f, 0.14f, 0.13f));
        var head = r.Add("head", neck, new Vector3(0f, 0.12f, 0f));
        r.P("头骨", "head", Geo.Box(), BRONZE, new Vector3(0f, 0.10f, 0f), Vector3.zero, new Vector3(0.26f, 0.24f, 0.24f));
        r.P("面罩", "head", Geo.Box(), BRONZE_D, new Vector3(0f, 0.09f, -0.12f), Vector3.zero, new Vector3(0.22f, 0.18f, 0.04f));
        r.PE("眼缝", "head", Geo.Box(), GLOW_GOLD, new Color(1.0f, 0.82f, 0.42f),
            new Vector3(0f, 0.11f, -0.135f), Vector3.zero, new Vector3(0.18f, 0.025f, 0.02f));
        r.P("面颊蚀刻", "head", Geo.Box(), RUST, new Vector3(-0.09f, 0.03f, -0.13f), new Vector3(0f, 0f, 18f),
            new Vector3(0.05f, 0.012f, 0.02f));
        r.P("面颊蚀刻2", "head", Geo.Box(), RUST, new Vector3(0.09f, 0.03f, -0.13f), new Vector3(0f, 0f, -18f),
            new Vector3(0.05f, 0.012f, 0.02f));
        r.P("角左", "head", Geo.Cone(6), BRONZE_B, new Vector3(-0.09f, 0.24f, 0.02f), new Vector3(0f, 0f, 16f),
            new Vector3(0.05f, 0.16f, 0.05f));
        r.P("角右", "head", Geo.Cone(6), BRONZE_B, new Vector3(0.09f, 0.24f, 0.02f), new Vector3(0f, 0f, -16f),
            new Vector3(0.05f, 0.16f, 0.05f));
        for (int k = 0; k < 3; k++)
            r.P("发丝" + k, "head", Geo.Box(), BRONZE_D, new Vector3((k - 1) * 0.06f, 0.16f, 0.13f),
                new Vector3(-16f, 0f, (k - 1) * 12f), new Vector3(0.03f, 0.10f, 0.02f));

        // 肩 / 臂
        string[] sides = { "L", "R" };
        int[] sgn = { -1, 1 };
        for (int i = 0; i < 2; i++)
        {
            string s = sides[i];
            int g = sgn[i];
            var sh = r.Add("shoulder" + s, chest, new Vector3(g * 0.27f, 0.16f, 0f));
            r.P("肩甲1" + s, "shoulder" + s, Geo.Sphere(12, 8), BRONZE, new Vector3(g * 0.02f, 0.03f, 0f),
                Vector3.zero, new Vector3(0.28f, 0.22f, 0.26f));
            r.P("肩甲2" + s, "shoulder" + s, Geo.Box(), BRONZE_D, new Vector3(g * 0.05f, 0.10f, 0f),
                new Vector3(0f, 0f, g * 18f), new Vector3(0.20f, 0.10f, 0.20f));
            r.P("肩刺" + s, "shoulder" + s, Geo.Cone(5), BRONZE_B, new Vector3(g * 0.12f, 0.14f, 0f),
                new Vector3(0f, 0f, g * -28f), new Vector3(0.05f, 0.16f, 0.05f));
            r.P("上臂" + s, "shoulder" + s, Geo.Cylinder(8, true), BRONZE_D, new Vector3(0f, -0.17f, 0f),
                Vector3.zero, new Vector3(0.15f, 0.32f, 0.15f));
            var el = r.Add("elbow" + s, sh, new Vector3(0f, -0.34f, 0f));
            r.P("肘球" + s, "elbow" + s, Geo.Sphere(10, 6), BRONZE_B, Vector3.zero, Vector3.zero,
                new Vector3(0.17f, 0.15f, 0.17f));
            r.P("前臂" + s, "elbow" + s, Geo.Cylinder(8, true), BRONZE, new Vector3(0f, -0.14f, 0f),
                Vector3.zero, new Vector3(0.13f, 0.28f, 0.13f));
            r.P("护腕" + s, "elbow" + s, Geo.Cylinder(10, true), BRONZE_B, new Vector3(0f, -0.24f, 0f),
                Vector3.zero, new Vector3(0.16f, 0.14f, 0.16f));
            for (int k = 0; k < 4; k++)
                r.P("腕钉" + s + k, "elbow" + s, Geo.Sphere(5, 4), BRONZE_D,
                    new Vector3(Mathf.Cos(k * 1.57f) * 0.085f, -0.24f, Mathf.Sin(k * 1.57f) * 0.085f),
                    Vector3.zero, Vector3.one * 0.018f);
            var hand = r.Add("hand" + s, el, new Vector3(0f, -0.30f, 0f));
            r.P("手掌" + s, "hand" + s, Geo.Box(), BRONZE_D, new Vector3(0f, -0.04f, 0f), Vector3.zero,
                new Vector3(0.10f, 0.12f, 0.14f));
            for (int k = 0; k < 3; k++)
                r.P("指" + s + k, "hand" + s, Geo.Box(), BRONZE, new Vector3((k - 1) * 0.032f, -0.12f, -0.03f),
                    new Vector3(-14f, 0f, 0f), new Vector3(0.026f, 0.08f, 0.03f));
            r.P("拇指" + s, "hand" + s, Geo.Box(), BRONZE, new Vector3(g * 0.06f, -0.07f, 0.02f),
                new Vector3(0f, 0f, g * 26f), new Vector3(0.026f, 0.06f, 0.03f));
            r.P("腕环" + s, "hand" + s, Geo.Cylinder(10, true), BRONZE_B, new Vector3(0f, 0.02f, 0f),
                Vector3.zero, new Vector3(0.13f, 0.03f, 0.13f));

            // 腿
            var hip = r.Add("hip" + s, pelvis, new Vector3(g * 0.15f, -0.06f, 0f));
            r.P("大腿" + s, "hip" + s, Geo.Cylinder(8, true), CLOTH_D, new Vector3(0f, -0.20f, 0f),
                Vector3.zero, new Vector3(0.20f, 0.40f, 0.20f));
            r.P("腿甲" + s, "hip" + s, Geo.Box(), BRONZE, new Vector3(0f, -0.16f, -0.09f), Vector3.zero,
                new Vector3(0.18f, 0.24f, 0.05f));
            var knee = r.Add("knee" + s, hip, new Vector3(0f, -0.44f, 0f));
            r.P("膝球" + s, "knee" + s, Geo.Sphere(10, 6), BRONZE_B, Vector3.zero, Vector3.zero,
                new Vector3(0.20f, 0.18f, 0.20f));
            r.P("小腿" + s, "knee" + s, Geo.Cylinder(8, true), BRONZE_D, new Vector3(0f, -0.19f, 0f),
                Vector3.zero, new Vector3(0.17f, 0.38f, 0.17f));
            r.P("胫甲" + s, "knee" + s, Geo.Box(), BRONZE, new Vector3(0f, -0.18f, -0.08f), Vector3.zero,
                new Vector3(0.15f, 0.28f, 0.04f));
            var ankle = r.Add("ankle" + s, knee, new Vector3(0f, -0.40f, 0f));
            r.P("踝环" + s, "ankle" + s, Geo.Cylinder(10, true), BRONZE_B, Vector3.zero, Vector3.zero,
                new Vector3(0.17f, 0.05f, 0.17f));
            r.P("靴" + s, "ankle" + s, Geo.Box(), LEATHER, new Vector3(0f, -0.045f, -0.03f), Vector3.zero,
                new Vector3(0.16f, 0.09f, 0.26f));
            r.P("靴头" + s, "ankle" + s, Geo.Box(), BRONZE_D, new Vector3(0f, -0.05f, -0.15f), Vector3.zero,
                new Vector3(0.14f, 0.06f, 0.08f));
        }

        // 披风
        var capeRoot = r.Add("capeRoot", chest, new Vector3(0f, 0.24f, 0.13f));
        capeRoot.localRotation = Quaternion.Euler(20f, 0f, 0f);
        r.capeRoot = capeRoot;
        r.cape = new CapeSim(GameData.Cape);
        var capeMat = Mats.Get(new Color(0.20f, 0.17f, 0.15f), new Color(0.03f, 0.05f, 0.06f));
        capeMat.color = new Color(0.30f, 0.24f, 0.20f);
        var capeGo = Make.MeshObject("披风", capeRoot, r.cape.mesh, capeMat, false);
        r.capeRenderer = capeGo.GetComponent<MeshRenderer>();
        var t = capeGo.transform;
        t.localPosition = Vector3.zero;

        // 剑（挂在右手）
        var sword = Make.Node("剑", r.refs["handR"], new Vector3(0f, -0.06f, 0f));
        sword.localRotation = Quaternion.Euler(0f, 0f, 0f);
        r.SwordPart(sword, "剑刃", Geo.Box(), new Color(0.30f, 0.33f, 0.36f), new Vector3(0f, -0.50f, 0f),
            Vector3.zero, new Vector3(0.055f, 0.90f, 0.014f));
        r.SwordPart(sword, "剑血槽", Geo.Box(), GLOW, new Vector3(0f, -0.50f, -0.008f), Vector3.zero,
            new Vector3(0.014f, 0.86f, 0.012f), new Color(0.20f, 0.70f, 0.85f));
        r.SwordPart(sword, "剑尖", Geo.Cone(4), new Color(0.34f, 0.37f, 0.40f), new Vector3(0f, -1.00f, 0f),
            new Vector3(180f, 0f, 0f), new Vector3(0.05f, 0.12f, 0.014f));
        r.SwordPart(sword, "护手", Geo.Box(), BRONZE_B, new Vector3(0f, -0.06f, 0f), Vector3.zero,
            new Vector3(0.20f, 0.035f, 0.05f));
        r.SwordPart(sword, "握柄", Geo.Cylinder(8, true), LEATHER, new Vector3(0f, 0.01f, 0f), Vector3.zero,
            new Vector3(0.045f, 0.16f, 0.045f));
        r.SwordPart(sword, "剑首", Geo.Sphere(8, 6), BRONZE_B, new Vector3(0f, 0.10f, 0f), Vector3.zero,
            new Vector3(0.07f, 0.05f, 0.07f));

        return r;
    }

    void SwordPart(Transform sword, string name, Mesh mesh, Color col, Vector3 pos, Vector3 euler, Vector3 scale,
                   Color emissive = default(Color))
    {
        var p = Make.PartOn(sword, name, mesh, Mats.Get(col, emissive), pos, euler, scale);
        parts.Add(p);
    }

    public Transform T(string key)
    {
        Transform t;
        return refs.TryGetValue(key, out t) ? t : root;
    }

    GaitData Gait(string key)
    {
        GaitData g;
        if (GameData.Gaits.TryGetValue(key, out g)) return g;
        if (GameData.Gaits.TryGetValue("walk", out g)) return g;
        return null;
    }

    float SampleCh(GaitData g, float phase, string key)
    {
        return g == null ? 0f : g.Sample(key, phase);
    }

    public void SetRoot(Vector3 pos, float yaw)
    {
        root.localPosition = pos;
        root.localRotation = Quaternion.Euler(0f, yaw * Mathf.Rad2Deg, 0f);
    }

    // ------------------------------------------------------------ 动画
    public void Animate(float dt, AnimState st)
    {
        time += dt;
        float spd = st.speed;
        float gaitG = 4f + spd * 8.5f;
        walkPhase = Mathf.Repeat(walkPhase + dt * gaitG, Mathf.PI * 2f);
        idleT += dt;
        if (st.swimming) swimPhase = Mathf.Repeat(swimPhase + dt * 2.6f, Mathf.PI * 2f);
        float gTarget = Mathf.Clamp(spd / 10.5f, 0f, 1.15f) * 1.12f;
        gaitW += (Mathf.Min(1f, gTarget) - gaitW) * Mathf.Min(1f, dt * 6f);

        // 后退 / 侧移混合
        float yaw = st.yaw;
        float fx = -Mathf.Sin(yaw), fz = -Mathf.Cos(yaw);
        float vlen = Mathf.Sqrt(st.vel.x * st.vel.x + st.vel.z * st.vel.z);
        float backW = 0f, sideW = 0f;
        if (vlen > 0.5f)
        {
            float cosR = (st.vel.x * fx + st.vel.z * fz) / vlen;
            backW = Mathf.Clamp01((-cosR - 0.35f) / 0.65f);
            sideW = Mathf.Clamp01((Mathf.Abs(st.vel.x * (-fz) + st.vel.z * fx) / vlen - 0.55f) / 0.45f);
        }

        GaitData active;
        if (st.swimming) active = Gait("swim") ?? Gait("run");
        else if (backW > 0.05f) active = Gait("back") ?? Gait("walk");
        else if (sideW > 0.05f) active = Gait("strafe") ?? Gait("walk");
        else if (spd < 0.4f) active = Gait("idle");
        else if (spd < 6.6f) active = Gait("walk");
        else if (spd < 11.2f) active = Gait("run");
        else active = Gait("sprint");

        GaitData idle = Gait("idle");
        GaitData alt = backW >= sideW ? (Gait("back") ?? Gait("walk")) : (Gait("strafe") ?? Gait("walk"));
        float mixdir = Mathf.Max(backW, sideW);

        float Ch(string key)
        {
            float v = Mathf.Lerp(SampleCh(idle, walkPhase * 0.5f, key), SampleCh(active, walkPhase, key), gaitW);
            if (mixdir > 0.02f && key != "bob" && key != "lean")
                v = v * (1f - mixdir) + SampleCh(alt, walkPhase, key) * gaitW * mixdir;
            return v;
        }

        // 一次性姿态
        float jt = st.jumpT, lt = st.landT, ft = st.flinchT;
        OneShot jumpPose = null, landPose = null, flinchPose = null;
        if (jt > 0.001f && GameData.OneShots.TryGetValue("jump", out jumpPose)) { }
        if (lt > 0.001f && GameData.OneShots.TryGetValue("land", out landPose)) { }
        if (ft > 0.001f && GameData.OneShots.TryGetValue("flinch", out flinchPose)) { }

        // 骨盆
        Transform pelvis = T("pelvis");
        float bob = Ch("bob"), sway = Ch("sway"), roll = Ch("roll"), lean = Ch("lean"), shift = Ch("shift");
        float accelLean = Mathf.Clamp(st.accel * 0.008f, -17f, 17f);
        float py = 1.00f + bob * gaitW;
        if (jumpPose != null) py += jumpPose.At("sink", 1f - jt) * (jt > 0.001f ? 1f : 0f);
        if (landPose != null) py += landPose.At("sink", 1f - lt / Mathf.Max(0.01f, landMax)) * (lt > 0.001f ? 1f : 0f);
        if (flinchPose != null) py += flinchPose.At("sink", 1f - ft / 0.32f) * (ft > 0.001f ? 1f : 0f);
        pelvis.localPosition = new Vector3(shift * 0.04f, py, 0f);
        pelvis.localRotation = Quaternion.Euler(
            (lean * 0.9f + accelLean * 0.012f) * Mathf.Rad2Deg,
            sway * 0.6f * Mathf.Rad2Deg,
            -roll * 0.6f * Mathf.Rad2Deg);

        // 根部俯仰（死亡 / 翻滚）
        float rootPitch = 0f;
        if (st.dead) deathT = Mathf.Min(1f, deathT + dt * 1.4f);
        else deathT = Mathf.Max(0f, deathT - dt * 2f);
        rootPitch = -deathT * Mathf.PI * 0.42f;
        if (st.rollT > 0f) rootPitch = -st.rollT * Mathf.PI * 2f;
        root.localRotation = Quaternion.Euler(rootPitch * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg, 0f);

        // 躯干 / 胸
        Transform torso = T("torso");
        float tx = -lean * 0.55f + accelLean * 0.010f;
        if (jumpPose != null && jt > 0.001f) tx += jumpPose.At("torso", 1f - jt) * 0.6f;
        if (landPose != null && lt > 0.001f) tx += landPose.At("torso", 1f - lt / Mathf.Max(0.01f, landMax)) * 0.9f;
        torso.localRotation = Quaternion.Euler(tx * Mathf.Rad2Deg, -sway * 0.5f * Mathf.Rad2Deg, roll * 0.15f * Mathf.Rad2Deg);
        Transform chest = T("chest");
        chest.localRotation = Quaternion.Euler(-lean * 0.2f * Mathf.Rad2Deg,
            -pelvis.localEulerAngles.z * 0.45f, -roll * 0.1f * Mathf.Rad2Deg);
        if (st.swimming)
            torso.localRotation = Quaternion.Euler(0.75f * Mathf.Rad2Deg, 0f, Mathf.Sin(swimPhase * 2f) * 0.08f * Mathf.Rad2Deg);

        // 腿
        float s = Mathf.Sin(walkPhase * Mathf.PI * 2f);
        if (st.onGround && !st.swimming && !st.dead && (st.state == "walk" || st.state == "run" || st.state == "sprint")
            && gaitW > 0.3f)
        {
            if (prevSin < 0f && s >= 0f) PlantFoot(-1);
            else if (prevSin > 0f && s <= 0f) PlantFoot(1);
        }
        prevSin = s;

        for (int i = 0; i < 2; i++)
        {
            string side = i == 0 ? "L" : "R";
            float hx = Ch("hip" + side);
            float kn = Ch("knee" + side);
            float an = Ch("ankle" + side);
            if (jumpPose != null && jt > 0.001f)
            {
                hx += jumpPose.At("hip" + side, 1f - jt);
                kn += jumpPose.At("knee" + side, 1f - jt);
                an += jumpPose.At("foot" + side, 1f - jt);
            }
            if (landPose != null && lt > 0.001f)
            {
                float w = lt / Mathf.Max(0.01f, landMax);
                hx += landPose.At("hip" + side, 1f - w) * w;
                kn += landPose.At("knee" + side, 1f - w) * w;
                an += landPose.At("foot" + side, 1f - w) * w;
            }
            if (flinchPose != null && ft > 0.001f) hx += flinchPose.At("hip" + side, 1f - ft / 0.32f) * 0.3f;
            float kneeRot = -kn;
            float hipRot = hx;
            if (!st.onGround && !st.swimming) { hipRot = hipRot * 0.5f - 0.25f; kneeRot = -Mathf.Max(0.5f, -kneeRot); }
            if (st.swimming)
            {
                float ph = swimPhase + (side == "L" ? 0f : Mathf.PI);
                hipRot += Mathf.Sin(ph) * 0.35f;
                kneeRot = -0.25f + Mathf.Cos(ph) * 0.15f;
            }
            if (st.rollT > 0f) { hipRot = 1.4f; kneeRot = -1.9f; }
            T("hip" + side).localRotation = Quaternion.Euler(hipRot * Mathf.Rad2Deg, 0f, side == "L" ? 2f : -2f);
            T("knee" + side).localRotation = Quaternion.Euler(kneeRot * Mathf.Rad2Deg, 0f, 0f);
            T("ankle" + side).localRotation = Quaternion.Euler((an + (st.onGround ? 0f : 0.25f)) * Mathf.Rad2Deg, 0f, 0f);
        }

        // 手臂
        bool carry = st.hasWeapon && !st.bowAim && st.swingT < 0f;
        carryW += ((carry ? 1f : 0f) - carryW) * Mathf.Min(1f, dt * 7f);
        float cw = carryW;
        for (int i = 0; i < 2; i++)
        {
            string side = i == 0 ? "L" : "R";
            float g = side == "L" ? -1f : 1f;
            float ax = Ch("arm" + side);
            float az = 0f;
            float el = Ch("elbow" + side);
            if (jumpPose != null && jt > 0.001f)
            {
                ax += jumpPose.At("arm" + side, 1f - jt);
                el += jumpPose.At("elbow" + side, 1f - jt);
            }
            if (landPose != null && lt > 0.001f)
            {
                ax += landPose.At("arm" + side, 1f - lt / Mathf.Max(0.01f, landMax)) * 0.8f;
                el += landPose.At("elbow" + side, 1f - lt / Mathf.Max(0.01f, landMax)) * 0.8f;
            }
            if (flinchPose != null && ft > 0.001f)
            {
                ax += flinchPose.At("arm" + side, 1f - ft / 0.32f);
                el += flinchPose.At("elbow" + side, 1f - ft / 0.32f);
            }
            if (st.swimming)
            {
                ax = -1.5f + Mathf.Sin(swimPhase + (side == "L" ? 0f : Mathf.PI)) * 1.2f;
                az = side == "L" ? 0.5f : -0.5f;
                el = 0.6f + Mathf.Cos(swimPhase) * 0.4f;
            }
            float sx = ax, sz = (side == "L" ? -0.10f : 0.10f) + az;
            if (side == "R" && cw > 0.01f)
            {
                sx = sx * (1f - cw) + 0.35f * cw;
                sz = sz * (1f - cw) + 0.12f * cw;
                el = el * (1f - cw) + 0.55f * cw;
                if (spd > 11f) { sx = 1.35f; el = 0.45f; }
            }
            T("shoulder" + side).localRotation = Quaternion.Euler(sx * Mathf.Rad2Deg, 0f, sz * Mathf.Rad2Deg);
            T("elbow" + side).localRotation = Quaternion.Euler(el * Mathf.Rad2Deg, 0f, 0f);
            T("hand" + side).localRotation = Quaternion.Euler(ax * 0.12f * Mathf.Rad2Deg * (1f - cw * 0.7f), 0f, 0f);
        }

        // 剑击覆盖
        if (st.swingT >= 0f && GameData.Swings.Count > 0)
        {
            var arc = GameData.Swings[Mathf.Abs(st.swingIdx) % GameData.Swings.Count];
            float t01 = Mathf.Clamp01(st.swingT);
            float armX = arc.Pick(arc.armX, t01);
            float armY = arc.Pick(arc.armY, t01);
            float armZ = arc.Pick(arc.armZ, t01);
            float elbow = arc.Pick(arc.elbow, t01);
            float torsoY = arc.Pick(arc.torso, t01);
            float lunge = arc.Pick(arc.lunge, t01);
            T("shoulderR").localRotation = Quaternion.Euler(armX * Mathf.Rad2Deg, armY * Mathf.Rad2Deg, armZ * Mathf.Rad2Deg);
            T("elbowR").localRotation = Quaternion.Euler(elbow * Mathf.Rad2Deg, 0f, 0f);
            torso.localRotation = Quaternion.Euler(torso.localEulerAngles.x, torsoY * Mathf.Rad2Deg, torso.localEulerAngles.z);
            pelvis.localPosition = new Vector3(pelvis.localPosition.x, pelvis.localPosition.y, -Mathf.Abs(lunge) * 0.10f);
        }

        // 头部跟随相机 + 呼吸 + 待机微动作
        Transform head = T("head");
        float headY = Mathf.Clamp(Mathf.DeltaAngle(yaw * Mathf.Rad2Deg, st.camYaw * Mathf.Rad2Deg) * 0.45f, -38f, 38f);
        float breath = Mathf.Sin(idleT * 1.6f) * 1.2f;
        head.localRotation = Quaternion.Euler(breath, headY, 0f);
        if (gaitW < 0.06f && st.onGround && !st.swimming && st.state != "bow")
        {
            float idleT01 = Mathf.Repeat(idleT / 8f, 1f);
            float breath2 = SampleIdle("breath", idleT01);
            head.localRotation = Quaternion.Euler(breath + breath2 * 2f, headY + SampleIdle("headY", idleT01) * 8f,
                SampleIdle("headX", idleT01) * 3f);
            T("chest").localScale = new Vector3(1f + breath2 * 0.02f, 1f + breath2 * 0.02f, 1f);
        }

        // 披风
        if (cape != null && capeRoot != null)
        {
            Vector3 wind = new Vector3(
                (Mathf.Sin(time * 1.7f) * 1.1f + Mathf.Sin(time * 0.6f + 1.3f) * 0.8f) * 1.1f,
                0f,
                Mathf.Max(0f, -st.vel.z * 0.10f) + Mathf.Sin(time * 2.3f + 0.4f) * 0.7f);
            float turn = Mathf.Clamp(st.turnRate, -1.5f, 1.5f);
            wind.x += turn * 1.4f;
            cape.Step(dt, wind, new Vector3(0f, -GameData.Cape.gravity, 0f), GameData.Cape.stiffness);
        }
    }

    float SampleIdle(string key, float t01)
    {
        float[] arr;
        if (!GameData.Idle2.TryGetValue(key, out arr) || arr.Length == 0) return 0f;
        float t = t01 * (arr.Length - 1);
        int i0 = Mathf.FloorToInt(t);
        int i1 = Mathf.Min(arr.Length - 1, i0 + 1);
        return Mathf.Lerp(arr[i0], arr[i1], t - i0);
    }

    void PlantFoot(int side)
    {
        if (stepCb != null) stepCb(side);
    }

    public Vector3 SwordTip()
    {
        var hand = T("handR");
        // 剑尖在手局部坐标 (0,-1.06,0) 处
        return hand.TransformPoint(new Vector3(0f, -1.06f, 0f));
    }
}

// ---------------------------------------------------------------- 敌人
public class EnemyRig
{
    public Transform root;
    public string kind;
    public List<Part> parts = new List<Part>();
    public float anim;
    public int maxHp, hp;
    public float speed, radius, dmg, atkRange;
    public bool dead;
    public float deathT, hitT, attackT, animWalk;
    public Vector3 pos;
    public float yaw;
    public Transform wingL, wingR;

    public static readonly Color CONSTRUCT_MAIN = new Color(0.25f, 0.23f, 0.20f);
    public static readonly Color CONSTRUCT_ACCENT = new Color(0.56f, 0.37f, 0.16f);
    public static readonly Color CONSTRUCT_GLOW = new Color(0.45f, 0.95f, 1.0f);
    public static readonly Color WINGED_MAIN = new Color(0.20f, 0.19f, 0.24f);
    public static readonly Color WINGED_ACCENT = new Color(0.48f, 0.29f, 0.14f);
    public static readonly Color WINGED_GLOW = new Color(1.0f, 0.45f, 0.22f);
    public static readonly Color LAVA_MAIN = new Color(0.27f, 0.17f, 0.13f);
    public static readonly Color LAVA_ACCENT = new Color(0.52f, 0.21f, 0.09f);
    public static readonly Color LAVA_GLOW = new Color(1.0f, 0.38f, 0.09f);

    public static EnemyRig Build(string kind, Transform parent, Vector3 pos, int level)
    {
        var e = new EnemyRig();
        e.kind = kind;
        e.pos = pos;
        e.root = Make.Node("敌人_" + kind, parent, pos);
        Color main, accent, glow;
        if (kind == "winged") { main = WINGED_MAIN; accent = WINGED_ACCENT; glow = WINGED_GLOW; e.maxHp = Mathf.RoundToInt(55 * (0.6f + 0.4f * level)); e.speed = 4.6f; e.radius = 0.5f; e.dmg = 9; e.atkRange = 2.0f; }
        else if (kind == "lava") { main = LAVA_MAIN; accent = LAVA_ACCENT; glow = LAVA_GLOW; e.maxHp = Mathf.RoundToInt(120 * (0.6f + 0.4f * level)); e.speed = 2.2f; e.radius = 0.75f; e.dmg = 18; e.atkRange = 2.8f; }
        else { main = CONSTRUCT_MAIN; accent = CONSTRUCT_ACCENT; glow = CONSTRUCT_GLOW; e.maxHp = Mathf.RoundToInt(90 * (0.6f + 0.4f * level)); e.speed = 2.6f; e.radius = 0.55f; e.dmg = 14; e.atkRange = 2.4f; }
        e.hp = e.maxHp;

        Material mm = Mats.Get(main, Color.black);
        Material am = Mats.Get(accent, Color.black);
        Material gm = Mats.Get(glow, glow * 1.2f);

        void AddP(string name, Mesh mesh, Material mat, Vector3 p, Vector3 rot, Vector3 scale, Transform node = null)
        {
            var part = Make.PartOn(node == null ? e.root : node, name, mesh, mat, p, rot, scale);
            e.parts.Add(part);
        }

        if (kind == "construct")
        {
            AddP("盆骨", Geo.Box(), mm, new Vector3(0f, 0.98f, 0f), Vector3.zero, new Vector3(0.38f, 0.20f, 0.28f));
            AddP("躯干", Geo.Box(), mm, new Vector3(0f, 1.34f, 0f), Vector3.zero, new Vector3(0.60f, 0.64f, 0.36f));
            AddP("胸甲", Geo.Box(), am, new Vector3(0f, 1.44f, -0.21f), Vector3.zero, new Vector3(0.52f, 0.30f, 0.09f));
            AddP("颈", Geo.Cylinder(8, true), mm, new Vector3(0f, 1.70f, 0f), Vector3.zero, new Vector3(0.22f, 0.16f, 0.22f));
            AddP("头", Geo.Box(), mm, new Vector3(0f, 1.86f, 0f), Vector3.zero, new Vector3(0.25f, 0.26f, 0.25f));
            AddP("眼缝", Geo.Box(), gm, new Vector3(0f, 1.88f, -0.135f), Vector3.zero, new Vector3(0.21f, 0.05f, 0.03f));
            AddP("角", Geo.Cone(5), am, new Vector3(0f, 2.04f, 0f), Vector3.zero, new Vector3(0.05f, 0.16f, 0.05f));
            for (int k = 0; k < 2; k++)
            {
                float g = k == 0 ? -1f : 1f;
                AddP("肩" + k, Geo.Sphere(10, 6), am, new Vector3(g * 0.40f, 1.58f, 0f), Vector3.zero, Vector3.one * 0.30f);
                AddP("上臂" + k, Geo.Cylinder(6, true), mm, new Vector3(g * 0.42f, 1.34f, 0f), Vector3.zero, new Vector3(0.17f, 0.42f, 0.17f));
                AddP("拳" + k, Geo.Box(), am, new Vector3(g * 0.42f, 1.10f, 0f), Vector3.zero, new Vector3(0.18f, 0.18f, 0.18f));
                AddP("腿" + k, Geo.Box(), mm, new Vector3(g * 0.16f, 0.48f, 0f), Vector3.zero, new Vector3(0.20f, 0.90f, 0.22f));
                AddP("脚" + k, Geo.Box(), am, new Vector3(g * 0.16f, 0.06f, -0.05f), Vector3.zero, new Vector3(0.22f, 0.12f, 0.32f));
            }
            AddP("背排气", Geo.Box(), gm, new Vector3(0f, 1.10f, 0.19f), Vector3.zero, new Vector3(0.26f, 0.05f, 0.05f));
        }
        else if (kind == "winged")
        {
            AddP("身躯", Geo.Sphere(12, 8), mm, new Vector3(0f, 0.88f, 0f), Vector3.zero, new Vector3(0.52f, 0.62f, 0.90f));
            AddP("胸", Geo.Sphere(10, 6), mm, new Vector3(0f, 0.88f, -0.32f), Vector3.zero, new Vector3(0.48f, 0.48f, 0.48f));
            AddP("颈", Geo.Cylinder(8, true), mm, new Vector3(0f, 0.94f, -0.52f), new Vector3(-60f, 0f, 0f), new Vector3(0.24f, 0.30f, 0.24f));
            AddP("头", Geo.Sphere(10, 6), mm, new Vector3(0f, 1.00f, -0.66f), Vector3.zero, Vector3.one * 0.32f);
            AddP("喙", Geo.Cone(6), gm, new Vector3(0f, 0.98f, -0.82f), new Vector3(-97f, 0f, 0f), new Vector3(0.15f, 0.24f, 0.15f));
            AddP("尾", Geo.Cone(6), am, new Vector3(0f, 0.90f, 0.62f), new Vector3(103f, 0f, 0f), new Vector3(0.14f, 0.55f, 0.14f));
            AddP("核心", Geo.Sphere(10, 6), gm, new Vector3(0f, 1.06f, 0.08f), Vector3.zero, Vector3.one * 0.17f);
            for (int k = 0; k < 4; k++)
            {
                float g = (k % 2 == 0) ? -1f : 1f;
                float z = (k < 2) ? -0.30f : 0.34f;
                AddP("腿" + k, Geo.Box(), mm, new Vector3(g * 0.22f, 0.30f, z), Vector3.zero, new Vector3(0.10f, 0.56f, 0.12f));
                AddP("爪" + k, Geo.Box(), am, new Vector3(g * 0.22f, 0.04f, z - 0.05f), Vector3.zero, new Vector3(0.14f, 0.08f, 0.20f));
            }
            for (int k = 0; k < 2; k++)
            {
                float g = k == 0 ? -1f : 1f;
                var wing = Make.Node("翼" + k, e.root, new Vector3(g * 0.20f, 1.02f, 0.02f));
                if (k == 0) e.wingL = wing; else e.wingR = wing;
                AddP("翼膜" + k, Geo.Quad(), Mats.Get(new Color(0.22f, 0.22f, 0.28f), Color.black),
                    new Vector3(g * 0.55f, 0f, 0f), new Vector3(0f, 0f, g * -35f), new Vector3(1.06f, 0.50f, 1f), wing);
                AddP("翼骨" + k, Geo.Box(), am, new Vector3(g * 0.55f, 0.02f, 0f),
                    new Vector3(0f, 0f, g * -35f), new Vector3(1.10f, 0.05f, 0.07f), wing);
            }
        }
        else
        {
            AddP("躯干", Geo.Sphere(14, 10), mm, new Vector3(0f, 0.80f, 0f), Vector3.zero, new Vector3(1.10f, 0.74f, 1.36f));
            AddP("背甲1", Geo.Box(), am, new Vector3(0f, 1.05f, 0f), Vector3.zero, new Vector3(0.52f, 0.09f, 0.44f));
            AddP("背甲2", Geo.Box(), am, new Vector3(0f, 1.10f, 0.02f), Vector3.zero, new Vector3(0.46f, 0.08f, 0.40f));
            AddP("头", Geo.Box(), mm, new Vector3(0f, 0.88f, -0.78f), Vector3.zero, new Vector3(0.42f, 0.32f, 0.42f));
            AddP("下颌", Geo.Box(), am, new Vector3(0f, 0.72f, -0.80f), new Vector3(12f, 0f, 0f), new Vector3(0.34f, 0.14f, 0.30f));
            for (int k = 0; k < 2; k++)
            {
                float g = k == 0 ? -1f : 1f;
                AddP("眼" + k, Geo.Cone(6), gm, new Vector3(g * 0.14f, 0.92f, -0.96f), new Vector3(-90f, 0f, 0f),
                    new Vector3(0.09f, 0.10f, 0.09f));
                AddP("背刺" + k, Geo.Cone(6), am, new Vector3(g * 0.44f, 1.16f, 0f), Vector3.zero,
                    new Vector3(0.16f, 0.42f, 0.16f));
                AddP("腿前" + k, Geo.Box(), mm, new Vector3(g * 0.38f, 0.34f, -0.42f), Vector3.zero,
                    new Vector3(0.17f, 0.64f, 0.21f));
                AddP("腿后" + k, Geo.Box(), mm, new Vector3(g * 0.38f, 0.34f, 0.40f), Vector3.zero,
                    new Vector3(0.17f, 0.64f, 0.21f));
                AddP("蹄前" + k, Geo.Box(), am, new Vector3(g * 0.38f, 0.05f, -0.44f), Vector3.zero,
                    new Vector3(0.20f, 0.10f, 0.26f));
                AddP("蹄后" + k, Geo.Box(), am, new Vector3(g * 0.38f, 0.05f, 0.42f), Vector3.zero,
                    new Vector3(0.20f, 0.10f, 0.26f));
            }
            AddP("熔裂纹", Geo.Box(), gm, new Vector3(0f, 0.58f, -0.30f), Vector3.zero, new Vector3(0.40f, 0.03f, 0.03f));
            AddP("尾", Geo.Cone(7), am, new Vector3(0f, 0.78f, 0.76f), new Vector3(112f, 0f, 0f), new Vector3(0.18f, 0.45f, 0.18f));
        }
        return e;
    }

    public void SetPos(Vector3 p, float yawRad)
    {
        pos = p;
        yaw = yawRad;
        root.localPosition = p;
        root.localRotation = Quaternion.Euler(0f, yawRad * Mathf.Rad2Deg, 0f);
    }

    public void Animate(float dt)
    {
        anim += dt;
        if (dead)
        {
            deathT = Mathf.Min(1f, deathT + dt * 1.6f);
            root.localRotation = Quaternion.Euler(-deathT * 78f, yaw * Mathf.Rad2Deg, 0f);
            root.localPosition = pos + new Vector3(0f, -deathT * 0.35f, 0f);
            return;
        }
        float bob = Mathf.Sin(anim * (kind == "winged" ? 6.5f : 2.6f)) * (kind == "winged" ? 0.10f : 0.04f);
        root.localPosition = pos + new Vector3(0f, Mathf.Abs(bob) + 0.02f, 0f);
        root.localRotation = Quaternion.Euler(0f, yaw * Mathf.Rad2Deg, 0f);
        if (kind == "winged" && wingL != null && wingR != null)
        {
            float flap = Mathf.Sin(anim * 9f) * 34f;
            wingL.localRotation = Quaternion.Euler(0f, 0f, flap);
            wingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
        }
        // 攻击前冲
        if (attackT > 0f)
        {
            float k = Mathf.Sin((1f - attackT / 0.55f) * Mathf.PI);
            root.localPosition = pos + new Vector3(-Mathf.Sin(yaw) * k * 0.55f, Mathf.Abs(bob) + 0.02f, -Mathf.Cos(yaw) * k * 0.55f);
        }
    }

    public void TakeDamage(int amount)
    {
        hp -= amount;
        hitT = 0.18f;
        if (hp <= 0 && !dead)
        {
            dead = true;
            deathT = 0f;
        }
    }

    public void TickTimers(float dt)
    {
        if (hitT > 0f) hitT -= dt;
        if (deathT > 0f && dead) deathT += dt;
    }

    public void Destroy()
    {
        if (root != null) Object.Destroy(root.gameObject);
    }
}
