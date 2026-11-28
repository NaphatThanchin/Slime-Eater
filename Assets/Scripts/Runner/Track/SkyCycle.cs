using System.Collections;
using UnityEngine;

// บรรยากาศเปลี่ยนทุก 1000 m: กลางวัน -> เย็น (เวลาล่า กินบอทได้มากขึ้น) -> กลางคืน (สไลม์เรืองแสง หมอกใกล้) แล้ววนซ้ำ ค่อยๆ เปลี่ยนไม่หยุดเกม
public class SkyCycle : MonoBehaviour
{
    public enum Phase { Day, Evening, Night }
    public static SkyCycle Instance { get; private set; }
    public static Phase Now => (Phase)((Mathf.Max(1, GameSession.Stage) - 1) % 3);
    public static bool Evening => Now == Phase.Evening;
    public Light sun;
    public Transform glowTarget;      // ผู้เล่น (กลางคืนเรืองแสง)
    struct Look { public Color sky, ground, fog, amb0, amb1, amb2, sunCol; public float exposure, thick, fogStart, fogEnd, sunInt; public Vector3 sunRot; }
    Look day;
    Material sky;
    Light glow;
    Renderer[] glowRenderers;
    Coroutine blend;

    void Awake() => Instance = this;

    IEnumerator Start()
    {
        if (RenderSettings.skybox) { sky = new Material(RenderSettings.skybox); RenderSettings.skybox = sky; } // copy ก่อนแก้ ไม่ให้ไฟล์จริงเปลี่ยน
        day = Current();
        yield return null;                                                        // รอโมเดลตัวละครสร้างเสร็จ
        if (glowTarget)
        {
            glow = new GameObject("SlimeGlow").AddComponent<Light>();             // แสงเรืองรอบตัวสไลม์ (กลางคืน)
            glow.type = LightType.Point; glow.range = 7f; glow.intensity = 0f; glow.color = new Color(0.55f, 1f, 0.7f);
            glow.transform.SetParent(glowTarget, false);
            glow.transform.localPosition = new Vector3(0f, 1.2f, 0.5f);
            glowRenderers = glowTarget.GetComponentsInChildren<Renderer>();
        }
        Apply(true);
    }

    public void Apply(bool instant) // ไปบรรยากาศของฉากปัจจุบัน (ค่อยๆ เปลี่ยน 3 วิ)
    {
        if (blend != null) StopCoroutine(blend);
        blend = StartCoroutine(Blend(Target(Now), instant ? 0f : 3f));
    }

    Look Target(Phase p)
    {
        if (p == Phase.Day) return day;
        bool eve = p == Phase.Evening;
        var l = day;
        l.sky = eve ? new Color(1f, 0.55f, 0.35f) : new Color(0.12f, 0.16f, 0.35f);
        l.ground = eve ? new Color(0.5f, 0.3f, 0.25f) : new Color(0.05f, 0.06f, 0.1f);
        l.exposure = eve ? 0.9f : 0.25f; l.thick = eve ? 2.2f : 0.6f;
        l.fog = eve ? new Color(0.95f, 0.6f, 0.42f) : new Color(0.1f, 0.13f, 0.24f);
        l.fogEnd = eve ? 150f : 90f; l.fogStart = eve ? 25f : 12f;                // กลางคืนเห็นอุปสรรคช้าลง
        l.amb0 = eve ? new Color(0.95f, 0.6f, 0.5f) : new Color(0.3f, 0.36f, 0.6f);
        l.amb1 = eve ? new Color(0.75f, 0.5f, 0.35f) : new Color(0.24f, 0.3f, 0.42f);
        l.amb2 = eve ? new Color(0.35f, 0.22f, 0.15f) : new Color(0.12f, 0.13f, 0.18f);
        l.sunCol = eve ? new Color(1f, 0.55f, 0.3f) : new Color(0.55f, 0.65f, 1f);
        l.sunInt = eve ? 1.1f : 0.45f;
        l.sunRot = new Vector3(eve ? 12f : 35f, eve ? -60f : 30f, 0f);           // เย็น = แดดต่ำ เงายาว
        return l;
    }

    Look Current()
    {
        var l = new Look
        {
            fog = RenderSettings.fogColor, fogStart = RenderSettings.fogStartDistance, fogEnd = RenderSettings.fogEndDistance,
            amb0 = RenderSettings.ambientSkyColor, amb1 = RenderSettings.ambientEquatorColor, amb2 = RenderSettings.ambientGroundColor,
            sunCol = sun ? sun.color : Color.white, sunInt = sun ? sun.intensity : 1f, sunRot = sun ? sun.transform.eulerAngles : Vector3.zero,
        };
        if (sky && sky.HasProperty("_SkyTint")) { l.sky = sky.GetColor("_SkyTint"); l.ground = sky.GetColor("_GroundColor"); l.exposure = sky.GetFloat("_Exposure"); l.thick = sky.GetFloat("_AtmosphereThickness"); }
        return l;
    }

    IEnumerator Blend(Look to, float time)
    {
        var from = Current();
        bool night = Now == Phase.Night;
        for (float t = 0f; ; t += Time.deltaTime)
        {
            float k = time <= 0f ? 1f : Mathf.Clamp01(t / time);
            RenderSettings.fogColor = Color.Lerp(from.fog, to.fog, k);
            RenderSettings.fogStartDistance = Mathf.Lerp(from.fogStart, to.fogStart, k);
            RenderSettings.fogEndDistance = Mathf.Lerp(from.fogEnd, to.fogEnd, k);
            RenderSettings.ambientSkyColor = Color.Lerp(from.amb0, to.amb0, k);
            RenderSettings.ambientEquatorColor = Color.Lerp(from.amb1, to.amb1, k);
            RenderSettings.ambientGroundColor = Color.Lerp(from.amb2, to.amb2, k);
            if (sky && sky.HasProperty("_SkyTint"))
            {
                sky.SetColor("_SkyTint", Color.Lerp(from.sky, to.sky, k)); sky.SetColor("_GroundColor", Color.Lerp(from.ground, to.ground, k));
                sky.SetFloat("_Exposure", Mathf.Lerp(from.exposure, to.exposure, k)); sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(from.thick, to.thick, k));
            }
            if (sun) { sun.color = Color.Lerp(from.sunCol, to.sunCol, k); sun.intensity = Mathf.Lerp(from.sunInt, to.sunInt, k); sun.transform.rotation = Quaternion.Slerp(Quaternion.Euler(from.sunRot), Quaternion.Euler(to.sunRot), k); }
            if (glow) glow.intensity = Mathf.Lerp(glow.intensity, night ? 2.2f : 0f, k);
            var cam = Camera.main;
            if (cam && cam.clearFlags == CameraClearFlags.SolidColor) cam.backgroundColor = RenderSettings.fogColor;
            if (k >= 1f) break;
            yield return null;
        }
        if (glowRenderers != null)                                                // กลางคืน: ตัวสไลม์เรืองแสง
            foreach (var r in glowRenderers)
                if (r) foreach (var m in r.materials)
                    if (m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", night ? new Color(0.25f, 0.6f, 0.35f) : Color.black); }
        blend = null;
    }
}
