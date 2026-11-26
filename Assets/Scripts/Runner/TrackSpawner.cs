using UnityEngine;

// สุ่มชุดสิ่งกีดขวาง/ไอเทม: ระยะห่างคิดเป็นวินาที (หลบทันทุกความเร็ว) และสุ่มใหม่ทุกครั้งที่เริ่มด่าน
public class TrackSpawner : MonoBehaviour
{
    enum Need { None, Jump, Slide, Lane }  // สิ่งที่ผู้เล่นต้องทำเพื่อผ่านชุดนั้น
    public RunnerConfig config;
    public Obstacle rockPrefab;            // หิน: กระโดด
    public Obstacle highLogPrefab;         // ท่อนไม้สูง: สไลด์
    public Obstacle boulderPrefab;         // หินใหญ่: หลบเลน
    public Obstacle treeFromRightPrefab, treeFromLeftPrefab; // ต้นไม้ล้ม
    public Collectible coinPrefab, heartPrefab, hintLeafPrefab;
    public Collectible[] powerUpPrefabs;   // แม่เหล็ก / เกราะ / เหรียญ x2
    public float stopBeforeBoss = 40f;     // ใกล้บอสแล้วเลิกสร้าง
    [Range(0f, 1f)] public float powerUpChance = 0.15f;
    public float[] hintLeafAt = { 250f, 550f, 850f };
    public float minGapTime = 1.15f;       // ห่างกันอย่างน้อย (วินาที) = ลอยตัวตอนกระโดด 0.64 วิ + เวลาตอบสนอง
    public float switchExtra = 0.35f;      // ต้องสลับกระโดด <-> สไลด์ ให้เวลาเพิ่ม
    System.Random rng;
    float sinceSpawn, nextGap, nextHeal;
    int nextLeaf, pending, tutorialStep;   // pending = ชุดถัดไปที่เลือกไว้ล่วงหน้า

    float Speed => WorldScroller.Instance ? WorldScroller.Instance.BaseSpeed : 11f;
    int MaxPattern => Mathf.Min(9, 6 + GameSession.Stage); // ด่านสูงปลดล็อกชุดยาก

