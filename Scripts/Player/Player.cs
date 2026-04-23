#nullable enable
using Godot;
using CyberPlant.Core;
using CyberPlant.Combat;

namespace CyberPlant.Player;

public partial class Player : CharacterBody2D, IDamageable
{
    private const string IdleAnimation = "idle";
    private const string IdleArmedAnimation = "idle_armed";
    private const string WalkAnimation = "walk";
    private const string WalkArmedAnimation = "walk_armed";
    private const string AttackAnimation = "attack";
    private const string AttackArmedAnimation = "attack_armed";

    [Signal]
    public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

    [Signal]
    public delegate void DiedEventHandler();

    [Export] public float MoveSpeed { get; set; } = 220.0f;
    [Export] public float JumpVelocity { get; set; } = -700.0f;
    [Export] public float FallGravityMultiplier { get; set; } = 1.8f;
    [Export] public float JumpReleaseGravityMultiplier { get; set; } = 2.4f;
    [Export] public float MaxFallSpeed { get; set; } = 1100.0f;

    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public int StartingHealth { get; set; } = 100;

    public int CurrentHealth { get; private set; }

    private float _gravity;
    private AnimatedSprite2D? _animatedSprite;
    private WeaponController? _weaponController;
    private GameManager? _gameManager;
    private Sprite2D? _weaponVisual;
    private double _weaponAnimationTime = 0.0;
    private bool _isWeaponAnimating = false;
    private bool _isFacingRight = true;
    private bool _hasActiveWeapon = false;

    public override void _Ready()
    {
        AddToGroup("player");

        _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
        _animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        CurrentHealth = Mathf.Clamp(StartingHealth, 0, MaxHealth);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        _gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
        _gameManager?.RegisterPlayer(this);

        _weaponController = GetNodeOrNull<WeaponController>("WeaponController");
        _weaponVisual = GetNodeOrNull<Sprite2D>("WeaponVisual");
        if (_gameManager != null)
        {
            UpdateActiveWeapon();
            _gameManager.Connect(GameManager.SignalName.ActiveInventorySlotChanged, new Callable(this, nameof(OnActiveInventorySlotChanged)));
        }

        UpdateVisualState(0.0f);
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;

        float moveInput = Input.GetAxis("move_left", "move_right");
        velocity.X = moveInput * MoveSpeed;

        if (!Mathf.IsZeroApprox(moveInput))
        {
            _isFacingRight = moveInput > 0.0f;
        }

        if (!IsOnFloor())
        {
            float gravityMultiplier = 1.0f;
            if (velocity.Y > 0.0f)
            {
                gravityMultiplier = FallGravityMultiplier;
            }
            else if (velocity.Y < 0.0f && !Input.IsActionPressed("jump"))
            {
                gravityMultiplier = JumpReleaseGravityMultiplier;
            }

            velocity.Y += _gravity * gravityMultiplier * (float)delta;
            velocity.Y = Mathf.Min(velocity.Y, MaxFallSpeed);
        }

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }

        if (Input.IsActionJustPressed("use_weapon"))
        {
            _weaponController?.Attack();
            PlayWeaponAnimation();
        }

        // Update weapon animation
        if (_isWeaponAnimating)
        {
            _weaponAnimationTime += delta;
            if (_weaponAnimationTime < 0.15)
            {
                if (_weaponVisual != null)
                {
                    float progress = (float)(_weaponAnimationTime / 0.15);
                    _weaponVisual.Rotation = Mathf.Lerp(0f, Mathf.Pi / 4, progress);
                }
            }
            else
            {
                if (_weaponVisual != null)
                {
                    _weaponVisual.Rotation = 0f;
                }
                _isWeaponAnimating = false;
            }
        }

        UpdateVisualState(moveInput);
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

    private void OnActiveInventorySlotChanged(int slotIndex)
    {
        UpdateActiveWeapon();
    }

    private void PlayWeaponAnimation()
    {
        _isWeaponAnimating = true;
        _weaponAnimationTime = 0.0;
        UpdateVisualState(Velocity.X);
    }

    private void UpdateActiveWeapon()
    {
        if (_gameManager == null || _weaponController == null)
        {
            return;
        }

        var item = _gameManager.GetActiveInventoryItem();
        var weapon = item?.Id switch
        {
            "base_item" => new Weapon("base_item", "Base Attack", 10, 0.3f),
            _ => null,
        };

        _hasActiveWeapon = weapon != null;
        _weaponController.SetWeapon(weapon);
        UpdateVisualState(Velocity.X);
    }

    public void SetSpawnPosition(Vector2 spawnPosition)
    {
        GlobalPosition = spawnPosition;
    }

    private void UpdateVisualState(float moveInput)
    {
        if (_animatedSprite == null)
        {
            return;
        }

        string animationName;
        if (_isWeaponAnimating)
        {
            animationName = _hasActiveWeapon ? AttackArmedAnimation : AttackAnimation;
        }
        else if (Mathf.IsZeroApprox(moveInput))
        {
            animationName = _hasActiveWeapon ? IdleArmedAnimation : IdleAnimation;
        }
        else
        {
            animationName = _hasActiveWeapon ? WalkArmedAnimation : WalkAnimation;
        }

        if (_animatedSprite.Animation != animationName)
        {
            _animatedSprite.Play(animationName);
        }

        _animatedSprite.FlipH = !_isFacingRight;

        if (_weaponVisual != null)
        {
            _weaponVisual.Visible = false;
            _weaponVisual.FlipH = !_isFacingRight;
        }
    }
}
