using System.Collections.Generic;
using TMPro;
using UnityEngine;

// วางด่านจากชุดอุปสรรคที่ออกแบบไว้ (Chunks) ยากขึ้นตามเวลา: ระยะคิดเป็นวินาที (หลบทันทุกความเร็ว)
// + เส้นทางเหรียญระหว่างชุด + Coin Rush + ทางเสี่ยง + ตัวอักษรก่อน Bonus time + หัวใจ (เล็ก = ของบอท, ใหญ่ = ท้ายด่าน)
public class TrackSpawner : MonoBehaviour
{
    public RunnerConfig config;
    public Obstacle rockPrefab;            // หิน: กระโดด
    public Obstacle highLogPrefab;         // ท่อนไม้สูง: สไลด์
    public Obstacle boulderPrefab;         // หินใหญ่: หลบเลน
    public Obstacle treeFromRightPrefab, treeFromLeftPrefab; // ต้นไม้ล้ม
    public Obstacle platformPrefab;        // ทางลาด + ขอนยาววิ่งบนหลังได้
    public Obstacle fallingLogPrefab;      // ท่อนไม้ยาวหล่นจากฟ้าลงเลน (มีเงาเตือน) ต้องหลบเลน
    public Collectible coinPrefab, heartPrefab, hintLeafPrefab, letterPrefab;
    public Collectible[] powerUpPrefabs;   // แม่เหล็ก / เกราะ / เหรียญ x2 / สปริงเห็ด / คะแนน x2
    [Range(0f, 1f)] public float powerUpChance = 0.15f;
    public float[] hintLeafAt = { 0.25f, 0.55f, 0.85f }; // ใบไม้วิเศษ (สัดส่วนของฉาก 1000 m)
    public float minGapTime = 1.15f;       // ห่างระหว่างชุดอย่างน้อย (วินาที)
    public float trailStep = 4f;           // เส้นทางเหรียญห่างกันกี่เมตร
    public float botHeartChance = 0.1f;    // หัวใจเล็ก (บอทเก็บได้) ต่อชุด
    public int botHeartMax = 3;
    public float riskEvery = 300f;         // ทางแยกเสี่ยง/ปลอดภัย ทุกกี่เมตร
    static readonly Color[] TierColors = { Color.white, new Color(0.75f, 0.9f, 1f), new Color(0.6f, 1f, 0.5f), new Color(1f, 0.6f, 0.85f), new Color(0.75f, 0.6f, 1f) }; // สีเหรียญ x1-x5
    System.Random rng;
    float sinceSpawn, nextGap, patternEnd, sinceTrail, trailX, rushLeft, rushAt, lastRisk, bigHeartAt, sinceLetter;
    int nextLeaf, tutorialStep, botHearts, segment;   // segment = ฉาก 1000 m ที่วางของพิเศษไว้แล้ว
    string pending;                         // ชุดถัดไป (รู้ล่วงหน้า เส้นทางเหรียญพาไปเลนปลอดภัย)
    bool rushDone;
    Transform poolRoot;
    readonly Stack<Collectible> pool = new Stack<Collectible>();           // เหรียญใช้ซ้ำ (มือถือไม่กระตุก)
    static List<Vector2> Letters => GameSession.Letters;                   // ตัวอักษรที่รอวาง (เก็บใน GameSession ไม่หายตอนเข้า/ออก Bonus)

    float Speed => WorldScroller.Instance ? WorldScroller.Instance.BaseSpeed : 11f;
    const float L = GameSession.StageLength;
    float Seg0 => (GameSession.Stage - 1) * L;                                 // ระยะรวมที่ฉากนี้เริ่ม
    int MaxTier => Mathf.Min(4, (GameSession.RunTime < 30f ? 2 : GameSession.RunTime < 90f ? 3 : 4) + (GameSession.Stage - 1) / 2); // ยากขึ้นตามเวลา + ฉาก

