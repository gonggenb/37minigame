using UnityEngine;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.Battle
{
    public partial class BattleManager
    {
        public CombatReview RunReview { get; private set; } = new CombatReview();
        public float EnemyOpeningArmor { get; private set; }
        public int PlayerVenomTicks { get; private set; }
        public int VenomBiteSequence { get; private set; }
        public int VenomTickSequence { get; private set; }
        public int OpeningArmorBreakSequence { get; private set; }
        private float playerVenomCooldown;
        public EnemyTrait CurrentEnemyTrait => !IsBossEncounter && currentEnemy != null ? currentEnemy.enemyTrait : EnemyTrait.None;
        public string EnemyTraitStatus => CurrentEnemyTrait switch
        {
            EnemyTrait.HeavyOpening => EnemyAttackAttempts == 0 ? "重撞·首击增强40%" : "重撞已使出",
            EnemyTrait.VenomBite => PlayerVenomTicks > 0 ? $"少侠中毒·剩余{PlayerVenomTicks}次" : "毒咬·命中附带余毒",
            EnemyTrait.OpeningArmor => EnemyOpeningArmor > 0 ? $"机关护甲 {CombatNumberDisplay.Format(EnemyOpeningArmor)}" : "机关护甲已破",
            _ => string.Empty
        };

        public void ResetRunReview() => RunReview = new CombatReview();
        public void CaptureFinalEnemy()
        {
            if (currentEnemy == null) return;
            RunReview.hasFinalEnemy = true;
            RunReview.finalEnemy = currentEnemy.displayName;
            RunReview.finalEnemyHealthRatio = currentEnemy.HealthRatio;
        }
        private void ResetEnemyTraits()
        {
            EnemyOpeningArmor = 0;
            PlayerVenomTicks = 0;
            VenomBiteSequence = VenomTickSequence = OpeningArmorBreakSequence = 0;
            playerVenomCooldown = 1;
        }
        private void BeginEnemyTraits()
        {
            ResetEnemyTraits();
            if (CurrentEnemyTrait == EnemyTrait.OpeningArmor)
                EnemyOpeningArmor = currentEnemy.maxHealth * EnemyTraits.ArmorHealthRatio;
        }
        private void ApplyEnemyVenom()
        {
            if (CurrentEnemyTrait != EnemyTrait.VenomBite || playerStats.runtimeStats.IsDead || currentEnemy.IsDead) return;
            if (PlayerVenomTicks == 0) playerVenomCooldown = 1;
            PlayerVenomTicks = EnemyTraits.VenomTicks;
            VenomBiteSequence++;
        }
        private void TickEnemyVenom(float delta)
        {
            if (delta <= 0 || PlayerVenomTicks <= 0 || currentEnemy == null || currentEnemy.IsDead || playerStats.runtimeStats.IsDead) return;
            playerVenomCooldown -= delta;
            if (playerVenomCooldown > 0) return;
            playerVenomCooldown += 1;
            PlayerVenomTicks--;
            VenomTickSequence++;
            var player = playerStats.runtimeStats;
            float damage = Mathf.Max(1, currentEnemy.attack * EnemyTraits.VenomAttackRatio - EffectivePlayerDefense * .25f);
            damage = AbsorbWithShield(damage * GetPlayerIncomingDamageMultiplier(player));
            player.TakeDamage(damage);
            battleLog = $"毒咬余毒：少侠受到 {CombatNumberDisplay.Format(damage)} 伤害";
        }
    }
}
