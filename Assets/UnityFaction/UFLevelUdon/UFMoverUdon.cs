
using System;
using UdonSharp;
using UFLevelStructure;
using UnityEngine;
using UnityEngine.Audio;
using VRC.SDKBase;
using VRC.Udon;

public class UFMoverUdon : UdonSharpBehaviour
{
    private AudioSource sound;
    private Rigidbody rb;

    //keyframe data
    public Transform[] kf;
    public float[] kf_pauseTime, kf_departTravelTime, kf_returnTravelTime, 
        kf_accelTime, kf_decelTime, kf_rotationAmount;

    public Rigidbody[] content;
    public int startKey;

    public int[] links;

    public bool isDoor, startsBackwards, rotateInPlace,
        useTravTimeAsSpd, forceOrient, noPlayerCollide;

    public int type;

    public AudioClip startClip, loopClip, stopClip, closeClip;
    public float startVol, loopVol, stopVol, closeVol;

    private const float MIN_TRAV_TIME = 1e-2f;

    //dynamic variables, used to control movement
    private bool moving;
    private int lastKey; //index of last keyframe
    private float time; //time since last keyframe
    private bool forward, paused;
    private bool completedSequence;
    private Quaternion baseRot;

    //algo variables to avoid out and ref
    private float T;
    private float Ta, Td;

    private void Start()
    {
        rb = this.GetComponent<Rigidbody>();
        sound = this.GetComponent<AudioSource>();
        
        ResetMotion();
        Activate(true);
    }

    /// <summary>
    /// Set motion state back to its starting condition.
    /// </summary>
    public void ResetMotion()
    {
        time = 0f;
        lastKey = startKey;
        forward = !startsBackwards;
        completedSequence = false;
        paused = false;
        baseRot = Quaternion.identity;

        rb.position = kf[0].position;

        //set appropriate starting position (for path movers)
        if(!rotateInPlace && lastKey > 0)
        {
            RecordPosition();
            rb.position = kf[startKey].position;
            ApplyDeltas(1f);
        }

        if(rotateInPlace || forceOrient)
            rb.rotation = Quaternion.identity;
    }

    Quaternion recordRot;
    Vector3 recordPos;

    private void Update()
    {
        Debug.Log("Update for " + name + ": " + rotateInPlace + " " + time + " " + forward);
        if(!moving)
            return;

        RecordPosition();

        float dt = Time.deltaTime;
        time += dt;
        if(!PauseUpdate())
        {
            if(rotateInPlace)
                RotateUpdate();
            else
                PathUpdate();
        }
        
        ApplyDeltas(dt);
    }

    private void RecordPosition()
    {
        recordRot = rb.rotation;
        recordPos = rb.position;
    }

    private void ApplyDeltas(float dt)
    {
        if(rotateInPlace)
        {
            Quaternion deltaRot = rb.rotation * Quaternion.Inverse(recordRot);

            foreach(Rigidbody rb in content)
            {

                Vector3 newPos = RotateAroundPivot(rb.position, this.rb.position, deltaRot);
                Vector3 deltaPos = newPos - rb.position;

                rb.position = newPos;
                rb.velocity = deltaPos / dt;
                rb.rotation = deltaRot * rb.rotation;
                rb.angularVelocity = GetAxis(deltaRot);
            }
        }
        else
        {
            Vector3 deltaPos = rb.position - recordPos;

            foreach(Rigidbody rb in content)
            {
                rb.position = rb.position + deltaPos;
                rb.velocity = deltaPos / dt;
            }
        }
    }

