using UnityEngine;
using UnityEngine.SceneManagement;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;

namespace WuxiaRoguelite.GameFlow
{
    /// <summary>
    /// Owns the small amount of cross-scene progression needed by the two-level
    /// prototype. Gameplay state remains in GameFlowController; this class only
    /// tracks the tutorial unlock and the requested automatic hand-off.
    /// </summary>
    public static class LevelSequence
    {
        public const string TutorialSceneName = "TutorialLevel";
        public const string LevelTwoSceneName = "MainPrototype";
        public const float TutorialTimeLimitSeconds = 30f;
        private const string TutorialCompletedKey = "WuxiaRoguelite.TutorialCompleted.v1";
        private static bool autoStartLevelTwo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            autoStartLevelTwo = false;
        }

        public static bool IsTutorialScene =>
            SceneManager.GetActiveScene().name == TutorialSceneName;

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
            Load(LevelTwoSceneName, GameTextCatalog.GameTitle, false);
        }

        private static void Load(string scene, string title, bool startRun, string subtitle = null)
        {
            // Reject duplicate requests before they can overwrite the destination intent.
            if (LevelLoadingScreen.IsLoading) return;
            autoStartLevelTwo = startRun;
            if (!LevelLoadingScreen.Load(scene, title, subtitle)) autoStartLevelTwo = false;
        }

        /// <summary>
        /// Consumed from GameFlowController.Start after the newly loaded scene has
        /// finished its own initialization. A sceneLoaded callback fires before
        /// Start and would let Start immediately overwrite the requested level state.
        /// </summary>
        public static bool ConsumeLevelTwoAutoStartRequest()
        {
            if (SceneManager.GetActiveScene().name != LevelTwoSceneName ||
                !autoStartLevelTwo)
            {
                return false;
            }

            autoStartLevelTwo = false;
            return true;
        }
    }
}
