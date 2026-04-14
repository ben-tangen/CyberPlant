using Godot;

namespace CyberPlant.Core;

public sealed class InventoryItem
{
    public InventoryItem(string id, string displayName, Texture2D? icon = null)
    {
        Id = id;
        DisplayName = displayName;
        Icon = icon;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public Texture2D? Icon { get; }
}
