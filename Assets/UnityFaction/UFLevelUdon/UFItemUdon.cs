
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UFItemUdon : UdonSharpBehaviour {

    public int type;
    public int count;
    public float respawnTime;

    private const float ROTATE_SPEED = 90;

    private AudioSource sound;
    private VRCPlayerApi localPlayer;
    private float respawnTimer;

    private void Start() {
        localPlayer = Networking.LocalPlayer;
        sound = GetComponent<AudioSource>();
    }

    public override void OnPlayerTriggerStay(VRCPlayerApi player) {
        if(respawnTimer == 0f) {
            if(PickUp()) {
                respawnTimer = respawnTime;
                sound.Play();
                SetAttainable(false);
            }
        }
    }

    private void Update() {
        if(respawnTimer < 0f)
            return;

        if(respawnTimer > 0f) {
            respawnTimer -= Time.deltaTime;
            if(respawnTimer <= 0f) {
                respawnTimer = 0f;
                SetAttainable(true);
            }
        }
        
        transform.Rotate(0f, Time.deltaTime * ROTATE_SPEED, 0f);
    }

    private void SetAttainable(bool value) {
        for(int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(value);
    }

    private bool PickUp() {

        float hp = localPlayer.CombatGetCurrentHitpoints();

        switch(type) {

        case 6: //health
        case 7: //armor
        if(hp < 100f) {
            localPlayer.CombatSetCurrentHitpoints(hp + count);
            return true;
        }
        return false;

        case 8: //superHealth
        case 9: //superArmor
        case 13: //invuln
        localPlayer.CombatSetCurrentHitpoints(hp + 100f);
        return true;

        //weapons can be physically picked up?

        case 4: //explosive
        case 1: //gun
        case 3: //special weapon
        case 5: //explosive ammo
        case 2: //gun ammo
        case 14: //damage amp
        return true;

        default:
        Debug.LogWarning("Tried to collect unkown item type: " + type + " of item " + name);
        return false;
        }
    }
}
