using System;
using UnityEngine;
using UnityEngine.UI;

namespace WuxiaRoguelite.Architecture.UI
{
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button startButton;

        public event Action StartRequested;

        private void Awake()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartClicked);
            }
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
            }
        }

        public void SetVisible(bool visible)
        {
            (root != null ? root : gameObject).SetActive(visible);
        }

        private void OnStartClicked()
        {
            StartRequested?.Invoke();
        }
    }
}
