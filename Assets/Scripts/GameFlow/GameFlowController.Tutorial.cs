using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.GameFlow
{
    public partial class GameFlowController
    {
        private readonly HashSet<string> learnedTutorialTopics = new HashSet<string>();
        private EncounterTrigger tutorialPendingEncounter;
        private GamePhase tutorialResumePhase;
        private string tutorialTopic;
        private float tutorialLessonOpenedAt;

        public TutorialLesson CurrentTutorialLesson { get; private set; }
        public bool IsTutorialLessonActive => IsTutorialLevel &&
            CurrentPhase == GamePhase.TutorialLearning && CurrentTutorialLesson != null;

        private void BeginTutorialBoss()
        {
            if (CurrentPhase == GamePhase.BossBattle || CurrentPhase == GamePhase.Result ||
                (IsTutorialLessonActive && tutorialTopic == "TutorialBoss")) return;
            tutorialTransitionPending = false;
            IsBossTransitionPending = false;
            IsBossIntroActive = false;
            BossIntroTimeRemaining = 0f;
            mainTimeRemaining = 0f;
            bossBattleTime = 0f;
            caveRoom?.ResetRoom();
            battleManager?.CancelBattle();
            ShowTutorialLesson("TutorialBoss", TutorialLessonCatalog.Boss, null);
        }

        private void BeginTutorialBossCombat()
        {
            playerStats.ClearTemporaryMoveSpeedBuffs();
            playerStats.runtimeStats.ResetHealth();
            SetPhase(GamePhase.BossBattle);
            statusMessage = $"新手试炼：击败{GameTextCatalog.TutorialBossName}，完成第一关。";
            // Reuse the basic auto-battle simulation; foxfire/armor/enrage belong to level two.
            battleManager.BeginBattle(bossStats.Clone(), OnBossBattleFinished);
        }

        private bool TryShowEncounterLesson(EncounterTrigger encounter)
        {
            if (!IsTutorialLevel) return false;
            string topic = encounter.encounterType == EncounterType.Herb
                ? $"Herb:{encounter.herbEffect}"
                : encounter.encounterType.ToString();
            if (learnedTutorialTopics.Contains(topic)) return false;
            TutorialLesson lesson = TutorialLessonCatalog.ForEncounter(encounter);
            if (lesson == null) return false;
            ShowTutorialLesson(topic, lesson, encounter);
            return true;
        }

        private void TryShowMartialArtLesson()
        {
            const string topic = "MartialArtChoice";
            if (IsTutorialLevel && !learnedTutorialTopics.Contains(topic))
                ShowTutorialLesson(topic, TutorialLessonCatalog.MartialArtChoice, null);
        }

        private void ShowTutorialLesson(string topic, TutorialLesson lesson, EncounterTrigger encounter)
        {
            tutorialResumePhase = CurrentPhase;
            tutorialTopic = topic;
            tutorialPendingEncounter = encounter;
            CurrentTutorialLesson = lesson;
            tutorialLessonOpenedAt = Time.unscaledTime;
            // A dedicated non-combat phase stops the tutorial clock without owning global timeScale.
            // The encounter stays intact until the player confirms; normal combat has not begun yet.
            SetPhase(GamePhase.TutorialLearning);
        }

        public void ConfirmTutorialLesson()
        {
            if (!IsTutorialLessonActive || Time.unscaledTime - tutorialLessonOpenedAt < 0.25f ||
                WuxiaRoguelite.UI.PrototypeHUDController.IsSettingsOpen) return;
            EncounterTrigger encounter = tutorialPendingEncounter;
            GamePhase resume = tutorialResumePhase;
            bool startsBoss = tutorialTopic == "TutorialBoss";
            learnedTutorialTopics.Add(tutorialTopic);
            if (startsBoss)
            {
                BeginTutorialBossCombat();
                return;
            }
            SetPhase(resume);
            if (encounter != null && encounter.isActiveAndEnabled && !encounter.consumed)
                HandleEncounter(encounter);
        }

        private void ClearTutorialLesson()
        {
            tutorialPendingEncounter = null;
            tutorialTopic = null;
            CurrentTutorialLesson = null;
        }

        private void ResetTutorialLessons()
        {
            ClearTutorialLesson();
            learnedTutorialTopics.Clear();
        }
    }
}
