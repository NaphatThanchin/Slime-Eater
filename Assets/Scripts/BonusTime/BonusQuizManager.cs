using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ช่วง Bonus time: ทายชื่อสัตว์จากเงา พิมพ์คำตอบภาษาอังกฤษ
public class BonusQuizManager : MonoBehaviour
{
    public List<AnimalDataSO> animals = new List<AnimalDataSO>(); // ว่าง = โหลดจาก Resources/Animals
    public float startTime = 30f;          // Bonus time 30 วิทุกรอบ
    public int baseQuota = 3;              // ตอบถูกครบ baseQuota + ด่าน (สูงสุด 6) = Perfect จบทันที
    public TMP_Text quotaText;
    public TMP_Text leafText;
    public float wrongTimePenalty = 2f;   // ตอบผิด = เสียเวลา 2 วิ (ไม่เสียเลือด) ผิดซ้ำตัวเดิมเปิดตัวใบ้ 1 ตัว
    public int correctScore = 100;        // x ด่าน ต่อข้อที่ถูก
    public int correctCoins = 10;         // x ด่าน ต่อข้อที่ถูก (ตอบครบ = รวม x1.5)
    public int timeBonusPerSecond = 10;   // โบนัสเวลาเหลือตอนตอบครบ
    public float revealDuration = 1.2f;   // เวลาโชว์ภาพสีจริง
    public Image animalImage;
    public TMP_Text timerText;
    public Image timerRing;
    public RectTransform slotContainer;
    public LetterSlotView slotTemplate;
    public TMP_Text counterText;
    public RectTransform typingBox;
    public BonusSlimeReaction slime;
    public TMP_Text[] leafLabels;         // ปุ่มใบไม้ (คอม/มือถือ): "ใช้แล้ว" เมื่อใช้ครบรอบนี้
    readonly List<AnimalDataSO> queue = new List<AnimalDataSO>();
    readonly List<LetterSlotView> slots = new List<LetterSlotView>();
    AnimalDataSO current;
    char[] typed;
    bool[] locked;        // ช่องที่เป็นตัวใบ้ (ลบไม่ได้)
    int wrongAttempts;
    float timeLeft;
    bool busy;            // กำลังเล่นอนิเมชัน ไม่รับพิมพ์
    bool finished;
    int solvedHere;       // ตอบถูกใน Bonus time นี้กี่ตัว
    int wrongTotal;       // ตอบผิดรวมรอบนี้ (0 = ผ่านแบบไม่ผิด)
    bool leafUsed;        // ใบไม้ใช้ได้รอบละ 1 ครั้ง
    public bool Busy => busy || finished;
    public string CurrentAnswer => current ? current.Answer : "";   // ให้ AutoPilot ทดสอบ
    public int NextSlot { get { if (typed == null) return -1; for (int i = 0; i < typed.Length; i++) if (!locked[i] && typed[i] == '\0') return i; return -1; } }
    int Quota => Mathf.Min(6, Mathf.Min(animals.Count, baseQuota + GameSession.Stage));

    void OnEnable() { if (Keyboard.current != null) Keyboard.current.onTextInput += OnKey; }
    void OnDisable() { if (Keyboard.current != null) Keyboard.current.onTextInput -= OnKey; }
    void OnKey(char c) { if (!PhoneKeyboard.Active) OnTextInput(c); } // มือถือรับจาก PhoneKeyboard แทน

    void Start()
    {
        LoadAnimalsIfEmpty();
        if (animals.Count == 0) { Debug.LogError("[Bonus] ไม่มีข้อมูลสัตว์ใน Resources/Animals"); return; }
        animals.RemoveAll(a => a.Answer == GameSession.Word);                     // คำที่เก็บตัวอักษรมาแล้ว ไม่เอามาถาม (กันเฉลย)
        GameSession.BonusRounds++;
        startTime = 30f;
        queue.Clear();
        queue.AddRange(animals.FindAll(a => !GameSession.SeenAnimals.Contains(a.englishName))); // ไม่ซ้ำกับรอบก่อนๆ
        if (queue.Count < Quota) { GameSession.SeenAnimals.Clear(); queue.Clear(); queue.AddRange(animals); }
        for (int i = queue.Count - 1; i > 0; i--) // Fisher-Yates shuffle
        {
            int j = Random.Range(0, i + 1);
            (queue[i], queue[j]) = (queue[j], queue[i]);
        }
        Debug.Log("[Bonus] ลำดับสัตว์รอบนี้: " + string.Join(", ", queue.ConvertAll(a => a.englishName)));
        timeLeft = startTime;
        UpdateCounters();
        NextAnimal();
    }

    void LoadAnimalsIfEmpty()
    {
        animals.RemoveAll(a => a == null || a.englishName == "bird" || a.englishName == "duck"); // นก/เป็ด เงาแยกยาก เอาออก
        if (animals.Count == 0) animals.AddRange(Resources.LoadAll<AnimalDataSO>("Animals"));
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (finished || gm == null || !gm.IsPlaying) return;
        if (!busy)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f) // หมดเวลา = ผ่าน Bonus time
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
        if (kb != null && !PhoneKeyboard.Active && kb.backspaceKey.wasPressedThisFrame) Backspace();
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
        RefreshSlots();
        Debug.Log("[Bonus] สัตว์ใหม่ (" + current.LetterCount + " ตัวอักษร) เหลืออีก " + queue.Count + " ตัว");
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

