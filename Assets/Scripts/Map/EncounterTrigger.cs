using UnityEngine;
using WuxiaRoguelite.Cave;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.Map
{
    [RequireComponent(typeof(Collider))]
    public class EncounterTrigger : MonoBehaviour
    {
        public EncounterType encounterType = EncounterType.NormalEnemy;
        public CombatantStats enemyStats = new CombatantStats
        {
            displayName = "山贼喽啰",
            maxHealth = 35f,
            currentHealth = 35f,
            attack = 5f,
            defense = 1f,
            attackSpeed = 0.9f
        };
        public int cultivationReward = 10;
        public int copperReward = 2;

        [Header("Map Pickup")]
        public float healRatio = 0.35f;
        public HerbEffectType herbEffect = HerbEffectType.Heal;
        [Min(0f)] public float herbBuffValue = 0.12f;
        [Min(0f)] public float visionIncrease = 0.14f;
        [Min(0)] public int mysteryCultivationReward = 45;
        [Range(0f, 1f)] public float mysteryPoisonChance = 0.25f;
        [Range(0f, 1f)] public float mysteryDebuffChance = 0.25f;
        [Range(0f, 0.95f)] public float mysteryHealthLossRatio = 0.25f;

        [Header("Cave")]
        public CaveContentType caveContent = CaveContentType.Random;
        public bool consumed;

        private bool waitForPlayerExit;
        private bool hasResolvedCaveContent;
        private CaveContentType resolvedCaveContent = CaveContentType.Random;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (consumed || waitForPlayerExit || other.GetComponent<PlayerController>() == null)
            {
                return;
            }

            GameFlowController controller = GameFlowController.Instance;
            if (controller != null)
            {
                controller.HandleEncounter(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (waitForPlayerExit && other.GetComponent<PlayerController>() != null)
            {
                waitForPlayerExit = false;
            }
        }

        public CombatantStats CreateEnemyStats()
        {
            CombatantStats clone = enemyStats.Clone();
            clone.ResetHealth();
            return clone;
        }

        public CaveContentType ResolveCaveContent(System.Func<CaveContentType> randomResolver)
        {
            if (caveContent != CaveContentType.Random)
            {
                return caveContent;
            }

            if (!hasResolvedCaveContent)
            {
                resolvedCaveContent = randomResolver != null
                    ? randomResolver()
                    : CaveContentType.Enemy;
                hasResolvedCaveContent = true;
            }

            return resolvedCaveContent;
        }

        public void Consume()
        {
            consumed = true;
            gameObject.SetActive(false);
        }

        public void ResetEncounter(bool requirePlayerExit = false, bool rerollCaveContent = false)
        {
            if (rerollCaveContent)
            {
                hasResolvedCaveContent = false;
                resolvedCaveContent = CaveContentType.Random;
            }

            consumed = false;
            waitForPlayerExit = requirePlayerExit;
            gameObject.SetActive(true);
        }
    }
}
