using System.Runtime.InteropServices;
using UnityEngine;

// มือถือเล่น (เว็บ): ถึงฉาก Bonus time เปิดแป้นพิมพ์ของเครื่องเอง แล้วส่งตัวอักษรให้ BonusQuizManager
public class PhoneKeyboard : MonoBehaviour
{
    public BonusQuizManager quiz;
    public GameObject tapHint;                          // "แตะจอเพื่อพิมพ์" ใต้ช่องคำตอบ 
    public static bool Active { get; private set; }     // true = รับจากแป้นมือถือ (ปิดรับคีย์บอร์ดปกติ กันพิมพ์ซ้ำ)
    bool open;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void PhoneKb_Enable(int on);
    [DllImport("__Internal")] static extern string PhoneKb_Read();
    public static bool Phone => SwipeInput.IsTouch;     // มือถือบนเว็บ
#else
    static void PhoneKb_Enable(int on) { }
    static string PhoneKb_Read() => null;
    public static bool Phone => false;
#endif
    public static void Enable(bool on) => PhoneKb_Enable(on ? 1 : 0);  // ใช้กับช่องตั้งชื่อในเมนูด้วย
    public static string Read() => PhoneKb_Read();

    void Start()
    {
        Active = Phone;
        if (tapHint) tapHint.SetActive(false);
    }

    void Update()
    {
        if (!Active) return;
        var gm = GameManager.Instance;
        bool want = !gm || gm.State == GameState.BonusPhase;    // จบ Bonus time = เก็บแป้นพิมพ์
        if (want != open) { open = want; PhoneKb_Enable(want ? 1 : 0); if (tapHint) tapHint.SetActive(want); }
        string s = PhoneKb_Read();
        if (string.IsNullOrEmpty(s)) return;
        foreach (char c in s) if (c == '\b') quiz.Backspace(); else quiz.TypeChar(c);
    }

    void OnDestroy() { if (open) PhoneKb_Enable(0); Active = false; }
}
