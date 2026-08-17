using System.Reflection;
using MGSC;

namespace Mechs
{
    public class MechRecord : DeviceRecord
    {
        public MechRecord(string id)
        {
            Id = id;
        }
    }

    public class MechFrameRecord : BreakableItemRecord
    {
        public MechFrameRecord(string id)
        {
            Id = id;
        }
    }

    public static class RecordReflection
    {
        private static readonly PropertyInfo _idProperty =
            typeof(ConfigTableRecord).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public);

        private static readonly PropertyInfo _resistSheetProperty =
            typeof(ResistRecord).GetProperty("ResistSheet", BindingFlags.Instance | BindingFlags.Public);

        public static void SetId(ConfigTableRecord record, string id)
        {
            _idProperty.GetSetMethod(nonPublic: true).Invoke(record, new object[] { id });
        }

        public static void SetResistSheet(ResistRecord record, System.Collections.Generic.List<DmgResist> sheet)
        {
            _resistSheetProperty.GetSetMethod(nonPublic: true).Invoke(record, new object[] { sheet });
        }
    }
}
