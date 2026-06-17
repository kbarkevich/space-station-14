using Content.Shared.MouseRotator;
using Content.Shared.Movement.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.CombatMode
{
    /// <summary>
    ///     Stores whether an entity is in "combat mode"
    ///     This is used to differentiate between regular item interactions or
    ///     using *everything* as a weapon.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
    [Access(typeof(SharedCombatModeSystem))]
    public sealed partial class CombatModeComponent : Component
    {
        #region Disarm

        /// <summary>
        /// Whether we are able to disarm. This requires our active hand to be free.
        /// False if it's toggled off for whatever reason, null if it's not possible.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("canDisarm")]
        public bool? CanDisarm;

        [DataField("disarmSuccessSound")]
        public SoundSpecifier DisarmSuccessSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

        [DataField("disarmFailChance")]
        public float BaseDisarmFailChance = 0.75f;

        #endregion

        [DataField("combatToggleAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string CombatToggleAction = "ActionThreatStanceToggle";

        [DataField, AutoNetworkedField]
        public EntityUid? CombatToggleActionEntity;

        [ViewVariables(VVAccess.ReadWrite), DataField("IsInThreatStance"), AutoNetworkedField]
        public bool IsInThreatStance;

        [ViewVariables(VVAccess.ReadWrite), DataField("ThreatStancePrepTime"), AutoNetworkedField]
        public float ThreatStancePrepTime = 1.0f;

        [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public TimeSpan? EnteredThreatStance = null;

        //[ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
        public bool CombatTurnProgressing { get => Target is not null; }//false;

        [ViewVariables(VVAccess.ReadWrite), DataField("TurnDuration"), AutoNetworkedField]
        public float CombatTurnDuration = 2.5f;

        [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
        public float CombatTurnTimer = 0.0f;

        [ViewVariables(VVAccess.ReadOnly), DataField("LastTurnStarted", serverOnly: true), AutoNetworkedField]
        public TimeSpan? LastTurnStarted = null;

        [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public EntityUid? Target = null;

        /// <summary>
        ///     Will add <see cref="MouseRotatorComponent"/> and <see cref="NoRotateOnMoveComponent"/>
        ///     to entities with this flag enabled that enter combat mode, and vice versa for removal.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool ToggleMouseRotator = true;
    }
}
