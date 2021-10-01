
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class UFForceRegionUdon : UdonSharpBehaviour
{

    private const float PLAYER_MASS = 100f;

    public int forceType;
    public int profile;
    public int soundType;

    public float power;
    public bool massIndependant, radial, noPlayer, grounded;
    public int turbulence;
    public Vector3 forwardDir;

    //signalling
    public int signal;

    private VRCPlayerApi localPlayer;
    private bool playerInside;

    private void Start() {
        localPlayer = Networking.LocalPlayer;
    }

    private void Update() {
        if(signal != 0) {
            Activate(signal > 0);
            signal = 0;
        }
        
        if(!playerInside)
            return;
        if(grounded && !localPlayer.IsPlayerGrounded())
            return;

        Vector3 point = localPlayer.GetPosition();
        
        switch(forceType) {

        case 0: //climbing region; push player up
        Vector3 v = localPlayer.GetVelocity();
        v = new Vector3(v.x, 2f, v.z);
        localPlayer.SetVelocity(v);
        break;

        case 1: //Jump pad (set velocity)
        localPlayer.SetVelocity(GetForce(point));
        break;

        case 2: //push region (shove player along)
        Vector3 p = localPlayer.GetPosition();
        Quaternion q = localPlayer.GetRotation();
        p += Time.deltaTime * GetForce(point);
        localPlayer.TeleportTo(p, q);
        break;
        }
    }

    private Vector3 GetForce(Vector3 point)
    {
        float power;

        switch(profile)
        {

        case 1: //grows to boundary
        power = BorderFactor(point) * this.power;
        break;

        case 2: //grows to center
        power = (1f - BorderFactor(point)) * this.power;
        break;

        default: //uniform
        power = this.power;
        break;
        }

        if(!massIndependant)
            power /= PLAYER_MASS;

        Vector3 dir;
        if(radial)
            dir = (point - transform.position).normalized;
        else
            dir = forwardDir;

        return power * dir;
    }

    private float BorderFactor(Vector3 point)
    {
        SphereCollider s = this.GetComponent<SphereCollider>();
        BoxCollider b = this.GetComponent<BoxCollider>();

        if(s != null)
        {
            float r = (point - transform.position).magnitude;
            return r / s.radius;
        }

        Quaternion toAARot = Quaternion.Inverse(transform.rotation);
        Vector3 rel = toAARot * (point - transform.position);
        return rel.x * rel.y * rel.z / b.size.x / b.size.y / b.size.z;
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player) {
        playerInside |= player.isLocal;
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player) {
        playerInside &= !player.isLocal;
    }

    public void Activate(bool positive) {
        foreach(Collider c in GetComponentsInChildren<Collider>())
            c.enabled = positive;
         playerInside &= positive;
    }
}
