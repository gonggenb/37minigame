using UnityEngine;
using WuxiaRoguelite.GameFlow;

namespace WuxiaRoguelite.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MainMapMusicController : MonoBehaviour
    {
        [Header("References")]
        public GameFlowController gameFlow;
        public AudioSource musicSource;

        [Header("Mix")]
        [Range(0f, 1f)]
        public float volume = 0.35f;

        private bool restartOnNextActivePhase = true;
        private float previousMainTime = -1f;

        private void Awake()
        {
            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
            }

            if (gameFlow == null)
            {
                gameFlow = GameFlowController.Instance;
            }

            ConfigureSource();
        }

        private void Start()
        {
            if (gameFlow == null)
            {
                gameFlow = GameFlowController.Instance;
            }

            SyncPlayback();
        }

        private void Update()
        {
            SyncPlayback();
        }

        private void OnValidate()
        {
            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
            }

            ConfigureSource();
        }

        private void ConfigureSource()
        {
            if (musicSource == null)
            {
                return;
            }

            musicSource.playOnAwake = false;
            musicSource.loop = false;
            musicSource.spatialBlend = 0f;
            musicSource.priority = 192;
            musicSource.volume = volume;
        }

        private void SyncPlayback()
        {
            if (gameFlow == null || musicSource == null || musicSource.clip == null)
            {
                return;
            }

            bool runRestarted = previousMainTime >= 0f &&
                                gameFlow.mainTimeRemaining > previousMainTime + 0.5f;
            if (runRestarted)
            {
                restartOnNextActivePhase = true;
            }

            GamePhase phase = gameFlow.CurrentPhase;
            bool mainTimerRunning =
                (phase == GamePhase.MainMapRunning || phase == GamePhase.NormalBattleRunning) &&
                gameFlow.mainTimeRemaining > 0f &&
                !gameFlow.IsCharacterMenuPaused;
            bool runFinished =
                phase == GamePhase.Ready ||
                phase == GamePhase.BossBattle ||
                phase == GamePhase.Result;

            if (mainTimerRunning)
            {
                if (restartOnNextActivePhase)
                {
                    musicSource.Stop();
                    musicSource.time = 0f;
                    musicSource.Play();
                    restartOnNextActivePhase = false;
                }
                else if (!musicSource.isPlaying)
                {
                    musicSource.UnPause();
                }
            }
            else if (runFinished)
            {
                if (musicSource.isPlaying || musicSource.time > 0f)
                {
                    musicSource.Stop();
                }

                restartOnNextActivePhase = true;
            }
            else if (musicSource.isPlaying)
            {
                musicSource.Pause();
            }

            previousMainTime = gameFlow.mainTimeRemaining;
        }
    }
}
