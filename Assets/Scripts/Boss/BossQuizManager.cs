using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ช่วงบอส: ทายชื่อสัตว์จากเงา พิมพ์คำตอบภาษาอังกฤษ
public class BossQuizManager : MonoBehaviour
{
    public List<AnimalDataSO> animals = new List<AnimalDataSO>(); // ว่าง = โหลดจาก Resources/Animals
    public float startTime = 90f;          // ด่าน 1 (ด่านต่อไปลดลงทีละ 10 วินาที ต่ำสุด 50)
    public int baseQuota = 4;              // ตอบถูกครบ baseQuota + ด่าน = ผ่านบอสทันที
    public TMP_Text quotaText;
    public TMP_Text leafText;
    public float timerSpeed = 2f;         // 1 วินาทีจริง = นับลด 2 วินาที
    public float wrongTimePenalty = 10f;  // ผิดครั้งแรก ลดเวลา
    public int wrongHpPenalty = 10;       // ผิดครั้งที่ 2+ ลดเลือด
    public float correctTimeBonus = 10f;
    public int correctHeal = 5;
    public int correctScore = 100;
    public int timeBonusPerSecond = 10;   // โบนัสเวลาเหลือตอนตอบครบ
    public float revealDuration = 1.2f;   // เวลาโชว์ภาพสีจริง
    public Image animalImage;
    public TMP_Text timerText;
    public Image timerRing;
    public RectTransform slotContainer;
    public LetterSlotView slotTemplate;
    public TMP_Text counterText;
    public RectTransform typingBox;
    public GameObject hintBubble;
    public TMP_Text hintText;
    public BossSlimeReaction slime;
    readonly List<AnimalDataSO> queue = new List<AnimalDataSO>();
    readonly List<LetterSlotView> slots = new List<LetterSlotView>();
    AnimalDataSO current;
    char[] typed;
    bool[] locked;        // ช่องที่เป็นตัวใบ้ (ลบไม่ได้)
    int wrongAttempts;
    float timeLeft;
    bool busy;            // กำลังเล่นอนิเมชัน ไม่รับพิมพ์
    bool finished;
    int solvedHere;       // ตอบถูกในบอสนี้กี่ตัว
    int Quota => Mathf.Min(animals.Count, baseQuota + GameSession.Stage);

    void OnEnable() { if (Keyboard.current != null) Keyboard.current.onTextInput += OnTextInput; }
    void OnDisable() { if (Keyboard.current != null) Keyboard.current.onTextInput -= OnTextInput; }

    void Start()
    {
        LoadAnimalsIfEmpty();
        if (animals.Count == 0) { Debug.LogError("[Boss] ไม่มีข้อมูลสัตว์ใน Resources/Animals"); return; }
        startTime = Mathf.Max(50f, startTime - 10f * (GameSession.Stage - 1));
        queue.Clear();
        queue.AddRange(animals.FindAll(a => !GameSession.SeenAnimals.Contains(a.englishName))); // ไม่ซ้ำกับบอสก่อนๆ
        if (queue.Count < Quota) { GameSession.SeenAnimals.Clear(); queue.Clear(); queue.AddRange(animals); }
        for (int i = queue.Count - 1; i > 0; i--) // Fisher-Yates shuffle
        {
            int j = Random.Range(0, i + 1);
            (queue[i], queue[j]) = (queue[j], queue[i]);
        }
        Debug.Log("[Boss] ลำดับสัตว์รอบนี้: " + string.Join(", ", queue.ConvertAll(a => a.englishName)));
        timeLeft = startTime;
        UpdateCounters();
        if (hintBubble) hintBubble.SetActive(false);
        NextAnimal();
    }

    void LoadAnimalsIfEmpty()
    {
        animals.RemoveAll(a => a == null);
        if (animals.Count == 0) animals.AddRange(Resources.LoadAll<AnimalDataSO>("Animals"));
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (finished || gm == null || !gm.IsPlaying) return;
        if (!busy)
        {
            timeLeft -= Time.deltaTime * timerSpeed;
            if (timeLeft <= 0f) // หมดเวลา = ชนะ
            {
                timeLeft = 0f;
                UpdateTimerUI();
                Finish(false);
                return;
            }
        }
        UpdateTimerUI();
        if (slime) slime.SetLowTime(timeLeft <= 10f);
        var kb = Keyboard.current;
        if (kb != null && kb.backspaceKey.wasPressedThisFrame) Backspace();
        if (kb != null && (kb.tabKey.wasPressedThisFrame || kb.digit1Key.wasPressedThisFrame)) UseHintLeaf();
    }

    void UpdateTimerUI()
    {
        if (timerText) timerText.text = Mathf.CeilToInt(timeLeft).ToString();
        if (timerRing) timerRing.fillAmount = Mathf.Clamp01(timeLeft / startTime);
    }

