using UnityEngine;
using WuxiaRoguelite.Battle;
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
        // Cosmetic randomness must not advance Unity's gameplay RNG (crits, loot, routes).
        private readonly System.Random heroBasicRandom = new System.Random();
        public int CurrentHeroBasicVariant { get; private set; } = -1;
        public HeroAttackForm CurrentHeroAttackForm { get; private set; }
        public float HeroAttackProgress => Mathf.Clamp01((Time.unscaledTime - heroAttackStartedAt) / heroAttackDuration);
        public bool IsHeroAttackPlaying => battleManager != null && battleManager.IsBattleActive && HeroAttackProgress < 1f;

        private void Awake()
        {
            // BootMenu reuses the dialogue view without loading combat animation strips.
            if (gameObject.scene.name == GameFlow.LevelSequence.MenuSceneName) return;
            HeroExternalVfxArt.Preload();
            Sprite[] basic = HeroAttackArt.Frames(HeroAttackForm.Basic);
            if (basic.Length == 8)
            {
                playerAttackFrames = basic;
                playerIdleFrames = HeroAttackArt.Idle;
            }
            // Load once before combat, not during the first random swing.
            HeroAttackArt.BasicFrames(0);
            HeroUltimateArt.Texture(0);
            HeroUltimateArt.Frames(0);
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
                CurrentHeroBasicVariant = -1;
                heroSelfCues = BattleVfxCue.None;
                ClearHeroSkillVfx();
            }
            if (!battleManager.IsBattleActive || observedHeroAttack == battleManager.PlayerAttackVisualSequence) return;
            observedHeroAttack = battleManager.PlayerAttackVisualSequence;
            CurrentHeroAttackForm = HeroAttackArt.Select(battleManager.PlayerAttackVisualCues);
            heroSelfCues = battleManager.PlayerAttackVisualCues;
            CurrentHeroBasicVariant = CurrentHeroAttackForm == HeroAttackForm.Basic
                ? heroBasicRandom.Next(HeroAttackArt.BasicVariantCount) : -1;
            // Match faster builds without delaying combat or queueing stale poses.
            float interval = battleManager.PlayerAttackCooldownDuration / Mathf.Max(0.01f, battleManager.BattleSpeedMultiplier);
            heroAttackDuration = Mathf.Clamp(interval * 0.90f, 0.14f, HeroAttackArt.Duration(CurrentHeroAttackForm));
            heroAttackStartedAt = Time.unscaledTime;
            if (!IsUltimatePosePlaying) QueueHeroSkillVfx(battleManager.PlayerAttackVisualCues, heroAttackDuration);
        }

        private Sprite[] CurrentHeroAttackFrames()
        {
            if (IsUltimatePosePlaying) return HeroUltimateArt.Frames(currentUltimateIndex);
            Sprite[] frames = CurrentHeroAttackForm == HeroAttackForm.Basic && CurrentHeroBasicVariant >= 0
                ? HeroAttackArt.BasicFrames(CurrentHeroBasicVariant)
                : HeroAttackArt.Frames(CurrentHeroAttackForm);
            return frames.Length == 8 ? frames : playerAttackFrames;
        }

        private float CurrentHeroSpriteScale => IsUltimatePosePlaying ? HeroUltimateArt.DisplayScale(currentUltimateIndex) :
            CurrentHeroAttackForm == HeroAttackForm.Basic &&
            CurrentHeroBasicVariant >= 0 && HeroAttackArt.BasicFrames(CurrentHeroBasicVariant).Length == 8
                ? HeroAttackArt.BasicDisplayScale(CurrentHeroBasicVariant) : 1f;
    }
}
