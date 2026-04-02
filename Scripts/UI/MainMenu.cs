using Godot;
using CyberPlant.Core;

namespace CyberPlant.UI;

public partial class MainMenu : Control
{
    [Export(PropertyHint.File, "*.tscn")]
    public string FirstLevelScenePath { get; set; } = "res://Scenes/Levels/Level01.tscn";

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