    void NextAnimal()
    {
        if (queue.Count == 0) { Finish(true); return; }
        current = queue[0];
        queue.RemoveAt(0); // เอาออก -> ไม่ซ้ำ
        GameSession.SeenAnimals.Add(current.englishName);
        wrongAttempts = 0;
        busy = false;
        typed = new char[current.LetterCount];
        locked = new bool[current.LetterCount];
        ShowAnimal(current, false);
        BuildSlots(current.LetterCount);
        if (hintText) hintText.text = current.thaiHint;
        if (hintBubble) hintBubble.SetActive(false);
        RefreshSlots();
        Debug.Log("[Boss] สัตว์ใหม่ (" + current.LetterCount + " ตัวอักษร) เหลืออีก " + queue.Count + " ตัว");
    }

    void ShowAnimal(AnimalDataSO a, bool reveal) // เงา หรือ ภาพสีจริง
    {
        if (!animalImage || a == null) return;
        if (reveal || a.silhouetteSprite == null)
        {
            animalImage.sprite = a.fullColorSprite;
            animalImage.color = reveal ? Color.white : Color.black; // ไม่มีภาพเงา -> ย้อมดำ
        }
        else
        {
            animalImage.sprite = a.silhouetteSprite;
            animalImage.color = Color.white;
        }
        animalImage.preserveAspect = true;
    }

