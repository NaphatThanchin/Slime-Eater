using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { RunnerPhase, TransitionToBoss, BossPhase, Victory, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public string mainMenuScene = "MainMenu";
    public string runnerScene = "RunnerScene";
    public string bossScene = "BossScene";
    public GameState startState = GameState.RunnerPhase; // state ตอนเริ่มฉากนี้
    public UIManager ui;
    public WorldScroller scroller;  // มีแค่ฉากวิ่ง
    public BossQuizManager boss;    // มีแค่ฉากบอส
    public int coinScore = 10;      // คะแนนต่อเหรียญ
    public float drainEvery = 200f; // ทุก 200m เลือดลดรวม drainAmount (ลดทีละนิดตลอดทาง)
    public int drainAmount = 10;    // 10% ของ 100
    public int countdownFrom = 3;   // นับถอยหลังก่อนเริ่ม/เล่นต่อ
    public GameState State { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsPlaying => !IsPaused && (State == GameState.RunnerPhase || State == GameState.BossPhase);
    int drainCount;                 // ลดเลือดไปแล้วกี่ครั้ง
    float distanceScore;            // คะแนนจากระยะ (สะสมทศนิยม)
    Coroutine countdown;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        if (startState == GameState.BossPhase) GameSession.EnsureInitializedForBoss();
        else if (!GameSession.Initialized) GameSession.Reset();
        drainCount = Mathf.FloorToInt(GameSession.Distance / MetersPerHp);
        SetState(startState);
        RefreshHud();
        MissionSystem.OnCompleted += OnMissionDone;
        if (ui) ui.SetStage(GameSession.Stage);
        countdown = StartCoroutine(CountdownRoutine()); // 3 2 1 ก่อนเริ่ม
    }

    void Update()
    {
        if (State != GameState.RunnerPhase || IsPaused || scroller == null) return;
        float step = scroller.CurrentSpeed * Time.deltaTime;
        GameSession.Distance = Mathf.Min(GameSession.TargetDistance, GameSession.Distance + step); // ระยะเพิ่มตามความเร็วฉาก
        GameSession.TotalDistance += step;
        distanceScore += step * GameSession.Stage;                  // ด่านสูง = คะแนนต่อเมตรมากขึ้น
        if (distanceScore >= 1f) { AddScore((int)distanceScore); distanceScore -= (int)distanceScore; }
        if (ui) ui.SetProgress(GameSession.Distance / GameSession.TargetDistance);
        if (GameSession.Distance < GameSession.TargetDistance && GameSession.Distance >= (drainCount + 1) * MetersPerHp)
        {
            drainCount++;
            Drain(1);
        }
        if (GameSession.Distance >= GameSession.TargetDistance) StartCoroutine(TransitionToBossRoutine());
    }

    float MetersPerHp => drainEvery / drainAmount / CharacterData.DrainRate[CharacterData.Current]; // ปกติลด 1 เลือดทุก 20m (ตัวละครเก่งลดช้ากว่า)
    bool Invincible => CharacterAbility.Instance && CharacterAbility.Instance.Invincible;

    IEnumerator CountdownRoutine() // หยุดเกมไว้ แล้วนับ 3 2 1 ก่อนเล่น
    {
        IsPaused = true;
        Time.timeScale = 0f;
        yield return null; // รอให้ UI พร้อมก่อน
        for (int i = countdownFrom; i > 0; i--)
        {
            if (ui) ui.ShowBanner(i.ToString());
            yield return new WaitForSecondsRealtime(1f);
        }
        if (ui) ui.ShowBanner("เริ่ม!");
        IsPaused = false;
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(0.6f);
        if (ui) ui.HideBanner();
        countdown = null;
        Debug.Log("[GameManager] Countdown จบ -> เล่นได้");
    }

    void SetState(GameState s)
    {
        State = s;
        Debug.Log("[GameManager] State -> " + s);
    }

    public void Damage(int amount)
    {
        if (!IsPlaying || Invincible) return;                         // อมตะ (โหมดปีศาจ / หลังเกิดใหม่) ไม่เสียเลือด
        GameSession.Hp = Mathf.Max(0, GameSession.Hp - amount);
        Debug.Log("[GameManager] Damage " + amount + " -> HP " + GameSession.Hp);
        if (ui) { ui.SetHp(GameSession.Hp, GameSession.MaxHp); ui.SpawnHpPopup("-", Color.red, 3); }
        CheckDeath();
    }

    void Drain(int amount) // เลือดลดเงียบๆ ระหว่างวิ่ง (ไม่มี popup)
    {
        if (!IsPlaying || Invincible) return;
        GameSession.Hp = Mathf.Max(0, GameSession.Hp - amount);
        if (ui) ui.SetHp(GameSession.Hp, GameSession.MaxHp);
        if (drainCount % 10 == 0) Debug.Log("[GameManager] วิ่งไป " + GameSession.Distance.ToString("0") + "m เลือดเหลือ " + GameSession.Hp);
        CheckDeath();
    }

    void CheckDeath() // เลือดหมด -> SlimeAngel เกิดใหม่ได้ 1 ครั้ง ไม่งั้นจบเกม
    {
        if (GameSession.Hp > 0) return;
        if (CharacterAbility.Instance && CharacterAbility.Instance.TryRevive()) { if (ui) ui.SetHp(GameSession.Hp, GameSession.MaxHp); return; }
        GameOver();
    }

    public void Heal(int amount)
    {
        if (!IsPlaying) return;
        GameSession.Hp = Mathf.Min(GameSession.MaxHp, GameSession.Hp + amount);
        Debug.Log("[GameManager] Heal " + amount + " -> HP " + GameSession.Hp);
        if (ui) { ui.SetHp(GameSession.Hp, GameSession.MaxHp); ui.SpawnHpPopup("+", new Color(1f, 0.2f, 0.2f), 3); }
    }

    public void AddCoin(int amount)
    {
        amount *= PowerUps.CoinMultiplier * CharacterData.CoinValue[CharacterData.Current]; // ไอเทม x2 / SlimeKing x2
        GameSession.Coins += amount;
        MissionSystem.Add(MissionSystem.Kind.Coins, amount);
        AddScore(amount * coinScore);
    }

    public void AddScore(int amount)
    {
        GameSession.Score = Mathf.Max(0, GameSession.Score + amount);
        if (ui) ui.SetScore(GameSession.Score, GameSession.Coins);
    }

    void RefreshHud()
    {
        if (!ui) return;
        ui.SetHp(GameSession.Hp, GameSession.MaxHp);
        ui.SetProgress(GameSession.Distance / GameSession.TargetDistance);
        ui.SetScore(GameSession.Score, GameSession.Coins);
    }

    IEnumerator TransitionToBossRoutine() // ถึง 1000m -> ชะลอฉาก -> ไปฉากบอส
    {
        SetState(GameState.TransitionToBoss);
        if (scroller) scroller.StopSmoothly(1.5f);
        if (ui) ui.ShowBanner("ถึงด่านบอสแล้ว!");
        yield return new WaitForSeconds(2.2f);
        if (ui) yield return ui.FadeOut(0.6f);
        SceneManager.LoadScene(bossScene);
    }

    public void GameOver() // เลือดหมด = จบรอบ -> บันทึกสถิติ/เหรียญ แล้วโชว์หน้าสรุป
    {
        if (State == GameState.GameOver) return;
        SetState(GameState.GameOver);
        if (scroller) scroller.StopSmoothly(0.5f);
        MissionSystem.Add(MissionSystem.Kind.Distance, Mathf.FloorToInt(GameSession.TotalDistance));
        bool newBest = SaveData.SubmitRun(GameSession.Score, GameSession.Stage, GameSession.Coins);
        Debug.Log("[GameManager] GAME OVER stage=" + GameSession.Stage + " score=" + GameSession.Score);
        if (ui) ui.ShowResult(newBest);
    }

    public void Victory(int bonusScore) // ผ่านบอส -> ไม่จบเกม แต่ไปวิ่งด่านต่อไป (Endless)
    {
        if (State == GameState.GameOver || State == GameState.Victory) return;
        SetState(GameState.Victory);
        AddScore(bonusScore + 500 * GameSession.Stage);
        StartCoroutine(StageClearRoutine());
    }

    IEnumerator StageClearRoutine()
    {
        if (ui) ui.ShowBanner("ผ่านด่าน " + GameSession.Stage + "!");
        Debug.Log("[GameManager] ผ่านด่าน " + GameSession.Stage + " score=" + GameSession.Score);
        yield return new WaitForSeconds(2.5f);
        GameSession.NextStage();
        if (ui) yield return ui.FadeOut(0.6f);
        SceneManager.LoadScene(runnerScene);
    }

    void OnMissionDone(string title) { if (ui) ui.ShowToast("ภารกิจสำเร็จ: " + title); }

    void OnDestroy() { MissionSystem.OnCompleted -= OnMissionDone; }
    public void SetPaused(bool paused) // หยุด/เล่นต่อ ด้วย timeScale
    {
        if (State == GameState.GameOver) paused = false;
        if (countdown != null) { StopCoroutine(countdown); countdown = null; if (ui) ui.HideBanner(); }
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        Debug.Log("[GameManager] Paused = " + paused);
    }

    public void ResumeWithCountdown() // ปิดหน้าตั้งค่า -> นับ 3 2 1 แล้วเล่นต่อ
    {
        if (countdown != null) StopCoroutine(countdown);
        countdown = StartCoroutine(CountdownRoutine());
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        GameSession.Reset();
        SceneManager.LoadScene(runnerScene);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        GameSession.Initialized = false;
        SceneManager.LoadScene(mainMenuScene);
    }
}
