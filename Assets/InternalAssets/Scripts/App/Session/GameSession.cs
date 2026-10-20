using System;
using System.Collections.Generic;

namespace Halloween2026
{
    public enum SessionState { Ready, Running, Paused, Completed }
    public enum ScoreSource { Candy, Scare, Prank, Trick }

    // No scene dependencies: mechanics submit awards, presentation observes events.
    public sealed class GameSession
    {
        private readonly Dictionary<ScoreSource, int> _breakdown = new Dictionary<ScoreSource, int>();

        public SessionState State { get; private set; }
        public float Duration { get; }
        public float Elapsed { get; private set; }
        public float Remaining => Math.Max(0f, Duration - Elapsed);
        public float Progress => Elapsed / Duration;
        public int Score { get; private set; }
        public IReadOnlyDictionary<ScoreSource, int> Breakdown { get; }

        public event Action Changed;
        public event Action Completed;

        public GameSession(float duration)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(duration));

            Duration = duration;
            Breakdown = new System.Collections.ObjectModel.ReadOnlyDictionary<ScoreSource, int>(_breakdown);
        }

        public bool Start()
        {
            if (State != SessionState.Ready) return false;

            State = SessionState.Running;
            Changed?.Invoke();

            return true;
        }

        public void SetPaused(bool paused)
        {
            if (State != SessionState.Running && State != SessionState.Paused) return;

            State = paused ? SessionState.Paused : SessionState.Running;
            Changed?.Invoke();
        }

        public void Tick(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (State != SessionState.Running) return;

            Elapsed = Math.Min(Duration, Elapsed + seconds);

            if (Elapsed >= Duration)
            {
                State = SessionState.Completed;
                Changed?.Invoke();
                Completed?.Invoke();
                return;
            }

            Changed?.Invoke();
        }

        public bool Award(ScoreSource source, int points)
        {
            int previous;

            if (points <= 0 || !Enum.IsDefined(typeof(ScoreSource), source)) return false;
            if (State != SessionState.Running || points > int.MaxValue - Score) return false;

            Score += points;
            _breakdown.TryGetValue(source, out previous);
            _breakdown[source] = previous + points;
            Changed?.Invoke();

            return true;
        }
    }
}
