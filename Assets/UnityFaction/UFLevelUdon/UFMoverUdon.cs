
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
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

    //signal triggers
    public int signal, reverse;

    private const float MIN_TRAV_TIME = 1e-2f;

    //dynamic variables, used to control movement
    private bool moving;
    private int lastKey; //index of last keyframe
    private float time; //time since last keyframe
    private bool forward, paused;
    private bool completedSequence;
    private Quaternion baseRot;

    private Quaternion recordRot;
    private Vector3 recordPos;

    private VRCPlayerApi localPlayer;

    //algorithm helper variables, some used to avoid out and ref
    private float T;
    private float Ta, Td;
    private float x1, x2, t1, t2;
    private float V, sum;

    private void Start()
    {
        localPlayer = Networking.LocalPlayer;

        rb = this.GetComponent<Rigidbody>();
        sound = this.GetComponent<AudioSource>();
        
        ResetMotion();
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
            recordPos = rb.position;
            rb.position = kf[startKey].position;
            ApplyDeltas();
        }

        if(rotateInPlace || forceOrient)
            rb.rotation = Quaternion.identity;
    }


    private void Update()
    {
        if(Vector3.Distance(rb.position, localPlayer.GetPosition()) > 30f)
            return;

        if(signal != 0) {
            moving = signal > 0;
            signal = 0;
        }

        if(reverse != 0) {
            Reverse(reverse > 0);
            reverse = 0;
        }

        if(!moving)
            return;

        recordRot = rb.rotation;
        recordPos = rb.position;

        float dt = Time.deltaTime;
        time += dt;
        if(!PauseUpdate())
        {
            if(rotateInPlace)
                RotateUpdate();
            else
                PathUpdate();
        }
        
        ApplyDeltas();
    }

    private void ApplyDeltas() {
        if(rotateInPlace)
        {
            Quaternion deltaRot = rb.rotation * Quaternion.Inverse(recordRot);
            foreach(Rigidbody rb in content)
            {
                Vector3 dir = deltaRot * (rb.position - this.rb.position);
                rb.position = this.rb.position + dir;
                rb.rotation = deltaRot * rb.rotation;
            }
        }
        else
        {
            Vector3 deltaPos = rb.position - recordPos;
            foreach(Rigidbody rb in content)
                rb.position += deltaPos;
        }
    }

    /// <summary>
    /// Advance pause time, return true if movement is still paused
    /// </summary>
    private bool PauseUpdate() {
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
    private void RotateUpdate() {
        //Get movement parameters (rotations ignore all but the first key)
        float travTime = GetNextTravTime();
        float angle = (forward ? 1f : -1f) * kf_rotationAmount[0];

        Ta = kf_accelTime[0];
        Td = kf_decelTime[0];

        //Calculate movement
        float x = GetX_TX(time, travTime, angle);

        //apply rotation
        Vector3 axis = kf[0].rotation * Vector3.up;
        rb.rotation = Quaternion.AngleAxis(x, axis) * baseRot;

        if(time > travTime)
        {
            //keyframe is finished, wrap up
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
    private void PathUpdate() {
        //get movement parameters
        int nextKey = GetNextKey();
        Vector3 fro = kf[lastKey].position;
        Vector3 to = kf[nextKey].position;
        float distance = (to - fro).magnitude;
        float travTime = GetNextTravTime();

        Ta = kf_accelTime[lastKey];
        Td = kf_decelTime[nextKey];

        //calculate movement
        float x;
        if(useTravTimeAsSpd) {
            x = GetX_VX(time, distance, travTime);
            travTime = T;
        }
        else
            x = GetX_TX(time, travTime, distance);

        //apply position and rotation
        rb.position = Vector3.MoveTowards(fro, to, x);
        ForceOrient(to - fro);

        if(time > travTime)
        {
            //keyframe is finished, wrap up
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
    /// Return interpolated x position based on total travel time and distance
    /// </summary>
    private float GetX_TX(float t, float T, float X)
    {
        if(T <= 0f)
            return X;

        ConstrainAccelTimes(T);
        V = X / (T - ((Ta + Td) / 2f));

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

        sum = Ta + Td;
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
        
        x1 = V * Ta / 2f;
        t1 = t - Ta;
        
        if(t < Ta)
            return V * t * t / Ta / 2f;
        else if(t <= T - Td)
            return x1 + V * t1;
        else {
            x2 = V * (T - Ta - Td);
            t2 = t - T + Td;
            return x1 + x2 + (V * t2) - (V * t2 * t2 / Td / 2f);
        }
    }

    private void ForceOrient(Vector3 dir) {
        if(!forceOrient)
            return;

        rb.rotation = Quaternion.LookRotation(dir);
    }

    private int GetNextKey() {
        if(forward) {
            if(lastKey == kf.Length - 1)
                return 0;
            else
                return lastKey + 1;
        }
        else {
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
    private float GetNextTravTime() {
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
    private float GetRealTravTime() {
        float travTime = GetNextTravTime();
        if(!rotateInPlace && useTravTimeAsSpd) {
            float speed = travTime;
            int nextKey = GetNextKey();
            Vector3 fro = kf[lastKey].position;
            Vector3 to = kf[nextKey].position;
            float distance = (to - fro).magnitude;
            Ta = kf_accelTime[lastKey];
            Td = kf_decelTime[nextKey];
            GetX_VX(time, distance, speed);
            travTime = T;
        }
        return travTime;
    }

    private bool AtLastKeyInSequence() {
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

    private void FinishSequence() {
        switch(type) {
        case 6: // MovingGroup.MovementType.Lift:
        case 1: // MovingGroup.MovementType.OneWay:
        forward = !forward;
        moving = false;
        break;

        case 2: // MovingGroup.MovementType.PingPongOnce:
        forward = !forward;
        if(!completedSequence)
            completedSequence = true;
        else {
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

    public void Reverse(bool goForward) {
        if(!moving || forward == goForward)
            return;

        float doneFrac = time / GetRealTravTime();

        if(!rotateInPlace)
            lastKey = GetNextKey();
        forward = goForward;

        time = (1f - doneFrac) * GetRealTravTime();
    }
}
