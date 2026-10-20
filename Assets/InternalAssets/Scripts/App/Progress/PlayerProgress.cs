using System;
using System.Collections.Generic;

namespace Halloween2026
{
    [Serializable]
    public sealed class PlayerProgress
    {
        public const int CurrentVersion = 1;
        public int Version = CurrentVersion;
        public int BestScore;
        public int LastScore;
        public int CompletedNights;
        public List<string> DiscoveredSecrets = new List<string>();

        public void Validate()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            if (Version != CurrentVersion) throw new InvalidOperationException("Unsupported save version: " + Version);
            if (BestScore < 0 || LastScore < 0 || CompletedNights < 0 || LastScore > BestScore || DiscoveredSecrets == null)
                throw new InvalidOperationException("Invalid progress data.");

            foreach (string id in DiscoveredSecrets)
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                    throw new InvalidOperationException("Invalid or duplicate secret ID.");
        }

        public void RecordNight(int score)
        {
            if (score < 0) throw new ArgumentOutOfRangeException(nameof(score));

            LastScore = score;
            BestScore = Math.Max(BestScore, score);
            if (CompletedNights < int.MaxValue) CompletedNights++;
        }

        public bool Discover(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || DiscoveredSecrets.Contains(id)) return false;

            DiscoveredSecrets.Add(id);

            return true;
        }
    }
}
