using TMPro;
using UnityEngine;

// หน้าเมนู: เหรียญ/สถิติ/ลีก และร้านค้า (อัปเกรด + ตัวละคร)
public class MenuShop : MonoBehaviour
{
    public GameObject shopPanel;
    public TMP_Text bankText, bestScoreText, bestStageText, totalDistText;
    public TMP_Text[] upgradeLevelTexts, upgradePriceTexts;   // แม่เหล็ก / เกราะ / เลือด (ของตัวที่ใช้อยู่)
    public TMP_Text upgradeOwnerText;                         // "อัปเกรดของ SlimeKing"
    public TMP_Text bubbleText, potionText;
    public GameObject missionBadge;                           // จุดแดงบนปุ่มภารกิจ = มีรางวัลรอรับ
    public TMP_Text missionBadgeText;                   // ของใช้: ฟองสไลม์ / ยาฟื้นสไลม์ (จำนวนที่มี)
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

    public void BuyBubble() { if (SaveData.TryBuy(SaveData.BubblePrice)) { SaveData.D.bubbles++; SaveData.Save(); } Refresh(); }
    public void BuyPotion() { if (SaveData.TryBuy(SaveData.PotionPrice)) { SaveData.D.potions++; SaveData.Save(); } Refresh(); }

    public void ConfirmCharacter() // ซื้อ (ถ้ายังไม่มี) / กดรับความสามารถที่เควสครบ / ใช้ตัวนี้
    {
        if (CharacterData.CanClaim(viewing)) { CharacterData.Claim(viewing); popMessage.text = "ปลดความสามารถขั้น " + CharacterData.Tier(viewing) + " แล้ว!"; Refresh(); return; }
        if (!SaveData.OwnsCharacter(viewing))
        {
            if (!CharacterData.CanBuy(viewing)) { popMessage.text = "ยังซื้อไม่ได้: " + CharacterData.BuyNeedText(viewing); return; } // ต้องทำเงื่อนไขปลดก่อน
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
        if (totalDistText) totalDistText.text = SaveData.D.stats[(int)MissionSystem.Kind.Distance].ToString("N0") + " m";
        int pending = MissionSystem.PendingCount;
        for (int c = 0; c < CharacterData.Count; c++) if (CharacterData.CanClaim(c)) pending++;     // ความสามารถตัวละครรอกดรับ
        if (missionBadge) { missionBadge.SetActive(pending > 0); missionBadgeText.text = pending.ToString(); }
        if (bubbleText) bubbleText.text = "ฟองสไลม์ (มี " + SaveData.D.bubbles + ")  " + SaveData.BubblePrice;
        if (potionText) potionText.text = "ยาฟื้น (มี " + SaveData.D.potions + ")  " + SaveData.PotionPrice.ToString("N0");
        if (upgradeOwnerText) upgradeOwnerText.text = "อัปเกรดของ " + CharacterData.Names[CharacterData.Current] + " (สูงสุด Lv " + SaveData.MaxLevel + " ปลดเพิ่มด้วยเควสตัวละคร)";
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

    static string Status(int i, bool card) => CharacterData.CanClaim(i) ? "รับความสามารถ!" : SaveData.Character == i ? "ใช้อยู่" : SaveData.OwnsCharacter(i) ? (card ? "มีแล้ว" : "ใช้ตัวนี้") : !CharacterData.CanBuy(i) ? "ล็อก" : CharacterData.Prices[i].ToString("N0");
}
