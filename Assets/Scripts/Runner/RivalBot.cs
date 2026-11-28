using System.Collections;
using TMPro;
using UnityEngine;

// สไลม์คู่แข่งในลีก: วิ่งเร็วกว่าผู้เล่น หลบอุปสรรคเอง เก็บของ/ฟื้นเลือดไม่ได้ หมดแรงแล้วนั่งหอบข้างทาง ผู้เล่นวิ่งถึง = กิน
public class RivalBot : MonoBehaviour
{
    public SlimeMotion motion;          // ท่าทางชุดเดียวกับผู้เล่น
    public CharacterModel model;
    public TMP_Text nameTag;
    public LanePlayer player;
    public Transform track;             // ที่อยู่ของอุปสรรค (TrackSpawner) ไว้ดูข้างหน้า
    public Material hueMaterial;        // shader เปลี่ยนเฉดสี ให้บอทสีต่างจากผู้เล่น
    public float showWithin = 70f;      // ใกล้กว่านี้จึงเห็นตัว
    public float laneWidth = 2.4f;
    static readonly float[] LeagueHue = { 0.72f, 0.45f, 0.25f, 0.55f, 0.85f, 0.6f }; // หมุนเฉดสีต่อลีก (เขียว->ส้ม, ม่วง->ฟ้า ...)
    int rank = -1, lane;
    bool eating;

    void Update()
    {
        var gm = GameManager.Instance;
        bool active = gm && gm.State == GameState.RunnerPhase && !TutorialGuide.Active; // สอนเล่นอยู่ยังไม่มีบอท
        if (gm && gm.ui) gm.ui.ShowRival(active && !eating);
        if (!active) { Show(false); return; }
        if (eating) return;
        if (rank != LeagueData.TargetRank) Spawn();

        GameSession.ChaseDist += WorldScroller.FrameDelta;
        float catchAt = LeagueData.CatchDistance;
        float botPos = Mathf.Min(catchAt, LeagueData.HeadStart + GameSession.ChaseDist * LeagueData.BotSpeed(rank)); // บอทหยุดที่ระยะจับ (เลือดหมด)
        float gap = botPos - GameSession.ChaseDist;
        bool tired = botPos >= catchAt;
        gm.ui.SetRival(LeagueData.BotName(rank), gap, 1f - Mathf.InverseLerp(LeagueData.HeadStart, catchAt, botPos), tired);
        if (gap <= 0f) { StartCoroutine(EatRoutine(gm)); return; }

        Show(gap < showWithin);
        motion.tired = tired;
        if (!tired) Dodge();
        float x = tired ? Mathf.Sign(lane == 0 ? 1 : lane) * (laneWidth * 1.5f + 0.4f) : lane * laneWidth; // หมดแรง = ไปนั่งริมทาง ไม่ขวางอุปสรรค
        motion.laneX = x;
        transform.position = new Vector3(Mathf.MoveTowards(transform.position.x, x, 8f * Time.deltaTime), transform.position.y, gap);
    }

    void Dodge() // มองอุปสรรคข้างหน้า: หิน/ต้นไม้ = กระโดด, ท่อนไม้สูง = สไลด์, หินใหญ่ = เปลี่ยนเลน
    {
        float z = transform.position.z, look = WorldScroller.Instance.CurrentSpeed * 0.35f + 1f;
        foreach (Transform t in track)
        {
            var o = t.GetComponent<Obstacle>();
            float dz = t.position.z - z;
            if (!o || dz < 0.5f || dz > look * (o.kind == Obstacle.Kind.Block ? 2f : 1f)) continue;
            bool sameLane = Mathf.Abs(t.position.x - lane * laneWidth) < 1.2f;
            if (o.kind == Obstacle.Kind.High) { if (!motion.Sliding) motion.Slide(); }
            else if (o.kind == Obstacle.Kind.Falling || (o.kind == Obstacle.Kind.Low && sameLane)) motion.Jump();
            else if (o.kind == Obstacle.Kind.Block && sameLane) lane = FreeLane(t.position.z);
        }
    }

