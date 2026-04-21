#nullable enable
using Godot;
using CyberPlant.Core;
using PlayerCharacter = CyberPlant.Player.Player;

namespace CyberPlant.UI;

public partial class HUD : CanvasLayer
{
    private const string HealthBarPath = "MarginContainer/VBoxContainer/HealthBar";
    private const string WaterLabelPath = "WaterContainer/WaterLabel";
    private const string InventoryRowPath = "MarginContainer/VBoxContainer/InventoryRow";

    private static readonly string[] InventorySlotPanelPaths =
    {
        "MarginContainer/VBoxContainer/InventoryRow/Slot1",
        "MarginContainer/VBoxContainer/InventoryRow/Slot2",
        "MarginContainer/VBoxContainer/InventoryRow/Slot3",
        "MarginContainer/VBoxContainer/InventoryRow/Slot4",
    };

    private static readonly string[] InventorySlotTexturePaths =
    {
        "MarginContainer/VBoxContainer/InventoryRow/Slot1/TextureRect",
        "MarginContainer/VBoxContainer/InventoryRow/Slot2/TextureRect",
        "MarginContainer/VBoxContainer/InventoryRow/Slot3/TextureRect",
        "MarginContainer/VBoxContainer/InventoryRow/Slot4/TextureRect",
    };

    [Export] public ProgressBar? HealthBar;
    [Export] public Label? WaterLabel;

    private GameManager? _gameManager;
    private PlayerCharacter? _player;
    private readonly PanelContainer?[] _inventorySlots = new PanelContainer?[GameManager.InventorySlotCount];
    private readonly TextureRect?[] _inventoryIcons = new TextureRect?[GameManager.InventorySlotCount];
    private readonly StyleBoxFlat _inactiveSlotStyle = new();
    private readonly StyleBoxFlat _activeSlotStyle = new();
    private readonly StyleBoxFlat _healthFillStyle = new();

    public override void _Ready()
    {
        ConfigureHealthBarStyle();
        ConfigureInventorySlotStyles();
        HealthBar ??= GetNodeOrNull<ProgressBar>(HealthBarPath);
        WaterLabel ??= GetNodeOrNull<Label>(WaterLabelPath);
        BindInventoryNodes();

        _gameManager = GetNodeOrNull<GameManager>("/root/GameManager");

        if (_gameManager == null)
        {
            GD.PushWarning("GameManager autoload not found. HUD will not receive data.");
            return;
        }

        if (HealthBar == null)
        {
            GD.PushWarning($"HUD could not find its health bar at '{HealthBarPath}'.");
        }

        if (WaterLabel == null)
        {
            GD.PushWarning($"HUD could not find its water label at '{WaterLabelPath}'.");
        }

        if (GetNodeOrNull<Node>(InventoryRowPath) == null)
        {
            GD.PushWarning($"HUD could not find its inventory row at '{InventoryRowPath}'.");
        }

        _gameManager.Connect(GameManager.SignalName.WaterChanged, new Callable(this, nameof(OnWaterChanged)));
        _gameManager.Connect(GameManager.SignalName.PlayerRegistered, new Callable(this, nameof(OnPlayerRegistered)));
        _gameManager.Connect(GameManager.SignalName.InventorySlotChanged, new Callable(this, nameof(OnInventorySlotChanged)));
        _gameManager.Connect(GameManager.SignalName.ActiveInventorySlotChanged, new Callable(this, nameof(OnActiveInventorySlotChanged)));

        OnWaterChanged(_gameManager.Water);
        RefreshInventory();

        if (_gameManager.CurrentPlayer is PlayerCharacter existingPlayer)
        {
            BindPlayer(existingPlayer);
        }
    }

    private void OnPlayerRegistered(Node2D registeredPlayer)
    {
        if (registeredPlayer is PlayerCharacter player)
        {
            BindPlayer(player);
        }
    }

    private void BindPlayer(PlayerCharacter player)
    {
        if (_player != null && _player.IsConnected(PlayerCharacter.SignalName.HealthChanged, new Callable(this, nameof(OnHealthChanged))))
        {
            _player.Disconnect(PlayerCharacter.SignalName.HealthChanged, new Callable(this, nameof(OnHealthChanged)));
        }

        _player = player;

        // TODO (Ben): Add stamina/ability widgets here once combat systems are defined.
        _player.Connect(PlayerCharacter.SignalName.HealthChanged, new Callable(this, nameof(OnHealthChanged)));
        OnHealthChanged(_player.CurrentHealth, _player.MaxHealth);
    }

    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (HealthBar == null)
        {
            return;
        }

