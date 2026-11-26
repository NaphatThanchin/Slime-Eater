using System.Collections.Generic;
using UnityEngine;

// ภารกิจ 3 ชั้น: หลัก 9 หน้า (ง่าย -> ยาก) / ประจำวัน 3 อย่าง / ต่อเนื่องไม่รู้จบ (ปลดหลังจบหน้า 9)
public class MissionSystem : MonoBehaviour
{
    public enum Kind { Coins, Jumps, Slides, Distance, Animals, PowerUps, Stage, EatBots, EatInRun, League, CleanCatch, Leaves }
    public struct Row { public string title; public int progress, target, reward; public bool done; }
    public const int KindCount = 12, Pages = 9, PerPage = 3, Count = Pages * PerPage;
    public static System.Action<string> OnCompleted; // แจ้ง UI ตอนทำสำเร็จ
    static SaveData.Data D => SaveData.D;
    static readonly string[] Titles = { "เก็บเหรียญ {0} เหรียญ", "กระโดด {0} ครั้ง", "สไลด์ {0} ครั้ง", "วิ่งรวม {0} เมตร", "ทายสัตว์ถูก {0} ตัว", "เก็บไอเทมพลังพิเศษ {0} ชิ้น", "ไปให้ถึงด่าน {0}",
                                        "กินสไลม์บอท {0} ตัว", "กินบอท {0} ตัวในรอบเดียว", "", "ไล่ทันบอทโดยไม่โดนชน {0} ครั้ง", "ใช้ใบไม้วิเศษ {0} ใบ" };
    static readonly int[] PageReward = { 100, 200, 400, 700, 1200, 2000, 2000, 2500, 3500 };
    static readonly (Kind kind, int target)[] Main =
    {
        (Kind.Coins, 50),       (Kind.Jumps, 15),      (Kind.Distance, 500),    // หน้า 1 พื้นฐาน
        (Kind.EatBots, 1),      (Kind.Slides, 15),     (Kind.Animals, 5),       // หน้า 2 เจอคู่แข่ง
        (Kind.Coins, 300),      (Kind.Stage, 2),       (Kind.EatBots, 3),       // หน้า 3 เริ่มล่า
        (Kind.Distance, 3000),  (Kind.Animals, 20),    (Kind.EatInRun, 2),      // หน้า 4 นักล่ามือใหม่
        (Kind.League, 1),       (Kind.PowerUps, 20),   (Kind.CleanCatch, 1),    // หน้า 5 เลื่อนลีก
        (Kind.EatBots, 15),     (Kind.Stage, 4),       (Kind.Animals, 50),      // หน้า 6 ผู้ล่าตัวจริง
        (Kind.League, 2),       (Kind.EatInRun, 3),    (Kind.Coins, 3000),      // หน้า 7 ป่าใบแดง
        (Kind.Distance, 25000), (Kind.EatBots, 30),    (Kind.Animals, 100),     // หน้า 8 จ้าวแห่งป่า
        (Kind.League, 3),       (Kind.Stage, 8),       (Kind.EatBots, 50),      // หน้า 9 ราชันย์
    };
    static readonly (Kind kind, int target, int reward)[] Daily =
    {
        (Kind.EatBots, 1, 300), (Kind.CleanCatch, 1, 300), (Kind.Coins, 150, 200), (Kind.Animals, 5, 200),
        (Kind.Jumps, 50, 150), (Kind.Slides, 30, 150), (Kind.Distance, 2000, 200), (Kind.Leaves, 2, 150),
    };
    static readonly (Kind kind, int target)[] Endless = { (Kind.EatBots, 10), (Kind.Distance, 10000), (Kind.Animals, 30) }; // เป้าแรก x1.5 ทุกครั้ง

    static int Stat(Kind k) => D.stats[(int)k];
    static bool IsMax(Kind k) => k == Kind.Stage || k == Kind.EatInRun || k == Kind.League; // นับค่าสูงสุด ไม่ใช่สะสม
    static string Title(Kind k, int target) => k == Kind.League ? "เลื่อนขึ้นลีก " + LeagueData.Name(target) : string.Format(Titles[(int)k], target.ToString("N0"));
    public static bool Done(int i) => D.missionDone[i];
    public static bool AllMainDone { get { for (int p = 0; p < Pages; p++) if (!PageDone(p)) return false; return true; } }
    static int MainProgress(int i) => Mathf.Min(Stat(Main[i].kind), Main[i].target);

    public static bool PageDone(int p)
    {
        for (int i = p * PerPage; i < (p + 1) * PerPage; i++) if (!Done(i)) return false;
        return true;
    }

    public static void Add(Kind kind, int amount) // เรียกตอนเกิดเหตุการณ์ในเกม
    {
        D.stats[(int)kind] = IsMax(kind) ? Mathf.Max(Stat(kind), amount) : Stat(kind) + amount;
        CheckAll();
    }

