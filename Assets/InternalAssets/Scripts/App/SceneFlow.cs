using UnityEngine;
using UnityEngine.SceneManagement;

namespace Halloween2026
{
    public static class SceneFlow
    {
        public const string MenuScene = "SCN_Menu";
        public const string GameplayScene = "SCN_Main";

        public static void StartNight()
        {
            Load(GameplayScene);
        }

        public static void OpenMenu()
        {
            Load(MenuScene);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void Load(string scene)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SceneManager.LoadScene(scene);
        }
    }
}