        HealthBar.MaxValue = maxHealth;
        HealthBar.Value = currentHealth;
        UpdateHealthBarColor(currentHealth, maxHealth);
    }

    private void OnWaterChanged(int newAmount)
    {
        if (WaterLabel == null)
        {
            return;
        }

        WaterLabel.Text = $"Water: {newAmount}";
    }

    private void OnInventorySlotChanged(int slotIndex)
    {
        UpdateInventorySlot(slotIndex);
    }

    private void OnActiveInventorySlotChanged(int slotIndex)
    {
        UpdateActiveSlotVisuals(slotIndex);
    }

    private void BindInventoryNodes()
    {
        for (int slotIndex = 0; slotIndex < GameManager.InventorySlotCount; slotIndex += 1)
        {
            _inventorySlots[slotIndex] = GetNodeOrNull<PanelContainer>(InventorySlotPanelPaths[slotIndex]);
            _inventoryIcons[slotIndex] = GetNodeOrNull<TextureRect>(InventorySlotTexturePaths[slotIndex]);
        }
    }

    private void RefreshInventory()
    {
        for (int slotIndex = 0; slotIndex < GameManager.InventorySlotCount; slotIndex += 1)
        {
            UpdateInventorySlot(slotIndex);
        }

        if (_gameManager != null)
        {
            UpdateActiveSlotVisuals(_gameManager.ActiveInventorySlotIndex);
        }
    }

    private void UpdateInventorySlot(int slotIndex)
    {
        if (_gameManager == null || !IsValidSlotIndex(slotIndex))
        {
            return;
        }

        var icon = _inventoryIcons[slotIndex];
        var item = _gameManager.GetInventorySlot(slotIndex);

        if (icon != null)
        {
            icon.Texture = item?.Icon;
            icon.Visible = item?.Icon != null;
            icon.Modulate = item == null
                ? new Color(1f, 1f, 1f, 0.2f)
                : Colors.White;
        }

        UpdateSlotSelectionStyle(slotIndex);
    }

    private void UpdateActiveSlotVisuals(int activeSlotIndex)
    {
        for (int slotIndex = 0; slotIndex < GameManager.InventorySlotCount; slotIndex += 1)
        {
            UpdateSlotSelectionStyle(slotIndex, activeSlotIndex);
        }
    }

    private void UpdateSlotSelectionStyle(int slotIndex)
    {
        if (_gameManager == null)
        {
            return;
        }

        UpdateSlotSelectionStyle(slotIndex, _gameManager.ActiveInventorySlotIndex);
    }

    private void UpdateSlotSelectionStyle(int slotIndex, int activeSlotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return;
        }

        var slot = _inventorySlots[slotIndex];
        if (slot == null)
        {
            return;
        }

        bool isActive = slotIndex == activeSlotIndex;
        slot.AddThemeStyleboxOverride("panel", isActive ? _activeSlotStyle : _inactiveSlotStyle);
        slot.SelfModulate = Colors.White;
    }

    private void ConfigureInventorySlotStyles()
    {
        _inactiveSlotStyle.BgColor = new Color(0.13f, 0.15f, 0.19f, 0.95f);
        _inactiveSlotStyle.BorderWidthLeft = 2;
        _inactiveSlotStyle.BorderWidthTop = 2;
        _inactiveSlotStyle.BorderWidthRight = 2;
        _inactiveSlotStyle.BorderWidthBottom = 2;
        _inactiveSlotStyle.BorderColor = new Color(0.24f, 0.27f, 0.33f, 1f);
        _inactiveSlotStyle.CornerRadiusTopLeft = 8;
        _inactiveSlotStyle.CornerRadiusTopRight = 8;
        _inactiveSlotStyle.CornerRadiusBottomLeft = 8;
        _inactiveSlotStyle.CornerRadiusBottomRight = 8;

        _activeSlotStyle.BgColor = new Color(0.18f, 0.21f, 0.27f, 1f);
        _activeSlotStyle.BorderWidthLeft = 4;
        _activeSlotStyle.BorderWidthTop = 4;
        _activeSlotStyle.BorderWidthRight = 4;
        _activeSlotStyle.BorderWidthBottom = 4;
        _activeSlotStyle.BorderColor = new Color(0.32f, 0.89f, 0.71f, 1f);
        _activeSlotStyle.CornerRadiusTopLeft = 8;
        _activeSlotStyle.CornerRadiusTopRight = 8;
        _activeSlotStyle.CornerRadiusBottomLeft = 8;
        _activeSlotStyle.CornerRadiusBottomRight = 8;
    }

    private void ConfigureHealthBarStyle()
    {
        _healthFillStyle.CornerRadiusTopLeft = 5;
        _healthFillStyle.CornerRadiusTopRight = 5;
        _healthFillStyle.CornerRadiusBottomLeft = 5;
        _healthFillStyle.CornerRadiusBottomRight = 5;
    }

    private void UpdateHealthBarColor(int currentHealth, int maxHealth)
    {
        if (HealthBar == null)
        {
            return;
        }

        float healthPercent = maxHealth <= 0 ? 0f : Mathf.Clamp((float)currentHealth / maxHealth, 0f, 1f);
        _healthFillStyle.BgColor = GetHealthColor(healthPercent);
        HealthBar.AddThemeStyleboxOverride("fill", _healthFillStyle);
    }

    private static Color GetHealthColor(float healthPercent)
    {
        if (healthPercent >= 0.66f)
        {
            float t = (healthPercent - 0.66f) / 0.34f;
            return new Color(1f, 1f, 0f).Lerp(new Color(0.2f, 0.9f, 0.2f), t);
        }

        if (healthPercent >= 0.33f)
        {
            float t = (healthPercent - 0.33f) / 0.33f;
            return new Color(1f, 0.55f, 0f).Lerp(new Color(1f, 1f, 0f), t);
        }

        float redBlend = healthPercent / 0.33f;
        return new Color(1f, 0.15f, 0.15f).Lerp(new Color(1f, 0.55f, 0f), redBlend);
    }

    private static bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < GameManager.InventorySlotCount;
    }
}
