using UnityEngine;

// กล้องหลังสไลม์ ตามเลนแบบนุ่มๆ มองไปข้างหน้า + สั่นตอนชน (มือถือแนวตั้งถอยกล้องให้เห็นทางเท่าคอม)
public class RunnerCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 3.2f, -6.2f);
    public Vector3 portraitOffset = new Vector3(0f, 3.4f, -6.2f);  // แนวตั้ง: มุมกว้างขึ้น (FOV 75) เห็นครบ 3 เลน
    public float lookAhead = 8f;          // มองไปข้างหน้ากี่เมตร
    [Range(0f, 1f)] public float followX = 0.7f;
    public float smooth = 8f;
    static float shakeTime, shakePower;
    Camera cam;

    public static void Shake(float power) { shakeTime = 0.25f; shakePower = power; }

    void Awake() => cam = GetComponent<Camera>();

    void LateUpdate()
    {
        if (!target) return;
        bool portrait = cam.aspect < 1f;
        cam.fieldOfView = portrait ? 75f : 60f;
        float x = target.position.x * (portrait ? 0.35f : followX);               // จอแคบ: ตามเลนน้อยลง ให้เห็นทุกเลนตลอด
        var off = portrait ? portraitOffset : offset;
        Vector3 pos = Vector3.Lerp(transform.position, new Vector3(x + off.x, off.y + target.position.y * 0.3f, off.z), 1f - Mathf.Exp(-smooth * Time.deltaTime));
        if (shakeTime > 0f) { shakeTime -= Time.deltaTime; pos += Random.insideUnitSphere * shakePower; }
        transform.position = pos;
        transform.LookAt(new Vector3(x, portrait ? 2.2f : 1f, portrait ? lookAhead + 14f : lookAhead)); // แนวตั้งเงยมองไกล: สไลม์อยู่ล่างจอ HUD อยู่บนท้องฟ้า
    }
}
