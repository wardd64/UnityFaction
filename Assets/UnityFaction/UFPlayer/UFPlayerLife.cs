using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UFPlayerLife : MonoBehaviour {

    protected float health, armor;
    protected float timer;
    protected bool invulnerable { get { return timer > 0f; } }

    const float MAX_HP = 100f;
    const float SUPER_HP = 200f;
    const float INVULN_TIME = 12f;

    protected virtual void Start() {
        SetBaseHealth();
    }

    protected virtual void Update() {
        if(timer > 0f) {
            timer -= Time.deltaTime;
            if(timer < 0f)
                timer = 0f;
        }
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

    public void TakeDamage(float amount, int type, bool bypassInvuln) {
        TakeDamage(amount, (DamageType)type, bypassInvuln);
    }

    public virtual void TakeDamage(float amount, DamageType type, bool bypassInvuln) {
        if(invulnerable && !bypassInvuln)
            return;

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

    public void Invulnerability() {
        timer = INVULN_TIME;
    }

    public enum DamageType {
        Melee, Bullet, ArmorPiercing, Explosive, Fire, Energy, Electrical, Acid, Scalding
    }
}
