using UnityEngine;
using UnityEngine.SceneManagement;

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
        private const string AutoStartLevelTwoKey = "WuxiaRoguelite.AutoStartLevelTwo.v1";
        private const string LevelTwoDifficultyNoticeShownKey =
            "WuxiaRoguelite.LevelTwoDifficultyNoticeShown.v2";

        public static bool IsTutorialScene =>
            SceneManager.GetActiveScene().name == TutorialSceneName;

        public static bool TutorialCompleted =>
            PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;

        public static bool ShouldShowLevelTwoDifficultyNotice =>
            TutorialCompleted && PlayerPrefs.GetInt(LevelTwoDifficultyNoticeShownKey, 0) != 1;

        public static void MarkTutorialCompleted()
        {
            PlayerPrefs.SetInt(TutorialCompletedKey, 1);
            PlayerPrefs.Save();
        }

        public static void MarkLevelTwoDifficultyNoticeShown()
        {
            PlayerPrefs.SetInt(LevelTwoDifficultyNoticeShownKey, 1);
            PlayerPrefs.Save();
        }

        public static void LoadTutorial()
        {
            PlayerPrefs.DeleteKey(AutoStartLevelTwoKey);
            PlayerPrefs.Save();
            SceneManager.LoadScene(TutorialSceneName);
        }

        public static void CompleteTutorialAndLoadLevelTwo()
        {
            MarkTutorialCompleted();
            PlayerPrefs.SetInt(AutoStartLevelTwoKey, 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene(LevelTwoSceneName);
        }

        public static void LoadLevelTwoFromSelection()
        {
            if (!TutorialCompleted)
            {
                return;
            }

            PlayerPrefs.SetInt(AutoStartLevelTwoKey, 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene(LevelTwoSceneName);
        }

        public static void LoadLevelSelection()
        {
            PlayerPrefs.DeleteKey(AutoStartLevelTwoKey);
            PlayerPrefs.Save();
            SceneManager.LoadScene(LevelTwoSceneName);
        }

        /// <summary>
        /// Consumed from GameFlowController.Start after the newly loaded scene has
        /// finished its own initialization. A sceneLoaded callback fires before
        /// Start and would let Start immediately overwrite the requested level state.
        /// </summary>
        public static bool ConsumeLevelTwoAutoStartRequest()
        {
            if (SceneManager.GetActiveScene().name != LevelTwoSceneName ||
                PlayerPrefs.GetInt(AutoStartLevelTwoKey, 0) != 1)
            {
                return false;
            }

            PlayerPrefs.DeleteKey(AutoStartLevelTwoKey);
            PlayerPrefs.Save();
            return true;
        }
    }
}
