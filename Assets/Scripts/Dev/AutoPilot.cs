#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// ทดสอบอัตโนมัติ (มีใน Editor เท่านั้น ไม่ติดไปกับเกมจริง): เล่นแทนคนตามระดับฝีมือ แล้วบันทึกสถิติทุกรอบลง TestResults/autopilot.csv
public class AutoPilot : MonoBehaviour
{
    struct Profile { public string name; public float jitter, miss, follow, quiz, typeDelay; }
    static readonly Profile[] Profiles =
    {
        new Profile { name = "มือใหม่", jitter = 0.12f, miss = 0.20f, follow = 0.4f, quiz = 0.4f, typeDelay = 0.45f },
        new Profile { name = "ทั่วไป", jitter = 0.07f, miss = 0.08f, follow = 0.7f, quiz = 0.7f, typeDelay = 0.3f },
        new Profile { name = "เก่ง", jitter = 0.03f, miss = 0.02f, follow = 0.95f, quiz = 0.95f, typeDelay = 0.18f },
    };
    public static AutoPilot Instance;
    public static string Summary = "";
    int runsPer = 2, profile, run, maxStage = 7, character = -1;   // character >= 0 = โหมดทดสอบตัวละคร (ฝีมือทั่วไป ทุกตัว)
    float speed = 4f, steerCd, typeCd, endWait = -1f, runStart;
    bool right;
    string lastAnswer = "";
    readonly Dictionary<Transform, float> plan = new Dictionary<Transform, float>();   // อุปสรรค -> จะกดตอนเหลือเวลาถึงตัวกี่วิ (-1 = พลาด)
    readonly StringBuilder csv = new StringBuilder("profile,character,run,stage,distance_m,game_s,hits_front,hits_side,damage,healed,bots_eaten,bonus,animals,coins,score\n");
    Profile P => Profiles[character >= 0 ? 1 : profile];

    public static void BeginCharacters(int runs, float timeScale, int stageCap = 7) // ทดสอบตัวละครทั้ง 4 ตัว (ปลดความสามารถครบในหน่วยความจำ ไม่บันทึก)
    {
        Begin(runs, timeScale, 1, stageCap);
        Instance.character = 0;
        Instance.StartRun();
    }

    public static void Begin(int runs, float timeScale, int fromProfile = 0, int stageCap = 7) // เรียกจาก Unity MCP ตอน Play mode
    {
        if (Instance) Destroy(Instance.gameObject);
        var go = new GameObject("AutoPilot");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<AutoPilot>();
        Instance.runsPer = runs; Instance.speed = timeScale; Instance.profile = fromProfile; Instance.maxStage = stageCap;
        Summary = "กำลังทดสอบ...";
        Instance.StartRun();
    }

    void StartRun()
    {
        plan.Clear(); lastAnswer = ""; endWait = -1f; runStart = Time.time;
        Time.timeScale = 1f;
        if (character >= 0) { SaveData.D.character = character; SaveData.D.owned[character] = true; SaveData.D.charClaimed[character] = 2; for (int k = 0; k < CharacterData.QuestKinds; k++) SaveData.D.charStats[character * CharacterData.QuestKinds + k] = 99999; }
        GameSession.Reset();
        SceneManager.LoadScene("RunnerScene");
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (!gm) return;
        if (gm.ui && gm.ui.continuePanel && gm.ui.continuePanel.activeSelf) { gm.DeclineContinue(); return; }
        if (gm.State == GameState.GameOver) { EndRun(); return; }
        if (!gm.IsPaused && Time.timeScale > 0f && Time.timeScale != speed && !TutorialGuide.Active) Time.timeScale = speed; // เร่งเวลา
        if (GameSession.Stage > maxStage && gm.State == GameState.RunnerPhase) { gm.GameOver(); return; } // จำกัดเวลาทดสอบ
        if (gm.IsPaused) return;
        if (gm.State == GameState.RunnerPhase) Run(gm);
        else if (gm.State == GameState.BonusPhase && gm.quiz) Quiz(gm.quiz);
    }

