// Env.cs —— 伪时间（24 分钟一昼夜）/ 天气 / 能量潮汐
using UnityEngine;

public class EnvPalette
{
    public Color zenith, horizon, sun, ambient, fog;
    public float fogFar;
}

public class Env
{
    public const float DAY_LEN = 24f * 60f; // 一昼夜 24 分钟（现实时间）
    public float t = 17f * 60f - 60f;      // 从黄昏前一小时开始
    public int day = 1;
    public float hour = 17f;
    public string phase = "黄昏";
    public int phaseIdx = 1;
    public string weather = "晴空";
    public string weatherKey = "clear";
    string weatherNext;
    float weatherT, weatherLen = 150f, weatherBlend;
    public float rain, dust, crystalAmt, wind = 0.3f;
    public float tide = 0.5f;     // 能量潮汐 0..1
    public string energyState = "平稳";
    public float fogFar = 620f;
    public Color sunColor = new Color(1f, 0.72f, 0.44f);
    public Color ambient = new Color(0.30f, 0.34f, 0.44f);
    public Color fogColor = new Color(0.36f, 0.22f, 0.16f);
    public Color horizon = new Color(0.55f, 0.24f, 0.11f);
    public Color zenith = new Color(0.03f, 0.04f, 0.10f);
    public Vector3 sunDir = new Vector3(0.42f, 0.26f, -0.87f);
    public Color waterColor = new Color(0.13f, 0.24f, 0.30f);
    public float sunIntensity = 1.05f;

    static readonly string[] WeatherKeys = { "clear", "cloudy", "rain", "fog", "dust", "crystal" };
    static readonly string[] WeatherNames = { "晴空", "阴云", "细雨", "浓雾", "尘暴", "晶尘雨" };

