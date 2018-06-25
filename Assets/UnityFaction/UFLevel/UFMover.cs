using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;

public class UFMover : MonoBehaviour {

    private AudioSource sound;
    private Rigidbody rb;

    public UFLevelStructure.Keyframe[] keys;
    public int startKey;

    public bool isDoor, startsBackwards, rotateInPlace,
        useTravTimeAsSpd, forceOrient, noPlayerCollide;

    public MovingGroup.MovementType type;

    public AudioClip startClip, loopClip, stopClip, closeClip;
    public float startVol, loopVol, stopVol, closeVol;

    public void Set(MovingGroup group) {
        
        //assign variables
        keys = group.keys;
        type = group.type;
        startKey = group.startIndex;

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

        ResetMotion();
    }

    public void AddAt(Transform brush, PosRot pr) {
        brush.SetParent(this.transform);
        UFUtils.SetTransform(brush, pr);

        //pr is always relative to keys[0]
        if(lastKey != 0) {
            //shift position to the actual starting key
            Vector3 at = keys[0].transform.posRot.position;
            Vector3 to = keys[lastKey].transform.posRot.position;
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

    //dynamic variables, used to control movement
    bool moving;
    int lastKey; //index of last keyframe
    float time; //time since last keyframe
    bool forward, paused;
    bool completedSequence;
    Quaternion baseRot;

    private void Start() {
        rb = this.GetComponent<Rigidbody>();
        sound = this.GetComponent<AudioSource>();
        ResetMotion();
        moving = true; //TODO set to false
    }

    /// <summary>
    /// Set motion state back to its starting condition.
    /// </summary>
    public void ResetMotion() {
        time = 0f;
        lastKey = startKey;
        forward = !startsBackwards;
        completedSequence = false;
        paused = false;
        baseRot = Quaternion.identity;

        rb.position = keys[startKey].transform.posRot.position;
        transform.position = keys[startKey].transform.posRot.position;

        if(rotateInPlace || forceOrient) {
            rb.rotation = Quaternion.identity;
            transform.rotation = Quaternion.identity;
        }
    }

    private void FixedUpdate() {
        if(!moving)
            return;

        time += Time.fixedDeltaTime;
        if(!PauseUpdate()) {
            if(rotateInPlace)
                RotateUpdate();
            else
                PathUpdate();
        }
    }

    /// <summary>
    /// Advance pause time, return true if movement is still paused
    /// </summary>
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

    /// <summary>
    /// Update movement as rotation around the first keyframe
    /// </summary>
    private void RotateUpdate() {
        //Get movement parameters (rotations ignore all but the first key)
        float travTime = forward ? keys[0].departTravelTime : keys[0].returnTravelTime;
        float angle = (forward ? 1f : -1f) * keys[0].rotationAmount;

        //Calculate movement
        float x = GetXByTravTime(time, travTime, angle, 0, 0);

        //apply rotation
        Vector3 axis = keys[0].transform.posRot.rotation * Vector3.up;
        rb.rotation = Quaternion.AngleAxis(x, axis) * baseRot;

        if(time > travTime) {
            //keyframe is finished, wrap up
            time -= travTime;
            FinishRotation();

            paused = moving && keys[lastKey].pauseTime > 0f;
            if(moving)
                RotateUpdate();
        }
    }

    /// <summary>
    /// Update movement as pathing from one keyframe to another
    /// </summary>
    private void PathUpdate() {
        //get movement parameters
        int nextKey = GetNextKey();
        PosRot fro = keys[lastKey].transform.posRot;
        PosRot to = keys[nextKey].transform.posRot;
        float distance = (to.position - fro.position).magnitude;
        float travTime = GetNextTravTime(nextKey);

        //calculate movement
        float x;
        if(useTravTimeAsSpd) {
            float speed = travTime;
            x = GetXBySpeed(time, speed, distance, lastKey, nextKey, out travTime);
        }
        else
            x = GetXByTravTime(time, travTime, distance, lastKey, nextKey);

        //apply position and rotation
        rb.position = Vector3.MoveTowards(fro.position, to.position, x);
        ForceOrient(to.position - fro.position);

        if(time > travTime) {
            //keyframe is finished, wrap up
            lastKey = nextKey;
            time -= travTime;
            if(AtLastKeyInSequence())
                FinishPath();

            paused = moving && keys[lastKey].pauseTime > 0f;
            if(moving)
                PathUpdate();
        }
    }

    /// <summary>
    /// Return distance that mover should have traveled from its last keyframe.
    /// In the case of rotation this returns the angle that should be covered.
    /// </summary>
    /// <param name="time">Time since last keyframe</param>
    /// <param name="travTime">Time to travel between keyframes</param>
    /// <param name="distance">Distance between keys (or angle)</param>
    /// <param name="accelKey">Key index that holds acceleration time</param>
    /// <param name="decelKey">Key index that holds deceleleration time</param>
    private float GetXByTravTime(float time, float travTime, float distance, int accelKey, int decelKey) {
        float Ta = keys[accelKey].accelTime;
        float Td = keys[decelKey].decelTime;
        return GetX_TX(time, Ta, Td, travTime, distance);
    }

    /// <summary>
    /// Return distance that mover should have traveled from its last keyframe.
    /// This version takes in a max speed value and returns the total travel time.
    /// </summary>
    /// <param name="time">Time since last keyframe</param>
    /// <param name="speed">Maximum speed that mover is allowed travel (can be degrees/second)</param>
    /// <param name="distance">Distance between keys (or angle)</param>
    /// <param name="accelKey">Key index that holds acceleration time</param>
    /// <param name="decelKey">Key index that holds deceleleration time</param>
    /// <param name="travTime">Total time needed to travel between keyframes</param>
    private float GetXBySpeed(float time, float speed, float distance, int accelKey, int decelKey, out float travTime) {
        float Ta = keys[accelKey].accelTime;
        float Td = keys[decelKey].decelTime;
        return GetX_VX(time, Ta, Td, distance, speed, out travTime);
    }

    /// <summary>
    /// Return interpolated x position based on total travel time and distance
    /// </summary>
    private float GetX_TX(float t, float Ta, float Td, float T, float X) {
        if(T <= 0f)
            return X;

        ConstrainAccelTimes(T, ref Ta, ref Td);
        float V = X / (T - ((Ta + Td) / 2f));

        return GetX_TV(t, Ta, Td, T, V);
    }

    /// <summary>
    /// Return interpolated x position based on maximum speed and total travel distance
    /// Also outputs the total travel time of the path that meets those conditions.
    /// </summary>
    private float GetX_VX(float t, float Ta, float Td, float X, float V, out float T) {
        if(V <= 0f) {
            T = float.PositiveInfinity;
            return 0f;
        }

        T = 2 * X / V;
        ConstrainAccelTimes(T, ref Ta, ref Td);
        T = X / V + ((Ta + Td) / 2f);

        return GetX_TV(t, Ta, Td, T, V);
    }

    /// <summary>
    /// Reduces referenced acceleration and deceleration times, so that 
    /// their sum is smaller than the given value maxSum.
    /// Negative values are set to 0 by default.
    /// </summary>
    private void ConstrainAccelTimes(float maxSum, ref float Ta, ref float Td) {
        Ta = Mathf.Max(0f, Ta);
        Td = Mathf.Max(0f, Td);

        float sum = Ta + Td;
        if(sum > maxSum) {
            Ta = maxSum * Ta / sum;
            Td = maxSum * Td / sum;
        }
    }

    /// <summary>
    /// Return interpolated x position based on maximum speed and total travel time.
    /// </summary>
    private float GetX_TV(float t, float Ta, float Td, float T, float V) {
        t = Mathf.Clamp(t, 0f, T);

        //calculate helper params
        float x1 = V * Ta / 2f;
        float x2 = V * (T - Ta - Td);
        float t1 = t - Ta;
        float t2 = t - T + Td;

        //calculate x
        if(t < Ta)
            return V * t * t / Ta / 2f;
        else if(t <= T - Td)
            return x1 + V * t1;
        else
            return x1 + x2 + (V * t2) - (V * t2 * t2 / Td / 2f);
    }

    /// <summary>
    /// If forcOrient == true this method will reorient this mover along the given travel direction
    /// </summary>
    private void ForceOrient(Vector3 dir) {
        if(!forceOrient)
            return;

        rb.rotation = Quaternion.LookRotation(dir);
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

    private void FinishPath() {
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
