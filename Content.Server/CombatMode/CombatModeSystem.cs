using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Events;
using Robust.Shared.Timing;

namespace Content.Server.CombatMode;

public sealed partial class CombatModeSystem : SharedCombatModeSystem
{
    [Dependency] private IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<BeginTurnEvent>(OnBeginTurnRequest);
    }

    private void OnBeginTurnRequest(BeginTurnEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {Valid: true} user)
        {
            Log.Error($"Client sent {nameof(BeginTurnEvent)} with no attached entity!");
            return;
        }

        if (!TryComp<CombatModeComponent>(user, out var component))
        {
            Log.Error($"Client sent {nameof(BeginTurnEvent)} with no {nameof(CombatModeComponent)} component!");
            return;
        }

        // Check to see if the appropriate amount of time has actually passed since the last turn.
        TimeSpan curTime = _gameTiming.CurTime;
        var xy = component.LastTurnStarted is null ? TimeSpan.FromSeconds(component.CombatTurnDuration) : (curTime - (TimeSpan)component.LastTurnStarted);
        if (xy.TotalSeconds < component.CombatTurnDuration)
            return;

        if (!component.IsInThreatStance)
            return;

        // Set the turn timer to 0 (thus confirming the new turn) and update the timestamp of the last started turn.
        component.CombatTurnTimer = 0.0f;
        component.LastTurnStarted = curTime;
        DirtyField(user, component, nameof(CombatModeComponent.CombatTurnTimer));
        RaiseNetworkEvent(new BeganTurnEvent(), user);
    }
}
