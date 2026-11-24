using UnityEngine;

// คลาสแม่ของของในด่าน: เลื่อนเข้าหาผู้เล่น (แกน -Z) ตามความเร็วโลก ผ่านกล้องแล้วลบทิ้ง
public class ScrollingObject : MonoBehaviour
{
    public float despawnZ = -12f;

    protected virtual void Update()
    {
        transform.position += Vector3.back * WorldScroller.FrameDelta;
        if (transform.position.z < despawnZ) Destroy(gameObject);
    }
}
