using MGSC;

namespace CombatPsychology
{
    // ConfigTableRecord.Id has a protected setter; these shims let the mod construct
    // records with an id outside the game's TSV parser.
    public class PsyStatusEffectsRecord : StatusEffectsRecord
    {
        public PsyStatusEffectsRecord(string id)
        {
            Id = id;
        }
    }

    public class PsyConsumableRecord : ConsumableRecord
    {
        public PsyConsumableRecord(string id)
        {
            Id = id;
        }
    }
}
