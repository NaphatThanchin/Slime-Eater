using UnityEngine;

// ค่าปรับเกมวิ่ง 3D ทั้งหมด (แก้ได้ที่ Assets/Data/RunnerConfig.asset)
[CreateAssetMenu(fileName = "RunnerConfig", menuName = "Slime Eater/Runner Config")]
public class RunnerConfig : ScriptableObject
{
    [Header("เลน / การเคลื่อนที่")]
    public float laneWidth = 2.4f;        // ระยะห่างระหว่างเลน
    public float laneSwitchSpeed = 14f;   // ความเร็วเปลี่ยนเลน (m/s)
    public float jumpVelocity = 9f;
    public float gravity = 28f;
    public float slideDuration = 0.7f;    // สไลด์นานกี่วินาที
    [Header("เลือด")]
    public int obstacleDamage = 34;       // ชน 3 ครั้งแพ้
    public float invincibleTime = 1f;
    public int healAmount = 40;           // หัวใจฮีล 40%
    public float healEvery = 400f;        // หัวใจออกทุก 400 m
    [Header("การสร้างด่าน")]
    public float spawnZ = 75f;            // สร้างของไกลแค่ไหน
    public float firstSpawnAt = 45f;      // ช่วงแรกไม่มีสิ่งกีดขวาง
    public float minGap = 16f;            // ระยะห่างระหว่างชุดสิ่งกีดขวาง
    public float maxGap = 24f;
    public float treeFallDistance = 32f;  // ต้นไม้เริ่มล้มเมื่อห่างผู้เล่นเท่านี้
    public float treeFallTime = 0.9f;
}
