using Robust.Shared.Serialization;

namespace Content.Shared.CombatMode.Events
{
    [Serializable, NetSerializable]
    public sealed class BeginTurnEvent : EntityEventArgs
    {
        /// <summary>
        /// Coordinates the beginning of an entity's next turn.
        /// </summary>
        public BeginTurnEvent()
        {

        }
    }

    [Serializable, NetSerializable]
    public sealed class BeganTurnEvent : EntityEventArgs
    {
        /// <summary>
        /// Coordinates the beginning of an entity's next turn.
        /// </summary>
        public BeganTurnEvent()
        {

        }
    }
}