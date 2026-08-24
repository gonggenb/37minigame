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
        public string LastSkillVfxName { get; private set; } = string.Empty;
        public BattleVfxCue LastVfxCues { get; private set; }
        public int PlayerSuccessfulHits { get; private set; }
        public int EnemyAttackAttempts { get; private set; }
        public int EnemyPoisonStacks { get; private set; }
        public int EnemyPoisonMaxStacks => playerStats == null
            ? 8
            : 8 + playerStats.GetMartialArtRank("百毒心经") * 4 +
              playerStats.GetMartialArtRank("化功毒雾") * 3;
        public int LastPoisonStackDelta { get; private set; }
        public float LastPoisonDamage { get; private set; }
        public float EnemyArmorBreak { get; private set; }
        public float PlayerShield { get; private set; }
        public float PlayerAttackCooldownRemaining { get; private set; }
        public float PlayerAttackCooldownDuration { get; private set; }
        public float EnemyAttackCooldownRemaining { get; private set; }
        public float EnemyAttackCooldownDuration { get; private set; }
        public bool IsBossBattle { get; private set; }
        public BossBattlePhase CurrentBossPhase { get; private set; }
        public BossSkillId LastBossSkill { get; private set; }
        public int BossSkillSequence { get; private set; }
        public float LastBossSkillTriggeredAt { get; private set; } = -100f;
        public float BossWard { get; private set; }
        public float BossWardMax { get; private set; }
        public float BossSkillCooldownRemaining { get; private set; }
        public float BossSkillCooldownDuration { get; private set; }

        public string CurrentBossPhaseName => CurrentBossPhase switch
        {
            BossBattlePhase.Foxfire => GameTextCatalog.FinalBossPhaseOneName,
            BossBattlePhase.DemonArmor => GameTextCatalog.FinalBossPhaseTwoName,
            BossBattlePhase.BloodFrenzy => GameTextCatalog.FinalBossPhaseThreeName,
            _ => string.Empty
        };

        public string LastBossSkillName => LastBossSkill switch
        {
            BossSkillId.FoxfireBarrage => GameTextCatalog.FinalBossFoxfireSkillName,
            BossSkillId.DemonArmor => GameTextCatalog.FinalBossPhaseTwoName,
            BossSkillId.BloodFrenzy => GameTextCatalog.FinalBossPhaseThreeName,
            _ => string.Empty
        };

        private Coroutine battleRoutine;
        private float poisonTickCooldown;
        private float bossBaseAttack;
        private float bossBaseAttackSpeed;
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
            BeginBattleInternal(enemy, onComplete, false);
        }

        public void BeginBossBattle(CombatantStats enemy, Action<bool> onComplete)
        {
            BeginBattleInternal(enemy, onComplete, true);
        }

        public void DebugSetBossHealthRatio(float healthRatio)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!IsBattleActive || !IsBossBattle || currentEnemy == null || currentEnemy.IsDead)
            {
                return;
            }

            currentEnemy.currentHealth = Mathf.Clamp(
                currentEnemy.maxHealth * Mathf.Clamp01(healthRatio),
                1f,
                currentEnemy.maxHealth);
            UpdateBossPhase();
