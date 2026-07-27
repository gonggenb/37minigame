using UnityEngine;
using UnityEngine.UI;
using WuxiaRoguelite.Architecture.Battle;
using WuxiaRoguelite.Architecture.GameFlow;
using WuxiaRoguelite.Config;

namespace WuxiaRoguelite.Architecture.UI
{
    [DisallowMultipleComponent]
    public sealed class InkBattleStage : MonoBehaviour
    {
        [SerializeField] private RunManager runManager;
        [SerializeField] private BattleRunner battleRunner;
        [SerializeField] private InkArtCatalog catalog;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image playerImage;
        [SerializeField] private Image enemyImage;
        [SerializeField] private string backgroundId = "background.landscape.main";
        [SerializeField, Min(1f)] private float framesPerSecond = 8f;

        private void OnEnable()
        {
            Refresh();
        }

        public void Configure(
            RunManager run,
            BattleRunner runner,
            InkArtCatalog artCatalog,
            Image background,
            Image player,
            Image enemy,
            string layoutBackgroundId)
        {
            runManager = run;
            battleRunner = runner;
            catalog = artCatalog;
            backgroundImage = background;
            playerImage = player;
            enemyImage = enemy;
            backgroundId = layoutBackgroundId;
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (catalog == null)
            {
                return;
            }

            if (backgroundImage != null)
            {
                backgroundImage.sprite = catalog.GetSprite(backgroundId);
                backgroundImage.preserveAspect = false;
            }

            SetActor(playerImage, catalog.GetCharacter("player_wuxia"), false);
            string enemyId = battleRunner != null ? battleRunner.CurrentEnemyId : null;
            SetActor(enemyImage, catalog.GetCharacter(enemyId), true);

            bool battleVisible = runManager != null && battleRunner != null && battleRunner.CurrentBattle != null;
            if (playerImage != null)
            {
                playerImage.enabled = battleVisible && playerImage.sprite != null;
            }

            if (enemyImage != null)
            {
                enemyImage.enabled = battleVisible && enemyImage.sprite != null;
            }
        }

        private void SetActor(Image image, InkArtCatalog.CharacterEntry character, bool facesLeft)
        {
            if (image == null)
            {
                return;
            }

            Sprite[] frames = character != null && character.moveFrames != null && character.moveFrames.Length > 0
                ? character.moveFrames
                : character?.idleFrames;
            if (frames == null || frames.Length == 0)
            {
                image.sprite = character?.portrait;
                return;
            }

            int index = Mathf.FloorToInt(Time.unscaledTime * framesPerSecond) % frames.Length;
            image.sprite = frames[index];
            image.preserveAspect = true;
            Vector3 scale = image.rectTransform.localScale;
            scale.x = Mathf.Abs(scale.x) * (facesLeft ? -1f : 1f);
            image.rectTransform.localScale = scale;
        }
    }
}
