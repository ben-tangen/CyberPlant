#nullable enable
using Godot;
using CyberPlant.Core;
using CyberPlant.Combat;
using PlayerCharacter = CyberPlant.Player.Player;

namespace CyberPlant.UI;

public partial class ShopUI : CanvasLayer
{
    private const int HealCost = 20;
    private const int HealAmount = 25;
    private const string ThornBladeId = "thorn_blade";
    private const string VineWhipId = "vine_whip";
    private const string SporeBurstId = "spore_burst";

    private Button? _buyHealButton;
    private Button? _buyThornBladeButton;
    private Button? _buyVineWhipButton;
    private Button? _buySporeBurstButton;
    private Button? _closeButton;
    private Label? _statusLabel;
    private PanelContainer? _panel;
    private GameManager? _gameManager;
    private int _currentLevelNumber = 1;

    public override void _Ready()
    {
        _panel = GetNodeOrNull<PanelContainer>("PanelContainer");
        _buyHealButton = GetNodeOrNull<Button>("PanelContainer/VBoxContainer/BuyHealButton");
        _buyThornBladeButton = GetNodeOrNull<Button>("PanelContainer/VBoxContainer/BuyThornBladeButton");
        _buyVineWhipButton = GetNodeOrNull<Button>("PanelContainer/VBoxContainer/BuyVineWhipButton");
        _buySporeBurstButton = GetNodeOrNull<Button>("PanelContainer/VBoxContainer/BuySporeBurstButton");
        _closeButton = GetNodeOrNull<Button>("PanelContainer/VBoxContainer/CloseButton");
        _statusLabel = GetNodeOrNull<Label>("PanelContainer/VBoxContainer/StatusLabel");
        _gameManager = GetNodeOrNull<GameManager>("/root/GameManager");

        if (_buyHealButton != null)
        {
            _buyHealButton.Pressed += BuyHeal;
        }

        if (_buyThornBladeButton != null)
        {
            _buyThornBladeButton.Pressed += BuyThornBlade;
        }

        if (_buyVineWhipButton != null)
        {
            _buyVineWhipButton.Pressed += BuyVineWhip;
        }

        if (_buySporeBurstButton != null)
        {
            _buySporeBurstButton.Pressed += BuySporeBurst;
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
        _currentLevelNumber = _gameManager?.HighestUnlockedLevel ?? 1;
        RefreshWeaponButtonLabels();
        SetStatus($"Level {_currentLevelNumber} shop open. Press 1-4 to switch active slot.");
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

    private void BuyThornBlade()
    {
        TryBuyWeapon(ThornBladeId);
    }

    private void BuyVineWhip()
    {
        TryBuyWeapon(VineWhipId);
    }

    private void BuySporeBurst()
    {
        TryBuyWeapon(SporeBurstId);
    }

    private void TryBuyWeapon(string weaponId)
    {
        if (_gameManager == null)
        {
            SetStatus("Shop is not connected.");
            return;
        }

        var definition = WeaponCatalog.GetDefinition(weaponId);
        if (definition == null || !definition.Purchasable)
        {
            SetStatus("Weapon data missing.");
            return;
        }

        if (_currentLevelNumber < definition.RequiredLevel)
        {
            SetStatus($"{definition.DisplayName} unlocks in Level {definition.RequiredLevel}.");
            return;
        }

        if (_gameManager.HasInventoryItem(weaponId))
        {
            SetStatus($"{definition.DisplayName} already owned.");
            return;
        }

        if (!_gameManager.TrySpendWater(definition.ShopCost))
        {
            SetStatus($"Need {definition.ShopCost} water for {definition.DisplayName}.");
            return;
        }

        var inventoryItem = WeaponCatalog.CreateInventoryItem(weaponId);
        if (inventoryItem == null)
        {
            _gameManager.AddWater(definition.ShopCost);
            SetStatus("Failed to create weapon item.");
            return;
        }

        int slotIndex = _gameManager.TryAddInventoryItemToFirstEmptySlot(inventoryItem, 1);
        if (slotIndex == -1)
        {
            _gameManager.AddWater(definition.ShopCost);
            SetStatus("Inventory is full.");
            return;
        }

        _gameManager.SetActiveInventorySlot(slotIndex);
        RefreshWeaponButtonLabels();
        SetStatus($"Unlocked {definition.DisplayName} in slot {slotIndex + 1}.");
    }

    private void RefreshWeaponButtonLabels()
    {
        UpdateWeaponButton(_buyThornBladeButton, ThornBladeId);
        UpdateWeaponButton(_buyVineWhipButton, VineWhipId);
        UpdateWeaponButton(_buySporeBurstButton, SporeBurstId);
    }

    private void UpdateWeaponButton(Button? button, string weaponId)
    {
        if (button == null)
        {
            return;
        }

        var definition = WeaponCatalog.GetDefinition(weaponId);
        if (definition == null)
        {
            button.Text = "Weapon Missing";
            button.Disabled = true;
            return;
        }

        bool alreadyOwned = _gameManager?.HasInventoryItem(weaponId) == true;
        bool lockedByLevel = _currentLevelNumber < definition.RequiredLevel;
        button.Disabled = alreadyOwned || lockedByLevel;

        if (alreadyOwned)
        {
            button.Text = $"{definition.DisplayName} - Owned";
            return;
        }

        if (lockedByLevel)
        {
            button.Text = $"{definition.DisplayName} - Unlocks L{definition.RequiredLevel}";
            return;
        }

        button.Text = $"Buy {definition.DisplayName} - {definition.ShopCost} Water";
    }

    private void SetStatus(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = message;
        }
    }
}
