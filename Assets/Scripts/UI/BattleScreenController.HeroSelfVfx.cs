using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Visual;

namespace WuxiaRoguelite.UI
{
    public partial class BattleScreenController
    {
        // The actor-attached layer belongs only to the current pose. Unlike traveling
        // projectiles it must never stack old casts on the hero during fast attacks.
        private BattleVfxCue heroSelfCues;
        public BattleVfxCue HeroSelfVfxCues => heroSelfCues;
        public int ActiveHeroSelfVfxCount => !IsUltimatePosePlaying && IsHeroAttackPlaying &&
            CurrentHeroAttackForm != HeroAttackForm.Basic &&
            HeroAttackProgress >= 0.04f && HeroAttackProgress < 0.94f ? 1 : 0;

        private void DrawHeroSelfVfx(Rect actor, bool foreground)
        {
            if (ActiveHeroSelfVfxCount == 0 || Event.current.type != EventType.Repaint) return;
            float p = HeroAttackProgress;
            float strength = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.04f, 0.22f, p)) *
                (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 0.94f, p)));
            Color edge = HeroSelfColor();
            edge.a = strength;
            Vector2 waist = new Vector2(actor.center.x, actor.y + actor.height * 0.67f);
            Vector2 foot = new Vector2(actor.center.x, actor.y + actor.height * 0.875f);
            float w = actor.width;
            switch (CurrentHeroAttackForm)
            {
                case HeroAttackForm.SwordQi:
                    DrawSwordSelfVfx(actor, waist, w, p, edge, foreground);
                    break;
                case HeroAttackForm.VenomPalm:
                    DrawPalmSelfVfx(actor, waist, w, p, edge, foreground);
                    break;
                case HeroAttackForm.BloodCleave:
                    DrawCleaveSelfVfx(actor, waist, foot, w, p, edge, foreground);
                    break;
            }
        }

        private Color HeroSelfColor()
        {
            if (CurrentHeroAttackForm == HeroAttackForm.SwordQi) return new Color(.57f, .86f, .91f);
            if (CurrentHeroAttackForm == HeroAttackForm.VenomPalm)
                return (heroSelfCues & BattleVfxCue.PoisonApplied) != 0
                    ? new Color(.43f, .79f, .53f) : new Color(.93f, .73f, .34f);
            return (heroSelfCues & BattleVfxCue.OpeningStrike) != 0
                ? new Color(.98f, .78f, .40f) : new Color(.84f, .27f, .20f);
        }

        private void DrawSwordSelfVfx(Rect actor, Vector2 waist, float w, float p, Color edge, bool front)
        {
            if (!front)
            {
                // Reuse the actual earlier animation poses for readable, clothed afterimages.
                Sprite[] frames = HeroAttackArt.Frames(HeroAttackForm.SwordQi);
                int count = (heroSelfCues & BattleVfxCue.SwiftCombo) != 0 ? 2 : 1;
                int frame = Mathf.Min(7, Mathf.FloorToInt(p * 8f));
                Color previous = GUI.color;
                for (int i = count; i >= 1; i--)
                {
                    GUI.color = new Color(edge.r, edge.g, edge.b, edge.a * .32f / i);
                    if (frames.Length == 8)
                        DrawSprite(OffsetRect(actor, -w * .075f * i, 0), frames[Mathf.Max(0, frame-i)], false);
                }
                GUI.color = previous;
                DrawHeroArc(waist, w*.29f, w*.14f, 165f+p*60f, 310f+p*60f, 3f,
                    WithAlpha(edge, edge.a*.55f));
                return;
            }
            // One curved ribbon hugs the body and blade; no duplicate flying projectile.
            if (p > .24f)
            {
                float sweep = Mathf.Clamp01((p-.24f)/.44f);
                DrawHeroArc(waist - Vector2.up*w*.03f, w*.35f, w*.27f,
                    -105f+sweep*35f, -45f+sweep*135f, 5f, WithAlpha(edge, edge.a*.86f));
                DrawHeroArc(waist - Vector2.up*w*.03f, w*.34f, w*.26f,
                    -100f+sweep*35f, -45f+sweep*132f, 2f, new Color(.95f,.97f,.84f,edge.a));
            }
            DrawSelfMotes(waist, w, p, edge, 5, false);
        }

        private void DrawPalmSelfVfx(Rect actor, Vector2 waist, float w, float p, Color edge, bool front)
        {
            if (!front)
            {
                DrawHeroArc(waist, w*.20f, w*.11f, 155f+p*70f, 310f+p*70f, 3f,
                    WithAlpha(edge, edge.a*.45f));
                DrawSelfMotes(waist, w*.78f, p, WithAlpha(edge, edge.a*.7f), 6, true);
                return;
            }
            // Follow the pull-in, palm extension and return of the existing eight poses.
            float reach = Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.30f,.50f,p)) *
                (1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.72f,.94f,p)));
            Vector2 palm = new Vector2(actor.x+w*Mathf.Lerp(.59f,.80f,reach),
                actor.y+actor.height*Mathf.Lerp(.57f,.54f,reach));
            float radius = w * Mathf.Lerp(.025f,.070f,reach);
            DrawHeroArc(palm, radius*1.20f, radius, -110f+p*170f, 155f+p*170f, 3f,
                WithAlpha(edge,edge.a*.85f));
            DrawHeroArc(palm, radius*.72f, radius*.68f, -155f-p*150f, 80f-p*150f, 2f,
                new Color(.97f,.94f,.73f,edge.a*.95f));
            Vector2 elbow = new Vector2(actor.center.x+w*.04f, actor.y+actor.height*.60f);
            DrawHeroLine(elbow, palm, 6f, WithAlpha(edge,edge.a*.22f));
            DrawHeroLine(elbow, palm, 2f, WithAlpha(edge,edge.a*.72f));
            for (int i=0;i<3;i++)
            {
                float angle=(p*330f+i*120f)*Mathf.Deg2Rad;
                Vector2 point=palm+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius*1.4f;
                FillRect(new Rect(point.x-1.5f,point.y-1.5f,3f,3f),edge);
            }
        }

        private void DrawCleaveSelfVfx(Rect actor, Vector2 waist, Vector2 foot, float w, float p, Color edge, bool front)
        {
            if (!front)
            {
                float charge = 1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.40f,.82f,p));
                DrawHeroArc(foot, w*(.19f+p*.12f), w*.055f, 0f,360f,3f,
                    WithAlpha(edge,edge.a*.64f));
                for(int i=0;i<5;i++)
                {
                    float x=foot.x+(i-2)*w*.085f;
                    float rise=w*(.15f+.11f*Mathf.Sin((i+1)*1.7f)+p*.18f);
                    DrawHeroLine(new Vector2(x,foot.y-w*.03f),new Vector2(x+w*.035f,foot.y-rise),
                        2f,WithAlpha(edge,edge.a*charge*.58f));
                }
                return;
            }
            if(p>.30f && p<.76f)
            {
                float cut=Mathf.Clamp01((p-.30f)/.38f);
                Vector2 center=waist-Vector2.up*w*.13f;
                float end=Mathf.Lerp(-82f,70f,cut);
                DrawHeroArc(center,w*.33f,w*.45f,end-66f,end,8f,WithAlpha(edge,edge.a*.83f));
                DrawHeroArc(center,w*.32f,w*.44f,end-58f,end,2f,new Color(1f,.91f,.69f,edge.a));
            }
            if(p>.57f)
            {
                float impact=Mathf.Clamp01((p-.57f)/.37f);
                DrawHeroArc(foot,w*(.12f+impact*.26f),w*(.035f+impact*.06f),5f,175f,3f,
                    WithAlpha(edge,edge.a*.70f));
                DrawSelfMotes(foot-Vector2.up*w*.05f,w,impact,edge,6,true);
            }
        }

        private static Color WithAlpha(Color color, float alpha) => new Color(color.r,color.g,color.b,alpha);

        private static void DrawSelfMotes(Vector2 center, float size, float p, Color color, int count, bool rising)
        {
            // Deterministic positions: no allocations, GameObjects, or gameplay RNG.
            for(int i=0;i<count;i++)
            {
                float t=Mathf.Repeat(p+i*.173f,1f);
                float angle=(i*137.5f+p*90f)*Mathf.Deg2Rad;
                Vector2 point=center+new Vector2(Mathf.Cos(angle)*size*.25f,
                    rising ? -size*(.07f+t*.30f) : Mathf.Sin(angle)*size*.17f);
                float alpha=color.a*Mathf.Sin(t*Mathf.PI)*.65f;
                FillRect(new Rect(Mathf.Round(point.x),Mathf.Round(point.y),3f,3f),WithAlpha(color,alpha));
            }
        }
    }
}
