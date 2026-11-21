// Player.cs —— 玩家：相机相对移动 / 跳跃重力 / 游泳 / 翻滚 / 三段剑击 / 体力 / 律令
using UnityEngine;

public struct InputState
{
    public float x, z;
    public bool sprint, jump, roll, attack, law, interact, talkAdvance;
}

public class PlayerController
{
    public Vector3 pos;
    public Vector3 vel;
    public float vy;
    public float yaw;
    public float hp = 100f, maxHp = 100f;
    public float stamina = 100f, maxStamina = 100f;
    public float speed, groundY;
    public bool onGround = true, swimming, dead;
    public float stateT, deadT, invuln, attackCd, combo, attackRequest;
    public float swingT = -1f;
    public int swingIdx;
    public float rollT, rollDirX, rollDirZ;
    public float jumpT, landT, landMax = 0.22f, flinchT, flinchSign = 1f;
    public float lawT, lawEnergy = 30f;
    public float damageBonus;
    public Rig rig;
    public BlobShadow shadow;
    public Transform weaponRoot;
    public Vector3 spawn;
    public float turnRate, prevYaw;

    public string animState = "idle";
    public float speedSmooth = 0f;

    public void Init(Transform parent, Vector3 spawnPos, float spawnYaw)
    {
        spawn = spawnPos;
        pos = spawnPos;
        pos.y = GameData.HeightAt(pos.x, pos.z);
        yaw = spawnYaw;
        maxHp = 100f; hp = maxHp;
        rig = Rig.BuildPlayer(parent, pos, yaw);
        shadow = BlobShadow.Create(parent, 1.15f);
    }

    public void Damage(float amount, Vector3 from)
    {
        if (dead || invuln > 0f) return;
        hp -= amount;
        invuln = 0.45f;
        flinchT = 0.32f;
        Vector3 d = from - pos;
        float side = (-Mathf.Sin(yaw)) * d.z - (-Mathf.Cos(yaw)) * d.x;
        flinchSign = side >= 0f ? -1f : 1f;
        if (hp <= 0f) { hp = 0f; dead = true; deadT = 0f; }
    }

    public void Respawn()
    {
        dead = false;
        deadT = 0f;
        hp = maxHp;
        stamina = maxStamina;
        pos = spawn;
        pos.y = GameData.HeightAt(pos.x, pos.z);
        vel = Vector3.zero;
        vy = 0f;
        invuln = 1.2f;
    }

    public void RequestAttack()
    {
        if (attackCd > 0f || dead) return;
        attackRequest = 0.12f;
    }

