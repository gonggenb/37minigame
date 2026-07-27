using UnityEngine;
using UnityEngine.UI;
using WuxiaRoguelite.Architecture.Characters;
using WuxiaRoguelite.Architecture.GameFlow;

namespace WuxiaRoguelite.Architecture.UI
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text timerText;
        [SerializeField] private Text healthText;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Text progressionText;
        [SerializeField] private Text currencyText;
        [SerializeField] private Text statusText;

        public void SetVisible(bool visible)
        {
            (root != null ? root : gameObject).SetActive(visible);
        }

        public void Render(RunManager run, CharacterManager characters)
        {
            if (run == null || characters == null || characters.Player == null)
            {
                return;
            }

            float currentHealth = characters.Player.CurrentHealth;
            float maximumHealth = characters.Player.Stats.MaxHealth;
            if (timerText != null)
            {
                timerText.text = $"余时 {run.Timer.MainTimeRemaining:0.0}s";
            }

            if (healthText != null)
            {
                healthText.text = $"气血 {currentHealth:0}/{maximumHealth:0}";
            }

            if (healthSlider != null)
            {
                healthSlider.minValue = 0f;
                healthSlider.maxValue = Mathf.Max(1f, maximumHealth);
                healthSlider.value = currentHealth;
            }

            if (progressionText != null)
            {
                progressionText.text = $"境界 {characters.Level} · 修为 {characters.Cultivation}";
            }

            if (currencyText != null)
            {
                currencyText.text = $"铜钱 {characters.Copper}";
            }

            if (statusText != null)
            {
                statusText.text = run.StatusMessage;
            }
        }
    }
}
