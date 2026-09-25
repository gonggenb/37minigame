using UnityEngine;

namespace WuxiaRoguelite.Battle
{
    public partial class BattleManager
    {
        public BossTalent CurrentBossTalent => IsBossEncounter && currentEnemy != null
            ? currentEnemy.bossTalent : BossTalent.None;
        private float EffectivePlayerDefense => Mathf.Max(0f, playerStats.runtimeStats.defense) *
            BossTalentCatalog.PlayerDefenseEfficiency * BossTalentCatalog.DefenseMultiplier(CurrentBossTalent);
        private float PlayerDodgeChance(float bonus = 0f) => Mathf.Clamp01(
            playerStats.runtimeStats.dodgeChance + bonus) * BossTalentCatalog.DodgeMultiplier(CurrentBossTalent);
        private void HealPlayerInBattle(float amount) => playerStats.runtimeStats.Heal(
            amount * BossTalentCatalog.HealingMultiplier(CurrentBossTalent));
    }
}
