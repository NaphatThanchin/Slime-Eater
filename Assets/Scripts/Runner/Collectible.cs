using UnityEngine;

// ไอเทมในด่าน: เหรียญ, หัวใจ, ใบไม้วิเศษ (ใช้ในฉากบอส), แม่เหล็ก, เกราะ, เหรียญ x2
public class Collectible : ScrollingObject
{
    public enum Kind { Coin, Heal, HintLeaf, Magnet, Shield, Double }
    public Kind kind = Kind.Coin;
    public int value = 1;           // จำนวนเหรียญ / เลือดที่ฮีล
    public float spinSpeed = 180f;
    public float bobHeight = 0.15f;
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
        if (kind == Kind.Coin && p && p.Active(PowerUps.Kind.Magnet)) // แม่เหล็กดูดเหรียญเข้าหาตัว
        {
            Vector3 to = p.transform.position + Vector3.up * 0.5f - transform.position;
            if (to.magnitude < p.magnetRadius) transform.position += to.normalized * 18f * Time.deltaTime;
        }
        if (visual == null) return;
        visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World); // หมุนแบบ Coin_Spin
        visual.localPosition = Vector3.up * Mathf.Sin((Time.time + bobSeed) * 4f) * bobHeight;
    }

    public void Collect(GameManager gm)
    {
        switch (kind)
        {
            case Kind.Coin: gm.AddCoin(value); AudioManager.Play(AudioManager.Sfx.Coin); break;
            case Kind.Heal: gm.Heal(value); break;
            case Kind.HintLeaf: GameSession.HintLeaves++; if (gm.ui) gm.ui.SetHintLeaves(GameSession.HintLeaves); break;
            case Kind.Magnet: PowerUps.Instance?.Activate(PowerUps.Kind.Magnet); break;
            case Kind.Shield: PowerUps.Instance?.Activate(PowerUps.Kind.Shield); break;
            case Kind.Double: PowerUps.Instance?.Activate(PowerUps.Kind.Double); break;
        }
        Destroy(gameObject);
    }
}
