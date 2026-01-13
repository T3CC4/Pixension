using UnityEngine;

namespace Pixension.Mobs
{
    public class Mob : MonoBehaviour
    {
        public string mobID;
        public Vector3 spawnPosition;
        public float health = 100f;
        public float maxHealth = 100f;
        public float moveSpeed = 3f;
        public float detectionRange = 10f;

        protected bool isAlive = true;

        public virtual void Initialize(string id, Vector3 position)
        {
            mobID = id;
            spawnPosition = position;
            health = maxHealth;
            isAlive = true;

            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }

        protected virtual void Update()
        {
            if (!isAlive) return;

            OnUpdate();
        }

        protected virtual void OnUpdate()
        {
        }

        public virtual void TakeDamage(float damage)
        {
            if (!isAlive) return;

            health -= damage;

            if (health <= 0)
            {
                health = 0;
                Die();
            }

            OnDamaged(damage);
        }

        protected virtual void OnDamaged(float damage)
        {
        }

        public virtual void Die()
        {
            if (!isAlive) return;

            isAlive = false;
            OnDeath();
        }

        protected virtual void OnDeath()
        {
            Destroy(gameObject, 2f);
        }

        public virtual void Heal(float amount)
        {
            if (!isAlive) return;

            health = Mathf.Min(health + amount, maxHealth);
        }

        public bool IsAlive()
        {
            return isAlive;
        }

        public float GetHealthPercent()
        {
            return health / maxHealth;
        }
    }
}