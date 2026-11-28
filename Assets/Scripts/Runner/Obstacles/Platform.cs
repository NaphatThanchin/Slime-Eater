using System.Collections.Generic;
using UnityEngine;

// ทางชั้นบน: ทางลาดขึ้น (ramp) แล้วขอนไม้ยาววิ่งบนหลังได้ ชนด้านหน้า/ข้างตอนอยู่ระดับพื้น = ชน
public class Platform : Obstacle
{
    public static readonly List<Platform> All = new List<Platform>();
    public float rampLength = 5f;     // ทางลาดเริ่มที่ z ของวัตถุ
    public float length = 16f;        // รวมทางลาด
    public float height = 1.6f;

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    float Height(float z) // ความสูงพื้นที่ระยะ z (โลก)
    {
        float d = z - transform.position.z;
        if (d < 0f || d > length) return 0f;
        return d < rampLength ? height * d / rampLength : height;
    }

    public static float HeightAt(Vector3 p) // พื้นสูงสุดใต้ตำแหน่งนี้ (ก้าวขึ้นได้ไม่เกิน 0.5 m = ไม่ปีนขึ้นจากด้านข้าง)
    {
        float g = 0f;
        foreach (var pl in All)
        {
            if (Mathf.Abs(p.x - pl.transform.position.x) > 1f) continue;
            float h = pl.Height(p.z);
            if (h > g && h <= p.y + 0.5f) g = h;
        }
        return g;
    }
}
