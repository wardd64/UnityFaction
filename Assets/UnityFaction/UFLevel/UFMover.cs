using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;

public class UFMover : MonoBehaviour {

    private AudioSource sound;
    private Rigidbody rb;

    public UFLevelStructure.Keyframe[] keys;

    public bool isDoor, startsBackwards, rotateInPlace,
        useTravTimeAsSpd, forceOrient, noPlayerCollide;

    public MovingGroup.MovementType type;

    public AudioClip startClip, loopClip, stopClip, closeClip;
    public float startVol, loopVol, stopVol, closeVol;

    public void Set(MovingGroup group) {
        
        //assign variables
        keys = group.keys;
        type = group.type;

        //flags
        isDoor = group.isDoor;
        startsBackwards = group.startsBackwards;
        rotateInPlace = group.rotateInPlace;
        useTravTimeAsSpd = group.useTravTimeAsSpd;
        forceOrient = group.forceOrient;
        noPlayerCollide = group.noPlayerCollide;        

        //audio
        startVol = group.startVol;
        loopVol = group.loopVol;
        stopVol = group.stopVol;
        closeVol = group.closeVol;
        // (clips themselves are assigned by level builder)

        rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        Reset();
    }

    public void AddAt(Transform brush, PosRot pr) {
        brush.SetParent(this.transform);
        UFUtils.SetTransform(brush, pr);

        //pr is always relative to keys[0]
        if(!rotateInPlace && startsBackwards) {
            //shift position to the actual base key
            Vector3 at = keys[0].transform.posRot.position;
            Vector3 to = baseKey.transform.posRot.position;
            Vector3 delta = to - at;
            brush.transform.localPosition += to - at;
        }
    }

    public void AddAudio() {
        if(startClip == null && loopClip == null && closeClip == null && stopClip == null)
            return;
        sound = gameObject.AddComponent<AudioSource>();
        sound.spatialBlend = 1f;
        sound.playOnAwake = false;
    }

    private UFLevelStructure.Keyframe baseKey { get {
        return startsBackwards ? keys[keys.Length - 1] : keys[0];
    } }

    //dynamic variables
    bool moving;
    int lastKey; //index of last keyframe
    float time; //time since last keyframe
    bool forward, paused;
    bool completedSequence;
    Quaternion baseRot;

    private void Start() {
        rb = this.GetComponent<Rigidbody>();
        sound = this.GetComponent<AudioSource>();
        Reset();
        moving = true; //TODO set to false


        /*
        //flip travel times for rotators by speed
        if(rotateInPlace && useTravTimeAsSpd) {
            useTravTimeAsSpd = false;
            keys[0].departTravelTime = keys[0].rotationAmount / keys[0].departTravelTime;
            keys[0].returnTravelTime = keys[0].rotationAmount / keys[0].returnTravelTime;
        }
        */
    }

    public void Reset(bool preserveTime = false) {
        if(!preserveTime)
            time = 0f;
        lastKey = startsBackwards ? 0 : keys.Length - 1;
        forward = !startsBackwards;
        completedSequence = false;
        paused = false;
        baseRot = Quaternion.identity;

        rb.position = baseKey.transform.posRot.position;
        transform.position = baseKey.transform.posRot.position;

        if(rotateInPlace || forceOrient) {
            rb.rotation = Quaternion.identity;
            transform.rotation = Quaternion.identity;
        }
    }

    private void FixedUpdate() {
        if(!moving)
            return;

        if(!rotateInPlace && useTravTimeAsSpd) {
            SpeedBasedUpdate(Time.fixedDeltaTime);
        }
        else {
            time += Time.fixedDeltaTime;
            TimeBasedUpdate();
        }
        
    }

    private void SpeedBasedUpdate(float dt) {
        if(paused) {
            time += dt;
            if(PauseUpdate())
                return;
        }

        int nextKey = GetNextKey();
        float speed = GetNextTravTime(nextKey);

        Vector3 lastPos = rb.position;
        PosRot fro = keys[lastKey].transform.posRot;
        PosRot to = keys[nextKey].transform.posRot;

        float distFro = (lastPos - fro.position).magnitude;
        float accelT = keys[lastKey].accelTime;
        float accelDist = accelT * speed / 2f;
        bool accel = distFro < accelDist;

        float distTo = (to.position - lastPos).magnitude;
        float decelT = keys[nextKey].decelTime;
        float decelDist = decelT * speed / 2f;
        bool decel = distTo < decelDist;

        if(accel && decel) {
            //split overlapping situation
            float totalDist = distFro + distTo;
            float totalT = accelT + decelT;
            float split = totalDist * accelT / totalT;
            if(distFro < split)
                decel = false;
            else
                accel = false;
        }

        float distance;
        if(accel) {
            float t = Mathf.Sqrt(2f * distFro * accelT / speed) + dt;
            distance = (speed * t * t/ accelT / 2f) - distFro;
        }
        else if(decel) {
            float t = Mathf.Sqrt(2f * distTo * decelT / speed) - dt;
            distance = distTo - (speed * t * t / decelT / 2f);
        }
        else
            distance = speed * dt;

        //apply position and rotation
        rb.position = Vector3.MoveTowards(lastPos, to.position, distance);
        ForceOrient(to.position - lastPos);

        if(distTo < SNAP_DIST) {
            //keyframe is finished, wrap up
            lastKey = nextKey;
            int lastKeyInSeq = forward ? keys.Length - 1 : 0;
            if(AtLastKeyInSequence())
                FinishKeySequence();

            paused = CheckPause();
            //TODO do something with remaining distance to travel
        }
    }

