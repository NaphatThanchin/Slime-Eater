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
    public GameObject resetPanel;    // ยืนยันลบข้อมูล

    public GameObject exitButton;    // ออกเกม (บนเว็บ = ปิดเกมแล้วขึ้นหน้าขอบคุณ)
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void SlimeExit(); // Plugins/WebGL/WebExit.jslib
#endif

    void Start()
    {
        Time.timeScale = 1f;
        settingPanel.SetActive(false);
        namePanel.SetActive(false);
        resetPanel.SetActive(false);
    }

    // ปุ่ม PLAY -> เปิดหน้าตั้งชื่อ (ใส่ชื่อเดิมไว้ให้)
    public void StartGame()
    {
        nameInput.text = SaveData.PlayerName;
        nameWarning.text = "";
        namePanel.SetActive(true);
        if (!PhoneKeyboard.Phone) { nameInput.Select(); nameInput.ActivateInputField(); nameInput.caretPosition = nameInput.text.Length; } // คอม: พิมพ์ได้ทันที Enter = เริ่ม
    }

    public void ConfirmName() // บันทึกชื่อแล้วเข้าเกม
    {
        string n = nameInput.text.Trim();
        if (n.Length == 0) { nameWarning.text = "กรุณาใส่ชื่อก่อนเริ่มเกม"; return; }
        SaveData.PlayerName = n;
        namePanel.SetActive(false);                       // ปิดกล่องพิมพ์ + แป้นพิมพ์มือถือทันที
        GameSession.Reset();
        SceneManager.LoadScene(runnerScene);
    }

    void OnEnable() { if (nameInput) nameInput.onSubmit.AddListener(OnNameSubmit); }   // คอม: กด Enter = เข้าเกม
    void OnDisable() { if (nameInput) nameInput.onSubmit.RemoveListener(OnNameSubmit); }
    void OnNameSubmit(string _) { if (namePanel.activeInHierarchy) ConfirmName(); }

    public void CloseName() => namePanel.SetActive(false);

    // ปุ่ม SETTINGS
    public void OpenSetting() { settingPanel.SetActive(true); ShowSaveTime(); }
    public void CloseSetting() { SaveData.Save(); settingPanel.SetActive(false); } // ปิดแล้วบันทึกเสียงด้วย
    public void SaveGame() { SaveData.Save(); ShowSaveTime(); }                  // ปุ่มบันทึกเกม
    void ShowSaveTime() => saveStatus.text = SaveData.D.savedAt == "" ? "ยังไม่เคยบันทึก" : "บันทึกล่าสุด " + SaveData.D.savedAt;
    public void ReplayTutorial() { SaveData.D.tutorialDone = false; SaveData.Save(); saveStatus.text = "รอบหน้าจะสอนเล่นอีกครั้ง"; }

    // รีเซ็ตข้อมูล (ถามยืนยันก่อน)
    public void AskReset() => resetPanel.SetActive(true);
    public void CancelReset() => resetPanel.SetActive(false);
    public void ConfirmReset() { SaveData.ResetAll(); SceneManager.LoadScene(SceneManager.GetActiveScene().name); } // โหลดเมนูใหม่ให้ตัวเลขเป็นค่าเริ่มต้น

    // ปุ่ม EXIT
    public void ExitGame()
    {
        SaveData.Save();
#if UNITY_WEBGL && !UNITY_EDITOR
        SlimeExit();                 // เว็บ: ออกจากเต็มจอ หยุดเกม/เสียง แล้วโชว์หน้า "ออกจากเกมแล้ว"
#endif
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
