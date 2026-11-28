using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { RunnerPhase, TransitionToBonus, BonusPhase, Victory, GameOver }

// ตัวคุมเกม 1 รอบ: นับถอยหลัง ระยะทาง เลือด คะแนน เปลี่ยนฉาก จบเกม
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public string mainMenuScene = "MainMenu", runnerScene = "RunnerScene", bonusScene = "BonusTimeScene";
    public GameState startState = GameState.RunnerPhase; // สถานะตอนเริ่มฉากนี้
    public UIManager ui;
    public WorldScroller scroller;  // มีแค่ฉากวิ่ง
    public BonusQuizManager quiz;    // มีแค่ฉาก Bonus time
    public int coinScore = 10;      // คะแนนต่อเหรียญ
    public float drainPerSec = 0.25f; // เลือดลดต่อวินาทีตอนเริ่มด่าน 1 (ด่านละ +15%, หนักขึ้นถึง x2.5 ที่ 2 นาที)
    public int countdownFrom = 3;
    public GameState State { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsPlaying => !IsPaused && (State == GameState.RunnerPhase || State == GameState.BonusPhase);
    float DrainNow => drainPerSec * (1f + 0.15f * (GameSession.Stage - 1)) * Mathf.Lerp(1f, 2.5f, GameSession.Pressure) * CharacterData.DrainRate[CharacterData.Current];
    bool Invincible => CharacterAbility.Instance && CharacterAbility.Instance.Invincible;
    float drainAcc, questDist;      // เลือดลด / ระยะเควสตัวละคร (สะสมทศนิยม)
    float distanceScore, coinFrac;  // คะแนนจากระยะ / เศษเหรียญจากตัวคูณตัวละคร (สะสมทศนิยม)
    int coinTier, stage;
    Coroutine countdown;

    void Awake()
    {
        Instance = this; Time.timeScale = 1f;
        if (startState == GameState.BonusPhase) GameSession.EnsureInitializedForBonus(); // เริ่มรอบก่อนระบบอื่น Start (คำตัวอักษรไม่หาย)
        else if (!GameSession.Initialized) GameSession.Reset();
    }

    void Start()
    {
        State = startState;
        coinTier = GameSession.CoinTier;
        stage = GameSession.Stage;
        if (startState == GameState.RunnerPhase && CharacterAbility.Tier2(CharacterData.Ability.Forest)) GameSession.HintLeaves = Mathf.Max(1, GameSession.HintLeaves); // Green ขั้น 2
        MissionSystem.OnCompleted += OnMissionDone;
        if (ui) { ui.SetHp(GameSession.Hp, GameSession.MaxHp); ui.SetProgress(GameSession.Distance / GameSession.StageLength); ui.SetScore(GameSession.Score, GameSession.Coins); ui.SetStage(GameSession.Stage); ui.SetCoinTier(coinTier); ui.SetHintLeaves(GameSession.HintLeaves); ui.SetMissionBadge(); }
        countdown = StartCoroutine(CountdownRoutine());
    }

    void OnDestroy() => MissionSystem.OnCompleted -= OnMissionDone;
    void OnMissionDone(string title) { if (ui) ui.SetMissionBadge(); } // ไม่ขึ้นข้อความกลางเกม แค่ตัวเลขบนไอคอนภารกิจ

    // ฉากวิ่งไม่รู้จบ: ระยะ คะแนน เลือดลดตามเวลา ทุก 1000 m เปลี่ยนฉากต่อเนื่อง (ไม่หยุด) ตัวอักษรครบ = Bonus time ทันที
    void Update()
    {
        if (State != GameState.RunnerPhase || IsPaused || !scroller) return;
        float step = scroller.CurrentSpeed * Time.deltaTime;
        GameSession.TotalDistance += step;
        if (GameSession.Stage != stage) // ครบ 1000 m = ฉากถัดไป (กลางวัน/เย็น/กลางคืน) ไม่มีนับถอยหลัง
        {
            stage = GameSession.Stage;
            MissionSystem.Add(MissionSystem.Kind.Stage, stage);
            if (ui) ui.SetStage(stage);
            if (SkyCycle.Instance) SkyCycle.Instance.Apply(false);
        }
        distanceScore += step * GameSession.Stage;                 // ด่านสูง คะแนนต่อเมตรมากขึ้น
        if (distanceScore >= 1f) { AddScore((int)distanceScore); distanceScore -= (int)distanceScore; }
        if ((questDist += step) >= 10f) { CharacterData.AddQuest(CharacterData.Quest.Distance, 10); questDist -= 10f; }
        if (ui) ui.SetProgress(GameSession.Distance / GameSession.StageLength);
        if (GameSession.CoinTier != coinTier) { coinTier = GameSession.CoinTier; if (ui) ui.SetCoinTier(coinTier); }
        if (GameSession.StumbleTimer > 0f) GameSession.StumbleTimer -= Time.deltaTime;
        if (!TutorialGuide.Active) // สอนเล่น: ไม่ลดเลือด ไม่เร่งความเร็ว
        {
            GameSession.RunTime += Time.deltaTime;
            if ((drainAcc += DrainNow * Time.deltaTime) >= 1f) { int n = (int)drainAcc; drainAcc -= n; LoseHp(n, false); }
        }
        if (GameSession.WordDone) StartCoroutine(TransitionToBonusRoutine());   // เก็บตัวอักษรครบ = เข้า Bonus time เลย
    }

    public static int HitDamage => Mathf.Min(16, 6 + 2 * GameSession.Stage); // ชนหน้า 8, 10, 12, 14, 16 (ชนข้าง 5 อยู่ใน LanePlayer, ชนซ้ำใน 6 วิ x2)
    public float DrainPerSec => DrainNow;                                      // บอทใช้คิดเลือดลดของมัน
    public int PotionCost => 1 << GameSession.Revives;                         // ยาฟื้น 1, 2, 4 ขวด

    IEnumerator CountdownRoutine() // หยุดเกม แล้วนับ 3 2 1 ก่อนเล่น
    {
        IsPaused = true;
        Time.timeScale = 0f;
        yield return null;
        for (int i = countdownFrom; i > 0; i--) { if (ui) ui.ShowBanner(i.ToString()); yield return new WaitForSecondsRealtime(1f); }
        if (ui) ui.ShowBanner("เริ่ม!");
        IsPaused = false;
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(0.6f);
        if (ui) ui.HideBanner();
        countdown = null;
    }

    // เลือด / เหรียญ / คะแนน
    public void Damage(int amount) => LoseHp(amount, true);

    void LoseHp(int amount, bool popup) // popup = โดนชน/ตอบผิด, ไม่มี popup = เลือดลดตอนวิ่ง
    {
        if (!IsPlaying || Invincible) return;                     // อมตะ (โหมดปีศาจ / หลังเกิดใหม่) ไม่เสียเลือด
        GameSession.DamageTaken += Mathf.Min(amount, GameSession.Hp);
        GameSession.Hp = Mathf.Max(0, GameSession.Hp - amount);
        if (ui) { ui.SetHp(GameSession.Hp, GameSession.MaxHp); if (popup) ui.SpawnHpPopup("-", Color.red, 3); }
        if (GameSession.Hp > 0) return;
        if (CharacterAbility.Instance && CharacterAbility.Instance.TryRevive()) { if (ui) ui.SetHp(GameSession.Hp, GameSession.MaxHp); return; } // SlimeAngel เกิดใหม่
        if (State == GameState.RunnerPhase && SaveData.D.potions >= PotionCost && ui) { IsPaused = true; Time.timeScale = 0f; ui.ShowContinue(PotionCost, SaveData.D.potions); return; } // ถามใช้ยาฟื้น
        GameOver();
    }

    public void ContinueRun() // ใช้ยาฟื้น: เลือด 50% อมตะ 3 วิ ล้างอุปสรรคข้างหน้า 30 m
    {
        SaveData.D.potions -= PotionCost; SaveData.Save();
        GameSession.Revives++;
        GameSession.Hp = GameSession.MaxHp / 2;
        var lp = Object.FindFirstObjectByType<LanePlayer>();
        if (lp) lp.Protect(3f);
        foreach (var o in Object.FindObjectsByType<Obstacle>(FindObjectsSortMode.None)) if (o.transform.position.z < 30f) Destroy(o.gameObject);
        if (ui) { ui.HideContinue(); ui.SetHp(GameSession.Hp, GameSession.MaxHp); }
        ResumeWithCountdown();
    }

    public void DeclineContinue() { if (ui) ui.HideContinue(); IsPaused = false; Time.timeScale = 1f; GameOver(); }

    public void Heal(int amount)
    {
        if (!IsPlaying) return;
        GameSession.Healed += Mathf.Min(amount, GameSession.MaxHp - GameSession.Hp);
        GameSession.Hp = Mathf.Min(GameSession.MaxHp, GameSession.Hp + amount);
        if (ui) { ui.SetHp(GameSession.Hp, GameSession.MaxHp); ui.SpawnHpPopup("+", new Color(1f, 0.2f, 0.2f), 3); }
    }

    public int AddCoin(int amount) // คืนค่าเหรียญที่ได้จริงหลังคูณ
    {
        float f = amount * PowerUps.CoinMultiplier * GameSession.CoinTier * CharacterData.CoinMul[CharacterData.Current] + coinFrac; // ไอเทม x2 / ตัวคูณตามระยะ / King x1.5 Angel x0.8
        amount = (int)f; coinFrac = f - amount;
        GameSession.Coins += amount;
        MissionSystem.Add(MissionSystem.Kind.Coins, amount);
        CharacterData.AddQuest(CharacterData.Quest.Coins, amount);
        AddScore(amount * coinScore);
        return amount;
    }

    public void AddScore(int amount)
    {
        if (amount > 0) amount *= PowerUps.ScoreMultiplier;                    // ไอเทมคะแนน x2
        GameSession.Score = Mathf.Max(0, GameSession.Score + amount);
        if (ui) ui.SetScore(GameSession.Score, GameSession.Coins);
    }

    // เปลี่ยนฉาก / จบรอบ
    IEnumerator TransitionToBonusRoutine() // ตัวอักษรครบ -> ชะลอ -> Bonus time
    {
        State = GameState.TransitionToBonus;
        scroller.StopSmoothly(1.5f);
        if (ui) ui.ShowBanner("Bonus time!");
        yield return new WaitForSeconds(2.2f);
        if (ui) yield return ui.FadeOut(0.6f);
        SceneManager.LoadScene(bonusScene);
    }

    public void Victory(int bonusScore, bool clean = false) // จบ Bonus time -> กลับไปวิ่งต่อจากระยะเดิม (นับถอยหลัง 3 วิ) clean = ไม่ตอบผิดเลย
    {
        if (State == GameState.GameOver || State == GameState.Victory) return;
        State = GameState.Victory;
        MissionSystem.Add(MissionSystem.Kind.Bonus, 1);
        CharacterData.AddQuest(CharacterData.Quest.BonusClear, 1);
        if (clean) CharacterData.AddQuest(CharacterData.Quest.CleanBonus, 1);
        AddScore(bonusScore + 500 * GameSession.Stage);
        StartCoroutine(StageClearRoutine());
    }

    IEnumerator StageClearRoutine()
    {
        if (ui) ui.ShowBanner("จบ Bonus time!");
        yield return new WaitForSeconds(2f);
        GameSession.ClearWord();                                              // คำใหม่ยาวขึ้น 1 ตัว
        if (ui) yield return ui.FadeOut(0.6f);
        SceneManager.LoadScene(runnerScene);
    }

    public void GameOver() // เลือดหมด -> บันทึกเหรียญ/สถิติ แล้วโชว์หน้าสรุป
    {
        if (State == GameState.GameOver) return;
        State = GameState.GameOver;
        if (scroller) scroller.StopSmoothly(0.5f);
        MissionSystem.Add(MissionSystem.Kind.Distance, Mathf.FloorToInt(GameSession.TotalDistance));
        bool newBest = SaveData.SubmitRun(GameSession.Score, GameSession.Stage, GameSession.Coins);
        Debug.Log("[GameManager] จบเกม ด่าน " + GameSession.Stage + " คะแนน " + GameSession.Score);
        if (ui) ui.ShowResult(newBest);
    }

    // หยุด / เล่นต่อ / เริ่มใหม่ / กลับเมนู
    public void SetPaused(bool paused)
    {
        if (State == GameState.GameOver) paused = false;
        if (countdown != null) { StopCoroutine(countdown); countdown = null; if (ui) ui.HideBanner(); }
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    public void ResumeWithCountdown() // ปิดหน้าตั้งค่า -> นับ 3 2 1 แล้วเล่นต่อ
    {
        if (countdown != null) StopCoroutine(countdown);
        countdown = StartCoroutine(CountdownRoutine());
    }

    public void RestartGame() { Time.timeScale = 1f; GameSession.Reset(); SceneManager.LoadScene(runnerScene); }

    public void GoToMainMenu() // ออกกลางทาง: บันทึกตัวนับภารกิจไว้ (เหรียญในรอบนี้ไม่ได้)
    {
        Time.timeScale = 1f;
        GameSession.Initialized = false;
        SaveData.Save();
        SceneManager.LoadScene(mainMenuScene);
    }
}
