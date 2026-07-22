using System;

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

        public string BonusSummary
        {
            get
            {
                string summary = string.Empty;
                AppendBonus(ref summary, "攻击", attackBonus, false);
                AppendBonus(ref summary, "防御", defenseBonus, false);
                AppendBonus(ref summary, "气血", maxHealthBonus, false);
                AppendBonus(ref summary, "攻速", attackSpeedBonus, true);
                AppendBonus(ref summary, "暴击", critChanceBonus * 100f, true, "%");
                AppendBonus(ref summary, "闪避", dodgeChanceBonus * 100f, true, "%");
                return string.IsNullOrEmpty(summary) ? "无属性加成" : summary;
            }
        }

        public EquipmentItem Clone()
        {
            return (EquipmentItem)MemberwiseClone();
        }

        private static void AppendBonus(ref string summary, string label, float value, bool decimalValue, string suffix = "")
        {
            if (Math.Abs(value) < 0.001f)
            {
                return;
            }

            if (!string.IsNullOrEmpty(summary))
            {
                summary += "  ";
            }

            summary += decimalValue
                ? $"{label} +{value:0.#}{suffix}"
                : $"{label} +{value:0}{suffix}";
        }
    }
}