    void BuildSlots(int count) // สร้างช่อง _ _ _ ตามจำนวนตัวอักษร
    {
        for (int i = slotContainer.childCount - 1; i >= 0; i--)
        {
            var child = slotContainer.GetChild(i).gameObject;
            if (child == slotTemplate.gameObject) continue;
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
        slots.Clear();
        slotTemplate.gameObject.SetActive(false);
        for (int i = 0; i < count; i++)
        {
            var s = Instantiate(slotTemplate, slotContainer);
            s.name = "Slot_" + i;
            s.gameObject.SetActive(true);
            slots.Add(s);
        }
    }

    void RefreshSlots() // อัปเดตตัวอักษร + ตัวนับ X / N
    {
        int filled = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].Set(typed[i], locked[i]);
            if (typed[i] != '\0') filled++;
        }
        if (counterText) counterText.text = filled + " / " + slots.Count;
    }

    public void TypeLetter(int i) => OnTextInput((char)('A' + i)); // ปุ่ม A-Z บนจอ (มือถือ)

    void OnTextInput(char c) // รับ A-Z เติมช่องซ้ายไปขวา
    {
        var gm = GameManager.Instance;
        if (busy || finished || current == null || gm == null || !gm.IsPlaying) return;
        c = char.ToUpperInvariant(c);
        if (c < 'A' || c > 'Z') return;
        for (int i = 0; i < typed.Length; i++)
        {
            if (locked[i] || typed[i] != '\0') continue;
            typed[i] = c;
            RefreshSlots();
            if (AllFilled()) Validate(); // ครบแล้วส่งคำตอบเลย
            return;
        }
    }

    public void Backspace() // ลบตัวท้าย (ข้ามตัวใบ้) ใช้ทั้งปุ่มคีย์บอร์ดและปุ่มบนจอ
    {
        if (busy || finished || typed == null) return;
        for (int i = typed.Length - 1; i >= 0; i--)
        {
            if (locked[i] || typed[i] == '\0') continue;
            typed[i] = '\0';
            RefreshSlots();
            return;
        }
    }

    bool AllFilled()
    {
        foreach (char c in typed) if (c == '\0') return false;
        return true;
    }

    void Validate()
    {
        string guess = new string(typed);
        Debug.Log("[Boss] ตอบ: " + guess);
        if (guess == current.Answer) StartCoroutine(CorrectRoutine());
        else StartCoroutine(WrongRoutine());
    }

    IEnumerator WrongRoutine()
    {
        busy = true;
        wrongAttempts++;
        var gm = GameManager.Instance;
        if (slime) slime.Nervous(2f);
        if (wrongAttempts == 1) // ผิดครั้งแรก -> ลดเวลา
        {
            timeLeft = Mathf.Max(0f, timeLeft - wrongTimePenalty);
            if (gm.ui) gm.ui.SpawnPopupAt(timerText ? timerText.rectTransform : null, "-" + wrongTimePenalty + "s", Color.red);
            Debug.Log("[Boss] ผิดครั้งที่ 1 -> เวลา -" + wrongTimePenalty);
        }
        else // ผิดครั้งที่ 2+ -> ลดเลือด + เปิดตัวใบ้ 1 ตัว
        {
            gm.Damage(wrongHpPenalty);
            RevealRandomLetter();
            Debug.Log("[Boss] ผิดครั้งที่ " + wrongAttempts + " -> เลือด -" + wrongHpPenalty + " และเปิดคำใบ้ 1 ตัว");
        }
        yield return Shake(typingBox, 0.4f);
        if (gm.State != GameState.BossPhase) yield break; // เลือดหมดระหว่างนั้น
        for (int i = 0; i < typed.Length; i++) if (!locked[i]) typed[i] = '\0'; // ล้างตัวผิด เก็บตัวใบ้
        RefreshSlots();
        busy = false;
        if (AllFilled()) Validate(); // เปิดใบ้ครบทุกตัวแล้ว
    }

    void RevealRandomLetter() // สุ่มเปิดตัวที่ถูกในช่องจริงของมัน
    {
        var candidates = new List<int>();
        for (int i = 0; i < locked.Length; i++) if (!locked[i]) candidates.Add(i);
        if (candidates.Count == 0) return;
        int idx = candidates[Random.Range(0, candidates.Count)];
        locked[idx] = true;
        typed[idx] = current.Answer[idx];
        RefreshSlots();
    }

    IEnumerator CorrectRoutine() // ถูก -> +เวลา +เลือด +แต้ม โชว์ภาพสีจริง แล้วไปตัวต่อไป
    {
        busy = true;
        var gm = GameManager.Instance;
        GameSession.AnimalsSolved++;
        solvedHere++;
        MissionSystem.Add(MissionSystem.Kind.Animals, 1);
        UpdateCounters();
        timeLeft += correctTimeBonus;
        gm.Heal(correctHeal);
        gm.AddScore(correctScore);
        if (gm.ui)
        {
            gm.ui.SpawnPopupAt(animalImage ? animalImage.rectTransform : null, "+1", new Color(1f, 0.85f, 0.1f));
            gm.ui.SpawnPopupAt(timerText ? timerText.rectTransform : null, "+" + correctTimeBonus + "s", new Color(0.2f, 0.85f, 0.2f));
        }
        if (slime) slime.Happy();
        if (hintBubble) hintBubble.SetActive(false);
        ShowAnimal(current, true);
        AudioManager.Play(AudioManager.Sfx.Eat);                    // ตอบถูก = เสียงกิน
        Debug.Log("[Boss] ถูกต้อง! (" + GameSession.AnimalsSolved + "/" + animals.Count + ")");
        yield return PopScale(animalImage ? animalImage.rectTransform : null, revealDuration);
        if (gm.State == GameState.BossPhase) { if (solvedHere >= Quota) Finish(true); else NextAnimal(); }
    }

    public void UseHintLeaf() // ใช้ใบไม้วิเศษเปิดตัวอักษร 1 ตัว (ไม่เสียเลือด/เวลา)
    {
        if (busy || finished || current == null || GameSession.HintLeaves <= 0) return;
        GameSession.HintLeaves--;
        MissionSystem.Add(MissionSystem.Kind.Leaves, 1);
        RevealRandomLetter();
        UpdateCounters();
        if (AllFilled()) Validate();
    }

    void UpdateCounters() // ตัวนับเป้าหมาย + ใบไม้ที่เหลือ
    {
        if (quotaText) quotaText.text = "ทายถูก " + solvedHere + " / " + Quota;
        if (leafText) leafText.text = "x" + GameSession.HintLeaves;
    }

    public void ToggleHint() // ปุ่ม ?
    {
        if (!hintBubble || finished) return;
        bool show = !hintBubble.activeSelf;
        hintBubble.SetActive(show);
        if (show && slime) slime.Nervous(1.5f);
        Debug.Log("[Boss] คำใบ้ " + (show ? "เปิด" : "ปิด"));
    }

    void Finish(bool perfect) // ตอบครบ = เพอร์เฟกต์ + โบนัสเวลา, หมดเวลา = ชนะธรรมดา
    {
        if (finished) return;
        finished = true;
        int bonus = perfect ? Mathf.CeilToInt(timeLeft) * timeBonusPerSecond : 0;
        Debug.Log(perfect ? "[Boss] ตอบครบเป้า! โบนัสเวลา " + bonus : "[Boss] หมดเวลา -> ผ่านด่าน (ตอบได้ " + solvedHere + " ตัว)");
        GameManager.Instance.Victory(bonus);
    }

    IEnumerator Shake(RectTransform rt, float duration) // สั่นกล่องพิมพ์
    {
        if (!rt) { yield return new WaitForSeconds(duration); yield break; }
        Vector2 basePos = rt.anchoredPosition;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            rt.anchoredPosition = basePos + Vector2.right * Mathf.Sin(t * 60f) * 14f * (1f - t / duration);
            yield return null;
        }
        rt.anchoredPosition = basePos;
    }

    IEnumerator PopScale(RectTransform rt, float duration) // ภาพเด้งตอนเฉลย
    {
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (rt) rt.localScale = Vector3.one * (1f + Mathf.Sin(Mathf.Clamp01(t / 0.3f) * Mathf.PI) * 0.12f);
            yield return null;
        }
        if (rt) rt.localScale = Vector3.one;
    }

    [ContextMenu("Preview First Animal")]
    public void BuildPreview() // โชว์เงาหมี + ช่องตัวอักษรใน Edit Mode
    {
        LoadAnimalsIfEmpty();
        if (animals.Count == 0 || !slotContainer || !slotTemplate) return;
        var a = animals.Find(x => x.englishName == "bear") ?? animals[0];
        current = a;
        typed = new char[a.LetterCount];
        locked = new bool[a.LetterCount];
        ShowAnimal(a, false);
        BuildSlots(a.LetterCount);
        if (hintText) hintText.text = a.thaiHint;
        RefreshSlots();
        if (timerText) timerText.text = Mathf.CeilToInt(startTime).ToString();
        if (timerRing) timerRing.fillAmount = 1f;
    }
}