#endif
        }

        private void BeginBattleInternal(CombatantStats enemy, Action<bool> onComplete, bool isBossBattle)
        {
            CancelBattle();
            currentEnemy = enemy;
            IsBossBattle = isBossBattle;
            CurrentBossPhase = isBossBattle ? BossBattlePhase.Foxfire : BossBattlePhase.None;
            bossBaseAttack = enemy != null ? enemy.attack : 0f;
            bossBaseAttackSpeed = enemy != null ? enemy.attackSpeed : 0f;
            BossWard = 0f;
            BossWardMax = 0f;
            BossSkillCooldownDuration = isBossBattle ? BossV2Tuning.OpeningFoxfireDelay : 0f;
            BossSkillCooldownRemaining = BossSkillCooldownDuration;
            LastBossSkill = BossSkillId.None;
            BossSkillSequence = 0;
            LastBossSkillTriggeredAt = -100f;
            IsBattleActive = true;
            BattleElapsed = 0f;
            AttackSequence = 0;
            LastDamage = 0f;
            LastAttackWasCritical = false;
            LastAttackWasDodged = false;
            LastTriggeredEffect = string.Empty;
            LastSkillVfxName = string.Empty;
            LastVfxCues = BattleVfxCue.None;
            PlayerSuccessfulHits = 0;
            EnemyAttackAttempts = 0;
            EnemyPoisonStacks = 0;
            LastPoisonStackDelta = 0;
            LastPoisonDamage = 0f;
            EnemyArmorBreak = 0f;
            poisonTickCooldown = 1f;
            martialArtLastActivationTimes.Clear();
            PlayerShield = CalculateOpeningShield();
            if (playerStats.GetMartialArtRank("金钟罩") > 0)
            {
                RegisterMartialArtActivation("金钟罩");
            }
            battleLog = PlayerShield > 0f
                ? $"遭遇 {currentEnemy.displayName}，护盾 {CombatNumberDisplay.FormatSigned(PlayerShield)}"
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
            LastPoisonStackDelta = 0;
            LastPoisonDamage = 0f;
            EnemyArmorBreak = 0f;
            LastSkillVfxName = string.Empty;
            LastVfxCues = BattleVfxCue.None;
            PlayerAttackCooldownRemaining = 0f;
            PlayerAttackCooldownDuration = 0f;
            EnemyAttackCooldownRemaining = 0f;
            EnemyAttackCooldownDuration = 0f;
            IsBossBattle = false;
            CurrentBossPhase = BossBattlePhase.None;
            LastBossSkill = BossSkillId.None;
            BossSkillSequence = 0;
            LastBossSkillTriggeredAt = -100f;
            BossWard = 0f;
            BossWardMax = 0f;
            BossSkillCooldownRemaining = 0f;
            BossSkillCooldownDuration = 0f;
            bossBaseAttack = 0f;
            bossBaseAttackSpeed = 0f;
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
                if (IsBossBattle)
                {
                    UpdateBossPhase();
                    BossSkillCooldownRemaining -= combatDeltaTime;
                }

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

                if (IsBossBattle && currentEnemy != null && !currentEnemy.IsDead &&
                    playerStats.runtimeStats != null && !playerStats.runtimeStats.IsDead &&
                    BossSkillCooldownRemaining <= 0f)
                {
                    CastFoxfireBarrage();
                    BossSkillCooldownDuration = GetFoxfireCooldown();
                    BossSkillCooldownRemaining = BossSkillCooldownDuration;
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
            LastSkillVfxName = string.Empty;
            LastPoisonStackDelta = 0;
            LastPoisonDamage = 0f;
            LastVfxCues = BattleVfxCue.None;
            AttackSequence += 1;

            bool forcedShadowDodge = false;
            if (!isPlayerAttack)
            {
                EnemyAttackAttempts += 1;
                int shadowRank = playerStats.GetMartialArtRank("无相残影");
                int interval = shadowRank <= 0 ? int.MaxValue : 7 - shadowRank;
                forcedShadowDodge = interval < int.MaxValue && EnemyAttackAttempts % interval == 0;
            }

            if (forcedShadowDodge || UnityEngine.Random.value < defender.dodgeChance)
            {
                LastAttackWasDodged = true;
                LastVfxCues |= BattleVfxCue.Dodge;
                if (!isPlayerAttack)
                {
                    HealPlayerOnDodge();
                    if (forcedShadowDodge)
                    {
                        LastVfxCues |= BattleVfxCue.ShadowDodge;
                        RegisterMartialArtActivation("无相残影");
                        FeatureSkillVfx("无相残影");
                        LastTriggeredEffect = string.IsNullOrEmpty(LastTriggeredEffect)
                            ? "无相残影"
                            : LastTriggeredEffect + " · 无相残影";
                    }
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

            float attackPower = attacker.attack;
            if (isPlayerAttack)
            {
                int openingRank = playerStats.GetMartialArtRank("惊鸿一式");
                if (openingRank > 0 && PlayerSuccessfulHits == 0)
                {
                    attackPower *= 1f + 0.20f + openingRank * 0.25f;
                    LastVfxCues |= BattleVfxCue.OpeningStrike;
                    RegisterMartialArtActivation("惊鸿一式");
                    FeatureSkillVfx("惊鸿一式");
                }

                int bloodBattleRank = playerStats.GetMartialArtRank("血战八方");
                if (bloodBattleRank > 0 && attacker.currentHealth <= attacker.maxHealth * 0.5f)
                {
                    attackPower *= 1f + 0.06f + bloodBattleRank * 0.12f;
                    LastVfxCues |= BattleVfxCue.BloodPower;
                    RegisterMartialArtActivation("血战八方");
                    FeatureSkillVfx("血战八方");
                }
            }

            float damage = Mathf.Max(1f, attackPower - effectiveDefense);
            float critChance = attacker.critChance;
            if (isPlayerAttack && attacker.currentHealth <= attacker.maxHealth * 0.5f)
            {
                int bloodDomainRank = playerStats.GetMartialArtRank("修罗血域");
                if (bloodDomainRank > 0)
                {
                    critChance += 0.04f + bloodDomainRank * 0.06f;
                }
            }
            bool isCrit = UnityEngine.Random.value < Mathf.Clamp01(critChance);
            if (isCrit)
            {
                damage *= Mathf.Max(1f, attacker.critMultiplier);
                LastVfxCues |= BattleVfxCue.CriticalHit;
            }

            if (!isPlayerAttack)
            {
                float reduction = playerStats.GetMartialArtRank("不动明王身") * 0.06f + 0.02f;
                if (playerStats.GetMartialArtRank("不动明王身") <= 0)
                {
                    reduction = 0f;
                }

                int bloodArmorRank = playerStats.GetSecretRank("血铸金身");
                if (bloodArmorRank > 0 && defender.currentHealth <= defender.maxHealth * 0.5f)
                {
                    reduction += bloodArmorRank * 0.06f;
                }
                damage *= 1f - Mathf.Clamp(reduction, 0f, 0.65f);
            }

            float shieldBefore = PlayerShield;
            if (!isPlayerAttack)
            {
                damage = AbsorbWithShield(damage);
            }

            if (isPlayerAttack && ReferenceEquals(defender, currentEnemy))
            {
                ApplyDamageToCurrentEnemy(damage);
            }
            else
            {
                defender.TakeDamage(damage);
            }
            float totalDamage = damage;
            LastVfxCues |= BattleVfxCue.BasicHit;
            string[] effects = new string[8];
            int effectCount = 0;
            bool triggeredSwiftCapstone = false;
            float shieldAbsorbed = shieldBefore - PlayerShield;
            if (shieldAbsorbed > 0f)
            {
                LastVfxCues |= BattleVfxCue.ShieldImpact;
                effects[effectCount++] = $"护盾抵消 {CombatNumberDisplay.Format(shieldAbsorbed)}";
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
                    effects[effectCount++] = $"剑气 {CombatNumberDisplay.Format(swordQiDamage)}";
                }
                float comboDamage = ApplySwiftCapstone(attacker, defender);
                if (comboDamage > 0f)
                {
                    triggeredSwiftCapstone = true;
                    totalDamage += comboDamage;
                    effects[effectCount++] = $"连环剑 {CombatNumberDisplay.Format(comboDamage)}";
                }
            }
            else if (damage > 0f)
            {
                float reflectedDamage = ApplyRetaliation(attacker);
                if (reflectedDamage > 0f)
                {
                    effects[effectCount++] = $"反震 {CombatNumberDisplay.Format(reflectedDamage)}";
                }
            }

            LastAttackWasCritical = isCrit;
            LastDamage = totalDamage;
            LastTriggeredEffect = JoinEffects(effects, effectCount);

            if (attacker.lifeSteal > 0f && totalDamage > 0f)
            {
                float healthBeforeHeal = attacker.currentHealth;
                attacker.Heal(totalDamage * attacker.lifeSteal);
                if (attacker.currentHealth > healthBeforeHeal + 0.01f)
                {
                    LastVfxCues |= BattleVfxCue.Heal;
                }
            }

            if (isPlayerAttack && isCrit && playerStats.GetMartialArtRank("修罗血域") > 0)
            {
                LastVfxCues |= BattleVfxCue.BloodBurst;
                RegisterMartialArtActivation("修罗血域");
                FeatureSkillVfx("修罗血域");
            }

            string attackText = isCrit
                ? $"{attacker.displayName} 暴击造成 {CombatNumberDisplay.Format(totalDamage)} 伤害"
                : $"{attacker.displayName} 造成 {CombatNumberDisplay.Format(totalDamage)} 伤害";
            battleLog = string.IsNullOrEmpty(LastTriggeredEffect)
                ? attackText
                : $"{attackText}（{LastTriggeredEffect}）";

            float cooldownMultiplier = triggeredSwiftCapstone ? 0.55f : 1f;
            if (isPlayerAttack && isCrit && playerStats.equipment != null)
            {
                cooldownMultiplier = Mathf.Min(
                    cooldownMultiplier,
                    playerStats.equipment.GetCriticalCooldownMultiplier());
            }
            return cooldownMultiplier;
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

            int immovableRank = playerStats.GetMartialArtRank("不动明王身");
            if (immovableRank > 0)
            {
                shield += immovableRank * 12f;
                RegisterMartialArtActivation("不动明王身");
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
            float healRatio = playerStats.equipment != null
                ? playerStats.equipment.GetDodgeHealRatio()
                : 0f;
            if (healRatio > 0f)
            {
                float heal = playerStats.runtimeStats.maxHealth * healRatio;
                playerStats.runtimeStats.Heal(heal);
                LastVfxCues |= BattleVfxCue.Heal;
                LastTriggeredEffect = $"闪避回血 {CombatNumberDisplay.Format(heal)}";
            }

            int bellShadowRank = playerStats.GetSecretRank("虚实金钟");
            if (bellShadowRank > 0)
            {
                float shieldGain = playerStats.runtimeStats.maxHealth * bellShadowRank * 0.04f;
                PlayerShield += shieldGain;
                LastVfxCues |= BattleVfxCue.ShieldImpact;
                LastTriggeredEffect = string.IsNullOrEmpty(LastTriggeredEffect)
                    ? $"虚实金钟 {CombatNumberDisplay.FormatSigned(shieldGain)}盾"
                    : LastTriggeredEffect + $" · 虚实金钟 {CombatNumberDisplay.FormatSigned(shieldGain)}盾";
            }

            int pursuitRank = playerStats.GetSecretRank("无影追风");
            if (pursuitRank > 0)
            {
                PlayerAttackCooldownRemaining *= 1f - pursuitRank * 0.35f;
                LastTriggeredEffect = string.IsNullOrEmpty(LastTriggeredEffect)
                    ? "无影追风"
                    : LastTriggeredEffect + " · 无影追风";
            }
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
            LastVfxCues |= BattleVfxCue.ArmorBreak;
            effects[effectCount++] = $"破甲 {CombatNumberDisplay.Format(EnemyArmorBreak)}";
            if (rank > 0)
            {
                RegisterMartialArtActivation("破甲掌");
                FeatureSkillVfx("破甲掌");
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

            int stacksBefore = EnemyPoisonStacks;
            EnemyPoisonStacks = Mathf.Min(EnemyPoisonMaxStacks, EnemyPoisonStacks + stacks);
            LastPoisonStackDelta += EnemyPoisonStacks - stacksBefore;
            LastVfxCues |= BattleVfxCue.PoisonApplied;
            effects[effectCount++] = $"毒 {EnemyPoisonStacks}";
            if (playerStats.GetMartialArtRank("毒砂掌") > 0)
            {
                RegisterMartialArtActivation("毒砂掌");
                FeatureSkillVfx("毒砂掌");
            }
            else
            {
                FeatureSkillVfx("淬毒");
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
                    FeatureSkillVfx("剑气诀");
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
            LastVfxCues |= BattleVfxCue.SwordQi;
            ApplyDamageToCurrentEnemy(damage);
            int temperedPoisonRank = playerStats.GetSecretRank("青锋淬毒");
            if (temperedPoisonRank > 0 && currentEnemy != null)
            {
                int stacksBefore = EnemyPoisonStacks;
                EnemyPoisonStacks = Mathf.Min(
                    EnemyPoisonMaxStacks,
                    EnemyPoisonStacks + temperedPoisonRank);
                LastPoisonStackDelta += EnemyPoisonStacks - stacksBefore;
                LastVfxCues |= BattleVfxCue.PoisonApplied;
                FeatureSkillVfx("青锋淬毒");
            }
            return damage;
        }

        private float ApplySwiftCapstone(CombatantStats attacker, CombatantStats defender)
        {
            int rank = playerStats.GetMartialArtRank("无影连环剑");
            if (rank <= 0)
            {
                return 0f;
            }

            int interval = 6 - rank;
            if (PlayerSuccessfulHits % interval != 0)
            {
                return 0f;
            }

            float damage = attacker.attack * (0.70f + rank * 0.20f);
            ApplyDamageToCurrentEnemy(damage);
            LastVfxCues |= BattleVfxCue.SwiftCombo;
            RegisterMartialArtActivation("无影连环剑");
            FeatureSkillVfx("无影连环剑");
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
            ApplyDamageToCurrentEnemy(damage);
            LastVfxCues |= BattleVfxCue.Retaliation;
            RegisterMartialArtActivation("反震诀");
            FeatureSkillVfx("反震诀");
            return damage;
        }

        private void ApplyPoisonTick()
        {
            if (EnemyPoisonStacks <= 0 || currentEnemy == null || currentEnemy.IsDead)
            {
                return;
            }

            int poisonHeartRank = playerStats.GetMartialArtRank("百毒心经");
            LastVfxCues = BattleVfxCue.PoisonTick;
            LastSkillVfxName = string.Empty;
            LastPoisonStackDelta = 0;
            LastPoisonDamage = 0f;
            float damage = EnemyPoisonStacks * (0.55f + poisonHeartRank * 0.25f);
            int poisonMistRank = playerStats.GetMartialArtRank("化功毒雾");
            if (poisonMistRank > 0)
            {
                LastVfxCues |= BattleVfxCue.PoisonMist | BattleVfxCue.ArmorBreak;
                damage *= 1f + 0.05f + poisonMistRank * 0.15f;
                EnemyArmorBreak = Mathf.Min(currentEnemy.defense, EnemyArmorBreak + poisonMistRank * 0.45f);
                RegisterMartialArtActivation("化功毒雾");
                FeatureSkillVfx("化功毒雾");
            }
            ApplyDamageToCurrentEnemy(damage);

            int lifeDrainRank = playerStats.GetMartialArtRank("吸星诀");
            float healthBeforePoisonHeal = playerStats.runtimeStats.currentHealth;
            if (lifeDrainRank > 0)
            {
                playerStats.runtimeStats.Heal(damage * lifeDrainRank * 0.10f);
            }
            int poisonBloodRank = playerStats.GetSecretRank("以毒养血");
            if (poisonBloodRank > 0)
            {
                playerStats.runtimeStats.Heal(damage * poisonBloodRank * 0.08f);
            }
            if (playerStats.runtimeStats.currentHealth > healthBeforePoisonHeal + 0.01f)
            {
                LastVfxCues |= BattleVfxCue.Heal;
            }

            LastAttackWasPlayer = true;
            LastAttackWasCritical = false;
            LastAttackWasDodged = false;
            LastDamage = damage;
            LastPoisonDamage = damage;
            LastTriggeredEffect = $"毒发 {EnemyPoisonStacks} 层";
            AttackSequence += 1;
            RegisterMartialArtActivation("毒砂掌");
            if (string.IsNullOrEmpty(LastSkillVfxName))
            {
                FeatureSkillVfx("毒发");
            }
            if (poisonHeartRank > 0)
            {
                RegisterMartialArtActivation("百毒心经");
            }
            if (lifeDrainRank > 0)
            {
                RegisterMartialArtActivation("吸星诀");
            }
            battleLog = $"{currentEnemy.displayName} 毒发，受到 {CombatNumberDisplay.Format(damage)} 伤害";
        }

        private void UpdateBossPhase()
        {
            if (!IsBossBattle || currentEnemy == null || currentEnemy.IsDead)
            {
                return;
            }

            float healthRatio = currentEnemy.HealthRatio;
            if (healthRatio <= BossV2Tuning.PhaseTwoHealthRatio &&
                CurrentBossPhase == BossBattlePhase.Foxfire)
            {
                CurrentBossPhase = BossBattlePhase.DemonArmor;
                BossWardMax = currentEnemy.maxHealth * BossV2Tuning.DemonArmorMaxHealthRatio;
                BossWard = BossWardMax;
                TriggerBossSkill(BossSkillId.DemonArmor,
                    $"{currentEnemy.displayName}凝成妖甲：全伤害均可破甲");
                return;
            }

            if (healthRatio <= BossV2Tuning.PhaseThreeHealthRatio &&
                CurrentBossPhase != BossBattlePhase.BloodFrenzy)
            {
                CurrentBossPhase = BossBattlePhase.BloodFrenzy;
                currentEnemy.attack = bossBaseAttack * BossV2Tuning.BloodFrenzyAttackMultiplier;
                currentEnemy.attackSpeed = bossBaseAttackSpeed * BossV2Tuning.BloodFrenzyAttackSpeedMultiplier;
                EnemyAttackCooldownRemaining = Mathf.Min(
                    EnemyAttackCooldownRemaining,
                    1f / Mathf.Max(0.1f, currentEnemy.attackSpeed));
                TriggerBossSkill(BossSkillId.BloodFrenzy,
                    $"{currentEnemy.displayName}踏入残血狂暴：攻势与狐火加快");
            }
        }

        private void CastFoxfireBarrage()
        {
            CombatantStats player = playerStats.runtimeStats;
            if (currentEnemy == null || player == null || player.IsDead)
            {
                return;
            }

            LastAttackWasPlayer = false;
            LastAttackWasCritical = false;
            LastAttackWasDodged = false;
            LastDamage = 0f;
            LastTriggeredEffect = string.Empty;
            LastSkillVfxName = GameTextCatalog.FinalBossFoxfireSkillName;
            LastPoisonStackDelta = 0;
            LastPoisonDamage = 0f;
            LastVfxCues = BattleVfxCue.Foxfire;
            AttackSequence += 1;

            int landedHits = 0;
            int dodgedHits = 0;
            float totalDamage = 0f;
            for (int i = 0;
                 i < BossV2Tuning.FoxfireHitCount && !player.IsDead && !currentEnemy.IsDead;
                 i++)
            {
                EnemyAttackAttempts += 1;
                int shadowRank = playerStats.GetMartialArtRank("无相残影");
                int shadowInterval = shadowRank <= 0 ? int.MaxValue : 7 - shadowRank;
                bool forcedShadowDodge = shadowInterval < int.MaxValue &&
                                         EnemyAttackAttempts % shadowInterval == 0;
                int lightnessRank = playerStats.GetMartialArtRank("踏雪无痕");
                float foxfireDodgeChance = Mathf.Clamp01(
                    player.dodgeChance +
                    lightnessRank * BossV2Tuning.FoxfireLightnessDodgeBonusPerRank);
                if (forcedShadowDodge || UnityEngine.Random.value < foxfireDodgeChance)
                {
                    dodgedHits += 1;
                    HealPlayerOnDodge();
                    if (forcedShadowDodge)
                    {
                        RegisterMartialArtActivation("无相残影");
                    }
                    continue;
                }

                float damage = Mathf.Max(1f,
                    currentEnemy.attack * BossV2Tuning.FoxfireAttackRatioPerHit -
                    player.defense * BossV2Tuning.FoxfireDefenseRatioPerHit);
                damage *= GetPlayerIncomingDamageMultiplier(player);
                damage = AbsorbWithShield(damage);
                player.TakeDamage(damage);
                totalDamage += damage;
                landedHits += 1;
                if (damage > 0f)
                {
                    ApplyRetaliation(currentEnemy);
                }
            }

            LastAttackWasDodged = landedHits == 0 && dodgedHits > 0;
            LastDamage = totalDamage;
            LastTriggeredEffect = $"{GameTextCatalog.FinalBossFoxfireSkillName} {landedHits}中{dodgedHits}避";
            TriggerBossSkill(BossSkillId.FoxfireBarrage,
                landedHits > 0
                    ? $"{currentEnemy.displayName}施展狐火连击，造成 {CombatNumberDisplay.Format(totalDamage)} 伤害"
                    : $"{currentEnemy.displayName}施展狐火连击，尽数被闪开");
        }

        private float GetFoxfireCooldown()
        {
            return CurrentBossPhase switch
            {
                BossBattlePhase.BloodFrenzy => BossV2Tuning.PhaseThreeFoxfireCooldown,
                BossBattlePhase.DemonArmor => BossV2Tuning.PhaseTwoFoxfireCooldown,
                _ => BossV2Tuning.PhaseOneFoxfireCooldown
            };
        }

        private void TriggerBossSkill(BossSkillId skill, string log)
        {
            LastBossSkill = skill;
            BossSkillSequence += 1;
            LastBossSkillTriggeredAt = Time.unscaledTime;
            battleLog = log;
        }

        private float GetPlayerIncomingDamageMultiplier(CombatantStats defender)
        {
            float reduction = playerStats.GetMartialArtRank("不动明王身") * 0.06f + 0.02f;
            if (playerStats.GetMartialArtRank("不动明王身") <= 0)
            {
                reduction = 0f;
            }

            int bloodArmorRank = playerStats.GetSecretRank("血铸金身");
            if (bloodArmorRank > 0 && defender.currentHealth <= defender.maxHealth * 0.5f)
            {
                reduction += bloodArmorRank * 0.06f;
            }

            return 1f - Mathf.Clamp(reduction, 0f, 0.65f);
        }

        private float ApplyDamageToCurrentEnemy(float damage)
        {
            if (currentEnemy == null || damage <= 0f)
            {
                return 0f;
            }

            float remaining = damage;
            if (IsBossBattle && BossWard > 0f)
            {
                float absorbed = Mathf.Min(BossWard, remaining);
                BossWard -= absorbed;
                remaining -= absorbed;
            }

            float healthBefore = currentEnemy.currentHealth;
            if (remaining > 0f)
            {
                currentEnemy.TakeDamage(remaining);
            }

            return damage - remaining + (healthBefore - currentEnemy.currentHealth);
        }

        private void RegisterMartialArtActivation(string artId)
        {
            if (!string.IsNullOrEmpty(artId))
            {
                martialArtLastActivationTimes[artId] = Time.unscaledTime;
            }
        }

        private void FeatureSkillVfx(string skillName)
        {
            if (!string.IsNullOrEmpty(skillName))
            {
                LastSkillVfxName = skillName;
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
