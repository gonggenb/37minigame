using UnityEngine;
using UnityEngine.UI;
using WuxiaRoguelite.Architecture.Battle;
using WuxiaRoguelite.Architecture.GameFlow;
using WuxiaRoguelite.Domain.GameFlow;

namespace WuxiaRoguelite.Architecture.UI
{
    public sealed class BattleView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text playerHealthText;
        [SerializeField] private Slider playerHealthSlider;
        [SerializeField] private Text enemyHealthText;
        [SerializeField] private Slider enemyHealthSlider;
        [SerializeField] private Text effectText;
        [SerializeField] private Text battleTimeText;

        public void SetVisible(bool visible)
        {
            (root != null ? root : gameObject).SetActive(visible);
        }

        public void Render(RunManager run, BattleRunner runner)
        {
            if (run == null || runner == null || runner.CurrentBattle == null)
            {
                return;
            }

            var battle = runner.CurrentBattle;
            if (titleText != null)
            {
                titleText.text = run.CurrentState == GameState.BossBattle ? "最终强敌" : "自动战斗";
            }

            SetHealth(
                playerHealthText,
                playerHealthSlider,
                "少侠",
                battle.Player.CurrentHealth,
                battle.Player.Stats.MaxHealth);
            SetHealth(
                enemyHealthText,
                enemyHealthSlider,
                runner.CurrentEnemyId,
                battle.Enemy.CurrentHealth,
                battle.Enemy.Stats.MaxHealth);

            if (effectText != null)
            {
                effectText.text = $"护盾 {battle.PlayerShield:0} · 敌方破甲 {battle.EnemyArmorBreak:0.0} · 毒层 {battle.EnemyPoisonStacks}";
            }

            if (battleTimeText != null)
            {
                battleTimeText.text = run.CurrentState == GameState.BossBattle
                    ? $"Boss 战 {run.Timer.BossBattleTime:0.0}s"
                    : $"战斗 {battle.Elapsed:0.0}s · 主时间继续";
            }
        }

        private static void SetHealth(
            Text label,
            Slider slider,
            string name,
            float current,
            float maximum)
        {
            if (label != null)
            {
                label.text = $"{name} {current:0}/{maximum:0}";
            }

            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = Mathf.Max(1f, maximum);
                slider.value = current;
            }
        }
    }
}
