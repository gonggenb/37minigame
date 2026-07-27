using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using WuxiaRoguelite.Config;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Architecture.UI
{
    public sealed class LevelUpView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button[] choiceButtons = Array.Empty<Button>();
        [SerializeField] private Text[] choiceLabels = Array.Empty<Text>();
        [SerializeField] private Image[] choiceIcons = Array.Empty<Image>();
        [SerializeField] private InkArtCatalog inkArtCatalog;
        [SerializeField] private Button rerollButton;
        [SerializeField] private Text rerollText;

        private UnityAction[] choiceActions = Array.Empty<UnityAction>();

        public event Action<int> ChoiceRequested;
        public event Action RerollRequested;

        public void ConfigureInkArt(InkArtCatalog catalog, Image[] icons)
        {
            inkArtCatalog = catalog;
            choiceIcons = icons ?? Array.Empty<Image>();
        }

        private void Awake()
        {
            choiceActions = new UnityAction[choiceButtons.Length];
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int index = i;
                choiceActions[i] = () => ChoiceRequested?.Invoke(index);
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].onClick.AddListener(choiceActions[i]);
                }
            }

            if (rerollButton != null)
            {
                rerollButton.onClick.AddListener(OnRerollClicked);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < choiceButtons.Length && i < choiceActions.Length; i++)
            {
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].onClick.RemoveListener(choiceActions[i]);
                }
            }

            if (rerollButton != null)
            {
                rerollButton.onClick.RemoveListener(OnRerollClicked);
            }
        }

        public void SetVisible(bool visible)
        {
            (root != null ? root : gameObject).SetActive(visible);
        }

        public void Render(GameDatabase database, IReadOnlyList<string> choiceIds, int rerollsRemaining)
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                bool available = choiceIds != null && i < choiceIds.Count;
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].gameObject.SetActive(available);
                }

                if (!available || i >= choiceLabels.Length || choiceLabels[i] == null)
                {
                    continue;
                }

                MartialArtConfig config = database.GetMartialArt(choiceIds[i]);
                choiceLabels[i].text = $"{config.displayName}\n{config.description}";
                if (i < choiceIcons.Length && choiceIcons[i] != null)
                {
                    Sprite icon = inkArtCatalog != null ? inkArtCatalog.GetSprite(choiceIds[i]) : null;
                    choiceIcons[i].sprite = icon;
                    choiceIcons[i].preserveAspect = true;
                    choiceIcons[i].gameObject.SetActive(icon != null);
                }
            }

            if (rerollButton != null)
            {
                rerollButton.interactable = rerollsRemaining > 0;
            }

            if (rerollText != null)
            {
                rerollText.text = $"刷新（{rerollsRemaining}）";
            }
        }

        private void OnRerollClicked()
        {
            RerollRequested?.Invoke();
        }
    }
}
