using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Visual;

namespace WuxiaRoguelite.UI
{
    public partial class BattleScreenController
    {
        private struct HeroSkillEffect
        {
            public bool active;
            public HeroAttackForm form;
            public BattleVfxCue cues;
            public float startedAt;
            public float duration;
            public Vector2 viewport;
            public bool hasOrigin;
            public Vector2 origin;
        }

        // Fixed capacity keeps fast builds bounded while previous projectiles reach their target.
        private readonly HeroSkillEffect[] heroSkillEffects = new HeroSkillEffect[3];
        private int nextHeroSkillEffect;
        public int ActiveHeroSkillVfxCount
        {
            get
            {
                int count = 0;
                foreach (var effect in heroSkillEffects)
                    if (effect.active && Time.unscaledTime - effect.startedAt < effect.duration) count++;
                return count;
            }
        }

        private void ClearHeroSkillVfx()
        {
            for (int i = 0; i < heroSkillEffects.Length; i++) heroSkillEffects[i].active = false;
            nextHeroSkillEffect = 0;
        }

        private void QueueHeroSkillVfx(BattleVfxCue cues, float poseDuration)
        {
            if (CurrentHeroAttackForm == HeroAttackForm.Basic) return;
            heroSkillEffects[nextHeroSkillEffect] = new HeroSkillEffect
            {
                active = true, form = CurrentHeroAttackForm, cues = cues,
                // Release with the swing and finish before its recovery ends, even at high speed.
                startedAt = Time.unscaledTime + poseDuration * 0.22f,
                duration = poseDuration * 0.72f,
                viewport = new Vector2(Screen.width, Screen.height)
            };
            nextHeroSkillEffect = (nextHeroSkillEffect + 1) % heroSkillEffects.Length;
        }

        private void DrawHeroSkillVfx(Rect player, Rect enemy)
        {
            if (Event.current.type != EventType.Repaint) return;
            for (int index = 0; index < heroSkillEffects.Length; index++)
            {
                var effect = heroSkillEffects[index];
                float age = Time.unscaledTime - effect.startedAt;
                if (!effect.active || age < 0f || age >= effect.duration) continue;
                // A cached launch point belongs to one layout; don't carry it across rotation.
                if (effect.viewport != new Vector2(Screen.width, Screen.height)) continue;
                if (!effect.hasOrigin)
                {
                    effect.origin = new Vector2(player.center.x + player.width * 0.16f,
                        player.y + player.height * 0.61f);
                    effect.hasOrigin = true;
                    heroSkillEffects[index] = effect;
                }
                float p = age / effect.duration;
                float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.60f, 1f, p));
                float size = Mathf.Clamp(player.width * 0.56f, 76f, 150f);
                Vector2 target = new Vector2(enemy.center.x, enemy.y + enemy.height * 0.60f);
                bool missed = (effect.cues & BattleVfxCue.Dodge) != 0;
                if (missed) target.y -= size * 0.45f;
                const float arrival = 0.48f;
                Vector2 center = Vector2.Lerp(effect.origin, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p / arrival)));
                Color cyan = new Color(0.48f, 0.91f, 1f, fade);
                Color gold = new Color(1f, 0.79f, 0.37f, fade);

                if (effect.form == HeroAttackForm.SwordQi)
                {
                    DrawHeroExternalSprite(effect.form, center, size, p, WithAlpha(Color.white, fade));
                    if (!missed && p > arrival)
                    {
                        float impact = (p - arrival) / (1f - arrival);
                        DrawSlashTrail(enemy, impact, cyan, -35f, 0.52f);
                        if ((effect.cues & BattleVfxCue.SwiftCombo) != 0)
                            DrawSlashTrail(enemy, impact, gold, 32f, 0.44f);
                    }
                }
                else if (effect.form == HeroAttackForm.VenomPalm)
                {
                    bool venom = (effect.cues & BattleVfxCue.PoisonApplied) != 0;
                    // B1 replaces the old literal palm icon. Warm the same qi art for armor break.
                    Color palette = venom ? Color.white : new Color(1.35f, .70f, .38f);
                    DrawHeroExternalSprite(effect.form, center, size, p, WithAlpha(palette, fade));
                    if (!missed && p > arrival)
                    {
                        float impact = (p - arrival) / (1f - arrival);
                        DrawBurst(enemy, venom ? poisonEffectFrames : impactEffectFrames,
                            impact * .50f, .52f, WithAlpha(palette, fade * .75f), .46f, .05f);
                    }
                }
                else if (effect.form == HeroAttackForm.BloodCleave)
                {
                    bool opening = (effect.cues & BattleVfxCue.OpeningStrike) != 0;
                    Color palette = opening ? new Color(1f, 1.25f, .90f) : new Color(1f, .72f, .65f);
                    // C1 is authored diagonally down-right. Keep its pivot stable on the opponent.
                    DrawHeroExternalSprite(effect.form, target, size * 1.15f, p, WithAlpha(palette, fade));
                    if (!missed && p > arrival)
                    {
                        float impact = (p - arrival) / (1f - arrival);
                        DrawBurst(enemy, impactEffectFrames, impact * .42f, .44f,
                            new Color(1f, .78f, .49f, fade * .65f), .40f, .15f);
                    }
                }
            }
        }

        private static void DrawHeroExternalSprite(HeroAttackForm form, Vector2 center, float size,
            float progress, Color tint)
        {
            Sprite frame = HeroExternalVfxArt.Frame(form, progress);
            // The approved atlases face right; sample one frame without rotating or stretching it.
            DrawEffectSprite(new Rect(center.x - size * .5f, center.y - size * .5f, size, size), frame, tint);
        }

        private static void DrawHeroArc(Vector2 center,float rx,float ry,float start,float end,float thickness,Color color)
        {
            const int segments=20;
            Vector2 previous=center+new Vector2(Mathf.Cos(start*Mathf.Deg2Rad)*rx,Mathf.Sin(start*Mathf.Deg2Rad)*ry);
            for(int i=1;i<=segments;i++)
            {
                float angle=Mathf.Lerp(start,end,(float)i/segments)*Mathf.Deg2Rad;
                Vector2 next=center+new Vector2(Mathf.Cos(angle)*rx,Mathf.Sin(angle)*ry);
                DrawHeroLine(previous,next,thickness,color);previous=next;
            }
        }

        private static void DrawHeroLine(Vector2 from,Vector2 to,float thickness,Color color)
        {
            Vector2 delta=to-from;
            DrawRotatedRect(new Rect((from.x+to.x)*.5f-delta.magnitude*.5f,(from.y+to.y)*.5f-thickness*.5f,
                delta.magnitude+1f,thickness),color,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
    }
}
