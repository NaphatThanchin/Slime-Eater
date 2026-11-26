using UnityEngine;

// ข้อมูลตัวละครในร้านค้า: ราคา เลือด อัตราเลือดลด ค่าเหรียญ ความสามารถ
public class CharacterData : MonoBehaviour
{
    public enum Ability { None, Rich, DemonGuard, AngelRevive }
    public static readonly string[] Names = { "SlimeGreen", "SlimeKing", "SlimeDemon", "SlimeAngel" };
    public static readonly string[] Folders = { "Slime_Green", "Slime_King", "Slime_Demon", "Slime_Angel" }; // โฟลเดอร์ใน Character_Model
    public static readonly int[] Prices = { 0, 1500, 5000, 10000 };
    public static readonly int[] MaxHp = { 100, 130, 160, 200 };
    public static readonly float[] DrainRate = { 1f, 0.8f, 0.65f, 0.5f }; // ตัวคูณเลือดลดตอนวิ่ง (น้อย = ลดช้า)
    public static readonly int[] CoinValue = { 1, 2, 1, 1 };               // เหรียญ 1 อันได้กี่เหรียญ
    public static readonly Ability[] Abilities = { Ability.None, Ability.Rich, Ability.DemonGuard, Ability.AngelRevive };
    static readonly string[] AbilityText =
    {
        "ตัวเริ่มต้น สมดุลทุกด้าน",
        "เก็บเหรียญได้ x2 เงินเยอะกว่าเดิม",
        "ชนสิ่งกีดขวางไม่เสียเลือด 1 ครั้ง (ชาร์จใหม่ทุก 10 วิ)\nชนครบ 3 ครั้ง = โหมดปีศาจ ชนอะไรก็ไม่เสียเลือด 30 วิ",
        "ตายแล้วเกิดใหม่เลือดเต็ม 1 ครั้ง\nพร้อมอมตะไม่โดนดาเมจ 30 วิ",
    };
    public static int Count => Names.Length;
    public static int Current => Mathf.Clamp(SaveData.Character, 0, Count - 1);
    public static Ability CurrentAbility => Abilities[Current];

    public static string Info(int i) // ข้อความในหน้าดูตัวละคร
    {
        int less = Mathf.RoundToInt((1f - DrainRate[i]) * 100f);
        return "เลือดสูงสุด  " + MaxHp[i] + "\nเลือดลดตอนวิ่ง  " + (less > 0 ? "น้อยลง " + less + "%" : "ปกติ") + "\n<color=#FFE27A>" + AbilityText[i] + "</color>";
    }
}
