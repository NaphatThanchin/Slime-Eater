using UnityEngine;

// ข้อมูลสัตว์ 1 ตัวสำหรับฉากบอส (ไฟล์ข้อมูล ScriptableObject ไม่ได้ติดกับวัตถุในฉาก)
[CreateAssetMenu(fileName = "Animal", menuName = "Slime Eater/Animal Data")]
public class AnimalDataSO : ScriptableObject
{
    public string englishName;              // คำตอบ
    [TextArea(2, 5)] public string thaiHint; // คำใบ้ภาษาไทย
    public Sprite fullColorSprite;          // ภาพสีจริง
    public Sprite silhouetteSprite;         // ภาพเงา
    public int LetterCount => string.IsNullOrEmpty(englishName) ? 0 : englishName.Length;
    public string Answer => englishName.Trim().ToUpperInvariant();
}
