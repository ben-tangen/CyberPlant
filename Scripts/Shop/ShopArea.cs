#nullable enable
using Godot;
using CyberPlant.UI;

namespace CyberPlant.Shop;

public partial class ShopArea : Area2D
{
    [Export] public NodePath ShopUiPath { get; set; } = "";
    [Export] public NodePath PromptLabelPath { get; set; } = "";

    private ShopUI? _shopUi;
    private CanvasItem? _promptLabel;
    private bool _playerNearby;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        if (!ShopUiPath.IsEmpty)
        {
            _shopUi = GetNodeOrNull<ShopUI>(ShopUiPath);
        }

        if (!PromptLabelPath.IsEmpty)
        {
            _promptLabel = GetNodeOrNull<CanvasItem>(PromptLabelPath);
            if (_promptLabel != null)
            {
                _promptLabel.Visible = false;
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_playerNearby && Input.IsActionJustPressed("interact"))
        {
            _shopUi?.OpenShop();
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            _playerNearby = true;
            if (_promptLabel != null)
            {
                _promptLabel.Visible = true;
            }
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            _playerNearby = false;
            if (_promptLabel != null)
            {
                _promptLabel.Visible = false;
            }
            _shopUi?.CloseShop();
        }
    }
}
