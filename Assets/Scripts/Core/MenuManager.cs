using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// หน้าเมนูหลัก: PLAY (ตั้งชื่อก่อนเล่น) / SETTINGS (เสียง + บันทึกเกม) / EXIT
public class MenuManager : MonoBehaviour
{
    public string runnerScene = "RunnerScene";
    public GameObject settingPanel;
    public TMP_Text saveStatus;      // เวลาบันทึกล่าสุด
    public GameObject namePanel;     // หน้าตั้งชื่อ
    public TMP_InputField nameInput;
    public TMP_Text nameWarning;

    void Start()
    {
        Time.timeScale = 1f;
        settingPanel.SetActive(false);
        namePanel.SetActive(false);
    }

    // ปุ่ม PLAY -> เปิดหน้าตั้งชื่อ (ใส่ชื่อเดิมไว้ให้)
    public void StartGame()
    {
        nameInput.text = SaveData.PlayerName;
        nameWarning.text = "";
        namePanel.SetActive(true);
    }

    public void ConfirmName() // บันทึกชื่อแล้วเข้าเกม
    {
        string n = nameInput.text.Trim();
        if (n.Length == 0) { nameWarning.text = "กรุณาใส่ชื่อก่อนเริ่มเกม"; return; }
        SaveData.PlayerName = n;
        GameSession.Reset();
        SceneManager.LoadScene(runnerScene);
    }

    public void CloseName() => namePanel.SetActive(false);

    // ปุ่ม SETTINGS
    public void OpenSetting() { settingPanel.SetActive(true); ShowSaveTime(); }
    public void CloseSetting() { SaveData.Save(); settingPanel.SetActive(false); } // ปิดแล้วบันทึกเสียงด้วย
    public void SaveGame() { SaveData.Save(); ShowSaveTime(); }                  // ปุ่มบันทึกเกม
    void ShowSaveTime() => saveStatus.text = SaveData.D.savedAt == "" ? "ยังไม่เคยบันทึก" : "บันทึกล่าสุด " + SaveData.D.savedAt;

    // ปุ่ม EXIT
    public void ExitGame()
    {
        SaveData.Save();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
