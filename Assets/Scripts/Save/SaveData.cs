using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ระบบบันทึกเกม: เก็บทุกอย่างลงไฟล์ save.json (ชื่อ เหรียญ สถิติ ตัวละคร อัปเกรด ลีก ภารกิจ เสียง)
public class SaveData : MonoBehaviour
{
    [System.Serializable]
    public class Data
    {
        public string playerName = "";
        public int bank, bestScore, bestStage, character;
        public bool[] owned = { true, false, false, false };     // ตัวละครที่ซื้อแล้ว (ตัวแรกฟรี)
        public int[] upgrades = new int[3];                       // เลเวล แม่เหล็ก / เกราะ / เลือด
        public float musicVolume = 0.4f, sfxVolume = 0.8f;      // ความดังเพลง / เสียงประกอบ
        public bool tutorialDone;
        public string savedAt = "";
        // ลีก
        public int league, leaguePoints, botsLeft = LeagueData.Bots, botFails;
        public float botCatch, leagueLastD;                       // ระยะจับบอทเป้าหมาย / ของบอทตัวก่อน
        public List<float> recentRuns = new List<float>();        // ระยะ 3 รอบล่าสุด
        public List<int> recentHp = new List<int>();              // เลือดสูงสุดตอนวิ่งรอบนั้น
        // ภารกิจ
        public int missionVersion;                                // 2 = ตารางภารกิจชุดที่มีการกินบอท
        public int[] stats = new int[MissionSystem.KindCount];   // ตัวนับสะสม
        public bool[] missionDone = new bool[MissionSystem.Count];
        public string dailyDate = "", lastFullDate = "";
        public int[] dailyPick = new int[3], dailyBase = new int[3];
        public bool[] dailyDone = new bool[3];
        public bool dailyBonus, endlessOn;
        public int streak;
        public int[] endlessLevel = new int[3], endlessBase = new int[3];
    }

    public enum Upgrade { Magnet, Shield, Hp }
    static Data data;
    static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");
    public static Data D { get { if (data == null) Load(); return data; } }

    // ค่าที่ใช้บ่อย
    public static int Bank { get => D.bank; set => D.bank = Mathf.Max(0, value); }
    public static int BestScore => D.bestScore;
    public static int BestStage => D.bestStage;
    public static string PlayerName { get => D.playerName; set { D.playerName = value; Save(); } }
    public static int Character { get => D.character; set { D.character = value; Save(); } }
    public static bool OwnsCharacter(int i) => D.owned[i];
    public static void UnlockCharacter(int i) { D.owned[i] = true; Save(); }
    public static int MaxLevel => 10 + 5 * D.league;                   // ขึ้นลีก = ปลดอัปเกรดเพิ่ม 5 เลเวล
    public static int Level(Upgrade u) => D.upgrades[(int)u];
    public static int UpgradePrice(Upgrade u) => 150 + 70 * Level(u);   // 150, 220, ... (Lv 1-10 รวม 4,650)
    public static void LevelUp(Upgrade u) { D.upgrades[(int)u] = Mathf.Min(MaxLevel, Level(u) + 1); Save(); }

    public static bool TryBuy(int price) // หักเหรียญถ้าพอ แล้วบันทึก
    {
        if (Bank < price) return false;
        Bank -= price;
        Save();
        return true;
    }

    public static bool SubmitRun(int score, int stage, int coins) // จบรอบ -> เหรียญเข้ากระเป๋า + เช็กสถิติใหม่
    {
        Bank += coins;
        bool newBest = score > D.bestScore;
        if (newBest) D.bestScore = score;
        D.bestStage = Mathf.Max(D.bestStage, stage);
        Save();
        return newBest;
    }

    public static void Save() // เขียนไฟล์
    {
        D.savedAt = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        File.WriteAllText(FilePath, JsonUtility.ToJson(D, true));
    }

    public static void ResetAll() // ลบข้อมูลทั้งหมด เริ่มเล่นใหม่
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        data = new Data { missionVersion = 2 };
        Save();
    }

    static void Load() // อ่านไฟล์ ถ้ายังไม่มีให้ย้ายข้อมูลจากระบบเก่า (PlayerPrefs)
    {
        data = File.Exists(FilePath) ? JsonUtility.FromJson<Data>(File.ReadAllText(FilePath)) : FromPlayerPrefs();
        System.Array.Resize(ref data.owned, CharacterData.Count);   // กันไฟล์เก่าที่ขนาดไม่ตรง
        System.Array.Resize(ref data.upgrades, 3);
        System.Array.Resize(ref data.stats, MissionSystem.KindCount);
        System.Array.Resize(ref data.missionDone, MissionSystem.Count);
        data.owned[0] = true;
        if (data.missionVersion < 2) MissionSystem.Migrate();       // เปลี่ยนตารางภารกิจ -> นับใหม่ (ไม่ให้รางวัลซ้ำ)
    }

    static Data FromPlayerPrefs()
    {
        var d = new Data
        {
            bank = PlayerPrefs.GetInt("Bank"), bestScore = PlayerPrefs.GetInt("BestScore"), bestStage = PlayerPrefs.GetInt("BestStage"),
            character = PlayerPrefs.GetInt("Char"), musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.4f),
            upgrades = new[] { PlayerPrefs.GetInt("Up_Magnet"), PlayerPrefs.GetInt("Up_Shield"), PlayerPrefs.GetInt("Up_Hp") },
        };
        for (int i = 1; i < d.owned.Length; i++) d.owned[i] = PlayerPrefs.GetInt("Char_" + i) == 1;
        d.stats[(int)MissionSystem.Kind.Stage] = d.bestStage;
        return d;
    }
}
