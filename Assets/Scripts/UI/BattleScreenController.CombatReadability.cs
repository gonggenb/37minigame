using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.UI
{
    public partial class BattleScreenController
    {
        private Texture2D softDisc, guardRing;
        private CombatantStats traitFeedbackEnemy;
        private int seenBite, seenVenomTick, seenArmorBreak, seenEnemyAttempts;
        private float biteAt = -100f, venomTickAt = -100f, armorBreakAt = -100f, heavyAt = -100f;

        private float HeavyOpeningWindup => battleManager.CurrentEnemyTrait == EnemyTrait.HeavyOpening &&
            battleManager.IsBattleActive && !battleManager.currentEnemy.IsDead && battleManager.EnemyAttackAttempts == 0
                ? Mathf.Clamp01(1f - battleManager.EnemyAttackCooldownRemaining / .55f) : 0f;

        private bool UsesRaisedEnemyFoot()
        {
            switch (battleManager.currentEnemy.visualId)
            {
                case "iron_tusk_boar": case "scarlet_viper": case "strawhat_bandit":
                case "gourd_rogue_monk": case "lantern_wraith": case "moss_mushroom_imp": return true;
                default: return false;
            }
        }

        private void TrackEnemyTraitFeedback()
        {
            if (!ReferenceEquals(traitFeedbackEnemy, battleManager.currentEnemy) ||
                battleManager.EnemyAttackAttempts < seenEnemyAttempts)
            {
                traitFeedbackEnemy = battleManager.currentEnemy;
                seenBite = seenVenomTick = seenArmorBreak = seenEnemyAttempts = 0;
                biteAt = venomTickAt = armorBreakAt = heavyAt = -100f;
            }
            float now = Time.unscaledTime;
            if (battleManager.VenomBiteSequence > seenBite) biteAt = now;
            if (battleManager.VenomTickSequence > seenVenomTick) venomTickAt = now;
            if (battleManager.OpeningArmorBreakSequence > seenArmorBreak) armorBreakAt = now;
            if (battleManager.CurrentEnemyTrait == EnemyTrait.HeavyOpening && seenEnemyAttempts == 0 &&
                battleManager.EnemyAttackAttempts > 0) heavyAt = now;
            seenBite = battleManager.VenomBiteSequence;
            seenVenomTick = battleManager.VenomTickSequence;
            seenArmorBreak = battleManager.OpeningArmorBreakSequence;
            seenEnemyAttempts = battleManager.EnemyAttackAttempts;
        }

        // Small procedural gradients are cached once; they need no imported art or scene binding.
        private void EnsureCombatShapes()
        {
            if (softDisc != null) return;
            softDisc = CreateCombatShape(false);
            guardRing = CreateCombatShape(true);
        }

        private static Texture2D CreateCombatShape(bool ring)
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = ring ? "CombatGuardRing" : "CombatSoftShadow",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) / size * 2f - 1f;
                float dy = (y + .5f) / size * 2f - 1f;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = ring ? Mathf.Exp(-Mathf.Pow((radius - .80f) / .045f, 2f))
                    : Mathf.Pow(Mathf.Clamp01(1f - radius * radius), 3f);
                if (ring) alpha *= .35f + .65f * Mathf.Abs(dx);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void DrawCombatShape(Rect rect, Texture2D shape, Color color)
        {
            Color before = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, shape, ScaleMode.StretchToFill, true);
            GUI.color = before;
        }

        private void DrawGroundShadow(Rect actor, float ground)
        {
            EnsureCombatShapes();
            float width = actor.width * .66f;
            float height = Mathf.Clamp(actor.height * .105f, 12f, 32f);
            DrawCombatShape(new Rect(actor.center.x - width * .5f, ground - height * .35f,
                width, height), softDisc, new Color(.035f, .03f, .025f, .60f));
        }

        private void DrawGuardAura(Rect actor, Color color)
        {
            EnsureCombatShapes();
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3f) * .025f;
            Rect shell = new Rect(actor.center.x - actor.width * .25f * pulse,
                actor.y + actor.height * .27f, actor.width * .50f * pulse, actor.height * .64f);
            DrawCombatShape(shell, guardRing, color);
        }

        private void DrawEnemyTraitFeedback(Rect player, Rect enemy, float ground)
        {
            if (battleManager.CurrentEnemyTrait == EnemyTrait.None) return;
            float now = Time.unscaledTime;
            float windup = HeavyOpeningWindup;
            if (windup > 0f)
            {
                DrawGuardAura(enemy, new Color(.78f, .40f, .18f, .20f + windup * .40f));
                DrawTraitBadge(enemy, ground, "蓄力重撞", new Color(.82f, .55f, .28f));
            }
            else if (now - heavyAt < .65f)
            {
                DrawRadialShards(enemy, Mathf.Clamp01((now - heavyAt) / .65f), new Color(.84f, .57f, .31f, .7f));
                DrawTraitBadge(enemy, ground, "重撞出手", Gold);
            }
            if (battleManager.CurrentEnemyTrait == EnemyTrait.VenomBite)
            {
                if (battleManager.PlayerVenomTicks > 0)
                {
                    DrawPoisonMotes(player, now, .45f, .45f);
                    DrawTraitBadge(player, ground, $"余毒 {battleManager.PlayerVenomTicks} 次", Poison);
                }
                if (now - biteAt < .65f)
                {
                    DrawBurst(player, poisonEffectFrames, now - biteAt, .65f,
                        new Color(.65f, .82f, .48f, .8f), .72f, 0f);
                    DrawTraitBadge(enemy, ground, "毒咬命中", Poison);
                }
                if (now - venomTickAt < .42f)
                    DrawGuardAura(player, new Color(.46f, .66f, .31f,
                        .65f * (1f - (now - venomTickAt) / .42f)));
            }
            if (battleManager.CurrentEnemyTrait == EnemyTrait.OpeningArmor)
            {
                if (battleManager.EnemyOpeningArmor > 0)
                    DrawGuardAura(enemy, new Color(.57f, .66f, .67f, .46f));
                if (now - armorBreakAt < .8f)
                {
                    DrawRadialShards(enemy, Mathf.Clamp01((now - armorBreakAt) / .6f), new Color(.78f, .68f, .45f, .85f));
                    DrawTraitBadge(enemy, ground, "机关破甲", Gold);
                }
            }
        }

        private void DrawTraitBadge(Rect actor, float ground, string label, Color accent)
        {
            float width = Mathf.Min(160f, ResponsiveGui.SafeArea.width * .44f);
            Rect badge = new Rect(Mathf.Clamp(actor.center.x - width * .5f,
                ResponsiveGui.SafeArea.x + 8f, ResponsiveGui.SafeArea.xMax - width - 8f),
                ResponsiveGui.IsPortrait ? ground + 24f : ground - 24f, width, 26f);
            WuxiaUiTheme.DrawCompactSurface(badge, new Color(.04f, .045f, .04f, .9f), accent);
            WuxiaUiComponents.Text(badge, label, 14, accent, TextAnchor.MiddleCenter);
        }

        private void OnDestroy()
        {
            if (softDisc != null) Destroy(softDisc);
            if (guardRing != null) Destroy(guardRing);
        }
    }
}
