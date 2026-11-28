using UnityEngine;

// ท่าทางสไลม์ (ผู้เล่นและบอทใช้ร่วมกัน): ฟิสิกส์กระโดด/สไลด์ ยืดหดตอนวิ่ง เอียงตอนเปลี่ยนเลน หอบตอนหมดแรง ตัวโตหลังกิน
public class SlimeMotion : MonoBehaviour
{
    public RunnerConfig config;
    public Transform visual;              // โมเดลที่ยืด/หด (ไม่กระทบกล่องชน)
    public float laneX;                   // ตำแหน่งเลนเป้าหมาย ใช้คิดท่าเอียง
    public bool tired;                    // บอทหมดแรง = หยุดเด้ง หายใจหอบ
    public float growScale = 1.35f;       // ตัวโตสุดหลังกิน
    public bool Grounded { get; private set; } = true;
    public bool Sliding => slideTimer > 0f && Grounded;
    float velocityY, slideTimer, landSquash, growTimer;
    Vector3 baseScale;

    void Awake() => baseScale = visual.localScale;

    public bool Jump() // คืนค่า false ถ้ายังลอยอยู่
    {
        if (!Grounded) return false;
        velocityY = config.jumpVelocity;
        Grounded = false;
        slideTimer = 0f;
        return true;
    }

    public void Slide()
    {
        if (!Grounded) velocityY = -config.jumpVelocity * 1.5f;   // กลางอากาศ = ดิ่งลง
        slideTimer = config.slideDuration;
    }

    public void Squash() => landSquash = 1f;
    public void Grow(float seconds) { growTimer = seconds; landSquash = 1f; } // กินบอท = ตัวโตชั่วคราว (กล่องชนเท่าเดิม ไม่เสียเปรียบ)

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm && gm.IsPaused) return;
        float dt = Time.deltaTime;
        if (!Grounded)
        {
            var p = transform.position;
            velocityY -= config.gravity * dt;
            p.y += velocityY * dt;
            if (p.y <= 0f) { p.y = 0f; Grounded = true; velocityY = 0f; landSquash = 1f; } // ไม่ต่ำกว่าพื้น = ไม่ตกแมพ
            transform.position = p;
        }
        if (Sliding) slideTimer -= dt;
        if (growTimer > 0f) growTimer -= dt;
        Animate(dt);
    }

    void Animate(float dt) // ยืดหด + เอียง (เลื่อนค่าแบบนุ่มๆ)
    {
        Vector3 t;
        if (tired) { float b = Mathf.Sin(Time.time * 5f) * 0.08f; t = new Vector3(1f + b, 1f - b, 1f + b); }          // หอบ
        else if (Sliding) t = new Vector3(1.35f, 0.45f, 1.25f);
        else if (!Grounded) t = velocityY > 0 ? new Vector3(0.85f, 1.25f, 0.85f) : new Vector3(1.05f, 0.95f, 1.05f);
        else { float w = Mathf.Sin(Time.time * 14f) * 0.06f; t = new Vector3(1f - w, 1f + w, 1f - w); }              // วิ่ง
        if (landSquash > 0f) { t = Vector3.Lerp(t, new Vector3(1.35f, 0.65f, 1.35f), landSquash); landSquash = Mathf.Max(0f, landSquash - dt * 5f); }
        float grow = Mathf.Lerp(1f, growScale, Mathf.Clamp01(growTimer));                                           // วินาทีสุดท้ายค่อยๆ หดกลับ
        visual.localScale = Vector3.Lerp(visual.localScale, Vector3.Scale(baseScale, t) * grow, dt * 18f);
        float lean = (laneX - transform.position.x) * -8f;
        visual.localRotation = Quaternion.Lerp(visual.localRotation, Quaternion.Euler(0f, visual.localEulerAngles.y, lean), dt * 12f);
    }
}
