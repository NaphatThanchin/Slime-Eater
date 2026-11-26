using TMPro;
using UnityEngine;
using UnityEngine.UI;

// หน้าตารางลีก: บอทที่ยังเหลือ (บน) -> ผู้เล่น -> บอทที่ถูกกินแล้ว (ล่าง)
public class LeaguePanel : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text title, info;
    public Image[] rowBg;                // 10 แถว
    public TMP_Text[] rowText, rowPoints;
    static readonly Color Me = new Color(1f, 0.75f, 0.2f), Target = new Color(0.9f, 0.35f, 0.3f), Normal = new Color(0.2f, 0.5f, 0.55f), Eaten = new Color(0.35f, 0.35f, 0.35f);

    void Start() => panel.SetActive(false);
    public void Close() => panel.SetActive(false);

    public void Open()
    {
        var d = SaveData.D;
        int left = d.botsLeft;
        title.text = "ลีก " + LeagueData.Name(d.league) + "  |  " + LeagueData.Region(d.league);
        for (int row = 0; row <= LeagueData.Bots; row++)
        {
            int rank = row + 1;
            bool me = row == left, eaten = row > left;
            int botRank = eaten ? row : rank;                                  // บอทที่ถูกกินเลื่อนลงไปใต้ผู้เล่น
            string name = me ? (d.playerName == "" ? "คุณ" : d.playerName) + " (คุณ)" : LeagueData.BotName(botRank) + (eaten ? "  ถูกกินแล้ว" : rank == left ? "  (เป้าหมาย)" : "");
            rowText[row].text = rank + ".  " + name;
            rowPoints[row].text = me ? d.leaguePoints.ToString("N0") : eaten ? "-" : LeagueData.BotPoints(botRank).ToString("N0");
            rowBg[row].color = me ? Me : eaten ? Eaten : rank == left ? Target : Normal;
        }
        info.text = "เป้าหมาย: " + LeagueData.BotName(left) + "  วิ่งเร็วกว่าคุณ " + Mathf.RoundToInt((LeagueData.BotSpeed(left) - 1f) * 100f) + "%  ต้องวิ่งให้ถึง ~" + Mathf.RoundToInt(LeagueData.CatchDistance).ToString("N0") + " m"
                  + "\nขึ้นอันดับ 1 = เลื่อนลีก + อัปเกรดได้ถึง Lv " + (SaveData.MaxLevel + 5);
        panel.SetActive(true);
    }
}
