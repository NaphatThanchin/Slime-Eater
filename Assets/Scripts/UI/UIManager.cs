using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI ของฉาก: หลอดระยะทาง/เลือด, ตัวหนังสือลอย, หน้าตั้งค่า, หน้าสรุปผล
public class UIManager : MonoBehaviour
{
    public GameObject hudRoot;
    public Image progressFill;
    public TMP_Text progressText;
    public Image hpFill;
    public Vector2 progressFillRange = new Vector2(0f, 1f); // ช่วงแถบจริงในรูป (หัวรูปโปร่งใส)
    public Vector2 hpFillRange = new Vector2(0f, 1f);
    public RectTransform hpPopupAnchor;
    public TMP_Text scoreText;           // จำนวนเหรียญ
    public TMP_Text scoreValueText;      // คะแนนรวม (กลางบน)
    public TMP_Text stageText;           // ด่านที่
    public TMP_Text leafText;            // ใบไม้วิเศษที่มี
    public Image[] powerUpFills;         // วงเวลาไอเทม แม่เหล็ก/เกราะ/x2
    public TMP_Text toast;               // ข้อความเด้ง เช่น ภารกิจสำเร็จ
    public GameObject rivalRoot;         // แถบบอทคู่แข่ง (มีแค่ฉากวิ่ง)
    public TMP_Text rivalName, rivalGap;
    public Image rivalHp;
    public FloatingPopup popupTemplate;
    public GameObject settingsPanel;
    public GameObject resultRoot;
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public TMP_Text[] distanceTexts;     // [0] = ชนะ, [1] = แพ้
    public TMP_Text[] bonusTexts;
    public TMP_Text[] coinTexts;
    public TMP_Text[] wordTexts;
    public TMP_Text[] scoreTexts;
    public TMP_Text banner;
    public CanvasGroup fader;            // จอดำตอนเปลี่ยนฉาก

