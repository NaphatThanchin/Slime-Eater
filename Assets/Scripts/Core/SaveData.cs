using UnityEngine;

// ข้อมูลที่บันทึกถาวร (PlayerPrefs): เหรียญสะสม สถิติ อัปเกรด ตัวละคร
public static class SaveData
{
    public const int MaxUpgrade = 3;

    public static int Bank { get => PlayerPrefs.GetInt("Bank", 0); set => PlayerPrefs.SetInt("Bank", Mathf.Max(0, value)); } // เหรียญสะสมไว้ซื้อของ
    public static int BestScore { get => PlayerPrefs.GetInt("BestScore", 0); set => PlayerPrefs.SetInt("BestScore", value); }
    public static int BestStage { get => PlayerPrefs.GetInt("BestStage", 0); set => PlayerPrefs.SetInt("BestStage", value); }
    public static int Character { get => PlayerPrefs.GetInt("Char", 0); set { PlayerPrefs.SetInt("Char", value); PlayerPrefs.Save(); } } // ตัวละครที่ใช้อยู่
    public static int Upgrade(string key) => PlayerPrefs.GetInt("Up_" + key, 0); // Magnet / Shield / Hp
    public static void SetUpgrade(string key, int lv) => PlayerPrefs.SetInt("Up_" + key, Mathf.Clamp(lv, 0, MaxUpgrade));
    public static int UpgradePrice(string key) => 150 * (Upgrade(key) + 1);
    public static bool OwnsCharacter(int i) => i == 0 || PlayerPrefs.GetInt("Char_" + i, 0) == 1; // SlimeGreen ได้ฟรี
    public static void UnlockCharacter(int i) { PlayerPrefs.SetInt("Char_" + i, 1); PlayerPrefs.Save(); }

    public static bool TryBuy(int price) // หักเหรียญถ้าพอ
    {
        if (Bank < price) return false;
        Bank -= price;
        PlayerPrefs.Save();
        return true;
    }

    public static bool SubmitRun(int score, int stage, int coins) // จบรอบ -> เก็บเหรียญเข้ากระเป๋า + เช็กสถิติใหม่
    {
        Bank += coins;
        bool newBest = score > BestScore;
        if (newBest) BestScore = score;
        if (stage > BestStage) BestStage = stage;
        PlayerPrefs.Save();
        Debug.Log("[SaveData] จบรอบ score=" + score + " stage=" + stage + " bank=" + Bank + (newBest ? " (สถิติใหม่!)" : ""));
        return newBest;
    }
}
