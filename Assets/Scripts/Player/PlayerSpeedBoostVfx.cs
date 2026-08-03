using UnityEngine;

namespace WuxiaRoguelite.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController), typeof(PlayerStats))]
    public class PlayerSpeedBoostVfx : MonoBehaviour
    {
        public PlayerController playerController;
        public PlayerStats playerStats;
        public Texture2D immortalQiTexture;

        public bool IsEffectEmitting => effectSystem != null && effectSystem.isEmitting;

        private ParticleSystem effectSystem;
        private Material runtimeMaterial;

        private void Awake()
        {
            playerController = playerController == null ? GetComponent<PlayerController>() : playerController;
            playerStats = playerStats == null ? GetComponent<PlayerStats>() : playerStats;
            CreateEffectSystem();
        }

        private void Update()
        {
            if (effectSystem == null)
            {
                return;
            }

            bool shouldEmit = playerStats != null &&
                              playerStats.ActiveTemporaryMoveSpeedBuffCount > 0 &&
                              playerController != null &&
                              playerController.IsMoving;
            if (shouldEmit)
            {
                if (!effectSystem.isPlaying)
                {
                    effectSystem.Play();
                }
            }
            else if (effectSystem.isPlaying)
            {
                effectSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void CreateEffectSystem()
        {
            GameObject effectObject = new GameObject("Immortal Qi Trail");
            effectObject.transform.SetParent(transform, false);
            effectObject.transform.localPosition = new Vector3(0f, 0.24f, 0f);

            effectSystem = effectObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = effectSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.48f, 0.72f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.42f, 0.68f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = 24;

            ParticleSystem.EmissionModule emission = effectSystem.emission;
            emission.rateOverTime = 14f;
            emission.rateOverDistance = 0f;

            ParticleSystem.ShapeModule shape = effectSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
            shape.radiusThickness = 1f;

            ParticleSystem.VelocityOverLifetimeModule velocity = effectSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.24f, 0.42f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = effectSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.78f, 0.12f),
                    new GradientAlphaKey(0.48f, 0.68f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = effectSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.55f),
                    new Keyframe(0.22f, 1f),
                    new Keyframe(1f, 0.72f)));

            ParticleSystemRenderer effectRenderer = effectObject.GetComponent<ParticleSystemRenderer>();
            effectRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            effectRenderer.alignment = ParticleSystemRenderSpace.View;
            effectRenderer.sortingFudge = -0.15f;

            Shader shader = Shader.Find("Sprites/Default");
            shader = shader == null ? Shader.Find("Particles/Standard Unlit") : shader;
            shader = shader == null ? Shader.Find("Universal Render Pipeline/Particles/Unlit") : shader;
            if (shader != null)
            {
                runtimeMaterial = new Material(shader)
                {
                    name = "Runtime Immortal Qi Material",
                    mainTexture = immortalQiTexture
                };
                effectRenderer.material = runtimeMaterial;
            }

            effectSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }
    }
}