    public void Update(float dt, Vector3 playerPos)
    {
        t += dt;
        if (t >= DAY_LEN)
        {
            t -= DAY_LEN;
            day++;
        }
        hour = t / DAY_LEN * 24f;
        phase = PhaseName();
        phaseIdx = (int)(hour / 6f) % 4;

        weatherT += dt;
        if (weatherT > weatherLen)
        {
            weatherT = 0f;
            weatherLen = 110f + (float)Random.value * 150f;
            weatherNext = PickNextWeather();
        }
        weatherBlend = Mathf.Clamp01(weatherT / 4f);

        // 能量潮汐：每游戏日一次峰值
        float cyc = (t % DAY_LEN) / DAY_LEN;
        tide = 0.35f + 0.65f * Mathf.Clamp01(Mathf.Sin(cyc * Mathf.PI * 2f) * 0.5f + 0.5f);
        energyState = tide > 0.72f ? "高能" : (tide < 0.34f ? "低能" : "平稳");

        // 天气参数
        float rainT = 0f, dustT = 0f, cystalT = 0f;
        if (weatherKey == "rain") rainT = 1f;
        else if (weatherKey == "dust") dustT = 1f;
        else if (weatherKey == "crystal") cystalT = 0.8f;
        else if (weatherKey == "cloudy") dustT = 0.15f;
        else if (weatherKey == "fog") dustT = 0.1f;
        rain = Mathf.Lerp(rain, rainT, dt * 1.2f);
        dust = Mathf.Lerp(dust, dustT, dt * 1.2f);
        crystalAmt = Mathf.Lerp(crystalAmt, cystalT, dt * 1.2f);
        wind = Mathf.Lerp(wind, weatherKey == "dust" ? 0.95f : 0.25f, dt * 0.8f);

        // 时间调色
        float ang = (hour / 24f) * Mathf.PI * 2f - Mathf.PI / 2f;
        sunDir = new Vector3(Mathf.Cos(ang) * 0.92f, Mathf.Sin(ang), -0.38f).normalized;
        if (hour < 6f || hour > 20f)
        {
            // 夜晚：月光
            sunColor = new Color(0.55f, 0.62f, 0.85f);
            ambient = new Color(0.16f, 0.19f, 0.30f);
            fogColor = new Color(0.05f, 0.07f, 0.13f);
            horizon = new Color(0.09f, 0.11f, 0.20f);
            zenith = new Color(0.02f, 0.03f, 0.07f);
            sunIntensity = 0.35f;
        }
        else if (hour < 8.5f)
        {
            float u = Mathf.InverseLerp(6f, 8.5f, hour);
            sunColor = Color.Lerp(new Color(1f, 0.52f, 0.30f), new Color(1f, 0.86f, 0.66f), u);
            ambient = Color.Lerp(new Color(0.22f, 0.20f, 0.26f), new Color(0.40f, 0.42f, 0.48f), u);
            fogColor = Color.Lerp(new Color(0.30f, 0.20f, 0.20f), new Color(0.42f, 0.40f, 0.38f), u);
            horizon = Color.Lerp(new Color(0.72f, 0.40f, 0.24f), new Color(0.62f, 0.52f, 0.40f), u);
            zenith = new Color(0.10f, 0.15f, 0.30f);
            sunIntensity = 0.85f;
        }
        else if (hour < 15.5f)
        {
            sunColor = new Color(1f, 0.90f, 0.74f);
            ambient = new Color(0.44f, 0.46f, 0.50f);
            fogColor = new Color(0.52f, 0.50f, 0.46f);
            horizon = new Color(0.66f, 0.62f, 0.52f);
            zenith = new Color(0.16f, 0.28f, 0.48f);
            sunIntensity = 1.25f;
        }
        else
        {
            float u = Mathf.InverseLerp(15.5f, 20f, hour);
            sunColor = Color.Lerp(new Color(1f, 0.72f, 0.42f), new Color(1f, 0.42f, 0.22f), u);
            ambient = Color.Lerp(new Color(0.36f, 0.34f, 0.38f), new Color(0.22f, 0.19f, 0.22f), u);
            fogColor = Color.Lerp(new Color(0.42f, 0.26f, 0.18f), new Color(0.24f, 0.15f, 0.17f), u);
            horizon = Color.Lerp(new Color(0.62f, 0.32f, 0.16f), new Color(0.42f, 0.20f, 0.14f), u);
            zenith = Color.Lerp(new Color(0.10f, 0.12f, 0.24f), new Color(0.04f, 0.05f, 0.12f), u);
            sunIntensity = 0.9f;
        }
        fogFar = weatherKey == "fog" ? 260f : (weatherKey == "dust" ? 380f : (weatherKey == "crystal" ? 500f : 620f));
        fogColor = Color.Lerp(fogColor, weatherKey == "rain" ? new Color(0.32f, 0.34f, 0.38f) : fogColor, rain);
        waterColor = Color.Lerp(new Color(0.10f, 0.20f, 0.26f), new Color(0.16f, 0.22f, 0.28f), tide);

        if (weatherKey == "cloudy") sunIntensity *= 0.8f;
        if (weatherKey == "rain") sunIntensity *= 0.55f;
        if (weatherKey == "dust") ambient *= 1.06f;
        sunIntensity *= 0.85f + 0.3f * tide;
    }

    string PickNextWeather()
    {
        float r = Random.value;
        if (tide > 0.75f && r < 0.35f) { weatherKey = "crystal"; weather = "晶尘雨"; return weather; }
        if (r < 0.34f) { weatherKey = "clear"; weather = "晴空"; }
        else if (r < 0.55f) { weatherKey = "cloudy"; weather = "阴云"; }
        else if (r < 0.72f) { weatherKey = "rain"; weather = "细雨"; }
        else if (r < 0.86f) { weatherKey = "fog"; weather = "浓雾"; }
        else { weatherKey = "dust"; weather = "尘暴"; }
        return weather;
    }

    public string PhaseName()
    {
        if (hour < 5f) return "午夜";
        if (hour < 8f) return "黎明";
        if (hour < 16f) return "白昼";
        if (hour < 20f) return "黄昏";
        return "夜晚";
    }

    public string ClockText()
    {
        return "第 " + day + " 日  " + Mathf.FloorToInt(hour).ToString("00") + ":" + Mathf.FloorToInt((hour - Mathf.Floor(hour)) * 60f).ToString("00");
    }
}
