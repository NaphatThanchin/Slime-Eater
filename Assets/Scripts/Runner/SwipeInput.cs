using UnityEngine;
using UnityEngine.InputSystem;

// ปัดจอ (นิ้วบนมือถือ / ลากเมาส์บนคอม): ปัดเกินระยะแล้วนับทันทีไม่ต้องรอปล่อยนิ้ว 1 ครั้งต่อการแตะ
[DefaultExecutionOrder(-50)] // อ่านการปัดก่อนสคริปต์อื่นใช้ค่า
public class SwipeInput : MonoBehaviour
{
    public static bool Up, Down, Left, Right;          // true เฉพาะเฟรมที่ปัด
    public static bool IsTouch => Application.isMobilePlatform || Touchscreen.current != null; // อุปกรณ์จอสัมผัส
    public float threshold = 0.05f;                    // ระยะปัดขั้นต่ำ = 5% ของความสูงจอ (เท่ากันทุกขนาดจอ)
    Vector2 start;
    bool tracking;

    void Update()
    {
        Up = Down = Left = Right = false;
        var p = Pointer.current;
        if (p == null) return;
        if (p.press.wasPressedThisFrame) { start = p.position.ReadValue(); tracking = true; }
        if (!p.press.isPressed) { tracking = false; return; }
        if (!tracking) return;
        Vector2 d = p.position.ReadValue() - start;
        if (d.magnitude < threshold * Screen.height) return;
        tracking = false;                              // 1 การแตะ = 1 ท่า
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) { Right = d.x > 0f; Left = !Right; }
        else { Up = d.y > 0f; Down = !Up; }
    }
}
