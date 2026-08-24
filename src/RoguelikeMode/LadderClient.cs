using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using MGSC;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

namespace RoguelikeMode
{
    public enum LadderStatus
    {
        Idle,
        Loading,
        Ready,
        Failed
    }

    public class LadderEntry
    {
        public int Rank;
        public string Name;
        public int Score;
        public int Floor;
        public int Kills;
        public bool Victory;
    }

    public class LadderBoard
    {
        public string Day = string.Empty;
        public string Tier = string.Empty;
        public int Total;
        public List<LadderEntry> Entries = new List<LadderEntry>();
    }

    public class LadderPending
    {
        [Save]
        public string Day { get; set; }

        [Save]
        public string Tier { get; set; }

        [Save]
        public int Floor { get; set; }

        [Save]
        public int Kills { get; set; }

        [Save]
        public int Turns { get; set; }

        [Save]
        public int Damage { get; set; }

        [Save]
        public bool Victory { get; set; }

        [Save]
        public int Score { get; set; }

        [Save]
        public int DurationSec { get; set; }

        [Save]
        public string Profile { get; set; }

        [Save]
        public string ClassId { get; set; }
    }

    public static class LadderClient
    {
        private const string PendingFile = "roguelike_ladder_pending.dat";
        private const int TimeoutSeconds = 15;

        public static LadderStatus BoardStatus = LadderStatus.Idle;
        public static LadderBoard Board;
        public static string BoardError = string.Empty;
        public static string LastSubmitResult = string.Empty;

        public static string TierKey(RogueTier tier)
        {
            return tier.ToString().ToLowerInvariant();
        }

        public static string TodayUtc()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        public static void Fetch(string day, RogueTier tier)
        {
            CoroutineRunner runner = SingletonMonoBehaviour<CoroutineRunner>.Instance;
            if (runner == null)
            {
                return;
            }
            BoardStatus = LadderStatus.Loading;
            BoardError = string.Empty;
            runner.StartCoroutine(FetchRoutine(day, tier));
        }

