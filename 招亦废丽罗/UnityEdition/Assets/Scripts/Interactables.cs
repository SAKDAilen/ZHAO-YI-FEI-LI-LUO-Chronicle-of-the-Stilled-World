// Interactables.cs —— 世界交互物（篝火/碑文/晶簇/遗物匣/五坛/灵泉）+ 四位 NPC
using System.Collections.Generic;
using UnityEngine;

public class InteractObj
{
    public string kind, id, prompt;
    public Vector3 pos;
    public float radius = 2.2f;
    public bool used;
    public GameObject go;
    public Dictionary<string, object> data = new Dictionary<string, object>();
    public List<MeshRenderer> glows = new List<MeshRenderer>();
    public string lore;
}

public class NpcUnit
{
    public NpcDef def;
    public Vector3 pos;
    public float yaw;
    public GameObject go;
    public float bob;
    public Transform body;
}

public class Population
{
    public List<InteractObj> objects = new List<InteractObj>();
    public List<NpcUnit> npcs = new List<NpcUnit>();
    Transform root;

    static Vector3 Around(Vector2 spawn, float angle, float dist)
    {
        float x = spawn.x + Mathf.Cos(angle) * dist;
        float z = spawn.y + Mathf.Sin(angle) * dist;
        return new Vector3(x, GameData.HeightAt(x, z), z);
    }

    public void Build(Transform parent)
    {
        root = Make.Node("交互物", parent, Vector3.zero);
        Vector2 sp = GameData.Spawn;

        Add("campfire", "camp_0", Around(sp, 0.4f, 8f), "篝火：在此休息回血", true);
        for (int k = 0; k < 8; k++)
            Add("stele", "stele_" + k, Around(sp, 2f * Mathf.PI * k / 8f + 0.2f, 16f + k * 4.2f), "石碑：阅读碑文", false);
        for (int k = 0; k < 16; k++)
            Add("crystal", "crystal_" + k, Around(sp, 2f * Mathf.PI * k / 16f, 12f + (k % 8) * 6f), "晶簇：采集晶尘", false);
        for (int k = 0; k < 5; k++)
            Add("chest", "chest_" + k, Around(sp, 2f * Mathf.PI * k / 5f + 0.5f, 34f + k * 8f), "遗物匣：开启", false);
        for (int k = 0; k < 5; k++)
        {
            int idx = Mathf.Min(k, GameData.Shrines.Count - 1);
            var obj = Add("shrine", GameData.Shrines.Count > k ? GameData.Shrines[k].id : "shrine_" + k,
                Around(sp, 2f * Mathf.PI * k / 5f, 88f), "楔之祭坛：触碰", false);
            if (GameData.Shrines.Count > k)
            {
                var s = GameData.Shrines[k];
                obj.data["bonus"] = s.bonus;
                obj.data["name"] = s.name;
                obj.data["desc"] = s.desc;
            }
        }
        for (int k = 0; k < 4; k++)
            Add("vent", "vent_" + k, Around(sp, 2f * Mathf.PI * k / 4f + 1.1f, 26f + k * 5f), "灵脉之泉：汲取能量", false);

        // 四位 NPC
        BuildNpc("zhonglou", Around(sp, 0.2f, 6.4f), 0.9f);
        BuildNpc("huisheng", Around(sp, 1.9f, 11f), 2.2f);
        BuildNpc("shouye", Around(sp, 3.4f, 9.6f), 3.9f);
        BuildNpc("tao", Around(sp, 5.0f, 8.6f), 0.4f);
    }

    InteractObj Add(string kind, string id, Vector3 pos, string prompt, bool glowAnimate)
    {
        var o = new InteractObj();
        o.kind = kind;
        o.id = id;
        o.pos = pos;
        o.prompt = prompt;
        o.go = Make.Node(kind + "_" + id, root, pos).gameObject;
        o.go.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        switch (kind)
        {
            case "campfire": BuildCampfire(o); o.radius = 2.4f; break;
            case "stele": BuildStele(o); o.radius = 2.2f; break;
            case "crystal": BuildCrystal(o); o.radius = 1.8f; break;
            case "chest": BuildChest(o); o.radius = 1.8f; break;
            case "shrine": BuildShrine(o); o.radius = 3.0f; break;
            case "vent": BuildVent(o); o.radius = 2.4f; break;
        }
        objects.Add(o);
        return o;
    }

    Transform T(InteractObj o, string name) { return Make.Node(name, o.go.transform, Vector3.zero); }

    Part P(Transform parent, string name, Mesh mesh, Color col, Vector3 pos, Vector3 rot, Vector3 scale)
    {
        return Make.PartOn(parent, name, mesh, Mats.Get(col, Color.black), pos, rot, scale);
    }

