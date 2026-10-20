using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Halloween2026.Tests
{
    public sealed class FoundationSceneTests
    {
        [UnityTest]
        public IEnumerator ConfiguredSceneRunsPausesAndDrivesMoon()
        {
            GameBootstrapper game;
            Transform moon;
            Vector3 before;
            float pausedAt;
            Color expected;

            EditorSceneManager.OpenScene("Assets/InternalAssets/Scenes/SCN_Main.unity");
            yield return new EnterPlayMode();
            yield return null;

            game = Object.FindFirstObjectByType<GameBootstrapper>();

            Assert.IsNotNull(game);
            Assert.AreEqual(SessionState.Running, game.Session.State);
            Assert.Greater(game.Session.Elapsed, 0f);
            Assert.AreEqual(1, game.Secrets.Count);

            moon = GameObject.Find("Moon Visual").transform;
            before = moon.position;

            game.SetPaused(true);
            pausedAt = game.Session.Elapsed;
            yield return null;

            Assert.AreEqual(pausedAt, game.Session.Elapsed);

            game.SetPaused(false);
            game.Session.Tick(game.Settings.DurationSeconds * 0.5f);
            yield return null;

            Assert.Greater(Vector3.Distance(before, moon.position), 10f);

            expected = game.Settings.SkyColor.Evaluate(game.Session.Progress);
            Assert.Less(Vector4.Distance(expected, Camera.main.backgroundColor), 0.01f);

            yield return new ExitPlayMode();
        }
    }
}
