using UnityEngine;

// กล้องหลังสไลม์ ตามเลนแบบนุ่มๆ มองไปข้างหน้า + สั่นตอนชน
public class RunnerCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 3.2f, -6.2f);
    public float lookAhead = 8f;          // มองไปข้างหน้ากี่เมตร
    [Range(0f, 1f)] public float followX = 0.7f;
    public float smooth = 8f;
    static float shakeTime, shakePower;

    public static void Shake(float power) { shakeTime = 0.25f; shakePower = power; }

    void LateUpdate()
    {
        if (!target) return;
        float x = target.position.x * followX;
        Vector3 desired = new Vector3(x + offset.x, offset.y + target.position.y * 0.3f, offset.z);
        Vector3 pos = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        if (shakeTime > 0f)
        {
            shakeTime -= Time.deltaTime;
            pos += Random.insideUnitSphere * shakePower;
        }
        transform.position = pos;
        transform.LookAt(new Vector3(x, 1f, lookAhead));
    }
}