    Part PE(Transform parent, string name, Mesh mesh, Color col, Color emis, Vector3 pos, Vector3 rot, Vector3 scale)
    {
        return Make.PartOn(parent, name, mesh, Mats.Get(col, emis), pos, rot, scale);
    }

    void BuildCampfire(InteractObj o)
    {
        var t = o.go.transform;
        for (int k = 0; k < 7; k++)
        {
            float a = k * 0.9f;
            P(t, "石" + k, Geo.Octa(), new Color(0.30f, 0.29f, 0.27f),
                new Vector3(Mathf.Cos(a) * 0.85f, 0.12f, Mathf.Sin(a) * 0.85f),
                new Vector3(0f, a * 57f, 0f), new Vector3(0.30f, 0.26f, 0.30f));
        }
        for (int k = 0; k < 3; k++)
            P(t, "木" + k, Geo.Cylinder(6, true), new Color(0.24f, 0.16f, 0.10f),
                new Vector3(0f, 0.16f, 0f), new Vector3(0f, k * 60f, 78f), new Vector3(0.12f, 0.85f, 0.12f));
        var fire = PE(t, "火", Geo.Cone(8), new Color(1.0f, 0.55f, 0.18f), new Color(1.0f, 0.45f, 0.10f),
            new Vector3(0f, 0.48f, 0f), Vector3.zero, new Vector3(0.30f, 0.55f, 0.30f));
        o.glows.Add(fire.renderer);
    }

    void BuildStele(InteractObj o)
    {
        var t = o.go.transform;
        P(t, "碑座", Geo.Box(), new Color(0.28f, 0.27f, 0.25f), new Vector3(0f, 0.15f, 0f), Vector3.zero,
            new Vector3(0.80f, 0.30f, 0.60f));
        P(t, "碑身", Geo.Box(), new Color(0.34f, 0.32f, 0.30f), new Vector3(0f, 1.10f, 0f), Vector3.zero,
            new Vector3(0.55f, 1.90f, 0.24f));
        var rune = PE(t, "碑文", Geo.Box(), new Color(0.75f, 0.85f, 0.95f), new Color(0.35f, 0.75f, 1.0f),
            new Vector3(0f, 1.15f, -0.13f), Vector3.zero, new Vector3(0.34f, 1.30f, 0.02f));
        o.glows.Add(rune.renderer);
    }

    void BuildCrystal(InteractObj o)
    {
        var t = o.go.transform;
        P(t, "母岩", Geo.Octa(), new Color(0.22f, 0.24f, 0.26f), new Vector3(0f, 0.20f, 0f), Vector3.zero,
            new Vector3(0.55f, 0.45f, 0.55f));
        for (int k = 0; k < 3; k++)
        {
            float a = k * 2.1f;
            var c = PE(t, "晶" + k, Geo.Octa(), new Color(0.62f, 0.92f, 1.0f), new Color(0.35f, 0.85f, 1.0f),
                new Vector3(Mathf.Cos(a) * 0.25f, 0.55f + k * 0.16f, Mathf.Sin(a) * 0.25f),
                new Vector3(0f, a * 57f, 18f), new Vector3(0.20f, 0.62f, 0.20f));
            o.glows.Add(c.renderer);
        }
    }

    void BuildChest(InteractObj o)
    {
        var t = o.go.transform;
        P(t, "箱体", Geo.Box(), new Color(0.30f, 0.22f, 0.13f), new Vector3(0f, 0.30f, 0f), Vector3.zero,
            new Vector3(0.90f, 0.60f, 0.62f));
        P(t, "箱盖", Geo.Box(), new Color(0.36f, 0.26f, 0.15f), new Vector3(0f, 0.64f, 0f), Vector3.zero,
            new Vector3(0.94f, 0.16f, 0.66f));
        var seam = PE(t, "符纹", Geo.Box(), new Color(0.95f, 0.80f, 0.45f), new Color(0.9f, 0.65f, 0.2f),
            new Vector3(0f, 0.40f, -0.32f), Vector3.zero, new Vector3(0.60f, 0.04f, 0.02f));
        o.glows.Add(seam.renderer);
    }

