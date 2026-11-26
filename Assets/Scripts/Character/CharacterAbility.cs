using UnityEngine;

// ความสามารถพิเศษของตัวละคร: เกราะปีศาจ / โหมดปีศาจ (SlimeDemon), เกิดใหม่ (SlimeAngel)
public class CharacterAbility : MonoBehaviour
{
    public static CharacterAbility Instance { get; private set; }
    public float guardCooldown = 10f;   // เกราะปีศาจชาร์จใหม่
    public int rageHits = 3;            // ชนครบกี่ครั้งเข้าโหมดปีศาจ
    public float rageTime = 30f;        // โหมดปีศาจ: ชนไม่เสียเลือด
    public float reviveInvincible = 30f;
    float guardTimer, invincible;
    int hits;
    bool warned;
    public bool Invincible => invincible > 0f;
    CharacterData.Ability Ability => CharacterData.CurrentAbility;

    void Awake() { Instance = this; }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm && !gm.IsPlaying) return;
        if (guardTimer > 0f) guardTimer -= Time.deltaTime;
        if (invincible <= 0f) return;
        invincible -= Time.deltaTime;
        if (invincible <= 3f && !warned) { warned = true; Toast("อมตะใกล้หมดแล้ว!"); }
    }

    public bool TryBlockHit() // ชนสิ่งกีดขวาง -> true = ไม่เสียเลือด
    {
        if (Invincible) return true;
        if (Ability != CharacterData.Ability.DemonGuard) return false;
        hits++;
        if (hits >= rageHits) { hits = 0; StartInvincible(rageTime, "โหมดปีศาจ! ชนไม่เสียเลือด " + rageTime + " วิ"); return true; }
        if (guardTimer > 0f) return false;            // เกราะยังชาร์จไม่เสร็จ
        guardTimer = guardCooldown;
        Toast("เกราะปีศาจกันไว้ได้! (" + hits + "/" + rageHits + ")");
        return true;
    }

    public bool TryRevive() // เลือดหมด -> SlimeAngel เกิดใหม่ 1 ครั้งต่อรอบ
    {
        if (Ability != CharacterData.Ability.AngelRevive || GameSession.ReviveUsed) return false;
        GameSession.ReviveUsed = true;
        GameSession.Hp = GameSession.MaxHp;
        StartInvincible(reviveInvincible, "นางฟ้าชุบชีวิต! อมตะ " + reviveInvincible + " วิ");
        return true;
    }

    void StartInvincible(float t, string msg) { invincible = t; warned = false; Toast(msg); Debug.Log("[Ability] " + msg); }
    void Toast(string s) { var gm = GameManager.Instance; if (gm && gm.ui) gm.ui.ShowToast(s); }
}
