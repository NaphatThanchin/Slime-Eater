using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// สไลม์คู่แข่ง (เราคือผู้ล่า): วิ่งนำในเลนเดียวกับผู้เล่น เร็วกว่าแต่เลือดน้อยกว่า หลบอุปสรรคเอง (พลาดได้) เก็บหัวใจเล็ก
// ผู้เล่นสะดุด = บอทเร่งหนี 6 วิ, ชนตัวบอทตอนมันยังมีเลือด = โจมตี (ลดเลือดตามพลังโจมตี) บอทหนีต่อ, เลือดหมด = ช้าลงแล้วชนเพื่อกิน
public class RivalBot : MonoBehaviour
{
    public SlimeMotion motion;          // ท่าทางวิ่งแบบเดียวกับผู้เล่น
    public CharacterModel model;
    public TMP_Text nameTag;
    public LanePlayer player;
    public Transform track;             // อุปสรรค/ไอเทม (TrackSpawner)
    public Material hueMaterial;        // shader เปลี่ยนเฉดสี ให้สีต่างจากผู้เล่น
    public float laneWidth = 2.4f;
    public float startGap = 30f;        // เกิดข้างหน้ากี่เมตร
    public float maxGap = 70f;          // หนีไกลสุด (ยังเห็นตัวเสมอ)
    public float hpRatio = 0.65f;       // เลือดบอท = 65% ของผู้เล่น (ทดสอบแล้ว: 50% ง่ายไป 80% ยากไป)
    public int crashDamage = 15;        // บอทพลาดชนอุปสรรค
    public float knockGap = 12f;        // โดนโจมตีแล้วกระเด็นนำไปกี่เมตร
    static readonly string[] First = { "Bubbly", "Jelly", "Gooey", "Mossy", "Puddle", "Sticky", "Wobbly", "Squishy", "Gloopy" };
    static readonly string[] Last = { "Sprout", "Fern", "Ember", "Mist", "Frost", "Crown" };
    int level = -1, lane;               // level = ตัวที่เท่าไรในรอบนี้ (ยิ่งกินมาก ตัวใหม่ยิ่งเก่งขึ้นนิดหน่อย)
    string botName = "";
    float gap, hp, maxHp, laneWait, stun, slowT, strikeCd;
    bool eating;
    readonly HashSet<Transform> judged = new HashSet<Transform>();
    readonly List<Transform> willCrash = new List<Transform>();
    public static int Attack => Mathf.RoundToInt((20 + 5 * SaveData.Level(SaveData.Upgrade.Attack)) * CharacterData.AttackMul[CharacterData.Current]); // พลังโจมตี (อัปเกรด + ตัวละคร)
    int Step => Mathf.Min(level, 5);
    float FailChance => 0.3f - 0.03f * Step;                                          // ตัวแรกๆ พลาดบ่อย ตัวหลังเก่งขึ้น
    float BotSpeed => 1.05f + 0.01f * Step - (SkyCycle.Evening ? 0.03f : 0f);         // เร็วกว่าผู้เล่น 5-10% (ยามเย็นช้าลง)
    public float Gap => gap;

