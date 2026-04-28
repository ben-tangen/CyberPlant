#nullable enable
using Godot;
using System.Collections.Generic;

namespace CyberPlant.Levels;

/// <summary>
/// Spawns enemies in waves. When all enemies in a wave are defeated, opens access or spawns next wave.
/// </summary>
public partial class EnemySpawner : Area2D
{
    [Signal]
    public delegate void AllEnemiesDefeatedEventHandler();

    [Export] public PackedScene? EnemyScene { get; set; }
    [Export] public int EnemiesPerWave { get; set; } = 3;
    [Export] public int WaveCount { get; set; } = 2;
    [Export] public Vector2 SpawnOffset { get; set; } = Vector2.Zero;
    [Export] public float SpawnIntervalSeconds { get; set; } = 1.5f;
    [Export] public NodePath? GateNodePath { get; set; } // Optional gate to open when defeated
    [Export] public NodePath? PromptLabelPath { get; set; } // Optional prompt label to show/hide

    private List<Node> _activeEnemies = new();
    private int _currentWave = 0;
    private bool _allWavesSpawned = false;
    private float _spawnTimer = 0.0f;
    private int _enemiesToSpawnThisWave;
    private Node? _gateNode;
    private CanvasItem? _promptLabel;

    public override void _Ready()
    {
        AreaEntered += OnAreaEntered;

        if (!GateNodePath.IsEmpty)
        {
            _gateNode = GetNode(GateNodePath);
            if (_gateNode is CanvasItem gateCanvas)
            {
                gateCanvas.Visible = true;
            }
        }

        if (!PromptLabelPath.IsEmpty)
        {
            _promptLabel = GetNode<CanvasItem>(PromptLabelPath);
            if (_promptLabel != null)
            {
                _promptLabel.Visible = false;
            }
        }

        AddToGroup("enemy_spawner");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_currentWave >= WaveCount)
        {
            return;
        }

        // Clean up dead enemies
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            if (!IsInstanceValid(_activeEnemies[i]))
            {
                _activeEnemies.RemoveAt(i);
            }
        }

        // If all enemies in wave are defeated, move to next wave
        if (_activeEnemies.Count == 0 && _allWavesSpawned)
        {
            OnAllEnemiesDefeated();
            return;
        }

        // Spawn enemies incrementally
        _spawnTimer -= (float)delta;
        while (_enemiesToSpawnThisWave > 0 && _spawnTimer <= 0.0f)
        {
            SpawnEnemy();
            _enemiesToSpawnThisWave--;
            _spawnTimer += SpawnIntervalSeconds;
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        // Trigger spawning when player enters
        if (area.IsInGroup("player"))
        {
            if (_currentWave == 0)
            {
                StartNextWave();
            }
        }
    }

    private void StartNextWave()
    {
        if (_currentWave >= WaveCount)
        {
            return;
        }

        _currentWave++;
        _enemiesToSpawnThisWave = EnemiesPerWave;
        _spawnTimer = 0.0f;

        GD.Print($"EnemySpawner: Starting Wave {_currentWave}/{WaveCount}");

        if (_currentWave >= WaveCount)
        {
            _allWavesSpawned = true;
        }
    }

    private void SpawnEnemy()
    {
        if (EnemyScene == null)
        {
            return;
        }

        var enemy = EnemyScene.Instantiate<Node>();
        Vector2 spawnPosition = GlobalPosition + SpawnOffset;
        if (enemy is Node2D enemy2d)
        {
            enemy2d.GlobalPosition = spawnPosition;
        }

        AddChild(enemy);
        _activeEnemies.Add(enemy);
        GD.Print($"EnemySpawner: Spawned enemy at {spawnPosition}");
    }

    private void OnAllEnemiesDefeated()
    {
        GD.Print("EnemySpawner: All enemies defeated!");
        
        // Open gate if specified
        if (_gateNode is CanvasItem gateCanvas)
        {
            gateCanvas.Visible = false;
        }

        // Show prompt if specified
        if (_promptLabel != null)
        {
            _promptLabel.Visible = true;
        }

        EmitSignal(SignalName.AllEnemiesDefeated);
        
        // Disable spawner after all waves complete
        AreaEntered -= OnAreaEntered;
        SetPhysicsProcess(false);
    }
}
