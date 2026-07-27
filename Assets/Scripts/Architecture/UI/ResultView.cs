using System;
using UnityEngine;
using UnityEngine.UI;
using WuxiaRoguelite.Architecture.Characters;
using WuxiaRoguelite.Architecture.GameFlow;

namespace WuxiaRoguelite.Architecture.UI
{
    public sealed class ResultView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text resultText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button restartButton;

        public event Action RestartRequested;

        private void Awake()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }
        }

        private void OnDestroy()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
            }
        }

        public void SetVisible(bool visible)
        {
            (root != null ? root : gameObject).SetActive(visible);
        }

        public void Render(RunManager run, CharacterManager characters)
        {
            if (resultText != null)
            {
                resultText.text = run.Victory ? "名震江湖" : "此战未竟";
            }

            if (summaryText != null)
            {
                summaryText.text =
                    $"{run.StatusMessage}\n击败 {characters.KillCount} 名敌人 · 洞穴 {characters.CaveEntries} 次\n" +
                    $"境界 {characters.Level} · 铜钱 {characters.Copper} · Boss 战 {run.Timer.BossBattleTime:0.0}s";
            }
        }

        private void OnRestartClicked()
        {
            RestartRequested?.Invoke();
        }
    }
}
