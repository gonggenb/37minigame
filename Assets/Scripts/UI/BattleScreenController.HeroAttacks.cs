using UnityEngine;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.Visual;

namespace WuxiaRoguelite.UI
{
    public partial class BattleScreenController
    {
        private int observedHeroAttack;
        private CombatantStats heroAnimationEnemy;
        private float heroAttackStartedAt = -100f;
        private float heroAttackDuration = 0.48f;
        public HeroAttackForm CurrentHeroAttackForm { get; private set; }
        public float HeroAttackProgress => Mathf.Clamp01((Time.unscaledTime - heroAttackStartedAt) / heroAttackDuration);
        public bool IsHeroAttackPlaying => battleManager != null && battleManager.IsBattleActive && HeroAttackProgress < 1f;

        private void Awake()
        {
            heroPalmTexture = Resources.Load<Texture2D>("Icons/art_venom_palm");
            Sprite[] basic = HeroAttackArt.Frames(HeroAttackForm.Basic);
            if (basic.Length == 8)
            {
                playerAttackFrames = basic;
                playerIdleFrames = HeroAttackArt.Idle;
            }
        }

        private void TrackHeroAttack()
        {
            if (battleManager == null) return;
            if (!battleManager.IsBattleActive || !ReferenceEquals(heroAnimationEnemy, battleManager.currentEnemy) ||
                battleManager.PlayerAttackVisualSequence < observedHeroAttack)
            {
                heroAnimationEnemy = battleManager.currentEnemy;
                observedHeroAttack = 0;
                heroAttackStartedAt = -100f;
                ClearHeroSkillVfx();
            }
            if (!battleManager.IsBattleActive || observedHeroAttack == battleManager.PlayerAttackVisualSequence) return;
            observedHeroAttack = battleManager.PlayerAttackVisualSequence;
            CurrentHeroAttackForm = HeroAttackArt.Select(battleManager.PlayerAttackVisualCues);
            // Match faster builds without delaying combat or queueing stale poses.
            float interval = battleManager.PlayerAttackCooldownDuration / Mathf.Max(0.01f, battleManager.BattleSpeedMultiplier);
            heroAttackDuration = Mathf.Clamp(interval * 0.90f, 0.14f, HeroAttackArt.Duration(CurrentHeroAttackForm));
            heroAttackStartedAt = Time.unscaledTime;
            QueueHeroSkillVfx(battleManager.PlayerAttackVisualCues, heroAttackDuration);
        }

        private Sprite[] CurrentHeroAttackFrames()
        {
            Sprite[] frames = HeroAttackArt.Frames(CurrentHeroAttackForm);
            return frames.Length == 8 ? frames : playerAttackFrames;
        }
    }
}
