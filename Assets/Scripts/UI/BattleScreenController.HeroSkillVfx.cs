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
        }

        // Fixed capacity keeps fast builds bounded while previous projectiles reach their target.
        private readonly HeroSkillEffect[] heroSkillEffects = new HeroSkillEffect[3];
        private int nextHeroSkillEffect;
        private Texture2D heroPalmTexture;
        private const float HeroSkillEffectLifetime = 0.68f;
        public int ActiveHeroSkillVfxCount
        {
            get
            {
                int count = 0;
                foreach (var effect in heroSkillEffects)
                    if (effect.active && Time.unscaledTime - effect.startedAt < HeroSkillEffectLifetime) count++;
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
                startedAt = Time.unscaledTime + poseDuration * 0.22f
            };
            nextHeroSkillEffect = (nextHeroSkillEffect + 1) % heroSkillEffects.Length;
        }

        private void DrawHeroSkillVfx(Rect player, Rect enemy)
        {
            foreach (var effect in heroSkillEffects)
            {
                float age = Time.unscaledTime - effect.startedAt;
                if (!effect.active || age < 0f || age >= HeroSkillEffectLifetime) continue;
                float p = age / HeroSkillEffectLifetime;
                float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.60f, 1f, p));
                float size = Mathf.Clamp(player.width * 0.75f, 108f, 210f);
                Vector2 from = new Vector2(player.center.x + player.width * 0.16f, player.y + player.height * 0.61f);
                Vector2 target = new Vector2(enemy.center.x, enemy.y + enemy.height * 0.60f);
                bool missed = (effect.cues & BattleVfxCue.Dodge) != 0;
                if (missed) target.y -= size * 0.45f;
                Vector2 center = Vector2.Lerp(from, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p / 0.64f)));
                Color cyan = new Color(0.48f, 0.91f, 1f, fade);
                Color gold = new Color(1f, 0.79f, 0.37f, fade);

                if (effect.form == HeroAttackForm.SwordQi)
                {
                    // A tall traveling crescent and two shorter wake arcs make direction unmistakable.
                    DrawHeroArc(center, size * 0.28f, size * 0.48f, -76f, 76f, 9f, cyan);
                    DrawHeroArc(center + Vector2.left * 6f, size * 0.28f, size * 0.46f, -68f, 68f, 3f,
                        new Color(0.94f, 1f, 1f, fade));
                    DrawHeroArc(center + Vector2.left * size * 0.18f, size * 0.24f, size * 0.36f, -62f, 62f, 4f,
                        new Color(0.40f, 0.78f, 0.92f, fade * 0.48f));
                    if (!missed && p > 0.56f)
                    {
                        float impact = (p - 0.56f) / 0.44f;
                        DrawSlashTrail(enemy, impact, cyan, -35f, 1.08f);
                        if ((effect.cues & BattleVfxCue.SwiftCombo) != 0)
                            DrawSlashTrail(enemy, impact, gold, 32f, 0.92f);
                    }
                }
                else if (effect.form == HeroAttackForm.VenomPalm)
                {
                    bool venom = (effect.cues & BattleVfxCue.PoisonApplied) != 0;
                    Color tint = venom ? new Color(0.48f, 0.90f, 0.58f, fade) : gold;
                    float palmSize = size * Mathf.Lerp(0.54f, 0.86f, Mathf.Clamp01(p / 0.64f));
                    // Reuse the existing transparent palm painting, with expanding rings as the wake.
                    DrawHeroArc(center, palmSize * 0.46f, palmSize * 0.52f, 0f, 360f, 4f,
                        new Color(tint.r, tint.g, tint.b, fade * 0.72f));
                    if (heroPalmTexture != null)
                    {
                        Color previous = GUI.color;
                        GUI.color = new Color(1f, 1f, 1f, fade * 0.90f);
                        GUI.DrawTexture(new Rect(center.x-palmSize*.5f, center.y-palmSize*.5f, palmSize,palmSize),heroPalmTexture);
                        GUI.color = previous;
                    }
                    if (!missed && p > 0.48f)
                    {
                        float impact = (p - 0.48f) / 0.52f;
                        DrawHeroArc(target, size * (0.30f + impact * 0.42f), size * (0.28f + impact * 0.28f),
                            0f, 360f, 5f * (1f-impact)+1f, new Color(tint.r,tint.g,tint.b,fade*.70f));
                        DrawBurst(enemy, venom ? poisonEffectFrames : impactEffectFrames, impact * 0.50f, 0.52f,
                            Color.white, venom ? 1.30f : 1.0f, 0.05f);
                        if (!venom) DrawRadialShards(enemy, impact, gold);
                    }
                }
                else if (effect.form == HeroAttackForm.BloodCleave)
                {
                    bool opening = (effect.cues & BattleVfxCue.OpeningStrike) != 0;
                    Color edge = opening ? gold : new Color(0.91f, 0.24f, 0.16f, fade);
                    Vector2 ground = new Vector2(target.x, enemy.y + enemy.height * (battleManager.IsBossBattle ? .875f : 1f));
                    // A near-vertical cut, then a wide low shockwave; unlike sword qi it does not float.
                    if (p < 0.70f)
                    {
                        float cut = Mathf.Clamp01(p / 0.70f);
                        DrawHeroArc(ground + new Vector2(-size*.20f,-size*.38f),size*.52f,size*.72f,
                            -115f, Mathf.Lerp(-65f,72f,cut), 11f, edge);
                        DrawHeroArc(ground + new Vector2(-size*.20f,-size*.38f),size*.50f,size*.70f,
                            -110f, Mathf.Lerp(-65f,70f,cut), 3f,new Color(1f,.91f,.68f,fade));
                    }
                    if (!missed && p > 0.34f)
                    {
                        float impact = (p-.34f)/.66f;
                        DrawHeroArc(ground, size*(.18f+impact*.66f),size*(.05f+impact*.13f),0f,360f,5f,edge);
                        for(int i=0;i<5;i++)
                        {
                            float angle = (205f+i*32f)*Mathf.Deg2Rad;
                            Vector2 tip=ground+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)) * size*(.12f+impact*.38f);
                            DrawHeroLine(ground,tip,3f, new Color(edge.r,edge.g,edge.b,fade*.72f));
                        }
                        DrawBurst(enemy,impactEffectFrames,impact*.42f,.44f,new Color(1f,.78f,.49f,.85f),.90f,.22f);
                    }
                }
            }
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
