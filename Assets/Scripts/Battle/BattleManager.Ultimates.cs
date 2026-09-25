using UnityEngine;
using WuxiaRoguelite.MartialArts;

namespace WuxiaRoguelite.Battle
{
    public partial class BattleManager
    {
        public int UltimateVisualSequence { get; private set; }
        public int UltimateBattleSerial { get; private set; }
        public string LastUltimateArtId { get; private set; }
        private float nextUltimateVisualAt = -100f;

        private void ResetUltimateVisuals()
        {
            UltimateBattleSerial++;
            UltimateVisualSequence = 0;
            LastUltimateArtId = null;
            nextUltimateVisualAt = -100f;
        }

        private void RecordUltimateActivation(string artId)
        {
            if (!IsBattleActive || playerStats == null || currentEnemy == null ||
                !MartialUltimateCatalog.IsEligible(artId, playerStats.GetMartialArtRank(artId)) ||
                Time.time < nextUltimateVisualAt) return;

            // A shared visual cooldown prevents simultaneous builds from covering the
            // battlefield repeatedly. Actual skill effects continue at their normal rate.
            LastUltimateArtId = artId;
            UltimateVisualSequence++;
            nextUltimateVisualAt = Time.time + MartialUltimateCatalog.PresentationCooldown;
        }
    }
}
