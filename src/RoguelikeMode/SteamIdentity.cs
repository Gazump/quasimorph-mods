using System;
using Steamworks;
using UnityEngine;

namespace RoguelikeMode
{
    public static class SteamIdentity
    {
        public static bool Available
        {
            get
            {
                try
                {
                    return SteamManager.Initialized;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public static string SteamId
        {
            get
            {
                try
                {
                    if (!SteamManager.Initialized)
                    {
                        return string.Empty;
                    }
                    return SteamUser.GetSteamID().m_SteamID.ToString();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[RoguelikeMode] Steam id unavailable: " + ex.Message);
                    return string.Empty;
                }
            }
        }

        public static string PersonaName
        {
            get
            {
                try
                {
                    if (!SteamManager.Initialized)
                    {
                        return "operator";
                    }
                    string name = SteamFriends.GetPersonaName();
                    return string.IsNullOrEmpty(name) ? "operator" : name;
                }
                catch (Exception)
                {
                    return "operator";
                }
            }
        }
    }
}
