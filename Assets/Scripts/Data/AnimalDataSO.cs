using UnityEngine;

// ข้อมูลสัตว์ 1 ตัวสำหรับฉาก Bonus time 
[CreateAssetMenu(fileName = "Animal", menuName = "Slime Eater/Animal Data")]
public class AnimalDataSO : ScriptableObject
{
    public string englishName;              // คำตอบ
    public Sprite fullColorSprite;          // ภาพสีจริง
    public Sprite silhouetteSprite;         // ภาพเงา
    public int LetterCount => string.IsNullOrEmpty(englishName) ? 0 : englishName.Length;
    public string Answer => englishName.Trim().ToUpperInvariant();
}
