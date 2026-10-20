using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Halloween2026.Tests
{
    public sealed class FoundationTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "halloween-tests-" + Guid.NewGuid());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void DawnCompletesOnceAndRejectsFurtherAwards()
        {
            var session = new GameSession(10f);
            int completions = 0;

            session.Completed += () => completions++;

            Assert.IsFalse(session.Award(ScoreSource.Candy, 10));

            session.Start();

            Assert.IsTrue(session.Award(ScoreSource.Candy, 10));

            session.Tick(20f);
            session.Tick(20f);

            Assert.AreEqual(1, completions);
            Assert.AreEqual(1f, session.Progress);
            Assert.AreEqual(0f, session.Remaining);
            Assert.AreEqual(10, session.Breakdown[ScoreSource.Candy]);
            Assert.IsFalse(session.Award(ScoreSource.Scare, 25));
            Assert.IsFalse(session.Start());
        }

        [Test]
        public void PauseFreezesClockAndAwards()
        {
            var session = new GameSession(10f);

            session.Start();
            session.Tick(2f);

            session.SetPaused(true);
            session.Tick(50f);

            Assert.AreEqual(2f, session.Elapsed);
            Assert.IsFalse(session.Award(ScoreSource.Candy, 10));

            session.SetPaused(false);
            session.Tick(3f);

            Assert.AreEqual(5f, session.Elapsed);
        }

        [Test]
        public void InvalidTimeAndOverflowCannotCorruptSession()
        {
            var session = new GameSession(10f);

            Assert.Throws<ArgumentOutOfRangeException>(() => new GameSession(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameSession(0f));

            session.Start();

            Assert.Throws<ArgumentOutOfRangeException>(() => session.Tick(float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.Tick(-1f));
            Assert.IsFalse(session.Award(ScoreSource.Candy, -1));
            Assert.IsFalse(session.Award((ScoreSource)999, 1));
            Assert.IsTrue(session.Award(ScoreSource.Candy, int.MaxValue));
            Assert.IsFalse(session.Award(ScoreSource.Candy, 1));
            Assert.AreEqual(int.MaxValue, session.Score);
        }

        [Test]
        public void ProgressRoundTripRetainsBestAndUniqueSecrets()
        {
            var store = new ProgressStore(Path.Combine(_directory, "progress.json"));
            var progress = store.Load(out _);
            PlayerProgress restored;

            progress.RecordNight(100);
            progress.RecordNight(25);

            Assert.IsTrue(progress.Discover("secret-a"));
            Assert.IsFalse(progress.Discover("secret-a"));

            store.Save(progress);
            restored = store.Load(out _);

            Assert.AreEqual(100, restored.BestScore);
            Assert.AreEqual(25, restored.LastScore);
            Assert.AreEqual(2, restored.CompletedNights);
            CollectionAssert.AreEqual(new[] { "secret-a" }, restored.DiscoveredSecrets);
        }

        [Test]
        public void CorruptPrimaryRecoversBackupAndCanSaveAgain()
        {
            string path = Path.Combine(_directory, "progress.json");
            var store = new ProgressStore(path);
            var progress = new PlayerProgress();
            PlayerProgress recovered;
            string warning;

            progress.RecordNight(100);
            store.Save(progress);

            progress.RecordNight(200);
            store.Save(progress);

            File.WriteAllText(path, "broken");
            recovered = store.Load(out warning);

            Assert.IsNotNull(warning);
            Assert.AreEqual(100, recovered.BestScore);

            store.Save(recovered);

            Assert.AreEqual(100, store.Load(out _).BestScore);
        }

        [Test]
        public void UnrecoverableAndFutureSavesArePreserved()
        {
            string path = Path.Combine(_directory, "progress.json");
            var store = new ProgressStore(path);

            Directory.CreateDirectory(_directory);

            File.WriteAllText(path, "broken");

            Assert.Throws<IOException>(() => store.Load(out _));
            Assert.AreEqual("broken", File.ReadAllText(path));

            File.WriteAllText(path, "{\"Version\":999}");

            Assert.Throws<NotSupportedException>(() => store.Load(out _));
            Assert.AreEqual("{\"Version\":999}", File.ReadAllText(path));
        }

        [Test]
        public void ClockCrossesMidnight()
        {
            var settings = ScriptableObject.CreateInstance<NightSettings>();

            try
            {
                Assert.AreEqual(20f, settings.HourAt(0f));
                Assert.AreEqual(1f, settings.HourAt(0.5f));
                Assert.AreEqual(6f, settings.HourAt(1f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
    }
}