    public void Step(GameManager g, float dt, InputState inp, float camYaw)
    {
        if (dead)
        {
            deadT += dt;
            rig.Animate(dt, MakeState(dt, camYaw));
            if (shadow != null) shadow.Follow(pos);
            return;
        }

        if (invuln > 0f) invuln -= dt;
        if (attackCd > 0f) attackCd -= dt;
        if (attackRequest > 0f) attackRequest -= dt;
        if (jumpT > 0f) jumpT -= dt;
        if (landT > 0f) landT -= dt;
        if (flinchT > 0f) flinchT -= dt;
        if (lawT > 0f) lawT -= dt;
        if (rollT > 0f) rollT -= dt;

        Vector3 fwd = new Vector3(-Mathf.Sin(camYaw), 0f, -Mathf.Cos(camYaw));
        Vector3 right = new Vector3(Mathf.Cos(camYaw), 0f, -Mathf.Sin(camYaw));
        Vector3 move = fwd * inp.z + right * inp.x;
        float ln = move.magnitude;
        if (ln > 0.001f) move /= ln;

        bool sprinting = inp.sprint && stamina > 2f && ln > 0.001f && !swimming;
        float maxSpd = swimming ? 4f : (sprinting ? 14f : (ln > 0.001f && onGround ? 10.5f : (onGround ? 6f : 6f)));
        if (!swimming && !sprinting && ln > 0.001f) maxSpd = 10.5f;

        float accel = onGround ? 46f : 12f;
        Vector3 targetVel = move * maxSpd;
        float k = Mathf.Min(1f, accel * dt / Mathf.Max(1f, maxSpd));
        vel.x += (targetVel.x - vel.x) * k;
        vel.z += (targetVel.z - vel.z) * k;
        if (ln > 0.001f)
        {
            float want = Mathf.Atan2(-move.x, -move.z);
            yaw = Mathf.LerpAngle(yaw * Mathf.Rad2Deg, want * Mathf.Rad2Deg, Mathf.Clamp01(dt * 12f)) * Mathf.Deg2Rad;
        }
        turnRate = Mathf.LerpAngle(prevYaw * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg, 1f) * Mathf.Deg2Rad / Mathf.Max(0.001f, dt);
        prevYaw = yaw;

        // 体力
        if (sprinting) stamina = Mathf.Max(0f, stamina - 20f * dt);
        else stamina = Mathf.Min(maxStamina, stamina + (speed < 1f ? 14f : 8f) * dt);

        // 跳跃
        if (inp.jump && onGround && stamina > 8f && !swimming)
        {
            vy = 9.6f;
            jumpT = 0.45f;
            stamina -= 8f;
            onGround = false;
        }
        // 翻滚
        if (inp.roll && rollT <= 0f && stamina > 20f && onGround && !swimming)
        {
            rollT = 0.5f;
            stamina -= 20f;
            invuln = 0.45f;
            Vector3 rd = ln > 0.001f ? move : new Vector3(-Mathf.Sin(yaw), 0f, -Mathf.Cos(yaw));
            rollDirX = rd.x; rollDirZ = rd.z;
        }
        if (rollT > 0f)
        {
            vel.x = rollDirX * 11f;
            vel.z = rollDirZ * 11f;
        }

        // 剑击
        if (attackRequest > 0f && attackCd <= 0f)
        {
            attackRequest = 0f;
            swingT = 0f;
            swingIdx = (int)(combo % 3f);
            combo += 1f;
            attackCd = 0.14f;
            g.OnSwingStart();
        }
        if (swingT >= 0f)
        {
            swingT += dt / 0.33f;
            if (swingT >= 1f) swingT = -1f;
            else g.OnSwingTick(swingT);
        }

        // 律令
        if (inp.law && lawEnergy >= 100f && lawT <= 0f && g.lawUnlocked)
        {
            lawT = 10f;
            lawEnergy = 0f;
            g.AddMessage("「以律令之名——」时间停滞了十息。", new Color(0.55f, 0.9f, 1f));
        }
        lawEnergy = Mathf.Min(100f, lawEnergy + dt * (3.5f * (g.lawUnlocked ? 1f : 0.6f)));

        // 重力 / 位移
        float gAcc = 22f;
        vy -= gAcc * dt;
        Vector3 next = pos + new Vector3(vel.x, 0f, vel.z) * dt;
        float gh = GameData.HeightAt(next.x, next.z);
        float wd = GameData.WaterAt(next.x, next.z);

        // 游泳
        swimming = wd > 0.5f && next.y < 7.2f && gh < 6.4f;
        if (swimming)
        {
            pos.y = Mathf.Max(5.95f, pos.y + vy * dt * 0.2f);
            vy = Mathf.Min(vy, 1f);
            onGround = true;
        }
        else
        {
            pos.y += vy * dt;
            if (pos.y <= gh)
            {
                if (vy < -3f)
                {
                    landMax = vy < -18f ? 0.22f : 0.12f;
                    landT = landMax;
                    g.OnLand(vy < -18f);
                }
                pos.y = gh;
                vy = 0f;
                onGround = true;
            }
            else onGround = false;
        }
        pos.x = Mathf.Clamp(next.x, -3950f, 3950f);
        pos.z = Mathf.Clamp(next.z, -3950f, 3950f);

        if (pos.y < -20f) { dead = true; deadT = 0f; }

        speed = Mathf.Sqrt(vel.x * vel.x + vel.z * vel.z);
        speedSmooth = Mathf.Lerp(speedSmooth, speed, Mathf.Min(1f, dt * 8f));
        if (swimming) animState = "swim";
        else if (dead) animState = "dead";
        else if (rollT > 0f) animState = "roll";
        else if (speedSmooth < 0.6f) animState = "idle";
        else if (speedSmooth < 8.2f) animState = "walk";
        else if (speedSmooth < 12f) animState = "run";
        else animState = "sprint";

        rig.SetRoot(pos, yaw);
        rig.Animate(dt, MakeState(dt, camYaw));
        if (shadow != null) shadow.Follow(pos);
    }

    AnimState MakeState(float dt, float camYaw)
    {
        AnimState st = new AnimState();
        st.speed = speedSmooth;
        st.vel = vel;
        st.yaw = yaw;
        st.camYaw = camYaw;
        st.onGround = onGround;
        st.swimming = swimming;
        st.dead = dead;
        st.hasWeapon = true;
        st.state = animState;
        st.swingT = swingT;
        st.swingIdx = swingIdx;
        st.rollT = rollT;
        st.jumpT = jumpT;
        st.landT = landT;
        st.flinchT = flinchT;
        st.turnRate = Mathf.Clamp(turnRate, -2f, 2f);
        st.accel = 0f;
        return st;
    }

    public Vector3 AimDir(float camYaw, float camPitch)
    {
        return new Vector3(-Mathf.Sin(camYaw) * Mathf.Cos(camPitch), -Mathf.Sin(camPitch), -Mathf.Cos(camYaw) * Mathf.Cos(camPitch));
    }
}
