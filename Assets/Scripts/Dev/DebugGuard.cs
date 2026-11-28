using UnityEngine;
using UnityEngine.SceneManagement;

// ระบบกันบัค: เช็กอ้างอิง, ผู้เล่นตกแมพ, เวลาเกมค้าง, เปลี่ยนฉากค้าง
public class DebugGuard : MonoBehaviour
{
    public Transform player;
    public float fallLimitY = -5f;       // ต่ำกว่านี้ = ตกแมพ
    public Vector3 safePosition;         // จุดย้ายกลับ
    public float transitionTimeout = 8f; // เวลาสูงสุดตอนเปลี่ยนฉาก
    float transitionTimer;

    void Start()
    {
        if (player) safePosition = player.position;
        CheckReferences();
    }

    void CheckReferences()
    {
        var gm = GameManager.Instance;
        if (!gm) { Debug.LogError("[DebugGuard] ไม่พบ GameManager ในฉาก!"); return; }
        if (!gm.ui) Debug.LogError("[DebugGuard] GameManager.ui ยังไม่ได้ใส่");
        if (gm.startState == GameState.RunnerPhase && !gm.scroller) Debug.LogError("[DebugGuard] RunnerScene ไม่มี WorldScroller");
        if (gm.startState == GameState.BonusPhase && !gm.quiz) Debug.LogError("[DebugGuard] BonusTimeScene ไม่มี BonusQuizManager");
        if (!Camera.main) Debug.LogError("[DebugGuard] ไม่มี Main Camera");
        Debug.Log("[DebugGuard] ตรวจสอบฉาก " + SceneManager.GetActiveScene().name + " เรียบร้อย");
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (!gm) return;
        if (player && player.position.y < fallLimitY)
        {
            Debug.LogWarning("[DebugGuard] ผู้เล่นตกแมพที่ y=" + player.position.y.ToString("0.0") + " -> ย้ายกลับ");
            player.position = safePosition;
        }
        if (Time.timeScale == 0f && !gm.IsPaused && !TutorialGuide.Active)
        {
            Debug.LogWarning("[DebugGuard] Time.timeScale ค้างที่ 0 -> คืนค่าเป็น 1");
            Time.timeScale = 1f;
        }
        if (gm.State == GameState.TransitionToBonus)
        {
            transitionTimer += Time.unscaledDeltaTime;
            if (transitionTimer > transitionTimeout)
            {
                Debug.LogError("[DebugGuard] เปลี่ยนฉากไป Bonus time ค้าง -> บังคับโหลด " + gm.bonusScene);
                transitionTimer = -999f;
                SceneManager.LoadScene(gm.bonusScene);
            }
        }
    }
}
