using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// สิ่งรบกวนระหว่าง Bonus time: หมึกบังเงา / เงาส่าย / ตัวอักษรหลอก / เงาสั่น (อยู่แค่บนภาพสัตว์ ไม่บังช่องคำตอบ นาฬิกา และแป้นพิมพ์)
public class BonusDistraction : MonoBehaviour
{
    public BonusQuizManager quiz;
    public RectTransform target;     // ภาพเงาสัตว์
    public Sprite blob;              // รูปวงกลม (หมึก)
    public TMP_FontAsset font;
    float wait = 4f;
    bool running;
    int Round => Mathf.Max(1, GameSession.BonusRounds); // รอบหลังรบกวนถี่ขึ้น นานขึ้น

    void Update()
    {
        var gm = GameManager.Instance;
        if (!gm || !gm.IsPlaying || !target || quiz.Busy || running) return;
        if ((wait -= Time.deltaTime) > 0f) return;
        wait = Random.Range(6f, 9f);                                       // Bonus 30 วิ = รบกวนราว 3-4 ครั้ง
        StartCoroutine(Run(Random.Range(0, 4), 2f));
    }

    IEnumerator Run(int kind, float dur)
    {
        running = true;
        var made = new List<RectTransform>();
        Vector2 basePos = target.anchoredPosition, size = target.rect.size;
        if (kind == 0) // หมึกบังเงาบางส่วน
            for (int i = 0; i < 2 + Mathf.Min(Round, 3); i++)
            {
                var img = New<Image>("Ink", made);
                img.sprite = blob; img.color = new Color(0.06f, 0.05f, 0.1f, 0f); img.raycastTarget = false;
                float s = size.y * Random.Range(0.3f, 0.45f);
                img.rectTransform.sizeDelta = new Vector2(s * Random.Range(1f, 1.4f), s);
                img.rectTransform.anchoredPosition = new Vector2(Random.Range(-0.3f, 0.3f) * size.x, Random.Range(-0.3f, 0.3f) * size.y);
            }
        if (kind == 2) // ตัวอักษรหลอกลอยผ่านภาพ
            for (int i = 0; i < 4; i++)
            {
                var t = New<TextMeshProUGUI>("FakeLetter", made);
                t.font = font; t.fontSize = 110; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
                t.text = ((char)('A' + Random.Range(0, 26))).ToString();
                t.color = new Color(1f, 1f, 1f, 0.85f);
                t.rectTransform.sizeDelta = new Vector2(140, 140);
                t.rectTransform.anchoredPosition = new Vector2(Random.Range(-0.5f, 0.5f) * size.x, Random.Range(-0.35f, 0.35f) * size.y);
            }
        for (float t = 0f; t < dur && !quiz.Busy; t += Time.deltaTime)
        {
            float fade = Mathf.Clamp01(Mathf.Min(t, dur - t) / 0.25f);
            if (kind == 0) foreach (var m in made) { var c = m.GetComponent<Image>().color; c.a = 0.95f * fade; m.GetComponent<Image>().color = c; }
            if (kind == 1) target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 6f) * 16f);           // เงาส่าย
            if (kind == 2) for (int i = 0; i < made.Count; i++) made[i].anchoredPosition += Vector2.right * (i % 2 == 0 ? 160f : -160f) * Time.deltaTime;
            if (kind == 3) target.anchoredPosition = basePos + Random.insideUnitCircle * 14f;                      // เงาสั่น
            yield return null;
        }
        target.localRotation = Quaternion.identity;
        if (kind == 3) target.anchoredPosition = basePos;
        foreach (var m in made) if (m) Destroy(m.gameObject);
        running = false;
    }

    T New<T>(string name, List<RectTransform> list) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(target, false);
        list.Add((RectTransform)go.transform);
        return go.AddComponent<T>();
    }
}