    void Run(GameManager gm) // อ่านอุปสรรคข้างหน้า กดตามจังหวะ (+คลาดเคลื่อน/พลาดตามฝีมือ) แล้วไล่เก็บของ
    {
        var lp = FindFirstObjectByType<LanePlayer>();
        if (!lp || !WorldScroller.Instance) return;
        float v = Mathf.Max(1f, WorldScroller.Instance.CurrentSpeed), w = lp.config.laneWidth;
        bool busy = false;
        foreach (var o in FindObjectsByType<Obstacle>(FindObjectsSortMode.None))
        {
            var t = o.transform;
            float tti = t.position.z / v;                                        // อีกกี่วิถึงตัว
            if (tti < 0f || tti > 2.5f || o.kind == Obstacle.Kind.Platform) continue;
            bool inLane = Mathf.Abs(t.position.x - lp.Lane * w) < 1.2f;
            bool wide = o.kind == Obstacle.Kind.High || o.kind == Obstacle.Kind.Falling;
            if (!wide && !inLane) continue;
            if (tti < 1.2f) busy = true;
            if (!plan.TryGetValue(t, out float at))
            {
                float target = o.kind == Obstacle.Kind.High ? 0.35f : o.kind == Obstacle.Kind.Block ? 0.5f : 0.3f;
                at = Random.value < P.miss ? -1f : target + Random.Range(-P.jitter, P.jitter);
                plan[t] = at;
            }
            if (at < 0f || tti > at) continue;
            plan[t] = -2f;                                                         // กดแล้ว
            if (o.kind == Obstacle.Kind.High) lp.Slide();
            else if (o.kind == Obstacle.Kind.Block) lp.ChangeLane(FreeDir(lp, w));
            else lp.Jump();
        }
        if (plan.Count > 200) plan.Clear();
        if ((steerCd -= Time.deltaTime) > 0f || busy) return;
        steerCd = 0.4f;
        if (Random.value > P.follow) return;
        int best = lp.Lane; float bestScore = 0f;                                  // เลนที่มีของคุ้มสุดข้างหน้า
        for (int l = -1; l <= 1; l++)
        {
            if (Mathf.Abs(l - lp.Lane) > 1 || Danger(l, w, v)) continue;
            float s = 0f;
            foreach (var c in FindObjectsByType<Collectible>(FindObjectsSortMode.None))
            {
                var p = c.transform.position;
                if (c.botOnly || p.z < 3f || p.z > 28f || Mathf.Abs(p.x - l * w) > 1f) continue;
                s += c.kind == Collectible.Kind.Letter ? 30f : c.kind == Collectible.Kind.Heal ? (GameSession.Hp < GameSession.MaxHp * 0.7f ? 15f : 2f) : c.kind == Collectible.Kind.Coin ? 1f : 5f;
            }
            if (l == lp.Lane) s += 0.5f;
            if (s > bestScore) { bestScore = s; best = l; }
        }
        if (best != lp.Lane) lp.ChangeLane(best - lp.Lane);
    }

    bool Danger(int l, float w, float v) // เลนนี้มีหินใหญ่/ขอนกลิ้งข้างหน้าใกล้ๆ
    {
        foreach (var o in FindObjectsByType<Obstacle>(FindObjectsSortMode.None))
        {
            var p = o.transform.position;
            if (o.kind == Obstacle.Kind.High || o.kind == Obstacle.Kind.Falling || Mathf.Abs(p.x - l * w) > 1.2f) continue;
            float reach = o.kind == Obstacle.Kind.Block ? v * 1.5f : o.kind == Obstacle.Kind.Platform ? v * 0.8f + 16f : v * 0.8f; // คนดูทางก่อนเปลี่ยนเลน
            if (p.z > -1f && p.z < reach) return true;
        }
        return false;
    }

    int FreeDir(LanePlayer lp, float w, float v = 15f) // หลบไปเลนข้างที่ว่าง
    {
        foreach (int d in Random.value < 0.5f ? new[] { -1, 1 } : new[] { 1, -1 })
        {
            int l = lp.Lane + d;
            if (l >= -1 && l <= 1 && !Danger(l, w, v)) return d;
        }
        return lp.Lane == 1 ? -1 : 1;
    }

    void Quiz(BonusQuizManager b) // พิมพ์คำตอบทีละตัว (ถูก/ผิดตามฝีมือ)
    {
        if (b.Busy) return;
        string ans = b.CurrentAnswer;
        int slot = b.NextSlot;
        if (ans != lastAnswer) { right = Random.value < P.quiz; lastAnswer = ans; }
        else if (slot >= 0 && slot < lastSlot) right = Random.value < P.quiz + 0.25f;   // ผิดแล้วช่องถูกล้าง = ลองใหม่ (มีตัวใบ้ช่วย)
        lastSlot = slot;
        if ((typeCd -= Time.deltaTime) > 0f || slot < 0) return;
        typeCd = P.typeDelay;
        b.TypeChar(right ? ans[slot] : (char)('A' + Random.Range(0, 26)));
    }
    int lastSlot;

    void EndRun() // บันทึกสถิติรอบนี้ แล้วเริ่มรอบ/ระดับถัดไป
    {
        if (endWait < 0f)
        {
            endWait = Time.realtimeSinceStartup + 1f;
            csv.AppendLine(string.Join(",", P.name, character >= 0 ? CharacterData.Names[character] : CharacterData.Names[CharacterData.Current], run + 1, GameSession.Stage, Mathf.RoundToInt(GameSession.TotalDistance), Mathf.RoundToInt(Time.time - runStart),
                GameSession.HitsFront, GameSession.HitsSide, GameSession.DamageTaken, GameSession.Healed, GameSession.EatenThisRun, GameSession.BonusRounds, GameSession.AnimalsSolved, GameSession.Coins, GameSession.Score));
            Debug.Log("[AutoPilot] " + P.name + " รอบ " + (run + 1) + " ด่าน " + GameSession.Stage + " ระยะ " + Mathf.RoundToInt(GameSession.TotalDistance) + " เหรียญ " + GameSession.Coins);
            return;
        }
        if (Time.realtimeSinceStartup < endWait) return;
        if (++run >= runsPer) { run = 0; if (character >= 0) character++; else profile++; }
        if (character >= CharacterData.Count || profile >= Profiles.Length) { Finish(); return; }
        StartRun();
    }

    void Finish()
    {
        string dir = Path.Combine(Application.dataPath, "../TestResults");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "autopilot.csv"), csv.ToString(), new UTF8Encoding(true));
        Summary = csv.ToString();
        Time.timeScale = 1f;
        Debug.Log("[AutoPilot] เสร็จ\n" + Summary);
        Destroy(gameObject);
    }
}
#endif
