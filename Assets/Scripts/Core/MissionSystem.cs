using UnityEngine;

// ภารกิจ 3 อย่างพร้อมกัน ทำสำเร็จได้เหรียญรางวัล แล้วสุ่มภารกิจใหม่มาแทน (บันทึกถาวร)
public static class MissionSystem
{
    public enum Kind { Coins, Jumps, Slides, Distance, Animals, PowerUps, Stage }
    static readonly string[] Titles = { "เก็บเหรียญ {0} เหรียญ", "กระโดด {0} ครั้ง", "สไลด์ {0} ครั้ง", "วิ่งให้ได้ {0} เมตร", "ทายสัตว์ถูก {0} ตัว", "เก็บไอเทมพลังพิเศษ {0} ชิ้น", "ไปให้ถึงด่าน {0}" };
    static readonly int[] Targets = { 150, 40, 25, 2000, 8, 6, 3 };
    public const int Count = 3;
    public static System.Action<string> OnCompleted; // แจ้ง UI ตอนทำภารกิจสำเร็จ

    public static Kind KindAt(int slot) => (Kind)PlayerPrefs.GetInt("Mis_K" + slot, slot);
    public static int Level(int slot) => PlayerPrefs.GetInt("Mis_L" + slot, 1);       // ยิ่งทำมากยิ่งยาก
    public static int Target(int slot) => Targets[(int)KindAt(slot)] * Level(slot);
    public static int Progress(int slot) => PlayerPrefs.GetInt("Mis_P" + slot, 0);
    public static int Reward(int slot) => 50 * Level(slot) + 50;
    public static string Title(int slot) => string.Format(Titles[(int)KindAt(slot)], Target(slot));

    public static void Add(Kind kind, int amount) // เรียกตอนเกิดเหตุการณ์ในเกม
    {
        for (int s = 0; s < Count; s++)
        {
            if (KindAt(s) != kind) continue;
            int p = kind == Kind.Stage ? Mathf.Max(Progress(s), amount) : Progress(s) + amount;
            PlayerPrefs.SetInt("Mis_P" + s, p);
            if (p >= Target(s)) Complete(s);
        }
    }

    static void Complete(int slot)
    {
        string title = Title(slot);
        SaveData.Bank += Reward(slot);
        Debug.Log("[Mission] สำเร็จ: " + title + " +" + Reward(slot) + " เหรียญ");
        OnCompleted?.Invoke(title);
        int next;
        do next = Random.Range(0, Titles.Length); while (IsActive((Kind)next));
        PlayerPrefs.SetInt("Mis_K" + slot, next);
        PlayerPrefs.SetInt("Mis_L" + slot, Level(slot) + 1);
        PlayerPrefs.SetInt("Mis_P" + slot, 0);
        PlayerPrefs.Save();
    }

    static bool IsActive(Kind k)
    {
        for (int s = 0; s < Count; s++) if (KindAt(s) == k) return true;
        return false;
    }
}
