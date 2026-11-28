using UnityEngine;
using UnityEngine.InputSystem;

// รับปุ่ม/การปัดจอ เปลี่ยนเลน กระโดด สไลด์ (กดล่วงหน้าได้) ชนหน้า/ชนข้าง สะดุด (โลกเลื่อนเข้าหาผู้เล่น ผู้เล่นอยู่กับที่)
public class LanePlayer : MonoBehaviour
{
    public RunnerConfig config;
    public SlimeMotion motion;         // ท่าทาง + ฟิสิกส์กระโดด
    public BoxCollider hitBox;
    public float buffer = 0.15f;       // กดกระโดด/สไลด์ล่วงหน้าก่อนลงพื้นได้
    public float stumbleTime = 6f;     // สะดุดแล้วบอทเร่งหนี + ชนซ้ำเสีย x2
    public int sideDamage = 5;         // ชนข้างตอนเปลี่ยนเลน
    int lane, prevLane;                // -1 ซ้าย, 0 กลาง, 1 ขวา
    float invincibleTimer, safeTimer, jumpBuf, slideBuf;  // safe = กันชนเงียบๆ (ช่วงสอนเล่น / เกิดใหม่)
    Vector3 baseBoxSize, baseBoxCenter;
    Renderer[] renderers;
    public int Lane => lane;
    public bool Hurt => invincibleTimer > 0f;

