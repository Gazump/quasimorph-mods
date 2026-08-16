using System.Collections.Generic;
using MGSC;

namespace CombatPsychology
{
    public class MercPsyche : IWrapTypeOnSave
    {
        [Save]
        public string ProfileId { get; set; }

        [Save]
        public int Trauma { get; set; }

        [Save]
        public List<string> Scars { get; set; } = new List<string>();

        [Save]
        public int CleanRaidStreak { get; set; }

        [Save]
        public bool SurvivorsHighPending { get; set; }

        public bool HasScar(string id)
        {
            return Scars.Contains(id);
        }
    }

    public class PsycheStore
    {
        public static PsycheStore Current = new PsycheStore();

        [Save]
        public List<MercPsyche> Entries { get; set; } = new List<MercPsyche>();

        public static void ResetAll()
        {
            Current = new PsycheStore();
        }

        public MercPsyche Find(string profileId)
        {
            foreach (MercPsyche entry in Entries)
            {
                if (entry.ProfileId == profileId)
                {
                    return entry;
                }
            }
            return null;
        }

        public MercPsyche GetOrCreate(string profileId)
        {
            MercPsyche mercPsyche = Find(profileId);
            if (mercPsyche == null)
            {
                mercPsyche = new MercPsyche
                {
                    ProfileId = profileId
                };
                Entries.Add(mercPsyche);
            }
            return mercPsyche;
        }
    }
}
