using System.Numerics;
using Content.Client.UserInterface.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Containers;
using Content.Shared.CombatMode;

namespace Content.Client.CombatMode;

public sealed class TargetMarkerOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    private readonly IEntityManager _entManager;
    private readonly IPlayerManager _player;
    private readonly SharedTransformSystem _transform;
    private readonly SharedContainerSystem _container;
    private readonly SpriteSystem _sprite;

    private readonly Texture _barTexture;
    private readonly ShaderInstance _unshadedShader;

    /// <summary>
    ///     Flash time for cancelled turns
    /// </summary>
    private const float FlashTime = 0.125f;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public TargetMarkerOverlay(IEntityManager entManager, IPrototypeManager protoManager, IPlayerManager player)
    {
        _entManager = entManager;
        _player = player;
        _transform = _entManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        _container = _entManager.EntitySysManager.GetEntitySystem<SharedContainerSystem>();
        _sprite = _entManager.System<SpriteSystem>();
        var sprite = new SpriteSpecifier.Rsi(new("/Textures/Objects/Misc/killsign.rsi"), "bald");
        _barTexture = _entManager.EntitySysManager.GetEntitySystem<SpriteSystem>().Frame0(sprite);

        _unshadedShader = protoManager.Index(UnshadedShader).Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var combatQuery = _entManager.GetEntityQuery<CombatModeComponent>();

        var handle = args.WorldHandle;
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var spriteQuery = _entManager.GetEntityQuery<SpriteComponent>();

        var localEnt = _player.LocalSession?.AttachedEntity;
        if (!combatQuery.TryGetComponent(localEnt, out var combatModeComponent) ||
                combatModeComponent.Target is null ||
                !xformQuery.TryGetComponent(combatModeComponent.Target, out var xform) ||
                !spriteQuery.TryGetComponent(combatModeComponent.Target, out var sprite) ||
                xform.MapID != args.MapId)
            return;

        // If you use the display UI scale then need to set max(1f, displayscale) because 0 is valid.
        const float scale = 1f;
        var scaleMatrix = Matrix3Helpers.CreateScale(new Vector2(scale, scale));
        var rotationMatrix = Matrix3Helpers.CreateRotation(-rotation);
        var bounds = args.WorldAABB.Enlarged(5f);

        var metaQuery = _entManager.GetEntityQuery<MetaDataComponent>();

        DrawTargetMarker((EntityUid)combatModeComponent.Target, sprite, xform, xformQuery, scaleMatrix, rotationMatrix, bounds, metaQuery, handle, scale);

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawTargetMarker(EntityUid uid, SpriteComponent sprite, TransformComponent xform, EntityQuery<TransformComponent> xformQuery,
                            Matrix3x2 scaleMatrix, Matrix3x2 rotationMatrix, Box2 bounds, EntityQuery<MetaDataComponent> metaQuery,
                            DrawingHandleWorld handle, float scale)
    {

        var worldPosition = _transform.GetWorldPosition(xform, xformQuery);
        if (!bounds.Contains(worldPosition))
            return;

        handle.UseShader(_unshadedShader);

        var meta = metaQuery.GetComponent(uid);

        var worldMatrix = Matrix3Helpers.CreateTranslation(worldPosition);
        var scaledWorld = Matrix3x2.Multiply(scaleMatrix, worldMatrix);
        var matty = Matrix3x2.Multiply(rotationMatrix, scaledWorld);
        handle.SetTransform(matty);

        float offset = 0f;

        var isInContainer = _container.IsEntityOrParentInContainer(uid, meta, xform);

        // Dim target indicator for players who are inside something.
        var alpha = 1.0f;
        if (isInContainer)
        {
            alpha = 0.5f;
        }

        // Use the sprite itself if we know its bounds. This means short or tall sprites don't get overlapped
        // by the indicator.
        var yOffset = _sprite.GetLocalBounds((uid, sprite)).Height / 2f + 0.05f;

        // Position above the entity (we've already applied the matrix transform to the entity itself and
        // calculated the height of any existing DoAfter bars)
        var position = new Vector2(-_barTexture.Width / 2f / EyeManager.PixelsPerMeter,
            yOffset / scale + offset / EyeManager.PixelsPerMeter * scale);

        // Draw the underlying bar texture
        handle.DrawTexture(_barTexture, position, new Color(1f, 1f, 1f, alpha));
    }
}
