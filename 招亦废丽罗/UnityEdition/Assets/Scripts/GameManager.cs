// GameManager.cs —— Unity 版引导与主循环（单例，运行时自动创建）
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Msg
{
    public string text;
    public Color color;
    public float age;
}

public class DmgNum
{
    public Vector3 world;
    public string text;
    public Color color;
    public float age;
    public float up;
}

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    // 场景对象
    public Camera cam;
    public CameraRig camRig;
    public PlayerController player;
    public TerrainStreamer terrain;
    public Population pop;
    public EnemyManager enemies;
    public Env env;
    public Hud hud;
    Light sun;
    Transform worldRoot;

    // 状态
    public DialogueContext ctx = new DialogueContext();
    public int kills;
    public bool lawUnlocked;
    public List<Msg> messages = new List<Msg>();
    public List<DmgNum> dmgNums = new List<DmgNum>();
    public string interactPrompt = "";
    public bool talking;
    public DialogueRunner talkRunner;
    public NpcUnit talkNpc;
    public float fps = 60f, frameMs = 16f;
    public bool showStats = true;
    public bool cursorLocked = true;
    public Vector3 respawnPoint;

    readonly HashSet<EnemyRig> killedSet = new HashSet<EnemyRig>();
    bool swingHit;
    bool lockOn;
    float autosaveT;
    float fpsSmooth = 60f;
    string lastDialogueNpc = "";

    static readonly string[] VersesSword =
    {
        "十步杀一人，千里不留行。",
        "提剑跨马去，白骨黄沙间。",
        "一笑出门去，千峰皆回响。",
    };
    static readonly string[] VersesTool =
    {
        "工欲善其事，必先利其器。",
        "纸上得来终觉浅，绝知此事要躬行。",
        "人生到处知何似，应似飞鸿踏雪泥。",
    };
    static readonly string[] VersesWing =
    {
        "大漠孤烟直，长河落日圆。",
        "醉卧沙场君莫笑，古来征战几人回。",
        "青海长云暗雪山，孤城遥望玉门关。",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (I != null) return;
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        Application.targetFrameRate = 60;
        GameData.Load();
        Mats.Init();
        BuildScene();
        LoadGame(true);
        AddMessage("你在一块锈蚀的青铜碑旁醒来。按 E 与最近之物交互。", new Color(0.92f, 0.86f, 0.70f));
    }

    void BuildScene()
    {
        worldRoot = Make.Node("世界", null, Vector3.zero);

        // 相机
        var camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.12f;
        cam.farClipPlane = 2600f;
        cam.fieldOfView = 62f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.24f, 0.11f);
        camGo.AddComponent<AudioListener>();
        camRig = new CameraRig();
        camRig.Init(cam);

        // 光
        var sunGo = new GameObject("Sun");
        sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.72f, 0.44f);
        sun.intensity = 1.05f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.72f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.30f, 0.34f, 0.44f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.36f, 0.22f, 0.16f);
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance = 620f;

        // 世界
        terr();
        Vector2 sp = GameData.Spawn;
        Vector3 spawnPos = new Vector3(sp.x, GameData.HeightAt(sp.x, sp.y) + 0.1f, sp.y);
        player = new PlayerController();
        player.Init(worldRoot, spawnPos, Mathf.PI * 1.25f);
        respawnPoint = spawnPos;

        pop = new Population();
        pop.Build(worldRoot);

        enemies = new EnemyManager();
        enemies.Init(worldRoot);
        SpawnInitialEnemies();

        env = new Env();
        hud = new Hud();
        hud.Init();

        camRig.yaw = player.yaw;
        camRig.pitch = -0.14f;
        camRig.pos = player.pos + new Vector3(Mathf.Sin(camRig.yaw) * 8f, 4f, Mathf.Cos(camRig.yaw) * 8f);
        LockCursor(true);
    }

    void terr()
    {
        terrain = new TerrainStreamer();
        terrain.Init(worldRoot);
    }

    void SpawnInitialEnemies()
    {
        Vector3 p = player.pos;
        enemies.Spawn("construct", p + new Vector3(9f, 0f, 6f), 1);
        enemies.Spawn("winged", p + new Vector3(-11f, 0f, 8f), 1);
        enemies.Spawn("lava", p + new Vector3(5f, 0f, -13f), 1);
        for (int k = 0; k < enemies.list.Count; k++)
        {
            var e = enemies.list[k];
            e.pos = new Vector3(e.pos.x, GameData.HeightAt(e.pos.x, e.pos.z), e.pos.z);
            e.SetPos(e.pos, 0f);
        }
    }

    void LockCursor(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void Update()
    {
        float raw = Time.deltaTime;
        float dt = Mathf.Clamp(raw, 1f / 240f, 1f / 20f);
        frameMs = Mathf.Lerp(frameMs, raw * 1000f, 0.1f);
        fpsSmooth = Mathf.Lerp(fpsSmooth, 1f / Mathf.Max(0.0001f, raw), 0.06f);
        fps = fpsSmooth;

        HandleCursorAndHotkeys();

        InputState inp = ReadInput();
        if (inp.attack && cursorLocked && !talking) player.RequestAttack();
        if (talking)
        {
            inp = new InputState();
            HandleDialogueKeys();
        }

        // 鼠标视角
        if (cursorLocked && !talking)
        {
            float mx = Input.GetAxisRaw("Mouse X");
            float my = Input.GetAxisRaw("Mouse Y");
            camRig.Look(mx, my, 1f);
            camRig.lastLookT = 0f;
        }
        else
        {
            camRig.lastLookT += dt;
        }
        if (Input.GetAxisRaw("Mouse ScrollWheel") != 0f)
            camRig.OnWheel(Input.GetAxisRaw("Mouse ScrollWheel"));

        // 玩家
        player.Step(this, dt, inp, camRig.yaw);
        if (player.dead)
        {
            // 死亡计时由 Player.Step 内推进 deadT
            if (player.deadT > 3f && player.deadT < 3f + dt * 2f)
            {
                ctx.deaths++;
                AddMessage("你倒下了……三息之后，钟漏重新转动。", new Color(0.9f, 0.5f, 0.45f));
            }
            if (player.deadT > 3.2f)
            {
                player.Respawn();
            }
        }

        // 敌人
        enemies.Update(this, dt, player.pos);

        // 相机
        bool sprinting = player.speed > 12.5f;
        EnemyRig near;
        float nd = enemies.NearestDist(player.pos, out near);
        bool combatNear = nd < 14f;
        camRig.Update(dt, player, player.vel, sprinting, false, combatNear, player.rollT, player.speed);
        ApplyCamera();
        if (lockOn && near == null) lockOn = false;
        camRig.lockTarget = (lockOn && near != null && cursorLocked && !talking) ? near.root : null;

        // 世界
        terrain.Update(player.pos, 2);
        pop.Update(dt, player.pos);
        env.Update(dt, player.pos);
        ApplyEnv();

        // 交互
        UpdateInteraction();

        // 消息与伤害数字
        for (int k = messages.Count - 1; k >= 0; k--)
        {
            messages[k].age += dt;
            if (messages[k].age > 4.2f) messages.RemoveAt(k);
        }
        for (int k = dmgNums.Count - 1; k >= 0; k--)
        {
            var d = dmgNums[k];
            d.age += dt;
            d.world += Vector3.up * dt * d.up;
            if (d.age > 1.4f) dmgNums.RemoveAt(k);
        }

        // 自动存档
        autosaveT += dt;
        if (autosaveT > 60f)
        {
            autosaveT = 0f;
            SaveGame(true);
        }
    }

    void HandleCursorAndHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (talking) EndTalk();
            else LockCursor(!cursorLocked);
        }
        if (Input.GetMouseButtonDown(0) && !cursorLocked && !talking) LockCursor(true);
        if (Input.GetKeyDown(KeyCode.Tab) && !talking) lockOn = !lockOn;
        if (Input.GetKeyDown(KeyCode.F1)) showStats = !showStats;
        if (Input.GetKeyDown(KeyCode.F5)) { SaveGame(false); AddMessage("已存档。", new Color(0.6f, 0.9f, 0.7f)); }
        if (Input.GetKeyDown(KeyCode.F9)) { LoadGame(false); AddMessage("已读取存档。", new Color(0.6f, 0.9f, 1f)); }
    }

    InputState ReadInput()
    {
        var inp = new InputState();
        inp.x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        inp.z = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
        inp.sprint = Input.GetKey(KeyCode.LeftShift);
        inp.jump = Input.GetKeyDown(KeyCode.Space);
        inp.roll = Input.GetKeyDown(KeyCode.Q);
        inp.attack = Input.GetMouseButtonDown(0);
        inp.law = Input.GetKeyDown(KeyCode.R);
        inp.interact = Input.GetKeyDown(KeyCode.E);
        return inp;
    }

    void HandleDialogueKeys()
    {
        if (talkRunner == null) { talking = false; return; }
        for (int k = 0; k < 6; k++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + k)))
            {
                if (!talkRunner.Select(k)) EndTalk();
                return;
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)) EndTalk();
    }

    void UpdateInteraction()
    {
        interactPrompt = "";
        if (talking) return;
        NpcUnit npc;
        var obj = pop.Nearest(player.pos, out npc);
        if (npc != null)
        {
            interactPrompt = "按 E 与「" + npc.def.name + "」交谈";
            if (Input.GetKeyDown(KeyCode.E)) StartTalk(npc);
        }
        else if (obj != null)
        {
            interactPrompt = "按 E " + obj.prompt;
            if (Input.GetKeyDown(KeyCode.E)) UseObject(obj);
        }
    }

    void StartTalk(NpcUnit npc)
    {
        talking = true;
        talkNpc = npc;
        talkRunner = new DialogueRunner(npc.def, ctx);
        lastDialogueNpc = npc.def.key;
    }

    void EndTalk()
    {
        talking = false;
        talkRunner = null;
        talkNpc = null;
    }

    void UseObject(InteractObj o)
    {
        switch (o.kind)
        {
            case "campfire":
                HealPlayer(30f);
                respawnPoint = o.pos;
                player.spawn = o.pos;
                AddMessage("你在篝火旁坐下，暖意顺着青铜关节流过。", new Color(1f, 0.75f, 0.45f));
                break;
            case "stele":
                {
                    int idx = (o.id.GetHashCode() & 0x7fffffff) % Mathf.Max(1, GameData.SteleTexts.Length);
                    string text = GameData.SteleTexts.Length > 0 ? GameData.SteleTexts[idx] : "碑文已被风沙磨平。";
                    AddMessage(text, new Color(0.75f, 0.85f, 0.92f));
                }
                break;
            case "crystal":
                ctx.inventory["crystal"] = ctx.Item("crystal") + 1;
                o.used = true;
                if (o.go != null) o.go.SetActive(false);
                AddMessage("获得 晶尘 ×1", new Color(0.5f, 0.9f, 1f));
                break;
            case "chest":
                {
                    string[] loot = { "crystal", "part", "fragment", "memory", "relic" };
                    string it = loot[(o.id.GetHashCode() & 0x7fffffff) % loot.Length];
                    ctx.inventory[it] = ctx.Item(it) + 1;
                    o.used = true;
                    if (o.go != null) o.go.SetActive(false);
                    string nm = GameData.ItemNames.ContainsKey(it) ? GameData.ItemNames[it] : it;
                    AddMessage("开启遗物匣：获得 " + nm + " ×1", new Color(0.95f, 0.85f, 0.55f));
                }
                break;
            case "vent":
                RestoreStamina(100f);
                HealPlayer(10f);
                AddMessage("灵脉的能量涌入四肢，灵力充盈。", new Color(0.45f, 0.9f, 1f));
                break;
            case "shrine":
                {
                    string name = o.data.ContainsKey("name") ? o.data["name"].ToString() : "楔之祭坛";
                    ApplyShrineBonus(o);
                    o.used = true;
                    if (o.go != null) o.go.SetActive(false);
                    AddMessage("「" + name + "」的力量融入你的律令。", new Color(0.7f, 0.95f, 1f));
                }
                break;
        }
    }

    void ApplyShrineBonus(InteractObj o)
    {
        if (!o.data.ContainsKey("bonus")) return;
        var bonus = o.data["bonus"] as Dictionary<string, object>;
        if (bonus == null) return;
        foreach (var kv in bonus)
        {
            switch (kv.Key)
            {
                case "max_hp": AddMaxHp(MiniJson.I(kv.Value)); break;
                case "max_stamina": player.maxStamina += MiniJson.F(kv.Value); break;
                case "damage_bonus": AddDamageBonus(MiniJson.I(kv.Value)); break;
                case "regen": player.stamina = player.maxStamina; break;
                case "law": lawUnlocked = true; AddMessage("你领悟了「法则之坛」——按 R 施展律令。", new Color(0.55f, 0.9f, 1f)); break;
            }
        }
    }

    // ---------- 战斗回调 ----------
    public void OnSwingStart()
    {
        swingHit = false;
    }

    public void OnSwingTick(float t)
    {
        if (swingHit || t < 0.22f || t > 0.62f) return;
        swingHit = true;
        Vector3 aim = player.AimDir(camRig.yaw, camRig.pitch);
        float range = 3.6f;
        for (int k = 0; k < enemies.list.Count; k++)
        {
            var e = enemies.list[k];
            if (e.dead) continue;
            Vector3 d = e.pos + Vector3.up * 0.9f - (player.pos + Vector3.up * 1.1f);
            float dist = d.magnitude;
            if (dist > range + e.radius) continue;
            if (Vector3.Dot(aim, d.normalized) < 0.42f) continue;
            int dmg = Mathf.RoundToInt(26f + player.damageBonus);
            if (player.lawT > 0f) dmg = Mathf.RoundToInt(dmg * 1.5f);
            e.TakeDamage(dmg);
            AddDmgNumber(e.pos + Vector3.up * 1.5f, dmg.ToString(), new Color(1f, 0.85f, 0.5f));
            camRig.Shake(0.16f);
            if (e.dead && killedSet.Add(e))
            {
                kills++;
                OnEnemyKilled(e);
            }
        }
    }

    void OnEnemyKilled(EnemyRig e)
    {
        if (Random.value < 0.4f)
        {
            ctx.inventory["crystal"] = ctx.Item("crystal") + 1;
            AddMessage("敌人崩解，拾得 晶尘 ×1", new Color(0.55f, 0.9f, 1f));
        }
        if (Random.value < 0.35f)
        {
            string[] pool = Random.value < 0.5f ? VersesSword : (Random.value < 0.6f ? VersesTool : VersesWing);
            AddMessage("「" + pool[Random.Range(0, pool.Length)] + "」", new Color(0.85f, 0.92f, 1f));
        }
    }

    public void OnLand(bool heavy)
    {
        camRig.Shake(heavy ? 0.34f : 0.16f);
    }

    public void HurtPlayer(float amount, Vector3 from)
    {
        if (player.invuln > 0f || player.dead) return;
        player.Damage(amount, from);
        camRig.Shake(0.5f);
        AddDmgNumber(player.pos + Vector3.up * 1.7f, "-" + Mathf.RoundToInt(amount), new Color(1f, 0.4f, 0.35f));
        if (player.dead)
        {
            player.deadT = 0f;
            AddMessage("青铜之躯发出断裂声……", new Color(0.9f, 0.45f, 0.4f));
        }
    }

    public void HealPlayer(float amount)
    {
        player.hp = Mathf.Min(player.maxHp, player.hp + amount);
    }

    public void RestoreStamina(float amount)
    {
        player.stamina = Mathf.Min(player.maxStamina, player.stamina + amount);
    }

    public void AddMaxHp(int amount)
    {
        player.maxHp += amount;
        player.hp += amount;
    }

    public void AddDamageBonus(int amount)
    {
        player.damageBonus += amount;
    }

    public void TeachVerse(string verse)
    {
        AddMessage("你习得诗句：「" + verse + "」", new Color(0.8f, 0.95f, 1f));
    }

    public void AddMessage(string text, Color color)
    {
        var m = new Msg();
        m.text = text;
        m.color = color;
        m.age = 0f;
        messages.Add(m);
        if (messages.Count > 6) messages.RemoveAt(0);
    }

    public void AddDmgNumber(Vector3 world, string text, Color color)
    {
        var d = new DmgNum();
        d.world = world;
        d.text = text;
        d.color = color;
        d.age = 0f;
        d.up = 1.2f;
        dmgNums.Add(d);
        if (dmgNums.Count > 24) dmgNums.RemoveAt(0);
    }

    // ---------- 相机 / 光照 ----------
    void ApplyCamera()
    {
        Vector3 pos = camRig.pos;
        pos.y = Mathf.Max(pos.y, GameData.HeightAt(pos.x, pos.z) + 0.35f);
        if (GameData.InLake(pos.x, pos.z) && pos.y < 7.8f) pos.y = 7.8f;
        camRig.pos = pos;
        Vector3 dir = camRig.lookS - camRig.pos;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        float shake = camRig.shakeAmt;
        Vector3 jitter = shake > 0.001f ? new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * shake * 0.18f : Vector3.zero;
        cam.transform.position = camRig.pos + jitter;
        cam.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        cam.fieldOfView = camRig.fov;
    }

    void ApplyEnv()
    {
        if (env.sunDir.sqrMagnitude > 0.001f)
            sun.transform.rotation = Quaternion.LookRotation(-env.sunDir.normalized, Vector3.up);
        sun.color = env.sunColor;
        sun.intensity = env.sunIntensity;
        RenderSettings.ambientLight = env.ambient;
        RenderSettings.fogColor = env.fogColor;
        RenderSettings.fogEndDistance = env.fogFar;
        cam.backgroundColor = env.horizon;
        if (terrain.waterMat != null)
        {
            if (terrain.waterMat.HasProperty("_Color")) terrain.waterMat.SetColor("_Color", env.waterColor);
        }
    }

    // ---------- 存档 ----------
    string SavePath()
    {
        return Path.Combine(Application.persistentDataPath, "save.json");
    }

    public void SaveGame(bool quiet)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("{");
        sb.Append("\"pos\":[").Append(player.pos.x.ToString("R")).Append(",").Append(player.pos.y.ToString("R")).Append(",").Append(player.pos.z.ToString("R")).Append("],");
        sb.Append("\"yaw\":").Append(player.yaw.ToString("R")).Append(",");
        sb.Append("\"hp\":").Append(player.hp.ToString("R")).Append(",");
        sb.Append("\"maxHp\":").Append(player.maxHp.ToString("R")).Append(",");
        sb.Append("\"stamina\":").Append(player.stamina.ToString("R")).Append(",");
        sb.Append("\"maxStamina\":").Append(player.maxStamina.ToString("R")).Append(",");
        sb.Append("\"law\":").Append(player.lawEnergy.ToString("R")).Append(",");
        sb.Append("\"lawUnlocked\":").Append(lawUnlocked ? "true" : "false").Append(",");
        sb.Append("\"lawPower\":").Append(player.damageBonus.ToString("R")).Append(",");
        sb.Append("\"kills\":").Append(kills).Append(",");
        sb.Append("\"deaths\":").Append(ctx.deaths).Append(",");
        sb.Append("\"day\":").Append(env.day).Append(",");
        sb.Append("\"time\":").Append(env.t.ToString("R")).Append(",");
        sb.Append("\"spawn\":[").Append(respawnPoint.x.ToString("R")).Append(",").Append(respawnPoint.z.ToString("R")).Append("],");
        sb.Append("\"flags\":");
        AppendDict(sb, ctx.flags);
        sb.Append(",\"inventory\":");
        AppendIntDict(sb, ctx.inventory);
        sb.Append(",\"quests\":");
        AppendIntDict(sb, ctx.quests);
        sb.Append("}");
        try { File.WriteAllText(SavePath(), sb.ToString()); }
        catch (System.Exception e) { Debug.LogWarning("存档失败: " + e.Message); }
    }

    void AppendDict(System.Text.StringBuilder sb, Dictionary<string, bool> d)
    {
        sb.Append("{");
        bool first = true;
        foreach (var kv in d)
        {
            if (!kv.Value) continue;
            if (!first) sb.Append(",");
            first = false;
            sb.Append("\"").Append(kv.Key).Append("\":true");
        }
        sb.Append("}");
    }

    void AppendIntDict(System.Text.StringBuilder sb, Dictionary<string, int> d)
    {
        sb.Append("{");
        bool first = true;
        foreach (var kv in d)
        {
            if (!first) sb.Append(",");
            first = false;
            sb.Append("\"").Append(kv.Key).Append("\":").Append(kv.Value);
        }
        sb.Append("}");
    }

    public void LoadGame(bool boot)
    {
        string path = SavePath();
        if (!File.Exists(path)) return;
        try
        {
            string text = File.ReadAllText(path);
            var root = MiniJson.Parse(text) as Dictionary<string, object>;
            if (root == null) return;
            var posList = MiniJson.List(root.ContainsKey("pos") ? root["pos"] : null);
            if (posList.Count >= 3)
            {
                player.pos = new Vector3(MiniJson.F(posList[0]), MiniJson.F(posList[1]), MiniJson.F(posList[2]));
            }
            if (root.ContainsKey("yaw")) player.yaw = MiniJson.F(root["yaw"]);
            if (root.ContainsKey("hp")) player.hp = MiniJson.F(root["hp"]);
            if (root.ContainsKey("maxHp")) player.maxHp = MiniJson.F(root["maxHp"]);
            if (root.ContainsKey("stamina")) player.stamina = MiniJson.F(root["stamina"]);
            if (root.ContainsKey("maxStamina")) player.maxStamina = MiniJson.F(root["maxStamina"]);
            if (root.ContainsKey("law")) player.lawEnergy = MiniJson.F(root["law"]);
            if (root.ContainsKey("lawUnlocked")) lawUnlocked = MiniJson.B(root["lawUnlocked"]);
            if (root.ContainsKey("lawPower")) player.damageBonus = MiniJson.F(root["lawPower"]);
            if (root.ContainsKey("kills")) kills = MiniJson.I(root["kills"]);
            if (root.ContainsKey("deaths")) ctx.deaths = MiniJson.I(root["deaths"]);
            if (root.ContainsKey("day")) env.day = MiniJson.I(root["day"]);
            if (root.ContainsKey("time")) env.t = MiniJson.F(root["time"]);
            var spawnList = MiniJson.List(root.ContainsKey("spawn") ? root["spawn"] : null);
            if (spawnList.Count >= 2)
                respawnPoint = new Vector3(MiniJson.F(spawnList[0]), GameData.HeightAt(MiniJson.F(spawnList[0]), MiniJson.F(spawnList[1])), MiniJson.F(spawnList[1]));
            ReadFlags(MiniJson.Dict(root.ContainsKey("flags") ? root["flags"] : null));
            ReadInts(MiniJson.Dict(root.ContainsKey("inventory") ? root["inventory"] : null), ctx.inventory);
            ReadInts(MiniJson.Dict(root.ContainsKey("quests") ? root["quests"] : null), ctx.quests);
            player.rig.SetRoot(player.pos, player.yaw);
            if (!boot) AddMessage("已读取存档。", new Color(0.6f, 0.9f, 1f));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("读档失败: " + e.Message);
        }
    }

    void ReadFlags(Dictionary<string, object> d)
    {
        foreach (var kv in d) ctx.flags[kv.Key] = MiniJson.B(kv.Value);
    }

    void ReadInts(Dictionary<string, object> d, Dictionary<string, int> target)
    {
        foreach (var kv in d) target[kv.Key] = MiniJson.I(kv.Value);
    }

    // ---------- HUD 辅助 ----------
    public List<string> StatsLines()
    {
        var lines = new List<string>();
        lines.Add("渲染 " + Screen.width + "×" + Screen.height + "  显卡 " + SystemInfo.graphicsDeviceName);
        lines.Add("区块 " + terrain.Count + " · 道具批次合并 · 敌人 " + enemies.list.Count);
        lines.Add("F1 统计 · F5 存档 · F9 读档 · Esc 释放鼠标");
        return lines;
    }

    void OnGUI()
    {
        if (hud != null) hud.Draw(this);
    }

    void OnApplicationQuit()
    {
        SaveGame(true);
    }
}
