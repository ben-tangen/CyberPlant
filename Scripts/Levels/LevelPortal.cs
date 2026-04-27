#nullable enable
using Godot;
using CyberPlant.Core;

namespace CyberPlant.Levels;

public partial class LevelPortal : Area2D
{
    [Export] public int RequiredUnlockedLevel { get; set; } = 1;
    [Export(PropertyHint.File, "*.tscn")] public string TargetScenePath { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "LEVEL";
    [Export] public NodePath PromptLabelPath { get; set; } = "";
    [Export] public NodePath StatusLabelPath { get; set; } = "";

    private bool _playerNearby;
    private Label? _promptLabel;
    private Label? _statusLabel;
    private GameManager? _gameManager;

    public override void _Ready()
    {
        _gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
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

        if (!StatusLabelPath.IsEmpty)
        {
            _statusLabel = GetNodeOrNull<Label>(StatusLabelPath);
        }

        if (_gameManager != null)
        {
            _gameManager.Connect(GameManager.SignalName.LevelProgressChanged, new Callable(this, nameof(OnLevelProgressChanged)));
        }

        RefreshPortalText();
    }

    public override void _Process(double delta)
    {
        if (!_playerNearby || !Input.IsActionJustPressed("interact"))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(TargetScenePath))
        {
            return;
        }

        if (_gameManager != null && !_gameManager.IsLevelUnlocked(RequiredUnlockedLevel))
        {
            return;
        }

        GetTree().ChangeSceneToFile(TargetScenePath);
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

    private void OnLevelProgressChanged(int highestUnlockedLevel)
    {
        RefreshPortalText();
    }

    private void RefreshPortalText()
    {
        if (_statusLabel == null)
        {
            return;
        }

        bool unlocked = _gameManager?.IsLevelUnlocked(RequiredUnlockedLevel) ?? RequiredUnlockedLevel <= 1;
        _statusLabel.Text = unlocked
            ? $"{DisplayName}\nUNLOCKED"
            : $"{DisplayName}\nLOCKED";
    }
}
