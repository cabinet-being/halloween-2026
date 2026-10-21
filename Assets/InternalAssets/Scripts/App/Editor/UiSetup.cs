using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Halloween2026.Editor
{
    public static class UiSetup
    {
        private const string UiPath = "Assets/InternalAssets/UI/";
        private const string InputPath = "Assets/InternalAssets/Input/InputSystem_Actions.inputactions";

        [MenuItem("Halloween/Set Up Screens and Controls")]
        public static void SetUp()
        {
            PanelSettings panel;
            SettingsService settings;
            GameObject ui;
            GameBootstrapper game;
            GhostController ghost;
            GameplayScreen gameplay;
            MenuScreen menu;
            SessionDebugPanel debug;

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            AssetDatabase.Refresh();
            panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPath + "RuntimePanel.asset");

            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(1280, 720);
                panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panel.match = 0.5f;
                panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiPath + "RuntimeTheme.tss");
                AssetDatabase.CreateAsset(panel, UiPath + "RuntimePanel.asset");
            }

            EditorSceneManager.OpenScene(SceneSetup.MenuPath);

            if (Object.FindAnyObjectByType<MenuScreen>() == null)
            {
                settings = CreateSettings();
                ui = CreateDocument("Menu.uxml");
                menu = ui.AddComponent<MenuScreen>();
                SetReference(menu, "_settings", settings);
                SetReference(menu, "_settingsLayout", AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Settings.uxml"));
                SetReference(menu, "_pauseAction", ActionReference("System/Pause"));
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }

            EditorSceneManager.OpenScene(SceneSetup.GamePath);

            if (Object.FindAnyObjectByType<GameplayScreen>() == null)
            {
                game = Object.FindAnyObjectByType<GameBootstrapper>();
                debug = Object.FindAnyObjectByType<SessionDebugPanel>();
                if (debug != null) Object.DestroyImmediate(debug);

                settings = CreateSettings();
                ghost = CreateGhost(game, settings);
                CreatePracticeGround();
                ui = CreateDocument("Gameplay.uxml");
                gameplay = ui.AddComponent<GameplayScreen>();
                SetReference(gameplay, "_game", game);
                SetReference(gameplay, "_settings", settings);
                SetReference(gameplay, "_ghost", ghost);
                SetReference(gameplay, "_settingsLayout", AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Settings.uxml"));
                SetReference(gameplay, "_pauseAction", ActionReference("System/Pause"));
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }

            // Scene switches may unload assets that only have local managed references.
            // Resolve the panel again and repair documents on every setup run.
            ConnectPanel(SceneSetup.MenuPath);
            ConnectPanel(SceneSetup.GamePath);

            AssetDatabase.SaveAssets();
            Debug.Log("Menu, gameplay, shared settings, and input references are configured.");
        }

        private static void ConnectPanel(string path)
        {
            UIDocument document;
            PanelSettings panel;

            EditorSceneManager.OpenScene(path);
            document = Object.FindAnyObjectByType<UIDocument>();
            panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPath + "RuntimePanel.asset");
            SetReference(document, "m_PanelSettings", panel);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static SettingsService CreateSettings()
        {
            var root = new GameObject("APP_Settings");
            var settings = root.AddComponent<SettingsService>();
            var source = root.AddComponent<AudioSource>();
            var volume = root.AddComponent<MusicVolumeApplier>();

            SetReference(settings, "_input", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath));
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/InternalAssets/Audio/NightLoop.wav");
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 0f;
            source.volume = 0.15f;
            source.ignoreListenerPause = true;
            SetReference(volume, "_settings", settings);

            return settings;
        }

        private static GameObject CreateDocument(string layout)
        {
            var root = new GameObject("UI_Screen");
            var document = root.AddComponent<UIDocument>();

            document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPath + "RuntimePanel.asset");
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + layout);

            return root;
        }

        private static GhostController CreateGhost(GameBootstrapper game, SettingsService settings)
        {
            var root = new GameObject("ACT_Ghost");
            var controller = root.AddComponent<CharacterController>();
            var ghost = root.AddComponent<GhostController>();
            var pivot = new GameObject("Camera Pitch").transform;
            Camera camera = Camera.main;
            GameObject body;
            GameObject eye;
            Material bodyMaterial = Material("MAT_Ghost", new Color(0.65f, 0.9f, 0.78f));
            Material eyeMaterial = Material("MAT_GhostEyes", new Color(0.025f, 0.07f, 0.08f));
            int side;

            root.transform.position = new Vector3(0f, 1f, -5f);
            controller.height = 1.3f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.65f, 0f);
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0f, 1.7f, 0f);
            pivot.localRotation = Quaternion.Euler(12f, 0f, 0f);
            camera.transform.SetParent(pivot, false);
            camera.transform.localPosition = new Vector3(0f, 0f, -3.4f);
            camera.transform.localRotation = Quaternion.identity;

            body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Ghost Visual";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

            for (side = -1; side <= 1; side += 2)
            {
                eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "Eye";
                Object.DestroyImmediate(eye.GetComponent<Collider>());
                eye.transform.SetParent(root.transform, false);
                eye.transform.localPosition = new Vector3(side * 0.14f, 1f, 0.32f);
                eye.transform.localScale = new Vector3(0.1f, 0.16f, 0.05f);
                eye.GetComponent<Renderer>().sharedMaterial = eyeMaterial;
            }

            SetReference(ghost, "_game", game);
            SetReference(ghost, "_settings", settings);
            SetReference(ghost, "_camera", camera);
            SetReference(ghost, "_pitchPivot", pivot);
            SetReference(ghost, "_moveAction", ActionReference("Player/Move"));
            SetReference(ghost, "_lookAction", ActionReference("Player/Look"));
            SetReference(ghost, "_primaryAction", ActionReference("Player/Attack"));
            SetReference(ghost, "_secondaryAction", ActionReference("Player/Interact"));

            return ghost;
        }

        private static void CreatePracticeGround()
        {
            Material ground = Material("MAT_Ground", new Color(0.17f, 0.23f, 0.23f));
            Material path = Material("MAT_Path", new Color(0.27f, 0.3f, 0.3f));
            Material house = Material("MAT_House", new Color(0.27f, 0.2f, 0.31f));
            Material pumpkin = Material("MAT_Pumpkin", new Color(0.9f, 0.38f, 0.09f));
            GameObject item;
            int index;
            int side;

            Cube("Practice Ground", new Vector3(0f, -0.3f, 0f), new Vector3(48f, 0.5f, 48f), ground);
            Cube("Street", new Vector3(0f, -0.04f, 0f), new Vector3(8f, 0.05f, 44f), path);

            for (index = 0; index < 3; index++)
            {
                for (side = -1; side <= 1; side += 2)
                {
                    Cube("Blockout House", new Vector3(side * 10f, 2.5f, index * 10f - 8f), new Vector3(5f, 5f, 5f), house);
                    item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    item.name = "Pumpkin Marker";
                    item.transform.position = new Vector3(side * 5f, 0.45f, index * 10f - 8f);
                    item.transform.localScale = new Vector3(1f, 0.8f, 1f);
                    item.GetComponent<Renderer>().sharedMaterial = pumpkin;
                }
            }

            Cube("Boundary North", new Vector3(0f, 1f, 23f), new Vector3(48f, 2f, 1f), house);
            Cube("Boundary South", new Vector3(0f, 1f, -23f), new Vector3(48f, 2f, 1f), house);
            Cube("Boundary East", new Vector3(23f, 1f, 0f), new Vector3(1f, 2f, 48f), house);
            Cube("Boundary West", new Vector3(-23f, 1f, 0f), new Vector3(1f, 2f, 48f), house);
        }

        private static void Cube(string name, Vector3 position, Vector3 scale, Material material)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);

            item.name = name;
            item.transform.position = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material Material(string name, Color color)
        {
            string path = "Assets/InternalAssets/Data/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null) return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);

            return material;
        }

        private static InputActionReference ActionReference(string path)
        {
            InputActionAsset input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            InputAction action = input.FindAction(path, true);

            return AssetDatabase.LoadAllAssetsAtPath(InputPath).OfType<InputActionReference>()
                .First(reference => reference.action.id == action.id);
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var data = new SerializedObject(target);

            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
