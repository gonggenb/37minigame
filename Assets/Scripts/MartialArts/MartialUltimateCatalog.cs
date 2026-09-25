using UnityEngine;

namespace WuxiaRoguelite.MartialArts
{
    // Presentation eligibility only. No extra ranks, damage or proc probability.
    public static class MartialUltimateCatalog
    {
        public const float PresentationDuration = 1.30f;
        public const float PresentationCooldown = 4f;
        public static readonly string[] ArtIds =
            { "无影连环剑", "化功毒雾", "不动明王身", "无相残影", "修罗血域" };
        public static readonly string[] TextureIds =
            { "swift_sword", "venom_mist", "iron_guard", "shadow_moon", "blood_domain" };

        public static int IndexOf(string artId) => System.Array.IndexOf(ArtIds, artId);
        public static bool IsEligible(string artId, int rank)
        {
            var definition = MartialArtCatalog.Get(artId);
            return definition != null && definition.isCapstone && rank >= definition.maxRank && IndexOf(artId) >= 0;
        }

        public static Color Tint(int index)
        {
            switch (index)
            {
                case 0: return new Color(.53f,.87f,.93f);
                case 1: return new Color(.48f,.79f,.46f);
                case 2: return new Color(.98f,.77f,.34f);
                case 3: return new Color(.67f,.72f,.92f);
                default: return new Color(.90f,.27f,.16f);
            }
        }
    }
}
