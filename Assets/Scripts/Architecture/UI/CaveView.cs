using System;
using UnityEngine;
using UnityEngine.UI;

namespace WuxiaRoguelite.Architecture.UI
{
    public sealed class CaveView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Button exitButton;

        public event Action ExitRequested;

        private void Awake()
        {
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(OnExitClicked);
            }
        }

        private void OnDestroy()
        {
            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitClicked);
            }
        }

        public void SetVisible(bool visible)
        {
            (root != null ? root : gameObject).SetActive(visible);
            if (visible && descriptionText != null)
            {
                descriptionText.text = "隐藏洞穴中主地图倒计时暂停。可绑定洞穴敌人、宝箱或商人内容。";
            }
        }

        private void OnExitClicked()
        {
            ExitRequested?.Invoke();
        }
    }
}