    void Start()
    {
        int seed = System.Environment.TickCount;     // เมล็ดสุ่มใหม่ทุกด่าน -> อุปสรรคไม่ซ้ำเดิม
        rng = new System.Random(seed);
        poolRoot = new GameObject("CoinPool").transform;
        poolRoot.SetParent(transform, false);
        nextGap = config.firstSpawnAt;
        pending = PickChunk();
        lastRisk = GameSession.TotalDistance;
        PlanSegment();
        PlanWord();
        var gm = GameManager.Instance;
        if (gm && gm.ui) gm.ui.SetWord();                // คำถูกเลือกหลัง UI เริ่ม -> วาดมุมขวาบนใหม่
        Debug.Log("[TrackSpawner] ด่าน " + GameSession.Stage + " ยาว " + L + " m seed " + seed + " คำ " + GameSession.Word);
    }

    string PickChunk() // สุ่มระดับ (เอียงไปทางยากเมื่อเวลาผ่านไป) แล้วสุ่มชุด
    {
        int max = MaxTier, t = Mathf.Clamp(max - rng.Next(3), 1, max) - 1;
        var set = Chunks.Tier[t];
        return set[rng.Next(set.Length)];
    }

    void PlanSegment() // ของพิเศษของฉาก 1000 m นี้: Coin Rush 1 ช่วง, หัวใจใหญ่ก่อนเปลี่ยนฉาก, ใบไม้ 3 จุด
    {
        segment = GameSession.Stage;
        rushDone = false; nextLeaf = 0; botHearts = 0;
        while (nextLeaf < hintLeafAt.Length && Seg0 + hintLeafAt[nextLeaf] * L < GameSession.TotalDistance) nextLeaf++;
        rushAt = Seg0 + L * (0.2f + 0.4f * (float)rng.NextDouble());
        bigHeartAt = Seg0 + L * (0.86f + 0.08f * (float)rng.NextDouble());     // หัวใจใหญ่ +40% ก่อนเปลี่ยนฉากทุกครั้ง
        if (bigHeartAt < GameSession.TotalDistance) bigHeartAt = -1f;
    }

    void PlanWord() // คำใหม่ (ยาว 3 ตัว แล้วเพิ่มทีละ 1 หลังผ่าน Bonus) ตัวอักษรสุ่มลำดับ เกิดทุก 400 m
    {
        if (GameSession.Word != "") return;                                   // คำเดิมยังเก็บไม่ครบ = ใช้คิวเดิม
        int len = GameSession.WordLength;
        var words = new List<string>();
        foreach (var a in Resources.LoadAll<AnimalDataSO>("Animals")) if (a.LetterCount == len) words.Add(a.Answer);
        if (words.Count == 0) foreach (var a in Resources.LoadAll<AnimalDataSO>("Animals")) if (a.LetterCount >= 3 && a.LetterCount <= len) words.Add(a.Answer);
        GameSession.Word = words.Count > 0 ? words[rng.Next(words.Count)] : "CAT";
        GameSession.Got = new bool[GameSession.Word.Length];
        Letters.Clear();
        var idx = new List<int>();
        for (int i = 0; i < GameSession.Got.Length; i++) idx.Add(i);
        for (int i = idx.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (idx[i], idx[j]) = (idx[j], idx[i]); }
        for (int k = 0; k < idx.Count; k++) Letters.Add(new Vector2(idx[k], GameSession.TotalDistance + GameSession.LetterEvery * (k + 1)));
    }

