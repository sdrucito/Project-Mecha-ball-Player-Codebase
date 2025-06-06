using System;
using UnityEngine;

public class PlayerAttributes : MonoBehaviour
{
    private int _health;
    private int _maxHealth;

    private void Start()
    {
        
    }

    public void ResetMaxHealth()
    {
        _health = _maxHealth;
    }
    public void SetHealth(int health)
    {
        _health = health > _maxHealth ? _maxHealth : health;
    }
    public int TakeDamage(int damage)
    {
        _health -= damage;
        return _health;
    }
    
    // Don't know if it's needed
    public float GetHealthPercentage()
    {
        return (float)_health / _maxHealth;
    }
    
    public int GetHealth()
    {
        return _health;
    }
}
