using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.Battle
{
    public partial class BattleManager
    {
        public BossSkillId CurrentFinalBossAction { get; private set; }
        public float FinalBossActionElapsed { get; private set; }
        public float FinalBossActionDuration => CurrentFinalBossAction == BossSkillId.FoxfireBarrage
            ? BossV2Tuning.FoxfireActionDuration : BossV2Tuning.PhaseActionDuration;
        public bool IsFinalBossActionActive => IsBattleActive && IsBossBattle &&
            currentEnemy != null && !currentEnemy.IsDead && playerStats.runtimeStats != null &&
            !playerStats.runtimeStats.IsDead && CurrentFinalBossAction != BossSkillId.None;
        public int FoxfireImpactSequence { get; private set; }
        public int FoxfireImpactIndex { get; private set; }
        public float FoxfireImpactAge { get; private set; } = 100f;
        public bool FoxfireImpactDodged { get; private set; }
        public float FinalBossWardAge { get; private set; } = 100f;
        public float FinalBossWardBreakAge { get; private set; } = 100f;
        public float FinalBossFrenzyAge { get; private set; } = 100f;
        private int finalBossHitsResolved;
        private readonly Queue<BossSkillId> finalBossPhaseActions = new Queue<BossSkillId>(2);

        private void ResetFinalBossSkills()
        {
            StopFinalBossAction();
            FoxfireImpactSequence = 0;
            FoxfireImpactIndex = 0;
            FoxfireImpactAge = FinalBossWardAge = FinalBossWardBreakAge = FinalBossFrenzyAge = 100f;
            FoxfireImpactDodged = false;
        }

        private void StopFinalBossAction()
        {
            CurrentFinalBossAction = BossSkillId.None;
            FinalBossActionElapsed = 0f;
            finalBossHitsResolved = 0;
            finalBossPhaseActions.Clear();
        }

        private void BeginFinalBossAction(BossSkillId action)
        {
            CurrentFinalBossAction = action;
            FinalBossActionElapsed = 0f;
            finalBossHitsResolved = 0;
        }

        private void QueueFinalBossPhaseAction(BossSkillId skill)
        {
            if (!IsBossBattle || (skill != BossSkillId.DemonArmor && skill != BossSkillId.BloodFrenzy)) return;
            // Mechanical phase changes still apply immediately at 70% / 35%.
            // Their presentation waits for the active action so a cast is never overwritten.
            if (skill == BossSkillId.DemonArmor) FinalBossWardAge = 0f;
            else FinalBossFrenzyAge = 0f;
            if (CurrentFinalBossAction == BossSkillId.None) BeginFinalBossAction(skill);
            else finalBossPhaseActions.Enqueue(skill);
        }

        private void AdvanceFinalBossSkills(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            FoxfireImpactAge += deltaTime;
            FinalBossWardAge += deltaTime;
            FinalBossWardBreakAge += deltaTime;
            FinalBossFrenzyAge += deltaTime;
            if (!IsFinalBossActionActive) return;
            FinalBossActionElapsed += deltaTime;
            if (CurrentFinalBossAction == BossSkillId.FoxfireBarrage)
            {
                while (finalBossHitsResolved < BossV2Tuning.FoxfireHitCount &&
                       FinalBossActionElapsed >= BossV2Tuning.FoxfireFirstImpact +
                           finalBossHitsResolved * BossV2Tuning.FoxfireImpactInterval)
                {
                    if (currentEnemy.IsDead || playerStats.runtimeStats.IsDead) break;
                    ResolveFoxfireHit(finalBossHitsResolved++);
                }
            }
            if (FinalBossActionElapsed < FinalBossActionDuration) return;
            CurrentFinalBossAction = BossSkillId.None;
            if (finalBossPhaseActions.Count > 0 && !currentEnemy.IsDead && !playerStats.runtimeStats.IsDead)
                BeginFinalBossAction(finalBossPhaseActions.Dequeue());
        }

        private void ResolveFoxfireHit(int index)
        {
            CombatantStats player = playerStats.runtimeStats;
            EnemyAttackAttempts++;
            int shadowRank = playerStats.GetMartialArtRank("无相残影");
            int shadowInterval = shadowRank <= 0 ? int.MaxValue : 7 - shadowRank;
            bool forcedShadowDodge = shadowInterval < int.MaxValue && EnemyAttackAttempts % shadowInterval == 0;
            int lightnessRank = playerStats.GetMartialArtRank("踏雪无痕");
            float dodgeChance = Mathf.Clamp01(player.dodgeChance +
                lightnessRank * BossV2Tuning.FoxfireLightnessDodgeBonusPerRank);
            bool dodged = forcedShadowDodge || UnityEngine.Random.value < dodgeChance;
            float damage = 0f;
            if (dodged)
            {
                HealPlayerOnDodge();
                if (forcedShadowDodge) RegisterMartialArtActivation("无相残影");
            }
            else
            {
                damage = Mathf.Max(1f, currentEnemy.attack * BossV2Tuning.FoxfireAttackRatioPerHit -
                    player.defense * BossV2Tuning.FoxfireDefenseRatioPerHit);
                damage *= GetPlayerIncomingDamageMultiplier(player);
                damage = RollDamage(damage);
                damage = AbsorbWithShield(damage);
                player.TakeDamage(damage);
                if (damage > 0f) ApplyRetaliation(currentEnemy);
            }
            FoxfireImpactSequence++;
            FoxfireImpactIndex = index;
            FoxfireImpactAge = 0f;
            FoxfireImpactDodged = dodged;
            LastAttackWasPlayer = false;
            LastAttackWasCritical = false;
            LastAttackWasDodged = dodged;
            LastDamage = damage;
            LastPoisonStackDelta = 0;
            LastPoisonDamage = 0f;
            LastSkillVfxName = GameTextCatalog.FinalBossFoxfireSkillName;
            LastVfxCues = BattleVfxCue.Foxfire;
            LastTriggeredEffect = LastSkillVfxName;
            AttackSequence++;
            battleLog = dodged ? $"{currentEnemy.displayName}施展狐火连击，尽数被闪开"
                : $"{currentEnemy.displayName}施展狐火连击，造成 {CombatNumberDisplay.Format(damage)} 伤害";
        }
    }
}
