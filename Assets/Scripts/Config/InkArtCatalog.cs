using System;
using System.Collections.Generic;
using UnityEngine;

namespace WuxiaRoguelite.Config
{
    [CreateAssetMenu(fileName = "InkArtCatalog", menuName = "一炷江湖/Ink Art Catalog")]
    public sealed class InkArtCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class SpriteEntry
        {
            public string id;
            public Sprite sprite;
        }

        [Serializable]
        public sealed class CharacterEntry
        {
            public string id;
            public Sprite[] idleFrames = Array.Empty<Sprite>();
            public Sprite[] moveFrames = Array.Empty<Sprite>();
            public Sprite portrait;
            [Min(0.1f)] public float worldScale = 1f;
        }

        [SerializeField] private SpriteEntry[] sprites = Array.Empty<SpriteEntry>();
        [SerializeField] private CharacterEntry[] characters = Array.Empty<CharacterEntry>();

        private Dictionary<string, Sprite> spriteIndex;
        private Dictionary<string, CharacterEntry> characterIndex;

        public Sprite GetSprite(string id)
        {
            EnsureIndex();
            return !string.IsNullOrWhiteSpace(id) && spriteIndex.TryGetValue(id, out Sprite sprite)
                ? sprite
                : null;
        }

        public CharacterEntry GetCharacter(string id)
        {
            EnsureIndex();
            return !string.IsNullOrWhiteSpace(id) && characterIndex.TryGetValue(id, out CharacterEntry entry)
                ? entry
                : null;
        }

        private void OnEnable()
        {
            spriteIndex = null;
            characterIndex = null;
        }

        private void OnValidate()
        {
            spriteIndex = null;
            characterIndex = null;
        }

        private void EnsureIndex()
        {
            if (spriteIndex != null && characterIndex != null)
            {
                return;
            }

            spriteIndex = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            foreach (SpriteEntry entry in sprites)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.id) && entry.sprite != null)
                {
                    spriteIndex[entry.id] = entry.sprite;
                }
            }

            characterIndex = new Dictionary<string, CharacterEntry>(StringComparer.Ordinal);
            foreach (CharacterEntry entry in characters)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.id))
                {
                    characterIndex[entry.id] = entry;
                }
            }
        }
    }
}
