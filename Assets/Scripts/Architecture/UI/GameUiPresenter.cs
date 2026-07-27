using UnityEngine;
using WuxiaRoguelite.Architecture.GameFlow;
using WuxiaRoguelite.Domain.GameFlow;

namespace WuxiaRoguelite.Architecture.UI
{
    [DisallowMultipleComponent]
    public sealed class GameUiPresenter : MonoBehaviour
    {
        [SerializeField] private RunManager runManager;
        [SerializeField] private MainMenuView mainMenuView;
        [SerializeField] private HudView hudView;
        [SerializeField] private BattleView battleView;
        [SerializeField] private CaveView caveView;
        [SerializeField] private LevelUpView levelUpView;
        [SerializeField] private ResultView resultView;

        private void OnEnable()
        {
            if (runManager != null)
            {
                runManager.StateChanged += Refresh;
                runManager.Updated += Refresh;
            }

            if (mainMenuView != null)
            {
                mainMenuView.StartRequested += StartRun;
            }

            if (caveView != null)
            {
                caveView.ExitRequested += ExitCave;
            }

            if (levelUpView != null)
            {
                levelUpView.ChoiceRequested += ChooseMartialArt;
                levelUpView.RerollRequested += Reroll;
            }

            if (resultView != null)
            {
                resultView.RestartRequested += StartRun;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (runManager != null)
            {
                runManager.StateChanged -= Refresh;
                runManager.Updated -= Refresh;
            }

            if (mainMenuView != null)
            {
                mainMenuView.StartRequested -= StartRun;
            }

            if (caveView != null)
            {
                caveView.ExitRequested -= ExitCave;
            }

            if (levelUpView != null)
            {
                levelUpView.ChoiceRequested -= ChooseMartialArt;
                levelUpView.RerollRequested -= Reroll;
            }

            if (resultView != null)
            {
                resultView.RestartRequested -= StartRun;
            }
        }

        private void Refresh()
        {
            if (runManager == null)
            {
                return;
            }

            GameState state = runManager.CurrentState;
            bool hasPlayer = runManager.Characters != null && runManager.Characters.Player != null;
            mainMenuView?.SetVisible(state == GameState.Ready);
            hudView?.SetVisible(hasPlayer && state != GameState.Ready && state != GameState.Result);
            battleView?.SetVisible(
                hasPlayer && runManager.Battle != null && runManager.Battle.CurrentBattle != null &&
                (state == GameState.NormalBattle || state == GameState.Cave || state == GameState.BossBattle));
            caveView?.SetVisible(state == GameState.Cave && (runManager.Battle == null || !runManager.Battle.IsActive));
            levelUpView?.SetVisible(state == GameState.LevelUp);
            resultView?.SetVisible(state == GameState.Result);

            if (!hasPlayer)
            {
                return;
            }

            hudView?.Render(runManager, runManager.Characters);
            battleView?.Render(runManager, runManager.Battle);
            if (state == GameState.LevelUp)
            {
                levelUpView?.Render(
                    runManager.Characters.Database,
                    runManager.MartialArtChoices,
                    runManager.RerollsRemaining);
            }

            if (state == GameState.Result)
            {
                resultView?.Render(runManager, runManager.Characters);
            }
        }

        private void StartRun()
        {
            runManager.StartRun();
        }

        private void ExitCave()
        {
            runManager.ExitCave();
        }

        private void ChooseMartialArt(int index)
        {
            runManager.ChooseMartialArt(index);
        }

        private void Reroll()
        {
            runManager.RerollMartialArtChoices();
        }
    }
}
