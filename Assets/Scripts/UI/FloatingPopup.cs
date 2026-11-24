using TMPro;
using UnityEngine;

// ตัวหนังสือลอยขึ้นแล้วจาง เช่น "-" "+10s" "+1"
public class FloatingPopup : MonoBehaviour
{
    public float lifetime = 0.9f;
    public float riseSpeed = 90f;
    TMP_Text label;
    float age;
    Vector2 drift;

    public void Init(string text, Color color)
    {
        label = GetComponent<TMP_Text>();
        label.text = text;
        label.color = color;
        drift = new Vector2(Random.Range(-30f, 30f), riseSpeed);
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        ((RectTransform)transform).anchoredPosition += drift * Time.unscaledDeltaTime;
        if (label) { var c = label.color; c.a = 1f - age / lifetime; label.color = c; }
        if (age >= lifetime) Destroy(gameObject);
    }
}
