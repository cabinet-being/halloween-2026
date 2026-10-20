using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Halloween2026.Editor
{
    public static class FoundationSetup
    {
        private const string ScenePath = "Assets/InternalAssets/Scenes/SCN_Main.unity";
        private const string DataPath = "Assets/InternalAssets/Data";

        [MenuItem("Halloween/Set Up Foundation Scene")]
        public static void SetUp()
        {
            NightSettings settings;
            SecretDefinition secret;
            SerializedObject secretData;
            GameObject root;
            GameBootstrapper game;
            SerializedObject gameData;
            SerializedProperty secrets;
            Light moonLight;
            Camera camera;
            GameObject moon;
            Material material;
            NightEnvironment environment;

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EditorSceneManager.OpenScene(ScenePath);
            if (Object.FindFirstObjectByType<GameBootstrapper>() != null) return;

            Directory.CreateDirectory(DataPath);
            AssetDatabase.Refresh();

            settings = LoadOrCreate<NightSettings>(DataPath + "/NightSettings.asset");
            secret = LoadOrCreate<SecretDefinition>(DataPath + "/SEC_FirstSecret.asset");
            secretData = new SerializedObject(secret);

            secretData.FindProperty("_id").stringValue = "first-secret";
            secretData.FindProperty("_title").stringValue = "First discovery";
            secretData.FindProperty("_description").stringValue = "Placeholder for the first Halloween secret.";
            secretData.ApplyModifiedPropertiesWithoutUndo();

            root = new GameObject("APP_Game");
            game = root.AddComponent<GameBootstrapper>();
            gameData = new SerializedObject(game);
            gameData.FindProperty("_settings").objectReferenceValue = settings;
            secrets = gameData.FindProperty("_secrets");
            secrets.arraySize = 1;
            secrets.GetArrayElementAtIndex(0).objectReferenceValue = secret;
            gameData.ApplyModifiedPropertiesWithoutUndo();

            moonLight = null;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional) continue;

                moonLight = light;
                break;
            }

            if (moonLight == null)
            {
                moonLight = new GameObject("Moon Light").AddComponent<Light>();
                moonLight.type = LightType.Directional;
            }

            moonLight.gameObject.name = "Moon Light";

            // A plain background makes the authored color gradient deterministic in this scaffold.
            camera = Camera.main;
            if (camera == null) camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, 200f);

            moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moon.name = "Moon Visual";
            Object.DestroyImmediate(moon.GetComponent<Collider>());
            moon.transform.localScale = Vector3.one * 8f;

            material = AssetDatabase.LoadAssetAtPath<Material>(DataPath + "/MAT_Moon.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetColor("_BaseColor", new Color(0.85f, 0.9f, 1f));
                AssetDatabase.CreateAsset(material, DataPath + "/MAT_Moon.mat");
            }

            moon.GetComponent<Renderer>().sharedMaterial = material;

            environment = root.AddComponent<NightEnvironment>();
            SetReference(environment, "_game", game);
            SetReference(environment, "_moonLight", moonLight);
            SetReference(environment, "_moonVisual", moon.transform);
            SetReference(environment, "_viewCamera", camera);
            SetReference(root.AddComponent<SessionDebugPanel>(), "_game", game);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Halloween foundation scene configured.");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);

            return asset;
        }

        private static void SetReference(Object target, string name, Object value)
        {
            var serialized = new SerializedObject(target);

            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
