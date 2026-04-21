#nullable enable
using Godot;
using CyberPlant.Core;
using PlayerCharacter = CyberPlant.Player.Player;

namespace CyberPlant.UI;

public partial class ShopUI : CanvasLayer
{
    private const int HealCost = 20;
    private const int HealAmount = 25;

    private Button? _buyHealButton;
    private Button? _closeButton;
    private Label? _statusLabel;
    private PanelContainer? _panel;
    private GameManager? _gameManager;

    public override void _Ready()
    {
        _panel = GetNodeOrNull<PanelContainer>("PanelContainer");
        _buyHealButton = GetNodeOrNull<Button>("PanelContainer/VBoxContainer/BuyHealButton");
        _closeButton = GetNodeOrNull<Button>("PanelContainer/VBoxContainer/CloseButton");
        _statusLabel = GetNodeOrNull<Label>("PanelContainer/VBoxContainer/StatusLabel");
        _gameManager = GetNodeOrNull<GameManager>("/root/GameManager");

        if (_buyHealButton != null)
        {
            _buyHealButton.Pressed += BuyHeal;
        }

        if (_closeButton != null)
        {
            _closeButton.Pressed += CloseShop;
        }

        CloseShop();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_panel?.Visible == true && @event.IsActionPressed("ui_cancel"))
        {
            CloseShop();
            GetViewport().SetInputAsHandled();
        }
    }

    public void OpenShop()
    {
        if (_panel == null)
        {
            return;
        }

        _panel.Visible = true;
        SetStatus("Buy 25 health for 20 water.");
    }

    public void CloseShop()
    {
        if (_panel != null)
        {
            _panel.Visible = false;
        }
    }

    private void BuyHeal()
    {
        if (_gameManager == null)
        {
            SetStatus("Shop is not connected.");
            return;
        }

        if (_gameManager.CurrentPlayer is not PlayerCharacter player)
        {
            SetStatus("Player not found.");
            return;
        }

        if (!_gameManager.TrySpendWater(HealCost))
        {
            SetStatus("Not enough water.");
            return;
        }

        player.Heal(HealAmount);
        SetStatus($"Healed {HealAmount} health.");
    }

    private void SetStatus(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = message;
        }
    }
}
