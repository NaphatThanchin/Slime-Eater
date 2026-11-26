using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { RunnerPhase, TransitionToBoss, BossPhase, Victory, GameOver }

// ตัวคุมเกม 1 รอบ: นับถอยหลัง ระยะทาง เลือด คะแนน เปลี่ยนฉาก จบเกม
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public string mainMenuScene = "MainMenu", runnerScene = "RunnerScene", bossScene = "BossScene";
    public GameState startState = GameState.RunnerPhase; // สถานะตอนเริ่มฉากนี้
    public UIManager ui;
    public WorldScroller scroller;  // มีแค่ฉากวิ่ง
    public BossQuizManager boss;    // มีแค่ฉากบอส
    public int coinScore = 10;      // คะแนนต่อเหรียญ
    public float drainEvery = 200f; // ทุก 200 m เลือดลดรวม drainAmount (ทีละ 1)
    public int drainAmount = 10;
    public int countdownFrom = 3;
    public GameState State { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsPlaying => !IsPaused && (State == GameState.RunnerPhase || State == GameState.BossPhase);
    float MetersPerHp => drainEvery / drainAmount / CharacterData.DrainRate[CharacterData.Current]; // ตัวละครเก่งเลือดลดช้ากว่า
    bool Invincible => CharacterAbility.Instance && CharacterAbility.Instance.Invincible;
    int drainCount;
    float distanceScore;            // คะแนนจากระยะ (สะสมทศนิยม)
    Coroutine countdown;

    void Awake() { Instance = this; Time.timeScale = 1f; }

    void Start()
    {
        if (startState == GameState.BossPhase) GameSession.EnsureInitializedForBoss();
        else if (!GameSession.Initialized) GameSession.Reset();
        drainCount = Mathf.FloorToInt(GameSession.Distance / MetersPerHp);
        State = startState;
        MissionSystem.OnCompleted += OnMissionDone;
        if (ui) { ui.SetHp(GameSession.Hp, GameSession.MaxHp); ui.SetProgress(GameSession.Distance / GameSession.TargetDistance); ui.SetScore(GameSession.Score, GameSession.Coins); ui.SetStage(GameSession.Stage); }
        countdown = StartCoroutine(CountdownRoutine());
    }

    void OnDestroy() => MissionSystem.OnCompleted -= OnMissionDone;
    void OnMissionDone(string title) { if (ui) ui.ShowToast("ภารกิจสำเร็จ: " + title); }

    // ฉากวิ่ง: ระยะทาง คะแนนตามระยะ เลือดลดทีละนิด ครบระยะไปบอส
    void Update()
    {
        if (State != GameState.RunnerPhase || IsPaused || !scroller) return;
        float step = scroller.CurrentSpeed * Time.deltaTime;
        GameSession.Distance = Mathf.Min(GameSession.TargetDistance, GameSession.Distance + step);
        GameSession.TotalDistance += step;
        distanceScore += step * GameSession.Stage;                 // ด่านสูง คะแนนต่อเมตรมากขึ้น
        if (distanceScore >= 1f) { AddScore((int)distanceScore); distanceScore -= (int)distanceScore; }
        if (ui) ui.SetProgress(GameSession.Distance / GameSession.TargetDistance);
        if (GameSession.Distance < GameSession.TargetDistance && GameSession.Distance >= (drainCount + 1) * MetersPerHp) { drainCount++; LoseHp(1, false); }
        if (GameSession.Distance >= GameSession.TargetDistance) StartCoroutine(TransitionToBossRoutine());
    }

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
        GameSession.Hp = Mathf.Max(0, GameSession.Hp - amount);
        if (ui) { ui.SetHp(GameSession.Hp, GameSession.MaxHp); if (popup) ui.SpawnHpPopup("-", Color.red, 3); }
        if (GameSession.Hp > 0) return;
        if (CharacterAbility.Instance && CharacterAbility.Instance.TryRevive()) { if (ui) ui.SetHp(GameSession.Hp, GameSession.MaxHp); return; } // SlimeAngel เกิดใหม่
        GameOver();
    }

    public void Heal(int amount)
    {
        if (!IsPlaying) return;
        GameSession.Hp = Mathf.Min(GameSession.MaxHp, GameSession.Hp + amount);
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

    // เปลี่ยนฉาก / จบรอบ
    IEnumerator TransitionToBossRoutine() // ครบ 1000 m -> ชะลอ -> ฉากบอส
    {
        State = GameState.TransitionToBoss;
        scroller.StopSmoothly(1.5f);
        if (ui) ui.ShowBanner("ถึงด่านบอสแล้ว!");
        yield return new WaitForSeconds(2.2f);
        if (ui) yield return ui.FadeOut(0.6f);
        SceneManager.LoadScene(bossScene);
    }

    public void Victory(int bonusScore) // ผ่านบอส -> ด่านต่อไป (วิ่งไม่รู้จบ)
    {
        if (State == GameState.GameOver || State == GameState.Victory) return;
        State = GameState.Victory;
        AddScore(bonusScore + 500 * GameSession.Stage);
        StartCoroutine(StageClearRoutine());
    }

    IEnumerator StageClearRoutine()
    {
        if (ui) ui.ShowBanner("ผ่านด่าน " + GameSession.Stage + "!");
        yield return new WaitForSeconds(2.5f);
        GameSession.NextStage();
        if (ui) yield return ui.FadeOut(0.6f);
        SceneManager.LoadScene(runnerScene);
    }

    public void GameOver() // เลือดหมด -> บันทึกเหรียญ/สถิติ แล้วโชว์หน้าสรุป
    {
        if (State == GameState.GameOver) return;
        State = GameState.GameOver;
        if (scroller) scroller.StopSmoothly(0.5f);
        MissionSystem.Add(MissionSystem.Kind.Distance, Mathf.FloorToInt(GameSession.TotalDistance));
        LeagueData.RunEnded(GameSession.TotalDistance, GameSession.MaxHp, GameSession.ChaseDist > 0f); // จำระยะไว้ปรับความยากบอท
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
