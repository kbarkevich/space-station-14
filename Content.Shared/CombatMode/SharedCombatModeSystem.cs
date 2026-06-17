using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.MouseRotator;
using Content.Shared.Movement.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared.CombatMode;

public abstract partial class SharedCombatModeSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private SharedActionsSystem _actionsSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedNPCSystem _npc = default!;
    [Dependency] private SharedChatSystem _chatSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatModeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CombatModeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CombatModeComponent, ToggleCombatActionEvent>(OnActionPerform);
    }

    private void OnMapInit(EntityUid uid, CombatModeComponent component, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref component.CombatToggleActionEntity, component.CombatToggleAction);
        Dirty(uid, component);
    }

    private void OnShutdown(EntityUid uid, CombatModeComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.CombatToggleActionEntity);

        SetMouseRotatorComponents(uid, false);
    }

    private void OnActionPerform(EntityUid uid, CombatModeComponent component, ToggleCombatActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        SetInThreatStance(uid, !component.IsInThreatStance, component);

        var msg = component.IsInThreatStance ? "action-popup-threat-stance-enabled" : "action-popup-threat-stance-disabled";
        _popup.PopupClient(Loc.GetString(msg), args.Performer, args.Performer);
    }

    public void SetCanDisarm(EntityUid entity, bool canDisarm, CombatModeComponent? component = null)
    {
        if (!Resolve(entity, ref component))
            return;

        component.CanDisarm = canDisarm;
    }

    public bool IsInThreatStance(EntityUid? entity, CombatModeComponent? component = null)
    {
        return entity != null && Resolve(entity.Value, ref component, false) && component.IsInThreatStance;
    }

    public virtual void SetInThreatStance(EntityUid entity, bool value, CombatModeComponent? component = null)
    {
        if (!Resolve(entity, ref component))
            return;

        if (component.IsInThreatStance == value)
            return;

        component.IsInThreatStance = value;
        Dirty(entity, component);

        if (component.CombatToggleActionEntity != null)
            _actionsSystem.SetToggled(component.CombatToggleActionEntity, component.IsInThreatStance);

        if (component.IsInThreatStance)
            _chatSystem.TrySendInGameICMessage(entity, "raises their dukes.", InGameICChatType.Emote, ChatTransmitRange.Normal);
        else
            _chatSystem.TrySendInGameICMessage(entity, "lowers their dukes.", InGameICChatType.Emote, ChatTransmitRange.Normal);

        // Change mouse rotator comps if flag is set
        if (!component.ToggleMouseRotator || _npc.IsNpc(entity) && !_mind.TryGetMind(entity, out _, out _))
            return;

        SetMouseRotatorComponents(entity, value);
    }

    private void SetMouseRotatorComponents(EntityUid uid, bool value)
    {
        if (value)
        {
            EnsureComp<MouseRotatorComponent>(uid);
            EnsureComp<NoRotateOnMoveComponent>(uid);
        }
        else
        {
            RemComp<MouseRotatorComponent>(uid);
            RemComp<NoRotateOnMoveComponent>(uid);
        }
    }
}

public sealed partial class ToggleCombatActionEvent : InstantActionEvent
{

}
