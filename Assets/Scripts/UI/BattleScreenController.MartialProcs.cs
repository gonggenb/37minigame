using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Visual;

namespace WuxiaRoguelite.UI
{
    public partial class BattleScreenController
    {
        private struct ProcVisual
        {
            public bool active;
            public MartialProcKind kind;
            public float startedAt, duration;
            public Vector2 viewport;
        }

        // Two slots per kind ensure rapid armor hits cannot evict a return-heal or retaliation.
        private readonly ProcVisual[] procVisuals = new ProcVisual[8];
        private int observedProcVersion = -1, observedProcSequence;
        public int ActiveMartialProcVfxCount
        {
            get
            {
                int count = 0;
                foreach (var fx in procVisuals)
                    if (fx.active && Time.time >= fx.startedAt && Time.time < fx.startedAt + fx.duration) count++;
                return count;
            }
        }

        private void TrackMartialProcs()
        {
            if (battleManager == null) return;
            if (!battleManager.IsBattleActive || observedProcVersion != battleManager.MartialProcBattleVersion)
            {
                for (int i = 0; i < procVisuals.Length; i++) procVisuals[i].active = false;
                observedProcVersion = battleManager.MartialProcBattleVersion;
                observedProcSequence = 0;
            }
            if (!battleManager.IsBattleActive) return;
            for (int i = 0; i < procVisuals.Length; i++)
                if (procVisuals[i].viewport != new Vector2(Screen.width, Screen.height)) procVisuals[i].active = false;
            int first = Mathf.Max(observedProcSequence + 1,
                battleManager.MartialProcSequence - BattleManager.MartialProcCapacity + 1);
            for (int sequence = first; sequence <= battleManager.MartialProcSequence; sequence++)
            {
                if (!battleManager.TryGetMartialProc(sequence, out var effect)) continue;
                float duration = .5f / Mathf.Max(.1f, battleManager.BattleSpeedMultiplier);
                if (Time.time - effect.triggeredAt >= duration) continue;
                int slot = (int)effect.kind * 2;
                int other = slot + 1;
                if (procVisuals[slot].active && procVisuals[slot].startedAt > procVisuals[other].startedAt) slot = other;
                procVisuals[slot] = new ProcVisual { active = true, kind = effect.kind,
                    startedAt = effect.triggeredAt, duration = duration,
                    viewport = new Vector2(Screen.width, Screen.height) };
            }
            observedProcSequence = battleManager.MartialProcSequence;
        }

        private void DrawMartialProcs(Rect player, Rect enemy)
        {
            if (Event.current.type != EventType.Repaint) return;
            foreach (var fx in procVisuals)
            {
                if (!fx.active || fx.viewport != new Vector2(Screen.width, Screen.height)) continue;
                float p = (Time.time - fx.startedAt) / fx.duration;
                Sprite sprite = MartialProcVfxArt.Frame(fx.kind, p);
                if (sprite == null) continue;
                float baseSize = Mathf.Clamp(player.width * .56f, 82f, 136f);
                // Scale the silhouettes independently so attack arcs read clearly without
                // making the defensive pulse cover the player's whole body.
                float scale = fx.kind == MartialProcKind.ArmorBreak ? 1.80f
                    : fx.kind == MartialProcKind.SwiftCombo ? 1.70f
                    : fx.kind == MartialProcKind.Retaliation ? 1.40f : 1.60f;
                float size = baseSize * scale;
                Vector2 playerAnchor = new Vector2(player.center.x, player.y + player.height * .60f);
                Vector2 enemyAnchor = new Vector2(enemy.center.x, enemy.y + enemy.height * .60f);
                Vector2 center = enemyAnchor;
                if (fx.kind == MartialProcKind.Retaliation)
                    center = playerAnchor + Vector2.right * (player.width * .30f + (size - baseSize) * .22f);
                else if (fx.kind == MartialProcKind.LifeDrain)
                {
                    size *= 1.12f;
                    // The authored receiver is at 28% of each inset cell. Land that point on
                    // the player's torso while the final two frames absorb, never beyond it.
                    // Keep the larger emission tail beside the enemy rather than beyond
                    // the screen edge; the receiving point still lands on the same torso.
                    Vector2 start = enemyAnchor - Vector2.right * size * .28f;
                    Vector2 end = playerAnchor + Vector2.right * size * .1925f;
                    center = Vector2.Lerp(start, end,
                        Mathf.SmoothStep(0, 1, Mathf.Clamp01(p / .70f)));
                }
                float alpha = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.80f, 1f, p));
                DrawEffectSprite(new Rect(center.x - size * .5f, center.y - size * .5f, size, size),
                    sprite, new Color(1, 1, 1, alpha));
            }
        }
    }
}
