using System.Collections;
using UnityEngine;

// ความเร็วโลกที่เลื่อนเข้าหาผู้เล่น (ผู้เล่นอยู่กับที่) ยิ่งด่านสูงยิ่งเร็ว
public class WorldScroller : MonoBehaviour
{
    public static WorldScroller Instance { get; private set; }
    public float secondsToBoss = 90f; // เวลาวิ่งถึงบอส
    public float BaseSpeed => GameSession.TargetDistance / secondsToBoss * GameSession.SpeedMultiplier; // 11.1 m/s x ด่าน
    public float CurrentSpeed { get; private set; }
    public static float FrameDelta => Instance ? Instance.CurrentSpeed * Time.deltaTime : 0f; // ระยะเลื่อนต่อเฟรม

    void Awake()
    {
        Instance = this;
        CurrentSpeed = BaseSpeed;
    }

    public void StopSmoothly(float duration) // ชะลอจนหยุด
    {
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
