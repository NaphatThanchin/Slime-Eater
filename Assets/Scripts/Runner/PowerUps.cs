using UnityEngine;

// ไอเทมพลังพิเศษของสไลม์: แม่เหล็กดูดเหรียญ, เกราะกันชน 1 ครั้ง, เหรียญ x2
public class PowerUps : MonoBehaviour
{
    public enum Kind { Magnet, Shield, Double }
    public static PowerUps Instance { get; private set; }
    public float magnetRadius = 7f;
    public GameObject shieldBubble;          // ฟองเกราะรอบตัว
    readonly float[] timers = new float[3];
    public static System.Action<Kind, float> OnChanged; // แจ้ง UI (ชนิด, เวลาที่เหลือ 0-1)

    public static int CoinMultiplier => Instance && Instance.Active(Kind.Double) ? 2 : 1;
    public bool Active(Kind k) => timers[(int)k] > 0f;
    float Duration(Kind k) => k == Kind.Double ? 10f : 8f + 3f * SaveData.Upgrade(k == Kind.Magnet ? "Magnet" : "Shield"); // อัปเกรดแล้วนานขึ้น

    void Awake() { Instance = this; }

    public void Activate(Kind k)
    {
        timers[(int)k] = Duration(k);
        MissionSystem.Add(MissionSystem.Kind.PowerUps, 1);
        Debug.Log("[PowerUps] เปิด " + k + " " + Duration(k) + " วินาที");
    }

    public bool ConsumeShield() // โดนชนตอนมีเกราะ -> เกราะแตกแทน
    {
        if (!Active(Kind.Shield)) return false;
        timers[(int)Kind.Shield] = 0f;
        Debug.Log("[PowerUps] เกราะกันการชนไว้ได้");
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
            OnChanged?.Invoke((Kind)i, timers[i] / Duration((Kind)i));
        }
        bool bubble = Active(Kind.Shield) || (CharacterAbility.Instance && CharacterAbility.Instance.Invincible); // ฟองโชว์ตอนอมตะด้วย
        if (shieldBubble && shieldBubble.activeSelf != bubble) shieldBubble.SetActive(bubble);
    }
}
