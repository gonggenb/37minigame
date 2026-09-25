using System;
using System.Linq;
using UnityEngine;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.Battle
{
    public enum DamageSource { Basic, SwordQi, Combo, Poison, Retaliation }

    [Serializable]
    public sealed class CombatReview
    {
        public float[] damage = new float[5];
        public float shieldAbsorbed, lifeStealHealing;
        public string finalEnemy = string.Empty;
        public float finalEnemyHealthRatio;
        public bool hasFinalEnemy;

        public void Record(DamageSource source, float resolvedDamage) => damage[(int)source] += Mathf.Max(0, resolvedDamage);
        public string DamageSummary
        {
            get
            {
                float total = damage.Sum();
                if (total <= 0) return "本局尚未造成伤害";
                string[] names = { "普攻", "剑气", "连环剑", "毒伤", "反震" };
                return string.Join(" · ", Enumerable.Range(0, damage.Length).Where(i => damage[i] > 0)
                    .OrderByDescending(i => damage[i]).Take(3).Select(i => $"{names[i]} {damage[i] / total:P0}"));
            }
        }
        public string DefenseSummary => $"护盾抵消 {CombatNumberDisplay.Format(shieldAbsorbed)} · 吸血恢复 {CombatNumberDisplay.Format(lifeStealHealing)}";
        public string EnemySummary => hasFinalEnemy ? $"{finalEnemy} · 剩余气血 {finalEnemyHealthRatio:P0}" : "本局未进入战斗";
    }
}