    public static void CheckAll() // ภารกิจไหนถึงเป้าแล้วให้รางวัล
    {
        EnsureDaily();
        for (int i = 0; i < Count; i++) if (!Done(i) && MainProgress(i) >= Main[i].target) { D.missionDone[i] = true; Reward(Title(Main[i].kind, Main[i].target), PageReward[i / PerPage]); }
        for (int s = 0; s < 3; s++)
        {
            var m = Daily[D.dailyPick[s]];
            if (!D.dailyDone[s] && Stat(m.kind) - D.dailyBase[s] >= m.target) { D.dailyDone[s] = true; Reward(Title(m.kind, m.target), m.reward); }
        }
        if (!D.dailyBonus && D.dailyDone[0] && D.dailyDone[1] && D.dailyDone[2]) // ครบ 3 อย่าง = โบนัส + นับวันติดต่อกัน
        {
            D.dailyBonus = true;
            D.streak = D.lastFullDate == System.DateTime.Now.AddDays(-1).ToString("yyyyMMdd") ? D.streak + 1 : 1;
            D.lastFullDate = D.dailyDate;
            Reward("ภารกิจประจำวันครบ", D.streak % 7 == 0 ? 1300 : 300);
        }
        if (!D.endlessOn && AllMainDone) { D.endlessOn = true; for (int e = 0; e < 3; e++) D.endlessBase[e] = Stat(Endless[e].kind); }
        for (int e = 0; D.endlessOn && e < 3; e++)
            if (Stat(Endless[e].kind) - D.endlessBase[e] >= EndlessTarget(e))
            {
                Reward(Title(Endless[e].kind, EndlessTarget(e)), EndlessReward(e));
                D.endlessLevel[e]++;
                D.endlessBase[e] = Stat(Endless[e].kind);
            }
    }

    static int EndlessTarget(int e) => Mathf.RoundToInt(Endless[e].target * Mathf.Pow(1.5f, D.endlessLevel[e]));
    static int EndlessReward(int e) => 1500 + 500 * D.endlessLevel[e];

    static void Reward(string title, int coins)
    {
        SaveData.Bank += coins;
        SaveData.Save();
        OnCompleted?.Invoke(title + " +" + coins.ToString("N0"));
    }

    static void EnsureDaily() // วันใหม่ -> สุ่มภารกิจประจำวัน 3 อย่างไม่ซ้ำกัน
    {
        string today = System.DateTime.Now.ToString("yyyyMMdd");
        if (D.dailyDate == today) return;
        D.dailyDate = today;
        var rng = new System.Random(int.Parse(today));
        var pool = new List<int>();
        for (int i = 0; i < Daily.Length; i++) pool.Add(i);
        for (int s = 0; s < 3; s++)
        {
            int pick = pool[rng.Next(pool.Count)];
            pool.Remove(pick);
            D.dailyPick[s] = pick;
            D.dailyBase[s] = Stat(Daily[pick].kind);
            D.dailyDone[s] = false;
        }
        D.dailyBonus = false;
    }

    public static List<Row> Rows(int tab, int page) // แถวสำหรับหน้า UI (0 = ประจำวัน, 1 = หลัก, 2 = ต่อเนื่อง)
    {
        EnsureDaily();
        var rows = new List<Row>();
        if (tab == 0)
            for (int s = 0; s < 3; s++)
            {
                var m = Daily[D.dailyPick[s]];
                rows.Add(new Row { title = Title(m.kind, m.target), progress = Mathf.Min(Stat(m.kind) - D.dailyBase[s], m.target), target = m.target, reward = m.reward, done = D.dailyDone[s] });
            }
        else if (tab == 1)
            for (int i = page * PerPage; i < (page + 1) * PerPage; i++)
            {
                if (!Done(i)) rows.Add(new Row { title = Title(Main[i].kind, Main[i].target), progress = MainProgress(i), target = Main[i].target, reward = PageReward[page] });
            }
        else if (D.endlessOn)
            for (int e = 0; e < 3; e++)
                rows.Add(new Row { title = "อีก " + Title(Endless[e].kind, EndlessTarget(e)), progress = Stat(Endless[e].kind) - D.endlessBase[e], target = EndlessTarget(e), reward = EndlessReward(e) });
        return rows;
    }

    public static string Footer(int tab) => tab == 0 ? "ทำครบ 3 อย่าง +300  |  ทำต่อเนื่อง " + D.streak + " วัน (ครบ 7 วัน +1,000)"
        : tab == 2 && !D.endlessOn ? "ทำภารกิจหลักครบ 9 หน้าเพื่อปลดล็อก" : "";

    public static void Migrate() // เปลี่ยนตารางภารกิจ: ภารกิจที่เคยทำถึงแล้วนับว่าเสร็จ (ไม่ให้รางวัลซ้ำ)
    {
        D.stats[(int)Kind.League] = D.league;
        for (int i = 0; i < Count; i++) D.missionDone[i] = MainProgress(i) >= Main[i].target;
        D.missionVersion = 2;
    }
}
