using UnityEngine;

namespace WuxiaRoguelite.Battle
{
    public enum MartialProcKind { ArmorBreak, SwiftCombo, Retaliation, LifeDrain }

    public struct MartialProcEvent
    {
        public int sequence;
        public MartialProcKind kind;
        public float triggeredAt;
    }

    public partial class BattleManager
    {
        // Keep independent settlement events: poison/enemy attacks can overwrite LastVfxCues
        // in the same frame. Presentation reads this bounded journal without touching combat RNG.
        public const int MartialProcCapacity = 32;
        private readonly MartialProcEvent[] martialProcs = new MartialProcEvent[MartialProcCapacity];
        public int MartialProcSequence { get; private set; }
        public int MartialProcBattleVersion { get; private set; }

        private void ResetMartialProcs()
        {
            MartialProcSequence = 0;
            MartialProcBattleVersion++;
        }

        private void RecordMartialProc(MartialProcKind kind)
        {
            int sequence = ++MartialProcSequence;
            martialProcs[(sequence - 1) % MartialProcCapacity] = new MartialProcEvent
                { sequence = sequence, kind = kind, triggeredAt = Time.time };
        }

        public bool TryGetMartialProc(int sequence, out MartialProcEvent effect)
        {
            effect = default;
            if (sequence <= 0 || sequence > MartialProcSequence ||
                sequence <= MartialProcSequence - MartialProcCapacity) return false;
            effect = martialProcs[(sequence - 1) % MartialProcCapacity];
            return effect.sequence == sequence;
        }
    }
}
