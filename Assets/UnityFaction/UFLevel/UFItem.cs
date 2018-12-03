using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;

public class UFItem : MonoBehaviour {

    public ItemType type;
    public int count;
    public float respawnTime;

    private const float ROTATE_SPEED = 90;

    float respawnTimer;

    public enum ItemType {
        None, Gun, GunAmmo, SpecialWeapon, Explosive, ExplosiveAmmo,
        Health, Armor, SuperHealth, SuperArmor, Flag, Suit, Key,
        Invulnerability, DamageAmp
    }

    public void Set(Item item) {
        this.type = GetItemType(item.name);

        this.count = Mathf.Max(1, item.count);
        this.respawnTime = item.respawnTime;
        SphereCollider sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 1.5f;
    }

    private static ItemType GetItemType(string itemName) {
        itemName = itemName.Replace(' ', '_');

        switch(itemName) {
        case ".50cal_ammo":
        case "10gauge_ammo":
        case "12mm_ammo":
        case "5.56mm_ammo":
        case "7.62mm_ammo":
        case "explosive_5.56mm_rounds":
        case "railgun_bolts":
        return ItemType.GunAmmo;

        case "Assault_Rifle":
        case "Sniper_Rifle":
        case "Silenced_12mm_Handgun":
        case "Shotgun":
        case "scope_assault_rifle":
        case "heavy_machine_gun":
        case "Machine_Pistol":
        case "rail_gun":
        case "Handgun":
        return ItemType.Gun;

        case "grenades":
        case "shoulder_cannon":
        case "rocket_launcher":
        case "Remote_Charges":
        return ItemType.Explosive;

        case "rocket_launcher_ammo":
        return ItemType.ExplosiveAmmo;

        case "flamethrower":
        case "riot_shield":
        case "Napalm":
        case "Riot_Stick":
        case "riot_stick_battery":
        return ItemType.SpecialWeapon;

        case "flag_blue":
        case "flag_red":
        case "CTF_Banner_Blue":
        case "CTF_Banner_Red":
        case "base_blue":
        case "base_red":
        return ItemType.Flag;

        case "keycard":
        return ItemType.Key;

        case "Multi_Damage_Amplifier":
        return ItemType.DamageAmp;

        case "Multi_Invulnerability":
        return ItemType.Invulnerability;

        case "Multi_Super_Armor":
        return ItemType.SuperArmor;

        case "Multi_Super_Health":
        return ItemType.SuperHealth;

        case "First_Aid_Kit":
        case "Medical_Kit":
        return ItemType.Health;

        case "Suit_Repair":
        return ItemType.Armor;

        case "Miner_Envirosuit":
        case "Doctor_Uniform":
        return ItemType.Suit;

        case "Brainstem":
        case "Demo_K000":
        return ItemType.None;

        default:
        Debug.LogWarning("Encountered unkown item: " + itemName);
        return ItemType.None;
        }
    }

    private void OnTriggerStay(Collider other) {
        if(!IsPlayer(other))
            return;

        if(respawnTimer == 0f) {
            if(PickUp())
                respawnTimer = respawnTime;
        }
    }

    private void Update() {
        if(respawnTimer < 0f) {
            SetAttainable(false);
            return;
        }

        respawnTimer -= Time.deltaTime;
        if(respawnTimer < 0f)
            respawnTimer = 0f;

        SetAttainable(respawnTimer == 0f);

        transform.Rotate(0f, Time.deltaTime * ROTATE_SPEED, 0f);
    }

    private void SetAttainable(bool value) {
        for(int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(value);
    }

    private bool IsPlayer(Collider c) {
        UFTriggerSensor uts = c.GetComponent<UFTriggerSensor>();
        return uts != null && uts.IsPlayer();
    }

    private bool PickUp() {
        UFPlayerLife life = UFLevel.GetPlayer<UFPlayerLife>();
        if(life.isDead)
            return false;
        UFPlayerMoveSounds sound = life.GetComponentInChildren<UFPlayerMoveSounds>();

        switch(type) {

        case ItemType.Health:
        if(life.CanPickUpHealth()) {
            life.GainHealth(count);
            sound.PickUpPowerup();
            return true;
        }
        return false;

        case ItemType.Armor:
        if(life.CanPickUpArmor()) {
            life.GainArmor(count);
            sound.PickUpPowerup();
            return true;
        }
        return false;

        case ItemType.SuperHealth:
        sound.PickUpPowerup();
        life.SuperHealth();
        return true;

        case ItemType.SuperArmor:
        sound.PickUpPowerup();
        life.SuperArmor();
        return true;

        case ItemType.Invulnerability:
        sound.PickUpInvuln();
        life.Invulnerability();
        return true;

        case ItemType.Explosive:
        case ItemType.Gun:
        case ItemType.SpecialWeapon:
        bool weapon = UFLevel.GetPlayer<UFPlayerWeapons>().PickupWeapon(this);
        if(weapon)
            sound.PickUpWeapon();
        return weapon;

        case ItemType.ExplosiveAmmo:
        case ItemType.GunAmmo:
            bool ammo = UFLevel.GetPlayer<UFPlayerWeapons>().PickupAmmo(this);
        if(ammo)
            sound.PickUpWeapon();
        return ammo;

        case ItemType.DamageAmp:
        UFLevel.GetPlayer<UFPlayerWeapons>().DamageAmp();
        sound.PickUpDamageAmp();
        return true;

        default:
        Debug.LogWarning("Tried to collect unkown item type: " + type + " of item " + this.name);
        return false;
        }
    }

}
