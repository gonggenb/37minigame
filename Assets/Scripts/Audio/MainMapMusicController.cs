using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.GameFlow;

namespace WuxiaRoguelite.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MainMapMusicController : MonoBehaviour
    {
        [Header("Runtime References")]
        public GameFlowController gameFlow;
        public BattleManager battleManager;
        public AudioSource musicSource;
        public AudioSource overlaySource;
        public AudioSource specialMusicSource;
        public AudioSource stingerSource;

        [Header("Music Assets")]
        public AudioClip normalBattleStem;
        public AudioClip caveMusic;
        public AudioClip caveBattleStem;
        public AudioClip bossIntro;
        public AudioClip bossMusic;
        public AudioClip bossMomentumStem;
        public AudioClip bossEnrageStem;
        public AudioClip victoryStinger;
        public AudioClip defeatStinger;

        [Header("Mix")]
        [Range(0f, 1f)] public float volume = 0.35f;
        [Range(0f, 1f)] public float overlayVolume = 0.2f;
        [Range(0f, 1f)] public float specialMusicVolume = 0.38f;
        [Range(0f, 1f)] public float stingerVolume = 0.55f;
        [Min(0.01f)] public float overlayFadeInSeconds = 0.15f;
        [Min(0.01f)] public float overlayFadeOutSeconds = 0.25f;
        [Range(0.05f, 0.95f)] public float bossMomentumHealthRatio = 0.72f;
        [Range(0.05f, 0.95f)] public float bossEnrageHealthRatio = 0.35f;

        public string ActiveMusicState { get; private set; } = "Ready";
        public bool MusicEnabled { get; private set; } = true;
        public string ActiveOverlayName =>
            overlaySource != null && overlaySource.clip != null && overlayTargetVolume > 0f
                ? overlaySource.clip.name
                : string.Empty;

        private const string MusicEnabledPreference = "Settings.MusicEnabled";
        private GamePhase previousPhase = (GamePhase)(-1);
        private bool restartMainMusicOnNextRun = true;
        private bool mainMusicPaused;
        private bool overlayPaused;
        private bool specialMusicPaused;
        private bool stingerPaused;
        private bool bossIntroActive;
        private bool resultStingerPlayed;
        private float previousMainTime = -1f;
        private float overlayTargetVolume;
        private GamePhase levelUpMusicContext = GamePhase.MainMapRunning;

        private void Awake()
        {
            ResolveReferences(true);
            ConfigureSources();
            MusicEnabled = PlayerPrefs.GetInt(MusicEnabledPreference, 1) != 0;
            ApplyMusicEnabledState();
        }

        private void Start()
        {
            ResolveReferences(false);
            SyncPlayback();
        }

        private void Update()
        {
            SyncPlayback();
        }

        private void OnValidate()
        {
            ResolveReferences(false);
            ConfigureSources();
            ApplyMusicEnabledState();
        }

        public void SetMusicEnabled(bool enabled)
        {
            MusicEnabled = enabled;
            ApplyMusicEnabledState();
            PlayerPrefs.SetInt(MusicEnabledPreference, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplyMusicEnabledState()
        {
            bool muted = !MusicEnabled;
            SetMuted(musicSource, muted);
            SetMuted(overlaySource, muted);
            SetMuted(specialMusicSource, muted);
            SetMuted(stingerSource, muted);
        }

        private static void SetMuted(AudioSource source, bool muted)
        {
            if (source != null)
            {
                source.mute = muted;
            }
        }

        private void ResolveReferences(bool createMissingSources)
        {
            if (gameFlow == null)
            {
                gameFlow = GameFlowController.Instance;
            }

            if (battleManager == null && gameFlow != null)
            {
                battleManager = gameFlow.battleManager;
            }

            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
            }

            if (!createMissingSources)
            {
                return;
            }

            overlaySource = overlaySource != null ? overlaySource : gameObject.AddComponent<AudioSource>();
            specialMusicSource = specialMusicSource != null
                ? specialMusicSource
                : gameObject.AddComponent<AudioSource>();
            stingerSource = stingerSource != null ? stingerSource : gameObject.AddComponent<AudioSource>();
        }

        private void ConfigureSources()
        {
            ConfigureSource(musicSource, 192, false, volume);
            ConfigureSource(overlaySource, 184, true, overlayVolume);
            ConfigureSource(specialMusicSource, 188, false, specialMusicVolume);
            ConfigureSource(stingerSource, 160, false, stingerVolume);
        }

        private static void ConfigureSource(AudioSource source, int priority, bool loop, float configuredVolume)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.priority = priority;
            source.volume = Mathf.Clamp01(configuredVolume);
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
                restartMainMusicOnNextRun = true;
                resultStingerPlayed = false;
            }

            GamePhase phase = gameFlow.CurrentPhase;
            if (phase == GamePhase.LevelUpPaused && previousPhase != GamePhase.LevelUpPaused)
            {
                levelUpMusicContext = previousPhase == GamePhase.CaveRunning
                    ? GamePhase.CaveRunning
                    : GamePhase.MainMapRunning;
            }

            switch (phase)
            {
                case GamePhase.MainMapRunning:
                    PlayMainMapMusic();
                    SetOverlay(null);
                    StopSpecialMusic();
                    ActiveMusicState = "MainMap";
                    break;

                case GamePhase.OpeningIntro:
                    PlayMainMapMusic();
                    SetOverlay(null);
                    StopSpecialMusic();
                    ActiveMusicState = "OpeningIntro";
                    break;

                case GamePhase.NormalBattleRunning:
                    PlayMainMapMusic();
                    SetOverlay(normalBattleStem);
                    StopSpecialMusic();
                    ActiveMusicState = "NormalBattle";
                    break;

                case GamePhase.CaveRunning:
                    PauseSource(musicSource, ref mainMusicPaused);
                    PlaySpecialLoop(caveMusic);
                    SetOverlay(battleManager != null && battleManager.IsBattleActive ? caveBattleStem : null);
                    ActiveMusicState = battleManager != null && battleManager.IsBattleActive
                        ? "CaveBattle"
                        : "Cave";
                    break;

                case GamePhase.LevelUpPaused:
                    SetOverlay(null);
                    if (levelUpMusicContext == GamePhase.CaveRunning)
                    {
                        PauseSource(musicSource, ref mainMusicPaused);
                        PlaySpecialLoop(caveMusic);
                        ActiveMusicState = "CaveChoice";
                    }
                    else
                    {
                        PlayMainMapMusic();
                        StopSpecialMusic();
                        ActiveMusicState = "MainMapChoice";
                    }

                    break;

                case GamePhase.BossBattle:
                    StopSource(musicSource, ref mainMusicPaused, true);
                    PlayBossMusic();
                    UpdateBossIntensity();
                    break;

                case GamePhase.Result:
                    StopSource(musicSource, ref mainMusicPaused, true);
                    StopOverlayImmediately();
                    StopSpecialMusic();
                    PlayResultStinger();
                    restartMainMusicOnNextRun = true;
                    ActiveMusicState = gameFlow.bossDefeated ? "Victory" : "Defeat";
                    break;

                default:
                    StopSource(musicSource, ref mainMusicPaused, true);
                    StopOverlayImmediately();
                    StopSpecialMusic();
                    StopSource(stingerSource, ref stingerPaused, true);
                    restartMainMusicOnNextRun = true;
                    ActiveMusicState = "Ready";
                    break;
            }

            UpdateOverlayFade();
            previousPhase = phase;
            previousMainTime = gameFlow.mainTimeRemaining;
        }

        private void PlayMainMapMusic()
        {
            if (restartMainMusicOnNextRun)
            {
                musicSource.Stop();
                musicSource.time = 0f;
                musicSource.loop = false;
                musicSource.volume = volume;
                musicSource.Play();
                mainMusicPaused = false;
                restartMainMusicOnNextRun = false;
                resultStingerPlayed = false;
                return;
            }

            ResumeOrPlay(musicSource, ref mainMusicPaused);
        }

        private void PlaySpecialLoop(AudioClip clip)
        {
            if (clip == null || specialMusicSource == null)
            {
                return;
            }

            if (specialMusicSource.clip != clip)
            {
                specialMusicSource.Stop();
                specialMusicSource.clip = clip;
                specialMusicSource.loop = true;
                specialMusicSource.volume = specialMusicVolume;
                specialMusicSource.Play();
                specialMusicPaused = false;
                bossIntroActive = false;
                return;
            }

            ResumeOrPlay(specialMusicSource, ref specialMusicPaused);
        }

        private void PlayBossMusic()
        {
            if (specialMusicSource == null || bossMusic == null)
            {
                return;
            }

            if (previousPhase != GamePhase.BossBattle)
            {
                specialMusicSource.Stop();
                specialMusicSource.clip = bossIntro != null ? bossIntro : bossMusic;
                specialMusicSource.loop = bossIntro == null;
                specialMusicSource.volume = specialMusicVolume;
                specialMusicSource.Play();
                specialMusicPaused = false;
                bossIntroActive = bossIntro != null;
                return;
            }

            if (bossIntroActive && !specialMusicSource.isPlaying && !specialMusicPaused)
            {
                specialMusicSource.clip = bossMusic;
                specialMusicSource.loop = true;
                specialMusicSource.volume = specialMusicVolume;
                specialMusicSource.Play();
                bossIntroActive = false;
                return;
            }

            ResumeOrPlay(specialMusicSource, ref specialMusicPaused);
        }

        private void StopSpecialMusic()
        {
            StopSource(specialMusicSource, ref specialMusicPaused, true);
            bossIntroActive = false;
        }

        private void UpdateBossIntensity()
        {
            bool bossActive = battleManager != null &&
                              battleManager.IsBattleActive &&
                              battleManager.currentEnemy != null;
            if (!bossActive)
            {
                SetOverlay(null);
                ActiveMusicState = bossIntroActive ? "BossIntro" : "Boss";
                return;
            }

            float healthRatio = battleManager.currentEnemy.HealthRatio;
            if (healthRatio <= bossEnrageHealthRatio && bossEnrageStem != null)
            {
                SetOverlay(bossEnrageStem, true);
                ActiveMusicState = "BossClimax";
                return;
            }

            if (healthRatio <= bossMomentumHealthRatio && bossMomentumStem != null)
            {
                SetOverlay(bossMomentumStem, true);
                ActiveMusicState = "BossMomentum";
                return;
            }

            SetOverlay(null);
            ActiveMusicState = "Boss";
        }

        private void SetOverlay(AudioClip clip, bool syncToSpecialMusic = false)
        {
            if (overlaySource == null)
            {
                return;
            }

            if (clip == null)
            {
                if (overlayPaused)
                {
                    StopOverlayImmediately();
                    return;
                }

                overlayTargetVolume = 0f;
                return;
            }

            if (overlaySource.clip != clip)
            {
                float synchronizedTime = 0f;
                if (syncToSpecialMusic &&
                    specialMusicSource != null &&
                    specialMusicSource.isPlaying &&
                    clip.length > 0.01f)
                {
                    synchronizedTime = specialMusicSource.time % clip.length;
                }

                overlaySource.Stop();
                overlaySource.clip = clip;
                overlaySource.loop = true;
                overlaySource.volume = 0f;
                overlaySource.time = Mathf.Clamp(synchronizedTime, 0f, Mathf.Max(0f, clip.length - 0.01f));
                overlaySource.Play();
                overlayPaused = false;
            }
            else
            {
                ResumeOrPlay(overlaySource, ref overlayPaused);
            }

            overlayTargetVolume = overlayVolume;
        }

        private void UpdateOverlayFade()
        {
            if (overlaySource == null || overlayPaused)
            {
                return;
            }

            float fadeSeconds = overlayTargetVolume > overlaySource.volume
                ? overlayFadeInSeconds
                : overlayFadeOutSeconds;
            float speed = Mathf.Max(overlayVolume, 0.01f) / Mathf.Max(fadeSeconds, 0.01f);
            overlaySource.volume = Mathf.MoveTowards(
                overlaySource.volume,
                overlayTargetVolume,
                speed * Time.unscaledDeltaTime);

            if (overlayTargetVolume <= 0f && overlaySource.volume <= 0.001f)
            {
                StopOverlayImmediately();
            }
        }

        private void StopOverlayImmediately()
        {
            overlayTargetVolume = 0f;
            StopSource(overlaySource, ref overlayPaused, true);
            if (overlaySource != null)
            {
                overlaySource.volume = 0f;
            }
        }

        private void PlayResultStinger()
        {
            if (resultStingerPlayed || stingerSource == null)
            {
                return;
            }

            AudioClip clip = gameFlow.bossDefeated ? victoryStinger : defeatStinger;
            if (clip != null)
            {
                stingerSource.Stop();
                stingerSource.clip = clip;
                stingerSource.loop = false;
                stingerSource.volume = stingerVolume;
                stingerSource.Play();
                stingerPaused = false;
            }

            resultStingerPlayed = true;
        }

        private static void PauseSource(AudioSource source, ref bool pausedByDirector)
        {
            if (source == null || source.clip == null || pausedByDirector)
            {
                return;
            }

            source.Pause();
            pausedByDirector = true;
        }

        private static void ResumeOrPlay(AudioSource source, ref bool pausedByDirector)
        {
            if (source == null || source.clip == null)
            {
                return;
            }

            if (pausedByDirector)
            {
                source.UnPause();
                pausedByDirector = false;
            }
            else if (!source.isPlaying)
            {
                source.Play();
            }
        }

        private static void StopSource(AudioSource source, ref bool pausedByDirector, bool resetTime)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            if (resetTime && source.clip != null)
            {
                source.time = 0f;
            }

            pausedByDirector = false;
        }
    }
}
