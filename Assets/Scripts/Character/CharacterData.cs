using UnityEngine;

// ข้อมูลตัวละคร: ราคา + เงื่อนไขปลดซื้อ, ค่าสถานะเด่น/ผลลบ, ความสามารถ 2 ขั้น (ปลดด้วยเควสเฉพาะตัว), ราคาอัปเกรดต่างกัน
public class CharacterData : MonoBehaviour
{
    public enum Ability { Forest, Rich, DemonGuard, AngelRevive }
    public enum Quest { Distance, Hearts, Coins, BonusClear, CleanBonus, EatBots }   // ตัวนับเควสแยกตัวละคร
    public const int QuestKinds = 6;
    public static readonly string[] Names = { "SlimeGreen", "SlimeKing", "SlimeDemon", "SlimeAngel" };          // เลขตัวละครในเซฟ (ห้ามสลับ)
    public static readonly string[] Folders = { "Slime_Green", "Slime_King", "Slime_Demon", "Slime_Angel" }; // โฟลเดอร์ใน Character_Model
    public static readonly int[] ShopOrder = { 0, 1, 3, 2 };                     // ลำดับในร้าน: Demon อยู่ท้ายสุด
    public static readonly int[] Prices = { 0, 6000, 25000, 20000 };          // แรร์ ~2 รอบ / เอพิก ~6 / ตำนาน ~8 รอบ ของคนเล่นทั่วไป (~3,300 เหรียญ/รอบ)
    //                                        Green  King   Demon  Angel
    public static readonly int[] MaxHp       = { 100,   110,   160,   170 };     // ทดสอบแล้ว: King 90 ตายเร็วไป, Angel 200 ทนเกินไป
    public static readonly float[] DrainRate = { 1f,    1.1f,  1.3f,  0.5f };    // King/Demon เลือดลดเร็ว (-), Angel ลดช้า (+)
    public static readonly float[] SpeedMul  = { 1f,    1f,    1.15f, 1f };      // Demon วิ่งเร็ว +15% (ได้ระยะ/คะแนนไว แต่หลบยากขึ้น)
    public static readonly float[] CoinMul   = { 1f,    1.5f,  1f,    0.8f };    // King เหรียญ x1.5 (+), Angel x0.8 (-)
    public static readonly float[] AttackMul   = { 1f,    0.8f,  1.5f,  0.7f };    // พลังโจมตี: Demon แรง (+), King/Angel อ่อน (-)
    public static readonly float[] HealMul   = { 1f,    1f,    0.7f,  1f };      // Demon ฮีลจากหัวใจ/กินบอทได้น้อย (-)
    public static readonly float[] UpgradeCost = { 1f,  1.1f,  1.25f, 1.5f };    // ราคาอัปเกรด: Angel แพงกว่า Demon 20% (ปลดง่ายกว่า)
    public static readonly (MissionSystem.Kind k, int n)[] BuyNeed =             // ต้องทำก่อนถึงจะซื้อได้ (นับรวมทุกรอบ)
        { (MissionSystem.Kind.Coins, 0), (MissionSystem.Kind.Coins, 0), (MissionSystem.Kind.EatBots, 40), (MissionSystem.Kind.Hearts, 9) };
    public static readonly Ability[] Abilities = { Ability.Forest, Ability.Rich, Ability.DemonGuard, Ability.AngelRevive };
    static readonly (Quest q, int target)[,] Quests =                            // [ตัวละคร, ขั้น]
    {
        { (Quest.Distance, 1000), (Quest.Hearts, 10) },
        { (Quest.Coins, 500), (Quest.BonusClear, 3) },
        { (Quest.CleanBonus, 1), (Quest.EatBots, 5) },
        { (Quest.Distance, 2000), (Quest.BonusClear, 3) },
    };
    static readonly string[] QuestText = { "วิ่งรวม {0} m", "เก็บหัวใจ {0} ลูก", "เก็บเหรียญ {0} เหรียญ", "ผ่าน Bonus time {0} ครั้ง", "ผ่าน Bonus time โดยไม่ตอบผิด {0} ครั้ง", "กินบอท {0} ตัว" };
    static readonly string[,] AbilityText =
    {
        { "ขวัญใจป่า: หัวใจฮีลเพิ่ม 50%", "เริ่มทุกด่านมีใบไม้ 1 ใบ + ตอบถูกใน Bonus +3 วิ" },
        { "ราชทรัพย์: แม่เหล็กอัตโนมัติ 6 วิ ทุก 45 วิ", "ตัวคูณเหรียญ +1 ขั้น + ตอบถูกใน Bonus +3 วิ" },
        { "เกราะกันชน 1 ครั้ง (ชาร์จ 20 วิ) ชนครบ 5 ครั้ง = โหมดปีศาจ ชนไม่เสียเลือด 10 วิ", "โหมดปีศาจชนอุปสรรคแตก ได้เหรียญ + ตอบถูกใน Bonus +3 วิ" },
        { "ฟื้นเลือด 5% ทุก 30 วิ + เกิดใหม่เลือดเต็ม 1 ครั้งต่อเกม อมตะ 10 วิ", "เก็บเหรียญติดกัน 40 อันโดยไม่ชน = บิน 6 วิ + ตอบถูกใน Bonus +3 วิ" },
    };
    public static int Count => Names.Length;
    public static int Current => Mathf.Clamp(SaveData.Character, 0, Count - 1);
    public static Ability CurrentAbility => Abilities[Current];

