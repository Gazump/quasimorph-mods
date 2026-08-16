using MGSC;

namespace CombatPsychology
{
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
