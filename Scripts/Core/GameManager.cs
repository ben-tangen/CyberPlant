#nullable enable
using Godot;
using CyberPlant.Combat;

namespace CyberPlant.Core;

public partial class GameManager : Node
{
    public const int InventorySlotCount = 4;
    public const int MaxLevelNumber = 3;

    [Signal]
    public delegate void WaterChangedEventHandler(int newAmount);

    [Signal]
    public delegate void PlayerRegisteredEventHandler(Node2D player);

    [Signal]
    public delegate void InventorySlotChangedEventHandler(int slotIndex);

    [Signal]
    public delegate void ActiveInventorySlotChangedEventHandler(int slotIndex);

    [Signal]
    public delegate void LevelProgressChangedEventHandler(int highestUnlockedLevel);

    public int Water { get; private set; }

    public int ActiveInventorySlotIndex { get; private set; }

    public int HighestUnlockedLevel { get; private set; } = 1;

    // We keep this as Node2D so systems can swap in subclasses (Player, debug dummies, etc.).
    public Node2D? CurrentPlayer { get; private set; }

    private readonly InventoryItem?[] _inventorySlots = new InventoryItem?[InventorySlotCount];

    public override void _Ready()
    {
        InitializeInventory();

        Water = 0;
        EmitSignal(SignalName.WaterChanged, Water);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        switch (key.Keycode)
        {
            case Key.Key1:
                SetActiveInventorySlot(0);
                break;
            case Key.Key2:
                SetActiveInventorySlot(1);
                break;
            case Key.Key3:
                SetActiveInventorySlot(2);
                break;
            case Key.Key4:
                SetActiveInventorySlot(3);
                break;
        }
    }

    public void RegisterPlayer(Node2D player)
    {
        CurrentPlayer = player;
        EmitSignal(SignalName.PlayerRegistered, player);
    }

    public void AddWater(int amount)
    {
        Water = Mathf.Max(0, Water + amount);
        EmitSignal(SignalName.WaterChanged, Water);
    }

    public bool TrySpendWater(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (Water < amount)
        {
            return false;
        }

        Water -= amount;
        EmitSignal(SignalName.WaterChanged, Water);
        return true;
    }

    public void SetWater(int amount)
    {
        Water = Mathf.Max(0, amount);
        EmitSignal(SignalName.WaterChanged, Water);
    }

    public InventoryItem? GetInventorySlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return null;
        }

        return _inventorySlots[slotIndex];
    }

    public InventoryItem? GetActiveInventoryItem()
    {
        return GetInventorySlot(ActiveInventorySlotIndex);
    }

    public bool HasInventoryItem(string itemId)
    {
        foreach (var inventoryItem in _inventorySlots)
        {
            if (inventoryItem?.Id == itemId)
            {
                return true;
            }
        }

        return false;
    }

    public int TryAddInventoryItemToFirstEmptySlot(InventoryItem item, int startSlotIndex = 1)
    {
        if (item == null)
        {
            return -1;
        }

        int clampedStartIndex = Mathf.Clamp(startSlotIndex, 0, InventorySlotCount - 1);
        for (int slotIndex = clampedStartIndex; slotIndex < InventorySlotCount; slotIndex += 1)
        {
            if (_inventorySlots[slotIndex] == null)
            {
                SetInventorySlot(slotIndex, item);
                return slotIndex;
            }
        }

        return -1;
    }

    public void SetInventorySlot(int slotIndex, InventoryItem? item)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return;
        }

        _inventorySlots[slotIndex] = item;
        EmitSignal(SignalName.InventorySlotChanged, slotIndex);
    }

    public void SetActiveInventorySlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex) || ActiveInventorySlotIndex == slotIndex)
        {
            return;
        }

        ActiveInventorySlotIndex = slotIndex;
        EmitSignal(SignalName.ActiveInventorySlotChanged, ActiveInventorySlotIndex);
    }

    public void ResetRunState()
    {
        // TODO (Team): Expand this with checkpoint/level/score state as systems come online.
        InitializeInventory();
        HighestUnlockedLevel = 1;
        EmitSignal(SignalName.LevelProgressChanged, HighestUnlockedLevel);
        Water = 0;
        EmitSignal(SignalName.WaterChanged, Water);
    }

    public bool IsLevelUnlocked(int levelNumber)
    {
        return levelNumber <= HighestUnlockedLevel;
    }

    public void CompleteLevel(int completedLevelNumber)
    {
        int nextLevel = Mathf.Clamp(completedLevelNumber + 1, 1, MaxLevelNumber);
        if (nextLevel > HighestUnlockedLevel)
        {
            HighestUnlockedLevel = nextLevel;
            EmitSignal(SignalName.LevelProgressChanged, HighestUnlockedLevel);
        }
    }

    private void InitializeInventory()
    {
        for (int slotIndex = 0; slotIndex < InventorySlotCount; slotIndex += 1)
        {
            _inventorySlots[slotIndex] = null;
        }

        _inventorySlots[0] = WeaponCatalog.CreateInventoryItem("kick")
            ?? new InventoryItem("kick", "Kick", GD.Load<Texture2D>("res://assets/player/player_kick.png"));
        ActiveInventorySlotIndex = 0;

        for (int slotIndex = 0; slotIndex < InventorySlotCount; slotIndex += 1)
        {
            EmitSignal(SignalName.InventorySlotChanged, slotIndex);
        }

        EmitSignal(SignalName.ActiveInventorySlotChanged, ActiveInventorySlotIndex);
    }

    private static bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < InventorySlotCount;
    }
}
