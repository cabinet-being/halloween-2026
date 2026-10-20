using System;
using System.IO;
using UnityEngine;

namespace Halloween2026
{
    // Only completed-night results and discoveries are persisted, not an unfinished run.
    public sealed class ProgressStore
    {
        private readonly string _path;

        public ProgressStore(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public PlayerProgress Load(out string warning)
        {
            PlayerProgress recovered;

            warning = null;
            if (!File.Exists(_path) && !File.Exists(_path + ".bak")) return new PlayerProgress();

            try
            {
                return Read(_path);
            }
            catch (Exception primary) when (IsReadError(primary))
            {
                // Never replace a save from a newer version with older data.
                if (primary is NotSupportedException) throw;

                try
                {
                    recovered = Read(_path + ".bak");
                    warning = "Recovered progress from backup: " + primary.Message;

                    return recovered;
                }
                catch (Exception backup) when (IsReadError(backup))
                {
                    throw new IOException("Progress and backup could not be loaded. Existing files were preserved.", primary);
                }
            }
        }

        public void Save(PlayerProgress progress)
        {
            string directory;
            string temporary;
            string json;
            bool valid = false;

            progress.Validate();

            directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            temporary = _path + ".tmp";
            json = JsonUtility.ToJson(progress, true);
            File.WriteAllText(temporary, json);

            // Preserve only a valid previous save as backup; a corrupt primary must not destroy a good backup.
            if (File.Exists(_path))
            {
                try
                {
                    Read(_path);
                    valid = true;
                }
                catch (Exception error) when (IsReadError(error))
                {
                    // Retain the existing backup when the primary cannot be read.
                }

                File.Replace(temporary, _path, valid ? _path + ".bak" : null);
            }
            else File.Move(temporary, _path);
        }

        private static PlayerProgress Read(string path)
        {
            string json = File.ReadAllText(path);
            PlayerProgress progress;

            // FromJson can accept incomplete objects. Require the version marker explicitly.
            if (!json.Contains("\"Version\"")) throw new InvalidDataException("Missing save version.");

            progress = JsonUtility.FromJson<PlayerProgress>(json);
            if (progress == null) throw new InvalidDataException("Empty save.");
            if (progress.Version != PlayerProgress.CurrentVersion)
                throw new NotSupportedException("Unsupported save version: " + progress.Version);

            progress.Validate();

            return progress;
        }

        private static bool IsReadError(Exception error) => error is IOException || error is InvalidDataException || error is UnauthorizedAccessException
            || error is ArgumentException || error is InvalidOperationException || error is NotSupportedException;
    }
}
