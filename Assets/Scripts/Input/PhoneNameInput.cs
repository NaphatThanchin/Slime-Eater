using TMPro;
using UnityEngine;

// มือถือ (เว็บ): ช่องตั้งชื่อรับจากแป้นพิมพ์ของเครื่อง (ช่องพิมพ์ของ Unity เปิดแป้นบนเว็บมือถือไม่ได้) กด "ไป" = เข้าเกมทันที
public class PhoneNameInput : MonoBehaviour
{
    public TMP_InputField field;
    public MenuManager menu;
    bool open;

    void Update()
    {
        if (!PhoneKeyboard.Phone) return;
        bool want = field.gameObject.activeInHierarchy;                 // หน้าตั้งชื่อเปิดอยู่
        if (want != open) { open = want; field.readOnly = want; PhoneKeyboard.Enable(want); } // แตะจอ = แป้นขึ้น
        string s = PhoneKeyboard.Read();
        if (string.IsNullOrEmpty(s)) return;
        string t = field.text;
        bool submit = false;
        foreach (char c in s)
        {
            if (c == '\n') { submit = true; break; }
            t = c == '\b' ? (t.Length > 0 ? t.Substring(0, t.Length - 1) : t) : t + c;
        }
        int max = field.characterLimit;
        field.text = max > 0 && t.Length > max ? t.Substring(0, max) : t;
        if (submit && menu) menu.ConfirmName();
    }

    void OnDisable() { if (open) { open = false; PhoneKeyboard.Enable(false); } }
}
