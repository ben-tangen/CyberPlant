using Godot;
using CyberPlant.Core;
using CyberPlant.Player;

namespace CyberPlant.UI;

public partial class HUD : CanvasLayer
{
    [Export] public ProgressBar? HealthBar;
    [Export] public Label? WaterLabel;

    private GameManager? _gameManager;
    private Player? _player;

    public override void _Ready()
    {
        _gameManager = GetNodeOrNull<GameManager>("/root/GameManager");

        if (_gameManager == null)
        {
            GD.PushWarning("GameManager autoload not found. HUD will not receive data.");
            return;
        }

        _gameManager.Connect(GameManager.SignalName.WaterChanged, new Callable(this, nameof(OnWaterChanged)));
        _gameManager.Connect(GameManager.SignalName.PlayerRegistered, new Callable(this, nameof(OnPlayerRegistered)));

        OnWaterChanged(_gameManager.Water);

        if (_gameManager.CurrentPlayer is Player existingPlayer)
        {
            BindPlayer(existingPlayer);
        }
    }

    private void OnPlayerRegistered(Node2D registeredPlayer)
    {
        if (registeredPlayer is Player player)
        {
            BindPlayer(player);
        }
    }

    private void BindPlayer(Player player)
    {
        _player = player;
        // TODO (Ben): Add stamina/ability widgets here once combat systems are defined.
        _player.Connect(Player.SignalName.HealthChanged, new Callable(this, nameof(OnHealthChanged)));
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
    }

    private void OnWaterChanged(int newAmount)
    {
        if (WaterLabel == null)
        {
            return;
        }

        WaterLabel.Text = $"Water: {newAmount}";
    }
}
