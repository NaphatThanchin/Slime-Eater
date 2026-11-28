using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ช่องตัวอักษร 1 ช่อง (ตัวอักษร + ขีดเส้นใต้)
public class LetterSlotView : MonoBehaviour
{
    public TMP_Text letter;
    public Image underline;
    static readonly Color TypedColor = new Color(0.17f, 0.1f, 0.08f);
    static readonly Color HintColor = new Color(1f, 0.5f, 0.05f);  // ตัวใบ้ = สีส้ม
    static readonly Color LineColor = new Color(0.45f, 0.25f, 0.12f);
    static readonly Color HintLine = new Color(0.3f, 0.7f, 0.2f);

    public void Set(char c, bool isHint)
    {
        letter.text = c == '\0' ? "" : c.ToString();
        letter.color = isHint ? HintColor : TypedColor;
        underline.color = isHint ? HintLine : LineColor;
    }
}
