using System;
using System.Collections.Generic;
using WuxiaRoguelite.Domain.Characters;
using WuxiaRoguelite.Domain.Combat;

namespace WuxiaRoguelite.Application.Combat
{
    public sealed class BattleService
    {
        private readonly CharacterRuntime player;
        private readonly CharacterRuntime enemy;
        private readonly IRandomSource randomSource;
        private readonly IReadOnlyList<CombatEffectDefinition> playerEffects;
        private readonly CombatEffectRegistry effectRegistry;
        private float playerCooldown;
        private float enemyCooldown;
        private float poisonCooldown = 1f;
        private int successfulPlayerHits;

        public BattleService(
            CharacterRuntime player,
            CharacterRuntime enemy,
            IRandomSource randomSource,
            IReadOnlyList<CombatEffectDefinition> playerEffects = null,
            CombatEffectRegistry effectRegistry = null)
        {
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            this.randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            this.playerEffects = playerEffects ?? Array.Empty<CombatEffectDefinition>();
            this.effectRegistry = effectRegistry ?? new CombatEffectRegistry();
            playerCooldown = AttackInterval(player);
            enemyCooldown = AttackInterval(enemy);
            PlayerShield = this.effectRegistry.CalculateOpeningShield(this.playerEffects, player);
        }

        public CharacterRuntime Player => player;
        public CharacterRuntime Enemy => enemy;
        public float Elapsed { get; private set; }
        public float PlayerShield { get; private set; }
        public float EnemyArmorBreak { get; private set; }
        public int EnemyPoisonStacks { get; private set; }
        public bool IsFinished => player.IsDead || enemy.IsDead;
        public bool PlayerWon => enemy.IsDead && !player.IsDead;

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || IsFinished)
            {
                return;
            }

            Elapsed += deltaTime;
            playerCooldown -= deltaTime;
            enemyCooldown -= deltaTime;
            poisonCooldown -= deltaTime;

            while (playerCooldown <= 0f && !IsFinished)
            {
                ResolveAttack(BattleSide.Player);
                playerCooldown += AttackInterval(player);
            }

            while (poisonCooldown <= 0f && !IsFinished)
            {
                ApplyPoisonTick();
                poisonCooldown += 1f;
            }

            while (enemyCooldown <= 0f && !IsFinished)
            {
                ResolveAttack(BattleSide.Enemy);
                enemyCooldown += AttackInterval(enemy);
            }
        }

        public BattleAttackResult ResolveAttack(BattleSide attackerSide)
        {
            if (IsFinished)
            {
                return new BattleAttackResult(attackerSide, false, false, 0f, 0f, 0f);
            }

            CharacterRuntime attacker = attackerSide == BattleSide.Player ? player : enemy;
            CharacterRuntime defender = attackerSide == BattleSide.Player ? enemy : player;
            if (randomSource.Next01() < defender.Stats.DodgeChance)
            {
                return new BattleAttackResult(attackerSide, true, false, 0f, 0f, 0f);
            }

            float defense = defender.Stats.Defense;
            if (attackerSide == BattleSide.Player)
            {
                defense = Math.Max(0f, defense - EnemyArmorBreak);
            }

            float damage = Math.Max(1f, attacker.Stats.Attack * (1f + attacker.Stats.DamageBonus) - defense);
            bool critical = randomSource.Next01() < attacker.Stats.CritChance;
            if (critical)
            {
                damage *= attacker.Stats.CritMultiplier;
            }

            float shieldAbsorbed = 0f;
            if (attackerSide == BattleSide.Enemy && PlayerShield > 0f)
            {
                shieldAbsorbed = Math.Min(PlayerShield, damage);
                PlayerShield -= shieldAbsorbed;
                damage -= shieldAbsorbed;
            }

            float healthBefore = defender.CurrentHealth;
            defender.TakeDamage(damage);
            float damageToHealth = healthBefore - defender.CurrentHealth;
            float bonusDamage = 0f;

            if (attackerSide == BattleSide.Player && damageToHealth > 0f)
            {
                successfulPlayerHits += 1;
                ApplyPlayerOnHitEffects();
                bonusDamage = effectRegistry.SwordQiDamage(
                    playerEffects,
                    successfulPlayerHits,
                    player.Stats.Attack);
                if (bonusDamage > 0f)
                {
                    float enemyHealthBefore = enemy.CurrentHealth;
                    enemy.TakeDamage(bonusDamage);
                    bonusDamage = enemyHealthBefore - enemy.CurrentHealth;
                }

                float lifeSteal = Math.Min(
                    1f,
                    player.Stats.LifeSteal + effectRegistry.AdditionalLifeSteal(playerEffects));
                player.Heal((damageToHealth + bonusDamage) * lifeSteal);
            }
            else if (attackerSide == BattleSide.Enemy && damageToHealth > 0f)
            {
                float retaliation = effectRegistry.RetaliationDamage(playerEffects, player.Stats.Defense);
                if (retaliation > 0f)
                {
                    float enemyHealthBefore = enemy.CurrentHealth;
                    enemy.TakeDamage(retaliation);
                    bonusDamage = enemyHealthBefore - enemy.CurrentHealth;
                }
            }

            return new BattleAttackResult(
                attackerSide,
                false,
                critical,
                damageToHealth,
                shieldAbsorbed,
                bonusDamage);
        }

        private void ApplyPlayerOnHitEffects()
        {
            float armorBreak = effectRegistry.ArmorBreakPerHit(playerEffects);
            EnemyArmorBreak = Math.Min(enemy.Stats.Defense, EnemyArmorBreak + armorBreak);

            int poison = effectRegistry.PoisonStacksPerHit(playerEffects);
            int limit = effectRegistry.PoisonStackLimit(playerEffects);
            EnemyPoisonStacks = Math.Min(limit, EnemyPoisonStacks + poison);
        }

        private void ApplyPoisonTick()
        {
            if (EnemyPoisonStacks <= 0 || enemy.IsDead)
            {
                return;
            }

            float damage = EnemyPoisonStacks * effectRegistry.PoisonDamagePerStack(playerEffects);
            enemy.TakeDamage(damage);
        }

        private static float AttackInterval(CharacterRuntime character)
        {
            return 1f / Math.Max(0.1f, character.Stats.AttackSpeed);
        }
    }
}