    void BuildShrine(InteractObj o)
    {
        var t = o.go.transform;
        P(t, "坛基", Geo.Cylinder(10, true), new Color(0.22f, 0.21f, 0.24f), new Vector3(0f, 0.20f, 0f),
            Vector3.zero, new Vector3(2.6f, 0.40f, 2.6f));
        for (int k = 0; k < 5; k++)
        {
            float a = k * Mathf.PI * 2f / 5f;
            P(t, "柱" + k, Geo.Cylinder(6, true), new Color(0.30f, 0.28f, 0.30f),
                new Vector3(Mathf.Cos(a) * 1.05f, 1.20f, Mathf.Sin(a) * 1.05f), Vector3.zero,
                new Vector3(0.20f, 2.0f, 0.20f));
        }
        P(t, "环形", Geo.Cylinder(14, false), new Color(0.36f, 0.34f, 0.38f), new Vector3(0f, 2.25f, 0f),
            Vector3.zero, new Vector3(2.3f, 0.12f, 2.3f));
        var core = PE(t, "坛心", Geo.Octa(), new Color(0.85f, 0.92f, 1.0f), new Color(0.45f, 0.85f, 1.0f),
            new Vector3(0f, 1.10f, 0f), Vector3.zero, new Vector3(0.35f, 0.55f, 0.35f));
        o.glows.Add(core.renderer);
    }

    void BuildVent(InteractObj o)
    {
        var t = o.go.transform;
        for (int k = 0; k < 6; k++)
        {
            float a = k * 1.05f;
            P(t, "泉石" + k, Geo.Octa(), new Color(0.26f, 0.27f, 0.30f),
                new Vector3(Mathf.Cos(a) * 1.0f, 0.12f, Mathf.Sin(a) * 1.0f), new Vector3(0f, a * 57f, 0f),
                new Vector3(0.34f, 0.30f, 0.34f));
        }
        var pool = PE(t, "灵脉", Geo.Cylinder(16, true), new Color(0.35f, 0.75f, 0.95f), new Color(0.25f, 0.75f, 1.0f),
            new Vector3(0f, 0.10f, 0f), Vector3.zero, new Vector3(1.5f, 0.06f, 1.5f));
        o.glows.Add(pool.renderer);
    }

