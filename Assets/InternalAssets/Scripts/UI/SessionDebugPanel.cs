using UnityEngine;

namespace Halloween2026
{
    // Optional development UI, not the final game HUD.
    public sealed class SessionDebugPanel : MonoBehaviour
    {
        [SerializeField, Tooltip("Session and progress services exercised by this development panel.")]
        private GameBootstrapper _game;

        [SerializeField]
        private bool _visible = true;

        private void OnGUI()
        {
            GameSession session;

            if (!_visible || _game == null || _game.Session == null) return;

            session = _game.Session;

            GUILayout.BeginArea(new Rect(16f, 16f, 300f, 430f), GUI.skin.box);
            GUILayout.Label("Night foundation — " + session.State);
            GUILayout.Label($"Remaining: {session.Remaining:F0}s | Hour: {_game.Settings.HourAt(session.Progress):F1}");
            GUILayout.Label($"Score: {session.Score} | Best: {_game.BestScore}");

            if (session.State == SessionState.Ready && GUILayout.Button("Start night")) session.Start();
            if (session.State == SessionState.Running || session.State == SessionState.Paused)
                if (GUILayout.Button(session.State == SessionState.Paused ? "Resume" : "Pause"))
                    _game.SetPaused(session.State != SessionState.Paused);

            GUI.enabled = session.State == SessionState.Running;
            if (GUILayout.Button("Award candy (+10)")) _game.Award(ScoreSource.Candy, 10);
            if (GUILayout.Button("Award scare (+25)")) _game.Award(ScoreSource.Scare, 25);
            if (GUILayout.Button("Advance 60 seconds")) session.Tick(60f);

            foreach (var secret in _game.Secrets)
                if (GUILayout.Button((_game.IsDiscovered(secret.Id) ? "Found: " : "Discover: ") + secret.Title))
                    _game.Discover(secret.Id);

            GUI.enabled = true;
            if (GUILayout.Button("Restart night")) _game.RestartNight();
            if (_game.SaveError != null) GUILayout.Label("Save error: " + _game.SaveError);

            GUILayout.EndArea();
        }
    }
}