    void Awake() { baseBoxSize = hitBox.size; baseBoxCenter = hitBox.center; }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm && gm.IsPaused) return;
        if ((!gm || gm.State == GameState.RunnerPhase) && !TutorialGuide.Frozen) ReadInput(); // ช่วงสอนเล่นที่หยุดภาพ TutorialGuide
        float dt = Time.deltaTime;
        if (jumpBuf > 0f && (jumpBuf -= dt) > 0f && motion.Grounded) { jumpBuf = 0f; Jump(); }   // กดไว้ก่อนลงพื้น
        if (slideBuf > 0f && (slideBuf -= dt) > 0f && motion.Grounded && !motion.Sliding) { slideBuf = 0f; Slide(); }
        var p = transform.position;
        p.x = Mathf.MoveTowards(p.x, lane * config.laneWidth, config.laneSwitchSpeed * dt); // เลื่อนไปเลนเป้าหมาย
        transform.position = p;
        motion.laneX = lane * config.laneWidth;
        motion.flying = CharacterAbility.Instance && CharacterAbility.Instance.Flying; // Angel ขั้น 2 บินเหนืออุปสรรค
        motion.jumpBoost = PowerUps.Instance && PowerUps.Instance.Active(PowerUps.Kind.Spring) ? 1.2f : 1f;  // สปริงเห็ด: สูงขึ้นนิดหน่อย (ลอย ~2 m)
        float h = motion.Sliding ? 0.45f : 1f;                                                          // สไลด์ = กล่องชนเตี้ยลง
        hitBox.size = new Vector3(baseBoxSize.x, baseBoxSize.y * h, baseBoxSize.z);
        hitBox.center = new Vector3(baseBoxCenter.x, baseBoxCenter.y * h, baseBoxCenter.z);
        UpdateInvincibility();
        if (safeTimer > 0f) safeTimer -= dt;
    }

    public void Protect(float seconds) => safeTimer = Mathf.Max(safeTimer, seconds);

    void ReadInput() // คีย์บอร์ด A/D ←/→ Space/W/↑ S/↓/Shift Q = ฟองสไลม์ หรือปัดจอ (มือถือ/ลากเมาส์)
    {
        var kb = Keyboard.current;
        bool k(Key a, Key b) => kb != null && (kb[a].wasPressedThisFrame || kb[b].wasPressedThisFrame);
        if (k(Key.A, Key.LeftArrow) || SwipeInput.Left) ChangeLane(-1);
        if (k(Key.D, Key.RightArrow) || SwipeInput.Right) ChangeLane(1);
        if (k(Key.Space, Key.W) || k(Key.UpArrow, Key.UpArrow) || SwipeInput.Up) Jump();
        if (k(Key.S, Key.DownArrow) || k(Key.LeftShift, Key.LeftShift) || SwipeInput.Down) Slide();
        if (k(Key.Q, Key.Q) && PowerUps.Instance) PowerUps.Instance.UseBubble();
    }

    public void ChangeLane(int dir) { int l = Mathf.Clamp(lane + dir, -1, 1); if (l != lane) { prevLane = lane; lane = l; RunnerCamera.Tilt(dir); } } // เปลี่ยนเลนกลางอากาศได้

    public void Jump()
    {
        if (!motion.Jump()) { jumpBuf = buffer; return; }
        AudioManager.Play(AudioManager.Sfx.Jump);
        MissionSystem.Add(MissionSystem.Kind.Jumps, 1);
    }

    public void Slide()
    {
        if (!motion.Grounded && transform.position.y > motion.Ground + 0.6f) slideBuf = buffer; // ดิ่งลงแล้วสไลด์ทันทีที่ถึงพื้น
        motion.Slide();
        MissionSystem.Add(MissionSystem.Kind.Slides, 1);
    }

    void OnTriggerEnter(Collider other) // ชนสิ่งกีดขวาง / เก็บไอเทม
    {
        var gm = GameManager.Instance;
        if (!gm || !gm.IsPlaying) return;
        var item = other.GetComponentInParent<Collectible>();
        if (item) { item.Collect(gm); return; }
        var obstacle = other.GetComponentInParent<Obstacle>();
        var ab = CharacterAbility.Instance;
        if (!obstacle || invincibleTimer > 0f || safeTimer > 0f || (ab && ab.Flying)) return;
        if (ab && ab.Smash) { Destroy(obstacle.gameObject); gm.AddCoin(5); RunnerCamera.Shake(0.2f); return; } // โหมดปีศาจขั้น 2: ชนแตก
        bool wide = obstacle.kind == Obstacle.Kind.High || obstacle.kind == Obstacle.Kind.Falling;      // ขวางทั้งแถว ไม่มีชนข้าง
        bool side = !wide && Mathf.Abs(transform.position.x - lane * config.laneWidth) > 0.3f
                    && Mathf.Abs(obstacle.transform.position.x - lane * config.laneWidth) < 1f;       // กำลังเปลี่ยนเลนเข้าไปชนด้านข้าง
        renderers = motion.visual.GetComponentsInChildren<Renderer>();   // โมเดลถูกสร้างตอน Start จึงดึงตอนชน
        invincibleTimer = config.invincibleTime;
        if (side) lane = prevLane;                                       // เด้งกลับเลนเดิม
        if (PowerUps.Instance && PowerUps.Instance.ConsumeShield()) { RunnerCamera.Shake(0.15f); return; }  // เกราะ/ฟองสไลม์รับแทน
        if (ab && ab.TryBlockHit()) { RunnerCamera.Shake(0.15f); return; }                                  // ความสามารถตัวละคร
        GameSession.ChaseHits++;                                         // ภารกิจไล่ทันบอทแบบไม่โดนชน
        int dmg = side ? sideDamage : GameManager.HitDamage;
        if (GameSession.StumbleTimer > 0f) dmg *= 2;                     // สะดุดซ้ำใน 6 วิ = เสีย x2
        if (side) GameSession.HitsSide++; else GameSession.HitsFront++;
        GameSession.StumbleTimer = stumbleTime;                          // บอทได้จังหวะเร่งหนี
        gm.Damage(dmg);
        RunnerCamera.Shake(side ? 0.15f : 0.3f);
        RunnerCamera.HitStop();
        motion.Squash();
    }

    void UpdateInvincibility() // ตัวกระพริบตอนอมตะ
    {
        if (invincibleTimer <= 0f || renderers == null) return;
        invincibleTimer -= Time.deltaTime;
        bool visible = invincibleTimer <= 0f || Mathf.Repeat(invincibleTimer, 0.16f) > 0.08f;
        foreach (var r in renderers) if (r) r.enabled = visible;
    }
}
