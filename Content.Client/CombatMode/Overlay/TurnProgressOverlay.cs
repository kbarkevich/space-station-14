using System.Numerics;
using Content.Shared.DoAfter;
using Content.Client.UserInterface.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Containers;
using Content.Shared.CombatMode;

namespace Content.Client.CombatMode;

public sealed class TurnProgressOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    private readonly IEntityManager _entManager;
    private readonly IGameTiming _timing;
    private readonly IPlayerManager _player;
    private readonly SharedTransformSystem _transform;
    private readonly MetaDataSystem _meta;
    private readonly ProgressColorSystem _progressColor;
    private readonly SharedContainerSystem _container;
    private readonly SpriteSystem _sprite;

    private readonly Texture _barTexture;
    private readonly ShaderInstance _unshadedShader;

    /// <summary>
    ///     Flash time for cancelled turns
    /// </summary>
    private const float FlashTime = 0.125f;

    // Hardcoded width of the progress bar because it doesn't match the texture.
    private const float StartX = 2;
    private const float EndX = 22f;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public TurnProgressOverlay(IEntityManager entManager, IPrototypeManager protoManager, IGameTiming timing, IPlayerManager player)
    {
        _entManager = entManager;
        _timing = timing;
        _player = player;
        _transform = _entManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        _meta = _entManager.EntitySysManager.GetEntitySystem<MetaDataSystem>();
        _container = _entManager.EntitySysManager.GetEntitySystem<SharedContainerSystem>();
        _progressColor = _entManager.System<ProgressColorSystem>();
        _sprite = _entManager.System<SpriteSystem>();
        var sprite = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/progress_bar.rsi"), "icon");
        _barTexture = _entManager.EntitySysManager.GetEntitySystem<SpriteSystem>().Frame0(sprite);

        _unshadedShader = protoManager.Index(UnshadedShader).Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        // If you use the display UI scale then need to set max(1f, displayscale) because 0 is valid.
        const float scale = 1f;
        var scaleMatrix = Matrix3Helpers.CreateScale(new Vector2(scale, scale));
        var rotationMatrix = Matrix3Helpers.CreateRotation(-rotation);

        var curTime = _timing.CurTime;

        var bounds = args.WorldAABB.Enlarged(5f);
        var localEnt = _player.LocalSession?.AttachedEntity;

        var metaQuery = _entManager.GetEntityQuery<MetaDataComponent>();
        var enumerator = _entManager.AllEntityQueryEnumerator<CombatModeComponent, SpriteComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var comp, out var sprite, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;
            if (!comp.CombatTurnProgressing)
                continue;  // Don't display the turn timer if the turn isn't actually progressing
            if (comp.CombatTurnTimer < 0f)
                continue;  // Don't display negative turn timers
            _entManager.TryGetComponent<DoAfterComponent>(uid, out var doAfterComponent);
            DrawTurnProgressBar(uid, comp, sprite, xform, xformQuery, scaleMatrix, rotationMatrix, curTime, bounds, localEnt, metaQuery, handle, scale, doAfterComponent);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawTurnProgressBar(EntityUid uid, CombatModeComponent comp, SpriteComponent sprite, TransformComponent xform, EntityQuery<TransformComponent> xformQuery,
                            Matrix3x2 scaleMatrix, Matrix3x2 rotationMatrix, TimeSpan curTime, Box2 bounds, EntityUid? localEnt, EntityQuery<MetaDataComponent> metaQuery,
                            DrawingHandleWorld handle, float scale, DoAfterComponent? doAfterComponent = null)
    {
        var worldPosition = _transform.GetWorldPosition(xform, xformQuery);
        if (!bounds.Contains(worldPosition))
            return;

        // shades the progress bar if the progress bar belongs to other players
        // does not shade progress bars belonging to the local player
        if (uid != localEnt)
            handle.UseShader(null);
        else
            handle.UseShader(_unshadedShader);

        // If the entity is paused, we might draw the progress bar as it was when the entity got paused.
        var meta = metaQuery.GetComponent(uid);
        var time = meta.EntityPaused
            ? curTime - _meta.GetPauseTime(uid, meta)
            : curTime;

        var worldMatrix = Matrix3Helpers.CreateTranslation(worldPosition);
        var scaledWorld = Matrix3x2.Multiply(scaleMatrix, worldMatrix);
        var matty = Matrix3x2.Multiply(rotationMatrix, scaledWorld);
        handle.SetTransform(matty);

        float offset = 0f;

        var isInContainer = _container.IsEntityOrParentInContainer(uid, meta, xform);

        // Place the turn progression bar at the top of all existing DoAfter bars.
        if (doAfterComponent is not null)
        {
            foreach (var doAfter in doAfterComponent.DoAfters.Values)
            {
                if (doAfter.Args.Hidden || isInContainer)
                    continue;
                offset += _barTexture.Height / scale;
            }
        }

        // Hide turn timer from players who are inside something.
        var alpha = 1f;
        if (isInContainer)
        {
            if (uid != localEnt)
                return;

            // Hints to the local player that this timer is not visible to other players.
            alpha = 0.5f;
        }

        // Use the sprite itself if we know its bounds. This means short or tall sprites don't get overlapped
        // by the bar.
        var yOffset = _sprite.GetLocalBounds((uid, sprite)).Height / 2f + 0.05f;

        // Position above the entity (we've already applied the matrix transform to the entity itself and
        // calculated the height of any existing DoAfter bars)
        var position = new Vector2(-_barTexture.Width / 2f / EyeManager.PixelsPerMeter,
            yOffset / scale + offset / EyeManager.PixelsPerMeter * scale);

        // Draw the underlying bar texture
        handle.DrawTexture(_barTexture, position);

        Color color;
        float elapsedRatio;

        /*
        // if we're cancelled then flick red / off.
        if (doAfter.CancelledTime != null)
        {
            var elapsed = doAfter.CancelledTime.Value - doAfter.StartTime;
            elapsedRatio = (float)Math.Min(1, elapsed.TotalSeconds / doAfter.Args.Delay.TotalSeconds);
            var cancelElapsed = (time - doAfter.CancelledTime.Value).TotalSeconds;
            var flash = Math.Floor(cancelElapsed / FlashTime) % 2 == 0;
            color = GetProgressColor(0, flash ? alpha : 0);
        }
        else
        {
            var elapsed = time - doAfter.StartTime;
            elapsedRatio = (float)Math.Min(1, elapsed.TotalSeconds / doAfter.Args.Delay.TotalSeconds);
            color = GetProgressColor(elapsedRatio, alpha);
        }
        */
        elapsedRatio = (float)Math.Min(1, comp.CombatTurnTimer / comp.CombatTurnDuration);
        color = GetProgressColor(elapsedRatio, alpha);

        var xProgress = (EndX - StartX) * elapsedRatio + StartX;
        var box = new Box2(new Vector2(StartX, 3f) / EyeManager.PixelsPerMeter, new Vector2(xProgress, 4f) / EyeManager.PixelsPerMeter);
        box = box.Translated(position);
        handle.DrawRect(box, color);
    }

    public Color GetProgressColor(float progress, float alpha = 1f)
    {
        return _progressColor.GetProgressColor(progress).WithAlpha(alpha);
    }
}
