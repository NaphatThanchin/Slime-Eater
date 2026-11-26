using TMPro;
using UnityEngine;
using UnityEngine.UI;

// หน้าเมนู: เหรียญ/สถิติ, ร้านค้า (อัปเกรด + ตัวละคร), ภารกิจ 9 หน้า
public class MenuShop : MonoBehaviour
{
    public GameObject shopPanel, missionPanel;
    public TMP_Text bankText, bestScoreText, bestStageText;
    public TMP_Text[] upgradeLevelTexts, upgradePriceTexts;   // แม่เหล็ก / เกราะ / เลือด
    public TMP_Text[] charStatusTexts;                        // ใต้การ์ดตัวละคร
    public CharacterModel menuModel;                          // สไลม์ตัวใหญ่หน้าเมนู
    [Header("หน้าดูตัวละคร")]
    public GameObject charPopup, previewStage;
    public CharacterModel previewModel;                       // โมเดลหมุนโชว์ (กล้องแยก -> RenderTexture)
    public TMP_Text popName, popInfo, popButtonText, popMessage;
    public GameObject popCoin;
    [Header("ภารกิจ")]
    public GameObject[] pageButtons;                          // ปุ่มหน้า 1-9 (หน้าที่ทำครบจะหายไป)
    public GameObject[] missionRows;                          // 3 แถวของหน้าที่เลือก
    public TMP_Text[] missionTexts;
    public Image[] missionBars;
    public TMP_Text pageTitle;
    int viewing, page;

    void Start()
    {
        shopPanel.SetActive(false);
        missionPanel.SetActive(false);
        charPopup.SetActive(false);
        MissionSystem.OnCompleted += OnMission;
        MissionSystem.CheckAll();
        Refresh();
    }

    void OnDestroy() => MissionSystem.OnCompleted -= OnMission;
    void OnMission(string _) => Refresh();

    public void OpenShop() { shopPanel.SetActive(true); Refresh(); }
    public void CloseShop() { CloseCharacter(); shopPanel.SetActive(false); }
    public void CloseMissions() => missionPanel.SetActive(false);

    public void OpenMissions() // เปิดที่หน้าแรกที่ยังทำไม่ครบ
    {
        page = FirstOpenPage();
        missionPanel.SetActive(true);
        Refresh();
    }

    public void ShowPage(int p) { page = p; Refresh(); }

    public void BuyUpgrade(int i) // ซื้ออัปเกรดขั้นถัดไป
    {
        var u = (SaveData.Upgrade)i;
        if (SaveData.Level(u) >= SaveData.MaxUpgrade || !SaveData.TryBuy(SaveData.UpgradePrice(u))) return;
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

    void Refresh() // อัปเดตตัวเลขทุกหน้า
    {
        bankText.text = SaveData.Bank.ToString("N0");
        bestScoreText.text = SaveData.BestScore.ToString("N0");
        bestStageText.text = SaveData.BestStage.ToString();
        for (int i = 0; i < upgradeLevelTexts.Length; i++)
        {
            int lv = SaveData.Level((SaveData.Upgrade)i);
            upgradeLevelTexts[i].text = "Lv " + lv + " / " + SaveData.MaxUpgrade;
            upgradePriceTexts[i].text = lv >= SaveData.MaxUpgrade ? "เต็ม" : SaveData.UpgradePrice((SaveData.Upgrade)i).ToString("N0");
        }
        for (int i = 0; i < charStatusTexts.Length; i++) charStatusTexts[i].text = Status(i, true);
        if (charPopup.activeSelf)
        {
            popName.text = CharacterData.Names[viewing];
            popInfo.text = CharacterData.Info(viewing);
            popButtonText.text = Status(viewing, false);
            popCoin.SetActive(!SaveData.OwnsCharacter(viewing));
        }
        RefreshMissions();
    }

    void RefreshMissions() // ซ่อนหน้าที่ทำครบ + ซ่อนภารกิจที่ทำเสร็จ
    {
        bool allDone = true;
        for (int p = 0; p < pageButtons.Length; p++) { pageButtons[p].SetActive(!MissionSystem.PageDone(p)); allDone &= MissionSystem.PageDone(p); }
        if (MissionSystem.PageDone(page)) page = FirstOpenPage();                 // หน้าเดิมทำครบ -> ไปหน้าที่ยังเหลือ
        pageTitle.text = allDone ? "ทำภารกิจครบทุกหน้าแล้ว!" : "หน้า " + (page + 1) + " / " + MissionSystem.Pages;
        for (int r = 0; r < missionRows.Length; r++)
        {
            int i = page * MissionSystem.PerPage + r;
            missionRows[r].SetActive(!allDone && !MissionSystem.Done(i));
            missionTexts[r].text = MissionSystem.Title(i) + "\n<size=70%>" + MissionSystem.Progress(i).ToString("N0") + " / " + MissionSystem.Target(i).ToString("N0") + "   รางวัล " + MissionSystem.Reward(i).ToString("N0") + " เหรียญ</size>";
            missionBars[r].fillAmount = (float)MissionSystem.Progress(i) / MissionSystem.Target(i);
        }
    }

    static int FirstOpenPage() // หน้าแรกที่ยังทำไม่ครบ
    {
        int p = 0;
        while (p < MissionSystem.Pages - 1 && MissionSystem.PageDone(p)) p++;
        return p;
    }

    static string Status(int i, bool card) => SaveData.Character == i ? "ใช้อยู่" : SaveData.OwnsCharacter(i) ? (card ? "มีแล้ว" : "ใช้ตัวนี้") : CharacterData.Prices[i].ToString("N0");
}
