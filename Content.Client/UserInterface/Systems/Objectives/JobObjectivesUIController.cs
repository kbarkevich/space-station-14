using System.Linq;
using Content.Client.CharacterInfo;
using Content.Client.Gameplay;
using Content.Client.Mind;
using Content.Client.Roles;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Character.Controls;
using Content.Client.UserInterface.Systems.Character.Windows;
using Content.Client.UserInterface.Systems.Objectives.Controls;
using Content.Client.UserInterface.Systems.Objectives.Windows;
using Content.Shared.Input;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Objectives;

[UsedImplicitly]
public sealed partial class JobObjectivesUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<CharacterInfoSystem>
{
    [Dependency] private IPlayerManager _player = default!;
    [UISystemDependency] private readonly CharacterInfoSystem _characterInfo = default!;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeNetworkEvent<MindRoleTypeChangedEvent>(OnRoleTypeChanged);
    }

    private JobObjectivesWindow? _window;

    private MenuButton? JobObjectivesButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.JobObjectivesButton;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);

        _window = UIManager.CreateWindow<JobObjectivesWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);

        _window.OnClose += DeactivateButton;
        _window.OnOpen += ActivateButton;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenJobObjectivesMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<JobObjectivesUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.Close();
            _window = null;
        }

        CommandBinds.Unregister<JobObjectivesUIController>();
    }

    public void OnSystemLoaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate += CharacterUpdated;
        _player.LocalPlayerDetached += CharacterDetached;
    }

    public void OnSystemUnloaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate -= CharacterUpdated;
        _player.LocalPlayerDetached -= CharacterDetached;
    }

    public void UnloadButton()
    {
        if (JobObjectivesButton == null)
        {
            return;
        }

        JobObjectivesButton.OnPressed -= JobObjectivesButtonPressed;
    }

    public void LoadButton()
    {
        if (JobObjectivesButton == null)
        {
            return;
        }

        JobObjectivesButton.OnPressed += JobObjectivesButtonPressed;
    }

    private void DeactivateButton()
    {
        if (JobObjectivesButton == null)
        {
            return;
        }

        JobObjectivesButton.Pressed = false;
    }

    private void ActivateButton()
    {
        if (JobObjectivesButton == null)
        {
            return;
        }

        JobObjectivesButton.Pressed = true;
    }

    private void CharacterUpdated(CharacterInfoSystem.CharacterData data)
    {
        if (_window == null)
        {
            return;
        }

        var (entity, job, objectives, briefing, entityName, departmentNames) = data;

        _window.SpriteView.SetEntity(entity);

        _window.NameLabel.Text = entityName + " (" + job + ")";
        _window.SubText.Text = departmentNames;
    }

    private void OnRoleTypeChanged(MindRoleTypeChangedEvent ev, EntitySessionEventArgs _)
    {

    }

    private void CharacterDetached(EntityUid uid)
    {
        CloseWindow();
    }

    private void JobObjectivesButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void CloseWindow()
    {
        _window?.Close();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;

        JobObjectivesButton?.SetClickPressed(!_window.IsOpen);

        if (_window.IsOpen)
        {
            CloseWindow();
        }
        else
        {
            _characterInfo.RequestCharacterInfo();
            _window.Open();
        }
    }
}