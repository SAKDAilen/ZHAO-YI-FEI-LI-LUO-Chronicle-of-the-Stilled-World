// Enemies.cs —— 敌人 AI 管理（构造体 / 飞翼幽魂 / 熔岩兽）
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager
{
    public List<EnemyRig> list = new List<EnemyRig>();
    Transform root;
    System.Random rng = new System.Random(20261121);
    float spawnTimer = 4f;

    public void Init(Transform parent)
    {
        root = Make.Node("敌人", parent, Vector3.zero);
    }

    public EnemyRig Spawn(string kind, Vector3 pos, int level)
    {
        var e = EnemyRig.Build(kind, root, pos, level);
        e.SetPos(pos, 0f);
        list.Add(e);
        return e;
    }

    public float NearestDist(Vector3 p, out EnemyRig near)
    {
        near = null;
        float best = 1e9f;
        for (int k = 0; k < list.Count; k++)
        {
            var e = list[k];
            if (e.dead) continue;
            float d = (e.pos - p).magnitude;
            if (d < best) { best = d; near = e; }
        }
        return best;
    }

    public EnemyRig NearestAlive(Vector3 p, float maxDist)
    {
        EnemyRig near;
        float d = NearestDist(p, out near);
        return d <= maxDist ? near : null;
    }

    public void Update(GameManager g, float dt, Vector3 playerPos)
    {
        for (int k = list.Count - 1; k >= 0; k--)
        {
            var e = list[k];
            e.TickTimers(dt);
            if (e.dead)
            {
                e.Animate(dt);
                if (e.deathT > 1.1f)
                {
                    e.Destroy();
                    list.RemoveAt(k);
                }
                continue;
            }
            Vector3 d = playerPos - e.pos;
            float dist = d.magnitude;
            float yawWant = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            e.yaw = Mathf.LerpAngle(e.yaw, yawWant, Mathf.Clamp01(dt * 3f));
            float spd = 0f;
            if (dist < 24f)
            {
                if (dist > e.atkRange + 0.4f)
                {
                    spd = e.speed;
                }
                else if (e.attackT <= 0f && e.hitT <= 0f)
                {
                    e.attackT = 0.55f;
                    e.animWalk = spd;
                }
            }
            if (spd > 0f)
            {
                Vector3 step = d.normalized * spd * dt;
                step.y = 0f;
                Vector3 np = e.pos + step;
                np.y = GameData.HeightAt(np.x, np.z);
                e.pos = np;
            }
            // 群体分离
            for (int j = 0; j < list.Count; j++)
            {
                if (j == k) continue;
                var o = list[j];
                if (o.dead) continue;
                Vector3 dd = e.pos - o.pos;
                dd.y = 0f;
                float m = dd.magnitude;
                float minD = e.radius + o.radius;
                if (m > 0.001f && m < minD)
                {
                    e.pos += dd / m * (minD - m) * 0.5f;
                }
            }
            if (dist < e.atkRange + 0.9f && e.attackT > 0f && e.attackT < 0.28f && e.hitT <= 0f)
            {
                e.hitT = 0.3f;
                g.HurtPlayer(e.dmg, e.pos);
            }
            e.animWalk = Mathf.Lerp(e.animWalk, spd, Mathf.Clamp01(dt * 6f));
            e.SetPos(e.pos, e.yaw * Mathf.Deg2Rad);
            e.Animate(dt);
        }

        // 刷怪
        spawnTimer -= dt;
        int alive = 0;
        for (int k = 0; k < list.Count; k++) if (!list[k].dead) alive++;
        if (alive < 4 && spawnTimer <= 0f)
        {
            spawnTimer = 4.5f;
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = 16f + (float)rng.NextDouble() * 18f;
            float x = playerPos.x + Mathf.Cos(ang) * rad;
            float z = playerPos.z + Mathf.Sin(ang) * rad;
            x = Mathf.Clamp(x, -3900f, 3900f);
            z = Mathf.Clamp(z, -3900f, 3900f);
            float y = GameData.HeightAt(x, z);
            if (GameData.InLake(x, z) || y < 2f) return;
            string[] kinds = { "construct", "winged", "lava" };
            string kind = kinds[rng.Next(kinds.Length)];
            int level = Mathf.Clamp(1 + g.kills / 6, 1, 6);
            Spawn(kind, new Vector3(x, y, z), level);
        }
    }
}
