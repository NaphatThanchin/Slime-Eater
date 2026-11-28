using UnityEngine;

// คลาสแม่ของของในด่าน: เลื่อนเข้าหาผู้เล่นตามความเร็วโลกผ่านกล้องแล้วจะลบทิ้ง
public class ScrollingObject : MonoBehaviour
{
    public float despawnZ = -12f;

    protected virtual void Update()
    {
        transform.position += Vector3.back * WorldScroller.FrameDelta;
        if (transform.position.z < despawnZ) Despawn();
    }

    protected virtual void Despawn() => Destroy(gameObject);
}
