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
    public TMP_Text coinTierText;        // ตัวคูณเหรียญ x1, x2 ...
    public TMP_Text wordText;            // ตัวอักษรที่ต้องเก็บก่อน Bonus time (มุมขวาบน)
    public TMP_Text abilityText;         // สถานะความสามารถตัวละคร
    public GameObject continuePanel;     // ตายแล้วถามใช้ยาฟื้น
    public TMP_Text continueText;
    public GameObject bubbleButton;      // ปุ่มฟองสไลม์ (มีของ + ยังไม่ใช้รอบนี้)
    public GameObject missionIcon;       // ไอคอนภารกิจที่ทำครบรอกดรับ + ตัวเลข (แทนข้อความกลางเกม)
    public TMP_Text missionCount;
    public TMP_Text bubbleCount;
    public Image[] powerUpFills;         // วงเวลาไอเทม แม่เหล็ก/เกราะ/x2
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
        if (continuePanel) continuePanel.SetActive(false);
        SetHintLeaves(GameSession.HintLeaves);
        SetWord();
        if (fader) StartCoroutine(FadeIn(0.5f));
    }

    public void SetProgress(float t)
    {
        t = Mathf.Clamp01(t);
        if (progressFill) progressFill.fillAmount = t <= 0f ? 0f : Mathf.Lerp(progressFillRange.x, progressFillRange.y, t);
        if (progressText) progressText.text = Mathf.FloorToInt(GameSession.TotalDistance).ToString("N0") + " m";   // ระยะจริงที่วิ่งได้ (หลอด = ถึงฉากถัดไป)
    }

    public void SetCoinTier(int tier) { if (coinTierText) coinTierText.text = "เหรียญ x" + tier; }

    public void SetWord() // B O _ _ : ตัวที่เก็บแล้วสีทอง ตัวที่ยังไม่ได้สีจาง
    {
        if (!wordText) return;
        string w = GameSession.Word;
        if (string.IsNullOrEmpty(w)) { wordText.text = ""; return; }
        var sb = new System.Text.StringBuilder("<size=70%>Bonus time</size>\n");
        for (int i = 0; i < w.Length; i++)
            sb.Append(i < GameSession.Got.Length && GameSession.Got[i] ? "<color=#FFD54A>" + w[i] + "</color>" : "<color=#FFFFFF55>" + w[i] + "</color>").Append(i < w.Length - 1 ? " " : "");
        wordText.text = sb.ToString();
    }

    float hpTarget = -1f;  // fillAmount เป้าหมาย (หลอดค่อยๆ เลื่อนไปหา)

    public void SetHp(int hp, int max)
    {
        float t = max > 0 ? Mathf.Clamp01((float)hp / max) : 0f;
        hpTarget = t <= 0f ? 0f : Mathf.Lerp(hpFillRange.x, hpFillRange.y, t);
        if (hpFill && !Application.isPlaying) hpFill.fillAmount = hpTarget;
    }

    void Update() // หลอดเลือดค่อยๆ ลด/เพิ่มแบบนุ่มๆ + สถานะความสามารถ
    {
        if (hpFill && hpTarget >= 0f) hpFill.fillAmount = Mathf.MoveTowards(hpFill.fillAmount, hpTarget, Time.unscaledDeltaTime * 0.6f);
        if (abilityText && CharacterAbility.Instance && Time.frameCount % 10 == 0) abilityText.text = CharacterAbility.Instance.Status;
        if (bubbleButton && PowerUps.Instance && Time.frameCount % 10 == 0)
        {
            bool show = PowerUps.Instance.CanBubble;
            if (bubbleButton.activeSelf != show) bubbleButton.SetActive(show);
            if (bubbleCount) bubbleCount.text = "x" + SaveData.D.bubbles;
        }
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

    public void UseBubble() { if (PowerUps.Instance) PowerUps.Instance.UseBubble(); }
    public void ShowContinue(int cost, int have) { if (!continuePanel) return; continueText.text = "ใช้ยาฟื้นสไลม์ " + cost + " ขวด เล่นต่อ?\n(มี " + have + " ขวด)"; continuePanel.SetActive(true); }
    public void HideContinue() { if (continuePanel) continuePanel.SetActive(false); }
    public void ContinueYes() { if (GameManager.Instance) GameManager.Instance.ContinueRun(); }
    public void ContinueNo() { if (GameManager.Instance) GameManager.Instance.DeclineContinue(); }

    public void RestartGame() { if (GameManager.Instance) GameManager.Instance.RestartGame(); }
    public void BackToMenu() { if (GameManager.Instance) GameManager.Instance.GoToMainMenu(); }

    public void ShowResult(bool newBest)
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        FillResult(newBest);
        if (resultRoot) resultRoot.SetActive(true);
    }

    void FillResult(bool newBest) // เลือดหมด = หน้าแพ้เสมอ (สถิติใหม่ขึ้นเป็นข้อความแทน ไม่ใช่หน้า Victory)
    {
        if (victoryPanel) victoryPanel.SetActive(false);
        if (defeatPanel) defeatPanel.SetActive(true);
        int i = 1;
        SetAt(distanceTexts, i, Mathf.FloorToInt(GameSession.TotalDistance) + " m.");
        SetAt(bonusTexts, i, "x" + GameSession.Stage);                  // โบนัสเลเวล = ตัวคูณคะแนนตามด่าน
        SetAt(coinTexts, i, GameSession.Coins.ToString());
        SetAt(wordTexts, i, GameSession.AnimalsSolved + " คำ");
        SetAt(scoreTexts, i, (newBest ? "สถิติใหม่! " : "") + "คะแนน " + GameSession.Score.ToString("N0") + "  |  สถิติ " + SaveData.BestScore.ToString("N0"));
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

    public void SetMissionBadge() // ภารกิจครบรอกดรับกี่อัน
    {
        if (!missionIcon) return;
        int n = MissionSystem.PendingCount;
        for (int c = 0; c < CharacterData.Count; c++) if (CharacterData.CanClaim(c)) n++;   // ความสามารถตัวละครที่รอกดรับ
        missionIcon.SetActive(n > 0);
        if (missionCount) missionCount.text = n.ToString();
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
