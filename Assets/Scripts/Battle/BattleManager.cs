using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.Battle
{
    public class BattleManager : MonoBehaviour
    {
        public PlayerStats playerStats;
        public CombatantStats currentEnemy;
        public string battleLog = "尚未进入战斗";
        [Min(0.1f)] public float battleSpeedMultiplier = 1.5f;

        public bool IsBattleActive { get; private set; }
        public float BattleElapsed { get; private set; }
        public float BattleSpeedMultiplier => Mathf.Max(0.1f, battleSpeedMultiplier);
        public int AttackSequence { get; private set; }
        public bool LastAttackWasPlayer { get; private set; }
        public bool LastAttackWasCritical { get; private set; }
        public bool LastAttackWasDodged { get; private set; }
        public float LastDamage { get; private set; }
        public string LastTriggeredEffect { get; private set; } = string.Empty;
        public int PlayerSuccessfulHits { get; private set; }
        public int EnemyPoisonStacks { get; private set; }
        public float EnemyArmorBreak { get; private set; }
        public float PlayerShield { get; private set; }
        public float PlayerAttackCooldownRemaining { get; private set; }
        public float PlayerAttackCooldownDuration { get; private set; }
        public float EnemyAttackCooldownRemaining { get; private set; }
        public float EnemyAttackCooldownDuration { get; private set; }

        private Coroutine battleRoutine;
        private float poisonTickCooldown;
        private readonly Dictionary<string, float> martialArtLastActivationTimes =
            new Dictionary<string, float>();

        public float PlayerAttackCooldownRatio => PlayerAttackCooldownDuration <= 0f
            ? 0f
            : Mathf.Clamp01(PlayerAttackCooldownRemaining / PlayerAttackCooldownDuration);

        public float EnemyAttackCooldownRatio => EnemyAttackCooldownDuration <= 0f
            ? 0f
            : Mathf.Clamp01(EnemyAttackCooldownRemaining / EnemyAttackCooldownDuration);

        public float GetMartialArtLastActivationTime(string artId)
        {
            return !string.IsNullOrEmpty(artId) &&
                   martialArtLastActivationTimes.TryGetValue(artId, out float activatedAt)
                ? activatedAt
                : -100f;
        }

        public void BeginBattle(CombatantStats enemy, Action<bool> onComplete)
        {
            CancelBattle();
            currentEnemy = enemy;
            IsBattleActive = true;
            BattleElapsed = 0f;
            AttackSequence = 0;
            LastDamage = 0f;
            LastAttackWasCritical = false;
            LastAttackWasDodged = false;
            LastTriggeredEffect = string.Empty;
            PlayerSuccessfulHits = 0;
            EnemyPoisonStacks = 0;
            EnemyArmorBreak = 0f;
            poisonTickCooldown = 1f;
            martialArtLastActivationTimes.Clear();
            PlayerShield = CalculateOpeningShield();
            if (playerStats.GetMartialArtRank("金钟罩") > 0)
            {
                RegisterMartialArtActivation("金钟罩");
            }
            battleLog = PlayerShield > 0f
                ? $"遭遇 {currentEnemy.displayName}，护盾 +{PlayerShield:0}"
                : $"遭遇 {currentEnemy.displayName}";
            battleRoutine = StartCoroutine(RunBattle(onComplete));
        }

        public void CancelBattle()
        {
            if (battleRoutine != null)
            {
                StopCoroutine(battleRoutine);
                battleRoutine = null;
            }

            IsBattleActive = false;
            currentEnemy = null;
            PlayerShield = 0f;
            EnemyPoisonStacks = 0;
            EnemyArmorBreak = 0f;
            PlayerAttackCooldownRemaining = 0f;
            PlayerAttackCooldownDuration = 0f;
            EnemyAttackCooldownRemaining = 0f;
            EnemyAttackCooldownDuration = 0f;
            martialArtLastActivationTimes.Clear();
        }

        private IEnumerator RunBattle(Action<bool> onComplete)
        {
            PlayerAttackCooldownRemaining = 0.2f;
            PlayerAttackCooldownDuration = 0.2f;
            EnemyAttackCooldownRemaining = 0.7f;
            EnemyAttackCooldownDuration = 0.7f;

            while (playerStats.runtimeStats != null && !playerStats.runtimeStats.IsDead &&
                   currentEnemy != null && !currentEnemy.IsDead)
            {
                float combatDeltaTime = Time.deltaTime * BattleSpeedMultiplier;
                BattleElapsed += Time.deltaTime;
                PlayerAttackCooldownRemaining -= combatDeltaTime;
                EnemyAttackCooldownRemaining -= combatDeltaTime;
                poisonTickCooldown -= combatDeltaTime;

                if (PlayerAttackCooldownRemaining <= 0f)
                {
                    float cooldownMultiplier = DoAttack(playerStats.runtimeStats, currentEnemy);
                    PlayerAttackCooldownDuration =
                        1f / Mathf.Max(0.1f, playerStats.runtimeStats.attackSpeed) * cooldownMultiplier;
                    PlayerAttackCooldownRemaining = PlayerAttackCooldownDuration;
                }

                if (currentEnemy != null && !currentEnemy.IsDead && poisonTickCooldown <= 0f)
                {
                    ApplyPoisonTick();
                    poisonTickCooldown += 1f;
                }

                if (currentEnemy != null && !currentEnemy.IsDead && EnemyAttackCooldownRemaining <= 0f)
                {
                    DoAttack(currentEnemy, playerStats.runtimeStats);
                    EnemyAttackCooldownDuration = 1f / Mathf.Max(0.1f, currentEnemy.attackSpeed);
                    EnemyAttackCooldownRemaining = EnemyAttackCooldownDuration;
                }

                yield return null;
            }

            bool playerWon = playerStats.runtimeStats != null && !playerStats.runtimeStats.IsDead;
            string enemyName = currentEnemy != null ? currentEnemy.displayName : "强敌";
            battleLog = playerWon ? $"击败 {enemyName}" : "少侠气血耗尽";
            yield return new WaitForSeconds(0.55f / BattleSpeedMultiplier);
            IsBattleActive = false;
            battleRoutine = null;
            onComplete?.Invoke(playerWon);
        }

        private float DoAttack(CombatantStats attacker, CombatantStats defender)
        {
            bool isPlayerAttack = ReferenceEquals(attacker, playerStats.runtimeStats);
            LastAttackWasPlayer = isPlayerAttack;
            LastAttackWasCritical = false;
            LastAttackWasDodged = false;
            LastDamage = 0f;
            LastTriggeredEffect = string.Empty;
            AttackSequence += 1;

            if (UnityEngine.Random.value < defender.dodgeChance)
            {
                LastAttackWasDodged = true;
                if (!isPlayerAttack)
                {
                    HealPlayerOnDodge();
                }

                string dodgeText = $"{defender.displayName} 闪开了 {attacker.displayName} 的攻击";
                battleLog = string.IsNullOrEmpty(LastTriggeredEffect)
                    ? dodgeText
                    : $"{dodgeText}（{LastTriggeredEffect}）";
                return 1f;
            }

            float effectiveDefense = defender.defense;
            if (isPlayerAttack && ReferenceEquals(defender, currentEnemy))
            {
                effectiveDefense = Mathf.Max(0f, effectiveDefense - EnemyArmorBreak);
            }

            float damage = Mathf.Max(1f, attacker.attack - effectiveDefense);
            bool isCrit = UnityEngine.Random.value < attacker.critChance;
            if (isCrit)
            {
                damage *= Mathf.Max(1f, attacker.critMultiplier);
            }

            float shieldBefore = PlayerShield;
            if (!isPlayerAttack)
            {
                damage = AbsorbWithShield(damage);
            }

            defender.TakeDamage(damage);
            float totalDamage = damage;
            string[] effects = new string[3];
            int effectCount = 0;
            float shieldAbsorbed = shieldBefore - PlayerShield;
            if (shieldAbsorbed > 0f)
            {
                effects[effectCount++] = $"护盾抵消 {shieldAbsorbed:0}";
            }

            if (isPlayerAttack)
            {
                PlayerSuccessfulHits += 1;
                ApplyArmorBreak(ref effects, ref effectCount);
                ApplyPoison(ref effects, ref effectCount);
                float swordQiDamage = ApplySwordQi(attacker, defender);
                if (swordQiDamage > 0f)
                {
                    totalDamage += swordQiDamage;
                    effects[effectCount++] = $"剑气 {swordQiDamage:0}";
                }
            }
            else if (damage > 0f)
            {
                float reflectedDamage = ApplyRetaliation(attacker);
                if (reflectedDamage > 0f)
                {
                    effects[effectCount++] = $"反震 {reflectedDamage:0}";
                }
            }

            LastAttackWasCritical = isCrit;
            LastDamage = totalDamage;
            LastTriggeredEffect = JoinEffects(effects, effectCount);

            if (attacker.lifeSteal > 0f && totalDamage > 0f)
            {
                attacker.Heal(totalDamage * attacker.lifeSteal);
            }

            string attackText = isCrit
                ? $"{attacker.displayName} 暴击造成 {totalDamage:0} 伤害"
                : $"{attacker.displayName} 造成 {totalDamage:0} 伤害";
            battleLog = string.IsNullOrEmpty(LastTriggeredEffect)
                ? attackText
                : $"{attackText}（{LastTriggeredEffect}）";

            return isPlayerAttack && isCrit && playerStats.equipment != null
                ? playerStats.equipment.GetCriticalCooldownMultiplier()
                : 1f;
        }

        private float CalculateOpeningShield()
        {
            if (playerStats == null || playerStats.runtimeStats == null)
            {
                return 0f;
            }

            float shield = playerStats.equipment != null ? playerStats.equipment.GetOpeningShield() : 0f;
            int rank = playerStats.GetMartialArtRank("金钟罩");
            if (rank > 0)
            {
                shield += rank * (8f + playerStats.runtimeStats.defense * 1.5f);
            }

            return shield;
        }

        private float AbsorbWithShield(float incomingDamage)
        {
            if (PlayerShield <= 0f || incomingDamage <= 0f)
            {
                return incomingDamage;
            }

            float absorbed = Mathf.Min(PlayerShield, incomingDamage);
            PlayerShield -= absorbed;
            return incomingDamage - absorbed;
        }

        private void HealPlayerOnDodge()
        {
            if (playerStats.equipment == null)
            {
                return;
            }

            float healRatio = playerStats.equipment.GetDodgeHealRatio();
            if (healRatio <= 0f)
            {
                return;
            }

            float heal = playerStats.runtimeStats.maxHealth * healRatio;
            playerStats.runtimeStats.Heal(heal);
            LastTriggeredEffect = $"游侠披风回血 {heal:0}";
        }

        private void ApplyArmorBreak(ref string[] effects, ref int effectCount)
        {
            int rank = playerStats.GetMartialArtRank("破甲掌");
            float amount = rank * 0.35f;
            if (playerStats.equipment != null)
            {
                amount += playerStats.equipment.GetArmorBreakPerHit();
            }

            if (amount <= 0f || currentEnemy == null)
            {
                return;
            }

            EnemyArmorBreak = Mathf.Min(currentEnemy.defense, EnemyArmorBreak + amount);
            effects[effectCount++] = $"破甲 {EnemyArmorBreak:0.0}";
            if (rank > 0)
            {
                RegisterMartialArtActivation("破甲掌");
            }
        }

        private void ApplyPoison(ref string[] effects, ref int effectCount)
        {
            int stacks = playerStats.GetMartialArtRank("毒砂掌");
            if (playerStats.equipment != null)
            {
                stacks += playerStats.equipment.GetPoisonStacksPerHit();
            }

            if (stacks <= 0)
            {
                return;
            }

            int poisonHeartRank = playerStats.GetMartialArtRank("百毒心经");
            int maxStacks = 8 + poisonHeartRank * 4;
            EnemyPoisonStacks = Mathf.Min(maxStacks, EnemyPoisonStacks + stacks);
            effects[effectCount++] = $"毒 {EnemyPoisonStacks}";
            if (playerStats.GetMartialArtRank("毒砂掌") > 0)
            {
                RegisterMartialArtActivation("毒砂掌");
            }
        }

        private float ApplySwordQi(CombatantStats attacker, CombatantStats defender)
        {
            float ratio = 0f;
            int rank = playerStats.GetMartialArtRank("剑气诀");
            if (rank > 0)
            {
                int interval = rank == 1 ? 3 : 2;
                if (PlayerSuccessfulHits % interval == 0)
                {
                    ratio += 0.4f + rank * 0.2f;
                    RegisterMartialArtActivation("剑气诀");
                }
            }

            if (playerStats.equipment != null)
            {
                ratio += playerStats.equipment.GetSwordQiDamageRatio(PlayerSuccessfulHits);
            }

            if (ratio <= 0f)
            {
                return 0f;
            }

            float damage = attacker.attack * ratio;
            defender.TakeDamage(damage);
            return damage;
        }

        private float ApplyRetaliation(CombatantStats attacker)
        {
            int rank = playerStats.GetMartialArtRank("反震诀");
            if (rank <= 0)
            {
                return 0f;
            }

            float ratio = 0.45f + rank * 0.20f;
            float damage = Mathf.Max(1f, playerStats.runtimeStats.defense * ratio);
            attacker.TakeDamage(damage);
            RegisterMartialArtActivation("反震诀");
            return damage;
        }

        private void ApplyPoisonTick()
        {
            if (EnemyPoisonStacks <= 0 || currentEnemy == null || currentEnemy.IsDead)
            {
                return;
            }

            int poisonHeartRank = playerStats.GetMartialArtRank("百毒心经");
            float damage = EnemyPoisonStacks * (0.55f + poisonHeartRank * 0.25f);
            currentEnemy.TakeDamage(damage);

            int lifeDrainRank = playerStats.GetMartialArtRank("吸星诀");
            if (lifeDrainRank > 0)
            {
                playerStats.runtimeStats.Heal(damage * lifeDrainRank * 0.10f);
            }

            LastAttackWasPlayer = true;
            LastAttackWasCritical = false;
            LastAttackWasDodged = false;
            LastDamage = damage;
            LastTriggeredEffect = $"毒发 {EnemyPoisonStacks} 层";
            AttackSequence += 1;
            RegisterMartialArtActivation("毒砂掌");
            if (poisonHeartRank > 0)
            {
                RegisterMartialArtActivation("百毒心经");
            }
            if (lifeDrainRank > 0)
            {
                RegisterMartialArtActivation("吸星诀");
            }
            battleLog = $"{currentEnemy.displayName} 毒发，受到 {damage:0} 伤害";
        }

        private void RegisterMartialArtActivation(string artId)
        {
            if (!string.IsNullOrEmpty(artId))
            {
                martialArtLastActivationTimes[artId] = Time.unscaledTime;
            }
        }

        private static string JoinEffects(string[] effects, int count)
        {
            string result = string.Empty;
            for (int i = 0; i < count; i++)
            {
                if (string.IsNullOrEmpty(effects[i]))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(result))
                {
                    result += " · ";
                }

                result += effects[i];
            }

            return result;
        }
    }
}
