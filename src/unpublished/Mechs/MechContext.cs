using MGSC;

namespace Mechs
{
    public static class MechContext
    {
        public static Creatures Creatures;
        public static ItemsOnFloor ItemsOnFloor;
        public static TurnController TurnController;
        public static TurnMetadata TurnMetadata;

        public static void Capture(IModContext context)
        {
            Creatures = context.State.Get<Creatures>();
            ItemsOnFloor = context.State.Get<ItemsOnFloor>();
            TurnController = context.State.Get<TurnController>();
            TurnMetadata = context.State.Get<TurnMetadata>();
        }

        public static void Clear()
        {
            Creatures = null;
            ItemsOnFloor = null;
            TurnController = null;
            TurnMetadata = null;
        }
    }
}
