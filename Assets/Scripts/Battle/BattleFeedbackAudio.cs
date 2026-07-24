using UnityEngine;

namespace WuxiaRoguelite.Battle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class BattleFeedbackAudio : MonoBehaviour
    {
        public enum FeedbackCue
        {
            None,
            PlayerImpact,
            EnemyImpact,
            Critical,
            Dodge
        }

        public BattleManager battleManager;
        [Range(0f, 1f)] public float masterVolume = 0.72f;
        [Header("Optional authored clips (procedural fallback when empty)")]
        public AudioClip swingSfx;
        public AudioClip impactSfx;
        public AudioClip criticalSfx;
        public AudioClip dodgeSfx;

        public int LastPlayedAttackSequence { get; private set; }
        public FeedbackCue LastCue { get; private set; }

        private const int SampleRate = 44100;

        private AudioSource audioSource;
        private enum CueShape
        {
            Swing,
            Impact,
            Critical,
            Dodge
        }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.priority = 64;
            audioSource.ignoreListenerPause = true;
            EnsureClips();
        }

        private void Update()
        {
            if (battleManager == null)
            {
                battleManager = GetComponent<BattleManager>();
            }

            if (battleManager == null || !battleManager.IsBattleActive || battleManager.AttackSequence <= 0)
            {
                LastPlayedAttackSequence = 0;
                LastCue = FeedbackCue.None;
                return;
            }

            if (LastPlayedAttackSequence == battleManager.AttackSequence)
            {
                return;
            }

            LastPlayedAttackSequence = battleManager.AttackSequence;
            EnsureClips();

            float pitchVariation = (LastPlayedAttackSequence % 5 - 2) * 0.018f;
            audioSource.pitch = (battleManager.LastAttackWasPlayer ? 1.04f : 0.94f) + pitchVariation;
            audioSource.PlayOneShot(swingSfx, masterVolume * (battleManager.LastAttackWasPlayer ? 0.22f : 0.16f));

            if (battleManager.LastAttackWasDodged)
            {
                audioSource.PlayOneShot(dodgeSfx, masterVolume * 0.38f);
                LastCue = FeedbackCue.Dodge;
                return;
            }

            if (battleManager.LastAttackWasCritical)
            {
                audioSource.pitch = 0.88f + pitchVariation;
                audioSource.PlayOneShot(criticalSfx, masterVolume);
                LastCue = FeedbackCue.Critical;
                return;
            }

            audioSource.PlayOneShot(impactSfx,
                masterVolume * (battleManager.LastAttackWasPlayer ? 0.72f : 0.58f));
            LastCue = battleManager.LastAttackWasPlayer ? FeedbackCue.PlayerImpact : FeedbackCue.EnemyImpact;
        }

        private void EnsureClips()
        {
            swingSfx ??= CreateClip("Battle Swing", 0.11f, CueShape.Swing);
            impactSfx ??= CreateClip("Battle Impact", 0.10f, CueShape.Impact);
            criticalSfx ??= CreateClip("Battle Critical Impact", 0.17f, CueShape.Critical);
            dodgeSfx ??= CreateClip("Battle Dodge", 0.12f, CueShape.Dodge);
        }

        private static AudioClip CreateClip(string clipName, float duration, CueShape shape)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            float[] samples = new float[sampleCount];
            float peak = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)SampleRate;
                float progress = i / (float)Mathf.Max(1, sampleCount - 1);
                float noiseSeed = Mathf.Sin(i * 12.9898f + (int)shape * 17.31f) * 43758.5453f;
                float noise = (noiseSeed - Mathf.Floor(noiseSeed)) * 2f - 1f;
                float sample;

                switch (shape)
                {
                    case CueShape.Swing:
                        float swingEnvelope = Mathf.Sin(progress * Mathf.PI) * (1f - progress);
                        float sweepFrequency = Mathf.Lerp(1050f, 360f, progress);
                        sample = (Mathf.Sin(2f * Mathf.PI * sweepFrequency * time) * 0.26f +
                                  noise * 0.44f) * swingEnvelope;
                        break;
                    case CueShape.Critical:
                        float criticalEnvelope = Mathf.Pow(1f - progress, 2.2f);
                        float criticalBody = Mathf.Sin(2f * Mathf.PI * 82f * time) * 0.72f;
                        float metal = Mathf.Sin(2f * Mathf.PI * 760f * time) * 0.24f;
                        sample = (criticalBody + metal + noise * 0.30f) * criticalEnvelope;
                        break;
                    case CueShape.Dodge:
                        float dodgeEnvelope = Mathf.Sin(progress * Mathf.PI) * (1f - progress * 0.6f);
                        float air = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(1250f, 1850f, progress) * time);
                        sample = (air * 0.18f + noise * 0.36f) * dodgeEnvelope;
                        break;
                    default:
                        float impactEnvelope = Mathf.Pow(1f - progress, 3f);
                        float body = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(150f, 72f, progress) * time);
                        sample = (body * 0.74f + noise * 0.36f) * impactEnvelope;
                        break;
                }

                samples[i] = sample;
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            }

            if (peak > 0.001f)
            {
                float normalization = 0.92f / peak;
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] *= normalization;
                }
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
