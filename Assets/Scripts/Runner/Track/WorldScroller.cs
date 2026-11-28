using System.Collections;
using UnityEngine;

// ความเร็วโลกที่เลื่อนเข้าหาผู้เล่น (ผู้เล่นอยู่กับที่) ยิ่งด่านสูงยิ่งเร็ว
public class WorldScroller : MonoBehaviour
{
    public static WorldScroller Instance { get; private set; }
    public float startSpeed = 11.11f; // ความเร็วเริ่มด่าน 1 (m/s)
    public float BaseSpeed => Mathf.Min(GameSession.MaxSpeed, startSpeed * GameSession.SpeedMultiplier); // x ด่าน x เวลาในด่าน x ตัวละคร
    public float CurrentSpeed { get; private set; }
    public static float FrameDelta => Instance ? Instance.CurrentSpeed * Time.deltaTime : 0f; // ระยะเลื่อนต่อเฟรม
    bool stopping;

    void Awake()
    {
        Instance = this;
        CurrentSpeed = BaseSpeed;
    }

    void Update() { if (!stopping) CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, BaseSpeed, 3f * Time.deltaTime); } // เร่งทีละนิด ไม่กระตุกตอน +5%

    public void StopSmoothly(float duration) // ชะลอจนหยุด
    {
        stopping = true;
        StopAllCoroutines();
        StartCoroutine(Decelerate(duration));
    }

    IEnumerator Decelerate(float duration)
    {
        float start = CurrentSpeed;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            CurrentSpeed = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        CurrentSpeed = 0f;
    }
}
