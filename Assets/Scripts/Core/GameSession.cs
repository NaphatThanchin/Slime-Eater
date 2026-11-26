using System.Collections.Generic;
using UnityEngine;

// ข้อมูลรอบเล่นที่ส่งต่อข้ามฉาก (วิ่ง -> บอส -> วิ่งด่านต่อไป ...)
public class GameSession : MonoBehaviour
{
    public const float TargetDistance = 1000f; // ระยะต่อด่านก่อนเจอบอส
    public static int MaxHp = 100, Hp, Coins, Score, AnimalsSolved, HintLeaves;
    public static int Stage;                   // ด่านที่ 1, 2, 3 ...
    public static float Distance;              // ระยะในด่านนี้
    public static float TotalDistance;         // ระยะรวมทั้งรอบ
    public static bool Initialized, ReviveUsed; // ReviveUsed = SlimeAngel เกิดใหม่ไปแล้ว
    public static readonly List<string> SeenAnimals = new List<string>(); // สัตว์ที่ออกแล้ว (ไม่ซ้ำจนกว่าจะครบ)
    public static float SpeedMultiplier => Stage <= 1 ? 1f : Mathf.Min(2.3f, 1.3f + 0.2f * (Stage - 2)); // ด่าน 2 เร็วขึ้น 30% แล้ว +20% ต่อด่าน

    public static void Reset() // เริ่มรอบใหม่
    {
        MaxHp = CharacterData.MaxHp[CharacterData.Current] + 5 * SaveData.Level(SaveData.Upgrade.Hp); // เลือดตามตัวละคร + อัปเกรด
        Hp = MaxHp;
        Coins = Score = AnimalsSolved = HintLeaves = 0;
        Distance = TotalDistance = 0f;
        Stage = 1;
        ReviveUsed = false;
        SeenAnimals.Clear();
        Initialized = true;
    }

    public static void NextStage() // ผ่านบอส -> ด่านต่อไป (เลือด/เหรียญ/คะแนนเก็บไว้)
    {
        Stage++;
        Distance = 0f;
        MissionSystem.Add(MissionSystem.Kind.Stage, Stage);
    }

    public static void EnsureInitializedForBoss() // กด Play ที่ฉากบอสตรงๆ ตอนทดสอบ
    {
        if (Initialized) return;
        Reset();
        Distance = TargetDistance;
    }
}
