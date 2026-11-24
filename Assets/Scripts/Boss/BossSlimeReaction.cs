using UnityEngine;

// อารมณ์สไลม์ตัวใหญ่ในฉากบอส: ปกติ / ตกใจ (เหงื่อ + ?) / ดีใจ
public class BossSlimeReaction : MonoBehaviour
{
    public Transform slimeVisual;
    public GameObject nervousIcons; // เหงื่อ + ?
    Vector3 baseScale, basePos;
    float nervousTimer, happyTimer;
    bool lowTime;

    void Awake()
    {
        if (slimeVisual) { baseScale = slimeVisual.localScale; basePos = slimeVisual.localPosition; }
        if (nervousIcons) nervousIcons.SetActive(false);
    }

    public void Nervous(float seconds = 1.5f) { nervousTimer = Mathf.Max(nervousTimer, seconds); }
    public void Happy() { happyTimer = 0.8f; nervousTimer = 0f; }
    public void SetLowTime(bool low) { lowTime = low; }

    void Update()
    {
        if (nervousTimer > 0f) nervousTimer -= Time.deltaTime;
        if (happyTimer > 0f) happyTimer -= Time.deltaTime;
        bool nervous = nervousTimer > 0f || lowTime;
        if (nervousIcons && nervousIcons.activeSelf != nervous) nervousIcons.SetActive(nervous);
        if (!slimeVisual) return;
        Vector3 pos = basePos;
        Vector3 scale;
        if (happyTimer > 0f) // กระโดดดีใจ
        {
            float k = Mathf.Abs(Mathf.Sin(happyTimer * Mathf.PI * 2.5f));
            pos += Vector3.up * k * 0.6f;
            scale = new Vector3(1f - k * 0.1f, 1f + k * 0.15f, 1f - k * 0.1f);
        }
        else if (nervous) // สั่น
        {
            pos += Vector3.right * Mathf.Sin(Time.time * 40f) * 0.04f;
            scale = new Vector3(1.02f, 0.98f, 1.02f);
        }
        else // หายใจเบาๆ
        {
            float b = Mathf.Sin(Time.time * 2.5f) * 0.04f;
            scale = new Vector3(1f + b, 1f - b, 1f + b);
        }
        slimeVisual.localPosition = pos;
        slimeVisual.localScale = Vector3.Scale(baseScale, scale);
    }
}
