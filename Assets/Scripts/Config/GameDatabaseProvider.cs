using System;
using UnityEngine;

namespace WuxiaRoguelite.Config
{
    [DisallowMultipleComponent]
    public sealed class GameDatabaseProvider : MonoBehaviour
    {
        [SerializeField] private GameDatabase database;

        public GameDatabase Database
        {
            get
            {
                if (database == null)
                {
                    throw new InvalidOperationException("GameDatabaseProvider 尚未绑定 GameDatabase.asset。");
                }

                return database;
            }
        }

        public bool IsConfigured => database != null;
    }
}
