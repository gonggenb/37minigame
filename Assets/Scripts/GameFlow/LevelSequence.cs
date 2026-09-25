using UnityEngine;
using UnityEngine.SceneManagement;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;

namespace WuxiaRoguelite.GameFlow
{
    /// <summary>
    /// Owns the small amount of cross-scene progression needed by the three-level
    /// prototype. Gameplay state remains in GameFlowController; this class only
    /// tracks the tutorial unlock and the requested automatic hand-off.
    /// </summary>
    public static class LevelSequence
    {
        public const string MenuSceneName = "BootMenu";
        public const string TutorialSceneName = "TutorialLevel";
        public const string LevelTwoSceneName = "MainPrototype";
        public const string LevelThreeSceneName = "BambooValleyLevel";
        public const float TutorialTimeLimitSeconds = 30f;
        private const string TutorialCompletedKey = "WuxiaRoguelite.TutorialCompleted.v1";
        private const string LevelTwoCompletedKey = "WuxiaRoguelite.LevelTwoCompleted.v1";
        private static string autoStartScene;
        private static bool showSelectionOnMenu;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            autoStartScene = null;
            showSelectionOnMenu = false;
        }

        public static bool IsTutorialScene =>
            SceneManager.GetActiveScene().name == TutorialSceneName;
        public static bool IsMenuScene => SceneManager.GetActiveScene().name == MenuSceneName;

        public static bool IsLevelThreeScene =>
            SceneManager.GetActiveScene().name == LevelThreeSceneName;

        public static bool LevelTwoCompleted => PlayerPrefs.GetInt(LevelTwoCompletedKey, 0) == 1;

        public static void MarkLevelTwoCompleted()
        {
            PlayerPrefs.SetInt(LevelTwoCompletedKey, 1);
            PlayerPrefs.Save();
        }

        public static bool TutorialCompleted =>
            PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;

        public static void MarkTutorialCompleted()
        {
            PlayerPrefs.SetInt(TutorialCompletedKey, 1);
            PlayerPrefs.Save();
        }

        public static void LoadTutorial()
        {
            Load(TutorialSceneName, GameTextCatalog.TutorialLevelName, false);
        }

        public static void CompleteTutorialAndLoadLevelTwo()
        {
            MarkTutorialCompleted();
            Load(LevelTwoSceneName, GameTextCatalog.MainLevelName, true, "难度飙升！！！");
        }

        public static void LoadLevelTwoFromSelection()
        {
            if (!TutorialCompleted)
            {
                return;
            }

            Load(LevelTwoSceneName, GameTextCatalog.MainLevelName, true);
        }

        public static void LoadLevelSelection()
        {
            LoadMainMenu(true);
        }

        public static void LoadMainMenu(bool showSelection = false)
        {
            if (LevelLoadingScreen.IsLoading) return;
            showSelectionOnMenu = showSelection;
            Load(MenuSceneName, GameTextCatalog.GameTitle, false);
        }

        public static bool ConsumeMenuSelectionRequest()
        {
            bool value = showSelectionOnMenu;
            showSelectionOnMenu = false;
            return value;
        }

        public static void CancelPendingRequest()
        {
            autoStartScene = null;
            showSelectionOnMenu = false;
        }

        public static void LoadLevelThree()
        {
            if (!LevelTwoCompleted) return;
            Load(LevelThreeSceneName, GameTextCatalog.BambooValleyLevelName, true);
        }

        private static void Load(string scene, string title, bool startRun, string subtitle = null)
        {
            // Reject duplicate requests before they can overwrite the destination intent.
            if (LevelLoadingScreen.IsLoading) return;
            autoStartScene = startRun ? scene : null;
            if (!LevelLoadingScreen.Load(scene, title, subtitle)) autoStartScene = null;
        }

        /// <summary>
        /// Consumed from GameFlowController.Start after the newly loaded scene has
        /// finished its own initialization. A sceneLoaded callback fires before
        /// Start and would let Start immediately overwrite the requested level state.
        /// </summary>
        public static bool ConsumeAutoStartRequest()
        {
            if (string.IsNullOrEmpty(autoStartScene) ||
                SceneManager.GetActiveScene().name != autoStartScene)
            {
                return false;
            }

            autoStartScene = null;
            return true;
        }

        // Keep the existing transition probe and integrations source-compatible.
        public static bool ConsumeLevelTwoAutoStartRequest() => ConsumeAutoStartRequest();
    }
}
