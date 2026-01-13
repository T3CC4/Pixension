using UnityEngine;

namespace Pixension.Mobs
{
    public class BanditMob : Mob
    {
        private Transform player;
        private float attackRange = 2f;
        private float attackCooldown = 1f;
        private float lastAttackTime = 0f;

        protected override void OnInitialize()
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        protected override void OnUpdate()
        {
            if (player == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange)
            {
                if (distanceToPlayer > attackRange)
                {
                    MoveTowardsPlayer();
                }
                else
                {
                    AttackPlayer();
                }
            }
        }

        private void MoveTowardsPlayer()
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;

            transform.position += direction * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        private void AttackPlayer()
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                Debug.Log($"Bandit {mobID} attacks player!");
            }
        }

        protected override void OnDeath()
        {
            Debug.Log($"Bandit {mobID} has died!");
            base.OnDeath();
        }
    }
}