    private const float SNAP_DIST = 1e-3f;

    private void TimeBasedUpdate() {
        if(PauseUpdate())
            return;

        if(rotateInPlace) {
            //rotations ignore all but the first key
            float travTime = forward ? keys[0].departTravelTime : keys[0].returnTravelTime;

            //get interpolation
            float r = Interp(time, travTime, 0, 0);
            float angle = r * (forward ? 1f : -1f) * keys[0].rotationAmount;

            //apply rotation
            Vector3 axis = keys[0].transform.posRot.rotation * Vector3.up;
            rb.rotation = Quaternion.AngleAxis(angle, axis) * baseRot;

            if(time > travTime) {
                //keyframe is finished, wrap up
                time -= travTime;
                FinishRotation();

                if(moving && keys[0].pauseTime > 0f)
                    paused = true;
                if(moving)
                    TimeBasedUpdate();
            }

        }
        else {
            int nextKey = GetNextKey();
            float travTime = GetNextTravTime(nextKey);

            //get interpolation point
            float r = Interp(time, travTime, lastKey, nextKey);
            PosRot fro = keys[lastKey].transform.posRot;
            PosRot to = keys[nextKey].transform.posRot;

            //apply position and rotation
            rb.position = Vector3.Lerp(fro.position, to.position, r);
            ForceOrient(to.position - fro.position);

            if(time > travTime) {
                //keyframe is finished, wrap up
                lastKey = nextKey;
                time -= travTime;
                if(AtLastKeyInSequence())
                    FinishKeySequence();

                paused = CheckPause();
                if(moving)
                    TimeBasedUpdate();
            }
        }
    }

    private void ForceOrient(Vector3 dir) {
        if(!forceOrient)
            return;

        rb.rotation = Quaternion.LookRotation(dir);
    }

    private bool CheckPause() {
        return moving && keys[lastKey].pauseTime > 0f;
    }

    private bool PauseUpdate() {
        if(!paused)
            return false;

        float pauseTime = keys[lastKey].pauseTime;
        if(time > pauseTime) {
            time -= pauseTime;
            paused = false;
            return false;
        }
        else
            return true;
    }

    private int GetNextKey() {
        if(forward) {
            if(lastKey == keys.Length - 1)
                return 0;
            else
                return lastKey + 1;
        }
        else {
            if(lastKey == 0)
                return keys.Length - 1;
            else
                return lastKey - 1;
        }
    }

    private float GetNextTravTime(int nextKey) {
        if(forward)
            return keys[lastKey].departTravelTime;
        else
            return keys[nextKey].returnTravelTime;
    }

    private float Interp(float time, float travTime, int accelKey, int decelKey) {
        float r = Mathf.Clamp01(time / travTime);
        float accel = keys[accelKey].accelTime;
        float decel = keys[decelKey].decelTime;

        if(accel > 0f || decel > 0f) {
            float easeIn = accel / travTime;
            float easeOut = decel / travTime;
            return Interpolator.FCBEF(r, easeIn, easeOut);
        }
        return r;
    }

    private bool AtLastKeyInSequence() {
        bool loopType = type == MovingGroup.MovementType.LoopOnce;
        loopType |= type == MovingGroup.MovementType.LoopInfinite;
        bool checkLast = forward;
        if(loopType)
            checkLast = !checkLast;

        if(checkLast)
            return lastKey == keys.Length - 1;
        else
            return lastKey == 0;        
    }

    private void FinishKeySequence() {
        switch(type) {
        case MovingGroup.MovementType.Lift:
        case MovingGroup.MovementType.OneWay:
        forward = !forward;
        moving = false;
        break;

        case MovingGroup.MovementType.PingPongOnce:
        if(!completedSequence) {
            forward = !forward;
            completedSequence = true;
        }
        else
            moving = false;
        break;

        case MovingGroup.MovementType.PingPongInfinite:
        forward = !forward;
        break;

        case MovingGroup.MovementType.LoopOnce:
        if(!completedSequence)
            completedSequence = true;
        else
            moving = false;
        break;

        case MovingGroup.MovementType.LoopInfinite: break;

        default:
        moving = false;
        Debug.LogWarning("Encountered unkown movement type: " + type);
        break;
        }
    }


    private void FinishRotation() {
        switch(type) {
        case MovingGroup.MovementType.Lift:
        case MovingGroup.MovementType.OneWay:
        baseRot = rb.rotation;
        forward = !forward;
        moving = false;
        break;

        case MovingGroup.MovementType.LoopOnce:
        case MovingGroup.MovementType.PingPongOnce:
        if(!completedSequence) {
            forward = !forward;
            baseRot = rb.rotation;
            completedSequence = true;
        }
        else
            moving = false;
        break;

        case MovingGroup.MovementType.PingPongInfinite:
        baseRot = forward ? rb.rotation : Quaternion.identity;
        forward = !forward;
        break;

        case MovingGroup.MovementType.LoopInfinite:
        baseRot = rb.rotation;
        break;

        default:
        moving = false;
        Debug.LogWarning("Encountered unkown movement type: " + type);
        break;
        }
    }

    private void PlayClip(AudioClip clip, float volume) {
        if(clip == null)
            return;

        sound.clip = clip;
        sound.volume = volume;
        sound.Play();
    }
}
