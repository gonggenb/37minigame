using System;
using System.Collections;
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

        private Coroutine battleRoutine;

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
            battleLog = $"遭遇 {currentEnemy.displayName}";
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
        }

        private IEnumerator RunBattle(Action<bool> onComplete)
        {
            float playerCooldown = 0.2f;
            float enemyCooldown = 0.7f;

            while (playerStats.runtimeStats != null && !playerStats.runtimeStats.IsDead && currentEnemy != null && !currentEnemy.IsDead)
            {
                float combatDeltaTime = Time.deltaTime * BattleSpeedMultiplier;
                BattleElapsed += Time.deltaTime;
                playerCooldown -= combatDeltaTime;
                enemyCooldown -= combatDeltaTime;

                if (playerCooldown <= 0f)
                {
                    DoAttack(playerStats.runtimeStats, currentEnemy);
                    playerCooldown = 1f / Mathf.Max(0.1f, playerStats.runtimeStats.attackSpeed);
                }

                if (currentEnemy != null && !currentEnemy.IsDead && enemyCooldown <= 0f)
                {
                    DoAttack(currentEnemy, playerStats.runtimeStats);
                    enemyCooldown = 1f / Mathf.Max(0.1f, currentEnemy.attackSpeed);
                }

                yield return null;
            }

            bool playerWon = playerStats.runtimeStats != null && !playerStats.runtimeStats.IsDead;
            battleLog = playerWon ? $"击败 {currentEnemy.displayName}" : "少侠气血耗尽";
            yield return new WaitForSeconds(0.55f / BattleSpeedMultiplier);
            IsBattleActive = false;
            battleRoutine = null;
            onComplete?.Invoke(playerWon);
        }

        private void DoAttack(CombatantStats attacker, CombatantStats defender)
        {
            LastAttackWasPlayer = ReferenceEquals(attacker, playerStats.runtimeStats);
            LastAttackWasCritical = false;
            LastAttackWasDodged = false;
            LastDamage = 0f;
            AttackSequence += 1;

            if (UnityEngine.Random.value < defender.dodgeChance)
            {
                LastAttackWasDodged = true;
                battleLog = $"{defender.displayName} 闪开了 {attacker.displayName} 的攻击";
                return;
            }

            float damage = Mathf.Max(1f, attacker.attack - defender.defense);
            bool isCrit = UnityEngine.Random.value < attacker.critChance;
            if (isCrit)
            {
                damage *= Mathf.Max(1f, attacker.critMultiplier);
            }

            LastAttackWasCritical = isCrit;
            LastDamage = damage;

            defender.TakeDamage(damage);
            if (attacker.lifeSteal > 0f)
            {
                attacker.Heal(damage * attacker.lifeSteal);
            }

            battleLog = isCrit
                ? $"{attacker.displayName} 暴击造成 {damage:0} 伤害"
                : $"{attacker.displayName} 造成 {damage:0} 伤害";
        }
    }
}
