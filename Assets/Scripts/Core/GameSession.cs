using System.Collections.Generic;
using UnityEngine;

// ข้อมูลรอบเล่นที่ส่งต่อข้ามฉาก (วิ่งไม่รู้จบ: ฉากเปลี่ยนทุก 1000 m, เก็บตัวอักษรครบ = เข้า Bonus time แล้วกลับมาวิ่งต่อ)
public class GameSession : MonoBehaviour
{
    public static int MaxHp = 100, Hp, Coins, Score, AnimalsSolved, HintLeaves;
    public static float TotalDistance;         // ระยะรวมทั้งรอบ (ที่โชว์บนจอ)
    public static float RunTime;               // วินาทีที่วิ่งจริงทั้งรอบ (ไม่นับหยุด/สอนเล่น/Bonus)
    public static bool Initialized, ReviveUsed; // ReviveUsed = SlimeAngel เกิดใหม่ไปแล้ว
   public static int ChaseHits, EatenThisRun; // โดนชนระหว่างไล่ / กินบอทได้ในรอบนี้
    public static int BonusRounds;             // เข้า Bonus time มาแล้วกี่ครั้ง (คำถัดไปยาวขึ้นทีละ 1 ตัว)
    public static string Word = "";            // ชื่อสัตว์ที่ต้องเก็บตัวอักษรก่อนเข้า Bonus time
    public static bool[] Got = new bool[0];    // ตัวอักษรตัวไหนเก็บแล้ว
    public static readonly List<Vector2> Letters = new List<Vector2>(); // ตัวอักษรที่รอวาง (x = ช่องในคำ, y = ระยะรวมที่ถึงคิว)
    public static int HitsFront, HitsSide, DamageTaken, Healed, Revives; // สถิติรอบนี้ (AutoPilot วัดสมดุล)
    public static float StumbleTimer;          // สะดุดแล้ว 6 วิ: บอทเร่ง + ชนซ้ำเสีย x2
    public static readonly List<string> SeenAnimals = new List<string>(); // สัตว์ที่ออกแล้ว (ไม่ซ้ำจนกว่าจะครบ)

    public const float StageLength = 1000f;    // เปลี่ยนฉาก (กลางวัน/เย็น/กลางคืน) ทุก 1000 m
    public const float LetterEvery = 400f, LetterRetry = 200f; // ตัวอักษรเกิดทุก 400 m พลาดแล้วกลับมาใน 200 m
    public const float MaxSpeed = 32f;         // เพดานความเร็ว (m/s) กันเร็วจนหลบไม่ทัน
    public static int Stage => 1 + (int)(TotalDistance / StageLength);
    public static float Distance => TotalDistance - (Stage - 1) * StageLength;               // ระยะในฉากนี้
    public static int WordLength => Mathf.Min(8, 3 + BonusRounds);                          // คำยาวขึ้นทีละ 1 ตัว
    public static float StageStart => Mathf.Min(1.6f, 1f + 0.1f * (Stage - 1));             // ฉากหลังเร็วขึ้น
    public static float Ramp => 1f + 0.05f * Mathf.Min(12, Mathf.Floor(RunTime / 10f));    // +5% ทุก 10 วิ ครบ 2 นาที = x1.6 แล้วคงที่
    public static float SpeedMultiplier => StageStart * Ramp * CharacterData.SpeedMul[CharacterData.Current];
    public static float Pressure => Mathf.Clamp01(RunTime / 120f);                          // 0-1 เลือดลดหนักสุดที่ 2 นาที
    public static int CoinTier => Mathf.Min(5, 1 + (int)(TotalDistance / 500f) + (CharacterAbility.Tier2(CharacterData.Ability.Rich) ? 1 : 0)); // ทุก 500 m เหรียญคูณเพิ่ม
    public static bool WordDone { get { if (Got.Length == 0) return false; foreach (bool g in Got) if (!g) return false; return true; } }

    public static void Reset() // เริ่มรอบใหม่
    {
        Hp = MaxHp = CalcMaxHp();
        Coins = Score = AnimalsSolved = HintLeaves = ChaseHits = EatenThisRun = BonusRounds = 0;
        HitsFront = HitsSide = DamageTaken = Healed = Revives = 0;
        TotalDistance = RunTime = StumbleTimer = 0f;
        ReviveUsed = false;
        ClearWord();
        SeenAnimals.Clear();
        Initialized = true;
    }

    public static void ClearWord() { Word = ""; Got = new bool[0]; Letters.Clear(); } // ผ่าน Bonus แล้วเริ่มคำใหม่

    public static int CalcMaxHp() => CharacterData.MaxHp[CharacterData.Current] + 5 * SaveData.Level(SaveData.Upgrade.Hp); // เลือดตามตัวละคร + อัปเกรด

    public static void EnsureInitializedForBonus() // กด Play ที่ฉาก Bonus time ตรงๆ ตอนทดสอบ
    {
        if (!Initialized) Reset();
    }
}
