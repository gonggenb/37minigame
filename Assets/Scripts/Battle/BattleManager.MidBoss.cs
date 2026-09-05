using UnityEngine;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.Battle
{
    public partial class BattleManager
    {
        public BossSkillId CurrentMidBossSkill { get; private set; }
        public bool IsMidBossSkillActive => IsMidBossBattle && CurrentMidBossSkill != BossSkillId.None;
        public float MidBossSkillElapsed { get; private set; }
        public float MidBossSkillDuration { get; private set; }
        public bool MidBossGuardUsed { get; private set; }
        public float MidBossWardRemaining { get; private set; }
        public float MidBossWardElapsed { get; private set; }
        public float MidBossWardBreakAge { get; private set; } = 100f;
        public BossSkillId MidBossImpactSkill { get; private set; }
        public int MidBossImpactIndex { get; private set; }
        public int MidBossImpactSequence { get; private set; }
        public float MidBossImpactAge { get; private set; } = 100f;

        private BossSkillId nextMidBossAttack = BossSkillId.MountainBreaker;
        private int midBossResolvedHits;

        private void ResetMidBossSkills()
        {
            CurrentMidBossSkill = BossSkillId.None;
            MidBossSkillElapsed = 0f;
            MidBossSkillDuration = 0f;
            MidBossGuardUsed = false;
            MidBossWardRemaining = 0f;
            MidBossWardElapsed = 0f;
            MidBossWardBreakAge = 100f;
            MidBossImpactSkill = BossSkillId.None;
            MidBossImpactIndex = 0;
            MidBossImpactSequence = 0;
            MidBossImpactAge = 100f;
            nextMidBossAttack = BossSkillId.MountainBreaker;
            midBossResolvedHits = 0;
        }

        private void AdvanceMidBossSkills(float deltaTime)
        {
            MidBossImpactAge += deltaTime;
            MidBossWardBreakAge += deltaTime;
            if (BossWard > 0f)
            {
                MidBossWardElapsed += deltaTime;
                MidBossWardRemaining = Mathf.Max(0f, MidBossWardRemaining - deltaTime);
                if (MidBossWardRemaining <= 0f)
                {
                    EndMidBossWard();
                }
            }

            if (!IsMidBossSkillActive)
            {
                return;
            }

            MidBossSkillElapsed += deltaTime;
            if (CurrentMidBossSkill == BossSkillId.MountainBreaker && midBossResolvedHits == 0 &&
                MidBossSkillElapsed >= MidBossTuning.SkillImpactDelay)
            {
                midBossResolvedHits = 1;
                ResolveMidBossHit(MidBossTuning.SkillAttackRatio, 0);
            }
            else if (CurrentMidBossSkill == BossSkillId.DoubleCleave)
            {
                if (midBossResolvedHits == 0 && MidBossSkillElapsed >= MidBossTuning.DoubleCleaveFirstImpact)
                {
                    midBossResolvedHits = 1;
                    ResolveMidBossHit(MidBossTuning.DoubleCleaveAttackRatio, 0);
                }
                if (midBossResolvedHits == 1 && MidBossSkillElapsed >= MidBossTuning.DoubleCleaveSecondImpact)
                {
                    midBossResolvedHits = 2;
                    ResolveMidBossHit(MidBossTuning.DoubleCleaveAttackRatio, 1);
                }
            }

            if (MidBossSkillElapsed >= MidBossSkillDuration)
            {
                bool wasGuard = CurrentMidBossSkill == BossSkillId.IronGuard;
                CurrentMidBossSkill = BossSkillId.None;
                if (!wasGuard)
                {
                    BossSkillCooldownDuration = MidBossTuning.SkillCooldown;
                    BossSkillCooldownRemaining = BossSkillCooldownDuration;
                }
                // Do not follow a special with an accumulated instant basic hit.
                EnemyAttackCooldownDuration = 1f / Mathf.Max(0.1f, currentEnemy.attackSpeed);
                EnemyAttackCooldownRemaining = EnemyAttackCooldownDuration;
            }
        }

        private void TryBeginMidBossSkill()
        {
            if (!IsBattleActive || !IsMidBossBattle || IsMidBossSkillActive || currentEnemy == null ||
                currentEnemy.IsDead || playerStats.runtimeStats == null || playerStats.runtimeStats.IsDead)
            {
                return;
            }

            if (!MidBossGuardUsed && currentEnemy.HealthRatio <= MidBossTuning.GuardHealthRatio)
            {
                BeginMidBossSkill(BossSkillId.IronGuard);
            }
            else if (BossSkillCooldownRemaining <= 0f)
            {
                BeginMidBossSkill(nextMidBossAttack);
                nextMidBossAttack = nextMidBossAttack == BossSkillId.MountainBreaker
                    ? BossSkillId.DoubleCleave : BossSkillId.MountainBreaker;
            }
        }

        private void BeginMidBossSkill(BossSkillId skill)
        {
            CurrentMidBossSkill = skill;
            MidBossSkillElapsed = 0f;
            midBossResolvedHits = 0;
            MidBossSkillDuration = skill == BossSkillId.DoubleCleave
                ? MidBossTuning.DoubleCleaveDuration
                : skill == BossSkillId.IronGuard ? MidBossTuning.GuardActionDuration : MidBossTuning.SkillVisualDuration;

            if (skill == BossSkillId.IronGuard)
            {
                MidBossGuardUsed = true;
                BossWardMax = currentEnemy.maxHealth * MidBossTuning.GuardMaxHealthRatio;
                BossWard = BossWardMax;
                MidBossWardRemaining = MidBossTuning.GuardDuration;
                MidBossWardElapsed = 0f;
                MidBossWardBreakAge = 100f;
            }
            // Starting a pose is not a hit: keep AttackSequence for resolved damage/audio only.
            TriggerBossSkill(skill, currentEnemy.displayName + "：" + GetMidBossSkillName(skill));
        }

        private static string GetMidBossSkillName(BossSkillId skill)
        {
            return skill == BossSkillId.DoubleCleave ? GameTextCatalog.MidBossDoubleCleaveName
                : skill == BossSkillId.IronGuard ? GameTextCatalog.MidBossIronGuardName
                : GameTextCatalog.MidBossSkillName;
        }

        private void EndMidBossWard()
        {
            BossWard = 0f;
            MidBossWardRemaining = 0f;
            MidBossWardBreakAge = 0f;
        }

        private void ResolveMidBossHit(float attackRatio, int hitIndex)
        {
            CombatantStats player = playerStats.runtimeStats;
            if (currentEnemy == null || player == null || currentEnemy.IsDead || player.IsDead)
            {
                return;
            }
            MidBossImpactSkill = CurrentMidBossSkill;
            MidBossImpactIndex = hitIndex;
            MidBossImpactAge = 0f;
            MidBossImpactSequence += 1;
            LastAttackWasPlayer = false;
            LastAttackWasCritical = false;
            LastAttackWasDodged = false;
            LastDamage = 0f;
            LastPoisonStackDelta = 0;
            LastPoisonDamage = 0f;
            LastTriggeredEffect = GetMidBossSkillName(CurrentMidBossSkill);
            LastSkillVfxName = LastTriggeredEffect;
            LastVfxCues = CurrentMidBossSkill == BossSkillId.DoubleCleave
                ? BattleVfxCue.DoubleCleave : BattleVfxCue.MountainBreaker;
            AttackSequence += 1;
            EnemyAttackAttempts += 1;
            int shadowRank = playerStats.GetMartialArtRank("无相残影");
            int interval = shadowRank <= 0 ? int.MaxValue : 7 - shadowRank;
            bool forcedDodge = interval < int.MaxValue && EnemyAttackAttempts % interval == 0;
            if (forcedDodge || UnityEngine.Random.value < player.dodgeChance)
            {
                LastAttackWasDodged = true;
                LastVfxCues |= BattleVfxCue.Dodge;
                HealPlayerOnDodge();
                if (forcedDodge)
                {
                    LastVfxCues |= BattleVfxCue.ShadowDodge;
                    RegisterMartialArtActivation("无相残影");
                }
                battleLog = currentEnemy.displayName + "的" + LastTriggeredEffect + "被闪开";
                return;
            }

            float damage = Mathf.Max(1f, currentEnemy.attack * attackRatio - player.defense);
            damage *= GetPlayerIncomingDamageMultiplier(player);
            float shieldBefore = PlayerShield;
            damage = AbsorbWithShield(damage);
            if (PlayerShield < shieldBefore)
            {
                LastVfxCues |= BattleVfxCue.ShieldImpact;
            }
            player.TakeDamage(damage);
            LastDamage = damage;
            LastVfxCues |= BattleVfxCue.BasicHit;
            if (damage > 0f)
            {
                ApplyRetaliation(currentEnemy);
            }
            battleLog = currentEnemy.displayName + "施展" + LastTriggeredEffect + "，造成 " +
                        CombatNumberDisplay.Format(damage) + " 伤害";
        }
    }
}
