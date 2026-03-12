// <author>QROST</author>
// <created>20260312</created>

using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace MeshyRevit.Services
{
    public class MeshySettings
    {
        [JsonProperty("api_key")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonProperty("default_topology")]
        public string DefaultTopology { get; set; } = "triangle";

        [JsonProperty("default_polycount")]
        public int DefaultPolycount { get; set; } = 30000;

        [JsonProperty("default_ai_model")]
        public string DefaultAiModel { get; set; } = "latest";

        [JsonProperty("enable_pbr")]
        public bool EnablePbr { get; set; } = false;

        [JsonProperty("poll_interval_ms")]
        public int PollIntervalMs { get; set; } = 3000;
    }

    public static class MeshySettingsService
    {
        private static readonly string _settingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MeshyRevit");

        private static readonly string _settingsFile = Path.Combine(_settingsFolder, "settings.json");

        private static readonly object _lock = new object();
        private static MeshySettings _cached;

        public static MeshySettings Load()
        {
            lock (_lock)
            {
                if (_cached != null)
                    return _cached;

                if (!File.Exists(_settingsFile))
                {
                    _cached = new MeshySettings();
                    return _cached;
                }

                try
                {
                    string json = File.ReadAllText(_settingsFile);
                    _cached = JsonConvert.DeserializeObject<MeshySettings>(json) ?? new MeshySettings();
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning(
                        $"[Meshy Revit] Failed to load settings from {_settingsFile}: {ex.Message}. Using defaults.");
                    _cached = new MeshySettings();
                }

                return _cached;
            }
        }

        public static void Save(MeshySettings settings)
        {
            lock (_lock)
            {
                _cached = settings;

                if (!Directory.Exists(_settingsFolder))
                    Directory.CreateDirectory(_settingsFolder);

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_settingsFile, json);
            }
        }

        public static bool HasApiKey()
        {
            var settings = Load();
            return !string.IsNullOrWhiteSpace(settings.ApiKey)
                && settings.ApiKey.StartsWith("msy_");
        }

        public static void ClearCache()
        {
            lock (_lock)
            {
                _cached = null;
            }
        }
    }
}
