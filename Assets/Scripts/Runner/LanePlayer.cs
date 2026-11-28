using UnityEngine;
using UnityEngine.InputSystem;

// สไลม์ผู้เล่น 3 เลน: รับปุ่ม/การปัดจอ เปลี่ยนเลน กระโดด สไลด์ ชนแล้วเสียเลือด (โลกเลื่อนเข้าหา ผู้เล่นอยู่กับที่)
public class LanePlayer : MonoBehaviour
{
    public RunnerConfig config;
    public SlimeMotion motion;         // ท่าทาง + ฟิสิกส์กระโดด (ใช้ร่วมกับบอท)
    public BoxCollider hitBox;
    int lane;                          // -1 ซ้าย, 0 กลาง, 1 ขวา
    float invincibleTimer;
    Vector3 baseBoxSize, baseBoxCenter;
    Renderer[] renderers;

    void Awake() { baseBoxSize = hitBox.size; baseBoxCenter = hitBox.center; }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm && gm.IsPaused) return;
        if ((!gm || gm.State == GameState.RunnerPhase) && !TutorialGuide.Frozen) ReadInput(); // ช่วงสอนเล่นที่หยุดภาพ TutorialGuide สั่งท่าเอง
        var p = transform.position;
        p.x = Mathf.MoveTowards(p.x, lane * config.laneWidth, config.laneSwitchSpeed * Time.deltaTime); // เลื่อนไปเลนเป้าหมาย
        transform.position = p;
        motion.laneX = lane * config.laneWidth;
        float h = motion.Sliding ? 0.45f : 1f;                                                          // สไลด์ = กล่องชนเตี้ยลง
        hitBox.size = new Vector3(baseBoxSize.x, baseBoxSize.y * h, baseBoxSize.z);
        hitBox.center = new Vector3(baseBoxCenter.x, baseBoxCenter.y * h, baseBoxCenter.z);
        UpdateInvincibility();
    }

    void ReadInput() // คีย์บอร์ด A/D ←/→ Space/W/↑ S/↓/Shift หรือปัดจอ (มือถือ/ลากเมาส์)
    {
        var kb = Keyboard.current;
        bool k(Key a, Key b) => kb != null && (kb[a].wasPressedThisFrame || kb[b].wasPressedThisFrame);
        if (k(Key.A, Key.LeftArrow) || SwipeInput.Left) ChangeLane(-1);
        if (k(Key.D, Key.RightArrow) || SwipeInput.Right) ChangeLane(1);
        if (k(Key.Space, Key.W) || k(Key.UpArrow, Key.UpArrow) || SwipeInput.Up) Jump();
        if (k(Key.S, Key.DownArrow) || k(Key.LeftShift, Key.LeftShift) || SwipeInput.Down) Slide();
    }

    public void ChangeLane(int dir) => lane = Mathf.Clamp(lane + dir, -1, 1);

    public void Jump()
    {
        if (!motion.Jump()) return;
        AudioManager.Play(AudioManager.Sfx.Jump);
        MissionSystem.Add(MissionSystem.Kind.Jumps, 1);
    }

    public void Slide()
    {
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
        if (!obstacle || invincibleTimer > 0f) return;
        renderers = motion.visual.GetComponentsInChildren<Renderer>();   // โมเดลถูกสร้างตอน Start จึงดึงตอนชน
        invincibleTimer = config.invincibleTime;
        if (PowerUps.Instance && PowerUps.Instance.ConsumeShield()) { RunnerCamera.Shake(0.15f); return; }                     // เกราะรับแทน
        if (CharacterAbility.Instance && CharacterAbility.Instance.TryBlockHit()) { RunnerCamera.Shake(0.15f); return; }     // ความสามารถตัวละคร
        GameSession.ChaseHits++;                                         // ภารกิจไล่ทันบอทแบบไม่โดนชน
        gm.Damage(config.obstacleDamage);
        RunnerCamera.Shake(0.3f);
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
