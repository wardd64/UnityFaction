using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UFPlayerLife : MonoBehaviour {

    protected float health, armor;

    const float MAX_HP = 100f;
    const float SUPER_HP = 200f;

    protected virtual void Start() {
        SetBaseHealth();
    }

    protected void SetBaseHealth() {
        health = MAX_HP;
        armor = 0f;
    }

    public bool CanPickUpHealth() {
        return health < MAX_HP;
    }

    public void GainHealth(float amount) {
        health = Mathf.Min(health + amount, MAX_HP);
    }

    public bool CanPickUpArmor() {
        return armor < MAX_HP;
    }

    public void GainArmor(float amount) {
        armor = Mathf.Min(armor + amount, MAX_HP);
    }

    public virtual void TakeDamage(float amount) {
        if(health <= 0f)
            return;

        float damage = Mathf.Min(amount, armor);
        armor -= damage; amount -= damage;

        damage = Mathf.Min(amount, health);
        health -= damage; amount -= damage;

        if(amount > 0f)
            Die();
    }

    protected virtual void Die() {
        GetComponent<UFPlayerMovement>().Spawn();
        SetBaseHealth();
    }

    public void SuperHealth() {
        health = SUPER_HP;
    }

    public void SuperArmor() {
        armor = SUPER_HP;
    }
}
