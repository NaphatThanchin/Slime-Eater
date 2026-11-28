using UnityEngine;

// ความสามารถตัวละคร (ต้องปลดด้วยเควสก่อน): Green ฮีลแรง / King เหรียญ+แม่เหล็ก / Demon เกราะ+โหมดปีศาจ / Angel ฟื้นเลือด+เกิดใหม่+บิน
public class CharacterAbility : MonoBehaviour
{
    public static CharacterAbility Instance { get; private set; }
    public float guardCooldown = 20f;   // Demon: เกราะชาร์จใหม่
    public int rageHits = 5;            // ชนครบกี่ครั้งเข้าโหมดปีศาจ
    public float rageTime = 10f;        // โหมดปีศาจ: ชนไม่เสียเลือด
    public float reviveInvincible = 10f;
    public float regenEvery = 30f;      // Angel ฟื้นเลือด 5% ทุก 30 วิ
    public float magnetEvery = 45f;     // King แม่เหล็กอัตโนมัติ
    public int flyStreak = 40;          // Angel ขั้น 2: เก็บเหรียญติดกันกี่อันถึงบิน
    public float flyTime = 6f;
    float guardTimer, invincible, regenTimer, magnetTimer, flyTimer;
    int hits, streak;
    bool rage;
    public bool Invincible => invincible > 0f;
    public bool Smash => rage && Invincible && Tier2(CharacterData.Ability.DemonGuard);  // โหมดปีศาจขั้น 2 ชนแล้วอุปสรรคแตก
    public bool Flying => flyTimer > 0f;
    static int MyTier => CharacterData.Tier(CharacterData.Current);
    public static bool Has(CharacterData.Ability a) => CharacterData.CurrentAbility == a && MyTier >= 1;
    public static bool Tier2(CharacterData.Ability a) => CharacterData.CurrentAbility == a && MyTier >= 2;
    public static float BonusTimePerCorrect => MyTier >= 2 ? 3f : 0f;              // ขั้น 2 ทุกตัว: ตอบถูกใน Bonus +3 วิ
    public static float HealBoost => (Has(CharacterData.Ability.Forest) ? 1.5f : 1f) * CharacterData.HealMul[CharacterData.Current]; // ค่าฮีลตามตัวละคร

    void Awake() { Instance = this; magnetTimer = magnetEvery; regenTimer = regenEvery; }

    void Update()
    {
        var gm = GameManager.Instance;
        if ((gm && !gm.IsPlaying) || TutorialGuide.Active) return;
        float dt = Time.deltaTime;
        if (guardTimer > 0f) guardTimer -= dt;
        if (flyTimer > 0f) flyTimer -= dt;
        if (Has(CharacterData.Ability.AngelRevive) && (regenTimer -= dt) <= 0f) { regenTimer = regenEvery; gm.Heal(Mathf.CeilToInt(GameSession.MaxHp * 0.05f)); }
        if (Has(CharacterData.Ability.Rich) && PowerUps.Instance && (magnetTimer -= dt) <= 0f) { magnetTimer = magnetEvery; PowerUps.Instance.Activate(PowerUps.Kind.Magnet, 6f); }
        if (invincible <= 0f) return;
        invincible -= dt;
        if (invincible <= 0f) rage = false;
    }

    public string Status // ข้อความสถานะใต้ HUD
    {
        get
        {
            if (MyTier == 0) return "";
            switch (CharacterData.CurrentAbility)
            {
                case CharacterData.Ability.DemonGuard: return rage && Invincible ? "โหมดปีศาจ " + Mathf.CeilToInt(invincible) + " วิ" : "เกราะ " + (guardTimer > 0f ? Mathf.CeilToInt(guardTimer) + " วิ" : "พร้อม") + "  ชน " + hits + "/" + rageHits;
                case CharacterData.Ability.AngelRevive: return Flying ? "บิน " + Mathf.CeilToInt(flyTimer) + " วิ" : (GameSession.ReviveUsed ? "" : "เกิดใหม่ได้ 1  ") + (Tier2(CharacterData.Ability.AngelRevive) ? "บิน " + streak + "/" + flyStreak : "");
                case CharacterData.Ability.Rich: return "แม่เหล็กอีก " + Mathf.CeilToInt(magnetTimer) + " วิ";
                default: return "";
            }
        }
    }

    public bool TryBlockHit() // ชนสิ่งกีดขวาง -> true = ไม่เสียเลือด
    {
        streak = 0;
        if (Invincible || Flying) return true;                         // ระหว่างโหมดปีศาจไม่นับการชน
        if (!Has(CharacterData.Ability.DemonGuard)) return false;
        if (++hits >= rageHits) { hits = 0; rage = true; invincible = rageTime; return true; } // โหมดปีศาจ
        if (guardTimer > 0f) return false;                             // เกราะยังชาร์จไม่เสร็จ
        guardTimer = guardCooldown;                                    // เกราะรับแทน 1 ครั้ง
        return true;
    }

    public void OnCoin() // Angel ขั้น 2: เก็บเหรียญติดกันครบ = บิน
    {
        if (!Tier2(CharacterData.Ability.AngelRevive) || Flying) return;
        if (++streak < flyStreak) return;
        streak = 0;
        flyTimer = flyTime;
    }

    public bool TryRevive() // เลือดหมด -> SlimeAngel เกิดใหม่ 1 ครั้งต่อเกม
    {
        if (!Has(CharacterData.Ability.AngelRevive) || GameSession.ReviveUsed) return false;
        GameSession.ReviveUsed = true;
        GameSession.Hp = GameSession.MaxHp;
        invincible = reviveInvincible;                                 // อมตะหลังเกิดใหม่
        return true;
    }
}
