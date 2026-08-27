using System;
using MGSC;
using SimpleJSON;
using UnityEngine;

namespace RoguelikeMode
{
    public class LadderSettings
    {
        [Save]
        public bool Enabled { get; set; }

        [Save]
        public string Endpoint { get; set; }
    }

    public static class LadderConfig
    {
        public const string ModVersion = "0.5.0";
        public const string DefaultEndpoint = LadderSecrets.Endpoint;
        public const string SubmitSecret = LadderSecrets.SubmitSecret;

        private const string FileName = "roguelike_ladder.dat";
        private static LadderSettings _settings;

        public static bool Configured
        {
            get { return !Endpoint.Contains("example.workers.dev"); }
        }

        public static LadderSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = LoadSettings();
                }
                return _settings;
            }
        }

        public static bool Enabled
        {
            get { return Settings.Enabled; }
        }

        public static string Endpoint
        {
            get
            {
                string endpoint = Settings.Endpoint;
                if (string.IsNullOrEmpty(endpoint))
                {
                    endpoint = DefaultEndpoint;
                }
                return endpoint.TrimEnd('/');
            }
        }

        public static void SetEnabled(bool enabled)
        {
            Settings.Enabled = enabled;
            Save();
        }

        public static void SetEndpoint(string endpoint)
        {
            Settings.Endpoint = string.IsNullOrEmpty(endpoint) ? DefaultEndpoint : endpoint;
            Save();
        }

        private static LadderSettings LoadSettings()
        {
            LadderSettings settings = new LadderSettings { Enabled = true, Endpoint = DefaultEndpoint };
            FileManager fileManager = SingletonMonoBehaviour<FileManager>.Instance;
            if (fileManager == null || !fileManager.IsFileExist(FileName))
            {
                return settings;
            }
            try
            {
                string text = fileManager.LoadTextFile(FileName);
                if (!string.IsNullOrEmpty(text))
                {
                    settings.LoadJSON(JSON.Parse(text));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoguelikeMode] Failed to load ladder settings: " + ex.Message);
                settings = new LadderSettings { Enabled = true, Endpoint = DefaultEndpoint };
            }
            if (string.IsNullOrEmpty(settings.Endpoint))
            {
                settings.Endpoint = DefaultEndpoint;
            }
            return settings;
        }

        private static void Save()
        {
            try
            {
                SingletonMonoBehaviour<FileManager>.Instance.SaveFile(FileName, SaveToJSON.CreateNode(Settings).ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoguelikeMode] Failed to save ladder settings: " + ex.Message);
            }
        }
    }
}
