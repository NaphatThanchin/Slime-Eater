using UnityEngine;

// ภารกิจ 27 อย่าง แบ่ง 9 หน้า หน้าละ 3 (หน้า 1 ง่ายสุด) นับสะสมตลอดการเล่น ทำครบได้เหรียญแล้วซ่อนไป
public class MissionSystem : MonoBehaviour
{
    public enum Kind { Coins, Jumps, Slides, Distance, Animals, PowerUps, Stage }
    public const int KindCount = 7, Pages = 9, PerPage = 3, Count = Pages * PerPage;
    public static System.Action<string> OnCompleted; // แจ้ง UI ตอนทำภารกิจสำเร็จ
    static readonly string[] Titles = { "เก็บเหรียญ {0} เหรียญ", "กระโดด {0} ครั้ง", "สไลด์ {0} ครั้ง", "วิ่งรวม {0} เมตร", "ทายสัตว์ถูก {0} ตัว", "เก็บไอเทมพลังพิเศษ {0} ชิ้น", "ไปให้ถึงด่าน {0}" };
    static readonly int[] PageReward = { 100, 200, 400, 700, 1200, 2000, 2000, 2500, 3500 }; // รางวัลต่อภารกิจ (หน้า 6 รวมแล้วซื้อตัวละครได้เกือบครบ)
    static readonly (Kind kind, int target)[] List =
    {
        (Kind.Coins, 50),     (Kind.Jumps, 15),    (Kind.Distance, 500),     // หน้า 1
        (Kind.Slides, 15),    (Kind.Animals, 5),   (Kind.PowerUps, 3),       // หน้า 2
        (Kind.Coins, 300),    (Kind.Stage, 2),     (Kind.Distance, 2000),    // หน้า 3
        (Kind.Jumps, 150),    (Kind.Animals, 20),  (Kind.Slides, 60),        // หน้า 4
        (Kind.Coins, 1000),   (Kind.Stage, 3),     (Kind.PowerUps, 20),      // หน้า 5
        (Kind.Distance, 10000), (Kind.Animals, 50), (Kind.Jumps, 400),       // หน้า 6
        (Kind.Coins, 3000),   (Kind.Stage, 5),     (Kind.Slides, 250),       // หน้า 7
        (Kind.Distance, 25000), (Kind.Animals, 100), (Kind.PowerUps, 60),    // หน้า 8
        (Kind.Coins, 6000),   (Kind.Stage, 8),     (Kind.Jumps, 1000),       // หน้า 9
    };

    public static int Target(int i) => List[i].target;
    public static int Progress(int i) => Mathf.Min(SaveData.D.stats[(int)List[i].kind], Target(i));
    public static int Reward(int i) => PageReward[i / PerPage];
    public static bool Done(int i) => SaveData.D.missionDone[i];
    public static string Title(int i) => string.Format(Titles[(int)List[i].kind], Target(i).ToString("N0"));

    public static bool PageDone(int p) // ทำครบ 3 ภารกิจในหน้านี้แล้ว
    {
        for (int i = p * PerPage; i < (p + 1) * PerPage; i++) if (!Done(i)) return false;
        return true;
    }

    public static void Add(Kind kind, int amount) // เรียกตอนเกิดเหตุการณ์ในเกม
    {
        var s = SaveData.D.stats;
        s[(int)kind] = kind == Kind.Stage ? Mathf.Max(s[(int)kind], amount) : s[(int)kind] + amount;
        CheckAll();
    }

    public static void CheckAll() // ภารกิจไหนถึงเป้าแล้วให้รางวัลเลย (เรียกตอนเปิดเมนูด้วย)
    {
        for (int i = 0; i < Count; i++) if (!Done(i) && Progress(i) >= Target(i)) Complete(i);
    }

    static void Complete(int i) // ให้รางวัล + บันทึก
    {
        SaveData.D.missionDone[i] = true;
        SaveData.Bank += Reward(i);
        SaveData.Save();
        Debug.Log("[Mission] สำเร็จ: " + Title(i) + " +" + Reward(i));
        OnCompleted?.Invoke(Title(i));
    }
}
