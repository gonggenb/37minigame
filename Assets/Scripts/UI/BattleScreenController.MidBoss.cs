using UnityEngine;
using WuxiaRoguelite.Battle;

namespace WuxiaRoguelite.UI
{
    public partial class BattleScreenController
    {
        private Sprite[] SelectMidBossActionFrames(EnemyVisualProfile profile, Sprite[] fallback)
        {
            if (profile == null) return fallback;
            Sprite[] frames = battleManager.CurrentMidBossSkill switch
            {
                BossSkillId.DoubleCleave => profile.doubleCleaveFrames,
                BossSkillId.IronGuard => profile.ironGuardFrames,
                _ => profile.skillFrames
            };
            return frames != null && frames.Length > 0 ? frames : fallback;
        }

        private float GetMidBossActionProgress()
        {
            float elapsed = battleManager.MidBossSkillElapsed;
            if (battleManager.CurrentMidBossSkill == BossSkillId.DoubleCleave)
            {
                // Frames 2 and 5 contain the hit poses. Keep them on damage timestamps.
                int frame = elapsed < 0.15f ? 0 : elapsed < 0.35f ? 1
                    : elapsed < 0.48f ? 2 : elapsed < 0.60f ? 3 : elapsed < 0.80f ? 4
                    : elapsed < 0.94f ? 5 : elapsed < 1.08f ? 6 : 7;
                return (frame + 0.1f) / 8f;
            }
            return Mathf.Clamp01(elapsed / Mathf.Max(0.01f, battleManager.MidBossSkillDuration));
        }

        private void DrawMidBossImpact(Rect playerRect, Rect enemyRect)
        {
            if (!battleManager.IsMidBossBattle || !battleManager.IsBattleActive) return;
            float age = battleManager.MidBossImpactAge;
            bool doubleCleave = battleManager.MidBossImpactSkill == BossSkillId.DoubleCleave;
            Sprite[] frames = doubleCleave ? doubleCleaveEffectFrames : mountainBreakerEffectFrames;
            float duration = 0.30f;
            if (age < 0f || age >= duration || frames == null || frames.Length == 0) return;
            int index = doubleCleave
                ? battleManager.MidBossImpactIndex * 3 + Mathf.Min(2, Mathf.FloorToInt(age / duration * 3f))
                : Mathf.Min(frames.Length - 1, Mathf.FloorToInt(age / duration * frames.Length));
            if (index >= frames.Length) return;
            float size = Mathf.Max(playerRect.width, enemyRect.width) * (doubleCleave ? 1.12f : 1.20f);
            bool flip = ShouldFlipDirectionalEffect(enemyRect, playerRect, sourceFacesLeft: doubleCleave);
            Vector2 center;
            if (doubleCleave)
            {
                center = new Vector2(Mathf.Lerp(enemyRect.center.x, playerRect.center.x, .60f),
                    playerRect.y + playerRect.height * .57f);
            }
            else
            {
                // The authored contact point moves inside the six cells. Pin that point
                // (not the transparent cell center) to the floor in front of the player.
                Vector2 contact = MountainBreakerContact(index);
                if (flip) contact.x = 1f - contact.x;
                Vector2 groundHit = new Vector2(playerRect.center.x + playerRect.width * .18f,
                    playerRect.y + playerRect.height * .875f);
                center = groundHit - (contact - Vector2.one * .5f) * size;
            }
            DrawEffectSprite(new Rect(center.x - size * .5f, center.y - size * .5f, size, size),
                frames[index], new Color(1f, 1f, 1f, 1f - age / duration * .55f), flip);
        }

        private static Vector2 MountainBreakerContact(int frame)
        {
            return frame switch
            {
                0 => new Vector2(128f, 138f) / 256f,
                1 => new Vector2(128f, 154f) / 256f,
                2 => new Vector2(171f, 161f) / 256f,
                3 => new Vector2(207f, 173f) / 256f,
                4 => new Vector2(182f, 166f) / 256f,
                _ => new Vector2(154f, 152f) / 256f
            };
        }

        private void DrawMidBossWard(Rect enemyRect)
        {
            if (!battleManager.IsMidBossBattle || !battleManager.IsBattleActive ||
                ironGuardEffectFrames == null || ironGuardEffectFrames.Length < 6) return;
            int frame;
            float alpha;
            if (battleManager.BossWard > 0f)
            {
                float age = battleManager.MidBossWardElapsed;
                frame = age < 0.48f ? Mathf.Min(2, Mathf.FloorToInt(age / 0.16f)) : 3;
                alpha = age < 0.48f ? 0.72f : 0.32f + Mathf.Sin(age * 3f) * 0.04f;
            }
            else if (battleManager.MidBossWardBreakAge < 0.32f)
            {
                frame = battleManager.MidBossWardBreakAge < 0.16f ? 4 : 5;
                alpha = 0.68f * (1f - battleManager.MidBossWardBreakAge / 0.32f);
            }
            else return;
            float size = enemyRect.width * 0.74f;
            // This boss uses legacy bottom-edge feet, unlike the fox and hero strips.
            float foot = enemyRect.yMax;
            DrawEffectSprite(new Rect(enemyRect.center.x - size * 0.5f, foot - size * 0.94f, size, size),
                ironGuardEffectFrames[frame], new Color(1f, 1f, 1f, alpha));
        }
    }
}
