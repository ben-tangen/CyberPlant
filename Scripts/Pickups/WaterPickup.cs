#nullable enable
using Godot;
using CyberPlant.Core;

namespace CyberPlant.Pickups;

public partial class WaterPickup : Area2D
{
    [Export] public int WaterAmount { get; set; } = 10;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node body)
    {
        var gameManager = GetNodeOrNull<GameManager>("/root/GameManager");

        if (body is CharacterBody2D && gameManager != null)
        {
            gameManager.AddWater(WaterAmount);
            QueueFree();
        }
    }
}