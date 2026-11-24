using System.Text;
using TMPro;
using UnityEngine;

// ยกวรรณยุกต์ไทยที่อยู่บนสระบนขึ้น (TMP จะวางซ้อนกันจนมองไม่เห็น)
[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class ThaiTextFixer : MonoBehaviour
{
    static readonly ThaiPreprocessor Shared = new ThaiPreprocessor();

    void OnEnable()
    {
        var t = GetComponent<TMP_Text>();
        t.textPreprocessor = Shared;
        t.havePropertiesChanged = true;
        t.SetAllDirty();
    }

    class ThaiPreprocessor : ITextPreprocessor
    {
        const string Lift = "<voffset=0.28em>";

        public string PreprocessText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            StringBuilder sb = null;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (IsToneMark(c) && i > 0 && IsUpperVowel(text[i - 1])) // วรรณยุกต์ตามหลังสระบน -> ยกขึ้น
                {
                    if (sb == null) sb = new StringBuilder(text, 0, i, text.Length + 32);
                    sb.Append(Lift).Append(c).Append("</voffset>");
                }
                else sb?.Append(c);
            }
            return sb == null ? text : sb.ToString();
        }

        static bool IsToneMark(char c) => c >= '่' && c <= '์';  // ่ ้ ๊ ๋ ์
        static bool IsUpperVowel(char c) => c == 'ั' || (c >= 'ิ' && c <= 'ื') || c == '็' || c == 'ํ';
    }
}