    public void TypeChar(char c) => OnTextInput(c); // แป้นพิมพ์มือถือ

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

    public void Backspace() // ลบตัวท้าย (ข้ามตัวใบ้) ไม่เสียอะไร
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
        Debug.Log("[Bonus] ตอบ: " + guess);
        if (guess == current.Answer) StartCoroutine(CorrectRoutine());
        else StartCoroutine(WrongRoutine());
    }

    IEnumerator WrongRoutine()
    {
        busy = true;
        wrongAttempts++;
        wrongTotal++;
        var gm = GameManager.Instance;
        if (slime) slime.Nervous(2f);
        timeLeft = Mathf.Max(0.01f, timeLeft - wrongTimePenalty);              // ผิด = เสียเวลา 2 วิ ไม่เสียเลือด
        if (gm.ui) gm.ui.SpawnPopupAt(timerText ? timerText.rectTransform : null, "-" + wrongTimePenalty + "s", Color.red);
        if (wrongAttempts >= 2) RevealRandomLetter();                          // ผิดซ้ำตัวเดิม = เปิดตัวใบ้ 1 ตัว
        yield return Shake(typingBox, 0.4f);
        if (gm.State != GameState.BonusPhase) yield break; // เลือดหมดระหว่างนั้น
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

    IEnumerator CorrectRoutine() // ถูก -> +แต้ม +เหรียญ (+เวลาเฉพาะความสามารถขั้น 2) โชว์ภาพสีจริง แล้วไปตัวต่อไป
    {
        busy = true;
        var gm = GameManager.Instance;
        GameSession.AnimalsSolved++;
        solvedHere++;
        MissionSystem.Add(MissionSystem.Kind.Animals, 1);
        UpdateCounters();
        float plus = CharacterAbility.BonusTimePerCorrect;
        timeLeft += plus;
        gm.AddScore(correctScore * GameSession.Stage);
        int coins = correctCoins * GameSession.Stage;                          // ไม่คูณตัวคูณเหรียญ (กันเฟ้อ)
        GameSession.Coins += coins; gm.AddScore(0);
        if (gm.ui)
        {
            gm.ui.SpawnPopupAt(animalImage ? animalImage.rectTransform : null, "+" + coins + " เหรียญ", new Color(1f, 0.85f, 0.1f));
            if (plus > 0f) gm.ui.SpawnPopupAt(timerText ? timerText.rectTransform : null, "+" + plus + "s", new Color(0.2f, 0.85f, 0.2f));
        }
        if (slime) slime.Happy();
        ShowAnimal(current, true);
        AudioManager.Play(AudioManager.Sfx.Eat);                    // ตอบถูก = เสียงกิน
        Debug.Log("[Bonus] ถูกต้อง! (" + GameSession.AnimalsSolved + "/" + animals.Count + ")");
        yield return PopScale(animalImage ? animalImage.rectTransform : null, revealDuration);
        if (gm.State == GameState.BonusPhase) { if (solvedHere >= Quota) Finish(true); else NextAnimal(); }
    }

    public void UseHintLeaf() // ใช้ใบไม้วิเศษเปิดตัวอักษร 1 ตัว (ใช้ได้รอบละ 1 ครั้ง)
    {
        if (busy || finished || current == null || GameSession.HintLeaves <= 0 || leafUsed) return;
        leafUsed = true;
        GameSession.HintLeaves--;
        MissionSystem.Add(MissionSystem.Kind.Leaves, 1);
        RevealRandomLetter();
        UpdateCounters();
        if (AllFilled()) Validate();
    }

    void UpdateCounters() // ตัวนับเป้าหมาย + ใบไม้ที่เหลือ
    {
        if (quotaText) quotaText.text = "ตอบถูก " + solvedHere + " / " + Quota;
        if (leafText) leafText.text = "x" + GameSession.HintLeaves;
        if (leafUsed && leafLabels != null) foreach (var l in leafLabels) if (l) l.text = "ใช้แล้ว";
    }

    void Finish(bool perfect) // ตอบครบ = เพอร์เฟกต์ + โบนัสเวลา, หมดเวลา = ชนะธรรมดา
    {
        if (finished) return;
        finished = true;
        int bonus = perfect ? Mathf.CeilToInt(timeLeft) * timeBonusPerSecond : 0;
        if (perfect && solvedHere > 0) // ตอบครบทุกข้อ = รางวัลรวม x1.5
        {
            int extra = solvedHere * correctCoins * GameSession.Stage / 2;
            GameSession.Coins += extra;
            bonus += solvedHere * correctScore * GameSession.Stage / 2;
        }
        Debug.Log(perfect ? "[Bonus] ตอบครบเป้า! โบนัสเวลา " + bonus : "[Bonus] หมดเวลา -> ผ่านด่าน (ตอบได้ " + solvedHere + " ตัว)");
        GameManager.Instance.Victory(bonus, wrongTotal == 0);
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
        RefreshSlots();
        if (timerText) timerText.text = Mathf.CeilToInt(startTime).ToString();
        if (timerRing) timerRing.fillAmount = 1f;
    }
}
