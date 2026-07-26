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

            AddItem(Item("qinggang_sword", "青钢剑", EquipmentSlot.Weapon,
                EquipmentRarity.Common, attack: 4f,
                swordQiInterval: 3, swordQiDamageRatio: 0.35f,
                effectSummary: "每 3 次命中追加 35% 攻击剑气"));
            AddItem(Item("light_scale", "轻鳞衣", EquipmentSlot.Armor,
                EquipmentRarity.Fine, defense: 2f, health: 18f,
                openingShield: 10f, effectSummary: "每场战斗获得 10 点护盾"));
            AddItem(Item("practice_bracer", "练功护腕", EquipmentSlot.Accessory,
                EquipmentRarity.Common, speed: 0.08f,
                criticalCooldownMultiplier: 0.70f,
                effectSummary: "暴击后下次攻击间隔缩短 30%"));
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

        public bool IsUpgrade(EquipmentItem candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            EquipmentItem current = GetEquipped(candidate.slot);
            if (current == null)
            {
                return true;
            }

            if (candidate.rarity != current.rarity)
            {
                return candidate.rarity > current.rarity;
            }

            return GetPowerScore(candidate) > GetPowerScore(current) + 0.001f;
        }

        public static float GetPowerScore(EquipmentItem item)
        {
            if (item == null)
            {
                return 0f;
            }

            float score = 0f;
            score += item.attackBonus * 10f;
            score += item.defenseBonus * 8f;
            score += item.maxHealthBonus * 0.5f;
            score += item.attackSpeedBonus * 100f;
            score += item.critChanceBonus * 120f;
            score += item.dodgeChanceBonus * 120f;
            score += item.openingShield * 0.5f;
            score += item.armorBreakPerHit * 15f;
            score += item.poisonStacksPerHit * 25f;
            score += item.dodgeHealRatio * 100f;

            if (item.swordQiInterval > 0)
            {
                score += item.swordQiDamageRatio / item.swordQiInterval * 100f;
            }

            if (item.criticalCooldownMultiplier > 0f && item.criticalCooldownMultiplier < 1f)
            {
                score += (1f - item.criticalCooldownMultiplier) * 40f;
            }

            return score;
        }

        public string AddTreasureItem()
        {
            List<EquipmentItem> available = new List<EquipmentItem>();
            foreach (EquipmentItem template in treasurePool)
            {
                if (!inventory.Exists(item => item.id == template.id))
                {
                    available.Add(template);
                }
            }

            if (available.Count == 0)
            {
                return string.Empty;
            }

            EquipmentItem reward = available[Random.Range(0, available.Count)].Clone();
            bool autoEquipped = AddItem(reward);
            return autoEquipped ? $"{reward.displayName}（已自动装备）" : reward.displayName;
        }

        public float GetOpeningShield()
        {
            return SumEquipped(item => item.openingShield);
        }

        public float GetArmorBreakPerHit()
        {
            return SumEquipped(item => item.armorBreakPerHit);
        }

        public int GetPoisonStacksPerHit()
        {
            return Mathf.RoundToInt(SumEquipped(item => item.poisonStacksPerHit));
        }

        public float GetSwordQiDamageRatio(int successfulHitCount)
        {
            float ratio = 0f;
            VisitEquipped(item =>
            {
                if (item.swordQiInterval > 0 && successfulHitCount % item.swordQiInterval == 0)
                {
                    ratio += item.swordQiDamageRatio;
                }
            });
            return ratio;
        }

        public float GetCriticalCooldownMultiplier()
        {
            float multiplier = 1f;
            VisitEquipped(item =>
            {
                if (item.criticalCooldownMultiplier > 0f)
                {
                    multiplier = Mathf.Min(multiplier, item.criticalCooldownMultiplier);
                }
            });
            return multiplier;
        }

        public float GetDodgeHealRatio()
        {
            return SumEquipped(item => item.dodgeHealRatio);
        }

        private bool AddItem(EquipmentItem item)
        {
            if (item == null)
            {
                return false;
            }

            inventory.Add(item);
            if (!IsUpgrade(item))
            {
                return false;
            }

            Equip(item);
            return IsEquipped(item);
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

            treasurePool.Add(Item("black_iron_ring", "玄铁戒", EquipmentSlot.Accessory,
                EquipmentRarity.Rare, attack: 2f, crit: 0.04f,
                armorBreakPerHit: 0.35f, effectSummary: "命中破甲 0.35"));
            treasurePool.Add(Item("wanderer_cloak", "游侠披风", EquipmentSlot.Armor,
                EquipmentRarity.Rare, health: 12f, dodge: 0.04f,
                dodgeHealRatio: 0.03f, effectSummary: "闪避时恢复 3% 气血"));
            treasurePool.Add(Item("poison_dart_pouch", "毒镖囊", EquipmentSlot.Accessory,
                EquipmentRarity.Rare, attack: 1f, poisonStacksPerHit: 1,
                effectSummary: "命中额外施加 1 层毒"));
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
            float dodge = 0f,
            int swordQiInterval = 0,
            float swordQiDamageRatio = 0f,
            float openingShield = 0f,
            float armorBreakPerHit = 0f,
            int poisonStacksPerHit = 0,
            float criticalCooldownMultiplier = 1f,
            float dodgeHealRatio = 0f,
            string effectSummary = "")
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
                dodgeChanceBonus = dodge,
                swordQiInterval = swordQiInterval,
                swordQiDamageRatio = swordQiDamageRatio,
                openingShield = openingShield,
                armorBreakPerHit = armorBreakPerHit,
                poisonStacksPerHit = poisonStacksPerHit,
                criticalCooldownMultiplier = criticalCooldownMultiplier,
                dodgeHealRatio = dodgeHealRatio,
                effectSummary = effectSummary
            };
        }

        private float SumEquipped(System.Func<EquipmentItem, float> selector)
        {
            float total = 0f;
            VisitEquipped(item => total += selector(item));
            return total;
        }

        private void VisitEquipped(System.Action<EquipmentItem> visitor)
        {
            if (equippedWeapon != null)
            {
                visitor(equippedWeapon);
            }

            if (equippedArmor != null)
            {
                visitor(equippedArmor);
            }

            if (equippedAccessory != null)
            {
                visitor(equippedAccessory);
            }
        }
    }
}
