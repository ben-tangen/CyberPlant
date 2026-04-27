#nullable enable
using Godot;
using CyberPlant.Core;

namespace CyberPlant.Levels;

public partial class LevelExitArea : Area2D
{
    [Export] public int CompletedLevelNumber { get; set; } = 1;
    [Export(PropertyHint.File, "*.tscn")] public string HomeScenePath { get; set; } = "res://Scenes/Levels/Home.tscn";
    [Export] public NodePath PromptLabelPath { get; set; } = "";

    private bool _playerNearby;
    private Label? _promptLabel;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        if (!PromptLabelPath.IsEmpty)
        {
            _promptLabel = GetNodeOrNull<Label>(PromptLabelPath);
            if (_promptLabel != null)
            {
                _promptLabel.Visible = false;
            }
        }
    }

    public override void _Process(double delta)
    {
        if (!_playerNearby || !Input.IsActionJustPressed("interact"))
        {
            return;
        }

        var gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
        gameManager?.CompleteLevel(CompletedLevelNumber);

        if (!string.IsNullOrWhiteSpace(HomeScenePath))
        {
            GetTree().ChangeSceneToFile(HomeScenePath);
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player"))
        {
            return;
        }

        _playerNearby = true;
        if (_promptLabel != null)
        {
            _promptLabel.Visible = true;
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player"))
        {
            return;
        }

        _playerNearby = false;
        if (_promptLabel != null)
        {
            _promptLabel.Visible = false;
        }
    }
}