    void Start()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        if (resultRoot) resultRoot.SetActive(false);
        if (banner) banner.gameObject.SetActive(false);
        if (hudRoot) hudRoot.SetActive(true);
        if (popupTemplate) popupTemplate.gameObject.SetActive(false);
        if (toast) toast.gameObject.SetActive(false);
        SetHintLeaves(GameSession.HintLeaves);
        if (fader) StartCoroutine(FadeIn(0.5f));
    }

    public void SetProgress(float t)
    {
        t = Mathf.Clamp01(t);
        if (progressFill) progressFill.fillAmount = t <= 0f ? 0f : Mathf.Lerp(progressFillRange.x, progressFillRange.y, t);
        if (progressText) progressText.text = Mathf.FloorToInt(t * GameSession.TargetDistance) + " m";
    }

    float hpTarget = -1f;  // fillAmount เป้าหมาย (หลอดค่อยๆ เลื่อนไปหา)

    public void SetHp(int hp, int max)
    {
        float t = max > 0 ? Mathf.Clamp01((float)hp / max) : 0f;
        hpTarget = t <= 0f ? 0f : Mathf.Lerp(hpFillRange.x, hpFillRange.y, t);
        if (hpFill && !Application.isPlaying) hpFill.fillAmount = hpTarget;
    }

    void Update() // หลอดเลือดค่อยๆ ลด/เพิ่มแบบนุ่มๆ
    {
        if (hpFill && hpTarget >= 0f) hpFill.fillAmount = Mathf.MoveTowards(hpFill.fillAmount, hpTarget, Time.unscaledDeltaTime * 0.6f);
    }

    public void SetScore(int score, int coins)
    {
        if (scoreText) scoreText.text = coins.ToString();
        if (scoreValueText) scoreValueText.text = score.ToString("N0");
    }

    public void SpawnHpPopup(string text, Color color, int count) // + / - ข้างหลอดเลือด
    {
        for (int i = 0; i < count; i++) SpawnPopupAt(hpPopupAnchor, text, color, new Vector2(i * 28f, -i * 18f));
    }

    public void SpawnPopupAt(RectTransform anchor, string text, Color color) => SpawnPopupAt(anchor, text, color, Vector2.zero);

    void SpawnPopupAt(RectTransform anchor, string text, Color color, Vector2 offset)
    {
        if (!popupTemplate) return;
        var p = Instantiate(popupTemplate, popupTemplate.transform.parent);
        p.gameObject.SetActive(true);
        var rt = (RectTransform)p.transform;
        rt.position = anchor ? anchor.position : rt.position;
        rt.anchoredPosition += offset + new Vector2(60f, 0f);
        p.Init(text, color);
    }

    public void OpenSettings() // เฟือง -> เปิดตั้งค่า + หยุดเกม
    {
        var gm = GameManager.Instance;
        if (gm && (gm.State == GameState.GameOver || gm.State == GameState.Victory)) return;
        if (settingsPanel) settingsPanel.SetActive(true);
        if (gm) gm.SetPaused(true);
    }

    public void ResumeGame()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        if (GameManager.Instance) GameManager.Instance.ResumeWithCountdown(); // นับ 3 2 1 ก่อนเล่นต่อ
    }

    public void RestartGame() { if (GameManager.Instance) GameManager.Instance.RestartGame(); }
    public void BackToMenu() { if (GameManager.Instance) GameManager.Instance.GoToMainMenu(); }

    public void ShowResult(bool win)
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        FillResult(win);
        if (resultRoot) resultRoot.SetActive(true);
    }

    void FillResult(bool win) // ใส่ค่าในหน้าสรุปผล (win = ทำสถิติใหม่ -> หน้า Victory)
    {
        if (victoryPanel) victoryPanel.SetActive(win);
        if (defeatPanel) defeatPanel.SetActive(!win);
        int i = win ? 0 : 1;
        SetAt(distanceTexts, i, Mathf.FloorToInt(GameSession.TotalDistance) + " m.");
        SetAt(bonusTexts, i, "x" + GameSession.Stage);                  // โบนัสเลเวล = ตัวคูณคะแนนตามด่าน
        SetAt(coinTexts, i, GameSession.Coins.ToString());
        SetAt(wordTexts, i, GameSession.AnimalsSolved + " คำ");
        SetAt(scoreTexts, i, "คะแนน " + GameSession.Score.ToString("N0") + "   สถิติ " + SaveData.BestScore.ToString("N0"));
    }

    static void SetAt(TMP_Text[] arr, int i, string value)
    {
        if (arr != null && i < arr.Length && arr[i]) arr[i].text = value;
    }

    public void SetStage(int stage) { if (stageText) stageText.text = "ด่าน " + stage; }
    public void SetHintLeaves(int n) { if (leafText) leafText.text = "x" + n; }

    void OnEnable() { PowerUps.OnChanged += OnPowerUp; }
    void OnDisable() { PowerUps.OnChanged -= OnPowerUp; }

    void OnPowerUp(PowerUps.Kind k, float t) // วงเวลาไอเทมพลังพิเศษ (ซ่อนเมื่อหมด)
    {
        int i = (int)k;
        if (powerUpFills == null || i >= powerUpFills.Length || !powerUpFills[i]) return;
        powerUpFills[i].fillAmount = t;
        var icon = powerUpFills[i].transform.parent.gameObject;
        if (icon.activeSelf != t > 0f) icon.SetActive(t > 0f);
    }

    readonly Queue<string> toasts = new Queue<string>();

    public void ShowToast(string text) // ข้อความต่อคิว โชว์ทีละอัน
    {
        if (!toast) return;
        toasts.Enqueue(text);
        if (toasts.Count == 1) StartCoroutine(ToastRoutine());
    }

    IEnumerator ToastRoutine()
    {
        while (toasts.Count > 0)
        {
            toast.text = toasts.Peek();
            toast.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(2.2f);
            toasts.Dequeue();
        }
        toast.gameObject.SetActive(false);
    }

    public void ShowRival(bool show) { if (rivalRoot && rivalRoot.activeSelf != show) rivalRoot.SetActive(show); }

    public void SetRival(string name, float gap, float hp, bool tired) // แถบบอทเป้าหมาย: ชื่อ ระยะห่าง เลือด
    {
        if (!rivalRoot) return;
        rivalName.text = name;
        rivalGap.text = (tired ? "หมดแรง! " : "") + Mathf.CeilToInt(gap) + " m";
        rivalGap.color = tired ? new Color(1f, 0.85f, 0.3f) : Color.white;
        rivalHp.fillAmount = hp;
    }

    public void ShowBanner(string text)
    {
        if (!banner) return;
        banner.text = text;
        banner.gameObject.SetActive(true);
        StartCoroutine(PopBanner());
    }

    public void HideBanner() { if (banner) banner.gameObject.SetActive(false); }

    IEnumerator PopBanner() // ตัวเลขเด้งใหญ่แล้วหดกลับ
    {
        var t = banner.transform;
        for (float a = 0; a < 0.25f; a += Time.unscaledDeltaTime)
        {
            t.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, a / 0.25f);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    public IEnumerator FadeOut(float d)
    {
        if (!fader) yield break;
        fader.blocksRaycasts = true;
        for (float t = 0; t < d; t += Time.unscaledDeltaTime) { fader.alpha = t / d; yield return null; }
        fader.alpha = 1f;
    }

    IEnumerator FadeIn(float d)
    {
        fader.alpha = 1f;
        for (float t = 0; t < d; t += Time.unscaledDeltaTime) { fader.alpha = 1f - t / d; yield return null; }
        fader.alpha = 0f;
        fader.blocksRaycasts = false;
    }
}
