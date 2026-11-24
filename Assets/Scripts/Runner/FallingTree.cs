using UnityEngine;

// ต้นไม้ข้างทาง ล้มลงมาขวางเลนเมื่อผู้เล่นเข้าใกล้ (ลำต้นกระโดดข้ามได้ พุ่มใบต้องหลบเลน)
public class FallingTree : Obstacle
{
    public Transform pivot;          // จุดหมุนที่โคนต้น
    public float fallDirection = 1f; // 1 = ล้มไปทางซ้าย, -1 = ล้มไปทางขวา
    public RunnerConfig config;
    float fallT = -1f;               // -1 = ยังไม่ล้ม

    protected override void Update()
    {
        base.Update();
        if (!pivot || !config) return;
        if (fallT < 0f && transform.position.z < config.treeFallDistance)
        {
            fallT = 0f;
            Debug.Log("[FallingTree] ต้นไม้เริ่มล้ม");
        }
        if (fallT < 0f || fallT >= 1f) return;
        fallT = Mathf.Min(1f, fallT + Time.deltaTime / config.treeFallTime);
        float eased = fallT * fallT;                                  // ล้มช้าแล้วเร็วขึ้นเหมือนแรงโน้มถ่วง
        pivot.localRotation = Quaternion.Euler(0f, 0f, 90f * eased * fallDirection);
        if (fallT >= 1f) RunnerCamera.Shake(0.15f);                  // กระแทกพื้น
    }
}
