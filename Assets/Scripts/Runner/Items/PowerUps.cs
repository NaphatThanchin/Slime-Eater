using UnityEngine;

// ไอเทมพลังพิเศษ: แม่เหล็ก, เกราะกันชน 1 ครั้ง, เหรียญ x2, สปริงเห็ด (กระโดดสูง), คะแนน x2, ฟองสไลม์ (ของซื้อ กันชน 20 วิ รอบละ 1)
public class PowerUps : MonoBehaviour
{
    public enum Kind { Magnet, Shield, Double, Spring, Score, Bubble }
    public const int KindCount = 6;
    public static PowerUps Instance { get; private set; }
    public float magnetRadius = 7f;
    public GameObject shieldBubble;          // เกราะรอบตัว
    readonly float[] timers = new float[KindCount], durations = new float[KindCount];
    bool bubbleUsed;                         // ฟองสไลม์ใช้ได้รอบละ 1
    public static System.Action<Kind, float> OnChanged; // แจ้ง UI (ชนิด, เวลาที่เหลือ 0-1)

    public static int CoinMultiplier => Instance && Instance.Active(Kind.Double) ? 2 : 1;
    public static int ScoreMultiplier => Instance && Instance.Active(Kind.Score) ? 2 : 1;
    public bool Active(Kind k) => timers[(int)k] > 0f;
    public bool Magnetic => Active(Kind.Magnet) || (CharacterAbility.Instance && CharacterAbility.Instance.Flying); // บินอยู่ = ดูดเหรียญด้วย
    public bool CanBubble => !bubbleUsed && SaveData.D.bubbles > 0;
    float Duration(Kind k) => k == Kind.Double || k == Kind.Score ? 10f : k == Kind.Spring ? 8f : 8f + SaveData.Level(k == Kind.Magnet ? SaveData.Upgrade.Magnet : SaveData.Upgrade.Shield); // อัปเกรด +1 วิ/เลเวล

    void Awake() { Instance = this; }

    public void Activate(Kind k) { Activate(k, Duration(k)); MissionSystem.Add(MissionSystem.Kind.PowerUps, 1); }

    public void Activate(Kind k, float seconds) // ใช้ตรงๆ ด้วย (King แม่เหล็กอัตโนมัติ ไม่นับภารกิจ)
    {
        timers[(int)k] = Mathf.Max(timers[(int)k], seconds);
        durations[(int)k] = Mathf.Max(seconds, 0.01f);
    }

    public void UseBubble() // ปุ่มฟองสไลม์ / Q
    {
        var gm = GameManager.Instance;
        if (!CanBubble || (gm && gm.State != GameState.RunnerPhase)) return;
        bubbleUsed = true;
        SaveData.D.bubbles--; SaveData.Save();
        Activate(Kind.Bubble, 20f);
    }

    public bool ConsumeShield() // โดนชนตอนมีเกราะ: ฟองสไลม์กันได้ตลอดเวลา, เกราะธรรมดาแตก 1 ครั้ง
    {
        if (Active(Kind.Bubble)) return true;
        if (!Active(Kind.Shield)) return false;
        timers[(int)Kind.Shield] = 0f;
        return true;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && !gm.IsPlaying) return;
        for (int i = 0; i < timers.Length; i++)
        {
            if (timers[i] <= 0f) { OnChanged?.Invoke((Kind)i, 0f); continue; }
            timers[i] = Mathf.Max(0f, timers[i] - Time.deltaTime);
            OnChanged?.Invoke((Kind)i, timers[i] / durations[i]);
        }
        bool bubble = Active(Kind.Shield) || Active(Kind.Bubble) || (CharacterAbility.Instance && CharacterAbility.Instance.Invincible); // โชว์ตอนอมตะด้วย
        if (shieldBubble && shieldBubble.activeSelf != bubble) shieldBubble.SetActive(bubble);
    }
}
