#nullable enable
using Godot;
using CyberPlant.Core;
using System.Collections.Generic;

namespace CyberPlant.Combat;

public sealed class WeaponDefinition
{
    public WeaponDefinition(
        string id,
        string displayName,
        int damage,
        float attackCooldown,
        float hitRadius,
        float hitOffsetX,
        int requiredLevel,
        int shopCost,
        string iconPath,
        bool purchasable)
    {
        Id = id;
        DisplayName = displayName;
        Damage = damage;
        AttackCooldown = attackCooldown;
        HitRadius = hitRadius;
        HitOffsetX = hitOffsetX;
        RequiredLevel = requiredLevel;
        ShopCost = shopCost;
        IconPath = iconPath;
        Purchasable = purchasable;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public int Damage { get; }
    public float AttackCooldown { get; }
    public float HitRadius { get; }
    public float HitOffsetX { get; }
    public int RequiredLevel { get; }
    public int ShopCost { get; }
    public string IconPath { get; }
    public bool Purchasable { get; }
}

public static class WeaponCatalog
{
    private static readonly Dictionary<string, WeaponDefinition> Definitions = new()
    {
        ["base_item"] = new WeaponDefinition(
            id: "base_item",
            displayName: "Starter Sprout",
            damage: 10,
            attackCooldown: 0.30f,
            hitRadius: 11.0f,
            hitOffsetX: 24.0f,
            requiredLevel: 1,
            shopCost: 0,
            iconPath: "res://assets/icon.svg",
            purchasable: false),
        ["thorn_blade"] = new WeaponDefinition(
            id: "thorn_blade",
            displayName: "Thorn Blade",
            damage: 16,
            attackCooldown: 0.38f,
            hitRadius: 13.0f,
            hitOffsetX: 28.0f,
            requiredLevel: 1,
            shopCost: 35,
            iconPath: "res://assets/player/plant_gun.png",
            purchasable: true),
        ["vine_whip"] = new WeaponDefinition(
            id: "vine_whip",
            displayName: "Vine Whip",
            damage: 8,
            attackCooldown: 0.20f,
            hitRadius: 18.0f,
            hitOffsetX: 36.0f,
            requiredLevel: 2,
            shopCost: 45,
            iconPath: "res://assets/player/plant_projectile.png",
            purchasable: true),
        ["spore_burst"] = new WeaponDefinition(
            id: "spore_burst",
            displayName: "Spore Burst",
            damage: 24,
            attackCooldown: 0.62f,
            hitRadius: 16.0f,
            hitOffsetX: 30.0f,
            requiredLevel: 3,
            shopCost: 60,
            iconPath: "res://assets/player/plant_gun.png",
            purchasable: true),
    };

    private static readonly string[] ShopWeaponIds =
    {
        "thorn_blade",
        "vine_whip",
        "spore_burst",
    };

    public static Weapon? GetWeaponForItem(string? itemId)
    {
        if (itemId == null || !Definitions.TryGetValue(itemId, out var definition))
        {
            return null;
        }

        return new Weapon(
            definition.Id,
            definition.DisplayName,
            definition.Damage,
            definition.AttackCooldown,
            definition.HitRadius,
            definition.HitOffsetX);
    }

    public static InventoryItem? CreateInventoryItem(string? itemId)
    {
        if (itemId == null || !Definitions.TryGetValue(itemId, out var definition))
        {
            return null;
        }

        Texture2D? icon = GD.Load<Texture2D>(definition.IconPath);
        return new InventoryItem(definition.Id, definition.DisplayName, icon);
    }

    public static WeaponDefinition? GetDefinition(string itemId)
    {
        return Definitions.TryGetValue(itemId, out var definition) ? definition : null;
    }

    public static IReadOnlyList<WeaponDefinition> GetShopWeapons()
    {
        var result = new List<WeaponDefinition>(ShopWeaponIds.Length);
        foreach (string id in ShopWeaponIds)
        {
            if (Definitions.TryGetValue(id, out var definition))
            {
                result.Add(definition);
            }
        }

        return result;
    }
}