    public static bool CanBuy(int c) => SaveData.D.stats[(int)BuyNeed[c].k] >= BuyNeed[c].n;
    public static string BuyNeedText(int c) => BuyNeed[c].n <= 0 ? "" : (BuyNeed[c].k == MissionSystem.Kind.EatBots ? "กินสไลม์บอท " : "เก็บหัวใจใหญ่ ")
        + Mathf.Min(SaveData.D.stats[(int)BuyNeed[c].k], BuyNeed[c].n) + "/" + BuyNeed[c].n + (BuyNeed[c].k == MissionSystem.Kind.EatBots ? " ตัว" : " ครั้ง");

    static int Stat(int c, Quest q) => SaveData.D.charStats[c * QuestKinds + (int)q];
    static bool Done(int c, int tier) => Stat(c, Quests[c, tier].q) >= Quests[c, tier].target;
    public static int RawTier(int c) => !Done(c, 0) ? 0 : Done(c, 1) ? 2 : 1;          // ทำเควสถึงขั้นไหนแล้ว
    public static int Tier(int c) => Mathf.Min(RawTier(c), SaveData.D.charClaimed[c]);  // ใช้ได้จริงเมื่อกดรับในร้านแล้ว
    public static bool CanClaim(int c) => SaveData.OwnsCharacter(c) && RawTier(c) > SaveData.D.charClaimed[c];
    public static void Claim(int c) { SaveData.D.charClaimed[c] = RawTier(c); SaveData.Save(); }

    public static void AddQuest(Quest q, int amount) // นับเควสของตัวที่ใช้อยู่ ครบขั้นใหม่ = แจ้งให้ไปกดรับที่ร้าน
    {
        int c = Current, before = RawTier(c);
        SaveData.D.charStats[c * QuestKinds + (int)q] += amount;
        int after = RawTier(c);
        if (after > before && GameManager.Instance && GameManager.Instance.ui) GameManager.Instance.ui.SetMissionBadge(); // ไม่ขึ้นข้อความกลางเกม ไปกดรับที่ร้าน
    }

    static string QuestLine(int c, int tier) // "เควส: วิ่งรวม 1,000 m (350/1000)"
    {
        var (q, target) = Quests[c, tier];
        int p = Mathf.Min(Stat(c, q), target);
        return string.Format(QuestText[(int)q], target.ToString("N0")) + " (" + p.ToString("N0") + "/" + target.ToString("N0") + ")";
    }

    public static string Info(int i) // ข้อความในหน้าดูตัวละคร
    {
        string s = "เลือด " + MaxHp[i] + Pct("  เลือดลด", DrainRate[i], true) + Pct("  ความเร็ว", SpeedMul[i], false) + Pct("\nเหรียญ", CoinMul[i], false) + Pct("  พลังโจมตี", AttackMul[i], false) + Pct("  ฮีล", HealMul[i], false);
        if (!SaveData.OwnsCharacter(i) && BuyNeed[i].n > 0) s += "\n" + (CanBuy(i) ? "<color=#9CFF9C>" : "<color=#FF9C9C>") + "ปลดซื้อ: " + BuyNeedText(i) + "</color>";
        int t = Tier(i);
        for (int k = 0; k < 2; k++)
        {
            bool on = t > k;
            s += "\n" + (on ? "<color=#FFE27A>" : "<color=#9FB3B8>") + "ขั้น " + (k + 1) + (on ? " (ปลดแล้ว) " : " (ล็อก) ") + AbilityText[i, k] + "</color>";
            if (!on && t == k) s += "\n<size=80%><color=#B8F0FF>" + (RawTier(i) > k ? "เควสครบแล้ว! กดปุ่มรับความสามารถ" : "เควส: " + QuestLine(i, k)) + "</color></size>";
        }
        return s;
    }

    static string Pct(string label, float v, bool lowGood) // "  เหรียญ +50%" สีเขียว = ข้อดี สีแดง = ข้อเสีย
    {
        if (Mathf.Approximately(v, 1f)) return "";
        int p = Mathf.RoundToInt((v - 1f) * 100f);
        bool good = lowGood ? p < 0 : p > 0;
        return label + " " + (good ? "<color=#9CFF9C>" : "<color=#FF9C9C>") + (p > 0 ? "+" : "") + p + "%</color>";
    }
}
