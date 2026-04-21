#nullable enable
using Godot;
using CyberPlant.Core;

namespace CyberPlant.Pickups;

public partial class WaterPickup : Area2D
{
    [Export] public int WaterAmount { get; set; } = 10;
    [Export] public float FloatAmplitude { get; set; } = 6f;
    [Export] public float FloatSpeed { get; set; } = 2.5f;

    private Sprite2D? _waterSprite;
    private Vector2 _spriteStartPosition;
    private float _floatTime;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        _waterSprite = GetNodeOrNull<Sprite2D>("WaterSprite");

        if (_waterSprite != null)
        {
            _spriteStartPosition = _waterSprite.Position;
        }
        else
        {
            GD.PushWarning("WaterPickup could not find its WaterSprite node for floating animation.");
        }
    }

    public override void _Process(double delta)
    {
        if (_waterSprite == null)
        {
            return;
        }

        _floatTime += (float)delta;
        _waterSprite.Position = _spriteStartPosition + Vector2.Up * Mathf.Sin(_floatTime * FloatSpeed) * FloatAmplitude;
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
