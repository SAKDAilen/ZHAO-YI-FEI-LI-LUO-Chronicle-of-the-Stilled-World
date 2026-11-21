// CameraRig.cs —— 顶级第三人称相机（旷野之息式）：四模式 / 肩偏移 / 俯仰补偿 / 地形碰撞 / 双速率平滑 / 震屏
using UnityEngine;

public class CameraRig
{
    public Camera cam;
    public float yaw, pitch = -0.14f;
    public float zoomT = 1f;
    public float dist = 6.6f, hitDist = 6.6f, fov = 62f;
    public Vector3 pos = Vector3.zero, lookS = Vector3.zero;
    public float shakeAmt, lastLookT;
    public Transform lockTarget;
    public float aspect = 16f / 9f;
    public float targetDist = 6.6f;

    Vector3 smoothPos;
    bool init;

    public void Init(Camera c)
    {
        cam = c;
        cam.fieldOfView = 62f;
        cam.nearClipPlane = 0.12f;
        cam.farClipPlane = 2600f;
    }

    public void OnWheel(float sy)
    {
        zoomT = Mathf.Clamp(zoomT - sy * GameData.Cam.zoomStep, GameData.Cam.zoomMin, GameData.Cam.zoomMax);
    }

    public void Shake(float amt)
    {
        shakeAmt = Mathf.Max(shakeAmt, amt);
    }