    void Update()
    {
        var gm = GameManager.Instance;
        bool active = gm && gm.State == GameState.RunnerPhase && !TutorialGuide.Active && WorldScroller.Instance;
        if (gm && gm.ui) gm.ui.ShowRival(active && !eating);
        if (!active) { Show(false); return; }
        if (eating || gm.IsPaused) return;
        if (level != GameSession.EatenThisRun) Spawn();

        float dt = Time.deltaTime, v = WorldScroller.Instance.CurrentSpeed;
       float rel;                                                            // ความเร็วเทียบผู้เล่น (m/s)
        if (hp > 0f)
        {
            rel = v * (BotSpeed - 1f) + (GameSession.StumbleTimer > 0f ? v * 0.15f : 0f); // ผู้เล่นสะดุด = เร่งหนี
            hp -= gm.DrainPerSec * dt;                                          // เลือดลดเท่าผู้เล่น (ต่างกันที่เลือดเริ่มน้อยกว่า)
        }
        else { slowT = Mathf.Min(1f, slowT + dt / 3f); rel = -v * 0.25f * slowT; } // หมดแรง ค่อยๆ ช้าลง
        if (stun > 0f) { stun -= dt; rel = -v * 0.35f; }                        // พลาดชน = สะดุดช้าลง
        if (strikeCd > 0f) strikeCd -= dt;
        gap = Mathf.Clamp(gap + rel * dt, 0f, maxGap);

        FollowLane(dt);
        Dodge(v);
        Hearts();
        motion.tired = hp <= 0f;
        float x = lane * laneWidth;
        motion.laneX = x;
        transform.position = new Vector3(Mathf.MoveTowards(transform.position.x, x, 10f * dt), transform.position.y, gap);
        Show(true);
        gm.ui.SetRival(botName, gap, Mathf.Clamp01(hp / maxHp), hp <= 0f);

        bool touch = gap < 1.3f && Mathf.Abs(transform.position.x - player.transform.position.x) < 1.2f && Mathf.Abs(transform.position.y - player.transform.position.y) < 1.4f;
        if (touch && hp <= 0f) StartCoroutine(EatRoutine(gm));
        else if (touch && strikeCd <= 0f) Strike(gm);
    }

    void Strike(GameManager gm) // ชนตัวบอทที่ยังมีแรง = โจมตี แล้วมันกระเด็นหนีต่อ (ยังไม่กิน)
    {
        strikeCd = 0.8f;
        hp -= Attack;
        gap = knockGap;
        RunnerCamera.Shake(0.15f);
        AudioManager.Play(AudioManager.Sfx.Eat);
    }

    void FollowLane(float dt) // ตามเลนผู้เล่น (ช้ากว่านิดหน่อย) ถ้าเลนนั้นมีของขวางข้างหน้าค่อยย้ายชั่วคราว
    {
        int want = player.Lane;
        if (Blocked(want, 14f)) want = FreeLane();
        if (want == lane) { laneWait = 0f; return; }
        if ((laneWait += dt) >= 0.3f) { lane = want; laneWait = 0f; }
    }

    void Dodge(float v) // มองอุปสรรคข้างหน้า: ตัดสินครั้งเดียวต่อชิ้น ว่าจะหลบได้หรือพลาด
    {
        float z = transform.position.z, look = v * 0.4f + 1f;
        foreach (Transform t in track)
        {
            var o = t.GetComponent<Obstacle>();
            if (!o || o.kind == Obstacle.Kind.Platform || judged.Contains(t)) continue;
            float dz = t.position.z - z;
            if (dz < 0.5f || dz > look) continue;
            bool full = o.kind == Obstacle.Kind.High || o.kind == Obstacle.Kind.Falling;
            if (!full && Mathf.Abs(t.position.x - lane * laneWidth) > 1.2f) continue;   // ไม่ใช่เลนเรา
            judged.Add(t);
            if (Random.value < FailChance) { willCrash.Add(t); continue; }
            if (o.kind == Obstacle.Kind.High) motion.Slide();
            else if (o.kind == Obstacle.Kind.Block) lane = FreeLane();
            else motion.Jump();                                                        // หิน / ต้นไม้ล้ม / ขอนกลิ้ง
        }
        for (int i = willCrash.Count - 1; i >= 0; i--) // พลาดจริงตอนถึงตัว
        {
            var t = willCrash[i];
            if (t && t.position.z - z > 0.3f) continue;
            willCrash.RemoveAt(i);
            if (!t) continue;
            hp -= crashDamage; stun = 1f;
        }
        judged.RemoveWhere(t => !t);
    }

    void Hearts() // บอทเก็บหัวใจเล็ก +10%
    {
        foreach (Transform t in track)
        {
            var c = t.GetComponent<Collectible>();
            if (!c || !c.botOnly || !t.gameObject.activeSelf) continue;
            if (Mathf.Abs(t.position.x - transform.position.x) < 1.2f && Mathf.Abs(t.position.z - gap) < 1.5f) { hp = Mathf.Min(maxHp, hp + maxHp * 0.1f); Destroy(t.gameObject); }
        }
    }

