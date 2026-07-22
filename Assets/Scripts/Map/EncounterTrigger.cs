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
        public float healRatio = 0.35f;
        public CaveContentType caveContent = CaveContentType.Random;
        public bool consumed;

        private bool waitForPlayerExit;

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

        public void Consume()
        {
            consumed = true;
            gameObject.SetActive(false);
        }

        public void ResetEncounter(bool requirePlayerExit = false)
        {
            consumed = false;
            waitForPlayerExit = requirePlayerExit;
            gameObject.SetActive(true);
        }
    }
}