        private static IEnumerator FetchRoutine(string day, RogueTier tier)
        {
            string url = LadderConfig.Endpoint + "/v1/board?day=" + UnityWebRequest.EscapeURL(day)
                + "&tier=" + TierKey(tier) + "&limit=15";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = TimeoutSeconds;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    BoardStatus = LadderStatus.Failed;
                    BoardError = request.error ?? "connection failed";
                    Debug.LogWarning("[RoguelikeMode] Ladder fetch failed: " + BoardError);
                    yield break;
                }
                try
                {
                    Board = ParseBoard(request.downloadHandler.text);
                    BoardStatus = LadderStatus.Ready;
                }
                catch (Exception ex)
                {
                    BoardStatus = LadderStatus.Failed;
                    BoardError = "bad response";
                    Debug.LogError("[RoguelikeMode] Ladder parse failed: " + ex.Message);
                }
            }
        }

        private static LadderBoard ParseBoard(string text)
        {
            JSONNode root = JSON.Parse(text);
            LadderBoard board = new LadderBoard
            {
                Day = root["day"].Value,
                Tier = root["tier"].Value,
                Total = root["total"].AsInt
            };
            JSONArray entries = root["entries"].AsArray;
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    JSONNode node = entries[i];
                    board.Entries.Add(new LadderEntry
                    {
                        Rank = node["rank"].AsInt,
                        Name = node["name"].Value,
                        Score = node["score"].AsInt,
                        Floor = node["floor"].AsInt,
                        Kills = node["kills"].AsInt,
                        Victory = node["victory"].AsBool
                    });
                }
            }
            return board;
        }

        public static bool CanSend(out string reason)
        {
            if (!LadderConfig.Enabled)
            {
                reason = "ladder submission is off";
                return false;
            }
            if (!SteamIdentity.Available || string.IsNullOrEmpty(SteamIdentity.SteamId))
            {
                reason = "Steam is not available";
                return false;
            }
            reason = string.Empty;
            return true;
        }

        public static bool EligibleForLadder(out string reason)
        {
            if (!CanSend(out reason))
            {
                return false;
            }
            if (!RogueRun.Daily)
            {
                reason = "only daily dives are ranked";
                return false;
            }
            if (RogueRun.ActiveMods.Count > 0)
            {
                reason = "other mods were active";
                return false;
            }
            if (RogueRun.CheatsUsed)
            {
                reason = "cheats were used";
                return false;
            }
            reason = string.Empty;
            return true;
        }

        public static void SubmitRun(RogueScoreEntry entry, int durationSec)
        {
            string reason;
            if (!EligibleForLadder(out reason))
            {
                LastSubmitResult = reason;
                return;
            }
            LadderPending pending = new LadderPending
            {
                Day = entry.Day,
                Tier = TierKey((RogueTier)entry.Tier),
                Floor = entry.Floor,
                Kills = entry.Kills,
                Turns = entry.Turns,
                Damage = entry.DamageTaken,
                Victory = entry.Victory,
                Score = entry.Score,
                DurationSec = durationSec,
                Profile = entry.ProfileId,
                ClassId = entry.ClassId
            };
            if (pending.Floor < 1)
            {
                LastSubmitResult = "run too short to rank";
                return;
            }
            SavePending(pending);
            Send(pending);
        }

        public static void FlushPending()
        {
            LadderPending pending = LoadPending();
            if (pending == null)
            {
                return;
            }
            string today = TodayUtc();
            string yesterday = DateTime.UtcNow.AddDays(-1.0).ToString("yyyy-MM-dd");
            if (pending.Day != today && pending.Day != yesterday)
            {
                ClearPending();
                return;
            }
            string reason;
            if (!CanSend(out reason))
            {
                return;
            }
            Send(pending);
        }

        private static void Send(LadderPending pending)
        {
            CoroutineRunner runner = SingletonMonoBehaviour<CoroutineRunner>.Instance;
            if (runner == null)
            {
                return;
            }
            runner.StartCoroutine(SubmitRoutine(pending));
        }

        private static IEnumerator SubmitRoutine(LadderPending pending)
        {
            string body = BuildPayload(pending);
            string signature = Sign(body, LadderConfig.SubmitSecret);
            using (UnityWebRequest request = new UnityWebRequest(LadderConfig.Endpoint + "/v1/runs", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-Rogue-Sig", signature);
                request.timeout = TimeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    LastSubmitResult = "no connection - will retry";
                    Debug.LogWarning("[RoguelikeMode] Ladder submit failed: " + request.error);
                    yield break;
                }

                string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                bool accepted = false;
                string reason = string.Empty;
                int rank = 0;
                int total = 0;
                try
                {
                    JSONNode node = JSON.Parse(responseText);
                    if (node != null)
                    {
                        accepted = node["accepted"].AsBool;
                        reason = node["reason"].Value;
                        rank = node["rank"].AsInt;
                        total = node["total"].AsInt;
                    }
                }
                catch (Exception)
                {
                    reason = "unreadable response";
                }

                if (accepted)
                {
                    ClearPending();
                    LastSubmitResult = (total > 0) ? ("ranked " + rank + " of " + total) : "submitted";
                    Debug.Log("[RoguelikeMode] Ladder submit accepted: " + LastSubmitResult);
                }
                else
                {
                    ClearPending();
                    LastSubmitResult = string.IsNullOrEmpty(reason) ? "rejected" : reason;
                    Debug.LogWarning("[RoguelikeMode] Ladder submit rejected (" + request.responseCode + "): " + LastSubmitResult);
                }
            }
        }

        private static string BuildPayload(LadderPending pending)
        {
            JSONObject payload = new JSONObject();
            payload["v"] = 1;
            payload["mod"] = LadderConfig.ModVersion;
            payload["game"] = Application.version;
            payload["day"] = pending.Day;
            payload["mode"] = "daily";
            payload["tier"] = pending.Tier;
            payload["steamId"] = SteamIdentity.SteamId;
            payload["name"] = SteamIdentity.PersonaName;
            payload["floor"] = pending.Floor;
            payload["kills"] = pending.Kills;
            payload["turns"] = pending.Turns;
            payload["damage"] = pending.Damage;
            payload["victory"] = pending.Victory;
            payload["score"] = pending.Score;
            payload["durationSec"] = pending.DurationSec;
            payload["mods"] = new JSONArray();
            payload["profile"] = pending.Profile ?? string.Empty;
            payload["class"] = pending.ClassId ?? string.Empty;
            payload["nonce"] = Guid.NewGuid().ToString("N");
            payload["ts"] = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return payload.ToString();
        }

        private static string Sign(string body, string secret)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private static void SavePending(LadderPending pending)
        {
            try
            {
                SingletonMonoBehaviour<FileManager>.Instance.SaveFile(PendingFile, SaveToJSON.CreateNode(pending).ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoguelikeMode] Failed to store pending ladder run: " + ex.Message);
            }
        }

        private static LadderPending LoadPending()
        {
            FileManager fileManager = SingletonMonoBehaviour<FileManager>.Instance;
            if (fileManager == null || !fileManager.IsFileExist(PendingFile))
            {
                return null;
            }
            try
            {
                string text = fileManager.LoadTextFile(PendingFile);
                if (string.IsNullOrEmpty(text))
                {
                    return null;
                }
                LadderPending pending = new LadderPending();
                pending.LoadJSON(JSON.Parse(text));
                return string.IsNullOrEmpty(pending.Day) ? null : pending;
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoguelikeMode] Failed to read pending ladder run: " + ex.Message);
                ClearPending();
                return null;
            }
        }

        private static void ClearPending()
        {
            FileManager fileManager = SingletonMonoBehaviour<FileManager>.Instance;
            if (fileManager != null && fileManager.IsFileExist(PendingFile))
            {
                fileManager.RemoveFile(PendingFile);
            }
        }
    }
}