    void BuildNpc(string key, Vector3 pos, float yaw)
    {
        NpcDef def;
        GameData.Npcs.TryGetValue(key, out def);
        if (def == null) return;
        var u = new NpcUnit();
        u.def = def;
        u.pos = pos;
        u.yaw = yaw;
        u.go = Make.Node("NPC_" + key, root, pos).gameObject;
        u.go.transform.localRotation = Quaternion.Euler(0f, yaw * Mathf.Rad2Deg, 0f);
        u.body = Make.Node("身体", u.go.transform, Vector3.zero);
        var t = u.body;
        if (def.model == "clockwork")
        {
            P(t, "下身", Geo.Cylinder(8, true), new Color(0.30f, 0.20f, 0.12f), new Vector3(0f, 0.55f, 0f),
                Vector3.zero, new Vector3(0.55f, 0.90f, 0.55f));
            P(t, "腹", Geo.Sphere(10, 8), new Color(0.42f, 0.28f, 0.14f), new Vector3(0f, 1.10f, 0f),
                Vector3.zero, new Vector3(0.70f, 0.75f, 0.66f));
            var glass = PE(t, "沙漏", Geo.Octa(), new Color(0.95f, 0.92f, 0.80f), new Color(0.9f, 0.8f, 0.55f),
                new Vector3(0f, 1.10f, -0.28f), Vector3.zero, new Vector3(0.22f, 0.34f, 0.10f));
            u.go.name = "NPC_" + key;
            P(t, "头", Geo.Box(), new Color(0.36f, 0.24f, 0.13f), new Vector3(0f, 1.62f, 0f), Vector3.zero,
                new Vector3(0.42f, 0.42f, 0.40f));
            PE(t, "眼", Geo.Box(), new Color(1.0f, 0.85f, 0.45f), new Color(1.0f, 0.7f, 0.25f),
                new Vector3(0f, 1.64f, -0.21f), Vector3.zero, new Vector3(0.26f, 0.04f, 0.02f));
            for (int k = 0; k < 2; k++)
            {
                float g = k == 0 ? -1f : 1f;
                P(t, "臂" + k, Geo.Cylinder(6, true), new Color(0.30f, 0.20f, 0.12f),
                    new Vector3(g * 0.42f, 1.20f, 0f), new Vector3(0f, 0f, g * 12f), new Vector3(0.14f, 0.80f, 0.14f));
            }
        }
        else if (def.model == "wisp")
        {
            var core = PE(t, "灵体", Geo.Octa(), new Color(0.7f, 0.92f, 1.0f), new Color(0.4f, 0.8f, 1.0f),
                new Vector3(0f, 1.20f, 0f), Vector3.zero, new Vector3(0.55f, 0.90f, 0.55f));
            PE(t, "光环", Geo.Cylinder(16, false), new Color(0.5f, 0.8f, 1.0f), new Color(0.3f, 0.7f, 1.0f),
                new Vector3(0f, 1.20f, 0f), Vector3.zero, new Vector3(1.30f, 0.04f, 1.30f));
        }
        else if (def.model == "watcher")
        {
            P(t, "身", Geo.Box(), new Color(0.24f, 0.24f, 0.26f), new Vector3(0f, 0.75f, 0f), Vector3.zero,
                new Vector3(0.70f, 1.40f, 0.46f));
            P(t, "肩", Geo.Box(), new Color(0.30f, 0.30f, 0.33f), new Vector3(0f, 1.35f, 0f), Vector3.zero,
                new Vector3(0.90f, 0.24f, 0.52f));
            P(t, "头", Geo.Sphere(8, 6), new Color(0.28f, 0.28f, 0.30f), new Vector3(0f, 1.62f, 0f), Vector3.zero,
                Vector3.one * 0.42f);
            PE(t, "目", Geo.Box(), new Color(0.9f, 0.75f, 0.5f), new Color(1.0f, 0.65f, 0.25f),
                new Vector3(0f, 1.62f, -0.21f), Vector3.zero, new Vector3(0.22f, 0.05f, 0.02f));
            P(t, "矛", Geo.Cylinder(6, true), new Color(0.40f, 0.33f, 0.20f), new Vector3(0.52f, 1.10f, -0.10f),
                new Vector3(0f, 0f, 8f), new Vector3(0.06f, 2.40f, 0.06f));
            P(t, "矛尖", Geo.Cone(5), new Color(0.55f, 0.55f, 0.60f), new Vector3(0.62f, 2.36f, -0.10f),
                new Vector3(180f, 0f, 8f), new Vector3(0.10f, 0.28f, 0.10f));
        }
        else
        {
            P(t, "箱车", Geo.Box(), new Color(0.34f, 0.24f, 0.14f), new Vector3(0f, 0.42f, 0.30f), Vector3.zero,
                new Vector3(0.90f, 0.80f, 0.70f));
            P(t, "身", Geo.Cylinder(8, true), new Color(0.40f, 0.28f, 0.16f), new Vector3(0f, 1.05f, 0f),
                Vector3.zero, new Vector3(0.62f, 0.95f, 0.62f));
            P(t, "头", Geo.Sphere(8, 6), new Color(0.46f, 0.33f, 0.20f), new Vector3(0f, 1.62f, 0f), Vector3.zero,
                Vector3.one * 0.40f);
            P(t, "帽", Geo.Cone(8), new Color(0.26f, 0.20f, 0.14f), new Vector3(0f, 1.92f, 0f), Vector3.zero,
                new Vector3(0.62f, 0.34f, 0.62f));
            PE(t, "眼", Geo.Box(), new Color(0.95f, 0.85f, 0.55f), new Color(1.0f, 0.7f, 0.3f),
                new Vector3(0f, 1.62f, -0.19f), Vector3.zero, new Vector3(0.20f, 0.04f, 0.02f));
        }
        npcs.Add(u);
    }

    public void Update(float dt, Vector3 playerPos)
    {
        for (int k = 0; k < npcs.Count; k++)
        {
            var u = npcs[k];
            u.bob += dt;
            float y = Mathf.Sin(u.bob * (u.def.model == "wisp" ? 2.2f : 1.4f)) * (u.def.model == "wisp" ? 0.28f : 0.05f);
            u.body.localPosition = new Vector3(0f, 0.6f + y, 0f);
            if (u.def.model == "wisp")
                u.body.localRotation = Quaternion.Euler(0f, u.bob * 40f, 0f);
            else
            {
                Vector3 d = playerPos - u.pos;
                if (d.magnitude < 6f)
                {
                    float want = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                    u.go.transform.localRotation = Quaternion.Euler(0f, want, 0f);
                }
            }
        }
    }

    public InteractObj Nearest(Vector3 p, out NpcUnit npc)
    {
        npc = null;
        float best = 999f;
        InteractObj found = null;
        for (int k = 0; k < objects.Count; k++)
        {
            var o = objects[k];
            if (o.used || o.go == null) continue;
            float d = (o.pos - p).magnitude;
            if (d < o.radius && d < best) { best = d; found = o; }
        }
        for (int k = 0; k < npcs.Count; k++)
        {
            var u = npcs[k];
            float d = (u.pos - p).magnitude;
            if (d < 2.6f && d < best) { best = d; found = null; npc = u; }
        }
        return npc != null ? null : found;
    }
}