    public void LetterMissed(int i) { Letters.Add(new Vector2(i, GameSession.TotalDistance + GameSession.LetterRetry)); Letters.Sort((p, q) => p.y.CompareTo(q.y)); } // พลาด = กลับมาใน 200 m

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.RunnerPhase) return;
        if (GameSession.Stage != segment) PlanSegment();                        // เข้าฉากใหม่ (ทุก 1000 m)
        float d = WorldScroller.FrameDelta;
        sinceSpawn += d; sinceTrail += d; sinceLetter += d;
        if (TutorialGuide.Active) { if (TutorialGuide.ReadyForNext && sinceSpawn > 8f) { sinceSpawn = 0f; SpawnTutorial(); } return; } // สอนเล่น: วางทีละบท
        if (rushLeft > 0f) { CoinRush(d); return; }
        if (!rushDone && GameSession.TotalDistance >= rushAt && sinceSpawn > patternEnd) // Coin Rush ฉากละ 1 ครั้ง
        {
            rushDone = true;
            rushLeft = 100f + 100f * (float)rng.NextDouble();
            return;
        }
        if (sinceSpawn >= nextGap)
        {
            sinceSpawn = 0f;
            float length = SpawnNext();
            nextGap = Mathf.Max((length + minGapTime + (float)rng.NextDouble() * 0.6f) * Speed, patternEnd + 6f); // แปลงวินาทีเป็นเมตรตามความเร็ว
            return;
        }
        if (sinceTrail >= trailStep && sinceSpawn > patternEnd && sinceSpawn < nextGap - 5f) { sinceTrail = 0f; Trail(); } // ช่องว่างระหว่างชุด = เส้นทางเหรียญ
    }

    void Trail() // เหรียญเส้นยาวพาไปเลนปลอดภัยของชุดถัดไป + บางทีมีเหรียญกลุ่มเล็กในเลนอื่น
    {
        float z = config.spawnZ;
        trailX = Mathf.MoveTowards(trailX, X(Chunks.SafeLane(pending)), config.laneWidth * 0.5f);
        if (!TryLetter(trailX, z)) Coin(trailX, 0.7f, z);
        if (sinceSpawn < nextGap - 16f && rng.NextDouble() < 0.12)
        {
            int l = Mathf.RoundToInt(trailX / config.laneWidth) == 1 ? -1 : 1;
            for (int i = 1; i <= 3; i++) Coin(X(l), 0.7f, z + i * trailStep);
        }
    }

    void CoinRush(float d) // 100-200 m เหรียญเต็ม 3 เลน ไม่มีอุปสรรค
    {
        rushLeft -= d;
        if (sinceTrail >= trailStep)
        {
            sinceTrail = 0f;
            for (int l = -1; l <= 1; l++) if (l != 0 || !TryLetter(0f, config.spawnZ)) Coin(X(l), 0.7f, config.spawnZ);
        }
        if (rushLeft <= 0f) { sinceSpawn = 0f; nextGap = 14f; patternEnd = 0f; trailX = 0f; }
    }

    bool TryLetter(float x, float z) // ถึงคิวตัวอักษร -> วางแทนเหรียญ
    {
        if (Letters.Count == 0 || !letterPrefab) return false;
        if (Letters[0].y > GameSession.TotalDistance || sinceLetter < 40f) return false;
        int i = (int)Letters[0].x;
        Letters.RemoveAt(0);
        sinceLetter = 0f;
        if (i >= GameSession.Got.Length || GameSession.Got[i]) return false;
        var c = Spawn(letterPrefab, x, 1f, z).GetComponent<Collectible>();
        c.value = i; c.owner = this; c.spinSpeed = 0f;
        var t = c.GetComponentInChildren<TMP_Text>();
        if (t) t.text = GameSession.Word[i].ToString();
        return true;
    }

    float SpawnNext() // วางของชิ้นถัดไป คืนความยาว (วินาที)
    {
        patternEnd = 9f;
        float z = config.spawnZ;
        string chunk = pending;
        pending = PickChunk();
        int safe = Chunks.SafeLane(chunk), free = safe;
        trailX = X(safe);
        if (nextLeaf < hintLeafAt.Length && GameSession.TotalDistance >= Seg0 + hintLeafAt[nextLeaf] * L) // ใบไม้วิเศษบนหิน ต้องกระโดดเก็บ
        {
            Spawn(rockPrefab, X(safe), 0f, z);
            Spawn(hintLeafPrefab, X(safe), 2.1f, z);
            nextLeaf++;
            return 0f;
        }
        if (bigHeartAt > 0f && GameSession.TotalDistance >= bigHeartAt) { bigHeartAt = -1f; Heart(safe, 40, z, false); return 0f; } // หัวใจใหญ่ก่อนเปลี่ยนฉาก
        if (GameSession.TotalDistance - lastRisk >= riskEvery) { lastRisk = GameSession.TotalDistance; int a = rng.Next(-1, 2), e = (a + 2) % 3 - 1; return Risk(a, e, -a - e, z); } // 3 เลนไม่ซ้ำกัน
        float length = PlaceChunk(chunk, z, ref free);
        if (botHearts < botHeartMax && rng.NextDouble() < botHeartChance) { botHearts++; Heart(free, 10, z - 8f, true); } // หัวใจเล็กของบอท
        else if (rng.NextDouble() < powerUpChance && powerUpPrefabs.Length > 0) Spawn(powerUpPrefabs[rng.Next(powerUpPrefabs.Length)], X(free), 1.2f, z - 8f); // ไอเทมพลังพิเศษก่อนชุด
        return length;
    }

    float PlaceChunk(string chunk, float z0, ref int free) // แปลงตัวอักษรเป็นของจริง แถวละ RowTime วินาที
    {
        var rows = chunk.Split('|');
        float step = Mathf.Max(9f, Speed * Chunks.RowTime);
        for (int r = 0; r < rows.Length; r++)
        {
            string row = rows[r];
            float z = z0 + r * step;
            bool log = false;
            for (int i = 0; i < 3; i++)
            {
                int lane = i - 1;
                char c = row[i];
                switch (c)
                {
                    case 'a': Spawn(rockPrefab, X(lane), 0f, z); CoinArc(lane, z); break;
                    case 'R': Spawn(rockPrefab, X(lane), 0f, z); break;
                    case 'B': Spawn(boulderPrefab, X(lane), 0f, z); break;
                    case 'M': Spawn(fallingLogPrefab, X(lane), 0f, z); break;                   // ท่อนไม้ยาวหล่นลงเลนนี้
                    case 'c': CoinLine(lane, z - 2f, 3); break;
                    case 'H': case 'h': if (!log) { Spawn(highLogPrefab, 0f, 0f, z); log = true; } if (c == 'h') CoinLine(lane, z - 2f, 3, 0.45f); break;
                    case 'T': case 't': Spawn(c == 'T' ? treeFromRightPrefab : treeFromLeftPrefab, 0f, 0f, z); CoinArc(lane, z); break;
                    case 'P':
                        if (r > 0 && rows[r - 1][i] == 'P') break;                                 // ขอนเดียวกันต่อจากแถวก่อน
                        var p = Spawn(platformPrefab, X(lane), 0f, z - 5f).GetComponent<Platform>();  // ทางลาดเริ่มก่อนแถว 5 m
                        for (float k = p.rampLength + 1f; k < p.length - 1f; k += trailStep) Coin(X(lane), p.height + 0.7f, z - 5f + k); // เหรียญบนขอน
                        patternEnd = Mathf.Max(patternEnd, r * step + p.length + 4f);
                        break;
                }
                if (c == '.' || c == 'c') free = lane;
            }
        }
        patternEnd = Mathf.Max(patternEnd, (rows.Length - 1) * step + 9f);
        return (rows.Length - 1) * Chunks.RowTime;
    }

    float Risk(int risky, int easy, int blocked, float z) // ทางแยก: เลนเสี่ยง (หินติดกัน 3 ก้อน เหรียญเยอะ + รางวัล) / เลนง่ายเหรียญน้อย / เลนตัน
    {
        float gap = Mathf.Max(9f, Speed * 0.85f);                    // ห่าง 0.85 วิ (ลอยตัว 0.64 วิ) ต้องกดต่อเนื่อง
        for (int i = 0; i < 3; i++) { Spawn(rockPrefab, X(risky), 0f, z + i * gap); CoinArc(risky, z + i * gap); }
        Spawn(boulderPrefab, X(blocked), 0f, z); Spawn(boulderPrefab, X(blocked), 0f, z + gap * 2f);
        CoinLine(easy, z, 3);
        float end = z + gap * 2f + 6f;
        if (!TryLetter(X(risky), end)) Spawn(powerUpPrefabs[rng.Next(powerUpPrefabs.Length)], X(risky), 1.2f, end);
        trailX = X(risky);
        patternEnd = gap * 2f + 12f;
        return 1.7f;
    }

    void SpawnTutorial() // บทเรียน: 0 กำแพงหิน (กระโดด) 1 ท่อนไม้ (สไลด์) 2 หินใหญ่เลนกลาง+ขวา (หลบซ้าย) 3 หัวใจเลนซ้าย
    {
        const float z = 40f;
        GameObject watch;
        if (tutorialStep == 0) { Spawn(rockPrefab, X(-1), 0f, z); Spawn(rockPrefab, X(1), 0f, z); watch = Spawn(rockPrefab, X(0), 0f, z); }
        else if (tutorialStep == 1) watch = Spawn(highLogPrefab, 0f, 0f, z);
        else if (tutorialStep == 2) { Spawn(boulderPrefab, X(1), 0f, z); watch = Spawn(boulderPrefab, X(0), 0f, z); }
        else watch = Heart(-1, 40, z, false).gameObject;
        TutorialGuide.Instance.Watch(watch.transform, tutorialStep++);
    }

    float X(int lane) => lane * config.laneWidth;

    void CoinLine(int lane, float z, int count, float y = 0.7f) { for (int i = 0; i < count; i++) Coin(X(lane), y, z + i * 2f); }

    void CoinArc(int lane, float z) // เหรียญโค้งตามแนวกระโดด
    {
        for (int i = 0; i < 5; i++) { float t = (i - 2) / 2f; Coin(X(lane), 2.1f - t * t, z + t * 3f); }
    }

    Collectible Heart(int lane, int percent, float z, bool botOnly) // หัวใจใหญ่ +40% (ผู้เล่น) / หัวใจเล็ก +10% (บอทเท่านั้น สีม่วงจาง)
    {
        var h = Spawn(heartPrefab, X(lane), 0.9f, z).GetComponent<Collectible>();
        h.value = percent; h.botOnly = botOnly;
        if (botOnly) { h.transform.localScale *= 0.7f; foreach (var sr in h.GetComponentsInChildren<SpriteRenderer>()) sr.color = new Color(0.75f, 0.5f, 1f, 0.6f); }
        else h.transform.localScale *= 1.6f;
        return h;
    }

    Collectible Coin(float x, float y, float z) // เหรียญจาก pool + สีตามตัวคูณ
    {
        var c = pool.Count > 0 ? pool.Pop() : Instantiate(coinPrefab);
        c.owner = this;
        c.transform.SetParent(transform, false);
        c.transform.SetPositionAndRotation(new Vector3(x, y, z), coinPrefab.transform.rotation);
        c.gameObject.SetActive(true);
        var sr = c.GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.color = TierColors[Mathf.Clamp(GameSession.CoinTier - 1, 0, TierColors.Length - 1)];
        return c;
    }

    public void Release(Collectible c) { c.gameObject.SetActive(false); c.transform.SetParent(poolRoot, false); pool.Push(c); }

    GameObject Spawn(Component prefab, float x, float y, float z) => Instantiate(prefab.gameObject, new Vector3(x, y, z), prefab.transform.rotation, transform);
}
