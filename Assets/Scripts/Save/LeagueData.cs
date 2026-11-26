using UnityEngine;

// ลีกสไลม์: บอท 9 ตัวต่อลีก กินบอทเพื่อแซงอันดับ ขึ้นที่ 1 = เลื่อนลีก (ปลดอัปเกรด +5 เลเวล)
public class LeagueData : MonoBehaviour
{
    public const int Bots = 9;
    public const float HeadStart = 30f;    // บอทเริ่มนำหน้ากี่เมตร
    static readonly string[] Leagues = { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Legend" };
    static readonly string[] Regions = { "ทุ่งหญ้าใบอ่อน", "ป่าเฟิร์น", "ป่าใบแดง", "หุบเขาหมอก", "ยอดเขาหิมะ", "บัลลังก์ราชาสไลม์" };
    static readonly string[] First = { "Bubbly", "Jelly", "Gooey", "Mossy", "Puddle", "Sticky", "Wobbly", "Squishy", "Gloopy" };
    static readonly string[] Last = { "Sprout", "Fern", "Ember", "Mist", "Frost", "Crown" };
    static SaveData.Data D => SaveData.D;

    public static string Name(int league) => Leagues[Mathf.Min(league, 5)] + (league > 5 ? " " + (league - 4) : ""); // เกิน Legend = Legend 2, 3 ...
    public static string Region(int league) => Regions[Mathf.Min(league, 5)];
    public static int TargetRank => D.botsLeft;                                      // บอทอันดับล่างสุดที่ยังเหลือ = เป้าหมาย
    public static int PlayerRank => D.botsLeft + 1;
    public static string BotName(int rank) => First[(rank + D.league * 4) % First.Length] + " " + Last[D.league % Last.Length];
    public static int BotModel(int rank) => rank >= 7 ? 0 : rank >= 4 ? 1 : rank >= 2 ? 2 : 3; // อันดับสูง = ตัวละครแพงกว่า
    public static int BotPoints(int rank) => (D.league + 1) * 10000 + (Bots + 1 - rank) * 1000;
    public static float BotSpeed(int rank) => 1.10f + 0.15f * (Bots - rank) / (Bots - 1f); // เร็วกว่าผู้เล่น 10-25%

    public static float Power // ระยะที่ผู้เล่นวิ่งได้โดยเฉลี่ย ปรับตามเลือดที่อัปเกรดเพิ่ม
    {
        get
        {
            if (D.recentRuns.Count == 0) return 600f;
            float sum = 0f;
            for (int i = 0; i < D.recentRuns.Count; i++) sum += D.recentRuns[i] * GameSession.CalcMaxHp() / Mathf.Max(1, D.recentHp[i]);
            return sum / D.recentRuns.Count;
        }
    }

    public static float CatchDistance // ระยะที่ต้องวิ่งให้ถึงบอท (คำนวณตอนเจอครั้งแรกแล้วล็อกไว้ อัปเกรดแล้วจึงตามทัน)
    {
        get
        {
            if (D.botCatch <= 0f)
            {
                float d = Power * (1.15f + 0.02f * (Bots + 1 - TargetRank));      // เกินระยะปกติ 15-33% = เกือบทัน
                D.botCatch = Mathf.Clamp(Mathf.Max(d, D.leagueLastD), 300f, 6000f);
                D.botFails = 0;
            }
            return D.botCatch;
        }
    }

    public static bool Eat() // กินบอทเป้าหมาย -> แซงอันดับ คืนค่า true ถ้าได้เลื่อนลีก
    {
        D.leaguePoints = BotPoints(TargetRank) + 1;
        D.leagueLastD = D.botCatch;
        D.botCatch = 0f;
        bool promote = --D.botsLeft == 0;
        if (promote)
        {
            D.league++;
            D.botsLeft = Bots;
            D.leagueLastD = 0f;
            SaveData.Bank += 1000 * D.league;                                     // รางวัลเลื่อนลีก
            MissionSystem.Add(MissionSystem.Kind.League, D.league);
        }
        SaveData.Save();
        return promote;
    }

    public static void RunEnded(float distance, int maxHp, bool escaped) // จบรอบ: จำระยะไว้ประเมินพลัง, ไล่ไม่ทัน 3 รอบ บอทเริ่มเหนื่อย
    {
        D.recentRuns.Add(distance);
        D.recentHp.Add(maxHp);
        if (D.recentRuns.Count > 3) { D.recentRuns.RemoveAt(0); D.recentHp.RemoveAt(0); }
        if (escaped && ++D.botFails >= 3) D.botCatch = Mathf.Min(D.botCatch, Mathf.Max(Power * 0.9f, D.botCatch * 0.95f)); // ลดทีละ 5% ไม่ต่ำกว่า 90% ของพลัง
    }
}
