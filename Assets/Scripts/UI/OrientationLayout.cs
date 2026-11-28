using UnityEngine;
using UnityEngine.UI;

// จัดหน้าจอตามแนวจอ: แนวนอน = แบบคอมเดิม, แนวตั้ง (มือถือ) = UI ใหญ่ขึ้น + ย้ายบางชิ้นไม่ให้ทับกัน
[RequireComponent(typeof(CanvasScaler))]
public class OrientationLayout : MonoBehaviour
{
    [System.Serializable]
    public struct Move
    {
        public RectTransform target;
        public bool reanchor;              // true = เปลี่ยนจุดยึดตอนแนวตั้ง (เช่น จากกลางจอเป็นขอบบน)
        public Vector2 anchor, pos;        // จุดยึด + ตำแหน่งตอนแนวตั้ง
        public float scale;                // 0 = ขนาดเดิม
    }
    public Move[] moves = new Move[0];
    public GameObject[] touchOnly = new GameObject[0];   // โชว์เฉพาะมือถือ (แป้นพิมพ์บนจอ)
    public GameObject[] desktopOnly = new GameObject[0]; // โชว์เฉพาะคอม (ข้อความบอกปุ่มคีย์บอร์ด)
    public GameObject rotatePrompt;          // มือถือถือแนวนอน -> บอกให้หมุนจอ
    public Transform slime3D;                // สไลม์ 3D หน้ากล้อง (ฉากบอส)
    public Vector2 slimeUnits = new Vector2(230f, 760f); // ตำแหน่งสไลม์ตอนแนวตั้ง (หน่วย UI จากมุมซ้ายล่าง เหนือช่องคำตอบ)
    public float portraitWidth = 1300f;      // แนวตั้งอ้างอิงกว้าง 1300 แทน 1920 -> UI/ตัวหนังสือใหญ่ขึ้น ~1.5 เท่า
    public Camera fixedCamera;               // กล้องที่อยู่นิ่ง (หน้าเมนู) ถอยกล้องตอนแนวตั้ง
    public Vector3 portraitCamPos;
    public float portraitFov = 60f;
    Vector3 camLand;
    float fovLand;
    CanvasScaler scaler;
    Vector2[] landPos, landMin, landMax;
    Vector3[] landScale;
    Vector3 slimeLand, slimeScale;
    int state = -1;                          // 0 = แนวนอน, 1 = แนวตั้ง

    public static bool Portrait => Screen.height > Screen.width;

    void Awake()
    {
        scaler = GetComponent<CanvasScaler>();
        int n = moves.Length;
        landPos = new Vector2[n]; landMin = new Vector2[n]; landMax = new Vector2[n]; landScale = new Vector3[n];
        for (int i = 0; i < n; i++) { var t = moves[i].target; landPos[i] = t.anchoredPosition; landMin[i] = t.anchorMin; landMax[i] = t.anchorMax; landScale[i] = t.localScale; }
        if (slime3D) { slimeLand = slime3D.localPosition; slimeScale = slime3D.localScale; }
        if (fixedCamera) { camLand = fixedCamera.transform.position; fovLand = fixedCamera.fieldOfView; }
        foreach (var g in touchOnly) g.SetActive(SwipeInput.IsTouch);
        foreach (var g in desktopOnly) g.SetActive(!SwipeInput.IsTouch);
    }

    void Update() // เช็กทุกเฟรม ผู้เล่นหมุนจอได้ตลอด
    {
        int s = Portrait ? 1 : 0;
        if (rotatePrompt) rotatePrompt.SetActive(SwipeInput.IsTouch && Screen.width > Screen.height * 1.2f);
        if (s == state) return;
        state = s;
        scaler.referenceResolution = new Vector2(s == 1 ? portraitWidth : 1920f, 1080f);
        scaler.matchWidthOrHeight = s == 1 ? 0f : 0.5f;           // แนวตั้งยึดความกว้าง UI ไม่ล้นข้างจอ
        for (int i = 0; i < moves.Length; i++)
        {
            var m = moves[i];
            bool p = s == 1;
            m.target.anchorMin = p && m.reanchor ? m.anchor : landMin[i];
            m.target.anchorMax = p && m.reanchor ? m.anchor : landMax[i];
            m.target.anchoredPosition = p ? m.pos : landPos[i];
            m.target.localScale = p && m.scale > 0f ? Vector3.one * m.scale : landScale[i];
        }
        if (fixedCamera) { fixedCamera.transform.position = s == 1 ? portraitCamPos : camLand; fixedCamera.fieldOfView = s == 1 ? portraitFov : fovLand; } // จอแคบ = ถอยกล้อง สไลม์ไม่ล้นจอ
        if (!slime3D) return;
        var cam = Camera.main;
        float canvasH = portraitWidth * Screen.height / Screen.width;                         // ความสูงจอในหน่วย UI
        var vp = new Vector3(slimeUnits.x / portraitWidth, slimeUnits.y / canvasH, slimeLand.z);
        slime3D.localPosition = s == 1 ? cam.transform.InverseTransformPoint(cam.ViewportToWorldPoint(vp)) : slimeLand;
        slime3D.localScale = s == 1 ? slimeScale * Mathf.Clamp(cam.aspect * 0.65f, 0.3f, 1f) : slimeScale; // จอแคบ = สไลม์เล็กลง ไม่บังคำตอบ/แป้นพิมพ์
    }
}
