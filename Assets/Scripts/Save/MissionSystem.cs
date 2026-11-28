using System.Collections.Generic;
using UnityEngine;

// ภารกิจ 3 ชั้น: หลัก 9 หน้า (ง่าย ไป ยาก) / ประจำวัน 3 อย่าง / ต่อเนื่องไม่รู้จบ (ปลดหลังจบหน้า 9)
public class MissionSystem : MonoBehaviour
{
    public enum Kind { Coins, Jumps, Slides, Distance, Animals, PowerUps, Stage, EatBots, EatInRun, League, CleanCatch, Leaves, Hearts, Bonus } // League เลิกใช้ (เก็บช่องไว้ให้เซฟเก่า)
    public struct Row { public string title; public int progress, target, reward; public bool done; }
    public const int KindCount = 14, Pages = 9, PerPage = 3, Count = Pages * PerPage;
    public static System.Action<string> OnCompleted; // แจ้ง UI ตอนทำสำเร็จ
    static SaveData.Data D => SaveData.D;
    static readonly string[] Titles = { "เก็บเหรียญ {0} เหรียญ", "กระโดด {0} ครั้ง", "สไลด์ {0} ครั้ง", "วิ่งรวม {0} เมตร", "ทายสัตว์ถูก {0} ตัว", "เก็บไอเทมพลังพิเศษ {0} ชิ้น", "ไปให้ถึงด่าน {0}",
                                        "กินสไลม์บอท {0} ตัว", "กินบอท {0} ตัวในรอบเดียว", "", "ไล่ทันบอทโดยไม่โดนชน {0} ครั้ง", "ใช้ใบไม้วิเศษ {0} ใบ", "เก็บหัวใจใหญ่ {0} ลูก", "ผ่าน Bonus time {0} ครั้ง" };
    static readonly int[] PageReward = { 100, 200, 400, 700, 1200, 2000, 2000, 2500, 3500 };
    static readonly (Kind kind, int target)[] Main =
    {
        (Kind.Coins, 100),      (Kind.Jumps, 20),      (Kind.Distance, 1000),   // หน้า 1 พื้นฐาน
        (Kind.EatBots, 1),      (Kind.Slides, 20),     (Kind.Bonus, 1),         // หน้า 2 เจอคู่แข่ง + Bonus แรก
        (Kind.Coins, 1000),     (Kind.Stage, 2),       (Kind.Animals, 10),      // หน้า 3 ไปไกลขึ้น
        (Kind.Distance, 10000), (Kind.EatBots, 5),     (Kind.Hearts, 9),        // หน้า 4 (หัวใจ 9 = ปลดซื้อ Angel)
        (Kind.PowerUps, 20),    (Kind.CleanCatch, 1),  (Kind.Stage, 4),         // หน้า 5 นักล่ามือใหม่
        (Kind.EatInRun, 3),     (Kind.Bonus, 10),      (Kind.Animals, 50),      // หน้า 6
        (Kind.EatBots, 20),     (Kind.Coins, 10000),   (Kind.Leaves, 10),       // หน้า 7
        (Kind.Distance, 50000), (Kind.EatBots, 40),    (Kind.Animals, 100),     // หน้า 8 (กิน 40 = ปลดซื้อ Demon)
        (Kind.Stage, 8),        (Kind.EatInRun, 5),    (Kind.EatBots, 66),      // หน้า 9 ราชันย์
    };
    static readonly (Kind kind, int target, int reward)[] Daily =
    {
        (Kind.EatBots, 1, 300), (Kind.CleanCatch, 1, 300), (Kind.Coins, 150, 200), (Kind.Animals, 5, 200),
        (Kind.Jumps, 50, 150), (Kind.Slides, 30, 150), (Kind.Distance, 2000, 200), (Kind.Leaves, 2, 150),
    };
    static readonly (Kind kind, int target)[] Endless = { (Kind.EatBots, 10), (Kind.Distance, 10000), (Kind.Animals, 30) }; // เป้าแรก x1.5 ทุกครั้ง

    static int Stat(Kind k) => D.stats[(int)k];
    static bool IsMax(Kind k) => k == Kind.Stage || k == Kind.EatInRun; // นับค่าสูงสุด ไม่ใช่สะสม
    static string Title(Kind k, int target) => string.Format(Titles[(int)k], target.ToString("N0"));
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

    static void Reward(string title, int coins) // ครบเป้า = เข้าคิวรอกดรับที่เมนู (ยังไม่ให้เหรียญ)
    {
        D.pendingTitles.Add(title); D.pendingCoins.Add(coins);
        SaveData.Save();
        OnCompleted?.Invoke(title);
    }

    public static int PendingCount => D.pendingCoins.Count;
    public static int PendingTotal { get { int t = 0; foreach (int c in D.pendingCoins) t += c; return t; } }

    public static int ClaimAll() // กดรับรางวัลทั้งหมดที่รอ
    {
        int t = PendingTotal;
        SaveData.Bank += t;
        D.pendingTitles.Clear(); D.pendingCoins.Clear();
        SaveData.Save();
        return t;
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
        for (int i = 0; i < Count; i++) D.missionDone[i] = MainProgress(i) >= Main[i].target;
        D.missionVersion = 3;
    }
}
