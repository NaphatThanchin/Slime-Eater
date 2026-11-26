using TMPro;
using UnityEngine;

// ชื่อผู้เล่นลอยเหนือหัวตัวละคร หันเข้ากล้องเสมอ
[RequireComponent(typeof(TMP_Text))]
public class NameTag : MonoBehaviour
{
    public Transform follow;  // ตัวละครที่ชื่อลอยอยู่บนหัว
    public float gap = 0.25f; // ระยะเหนือหัว
    public bool usePlayerName = true; // false = บอท (RivalBot ใส่ชื่อเอง)

    void Start() { if (usePlayerName) GetComponent<TMP_Text>().text = SaveData.PlayerName; }

    void LateUpdate()
    {
        if (Camera.main) transform.rotation = Camera.main.transform.rotation;
        if (!follow) return;
        float top = follow.position.y;
        foreach (var r in follow.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y); // จุดสูงสุดของโมเดล (สูงไม่เท่ากันแต่ละตัว)
        transform.position = new Vector3(follow.position.x, top + gap * transform.lossyScale.y, follow.position.z);
    }
}
