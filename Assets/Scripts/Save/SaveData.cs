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
        public int[] upgrades = new int[3];                       // เลเวล แม่เหล็ก / เกราะ / เลือด (เวอร์ชัน 3 = แยกตัวละคร ตัวละ 3 ช่อง)
        public int saveVersion;                                   // 4 = อัปเกรด 4 ช่อง/ตัว + ของใช้ + รางวัลรอกดรับ
        public int[] charStats = new int[0];                      // ตัวนับเควสเฉพาะตัว [ตัวละคร x ชนิด]
        public int[] charClaimed = new int[0];                    // ขั้นความสามารถที่กดรับแล้วต่อตัว
        public int bubbles, potions;                              // ฟองสไลม์ / ยาฟื้นสไลม์ ที่มี
        public List<string> pendingTitles = new List<string>();   // ภารกิจที่ทำครบแล้ว รอกดรับ
        public List<int> pendingCoins = new List<int>();
        public float musicVolume = 0.5f, sfxVolume = 0.5f;      // ตำแหน่งแถบเสียง 0-1 (กลาง = ค่าพื้นฐาน)
        public bool tutorialDone;
        public string savedAt = "";
        // ลีก
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

    public enum Upgrade { Magnet, Shield, Hp, Attack }   // Attack = พลังโจมตี
    public const int Ups = 4;
    static Data data;
    static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");
    const string PrefsKey = "SaveJson";
    public static Data D { get { if (data == null) Load(); return data; } }

    // ค่าที่ใช้บ่อย
    public static int Bank { get => D.bank; set => D.bank = Mathf.Max(0, value); }
    public static int BestScore => D.bestScore;
    public static int BestStage => D.bestStage;
    public static string PlayerName { get => D.playerName; set { D.playerName = value; Save(); } }
    public static int Character { get => D.character; set { D.character = value; Save(); } }
    public static bool OwnsCharacter(int i) => D.owned[i];
    public static void UnlockCharacter(int i) { D.owned[i] = true; Save(); }
    public static int MaxLevel => 5 + 5 * CharacterData.Tier(CharacterData.Current); // ปลดความสามารถขั้น 1/2 = อัปเกรดได้ถึง Lv 10/15
    static int Slot(Upgrade u) => CharacterData.Current * Ups + (int)u; // อัปเกรดของตัวที่ใช้อยู่
    public static int Level(Upgrade u) => D.upgrades[Slot(u)];
    public static int UpgradePrice(Upgrade u) => Mathf.RoundToInt(250f * Mathf.Pow(1.25f, Level(u)) * CharacterData.UpgradeCost[CharacterData.Current] / 10f) * 10; // 250 -> 2,330 x ตัวละคร
    public const int BubblePrice = 800, PotionPrice = 2500;            // ของใช้แล้วหมด
    public static void LevelUp(Upgrade u) { D.upgrades[Slot(u)] = Mathf.Min(MaxLevel, Level(u) + 1); Save(); }

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

    public static void Save() // เขียนไฟล์ + สำรองใน PlayerPrefs (บนเว็บ itch.io เบราว์เซอร์เก็บ PlayerPrefs ถาวรแน่นอน)
    {
        D.savedAt = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        string json = JsonUtility.ToJson(D, true);
        try { File.WriteAllText(FilePath, json); } catch (System.Exception e) { Debug.LogWarning("[Save] เขียนไฟล์ไม่ได้ ใช้ PlayerPrefs แทน: " + e.Message); }
        PlayerPrefs.SetString(PrefsKey, json);
        PlayerPrefs.Save();
    }

    public static void ResetAll() // ลบข้อมูลทั้งหมด เริ่มเล่นใหม่
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        PlayerPrefs.DeleteKey(PrefsKey);
        data = new Data { missionVersion = 3 };
        Upgrade3(data); Upgrade4(data); data.saveVersion = 5;
        Save();
    }

    static void Load() // อ่านไฟล์ ถ้ายังไม่มีให้ย้ายข้อมูลจากระบบเก่า (PlayerPrefs)
    {
        string json = File.Exists(FilePath) ? File.ReadAllText(FilePath) : PlayerPrefs.GetString(PrefsKey, "");   // ไฟล์หาย (เว็บ) -> ใช้สำรองใน PlayerPrefs
        data = json != "" ? JsonUtility.FromJson<Data>(json) : FromPlayerPrefs();
        System.Array.Resize(ref data.owned, CharacterData.Count);   // กันไฟล์เก่าที่ขนาดไม่ตรง
        if (data.saveVersion < 3) Upgrade3(data);
        if (data.saveVersion < 4) Upgrade4(data);
        if (data.saveVersion < 5) { data.musicVolume = Mathf.Clamp01(data.musicVolume / AudioManager.MusicMax); data.sfxVolume = Mathf.Clamp01(data.sfxVolume / AudioManager.SfxMax); data.saveVersion = 5; } // แถบเสียงสเกลใหม่ (ดังเท่าเดิม)
        System.Array.Resize(ref data.upgrades, CharacterData.Count * Ups);
        System.Array.Resize(ref data.charStats, CharacterData.Count * CharacterData.QuestKinds);
        System.Array.Resize(ref data.charClaimed, CharacterData.Count);
        System.Array.Resize(ref data.stats, MissionSystem.KindCount);
        System.Array.Resize(ref data.missionDone, MissionSystem.Count);
        data.owned[0] = true;
        if (data.missionVersion < 3) MissionSystem.Migrate();      // ตารางภารกิจใหม่ (ไม่มีลีก) -> ของที่ทำถึงแล้วนับว่าสำเร็จ       // เปลี่ยนตารางภารกิจ -> นับใหม่ (ไม่ให้รางวัลซ้ำ)
    }

    static void Upgrade3(Data d) // เซฟเก่า: อัปเกรดเดิมคัดลอกให้ทุกตัวที่มีแล้ว + ล้างระยะจับบอทให้คำนวณใหม่ (ลีกง่ายขึ้น)
    {
        var old = d.upgrades ?? new int[3];
        d.upgrades = new int[CharacterData.Count * 3];
        for (int c = 0; c < CharacterData.Count; c++)
            for (int u = 0; u < 3; u++) d.upgrades[c * 3 + u] = c < d.owned.Length && d.owned[c] && u < old.Length ? old[u] : 0;
        d.charStats = new int[CharacterData.Count * CharacterData.QuestKinds];
        d.saveVersion = 3;
    }

    static void Upgrade4(Data d) // เซฟ v3 -> v4: อัปเกรด 3 -> 4 ช่องต่อตัว, ขั้นความสามารถที่ปลดไปแล้วนับว่ารับแล้ว
    {
        var old = d.upgrades ?? new int[0];
        d.upgrades = new int[CharacterData.Count * Ups];
        for (int c = 0; c < CharacterData.Count; c++)
            for (int u = 0; u < 3; u++) if (c * 3 + u < old.Length) d.upgrades[c * Ups + u] = old[c * 3 + u];
        System.Array.Resize(ref d.charStats, CharacterData.Count * CharacterData.QuestKinds);
        d.charClaimed = new int[CharacterData.Count];
        data = d;                                                         // RawTier อ่านจาก D
        for (int c = 0; c < CharacterData.Count; c++) d.charClaimed[c] = CharacterData.RawTier(c);
        d.saveVersion = 4;
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
