using UnityEngine;
using UnityEngine.InputSystem;

// สไลม์วิ่ง 3 เลน: เปลี่ยนเลน กระโดด สไลด์ ชนแล้วเสียเลือด (วิ่งไปข้างหน้าเองไม่ได้ โลกเลื่อนเข้าหาแทน)
public class LanePlayer : MonoBehaviour
{
    public RunnerConfig config;
    public Transform visual;          // โมเดลสไลม์ ใช้ทำอนิเมชัน
    public BoxCollider hitBox;
    int lane;                         // -1 ซ้าย, 0 กลาง, 1 ขวา
    float velocityY;
    bool grounded = true;
    float slideTimer;
    float invincibleTimer;
    float landSquash;
    Vector3 baseVisualScale, baseBoxSize, baseBoxCenter;
    Renderer[] renderers;

    void Awake()
    {
        baseVisualScale = visual ? visual.localScale : Vector3.one;
        if (hitBox) { baseBoxSize = hitBox.size; baseBoxCenter = hitBox.center; }
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.IsPaused) return;
        bool canControl = gm == null || gm.State == GameState.RunnerPhase;
        if (canControl) ReadInput();
        Move();
        UpdateCollider();
        Animate();
        UpdateInvincibility();
    }

    void ReadInput() // A/D ←/→ เปลี่ยนเลน, Space/W/↑ กระโดด, S/↓/Shift สไลด์
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) lane = Mathf.Max(-1, lane - 1);
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) lane = Mathf.Min(1, lane + 1);
        if ((kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) && grounded)
        {
            velocityY = config.jumpVelocity;
            grounded = false;
            slideTimer = 0f;
            MissionSystem.Add(MissionSystem.Kind.Jumps, 1);
        }
        if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame)
        {
            if (!grounded) velocityY = -config.jumpVelocity * 1.5f; // กลางอากาศ = ดิ่งลง
            slideTimer = config.slideDuration;
            MissionSystem.Add(MissionSystem.Kind.Slides, 1);
        }
    }

    void Move() // เลื่อนไปเลนเป้าหมาย + ฟิสิกส์กระโดด (ไม่ต่ำกว่าพื้น กันตกแมพ)
    {
        Vector3 p = transform.position;
        p.x = Mathf.MoveTowards(p.x, lane * config.laneWidth, config.laneSwitchSpeed * Time.deltaTime);
        if (!grounded)
        {
            velocityY -= config.gravity * Time.deltaTime;
            p.y += velocityY * Time.deltaTime;
            if (p.y <= 0f) { p.y = 0f; grounded = true; velocityY = 0f; landSquash = 1f; }
        }
        if (slideTimer > 0f && grounded) slideTimer -= Time.deltaTime;
        transform.position = p;
    }

    bool Sliding => slideTimer > 0f && grounded;

    void UpdateCollider() // สไลด์แล้ว collider เตี้ยลงครึ่งหนึ่ง
    {
        if (!hitBox) return;
        float h = Sliding ? 0.45f : 1f;
        hitBox.size = new Vector3(baseBoxSize.x, baseBoxSize.y * h, baseBoxSize.z);
        hitBox.center = new Vector3(baseBoxCenter.x, baseBoxCenter.y * h, baseBoxCenter.z);
    }

    void Animate() // squash & stretch + เอียงตัวตอนเปลี่ยนเลน
    {
        if (!visual) return;
        Vector3 target;
        if (Sliding) target = new Vector3(1.35f, 0.45f, 1.25f);
        else if (!grounded) target = velocityY > 0 ? new Vector3(0.85f, 1.25f, 0.85f) : new Vector3(1.05f, 0.95f, 1.05f);
        else { float w = Mathf.Sin(Time.time * 14f) * 0.06f; target = new Vector3(1f - w, 1f + w, 1f - w); }
        if (landSquash > 0f)
        {
            target = Vector3.Lerp(target, new Vector3(1.35f, 0.65f, 1.35f), landSquash);
            landSquash = Mathf.Max(0f, landSquash - Time.deltaTime * 5f);
        }
        visual.localScale = Vector3.Lerp(visual.localScale, Vector3.Scale(baseVisualScale, target), Time.deltaTime * 18f);
        float lean = (lane * config.laneWidth - transform.position.x) * -8f;
        visual.localRotation = Quaternion.Lerp(visual.localRotation, Quaternion.Euler(0f, 0f, lean), Time.deltaTime * 12f);
    }

    void OnTriggerEnter(Collider other) // ชนสิ่งกีดขวาง / เก็บไอเทม
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return;
        var item = other.GetComponentInParent<Collectible>();
        if (item) { item.Collect(gm); return; }
        var obstacle = other.GetComponentInParent<Obstacle>();
        if (!obstacle || invincibleTimer > 0f) return;
        renderers = visual.GetComponentsInChildren<Renderer>();   // โมเดลตัวละครถูกสร้างตอน Start จึงดึงใหม่ตอนชน
        if (PowerUps.Instance && PowerUps.Instance.ConsumeShield()) { invincibleTimer = config.invincibleTime; RunnerCamera.Shake(0.15f); return; } // เกราะรับแทน
        if (CharacterAbility.Instance && CharacterAbility.Instance.TryBlockHit()) { invincibleTimer = config.invincibleTime; RunnerCamera.Shake(0.15f); return; } // ความสามารถตัวละคร
        invincibleTimer = config.invincibleTime;
        Debug.Log("[Player] ชน " + obstacle.kind + " -" + config.obstacleDamage + " HP");
        gm.Damage(config.obstacleDamage);
        RunnerCamera.Shake(0.3f);
        landSquash = 1f;
    }

    void UpdateInvincibility() // ตัวกระพริบตอนอมตะ
    {
        if (invincibleTimer <= 0f || renderers == null) return;
        invincibleTimer -= Time.deltaTime;
        bool visible = invincibleTimer <= 0f || Mathf.Repeat(invincibleTimer, 0.16f) > 0.08f;
        foreach (var r in renderers) if (r) r.enabled = visible;
    }
}
