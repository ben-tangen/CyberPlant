#nullable enable
using Godot;
using CyberPlant.Core;

namespace CyberPlant.UI;

public partial class MainMenu : Control
{
    [Export(PropertyHint.File, "*.tscn")]
    public string FirstLevelScenePath { get; set; } = "res://scenes/Levels/Home.tscn";

    [Export] public Label? TitleLabel;
    [Export] public Button? StartButton;
    [Export] public Button? QuitButton;
    [Export] public float TitleFloatDistance { get; set; } = 12.0f;
    [Export] public float TitleFloatDuration { get; set; } = 2.0f;

    private float _titleBaseY;
    private double _titleFloatElapsed = 0.0;

    public override void _Ready()
    {
        if (TitleLabel != null)
        {
            _titleBaseY = TitleLabel.Position.Y;
        }

        if (StartButton != null)
        {
            StartButton.Pressed += OnStartButtonPressed;
        }

        if (QuitButton != null)
        {
            QuitButton.Pressed += OnQuitButtonPressed;
        }
    }

    public override void _Process(double delta)
    {
        if (TitleLabel == null || TitleFloatDuration <= 0.0f)
        {
            return;
        }

        _titleFloatElapsed += delta;

        float cycle = (float)(_titleFloatElapsed * Mathf.Tau / TitleFloatDuration);
        var position = TitleLabel.Position;
        position.Y = _titleBaseY + Mathf.Sin(cycle) * TitleFloatDistance;
        TitleLabel.Position = position;
    }

    private void OnStartButtonPressed()
    {
        var gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
        gameManager?.ResetRunState();

        GetTree().ChangeSceneToFile(FirstLevelScenePath);
    }

    private void OnQuitButtonPressed()
    {
        GetTree().Quit();
    }
}