    public void Update(float dt, PlayerController p, Vector3 vel, bool sprinting, bool aiming,
                       bool combatNear, float rollT, float speed)
    {
        var cfg = GameData.Cam;

        // 锁定视角
        if (lockTarget != null)
        {
            Vector3 d = lockTarget.position - p.pos;
            float wantYaw = Mathf.Atan2(d.x, d.z);
            yaw = Mathf.LerpAngle(yaw * Mathf.Rad2Deg, wantYaw * Mathf.Rad2Deg, Mathf.Clamp01(dt * 6f)) * Mathf.Deg2Rad;
            lastLookT = 0f;
        }
        else if (sprinting && speed > 6.5f)
        {
            lastLookT += dt;
            if (lastLookT > cfg.autoDelay)
                yaw = Mathf.LerpAngle(yaw * Mathf.Rad2Deg, p.yaw * Mathf.Rad2Deg, Mathf.Clamp01(dt * cfg.autoSpeed)) * Mathf.Deg2Rad;
        }
        else lastLookT = 0f;

        // 模式
        string mode = "explore";
        if (aiming) mode = "aim";
        else if (sprinting && speed > GameData.Cam.modeDist["explore"][0] + 3f) mode = "sprint";
        else if (combatNear) mode = "combat";
        float[] md;
        if (!cfg.modeDist.TryGetValue(mode, out md)) md = cfg.modeDist["explore"];

        // 俯仰补偿
        float pitchMult = 1f;
        if (cfg.pitchCurve.Length > 1)
        {
            float t = Mathf.InverseLerp(cfg.pitchMin, cfg.pitchMax, pitch);
            float f = t * (cfg.pitchCurve.Length - 1);
            int i0 = Mathf.FloorToInt(f);
            int i1 = Mathf.Min(cfg.pitchCurve.Length - 1, i0 + 1);
            pitchMult = Mathf.Lerp(cfg.pitchCurve[i0], cfg.pitchCurve[i1], f - i0);
        }
        targetDist = Mathf.Clamp(md[0] * zoomT * pitchMult, 1.3f, 14f);

        dist = Mathf.Lerp(dist, targetDist, 1f - Mathf.Exp(-cfg.smoothDist * dt));

        // 视线点（前瞻 + 俯仰补偿）
        Vector3 wantLook = p.pos + new Vector3(0f, 1.52f - pitch * 0.30f, 0f);
        Vector3 ahead = vel * 0.14f;
        ahead.x = Mathf.Clamp(ahead.x, -cfg.lookAhead, cfg.lookAhead);
        ahead.z = Mathf.Clamp(ahead.z, -cfg.lookAhead, cfg.lookAhead);
        ahead.y = 0f;
        wantLook += ahead;
        if (!init) { lookS = wantLook; smoothPos = p.pos; init = true; }
        lookS = Vector3.Lerp(lookS, wantLook, 1f - Mathf.Exp(-cfg.smoothLook * dt));

        // 理想机位（球面）
        Vector3 back = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
        float horiz = dist * Mathf.Cos(pitch);
        float vert = dist * Mathf.Sin(pitch) + 1.10f;
        Vector3 pivot = p.pos + new Vector3(0f, 1.50f, 0f);
        Vector3 ideal = pivot + back * horiz + Vector3.up * vert;

        // 地形碰撞（沿 pivot→ideal 采样）
        float hit = dist;
        Vector3 dir = ideal - pivot;
        float len = dir.magnitude;
        if (len > 0.01f)
        {
            Vector3 dn = dir / len;
            int steps = Mathf.CeilToInt(len / 0.45f);
            for (int k = 1; k <= steps; k++)
            {
                float d = len * k / steps;
                Vector3 sp = pivot + dn * d;
                float gh = GameData.HeightAt(sp.x, sp.z);
                if (sp.y < gh + cfg.collideMargin)
                {
                    hit = Mathf.Max(1.2f, d - 0.25f);
                    break;
                }
            }
        }
        float targetHit = Mathf.Min(dist, hit);
        hitDist = Mathf.Lerp(hitDist, targetHit, 1f - Mathf.Exp(-cfg.smoothDistHit * dt));

        // 肩部偏移（低俯角时滑向肩侧）
        float shoulderW = cfg.shoulderX * Mathf.Clamp01(1f - pitch / Mathf.Max(0.01f, cfg.shoulderFade));
        Vector3 right = new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw));
        Vector3 finalPos = pivot + back * (hitDist * Mathf.Cos(pitch)) + Vector3.up * (hitDist * Mathf.Sin(pitch) + 1.10f)
                           + right * shoulderW;

        // 贴地 / 入水
        float groundHere = GameData.HeightAt(finalPos.x, finalPos.z) + 0.35f;
        if (finalPos.y < groundHere) finalPos.y = groundHere;
        if (GameData.InLake(finalPos.x, finalPos.z) && finalPos.y < 7.8f) finalPos.y = 7.8f;

        // 震屏
        if (shakeAmt > 0.001f)
        {
            finalPos += new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * shakeAmt * 0.18f;
            shakeAmt *= Mathf.Exp(-dt * 6f);
        }
        if (sprinting && speed > 11f)
        {
            finalPos += new Vector3(Mathf.Sin(Time.time * 23f), Mathf.Sin(Time.time * 31f), 0f) * 0.006f;
        }

        // 平滑：水平快、垂直稍慢，避免上下抖动
        float rate = cfg.smoothPos;
        smoothPos.x = Mathf.Lerp(smoothPos.x, finalPos.x, 1f - Mathf.Exp(-rate * dt));
        smoothPos.z = Mathf.Lerp(smoothPos.z, finalPos.z, 1f - Mathf.Exp(-rate * dt));
        smoothPos.y = Mathf.Lerp(smoothPos.y, finalPos.y, 1f - Mathf.Exp(-rate * 1.6f * dt));
        pos = smoothPos;

        cam.transform.position = pos;
        Vector3 lookDir = lookS - pos;
        if (lookDir.sqrMagnitude > 0.0001f)
            cam.transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

        // FOV
        float targetFov = md[1] + Mathf.Clamp(speed * 0.35f, 0f, 6f) + (rollT > 0f ? 4f : 0f);
        fov = Mathf.Lerp(fov, targetFov, 1f - Mathf.Exp(-cfg.smoothFov * dt));
        cam.fieldOfView = fov;
    }

    public void Look(float dx, float dy, float sens)
    {
        var cfg = GameData.Cam;
        yaw += dx * 0.0022f * sens;
        pitch += dy * 0.0022f * sens;
        pitch = Mathf.Clamp(pitch, cfg.pitchMin, cfg.pitchMax);
        lastLookT = 0f;
        if (lockTarget != null) lockTarget = null;
    }
}
