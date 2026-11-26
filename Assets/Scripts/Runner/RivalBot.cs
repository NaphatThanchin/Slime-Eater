using TMPro;
using UnityEngine;

// สไลม์คู่แข่งในลีก: วิ่งเร็วกว่าผู้เล่น เก็บของ/ฟื้นเลือดไม่ได้ เลือดหมดแล้วหยุดหอบ ผู้เล่นวิ่งถึงตัว = กิน
public class RivalBot : MonoBehaviour
{
    public CharacterModel model;
    public TMP_Text nameTag;
    public LanePlayer player;
    public float showWithin = 70f;   // ใกล้กว่านี้จึงเห็นตัวบอทในฉาก
    public float laneWidth = 2.4f;
    int rank = -1;
    float laneX, laneTimer;
    Vector3 baseScale;

    void Update()
    {
        var gm = GameManager.Instance;
        bool active = gm && gm.State == GameState.RunnerPhase && !TutorialGuide.Active; // สอนเล่นอยู่ยังไม่มีบอท
        if (gm && gm.ui) gm.ui.ShowRival(active);
        if (!active) { model.gameObject.SetActive(false); nameTag.gameObject.SetActive(false); return; }
        if (rank != LeagueData.TargetRank) Spawn();

        GameSession.ChaseDist += WorldScroller.FrameDelta;
        float catchAt = LeagueData.CatchDistance;
        float botPos = Mathf.Min(catchAt, LeagueData.HeadStart + GameSession.ChaseDist * LeagueData.BotSpeed(rank)); // บอทหยุดที่ระยะจับ (เลือดหมด)
        float gap = botPos - GameSession.ChaseDist;
        bool tired = botPos >= catchAt;
        gm.ui.SetRival(LeagueData.BotName(rank), gap, 1f - Mathf.InverseLerp(LeagueData.HeadStart, catchAt, botPos), tired);
        if (gap <= 0f) { Eat(gm); return; }

        bool show = gap < showWithin;
        model.gameObject.SetActive(show);
        nameTag.gameObject.SetActive(show);
        if (!show) return;
        if (!tired && (laneTimer -= Time.deltaTime) <= 0f) { laneX = Random.Range(-1, 2) * laneWidth; laneTimer = Random.Range(1.5f, 3f); } // สลับเลนเป็นระยะ
        float hop = tired ? 0f : Mathf.Abs(Mathf.Sin(Time.time * 7f)) * 0.5f;           // วิ่งกระโดดข้ามของเอง
        transform.position = new Vector3(Mathf.MoveTowards(transform.position.x, laneX, 8f * Time.deltaTime), hop, gap);
        float b = tired ? Mathf.Sin(Time.time * 6f) * 0.08f : 0f;                         // หมดแรง = หอบ
        model.transform.localScale = Vector3.Scale(baseScale, new Vector3(1f + b, 1f - b, 1f + b));
    }

    void Awake() => baseScale = model.transform.localScale;

    void Spawn() // บอทตัวใหม่ตามอันดับเป้าหมาย
    {
        rank = LeagueData.TargetRank;
        model.Show(LeagueData.BotModel(rank));
        nameTag.text = LeagueData.BotName(rank);
        laneX = 0f;
        transform.position = new Vector3(0f, 0f, LeagueData.HeadStart);
    }

    void Eat(GameManager gm) // ไล่ทัน -> กิน + ภารกิจ + แซงอันดับ
    {
        string name = LeagueData.BotName(rank);
        GameSession.EatenThisRun++;
        MissionSystem.Add(MissionSystem.Kind.EatBots, 1);
        MissionSystem.Add(MissionSystem.Kind.EatInRun, GameSession.EatenThisRun);
        if (GameSession.ChaseHits == 0) MissionSystem.Add(MissionSystem.Kind.CleanCatch, 1);
        bool promoted = LeagueData.Eat();
        GameSession.ChaseDist = 0f;
        GameSession.ChaseHits = 0;
        gm.AddScore(500);
        gm.ui.ShowToast(promoted ? "เลื่อนขึ้นลีก " + LeagueData.Name(SaveData.D.league) + "! อัปเกรดได้ถึง Lv " + SaveData.MaxLevel
                                 : "กิน " + name + " แล้ว! ขึ้นอันดับ " + LeagueData.PlayerRank);
        player.Gulp();
        RunnerCamera.Shake(0.2f);
        rank = -1;
    }
}
