using UnityEngine;

// ท่อนไม้ยาวหล่นจากฟ้าลงกลางเลน: เงาแดงบนพื้นโตขึ้นเตือนก่อน แล้วไม้ตกกระแทก = ต้องหลบเลน
public class FallingLog : Obstacle
{
    public Transform log;            // โมเดลไม้ (เริ่มอยู่สูง)
    public Transform shadow;         // เงาเตือนบนพื้น
    public float startHeight = 14f;
    public float fallTime = 0.7f;    // ใช้เวลาตกกี่วิ
    public float landBefore = 1.4f;  // ตกถึงพื้นก่อนถึงผู้เล่นกี่วิ (มีเวลาหลบ)
    float t = -1f;                    // -1 = ยังไม่ตก

    protected override void Update()
    {
        base.Update();
        var w = WorldScroller.Instance;
        if (!log || !w || t >= 1f) return;
        float v = Mathf.Max(1f, w.CurrentSpeed);
        if (t < 0f && transform.position.z < v * (landBefore + fallTime)) t = 0f;   // ถึงจังหวะปล่อยไม้
        float k = t < 0f ? 0f : (t = Mathf.Min(1f, t + Time.deltaTime / fallTime));
        log.localPosition = new Vector3(0f, Mathf.Lerp(startHeight, 0.5f, k * k), 0f); // ตกเร็วขึ้นเรื่อยๆ
        if (shadow) shadow.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, k);
        if (k >= 1f) { RunnerCamera.Shake(0.12f); if (shadow) shadow.gameObject.SetActive(false); }
    }
}
