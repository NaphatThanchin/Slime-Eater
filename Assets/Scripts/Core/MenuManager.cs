using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public string runnerScene = "RunnerScene";
    public GameObject settingPanel; // หน้าตั้งค่า

    void Start()
    {
        Time.timeScale = 1f;
        if (settingPanel) settingPanel.SetActive(false);
    }

    // ปุ่ม START
    public void StartGame()
    {
        GameSession.Reset();
        Debug.Log("[Menu] Start Game -> " + runnerScene);
        SceneManager.LoadScene(runnerScene);
    }

    // ปุ่ม SETTING
    public void OpenSetting()
    {
        Debug.Log("[Menu] Open Setting");
        if (settingPanel) settingPanel.SetActive(true);
    }

    public void CloseSetting()
    {
        if (settingPanel) settingPanel.SetActive(false);
    }

    // ปุ่ม EXIT
    public void ExitGame()
    {
        Debug.Log("[Menu] Exit Game");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
