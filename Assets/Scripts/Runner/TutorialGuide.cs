using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// สอนเล่นรอบแรก: เกมช้าลง หยุดภาพก่อนถึงอุปสรรคแล้วบอกปุ่ม กดถูกจึงเล่นต่อ
public class TutorialGuide : MonoBehaviour
{
    public static TutorialGuide Instance { get; private set; }
    public static bool Active => Instance && Instance.on;
    public static bool Frozen => Active && Instance.frozen;
    public static bool ReadyForNext => Active && !Instance.frozen && !Instance.watch && Instance.step < 3; // บทก่อนจบแล้ว วางบทต่อไปได้
    public LanePlayer player;
    public GameObject hintBox;
    public TMP_Text hint;
    public float slow = 0.5f;              // ความเร็วเกมระหว่างสอน
    static readonly string[] Keys = { "กด Space หรือ W เพื่อกระโดดข้ามหิน", "กด S หรือปุ่มลูกศรลง เพื่อสไลด์ลอดท่อนไม้", "กด A หรือปุ่มลูกศรซ้าย เพื่อหลบไปเลนซ้าย", "เก็บหัวใจเพื่อเพิ่มเลือด" };
    static readonly string[] Swipes = { "ปัดขึ้น เพื่อกระโดดข้ามหิน", "ปัดลง เพื่อสไลด์ลอดท่อนไม้", "ปัดซ้าย เพื่อหลบไปเลนซ้าย", "เก็บหัวใจเพื่อเพิ่มเลือด" };
    static string[] Texts => SwipeInput.IsTouch ? Swipes : Keys;     // มือถือสอนแบบปัดจอ คอมสอนแบบกดปุ่ม
    static readonly float[] FreezeAt = { 0.3f, 0.3f, 0.45f }; // หยุดภาพเมื่ออุปสรรคอีกกี่วินาทีถึงตัว
    bool on, frozen;
    int step = -1;
    Transform watch;
    float endTimer = -1f;

    void Awake() { Instance = this; on = !SaveData.D.tutorialDone; }
    void Start() => hintBox.SetActive(false);

    public void Watch(Transform t, int s) // TrackSpawner แจ้งอุปสรรคของบทเรียนที่ s
    {
        watch = t;
        step = s;
        if (s == 3) Show(Texts[3]);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (!on || gm.IsPaused || gm.State != GameState.RunnerPhase) return;
        if (frozen && !Pressed()) { Time.timeScale = 0f; return; }  // รอกดปุ่มที่ถูก
        if (frozen) { frozen = false; hintBox.SetActive(false); }
        Time.timeScale = slow;
        if (step < 3 && watch && watch.position.z < WorldScroller.Instance.BaseSpeed * FreezeAt[step]) { frozen = true; watch = null; Show(Texts[step]); Time.timeScale = 0f; }
        if (step == 3 && !watch && endTimer < 0f) { endTimer = 4f; Show("ไล่กินสไลม์คู่แข่งข้างหน้า! มันวิ่งเร็วกว่าแต่เลือดจะหมดก่อน"); } // หัวใจถูกเก็บ/ผ่านไปแล้ว
        if (endTimer >= 0f && (endTimer -= Time.unscaledDeltaTime) < 0f) Finish();
    }

    bool Pressed() // กดปุ่มถูกแล้วทำท่านั้นให้เลย
    {
        var kb = Keyboard.current;
        bool k(Key a, Key b) => kb != null && (kb[a].wasPressedThisFrame || kb[b].wasPressedThisFrame);
        if (step == 0 && (k(Key.Space, Key.W) || k(Key.UpArrow, Key.UpArrow) || SwipeInput.Up)) player.Jump();
        else if (step == 1 && (k(Key.S, Key.DownArrow) || SwipeInput.Down)) player.Slide();
        else if (step == 2 && (k(Key.A, Key.LeftArrow) || SwipeInput.Left)) player.ChangeLane(-1);
        else return false;
        return true;
    }

    void Show(string text) { hint.text = text; hintBox.SetActive(true); }

    void Finish() // จบการสอน -> ความเร็วปกติ + จำไว้ว่าเคยสอนแล้ว
    {
        on = false;
        hintBox.SetActive(false);
        Time.timeScale = 1f;
        SaveData.D.tutorialDone = true;
        SaveData.Save();
    }
}
