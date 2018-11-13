using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UFPlayerLife : MonoBehaviour {

    protected float health, armor;
    protected float invulnTimer;
    protected bool invulnerable { get { return invulnTimer > 0f; } }
    protected bool initial;

    public const float MAX_HP = 100f;
    public const float SUPER_HP = 200f;
    private const float INVULN_TIME = 12f;

    protected virtual void Start() {
        SetBaseHealth();
    }

    protected virtual void Update() {
        if(invulnTimer > 0f) {
            invulnTimer -= Time.deltaTime;
            if(invulnTimer < 0f) {
                invulnTimer = 0f;
                initial = false;
            }
        }
    }

    protected void SetBaseHealth() {
        health = MAX_HP;
        armor = 0f;
        initial = true;
        invulnTimer = 1f;
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
        if(invulnerable && (!bypassInvuln || initial))
            return;

        if(health <= 0f)
            return;

        if(GetComponent<UFPlayerMovement>().IsNoClipping())
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
        GetComponentInChildren<UFPlayerMoveSounds>().Die();
        SetBaseHealth();
    }

    public void SuperHealth() {
        health = SUPER_HP;
    }

    public void SuperArmor() {
        armor = SUPER_HP;
    }

    public void Invulnerability() {
        if(initial)
            return;
        invulnTimer = INVULN_TIME;
    }

    public enum DamageType {
        Melee, Bullet, ArmorPiercing, Explosive, Fire, Energy, Electrical, Acid, Scalding
    }

    public float GetHealth() {
        return health;
    }

    public float GetArmor() {
        return armor;
    }
}
