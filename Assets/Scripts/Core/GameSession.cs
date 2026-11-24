using System.Collections.Generic;
using UnityEngine;

// ข้อมูลรอบเล่นที่ส่งต่อข้ามฉาก (วิ่ง -> บอส -> วิ่งด่านต่อไป ...)
public static class GameSession
{
    public const float TargetDistance = 1000f; // ระยะต่อด่านก่อนเจอบอส (เมตร)
    public static int MaxHp = 100;
    public static int Hp;
    public static int Coins;
    public static int Score;
    public static float Distance;            // ระยะในด่านนี้
    public static float TotalDistance;       // ระยะรวมทั้งรอบ
    public static int AnimalsSolved;
    public static int Stage;                 // ด่านที่ 1, 2, 3 ...
    public static int HintLeaves;            // ใบไม้วิเศษ ใช้เปิดตัวอักษรในฉากบอส
    public static bool Initialized;
    public static bool ReviveUsed;           // SlimeAngel เกิดใหม่ไปแล้วในรอบนี้
    public static readonly List<string> SeenAnimals = new List<string>(); // สัตว์ที่ออกไปแล้ว (ไม่ซ้ำจนกว่าจะครบ 21)

    public static void Reset() // เริ่มรอบใหม่
    {
        MaxHp = CharacterData.MaxHp[CharacterData.Current] + 10 * SaveData.Upgrade("Hp"); // เลือดตามตัวละคร + อัปเกรด
        ReviveUsed = false;
        Hp = MaxHp;
        Coins = 0;
        Score = 0;
        Distance = 0f;
        TotalDistance = 0f;
        AnimalsSolved = 0;
        Stage = 1;
        HintLeaves = 0;
        SeenAnimals.Clear();
        Initialized = true;
        Debug.Log("[GameSession] Reset -> HP " + Hp);
    }

    public static void NextStage() // ผ่านบอส -> วิ่งด่านต่อไป (เลือด/เหรียญ/คะแนนเก็บไว้)
    {
        Stage++;
        Distance = 0f;
        MissionSystem.Add(MissionSystem.Kind.Stage, Stage);
        Debug.Log("[GameSession] ไปด่าน " + Stage);
    }

    public static float SpeedMultiplier => Stage <= 1 ? 1f : Mathf.Min(2.3f, 1.3f + 0.2f * (Stage - 2)); // ด่าน 2 เร็วขึ้น 30% แล้ว +20% ต่อด่าน

    public static void EnsureInitializedForBoss() // กด Play ที่ฉากบอสตรงๆ ตอนทดสอบ
    {
        if (Initialized) return;
        Reset();
        Distance = TargetDistance;
        Debug.LogWarning("[GameSession] BossScene เปิดโดยตรง -> ใช้ค่าทดสอบ");
    }
}
