using System;
using UnityEngine;

namespace Player
{
    public class PawnAttributes : MonoBehaviour
    {
        public Action<float> OnHealthChange;
        public Action<float, float> OnDamageTaken;
        
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
                if (_hud != null)
                {
                    OnHealthChange += _hud.SetHealth;
                    OnDamageTaken += _hud.TakeDamage;
                }
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
            _health = Mathf.Clamp(health, 0, _maxHealth);
            OnHealthChange?.Invoke(GetHealthPercentage());
        }
        public float TakeDamage(float damage)
        {
            float oldHealthPercentage = GetHealthPercentage();
            _health = Mathf.Clamp(_health - damage, 0, _maxHealth);
            OnDamageTaken?.Invoke(oldHealthPercentage, GetHealthPercentage());
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
