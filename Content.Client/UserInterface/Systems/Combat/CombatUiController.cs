using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Combat.Widgets;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Systems.Combat;

public sealed class CombatUIController : UIController
{
    private TextureButton? _attackButton;
    private TextureButton? _defendButton;
    private TextureButton? _fleeButton;
    private TextureButton? _giveupButton;

    private TextureButton? _strikeButton;
    private TextureButton? _shootButton;
    private TextureButton? _throwButton;

    private BoxContainer? _combatPanel;

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
    }

    private void OnScreenLoad()
    {
        ReloadCombatMenu();
    }

    private void MakeButtonsVisible()
    {
        _attackButton?.Visible = true;
        _defendButton?.Visible = true;
        _fleeButton?.Visible = true;
        _giveupButton?.Visible = true;

        _strikeButton?.Visible = true;
        _shootButton?.Visible = true;
        _throwButton?.Visible = true;

        _combatPanel?.Visible = true;
    }

    public void ReloadCombatMenu()
    {
        if (UIManager.ActiveScreen == null)
        {
            return;
        }

        if (UIManager.GetActiveUIWidgetOrNull<CombatGui>() is { } combatGui)
        {
            RegisterAttackButton(combatGui.AttackButton);
            RegisterDefendButton(combatGui.DefendButton);
            RegisterFleeButton(combatGui.FleeButton);
            RegisterGiveUpButton(combatGui.GiveUpButton);
            RegisterStrikeButton(combatGui.StrikeButton);
            RegisterShootButton(combatGui.ShootButton);
            RegisterThrowButton(combatGui.ThrowButton);
            RegisterCombatPanel(combatGui.CombatPanel);
        }

        MakeButtonsVisible();
    }

    public void RegisterAttackButton(TextureButton? button)
    {
        if (_attackButton != null)
        {
            _attackButton.OnPressed -= AttackButtonPressed;
        }

        if (button != null)
        {
            _attackButton = button;
            _attackButton.OnPressed += AttackButtonPressed;
        }
    }

    public void RegisterDefendButton(TextureButton? button)
    {
        if (_defendButton != null)
        {
            _defendButton.OnPressed -= DefendButtonPressed;
        }

        if (button != null)
        {
            _defendButton = button;
            _defendButton.OnPressed += DefendButtonPressed;
        }
    }

    public void RegisterFleeButton(TextureButton? button)
    {
        if (_fleeButton != null)
        {
            _fleeButton.OnPressed -= FleeButtonPressed;
        }

        if (button != null)
        {
            _fleeButton = button;
            _fleeButton.OnPressed += FleeButtonPressed;
        }
    }

    public void RegisterGiveUpButton(TextureButton? button)
    {
        if (_giveupButton != null)
        {
            _giveupButton.OnPressed -= GiveUpButtonPressed;
        }

        if (button != null)
        {
            _giveupButton = button;
            _giveupButton.OnPressed += GiveUpButtonPressed;
        }
    }

    public void RegisterStrikeButton(TextureButton? button)
    {
        if (_strikeButton != null)
        {
            _strikeButton.OnPressed -= StrikeButtonPressed;
        }

        if (button != null)
        {
            _strikeButton = button;
            _strikeButton.OnPressed += StrikeButtonPressed;
        }
    }

    public void RegisterShootButton(TextureButton? button)
    {
        if (_shootButton != null)
        {
            _shootButton.OnPressed -= ShootButtonPressed;
        }

        if (button != null)
        {
            _shootButton = button;
            _shootButton.OnPressed += ShootButtonPressed;
        }
    }

    public void RegisterThrowButton(TextureButton? button)
    {
        if (_throwButton != null)
        {
            _throwButton.OnPressed -= ThrowButtonPressed;
        }

        if (button != null)
        {
            _throwButton = button;
            _throwButton.OnPressed += ThrowButtonPressed;
        }
    }

    public void RegisterCombatPanel(BoxContainer? panel)
    {
        if (panel != null)
            _combatPanel = panel;
    }

    private void AttackButtonPressed(BaseButton.ButtonEventArgs args)
    {
        if (args.Event.Function != EngineKeyFunctions.UIClick || _attackButton is null || _defendButton is null || _fleeButton is null || _giveupButton is null)
            return;

        foreach (var x in _attackButton.Children)
        {
            x.Visible = true;
        }
        foreach (var x in _defendButton.Children)
        {
            x.Visible = false;
        }
        foreach (var x in _fleeButton.Children)
        {
            x.Visible = false;
        }
        foreach (var x in _giveupButton.Children)
        {
            x.Visible = false;
        }

        Log.Log(LogLevel.Debug, "Attack button pressed!");
    }

    private void DefendButtonPressed(BaseButton.ButtonEventArgs args)
    {
        if (args.Event.Function != EngineKeyFunctions.UIClick || _attackButton is null || _defendButton is null || _fleeButton is null || _giveupButton is null)
            return;

        foreach (var x in _attackButton.Children)
        {
            x.Visible = false;
        }
        foreach (var x in _defendButton.Children)
        {
            x.Visible = true;
        }
        foreach (var x in _fleeButton.Children)
        {
            x.Visible = false;
        }
        foreach (var x in _giveupButton.Children)
        {
            x.Visible = false;
        }

        Log.Log(LogLevel.Debug, "Defend button pressed!");
    }

    private void FleeButtonPressed(BaseButton.ButtonEventArgs args)
    {
        if (args.Event.Function != EngineKeyFunctions.UIClick || _attackButton is null || _defendButton is null || _fleeButton is null || _giveupButton is null)
            return;

        foreach (var x in _attackButton.Children)
        {
            x.Visible = false;
        }
        foreach (var x in _defendButton.Children)
        {
            x.Visible = false;
        }
        foreach (var x in _fleeButton.Children)
        {
            x.Visible = true;
        }
        foreach (var x in _giveupButton.Children)
        {
            x.Visible = false;
        }

        Log.Log(LogLevel.Debug, "Flee button pressed!");
    }

    private void GiveUpButtonPressed(BaseButton.ButtonEventArgs args)
    {
        if (args.Event.Function != EngineKeyFunctions.UIClick || _attackButton is null || _defendButton is null || _fleeButton is null || _giveupButton is null)
            return;

        foreach (var x in _attackButton.Children)
        {
            x.Visible = false;
        }
        foreach (var x in _defendButton.Children)
        {
            x.Visible = false;
        }
        foreach (var x in _fleeButton.Children)
        {
            x.Visible = false;
        }
        foreach (var x in _giveupButton.Children)
        {
            x.Visible = true;
        }

        Log.Log(LogLevel.Debug, "Give Up button pressed!");
    }

    private void StrikeButtonPressed(BaseButton.ButtonEventArgs args)
    {
        if (args.Event.Function != EngineKeyFunctions.UIClick || _strikeButton is null || _shootButton is null || _throwButton is null)
            return;

        foreach (var x in _strikeButton.Children)
        {
            x.Visible = true;
        }
        foreach (var x in _shootButton.Children)
        {
            x.Visible = false;
        }
        foreach (var x in _throwButton.Children)
        {
            x.Visible = false;
        }

        Log.Log(LogLevel.Debug, "Strike button pressed!");
    }

    private void ShootButtonPressed(BaseButton.ButtonEventArgs args)
    {
        if (args.Event.Function != EngineKeyFunctions.UIClick || _strikeButton is null || _shootButton is null || _throwButton is null)
            return;

        foreach (var x in _strikeButton.Children)
        {
            x.Visible = false;
        }
        foreach (var x in _shootButton.Children)
        {
            x.Visible = true;
        }
        foreach (var x in _throwButton.Children)
        {
            x.Visible = false;
        }

        Log.Log(LogLevel.Debug, "Shoot button pressed!");
    }

    private void ThrowButtonPressed(BaseButton.ButtonEventArgs args)
    {
        if (args.Event.Function != EngineKeyFunctions.UIClick || _strikeButton is null || _shootButton is null || _throwButton is null)
            return;

        foreach (var x in _strikeButton.Children)
        {
            x.Visible = false;
        }
        foreach (var x in _shootButton.Children)
        {
            x.Visible = false;
        }
        foreach (var x in _throwButton.Children)
        {
            x.Visible = true;
        }

        Log.Log(LogLevel.Debug, "Throw button pressed!");
    }
}