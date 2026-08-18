using System;
using System.Collections.Generic;
using HarmonyLib;
using MGSC;

namespace RoguelikeMode
{
    public static class LocalizationInjector
    {
        private static Dictionary<Localization.Lang, Dictionary<string, string>> Db()
        {
            return AccessTools.Field(typeof(Localization), "db").GetValue(Singleton<Localization>.Instance)
                as Dictionary<Localization.Lang, Dictionary<string, string>>;
        }

        public static void Set(string key, string english)
        {
            Dictionary<Localization.Lang, Dictionary<string, string>> db = Db();
            foreach (Localization.Lang lang in Enum.GetValues(typeof(Localization.Lang)))
            {
                if (db.TryGetValue(lang, out Dictionary<string, string> dict))
                {
                    dict[key] = english;
                }
            }
        }

        public static void EnsureKey(string key, string english)
        {
            if (!Localization.HasKey(key))
            {
                Set(key, english);
            }
        }
    }
}
