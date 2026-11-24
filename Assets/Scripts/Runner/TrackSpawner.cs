using UnityEngine;

// สุ่มสร้างชุดสิ่งกีดขวาง/ไอเทมตามเลน ด่านยิ่งสูงยิ่งถี่และมีชุดยากขึ้น
public class TrackSpawner : MonoBehaviour
{
    public RunnerConfig config;
    public Obstacle rockPrefab;          // หิน: กระโดด
    public Obstacle highLogPrefab;       // ท่อนไม้สูง: สไลด์
    public Obstacle boulderPrefab;       // หินใหญ่: หลบเลน
    public Obstacle treeFromRightPrefab; // ต้นไม้ล้มจากขวา
    public Obstacle treeFromLeftPrefab;  // ต้นไม้ล้มจากซ้าย
    public Collectible coinPrefab;
    public Collectible heartPrefab;
    public Collectible hintLeafPrefab;   // ใบไม้วิเศษ (ช่วยในฉากบอส)
    public Collectible[] powerUpPrefabs; // แม่เหล็ก / เกราะ / เหรียญ x2
    public float stopBeforeBoss = 40f;   // ใกล้บอสแล้วเลิกสร้าง
    [Range(0f, 1f)] public float powerUpChance = 0.15f;
    public float[] hintLeafAt = { 250f, 550f, 850f }; // ใบไม้วิเศษออกที่ระยะเหล่านี้ทุกด่าน
    float distanceSinceSpawn;
    float nextGap;
    float nextHeal;
    int nextLeaf;

    void Start()
    {
        nextGap = config.firstSpawnAt;
        nextHeal = config.healEvery;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.RunnerPhase) return;
        if (GameSession.Distance > GameSession.TargetDistance - stopBeforeBoss) return;
        distanceSinceSpawn += WorldScroller.FrameDelta;
        if (distanceSinceSpawn < nextGap) return;
        distanceSinceSpawn = 0f;
        float tighter = 1f + 0.1f * (GameSession.Stage - 1);                 // ด่านสูง = ระยะห่างสั้นลง
        nextGap = Random.Range(config.minGap, config.maxGap) / tighter;
        SpawnPattern();
    }

    float X(int lane) => lane * config.laneWidth;

    void SpawnPattern()
    {
        float z = config.spawnZ;
        int a = Random.Range(-1, 2), b = (a + Random.Range(1, 3) + 1) % 3 - 1, free = -a - b; // 3 เลนไม่ซ้ำกัน
        if (GameSession.Distance >= nextHeal) { Spawn(heartPrefab, X(a), 0.9f, z); nextHeal += config.healEvery; return; } // หัวใจทุก 400m
        if (nextLeaf < hintLeafAt.Length && GameSession.Distance >= hintLeafAt[nextLeaf]) // ใบไม้วิเศษวางบนหิน ต้องกระโดดเก็บ
        {
            Spawn(rockPrefab, X(a), 0f, z);
            Spawn(hintLeafPrefab, X(a), 2.1f, z);
            nextLeaf++;
            return;
        }
        int maxPattern = Mathf.Min(9, 6 + GameSession.Stage);                // ด่านสูงปลดล็อกชุดยาก
        switch (Random.Range(0, maxPattern))
        {
            case 0: // หิน 1 ก้อน + แถวเหรียญเลนอื่น
                Spawn(rockPrefab, X(a), 0f, z);
                CoinLine(b, z - 4f, 5);
                break;
            case 1: // หิน 2 เลน + เหรียญโค้งเหนือหิน
                Spawn(rockPrefab, X(a), 0f, z);
                Spawn(rockPrefab, X(b), 0f, z);
                CoinArc(a, z);
                break;
            case 2: // หินใหญ่ + หิน -> เหลือเลนว่าง 1 เลน
                Spawn(boulderPrefab, X(a), 0f, z);
                Spawn(rockPrefab, X(b), 0f, z);
                CoinLine(free, z - 3f, 4);
                break;
            case 3: // ท่อนไม้สูงทุกเลน ต้องสไลด์ + เหรียญใต้ท่อน
                Spawn(highLogPrefab, 0f, 0f, z);
                CoinLine(a, z - 2f, 3, 0.45f);
                break;
            case 4: // ต้นไม้ล้มขวางทาง + เหรียญโค้งเหนือลำต้น
                bool fromRight = Random.value < 0.5f;
                Spawn(fromRight ? treeFromRightPrefab : treeFromLeftPrefab, 0f, 0f, z);
                CoinArc(fromRight ? 1 : -1, z);
                break;
            case 5: // แถวเหรียญซิกแซกเปลี่ยนเลน
                for (int i = 0; i < 9; i++) Spawn(coinPrefab, X(i / 3 % 2 == 0 ? a : b), 0.7f, z + i * 2f);
                break;
            case 6: // กำแพงหิน 3 เลน ต้องกระโดดอย่างเดียว
                for (int l = -1; l <= 1; l++) Spawn(rockPrefab, X(l), 0f, z);
                CoinArc(free, z);
                break;
            case 7: // หินใหญ่ 2 เลน + ท่อนไม้สูงตามมา (หลบแล้วต้องสไลด์ต่อ)
                Spawn(boulderPrefab, X(a), 0f, z);
                Spawn(boulderPrefab, X(b), 0f, z);
                Spawn(highLogPrefab, 0f, 0f, z + 12f);
                break;
            default: // ต้นไม้ล้ม 2 ต้นติดกันจากคนละฝั่ง
                Spawn(treeFromRightPrefab, 0f, 0f, z);
                Spawn(treeFromLeftPrefab, 0f, 0f, z + 14f);
                break;
        }
        if (Random.value < powerUpChance && powerUpPrefabs.Length > 0) // ไอเทมพลังพิเศษวางเหนือเลนว่าง
            Spawn(powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)], X(free), 1.2f, z - 8f);
    }

    void CoinLine(int lane, float z, int count, float y = 0.7f)
    {
        for (int i = 0; i < count; i++) Spawn(coinPrefab, X(lane), y, z + i * 2f);
    }

    void CoinArc(int lane, float z) // เหรียญโค้งตามแนวกระโดด
    {
        for (int i = 0; i < 5; i++) { float t = (i - 2) / 2f; Spawn(coinPrefab, X(lane), 2.1f - t * t, z + t * 3f); }
    }

    void Spawn(Component prefab, float x, float y, float z)
    {
        if (prefab == null) { Debug.LogWarning("[TrackSpawner] ยังไม่ได้ใส่ Prefab"); return; }
        Instantiate(prefab.gameObject, new Vector3(x, y, z), prefab.transform.rotation, transform);
    }
}
