using TMPro;
using UnityEngine;
using UnityEngine.UI;

// หน้าเมนู: ร้านค้า (อัปเกรด + ตัวละคร) และหน้าภารกิจ ใช้เหรียญสะสมจาก SaveData
public class MenuShop : MonoBehaviour
{
    static readonly string[] UpgradeKeys = { "Magnet", "Shield", "Hp" };
    public GameObject shopPanel;
    public GameObject missionPanel;
    public TMP_Text bankText;               // เหรียญสะสม
    public TMP_Text bestText;               // สถิติสูงสุด
    public TMP_Text[] upgradeLevelTexts;    // แม่เหล็ก / เกราะ / เลือด
    public TMP_Text[] upgradePriceTexts;
    public TMP_Text[] charStatusTexts;      // ใต้การ์ดตัวละคร (ราคา / เลือก / ใช้อยู่)
    public TMP_Text[] missionTexts;
    public Image[] missionBars;
    public CharacterModel menuModel;        // สไลม์ตัวใหญ่หน้าเมนู = ตัวที่ใช้อยู่
    [Header("หน้าดูตัวละคร")]
    public GameObject charPopup;
    public CharacterModel previewModel;     // โมเดลหมุนโชว์ (กล้องแยก -> RenderTexture)
    public GameObject previewStage;         // เปิดเฉพาะตอนดูตัวละคร
    public TMP_Text popName, popInfo, popButtonText, popMessage;
    public GameObject popCoin;              // ไอคอนเหรียญบนปุ่ม (โชว์ตอนยังไม่ได้ซื้อ)
    int viewing;

    void Start()
    {
        if (shopPanel) shopPanel.SetActive(false);
        if (missionPanel) missionPanel.SetActive(false);
        if (charPopup) charPopup.SetActive(false);
        Refresh();
    }

    public void OpenShop() { shopPanel.SetActive(true); Refresh(); }
    public void CloseShop() { CloseCharacter(); shopPanel.SetActive(false); }
    public void OpenMissions() { missionPanel.SetActive(true); Refresh(); }
    public void CloseMissions() { missionPanel.SetActive(false); }

    public void BuyUpgrade(int i) // ซื้ออัปเกรดระดับถัดไป
    {
        string key = UpgradeKeys[i];
        if (SaveData.Upgrade(key) >= SaveData.MaxUpgrade || !SaveData.TryBuy(SaveData.UpgradePrice(key))) return;
        SaveData.SetUpgrade(key, SaveData.Upgrade(key) + 1);
        Debug.Log("[Shop] อัปเกรด " + key + " -> Lv" + SaveData.Upgrade(key));
        Refresh();
    }

    public void ViewCharacter(int i) // กดการ์ด -> เปิดหน้าดูโมเดล + ความสามารถก่อนซื้อ
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

    public void ConfirmCharacter() // ปุ่มในหน้าดูตัวละคร: ซื้อ (ถ้ายังไม่มี) แล้วใช้ตัวนี้
    {
        if (!SaveData.OwnsCharacter(viewing))
        {
            if (!SaveData.TryBuy(CharacterData.Prices[viewing])) { popMessage.text = "เหรียญไม่พอ! ขาดอีก " + (CharacterData.Prices[viewing] - SaveData.Bank).ToString("N0"); return; }
            SaveData.UnlockCharacter(viewing);
            popMessage.text = "ซื้อสำเร็จ!";
            Debug.Log("[Shop] ซื้อตัวละคร " + CharacterData.Names[viewing]);
        }
        SaveData.Character = viewing;
        if (menuModel) menuModel.Show(viewing);
        Refresh();
    }

    void Refresh()
    {
        if (bankText) bankText.text = SaveData.Bank.ToString("N0");
        if (bestText) bestText.text = "สถิติ " + SaveData.BestScore.ToString("N0") + "  |  ด่านสูงสุด " + SaveData.BestStage;
        for (int i = 0; i < UpgradeKeys.Length; i++)
        {
            int lv = SaveData.Upgrade(UpgradeKeys[i]);
            if (i < upgradeLevelTexts.Length) upgradeLevelTexts[i].text = "Lv " + lv + " / " + SaveData.MaxUpgrade;
            if (i < upgradePriceTexts.Length) upgradePriceTexts[i].text = lv >= SaveData.MaxUpgrade ? "เต็ม" : SaveData.UpgradePrice(UpgradeKeys[i]).ToString();
        }
        for (int i = 0; i < charStatusTexts.Length; i++) charStatusTexts[i].text = Status(i, true);
        if (charPopup && charPopup.activeSelf)
        {
            popName.text = CharacterData.Names[viewing];
            popInfo.text = CharacterData.Info(viewing);
            popButtonText.text = Status(viewing, false);
            popCoin.SetActive(!SaveData.OwnsCharacter(viewing));
        }
        for (int s = 0; s < MissionSystem.Count && s < missionTexts.Length; s++)
        {
            missionTexts[s].text = MissionSystem.Title(s) + "\n<size=70%>" + MissionSystem.Progress(s) + " / " + MissionSystem.Target(s) + "   รางวัล " + MissionSystem.Reward(s) + " เหรียญ</size>";
            if (s < missionBars.Length) missionBars[s].fillAmount = Mathf.Clamp01((float)MissionSystem.Progress(s) / MissionSystem.Target(s));
        }
    }

    static string Status(int i, bool card) => SaveData.Character == i ? "ใช้อยู่" : SaveData.OwnsCharacter(i) ? (card ? "มีแล้ว" : "ใช้ตัวนี้") : CharacterData.Prices[i].ToString("N0");
}
