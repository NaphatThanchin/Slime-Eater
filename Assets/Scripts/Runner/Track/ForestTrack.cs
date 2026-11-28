using UnityEngine;

// พื้นป่าแบบต่อกันไปเรื่อยๆ: แผ่นพื้นเลื่อนเข้าหาผู้เล่น ผ่านกล้องแล้ววนไปต่อด้านหน้า พร้อมสุ่มต้นไม้/หญ้า/หินใหม่
public class ForestTrack : MonoBehaviour
{
    public Transform[] segments;
    public float segmentLength = 20f;
    public float pathHalfWidth = 4.2f;     // ครึ่งความกว้างทางเดิน 
    public GameObject[] trees;             // ต้นไม้
    public GameObject[] plants;            // หญ้า ดอกไม้ พุ่มไม้
    public GameObject[] rocks;             // ก้อนหินข้างทาง
    public GameObject[] edgePlants;        // หญ้าริมทางเดิน
    public GameObject[] canopyTrees;       // ต้นไม้ใหญ่ใบส้ม/เขียว โน้มเหนือทาง
    public int treesPerSide = 5;
    public int plantsPerSide = 10;
    public int rocksPerSide = 2;
    public int edgePlantsPerSide = 30;
    public int canopyPerSide = 1;

    void Update()
    {
        float d = WorldScroller.FrameDelta;
        if (d == 0f) return;
        foreach (var s in segments)
        {
            s.position += Vector3.back * d;
            if (s.position.z < -segmentLength * 1.5f) // หลุดหลังกล้องไปต่อท้ายด้านหน้า
            {
                s.position += Vector3.forward * segmentLength * segments.Length;
                Decorate(s, Random.Range(0, 99999));
            }
        }
    }

    public void Decorate(Transform seg, int seed) // สุ่มของประดับใหม่ในแผ่นนี้
    {
        var rnd = new System.Random(seed);
        var old = seg.Find("Tree");                                       // กลุ่มของประดับเดิม (ต้นไม้/หญ้า/หิน)
        if (old) { if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject); }
        var decor = new GameObject("Tree").transform;
        decor.SetParent(seg, false);
        string tag = "(group" + System.Array.IndexOf(segments, seg) + ")"; // เช่น CommonTree_1(group0) ตามเลข Scene_Group
        for (int side = -1; side <= 1; side += 2)
        {
            Place(trees, decor, rnd, side, pathHalfWidth + 2.5f, pathHalfWidth + 24f, treesPerSide, 0.7f, 1.2f, tag);
            Place(plants, decor, rnd, side, pathHalfWidth + 0.2f, pathHalfWidth + 9f, plantsPerSide, 0.6f, 1.2f, tag);
            Place(rocks, decor, rnd, side, pathHalfWidth + 1f, pathHalfWidth + 12f, rocksPerSide, 0.4f, 0.9f, tag);
            Place(edgePlants, decor, rnd, side, pathHalfWidth - 0.3f, pathHalfWidth + 3f, edgePlantsPerSide, 0.7f, 1.4f, tag);
            Place(canopyTrees, decor, rnd, side, pathHalfWidth + 4f, pathHalfWidth + 14f, canopyPerSide, 0.4f, 0.6f, tag);
        }
    }

    void Place(GameObject[] set, Transform parent, System.Random rnd, int side, float minX, float maxX, int count, float minS, float maxS, string tag)
    {
        if (set == null || set.Length == 0) return;
        for (int i = 0; i < count; i++)
        {
            var prefab = set[rnd.Next(set.Length)];
            if (!prefab) continue;
            var go = Instantiate(prefab, parent);
            go.name = prefab.name + tag;                                  
            float x = side * Mathf.Lerp(minX, maxX, (float)rnd.NextDouble());
            float z = ((float)rnd.NextDouble() - 0.5f) * segmentLength;
            go.transform.localPosition = new Vector3(x, 0f, z);
            go.transform.localRotation = Quaternion.Euler(0f, rnd.Next(360), 0f);
            go.transform.localScale = Vector3.one * Mathf.Lerp(minS, maxS, (float)rnd.NextDouble());
        }
    }

    [ContextMenu("Redecorate All")] // คลิกขวาสุ่มป่าใหม่ใน Edit Mode
    public void DecorateAll()
    {
        for (int i = 0; i < segments.Length; i++) Decorate(segments[i], i * 7919 + 13);
    }
}
