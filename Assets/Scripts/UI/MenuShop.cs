using TMPro;
using UnityEngine;

// หน้าเมนู: เหรียญ/สถิติ/ลีก และร้านค้า (อัปเกรด + ตัวละคร)
public class MenuShop : MonoBehaviour
{
    public GameObject shopPanel;
    public TMP_Text bankText, bestScoreText, bestStageText, leagueText;
    public TMP_Text[] upgradeLevelTexts, upgradePriceTexts;   // แม่เหล็ก / เกราะ / เลือด
    public TMP_Text[] charStatusTexts;                        // ใต้การ์ดตัวละคร
    public CharacterModel menuModel;                          // สไลม์ตัวใหญ่หน้าเมนู
    [Header("หน้าดูตัวละคร")]
    public GameObject charPopup, previewStage;
    public CharacterModel previewModel;                       // โมเดลหมุนโชว์ (กล้องแยก -> RenderTexture)
    public TMP_Text popName, popInfo, popButtonText, popMessage;
    public GameObject popCoin;
    int viewing;

    void Start()
    {
        shopPanel.SetActive(false);
        charPopup.SetActive(false);
        MissionSystem.CheckAll();                             // ภารกิจที่ถึงเป้าแล้วรับรางวัลเลย
        Refresh();
    }

    public void OpenShop() { shopPanel.SetActive(true); Refresh(); }
    public void CloseShop() { CloseCharacter(); shopPanel.SetActive(false); }

    public void BuyUpgrade(int i) // ซื้ออัปเกรดขั้นถัดไป (เพดานตามลีก)
    {
        var u = (SaveData.Upgrade)i;
        if (SaveData.Level(u) >= SaveData.MaxLevel || !SaveData.TryBuy(SaveData.UpgradePrice(u))) return;
        SaveData.LevelUp(u);
        Refresh();
    }

    public void ViewCharacter(int i) // กดการ์ด -> ดูโมเดล + ความสามารถก่อนซื้อ
    {
        viewing = i;
        previewStage.SetActive(true);
        previewModel.Show(i);
        previewModel.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
        popMessage.text = "";
        charPopup.SetActive(true);
        Refresh();
    }

    public void CloseCharacter() { charPopup.SetActive(false); previewStage.SetActive(false); }

    public void ConfirmCharacter() // ซื้อ (ถ้ายังไม่มี) แล้วใช้ตัวนี้
    {
        if (!SaveData.OwnsCharacter(viewing))
        {
            if (!SaveData.TryBuy(CharacterData.Prices[viewing])) { popMessage.text = "เหรียญไม่พอ! ขาดอีก " + (CharacterData.Prices[viewing] - SaveData.Bank).ToString("N0"); return; }
            SaveData.UnlockCharacter(viewing);
            popMessage.text = "ซื้อสำเร็จ!";
        }
        SaveData.Character = viewing;
        menuModel.Show(viewing);
        Refresh();
    }

    public void Refresh() // อัปเดตตัวเลขทุกจุด
    {
        bankText.text = SaveData.Bank.ToString("N0");
        bestScoreText.text = SaveData.BestScore.ToString("N0");
        bestStageText.text = SaveData.BestStage.ToString();
        leagueText.text = LeagueData.Name(SaveData.D.league) + "  #" + LeagueData.PlayerRank;
        for (int i = 0; i < upgradeLevelTexts.Length; i++)
        {
            int lv = SaveData.Level((SaveData.Upgrade)i);
            upgradeLevelTexts[i].text = "Lv " + lv + " / " + SaveData.MaxLevel;
            upgradePriceTexts[i].text = lv >= SaveData.MaxLevel ? "เต็ม" : SaveData.UpgradePrice((SaveData.Upgrade)i).ToString("N0");
        }
        for (int i = 0; i < charStatusTexts.Length; i++) charStatusTexts[i].text = Status(i, true);
        if (!charPopup.activeSelf) return;
        popName.text = CharacterData.Names[viewing];
        popInfo.text = CharacterData.Info(viewing);
        popButtonText.text = Status(viewing, false);
        popCoin.SetActive(!SaveData.OwnsCharacter(viewing));
    }

    static string Status(int i, bool card) => SaveData.Character == i ? "ใช้อยู่" : SaveData.OwnsCharacter(i) ? (card ? "มีแล้ว" : "ใช้ตัวนี้") : CharacterData.Prices[i].ToString("N0");
}
