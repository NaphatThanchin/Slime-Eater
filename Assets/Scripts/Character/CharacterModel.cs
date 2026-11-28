using UnityEngine;

// โชว์โมเดลตัวละครที่เลือก (เมนู / ฉากวิ่ง / ฉาก Bonus time / หน้าดูตัวละครในร้าน)
public class CharacterModel : MonoBehaviour
{
    public GameObject[] prefabs;      // Char_SlimeGreen, Char_SlimeKing, ... เรียงตาม CharacterData
    public bool useSelected = true;   // false = หน้าพรีวิวในร้าน (เลือกเองด้วย Show)
    public float spin;                // หมุนโชว์ (องศา/วินาที)
    int shown = -1;

    void Start() { if (useSelected) Show(CharacterData.Current); }
    void Update() { if (spin != 0f) transform.Rotate(0f, spin * Time.unscaledDeltaTime, 0f); }

    public void Show(int i)
    {
        if (i == shown && transform.childCount > 0) return;
        for (int c = transform.childCount - 1; c >= 0; c--)
        {
            var old = transform.GetChild(c).gameObject;
            if (Application.isPlaying) { old.SetActive(false); Destroy(old); } else DestroyImmediate(old);
        }
        if (prefabs == null || i < 0 || i >= prefabs.Length || !prefabs[i]) return;
        var m = Instantiate(prefabs[i], transform);
        m.name = prefabs[i].name;
        shown = i;
    }
}
