using System;
using UnityEngine;

namespace Player
{
    public class PawnAttributes : MonoBehaviour
    {
        public Action<float> OnHealthChange;
        
        private float _health;
        private float _maxHealth = 100f;
    
        private HUDUI _hud;
        public bool IsDead {get; private set;}

        public void InitAttributes()
        {
            ResetMaxHealth();
            // If HUD available, setup callbacks for HUD
            if (GameManager.Instance && GameManager.Instance.UIManager)
            {
                _hud = GameManager.Instance.UIManager.HudUI;
                if(_hud != null)
                    OnHealthChange += _hud.SetHealth;
            }
            ResetMaxHealth();
            IsDead = false;
        }
        
        public void ResetMaxHealth()
        {
            SetHealth(_maxHealth);
        }
        public void SetHealth(float health)
        {
            _health = health > _maxHealth ? _maxHealth : health;
            OnHealthChange?.Invoke(GetHealthPercentage());
        }
        public float TakeDamage(float damage)
        {
            SetHealth(_health-damage);
            if (_health <= 0)
            {
                Die();
            }
            return _health;
        }
    
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
            IsDead = true;
            // TODO: Call GameManager and switch to death state
            Player.Instance.Die();
        }

        private void OnDisable()
        {
            if (GameManager.Instance && GameManager.Instance.UIManager)
            {
                _hud = GameManager.Instance.UIManager.HudUI;
                if(_hud != null)
                    OnHealthChange -= _hud.SetHealth;
            }
        }
    }
}
