using UnityEngine;

// ไอเทมในด่าน: เหรียญ, หัวใจ (% เลือดสูงสุด), ใบไม้วิเศษ, แม่เหล็ก, เกราะ, เหรียญ x2, ตัวอักษร (ก่อน Bonus time)
public class Collectible : ScrollingObject
{
    public enum Kind { Coin, Heal, HintLeaf, Magnet, Shield, Double, Letter, Spring, Score }
    public const int MaxLeaves = 3;
    public Kind kind = Kind.Coin;
    public int value = 1;           // จำนวนเหรียญ / % เลือดที่ฮีล / ช่องตัวอักษรในคำ
    public float spinSpeed = 180f;
    public float bobHeight = 0.15f;
    public bool botOnly;            // หัวใจเล็ก: บอทเก็บได้อย่างเดียว ผู้เล่นวิ่งทะลุ
    [HideInInspector] public TrackSpawner owner; // เหรียญจาก pool -> คืน pool แทนการลบ
    Transform visual;
    float bobSeed;

    void Start()
    {
        visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
        bobSeed = Random.value * 10f;
    }

    protected override void Update()
    {
        base.Update();
        var p = PowerUps.Instance;
        if (kind == Kind.Coin && p && p.Magnetic) // แม่เหล็ก/บิน ดูดเหรียญเข้าหาตัว
        {
            Vector3 to = p.transform.position + Vector3.up * 0.5f - transform.position;
            if (to.magnitude < p.magnetRadius) transform.position += to.normalized * 18f * Time.deltaTime;
        }
        if (visual == null) return;
        visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World); // หมุนแบบ Coin_Spin
        visual.localPosition = Vector3.up * Mathf.Sin((Time.time + bobSeed) * 4f) * bobHeight;
    }

    protected override void Despawn() // หลุดหลังกล้อง: ตัวอักษรที่พลาดจะเกิดใหม่ข้างหน้า
    {
        if (kind == Kind.Letter && owner) owner.LetterMissed(value);
        Remove();
    }

    void Remove() { if (owner && kind == Kind.Coin) owner.Release(this); else Destroy(gameObject); }

    public void Collect(GameManager gm)
    {
        if (botOnly) return;
        switch (kind)
        {
            case Kind.Coin:
                gm.AddCoin(value); AudioManager.Play(AudioManager.Sfx.Coin);
                if (CharacterAbility.Instance) CharacterAbility.Instance.OnCoin();
                break;
            case Kind.Heal:
                gm.Heal(Mathf.CeilToInt(GameSession.MaxHp * value / 100f * CharacterAbility.HealBoost));
                CharacterData.AddQuest(CharacterData.Quest.Hearts, 1);
                MissionSystem.Add(MissionSystem.Kind.Hearts, 1);                 // นับปลดซื้อ SlimeAngel
                break;
            case Kind.HintLeaf:
                if (GameSession.HintLeaves >= MaxLeaves) break;                   // ถือได้สูงสุด 3 ใบ
                GameSession.HintLeaves++; if (gm.ui) gm.ui.SetHintLeaves(GameSession.HintLeaves);
                break;
            case Kind.Magnet: PowerUps.Instance?.Activate(PowerUps.Kind.Magnet); break;
            case Kind.Shield: PowerUps.Instance?.Activate(PowerUps.Kind.Shield); break;
            case Kind.Double: PowerUps.Instance?.Activate(PowerUps.Kind.Double); break;
            case Kind.Spring: PowerUps.Instance?.Activate(PowerUps.Kind.Spring); break;
            case Kind.Score: PowerUps.Instance?.Activate(PowerUps.Kind.Score); break;
            case Kind.Letter:
                if (value < GameSession.Got.Length) GameSession.Got[value] = true;
                AudioManager.Play(AudioManager.Sfx.Eat);
                if (gm.ui) gm.ui.SetWord();
                owner = null;   // เก็บแล้ว ไม่นับว่าพลาด
                break;
        }
        Remove();
    }
}
