using System;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.Player
{
    public enum EquipmentSlot
    {
        Weapon,
        Armor,
        Accessory
    }

    public enum EquipmentRarity
    {
        Common,
        Fine,
        Rare
    }

    [Serializable]
    public class EquipmentItem
    {
        public string id;
        public string displayName;
        public EquipmentSlot slot;
        public EquipmentRarity rarity;
        public float attackBonus;
        public float defenseBonus;
        public float maxHealthBonus;
        public float attackSpeedBonus;
        public float critChanceBonus;
        public float dodgeChanceBonus;
        public int swordQiInterval;
        public float swordQiDamageRatio;
        public float openingShield;
        public float armorBreakPerHit;
        public int poisonStacksPerHit;
        public float criticalCooldownMultiplier = 1f;
        public float dodgeHealRatio;
        public string effectSummary;

        public string BonusSummary
        {
            get
            {
                string summary = string.Empty;
                AppendBonus(ref summary, "攻击", attackBonus, false, true);
                AppendBonus(ref summary, "防御", defenseBonus, false, true);
                AppendBonus(ref summary, "气血", maxHealthBonus, false, true);
                AppendBonus(ref summary, "攻速", attackSpeedBonus, true, false);
                AppendBonus(ref summary, "暴击", critChanceBonus * 100f, true, false, "%");
                AppendBonus(ref summary, "闪避", dodgeChanceBonus * 100f, true, false, "%");
                if (!string.IsNullOrEmpty(effectSummary))
                {
                    if (!string.IsNullOrEmpty(summary))
                    {
                        summary += "  ·  ";
                    }

                    summary += effectSummary;
                }

                return string.IsNullOrEmpty(summary) ? "无属性加成" : summary;
            }
        }

        public EquipmentItem Clone()
        {
            return (EquipmentItem)MemberwiseClone();
        }

        private static void AppendBonus(ref string summary, string label, float value,
            bool decimalValue, bool combatValue, string suffix = "")
        {
            if (Math.Abs(value) < 0.001f)
            {
                return;
            }

            if (!string.IsNullOrEmpty(summary))
            {
                summary += "  ";
            }

            string displayedValue = combatValue
                ? CombatNumberDisplay.Format(value)
                : decimalValue ? value.ToString("0.#") : value.ToString("0");
            summary += $"{label} +{displayedValue}{suffix}";
        }
    }
}
