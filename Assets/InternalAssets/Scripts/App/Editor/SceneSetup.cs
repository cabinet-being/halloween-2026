using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Halloween2026.Editor
{
    public static class SceneSetup
    {
        public const string MenuPath = "Assets/InternalAssets/Scenes/SCN_Menu.unity";
        public const string GamePath = "Assets/InternalAssets/Scenes/SCN_Main.unity";

        [MenuItem("Halloween/Create Base Scenes")]
        public static void CreateScenes()
        {
            Scene scene;
            Camera camera;

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath) == null)
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                camera = new GameObject("Menu Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.025f, 0.08f);
                camera.gameObject.AddComponent<AudioListener>();

                EditorSceneManager.SaveScene(scene, MenuPath);
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuPath, true),
                new EditorBuildSettingsScene(GamePath, true)
            };

            AssetDatabase.SaveAssets();
        }
    }
}
