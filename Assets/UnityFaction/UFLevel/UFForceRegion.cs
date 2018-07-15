using System.Collections;
using System.Collections.Generic;
using UFLevelStructure;
using UnityEngine;

public class UFForceRegion : MonoBehaviour {

    private const float PLAYER_MASS = 100f;

    public ForceType type;

    public enum ForceType {
        Climb, SetVel, AddVel
    }

    public PushRegion.Profile profile;

    public ClimbingRegion.ClimbingType soundType;

    public float power;
    public bool massIndependant, radial, noPlayer, grounded;
    public int turbulence;
    public Vector3 forwardDir;

    //dynamic variables
    private UFPlayerMovement player;

    public void Set(ClimbingRegion climb) {
        AddTrigger(true, 0f, climb.cbTransform.extents);

        UFUtils.SetTransform(transform, climb.cbTransform.transform.posRot);

        soundType = climb.type;

        this.type = ForceType.Climb;
    }

    public void Set(PushRegion push) {
        bool aligned = push.shape == PushRegion.PushShape.alignedBox;
        bool box = aligned || push.shape == PushRegion.PushShape.orientedBox;
        AddTrigger(box, push.sphereRadius, push.extents);

        this.transform.position = push.transform.posRot.position;
        if(!aligned)
            this.transform.rotation = push.transform.posRot.rotation;
        forwardDir = push.transform.posRot.rotation * Vector3.forward;

        power = push.strength;
        noPlayer = push.noPlayer;
        massIndependant = push.massIndependent;
        radial = push.radial;
        grounded = push.grounded;
        turbulence = push.turbulence;

        if(push.jumpPad)
            this.type = ForceType.SetVel;
        else
            this.type = ForceType.AddVel;

        UFLevel.SetObject(push.transform.id, gameObject);
    }

    private void AddTrigger(bool box, float radius, Vector3 extents) {
        if(box) {
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            bc.size = extents;
        }
        else {
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = radius;
        }

        GetComponent<Collider>().isTrigger = true;
    }

    private void Update() {
        if(player == null)
            return;
        if(this.grounded && !player.grounded)
            return;

        Vector3 point = player.transform.position;

        switch(type) {

        case ForceType.Climb:
        player.ClimbState(transform.up);
        break;

        case ForceType.AddVel:
        player.ShiftVelocity(GetForce(point));
        break;

        case ForceType.SetVel:
        player.SetVelocity(GetForce(point));
        break;
        }
    }

    private Vector3 GetForce(Vector3 point) {
        float power;

        switch(profile) {

        case PushRegion.Profile.GrowsToBoundary:
        power = BorderFactor(point) * this.power;
        break;

        case PushRegion.Profile.GrowsToCenter:
        power = (1f - BorderFactor(point)) * this.power;
        break;

        default:
        power = this.power;
        break;
        }

        //if(!massIndependant)
        //    power /= PLAYER_MASS;

        Vector3 dir;
        if(radial)
            dir = (point - transform.position).normalized;
        else
            dir = forwardDir;

        return power * dir;
    }

    private float BorderFactor(Vector3 point) {
        SphereCollider s = this.GetComponent<SphereCollider>();
        BoxCollider b = this.GetComponent<BoxCollider>();

        if(s != null) {
            float r = (point - transform.position).magnitude;
            return r / s.radius;
        }

        Quaternion toAARot = Quaternion.Inverse(transform.rotation);
        Vector3 rel = toAARot * (point - transform.position);
        return rel.x * rel.y * rel.z / b.size.x / b.size.y / b.size.z;
    }

    private void OnTriggerEnter(Collider other) {
        UFPlayerMovement p = GetPlayer(other);
        if(p == null)
            return;
        player = p;
    }

    private void OnTriggerExit(Collider other) {
        if(GetPlayer(other) == null)
            return;
        player = null;
    }

    private UFPlayerMovement GetPlayer(Collider c) {
        if(noPlayer)
            return null;
        UFTriggerSensor uts = c.GetComponent<UFTriggerSensor>();
        if(uts == null || !uts.IsPlayer())
            return null;
        return c.transform.GetComponentInParent<UFPlayerMovement>();
    }
}
