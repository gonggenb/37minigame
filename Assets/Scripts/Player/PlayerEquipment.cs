using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.Player
{
    [DisallowMultipleComponent]
    public class PlayerEquipment : MonoBehaviour
    {
        public PlayerStats playerStats;
        public readonly List<EquipmentItem> inventory = new List<EquipmentItem>();
        public EquipmentItem equippedWeapon;
        public EquipmentItem equippedArmor;
        public EquipmentItem equippedAccessory;

        private readonly List<EquipmentItem> treasurePool = new List<EquipmentItem>();

        private void Awake()
        {
            playerStats = playerStats == null ? GetComponent<PlayerStats>() : playerStats;
            BuildTreasurePool();
        }

        public void ResetRun(PlayerStats owner)
        {
            playerStats = owner;
            inventory.Clear();
            equippedWeapon = null;
            equippedArmor = null;
            equippedAccessory = null;
            BuildTreasurePool();

            AddItem(Item("qinggang_sword", "青钢剑", EquipmentSlot.Weapon, EquipmentRarity.Common, attack: 4f));
            AddItem(Item("light_scale", "轻鳞衣", EquipmentSlot.Armor, EquipmentRarity.Fine, defense: 2f, health: 18f));
            AddItem(Item("practice_bracer", "练功护腕", EquipmentSlot.Accessory, EquipmentRarity.Common, speed: 0.08f));
        }

        public void Equip(EquipmentItem item)
        {
            if (item == null || playerStats == null || playerStats.runtimeStats == null || IsEquipped(item))
            {
                return;
            }

            EquipmentItem current = GetEquipped(item.slot);
            ApplyBonuses(current, -1f);
            SetEquipped(item.slot, item);
            ApplyBonuses(item, 1f);
        }

        public void Unequip(EquipmentSlot slot)
        {
            EquipmentItem current = GetEquipped(slot);
            if (current == null)
            {
                return;
            }

            ApplyBonuses(current, -1f);
            SetEquipped(slot, null);
        }

        public EquipmentItem GetEquipped(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    return equippedWeapon;
                case EquipmentSlot.Armor:
                    return equippedArmor;
                default:
                    return equippedAccessory;
            }
        }

        public bool IsEquipped(EquipmentItem item)
        {
            return item != null && GetEquipped(item.slot) == item;
        }

        public string AddTreasureItem()
        {
            foreach (EquipmentItem template in treasurePool)
            {
                if (!inventory.Exists(item => item.id == template.id))
                {
                    EquipmentItem reward = template.Clone();
                    AddItem(reward);
                    return reward.displayName;
                }
            }

            return string.Empty;
        }

        private void AddItem(EquipmentItem item)
        {
            inventory.Add(item);
        }

        private void ApplyBonuses(EquipmentItem item, float direction)
        {
            if (item == null)
            {
                return;
            }

            CombatantStats stats = playerStats.runtimeStats;
            float healthDelta = item.maxHealthBonus * direction;
            stats.attack = Mathf.Max(1f, stats.attack + item.attackBonus * direction);
            stats.defense = Mathf.Max(0f, stats.defense + item.defenseBonus * direction);
            stats.attackSpeed = Mathf.Max(0.1f, stats.attackSpeed + item.attackSpeedBonus * direction);
            stats.critChance = Mathf.Clamp01(stats.critChance + item.critChanceBonus * direction);
            stats.dodgeChance = Mathf.Clamp01(stats.dodgeChance + item.dodgeChanceBonus * direction);
            stats.maxHealth = Mathf.Max(1f, stats.maxHealth + healthDelta);
            if (healthDelta > 0f)
            {
                stats.Heal(healthDelta);
            }
            else
            {
                stats.currentHealth = Mathf.Min(stats.currentHealth, stats.maxHealth);
            }
        }

        private void SetEquipped(EquipmentSlot slot, EquipmentItem item)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    equippedWeapon = item;
                    break;
                case EquipmentSlot.Armor:
                    equippedArmor = item;
                    break;
                case EquipmentSlot.Accessory:
                    equippedAccessory = item;
                    break;
            }
        }

        private void BuildTreasurePool()
        {
            if (treasurePool.Count > 0)
            {
                return;
            }

            treasurePool.Add(Item("black_iron_ring", "玄铁戒", EquipmentSlot.Accessory, EquipmentRarity.Rare, attack: 2f, crit: 0.04f));
            treasurePool.Add(Item("wanderer_cloak", "游侠披风", EquipmentSlot.Armor, EquipmentRarity.Rare, health: 12f, dodge: 0.04f));
        }

        private static EquipmentItem Item(
            string id,
            string displayName,
            EquipmentSlot slot,
            EquipmentRarity rarity,
            float attack = 0f,
            float defense = 0f,
            float health = 0f,
            float speed = 0f,
            float crit = 0f,
            float dodge = 0f)
        {
            return new EquipmentItem
            {
                id = id,
                displayName = displayName,
                slot = slot,
                rarity = rarity,
                attackBonus = attack,
                defenseBonus = defense,
                maxHealthBonus = health,
                attackSpeedBonus = speed,
                critChanceBonus = crit,
                dodgeChanceBonus = dodge
            };
        }
    }
}