    int FreeLane(float z) // เลนที่ไม่มีหินใหญ่ใกล้ระยะ z
    {
        for (int l = -1; l <= 1; l++)
        {
            bool blocked = false;
            foreach (Transform t in track)
            {
                var o = t.GetComponent<Obstacle>();
                if (o && o.kind == Obstacle.Kind.Block && Mathf.Abs(t.position.z - z) < 3f && Mathf.Abs(t.position.x - l * laneWidth) < 1.2f) blocked = true;
            }
            if (!blocked && l != lane) return l;
        }
        return lane;
    }

    void Spawn() // บอทตัวใหม่ตามอันดับเป้าหมาย + ย้อมเฉดสีตามลีก
    {
        rank = LeagueData.TargetRank;
        lane = 0;
        model.Show(LeagueData.BotModel(rank));
        float hue = Mathf.Repeat(LeagueHue[SaveData.D.league % LeagueHue.Length] + 0.025f * (rank - 5), 1f);
        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
        {
            var m = new Material(hueMaterial);
            m.SetTexture("_BaseMap", r.sharedMaterial.GetTexture("_BaseMap"));
            m.SetFloat("_Hue", hue);
            r.sharedMaterial = m;
        }
        nameTag.text = LeagueData.BotName(rank);
        motion.visual.localScale = Vector3.one * 1.3f;
        transform.position = new Vector3(0f, 0f, LeagueData.HeadStart);
    }

    void Show(bool on) { if (model.gameObject.activeSelf != on) { model.gameObject.SetActive(on); nameTag.gameObject.SetActive(on); } }

    IEnumerator EatRoutine(GameManager gm) // บอทหดตัวไหลเข้าหาผู้เล่น แล้วผู้เล่นตัวโต + ได้รางวัล
    {
        eating = true;
        Show(true);
        nameTag.gameObject.SetActive(false);
        Vector3 from = transform.position, scale = motion.visual.localScale;
        motion.enabled = false;                                           // หยุดท่าเด้งระหว่างถูกดูด
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(from, player.transform.position + Vector3.up * 0.5f, t / 0.3f);
            motion.visual.localScale = scale * (1f - t / 0.3f);
            yield return null;
        }
        Show(false);
        motion.enabled = true;
        int coins = 20 + 10 * (LeagueData.Bots - rank) + 50 * SaveData.D.league;   // อันดับสูง/ลีกสูง = รางวัลมากขึ้น
        int score = 500 + 100 * (LeagueData.Bots - rank) + 1000 * SaveData.D.league;
        string name = LeagueData.BotName(rank);
        GameSession.EatenThisRun++;
        MissionSystem.Add(MissionSystem.Kind.EatBots, 1);
        MissionSystem.Add(MissionSystem.Kind.EatInRun, GameSession.EatenThisRun);
        if (GameSession.ChaseHits == 0) MissionSystem.Add(MissionSystem.Kind.CleanCatch, 1);
        bool promoted = LeagueData.Eat();
        GameSession.ChaseDist = 0f;
        GameSession.ChaseHits = 0;
        gm.AddCoin(coins);
        gm.AddScore(score);
        gm.Heal(15);                                                       // ดูดพลังของบอท = ฟื้นเลือดนิดหน่อย
        player.motion.Grow(6f);                                            // ตัวโต 6 วิ แล้วค่อยๆ หดกลับ
        AudioManager.Play(AudioManager.Sfx.Eat);
        RunnerCamera.Shake(0.2f);
        gm.ui.ShowToast(promoted ? "เลื่อนขึ้นลีก " + LeagueData.Name(SaveData.D.league) + "! อัปเกรดได้ถึง Lv " + SaveData.MaxLevel
                                 : "กิน " + name + " แล้ว! +" + coins + " เหรียญ  ขึ้นอันดับ " + LeagueData.PlayerRank);
        rank = -1;
        eating = false;
    }
}
