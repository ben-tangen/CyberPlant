#nullable enable
using Godot;

namespace CyberPlant.Core;

public partial class GameManager : Node
{
    [Signal]
    public delegate void WaterChangedEventHandler(int newAmount);

    [Signal]
    public delegate void PlayerRegisteredEventHandler(Node2D player);

    public int Water { get; private set; }

    // We keep this as Node2D so systems can swap in subclasses (Player, debug dummies, etc.).
    public Node2D? CurrentPlayer { get; private set; }

    public override void _Ready()
    {
        Water = 0;
        EmitSignal(SignalName.WaterChanged, Water);
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

    public void SetWater(int amount)
    {
        Water = Mathf.Max(0, amount);
        EmitSignal(SignalName.WaterChanged, Water);
    }

    public void ResetRunState()
    {
        // TODO (Team): Expand this with checkpoint/level/score state as systems come online.
        Water = 0;
        EmitSignal(SignalName.WaterChanged, Water);
    }
}