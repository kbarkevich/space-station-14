using Content.Client.Hands.Systems;
using Content.Client.Weapons.Melee;
using Content.Shared.CCVar;
using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Events;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.CombatMode;

public sealed partial class CombatModeSystem : SharedCombatModeSystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private MeleeWeaponSystem _meleeWeaponSystem = default!;

    /// <summary>
    /// Raised whenever combat mode changes.
    /// </summary>
    public event Action<bool>? LocalPlayerCombatModeUpdated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatModeComponent, AfterAutoHandleStateEvent>(OnHandleState);

        SubscribeNetworkEvent<BeganTurnEvent>(OnBeganTurnConfirmation);

        Subs.CVar(_cfg, CCVars.CombatModeIndicatorsPointShow, OnShowCombatIndicatorsChanged, true);

        _overlayManager.AddOverlay(new TurnProgressOverlay(EntityManager, _prototype, Timing, _playerManager));
        _overlayManager.AddOverlay(new TargetMarkerOverlay(EntityManager, _prototype, _playerManager));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var entityNull = _playerManager.LocalEntity;

        if (entityNull == null)
            return;

        var entity = entityNull.Value;

        if (!TryGetCombatModeComponent(entity, out var component))
            return;

        if (IsTurnProgressing(entity, component))
        {
            // If the turn is progressing, add the frame time.
            component.CombatTurnTimer += frameTime;
            if (component.CombatTurnTimer >= component.CombatTurnDuration)
            {
                // Lock the frame time at the turn duration and wait for approval to begin a new turn.
                component.CombatTurnTimer = component.CombatTurnDuration;
                RaiseNetworkEvent(new BeginTurnEvent());
            }
        }
    }

    private void OnHandleState(EntityUid uid, CombatModeComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateHud(uid);
    }

    /// <summary>
    /// Attempt to mark a target for turn-based combat.
    /// </summary>
    /// <param name="target">The EntityUid of the target being marked.</param>
    /// <returns>true/false depending on whether or not the "target" is a valid EntityUid to target.</returns>
    public bool MarkTarget(EntityUid target)
    {
        var entityNull = _playerManager.LocalEntity;

        if (entityNull == null)
            return false;

        var entity = entityNull.Value;

        // Can only mark targets that themselves are capable of turn-based combat.
        if (!TryGetCombatModeComponent(entity, out var component) || !TryGetCombatModeComponent(target, out var _))
            return false;

        if (component.Target is null)
        {  // Request to begin a turn if this is a new combat instance.
            if (component.EnteredThreatStance is not null
                && (Timing.CurTime - (TimeSpan)component.EnteredThreatStance).Seconds < component.ThreatStancePrepTime)
                return true;

            component.Target = target;
            component.CombatTurnTimer = -0.01f;  // Do not display the timer bar until the turn is confirmed.
            RaiseNetworkEvent(new BeginTurnEvent());
        }
        else
            component.Target = target;  // If this is an existing combat instance, just switch targets.

        return true;
    }

    private void OnBeganTurnConfirmation(BeganTurnEvent ev)
    {
        Log.Log(LogLevel.Debug, "Got a-OK to begin new turn from server.");

        var entityNull = _playerManager.LocalEntity;

        if (entityNull == null)
            return;

        var entity = entityNull.Value;

        // Can only attack the existing target if it's capable of turn-based combat.
        if (!IsInThreatStance()
            || !TryGetCombatModeComponent(entity, out var component)
            || component.Target is null
            || !TryGetCombatModeComponent((EntityUid)component.Target, out var _))
            return;

        _meleeWeaponSystem.AutoLightAttack((EntityUid)component.Target);
    }

    public override void Shutdown()
    {
        _overlayManager.RemoveOverlay<CombatModeIndicatorsOverlay>();
        _overlayManager.RemoveOverlay<TurnProgressOverlay>();
        _overlayManager.RemoveOverlay<TargetMarkerOverlay>();

        base.Shutdown();
    }

    public bool IsInThreatStance()
    {
        var entity = _playerManager.LocalEntity;

        if (entity == null)
            return false;

        return IsInThreatStance(entity.Value);
    }

    public override void SetInThreatStance(EntityUid entity, bool value, CombatModeComponent? component = null)
    {
        base.SetInThreatStance(entity, value, component);
        UpdateHud(entity);
    }

    private void UpdateHud(EntityUid entity)
    {
        if (entity != _playerManager.LocalEntity || !Timing.IsFirstTimePredicted)
        {
            return;
        }

        var inThreatStance = IsInThreatStance();
        LocalPlayerCombatModeUpdated?.Invoke(inThreatStance);
    }

    /// <summary>
    /// Does the current entity have a valid target and has the turn been marked for approval by the server?
    /// </summary>
    /// <returns>true/false depending on whether or not the turn timer should advance.</returns>
    public bool IsTurnProgressing()
    {
        var entity = _playerManager.LocalEntity;

        if (entity == null)
            return false;

        return IsTurnProgressing(entity.Value);
    }

    private void OnShowCombatIndicatorsChanged(bool isShow)
    {
        if (isShow)
        {
            _overlayManager.AddOverlay(new CombatModeIndicatorsOverlay(
                _inputManager,
                EntityManager,
                _eye,
                this,
                EntityManager.System<HandsSystem>()));
        }
        else
        {
            _overlayManager.RemoveOverlay<CombatModeIndicatorsOverlay>();
        }
    }
}