    bool Blocked(int l, float ahead) // เลนนี้มีหินใหญ่/ขอนกลิ้งข้างหน้าบอทไหม
    {
        foreach (Transform t in track)
        {
            var o = t.GetComponent<Obstacle>();
            if (!o || o.kind != Obstacle.Kind.Block) continue;
            float dz = t.position.z - gap;
            if (dz > 0f && dz < ahead && Mathf.Abs(t.position.x - l * laneWidth) < 1.2f) return true;
        }
        return false;
    }

    int FreeLane() // เลนที่ไม่มีของขวาง ใกล้ผู้เล่นที่สุด
    {
        int best = lane; float bestD = 99f;
        for (int l = -1; l <= 1; l++)
            if (!Blocked(l, 16f) && Mathf.Abs(l - player.Lane) < bestD) { best = l; bestD = Mathf.Abs(l - player.Lane); }
        return best;
    }

    void Spawn() // บอทตัวใหม่ วิ่งนำข้างหน้า + สุ่มหน้าตา/ชื่อ/เฉดสี
    {
        level = GameSession.EatenThisRun;
        botName = First[Random.Range(0, First.Length)] + " " + Last[Random.Range(0, Last.Length)];
        lane = player.Lane;
        maxHp = hp = GameSession.MaxHp * hpRatio;
        gap = startGap; stun = slowT = 0f;
        judged.Clear(); willCrash.Clear();
        model.Show(Step >= 4 ? Random.Range(2, 4) : Random.Range(0, 2));           // ตัวหลังๆ หน้าตาเท่ขึ้น
        float hue = Random.value;
        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
        {
            var m = new Material(hueMaterial);
            m.SetTexture("_BaseMap", r.sharedMaterial.GetTexture("_BaseMap"));
            m.SetFloat("_Hue", hue);
            r.sharedMaterial = m;
        }
        nameTag.text = botName;
        motion.visual.localScale = Vector3.one * 1.3f;
        transform.position = new Vector3(lane * laneWidth, 0f, gap);
    }

    void Show(bool on) { if (model.gameObject.activeSelf != on) { model.gameObject.SetActive(on); nameTag.gameObject.SetActive(on); } }

    IEnumerator EatRoutine(GameManager gm) // บอทหดตัวไหลเข้าหาผู้เล่น แล้วผู้เล่นตัวโต + เลือด +20% + รางวัล
    {
        eating = true;
        nameTag.gameObject.SetActive(false);
        Vector3 from = transform.position, scale = motion.visual.localScale;
        motion.enabled = false;                                           // หยุดท่าเด้งระหว่างถูกดูด
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(from, player.transform.position + Vector3.up * 0.5f, t / 0.3f);
            motion.visual.localScale = scale * (1f - t / 0.3f);
            yield return null;
        }
        Show(false);
        motion.enabled = true;
        int coins = 40 + 15 * Step;                                         // ตัวหลังๆ รางวัลมากขึ้น
        int score = 600 + 150 * Step;
        if (SkyCycle.Evening) { coins = coins * 3 / 2; score *= 2; }               // ยามเย็น = เวลาล่า
        GameSession.EatenThisRun++;
        MissionSystem.Add(MissionSystem.Kind.EatBots, 1);
        MissionSystem.Add(MissionSystem.Kind.EatInRun, GameSession.EatenThisRun);
        CharacterData.AddQuest(CharacterData.Quest.EatBots, 1);
        if (GameSession.ChaseHits == 0) MissionSystem.Add(MissionSystem.Kind.CleanCatch, 1);
        GameSession.ChaseHits = 0;
        GameSession.Coins += coins;                                        // รางวัลบอทไม่คูณตัวคูณเหรียญ (กันเศรษฐกิจพัง)
        MissionSystem.Add(MissionSystem.Kind.Coins, coins);
        gm.AddScore(score);
        gm.Heal(Mathf.CeilToInt(GameSession.MaxHp * 0.2f * CharacterData.HealMul[CharacterData.Current])); // กินบอท = เลือด +20%
        player.motion.Grow(6f);
        AudioManager.Play(AudioManager.Sfx.Eat);
        RunnerCamera.Shake(0.2f);
                                                                           // EatenThisRun เพิ่มแล้ว -> ตัวใหม่ออกมาวิ่งนำทันที
        eating = false;
    }
}
