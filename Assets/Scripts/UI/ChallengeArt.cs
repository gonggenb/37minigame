using System.Collections.Generic;
using UnityEngine;

namespace WuxiaRoguelite.UI
{
    public static class ChallengeArt
    {
        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        public static Texture2D Get(string id)
        {
            if (!cache.TryGetValue(id, out var texture) || texture == null)
                cache[id] = texture = Resources.Load<Texture2D>("ChallengeIcons/" + id);
            return texture;
        }
    }
}
