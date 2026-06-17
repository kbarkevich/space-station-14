using System.Diagnostics.CodeAnalysis;
using Content.Shared.CombatMode.Events;
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

    public bool TryGetCombatModeComponent(EntityUid entity, [NotNullWhen(true)] out CombatModeComponent? combatModeComponent)
    {
        if (TryComp(entity, out combatModeComponent))
        {
            return true;
        }
        return false;
    }

    private void OnActionPerform(EntityUid uid, CombatModeComponent component, ToggleCombatActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        bool oldThreatStance = component.IsInThreatStance;
        SetInThreatStance(uid, !component.IsInThreatStance, component);

        if (component.IsInThreatStance == oldThreatStance)
            return;
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

        if (component.EnteredThreatStance is not null
            && (Timing.CurTime - (TimeSpan)component.EnteredThreatStance).Seconds < component.ThreatStancePrepTime)
            return;

        component.IsInThreatStance = value;
        component.EnteredThreatStance = Timing.CurTime;
        string[] x = { nameof(CombatModeComponent.IsInThreatStance), nameof(CombatModeComponent.Target), nameof(CombatModeComponent.EnteredThreatStance) };
        DirtyFields(entity, component, null, x);

        if (component.CombatToggleActionEntity != null)
            _actionsSystem.SetToggled(component.CombatToggleActionEntity, component.IsInThreatStance);

        if (component.IsInThreatStance)
            _chatSystem.TrySendInGameICMessage(entity, Loc.GetString("action-popup-threat-stance-enabled"), InGameICChatType.Emote, ChatTransmitRange.Normal);
        else
            _chatSystem.TrySendInGameICMessage(entity, Loc.GetString("action-popup-threat-stance-disabled"), InGameICChatType.Emote, ChatTransmitRange.Normal);

        // Change mouse rotator comps if flag is set
        if (!component.ToggleMouseRotator || _npc.IsNpc(entity) && !_mind.TryGetMind(entity, out _, out _))
            return;

        SetMouseRotatorComponents(entity, value);
    }

    /// <summary>
    /// Do we have a valid target and has the turn been marked for approval by the server?
    /// </summary>
    /// <param name="entity">The EntityUid associated with the CombatModeComponent.</param>
    /// <param name="component">Obtained from "entity" if null.</param>
    /// <returns>true/false depending on whether or not the turn timer should advance.</returns>
    public bool IsTurnProgressing(EntityUid? entity, CombatModeComponent? component = null)
    {
        return entity != null
            && Resolve(entity.Value, ref component, false)
            && IsInThreatStance(entity, component)
            && component.CombatTurnProgressing
            && component.CombatTurnTimer >= 0f;
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
