using UnityEngine;
using WuxiaRoguelite.Battle;

namespace WuxiaRoguelite.UI
{
    public partial class BattleScreenController
    {
        private Rect GetFinalBossPoseRect(Rect rect, EnemyVisualProfile profile)
        {
            if (profile == null) return rect;
            float scale = battleManager.CurrentFinalBossAction switch
            {
                BossSkillId.FoxfireBarrage => profile.foxfireVisualScale,
                BossSkillId.DemonArmor => profile.demonArmorVisualScale,
                BossSkillId.BloodFrenzy => profile.bloodFrenzyVisualScale,
                _ => 1f
            };
            scale = Mathf.Max(0.1f, scale);
            // Existing atlas's visible toes are offset from its Sprite pivot. Match them across clips.
            float footX = profile.flipHorizontally ? 1f - 147.5f / 256f : 147.5f / 256f;
            return new Rect(rect.x + rect.width * footX * (1f-scale),
                rect.y + rect.height * 0.875f * (1f-scale), rect.width*scale, rect.height*scale);
        }

        private Sprite[] SelectFinalBossActionFrames(EnemyVisualProfile profile, Sprite[] fallback)
        {
            if (profile == null) return fallback;
            Sprite[] frames = battleManager.CurrentFinalBossAction switch
            {
                BossSkillId.FoxfireBarrage => profile.foxfireFrames,
                BossSkillId.DemonArmor => profile.demonArmorFrames,
                BossSkillId.BloodFrenzy => profile.bloodFrenzyFrames,
                _ => fallback
            };
            return frames != null && frames.Length == 8 ? frames : fallback;
        }

        private float GetFinalBossActionProgress()
        {
            float t = battleManager.FinalBossActionElapsed;
            if (battleManager.CurrentFinalBossAction == BossSkillId.FoxfireBarrage)
            {
                // Frames 3/4/5 launch the three projectiles; impacts follow 0.18s later.
                int frame = t < 0.09f ? 0 : t < 0.18f ? 1 : t < 0.27f ? 2
                    : t < 0.47f ? 3 : t < 0.67f ? 4 : t < 0.90f ? 5 : t < 1.03f ? 6 : 7;
                return (frame + 0.1f) / 8f;
            }
            return Mathf.Clamp01(t / BossV2Tuning.PhaseActionDuration);
        }

        private void DrawFinalBossFoxfire(Rect playerRect, Rect enemyRect)
        {
            if (!battleManager.IsBossBattle || !battleManager.IsBattleActive ||
                foxfireEffectFrames == null || foxfireEffectFrames.Length != 6) return;
            bool flip = ShouldFlipDirectionalEffect(enemyRect, playerRect, sourceFacesLeft: false);
            if (battleManager.CurrentFinalBossAction == BossSkillId.FoxfireBarrage && battleManager.IsFinalBossActionActive)
            {
                float elapsed = battleManager.FinalBossActionElapsed;
                float size = playerRect.width * 0.72f;
                Vector2 from = new Vector2(enemyRect.center.x - enemyRect.width * 0.12f,
                    enemyRect.y + enemyRect.height * 0.44f);
                Vector2 to = new Vector2(playerRect.center.x, playerRect.y + playerRect.height * 0.53f);
                for (int i = 0; i < BossV2Tuning.FoxfireHitCount; i++)
                {
                    float impact = BossV2Tuning.FoxfireFirstImpact + i * BossV2Tuning.FoxfireImpactInterval;
                    float flight = (elapsed - (impact - BossV2Tuning.FoxfireFlightDuration)) /
                        BossV2Tuning.FoxfireFlightDuration;
                    if (flight < -0.5f || flight >= 1f) continue;
                    Vector2 center = Vector2.Lerp(from, to, Mathf.Clamp01(flight));
                    center.y += (i - 1) * size * 0.10f * (1f - Mathf.Clamp01(flight));
                    int frame = flight < 0f ? 0 : flight < 0.5f ? 1 : 2;
                    DrawEffectSprite(new Rect(center.x-size*0.5f, center.y-size*0.5f, size, size),
                        foxfireEffectFrames[frame], Color.white, flip);
                }
            }
            float age = battleManager.FoxfireImpactAge;
            if (age < 0f || age >= 0.19f) return;
            // A dodged projectile disperses beside the player, never shows a hit on the body.
            float burstSize = playerRect.width * 0.70f;
            float offset = battleManager.FoxfireImpactDodged ? -playerRect.width * 0.40f : 0f;
            Vector2 hit = new Vector2(playerRect.center.x + offset, playerRect.y + playerRect.height * 0.55f);
            int burstFrame = battleManager.FoxfireImpactDodged ? 5 : 3 + Mathf.Min(2, (int)(age / 0.19f * 3f));
            DrawEffectSprite(new Rect(hit.x-burstSize*0.5f, hit.y-burstSize*0.5f, burstSize, burstSize),
                foxfireEffectFrames[burstFrame], new Color(1f,1f,1f,1f-age/0.19f*0.6f), flip);
        }

        private void DrawFinalBossStateEffects(Rect enemyRect)
        {
            if (!battleManager.IsBossBattle || !battleManager.IsBattleActive) return;
            float foot = enemyRect.y + enemyRect.height * 0.875f;
            if (battleManager.CurrentBossPhase == BossBattlePhase.BloodFrenzy &&
                bloodFrenzyEffectFrames != null && bloodFrenzyEffectFrames.Length == 6)
            {
                bool casting = battleManager.CurrentFinalBossAction == BossSkillId.BloodFrenzy;
                float t = casting ? battleManager.FinalBossActionElapsed : battleManager.FinalBossFrenzyAge;
                int frame = casting ? Mathf.Min(5, (int)(t / BossV2Tuning.PhaseActionDuration * 6f)) : 5;
                float alpha = casting ? 0.72f : 0.26f + Mathf.Sin(t * 3f) * 0.035f;
                float size = enemyRect.width * 1.22f;
                DrawEffectSprite(new Rect(enemyRect.center.x-size*0.5f, foot-size*0.84f, size,size),
                    bloodFrenzyEffectFrames[frame], new Color(1f,1f,1f,alpha));
            }
            if (demonArmorEffectFrames == null || demonArmorEffectFrames.Length != 6) return;
            int armorFrame;
            float armorAlpha;
            if (battleManager.BossWard > 0f)
            {
                float t = battleManager.FinalBossWardAge;
                armorFrame = t < 0.48f ? Mathf.Min(2, (int)(t / 0.16f)) : 3;
                armorAlpha = t < 0.48f ? 0.85f : 0.44f + Mathf.Sin(t * 3f) * 0.04f;
            }
            else if (battleManager.FinalBossWardBreakAge < 0.32f)
            {
                float t = battleManager.FinalBossWardBreakAge;
                armorFrame = t < 0.16f ? 4 : 5;
                armorAlpha = 0.85f * (1f-t/0.32f);
            }
            else return;
            float armorSize = enemyRect.width * 1.22f;
            DrawEffectSprite(new Rect(enemyRect.center.x-armorSize*0.5f, foot-armorSize*0.86f, armorSize,armorSize),
                demonArmorEffectFrames[armorFrame], new Color(1f,1f,1f,armorAlpha));
        }
    }
}