    /// <summary>
    /// Advance pause time, return true if movement is still paused
    /// </summary>
    private bool PauseUpdate()
    {
        if(!paused)
            return false;

        float pauseTime = kf_pauseTime[lastKey];
        if(time > pauseTime)
        {
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
    private void RotateUpdate()
    {
        //Get movement parameters (rotations ignore all but the first key)
        float travTime = GetNextTravTime();
        float angle = (forward ? 1f : -1f) * kf_rotationAmount[0];

        //Calculate movement
        float x = GetXByTravTime(time, travTime, angle, 0, 0);

        //apply rotation
        Vector3 axis = kf[0].rotation * Vector3.up;
        rb.rotation = Quaternion.AngleAxis(x, axis) * baseRot;

        if(time > travTime)
        {
            //keyframe is finished, wrap up
            TriggerKeyLink(0);

            rb.rotation = Quaternion.AngleAxis(angle, axis) * baseRot;

            time -= travTime;
            if(travTime < MIN_TRAV_TIME)
                time = 0f;
            FinishSequence();
            baseRot = rb.rotation;

            paused = moving && kf_pauseTime[lastKey] > 0f;
            if(moving && !paused && time > 0f)
                RotateUpdate();
        }
    }

    /// <summary>
    /// Update movement as pathing from one keyframe to another
    /// </summary>
    private void PathUpdate()
    {
        //get movement parameters
        int nextKey = GetNextKey();
        Vector3 fro = kf[lastKey].position;
        Vector3 to = kf[nextKey].position;
        float distance = (to - fro).magnitude;
        float travTime = GetNextTravTime();

        //calculate movement
        float x;
        if(useTravTimeAsSpd)
        {
            float speed = travTime;
            x = GetXBySpeed(time, speed, distance, lastKey, nextKey);
            travTime = T;
        }
        else
            x = GetXByTravTime(time, travTime, distance, lastKey, nextKey);

        //apply position and rotation
        rb.position = Vector3.MoveTowards(fro, to, x);
        ForceOrient(to - fro);

        if(time > travTime)
        {
            //keyframe is finished, wrap up

            TriggerKeyLink(nextKey);
            lastKey = nextKey;

            rb.position = to;

            time -= travTime;
            if(travTime < MIN_TRAV_TIME)
                time = 0f;
            if(AtLastKeyInSequence())
                FinishSequence();

            paused = moving && kf_pauseTime[lastKey] > 0f;
            if(moving && !paused)
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
    private float GetXByTravTime(float time, float travTime, float distance, int accelKey, int decelKey)
    {
        Ta = kf_accelTime[accelKey];
        Td = kf_decelTime[decelKey];
        return GetX_TX(time, travTime, distance);
    }

    /// <summary>
    /// Return distance that mover should have traveled from its last keyframe.
    /// This version takes in a max speed value and yields the total travel time.
    /// </summary>
    /// <param name="time">Time since last keyframe</param>
    /// <param name="speed">Maximum speed that mover is allowed travel (can be degrees/second)</param>
    /// <param name="distance">Distance between keys (or angle)</param>
    /// <param name="accelKey">Key index that holds acceleration time</param>
    /// <param name="decelKey">Key index that holds deceleleration time</param>
    /// <param name="travTime">Total time needed to travel between keyframes</param>
    private float GetXBySpeed(float time, float speed, float distance, int accelKey, int decelKey)
    {
        Ta = kf_accelTime[accelKey];
        Td = kf_decelTime[decelKey];
        return GetX_VX(time, distance, speed);
    }

    /// <summary>
    /// Return interpolated x position based on total travel time and distance
    /// </summary>
    private float GetX_TX(float t, float T, float X)
    {
        if(T <= 0f)
            return X;

        ConstrainAccelTimes(T);
        float V = X / (T - ((Ta + Td) / 2f));

        return GetX_TV(t, T, V);
    }

    /// <summary>
    /// Return interpolated x position based on maximum speed and total travel distance
    /// Also outputs the total travel time of the path that meets those conditions.
    /// </summary>
    private float GetX_VX(float t, float X, float V)
    {
        if(V <= 0f)
        {
            T = float.PositiveInfinity;
            return 0f;
        }

        T = 2 * X / V;
        ConstrainAccelTimes(T);
        T = X / V + ((Ta + Td) / 2f);

        return GetX_TV(t, T, V);
    }

    /// <summary>
    /// Reduces referenced acceleration and deceleration times, so that 
    /// their sum is smaller than the given value maxSum.
    /// Negative values are set to 0 by default.
    /// </summary>
    private void ConstrainAccelTimes(float maxSum)
    {
        Ta = Mathf.Max(0f, Ta);
        Td = Mathf.Max(0f, Td);

        float sum = Ta + Td;
        if(sum > maxSum)
        {
            Ta = maxSum * Ta / sum;
            Td = maxSum * Td / sum;
        }
    }

    /// <summary>
    /// Return interpolated x position based on maximum speed and total travel time.
    /// </summary>
    private float GetX_TV(float t, float T, float V)
    {
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
    private void ForceOrient(Vector3 dir)
    {
        if(!forceOrient)
            return;

        rb.rotation = Quaternion.LookRotation(dir);
    }

    private int GetNextKey()
    {
        if(forward)
        {
            if(lastKey == kf.Length - 1)
                return 0;
            else
                return lastKey + 1;
        }
        else
        {
            if(lastKey == 0)
                return kf.Length - 1;
            else
                return lastKey - 1;
        }
    }

    /// <summary>
    /// Returns travel time to be used on next section of the path.
    /// Note that this value encodes a speed if the corresponding flag is active.
    /// </summary>
    private float GetNextTravTime()
    {
        if(rotateInPlace)
            return forward ? kf_departTravelTime[0] : kf_returnTravelTime[0];

        if(forward)
            return kf_departTravelTime[lastKey];
        else
            return kf_returnTravelTime[GetNextKey()];
    }

    /// <summary>
    /// Returns travel time to be used on next section of the path.
    /// Also returns travel time of the next section when using "useTravTimeAsSpd".
    /// the time parameter should always be constrained between 0 and this value.
    /// </summary>
    private float GetRealTravTime()
    {
        float travTime = GetNextTravTime();
        if(!rotateInPlace && useTravTimeAsSpd)
        {
            float speed = travTime;
            int nextKey = GetNextKey();
            Vector3 fro = kf[lastKey].position;
            Vector3 to = kf[nextKey].position;
            float distance = (to - fro).magnitude;
            GetXBySpeed(time, speed, distance, lastKey, nextKey);
            travTime = T;
        }
        return travTime;
    }

    private void TriggerKeyLink(int key)
    {
        /*
        if(key.triggerID > 0)
            UFTrigger.Activate(key.triggerID);
            */
    }

    private bool AtLastKeyInSequence()
    {
        bool loopType = type == 4; // MovingGroup.MovementType.LoopOnce;
        loopType |= type == 5; // MovingGroup.MovementType.LoopInfinite;

        bool checkLast = forward;
        if(loopType)
            checkLast = !checkLast;

        if(checkLast)
            return lastKey == kf.Length - 1;
        else
            return lastKey == 0;
    }

    private void FinishSequence()
    {
        switch(type)
        {
        case 6: // MovingGroup.MovementType.Lift:
        case 1: // MovingGroup.MovementType.OneWay:
        forward = !forward;
        moving = false;
        break;

        case 2: // MovingGroup.MovementType.PingPongOnce:
        forward = !forward;
        if(!completedSequence)
            completedSequence = true;
        else
        {
            completedSequence = false;
            moving = false;
        }
        break;

        case 3: // MovingGroup.MovementType.PingPongInfinite:
        forward = !forward;
        break;

        case 4: // MovingGroup.MovementType.LoopOnce:
        moving = false;
        break;

        case 5: // MovingGroup.MovementType.LoopInfinite:
        break;

        default:
        moving = false;
        Debug.LogWarning("Encountered unkown movement type: " + type);
        break;
        }
    }

    public void Activate(bool positive)
    {
        moving = positive;
    }

    public void Reverse(bool goForward)
    {
        if(!moving || forward == goForward)
            return;

        float doneFrac = time / GetRealTravTime();

        if(!rotateInPlace)
            lastKey = GetNextKey();
        forward = goForward;

        time = (1f - doneFrac) * GetRealTravTime();
    }

    public void ChangeRotationSpeed(float factor)
    {
        kf_departTravelTime[0] /= factor;
        kf_returnTravelTime[0] /= factor;
        time /= factor;
    }

    private void PlayClip(AudioClip clip, float volume)
    {
        if(clip == null)
            return;

        sound.clip = clip;
        sound.volume = volume;
        sound.Play();
    }


    //Utility methods
    private Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
    {
        Vector3 dir = point - pivot;
        Vector3 rotatedDir = rotation * dir;
        return pivot + rotatedDir;
    }

    private Vector3 GetAxis(Quaternion q) {
        Vector3 v = Vector3.forward;
        float angle = GetQuatAngle(q);
        return angle * Vector3.Cross(v, q * v).normalized;
    }

    private float GetQuatAngle(Quaternion q) {
        float angle = Quaternion.Angle(Quaternion.identity, q);

        if(angle < 45f) {
            Vector3 v = Vector3.forward;
            float sinRad = Vector3.Cross(v, q * v).magnitude;
            return Mathf.Asin(sinRad) * Mathf.Rad2Deg;
        }
        else
            return angle;
    }
}
