using System.Collections.Generic;
using MGSC;

namespace CombatPsychology
{
    /// <summary>Persistent psychological state for one mercenary profile. Keyed by ProfileId,
    /// so it survives cloning — the clone remembers.</summary>
    public class MercPsyche : IWrapTypeOnSave
    {
        [Save]
        public string ProfileId { get; set; }

        [Save]
        public int Trauma { get; set; }

        [Save]
        public List<string> Scars { get; set; } = new List<string>();

        /// <summary>Consecutive high-stress raids survived without a breakdown (feeds Cold Blood).</summary>
        [Save]
        public int CleanRaidStreak { get; set; }

        /// <summary>Extracted at 75+ stress last raid: next raid starts with bonus fortitude.</summary>
        [Save]
        public bool SurvivorsHighPending { get; set; }

        public bool HasScar(string id)
        {
            return Scars.Contains(id);
        }
    }

    /// <summary>
    /// The whole-campaign psyche table. Not part of the game's hardcoded save component
    /// list, so PersistencePatches serialize it to a slot_N_psyche.dat sidecar with the
    /// game's own SaveToJSON/LoadJSON machinery.
    /// </summary>
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
