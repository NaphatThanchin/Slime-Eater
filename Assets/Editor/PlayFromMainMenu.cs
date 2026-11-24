using UnityEditor;
using UnityEditor.SceneManagement;

// กด Play ที่ฉากไหนก็เริ่มจากหน้า MainMenu (ปิดได้ที่ Tools > Slime Eater)
[InitializeOnLoad]
public static class PlayFromMainMenu
{
    const string Key = "SlimeEater.PlayFromMenu";
    const string MenuPath = "Tools/Slime Eater/Play From Main Menu";
    const string ScenePath = "Assets/Scenes/MainMenu.unity";

    static PlayFromMainMenu() => EditorApplication.delayCall += Apply;

    static void Apply()
    {
        bool on = EditorPrefs.GetBool(Key, true);
        EditorSceneManager.playModeStartScene = on ? AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) : null;
        Menu.SetChecked(MenuPath, on);
    }

    [MenuItem(MenuPath, priority = 30)]
    static void Toggle()
    {
        EditorPrefs.SetBool(Key, !EditorPrefs.GetBool(Key, true));
        Apply();
    }
}
