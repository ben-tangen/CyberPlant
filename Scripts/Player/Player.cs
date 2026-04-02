using Godot;
using CyberPlant.Core;

namespace CyberPlant.Player;

public partial class Player : CharacterBody2D
{
    [Signal]
    public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

    [Signal]
    public delegate void DiedEventHandler();

    [Export] public float MoveSpeed { get; set; } = 220.0f;
    [Export] public float JumpVelocity { get; set; } = -380.0f;

    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public int StartingHealth { get; set; } = 100;

    public int CurrentHealth { get; private set; }

    private float _gravity;

    public override void _Ready()
    {
        _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
        CurrentHealth = Mathf.Clamp(StartingHealth, 0, MaxHealth);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        var gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
        gameManager?.RegisterPlayer(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;

        float moveInput = Input.GetAxis("move_left", "move_right");
        velocity.X = moveInput * MoveSpeed;

        if (!IsOnFloor())
        {
            velocity.Y += _gravity * (float)delta;
        }

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    public void TakeDamage(int amount)
    {
        // TODO (Judah): Enemy attacks/projectiles should call this entry point.
        if (amount <= 0 || CurrentHealth <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        if (CurrentHealth == 0)
        {
            EmitSignal(SignalName.Died);
        }
    }

    public void Heal(int amount)
    {
        // TODO (Ben): Hook healing pickups/weapon effects here.
        if (amount <= 0 || CurrentHealth <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }
}
