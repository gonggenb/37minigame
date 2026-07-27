using System;
using UnityEngine;
using WuxiaRoguelite.Application.Characters;
using WuxiaRoguelite.Application.Combat;
using WuxiaRoguelite.Architecture.Characters;
using WuxiaRoguelite.Domain.Characters;
using WuxiaRoguelite.Domain.Combat;

namespace WuxiaRoguelite.Architecture.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleRunner : MonoBehaviour
    {
        [SerializeField] private CharacterManager characterManager;
        [SerializeField, Min(0.1f)] private float battleSpeed = 1.5f;

        private readonly CharacterFactory characterFactory = new CharacterFactory();
        private Action<bool> completion;

        public event Action BattleChanged;
        public event Action<bool> BattleEnded;

        public BattleService CurrentBattle { get; private set; }
        public bool IsActive => CurrentBattle != null && !CurrentBattle.IsFinished;
        public string CurrentEnemyId { get; private set; } = string.Empty;

        private void Update()
        {
            if (CurrentBattle == null || CurrentBattle.IsFinished)
            {
                return;
            }

            CurrentBattle.Tick(Time.deltaTime * Mathf.Max(0.1f, battleSpeed));
            BattleChanged?.Invoke();
            if (CurrentBattle.IsFinished)
            {
                CompleteBattle();
            }
        }

        public void BeginBattle(string enemyCharacterId, Action<bool> onComplete)
        {
            if (characterManager == null || characterManager.Player == null)
            {
                throw new InvalidOperationException("BattleRunner 需要已初始化的 CharacterManager。");
            }

            CancelBattle();
            CharacterRuntime enemy = characterFactory.Create(
                characterManager.Database.GetCharacter(enemyCharacterId));
            CurrentEnemyId = enemyCharacterId;
            completion = onComplete;
            CurrentBattle = new BattleService(
                characterManager.Player,
                enemy,
                new UnityRandomSource(),
                characterManager.BuildCombatEffects());
            BattleChanged?.Invoke();
        }

        public void CancelBattle()
        {
            CurrentBattle = null;
            CurrentEnemyId = string.Empty;
            completion = null;
            BattleChanged?.Invoke();
        }

        private void CompleteBattle()
        {
            bool playerWon = CurrentBattle.PlayerWon;
            Action<bool> callback = completion;
            completion = null;
            BattleEnded?.Invoke(playerWon);
            callback?.Invoke(playerWon);
        }

        private sealed class UnityRandomSource : IRandomSource
        {
            public float Next01()
            {
                return UnityEngine.Random.value;
            }
        }
    }
}
