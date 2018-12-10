using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageTool : PlayerTool {

    public Transform gunTip;

    public GameObject bulletEffect, bulletLinger;
    public Color[] colors;

    public AudioClip fireClip, switchClip;

    private int clrIndex;
    private float timer;

    private const float FIRE_COOLDOWN = 0.2f;
    private const float AMP_COOLDOWN = 0.05f;
    private const float EFFECT_OFFSET = 1e-2f;
    private const float BASE_DAMAGE = 40f;
    private const float AMP_DAMAGE = 60f;

    private void Start() {
        SetDetailColor();
    }

    public override void DoUpdate(bool mainFire, bool alt) {
        timer -= Time.deltaTime;
        if(timer < 0f)
            timer = 0f;

        if(mainFire) {
            while(timer <= 0f) {
                float cdup = amplified ? AMP_COOLDOWN : FIRE_COOLDOWN;
                timer += cdup;
                Fire();
            }
        }

        if(alt) {
            clrIndex = (clrIndex + 1) % colors.Length;
            SetDetailColor();
            sound.PlayOneShot(switchClip);
        }
    }

    private void SetDetailColor() {
        GetComponentInChildren<MeshRenderer>().materials[1].SetColor("_TintColor", colors[clrIndex]);
    }

    private void Fire() {
        anim.SetTrigger("Fire");

        Color clr = colors[clrIndex];
        Color fadeClr = new Color(clr.r, clr.g, clr.b, clr.a / 2f);

        GameObject effect = SFXHolder.Spawn(bulletEffect, gunTip.position);
        effect.GetComponentInChildren<LineRenderer>().material.SetColor("_TintColor", fadeClr);

        RaycastHit hit = Raycast(effect.transform);
        if(hit.collider != null) {
            Vector3 lingerPos = hit.point + EFFECT_OFFSET * hit.normal;
            Quaternion lingerRot = Quaternion.LookRotation(-hit.normal);

            Rigidbody lingerMover = hit.collider.GetComponentInParent<Rigidbody>();
            UFDestructible lingerDestr = hit.collider.GetComponent<UFDestructible>();
            Transform holder = null;
            if(lingerMover != null)
                holder = lingerMover.transform;
            else if(lingerDestr != null)
                holder = lingerDestr.transform;

            GameObject linger = SFXHolder.Spawn(bulletLinger, lingerPos, lingerRot, holder);
            linger.GetComponentInChildren<MeshRenderer>().material.SetColor("_TintColor", fadeClr);

            if(DealDamage(hit.collider.transform) && linger != null && linger.activeInHierarchy)
                linger.GetComponent<AudioSource>().Play();
        }

        sound.clip = fireClip;
        sound.Play();
    }

    private bool DealDamage(Transform target) {
        float damage = amplified ? AMP_DAMAGE : BASE_DAMAGE;

        UFTrigger trig = target.GetComponent<UFTrigger>();
        if(trig != null && trig.triggeredByWeapon) {
            trig.ExternalTrigger();
            return true;
        }

        UFDestructible dstr = target.GetComponent<UFDestructible>();
        if(dstr != null) {
            dstr.DealDamage(damage);
            return true;
        }

        UFClutter clut = target.GetComponentInParent<UFClutter>();
        if(clut != null && clut.life > 0f) {
            clut.Damage(damage);
            return true;
        }

        //no valid damage target
        return false;
    }

    protected override bool ValidTrigger(Collider trigger) {
        UFTrigger trig = trigger.GetComponent<UFTrigger>();
        return trig != null && trig.triggeredByWeapon && trig.resetsRemaining != 0;
    }
}
