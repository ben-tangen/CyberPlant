#nullable enable
using Godot;
using System.Collections.Generic;

namespace CyberPlant.Core;

public sealed class InventoryItem
{
    public InventoryItem(
        string id,
        string displayName,
        Texture2D? icon = null,
        IReadOnlyList<Texture2D>? animationFrames = null,
        float animationSpeed = 8.0f)
    {
        Id = id;
        DisplayName = displayName;
        Icon = icon;
        AnimationFrames = animationFrames;
        AnimationSpeed = animationSpeed;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public Texture2D? Icon { get; }

    public IReadOnlyList<Texture2D>? AnimationFrames { get; }

    public float AnimationSpeed { get; }
}
