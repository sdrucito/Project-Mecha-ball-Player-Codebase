using UnityEngine;

namespace Player
{
    public class PawnAttributes : MonoBehaviour
    {
        private float _health;
        private float _maxHealth = 100f;
    
        private bool _isDead;
        public void ResetMaxHealth()
        {
            _health = _maxHealth;
        }
        public void SetHealth(float health)
        {
            _health = health > _maxHealth ? _maxHealth : health;
        }
        public float TakeDamage(float damage)
        {
            _health -= damage;
            if (_health <= 0)
            {
                Die();
            }
            return _health;
        }
    
        // Don't know if it's needed
        public float GetHealthPercentage()
        {
            return (float)_health / _maxHealth;
        }
    
        public float GetHealth()
        {
            return _health;
        }

        private void Die()
        {
            Player.Instance.OnPlayerDeath?.Invoke();
            // TODO: Call GameManager and switch to death state
        }
    }
}