    void Start()
    {
        int seed = System.Environment.TickCount;     // เมล็ดสุ่มใหม่ทุกด่าน -> อุปสรรคไม่ซ้ำเดิม
        rng = new System.Random(seed);
        nextGap = config.firstSpawnAt;
        nextHeal = config.healEvery;
        pending = rng.Next(MaxPattern);
        Debug.Log("[TrackSpawner] ด่าน " + GameSession.Stage + " seed " + seed);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.RunnerPhase || GameSession.Distance > GameSession.TargetDistance - stopBeforeBoss) return;
        sinceSpawn += WorldScroller.FrameDelta;
        if (TutorialGuide.Active) { if (TutorialGuide.ReadyForNext && sinceSpawn > 8f) { sinceSpawn = 0f; SpawnTutorial(); } return; } // สอนเล่น: วางทีละบท
        if (sinceSpawn < nextGap) return;
        sinceSpawn = 0f;
        Need end = SpawnNext(out float length);
        float minT = Mathf.Max(minGapTime, 1.7f - 0.08f * (GameSession.Stage - 1)); // ด่านสูงถี่ขึ้นแต่ไม่ต่ำกว่า minGapTime
        float t = length + minT + (float)rng.NextDouble() * 0.8f;
        if (IsSwitch(end, StartNeed(pending))) t += switchExtra;
        nextGap = t * Speed;                                                          // แปลงวินาทีเป็นเมตรตามความเร็วด่าน
    }

    Need SpawnNext(out float length) // วางชุดถัดไป คืนค่าสิ่งที่ต้องทำท้ายชุด + ความยาวชุด (วินาที)
    {
        length = 0f;
        float z = config.spawnZ;
        int a = rng.Next(-1, 2), b = (a + rng.Next(1, 3) + 1) % 3 - 1, free = -a - b; // 3 เลนไม่ซ้ำกัน
        if (GameSession.Distance >= nextHeal) { Spawn(heartPrefab, X(a), 0.9f, z); nextHeal += config.healEvery; return Need.None; }
        if (nextLeaf < hintLeafAt.Length && GameSession.Distance >= hintLeafAt[nextLeaf]) // ใบไม้วิเศษบนหิน ต้องกระโดดเก็บ
        {
            Spawn(rockPrefab, X(a), 0f, z);
            Spawn(hintLeafPrefab, X(a), 2.1f, z);
            nextLeaf++;
            return Need.Jump;
        }
        int p = pending;
        pending = rng.Next(MaxPattern);
        switch (p)
        {
            case 0: Spawn(rockPrefab, X(a), 0f, z); CoinLine(b, z - 4f, 5); break;                         // หิน 1 ก้อน + เหรียญเลนอื่น
            case 1: Spawn(rockPrefab, X(a), 0f, z); Spawn(rockPrefab, X(b), 0f, z); CoinArc(a, z); break;  // หิน 2 เลน + เหรียญโค้ง
            case 2: Spawn(boulderPrefab, X(a), 0f, z); Spawn(rockPrefab, X(b), 0f, z); CoinLine(free, z - 3f, 4); break; // เหลือเลนว่าง 1 เลน
            case 3: Spawn(highLogPrefab, 0f, 0f, z); CoinLine(a, z - 2f, 3, 0.45f); break;                 // ท่อนไม้สูง ต้องสไลด์
            case 4:                                                                                         // ต้นไม้ล้มขวางทาง
                bool right = rng.Next(2) == 0;
                Spawn(right ? treeFromRightPrefab : treeFromLeftPrefab, 0f, 0f, z);
                CoinArc(right ? 1 : -1, z);
                break;
            case 5: for (int i = 0; i < 9; i++) Spawn(coinPrefab, X(i / 3 % 2 == 0 ? a : b), 0.7f, z + i * 2f); break; // เหรียญซิกแซก
            case 6: for (int l = -1; l <= 1; l++) Spawn(rockPrefab, X(l), 0f, z); CoinArc(free, z); break; // กำแพงหิน ต้องกระโดด
            case 7:                                                                                         // หลบเลน แล้วสไลด์ (ห่าง 1 วิ)
                length = 1f;
                Spawn(boulderPrefab, X(a), 0f, z); Spawn(boulderPrefab, X(b), 0f, z);
                Spawn(highLogPrefab, 0f, 0f, z + Mathf.Max(12f, Speed * length));
                break;
            default:                                                                                        // ต้นไม้ล้ม 2 ต้นคนละฝั่ง (ห่าง 1.1 วิ)
                length = 1.1f;
                Spawn(treeFromRightPrefab, 0f, 0f, z);
                Spawn(treeFromLeftPrefab, 0f, 0f, z + Mathf.Max(14f, Speed * length));
                break;
        }
        if (rng.NextDouble() < powerUpChance && powerUpPrefabs.Length > 0) // ไอเทมพลังพิเศษเหนือเลนว่าง
            Spawn(powerUpPrefabs[rng.Next(powerUpPrefabs.Length)], X(free), 1.2f, z - 8f);
        return EndNeed(p);
    }

    void SpawnTutorial() // บทเรียน: 0 กำแพงหิน (กระโดด) 1 ท่อนไม้ (สไลด์) 2 หินใหญ่เลนกลาง+ขวา (หลบซ้าย) 3 หัวใจเลนซ้าย
    {
        const float z = 40f;
        GameObject watch;
        if (tutorialStep == 0) { Spawn(rockPrefab, X(-1), 0f, z); Spawn(rockPrefab, X(1), 0f, z); watch = Spawn(rockPrefab, X(0), 0f, z); }
        else if (tutorialStep == 1) watch = Spawn(highLogPrefab, 0f, 0f, z);
        else if (tutorialStep == 2) { Spawn(boulderPrefab, X(1), 0f, z); watch = Spawn(boulderPrefab, X(0), 0f, z); }
        else watch = Spawn(heartPrefab, X(-1), 0.9f, z);
        TutorialGuide.Instance.Watch(watch.transform, tutorialStep++);
    }

    static Need StartNeed(int p) => p == 3 ? Need.Slide : p == 2 || p == 7 ? Need.Lane : p == 5 ? Need.None : Need.Jump;
    static Need EndNeed(int p) => p == 7 || p == 3 ? Need.Slide : p == 2 ? Need.Lane : p == 5 ? Need.None : Need.Jump;
    static bool IsSwitch(Need a, Need b) => (a == Need.Jump && b == Need.Slide) || (a == Need.Slide && b == Need.Jump);
    float X(int lane) => lane * config.laneWidth;

    void CoinLine(int lane, float z, int count, float y = 0.7f) { for (int i = 0; i < count; i++) Spawn(coinPrefab, X(lane), y, z + i * 2f); }

    void CoinArc(int lane, float z) // เหรียญโค้งตามแนวกระโดด
    {
        for (int i = 0; i < 5; i++) { float t = (i - 2) / 2f; Spawn(coinPrefab, X(lane), 2.1f - t * t, z + t * 3f); }
    }

    GameObject Spawn(Component prefab, float x, float y, float z) => Instantiate(prefab.gameObject, new Vector3(x, y, z), prefab.transform.rotation, transform);
}
