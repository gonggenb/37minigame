using NUnit.Framework;
using UnityEngine;
using WuxiaRoguelite.Config;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Tests.Unity
{
    public sealed class GameDatabaseTests
    {
        [Test]
        public void ReplaceAllBuildsRuntimeLookup()
        {
            GameDatabase database = ScriptableObject.CreateInstance<GameDatabase>();
            try
            {
                var data = new GameConfigSet();
                data.characters.Add(new CharacterConfig
                {
                    id = "player_wuxia",
                    displayName = "无名少侠",
                    maxHealth = 100f,
                    attack = 12f,
                    attackSpeed = 1f,
                    critMultiplier = 1.5f
                });

                database.ReplaceAll(data);

                Assert.That(database.GetCharacter("player_wuxia").displayName, Is.EqualTo("无名少侠"));
            }
            finally
            {
                Object.DestroyImmediate(database);
            }
        }
    }
}
