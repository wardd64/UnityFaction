using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;
using UnityEngine.Audio;

public class UFMover : MonoBehaviour {

    private AudioSource sound;
    private Rigidbody rb;
    private Rigidbody[] content;

    public UFLevelStructure.Keyframe[] keys;
    public int startKey;

    public int[] links;

    public bool isDoor, startsBackwards, rotateInPlace,
        useTravTimeAsSpd, forceOrient, noPlayerCollide;

    public MovingGroup.MovementType type;

    public AudioClip startClip, loopClip, stopClip, closeClip;
    public float startVol, loopVol, stopVol, closeVol;

    private const float MIN_TRAV_TIME = 1e-2f;

    public void Set(MovingGroup group) {
        
        //assign variables
        keys = group.keys;
        type = group.type;
        startKey = group.startIndex;
        links = group.contents;

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
    }

    public void AddAudio(AudioMixerGroup channel) {
        if(startClip == null && loopClip == null && closeClip == null && stopClip == null)
            return;
        sound = gameObject.AddComponent<AudioSource>();
        sound.outputAudioMixerGroup = channel;
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

    private void Awake() {
        rb = this.GetComponent<Rigidbody>();
        sound = this.GetComponent<AudioSource>();

        //extract transforms to be moved by this mover
        List<Transform> contents = new List<Transform>();
        foreach(int link in links) {
            GameObject g = UFLevel.GetByID(link).objectRef;
            if(g != null)
                contents.Add(g.transform);
        }

        //make sure all contents have (kinematic) rigidbodies
        this.content = new Rigidbody[contents.Count];
        for(int i = 0; i < contents.Count; i++) {
            Rigidbody rb = contents[i].GetComponent<Rigidbody>();
            if(rb == null)
                rb = contents[i].gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            this.content[i] = rb;
        }

        //turn off all contained colliders if needed
        if(noPlayerCollide) {
            foreach(Transform t in contents) {
                Collider[] cols = t.GetComponentsInChildren<Collider>();
                foreach(Collider c in cols) {
                    if(!c.isTrigger)
                        c.enabled = false;
                }  
            }
        }
    }

    private void Start() {
        ResetMotion();
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

        rb.position = keys[0].transform.posRot.position;

        //set appropriate starting position (for path movers)
        if(!rotateInPlace && lastKey > 0) {
            RecordPosition();
            rb.position = keys[startKey].transform.posRot.position;
            ApplyDeltas(1f);
        }

        if(rotateInPlace || forceOrient)
            rb.rotation = Quaternion.identity;
    }

    Quaternion recordRot;
    Vector3 recordPos;

    private void Update() {
        if(!moving)
            return;

        RecordPosition();

        float dt = Time.deltaTime;
        time += dt;
        if(!PauseUpdate()) {
            if(rotateInPlace)
                RotateUpdate();
            else
                PathUpdate();
        }

        ApplyDeltas(dt);
    }

    private void RecordPosition() {
        recordRot = rb.rotation;
        recordPos = rb.position;
    }

    private void ApplyDeltas(float dt) {
        if(rotateInPlace) {
            Quaternion deltaRot = rb.rotation * Quaternion.Inverse(recordRot);

            foreach(Rigidbody rb in content) {

                Vector3 newPos = UFUtils.RotateAroundPivot(rb.position, this.rb.position, deltaRot);
                Vector3 deltaPos = newPos - rb.position;

                PushPlayer(rb, deltaPos, dt);

                rb.position = newPos;
                rb.velocity = deltaPos / dt;
                rb.rotation = deltaRot * rb.rotation;
                rb.angularVelocity = UFUtils.GetAxis(deltaRot);
            }
        }
        else {
            Vector3 deltaPos = rb.position - recordPos;

            foreach(Rigidbody rb in content) {

                PushPlayer(rb, deltaPos, dt);

                rb.position = rb.position + deltaPos;
                rb.velocity = deltaPos / dt;
            }
        }
    }

    private void PushPlayer(Rigidbody rb, Vector3 delta, float dt) {
        /*
        CharacterController player = UFLevel.GetPlayer<CharacterController>();
        Collider col = rb.GetComponent<Collider>();

        if(!InMoveRadius(col, delta, player))
            return;

        float dist = delta.magnitude;

        Vector3 center = player.transform.position + player.center;
        float capDist = .5f * player.height - 2f * player.radius;
        Vector3 capOffset = capDist * Vector3.up;
        Vector3 p1 = center + capOffset;
        Vector3 p2 = center - capOffset;
        LayerMask mask = 1 << rb.gameObject.layer;

        RaycastHit[] hits = Physics.CapsuleCastAll(p1, p2, player.radius, -delta, dist, mask);
        foreach(RaycastHit hit in hits) {
            if(hit.collider == col) {
                Vector3 push = hit.distance * delta.normalized;
                player.GetComponent<UFPlayerMovement>().GetPushed(push, dist / dt);
                return;
            }
        }
        */
    }

    private static bool InMoveRadius(Collider mover, Vector3 delta, CharacterController player) {
        if(mover == null || player == null || delta == Vector3.zero)
            return false;

        Vector3 playerPos = player.transform.position + player.center;
        Vector3 center = mover.transform.position + .5f * delta;
        Vector3 mbe = mover.bounds.extents;
        float moverRadius = 1.5f * Mathf.Max(mbe.x, mbe.y, mbe.z);
        float playerRadius = player.height / 2f;
        float radius = moverRadius + playerRadius;

        return UFUtils.InsideSphere(playerPos, center, radius);
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
        float travTime = GetNextTravTime();
        float angle = (forward ? 1f : -1f) * keys[0].rotationAmount;

        //Calculate movement
        float x = GetXByTravTime(time, travTime, angle, 0, 0);

        //apply rotation
        Vector3 axis = keys[0].transform.posRot.rotation * Vector3.up;
        rb.rotation = Quaternion.AngleAxis(x, axis) * baseRot;

        if(time > travTime) {
            //keyframe is finished, wrap up
            TriggerKeyLink(keys[0]);

            rb.rotation = Quaternion.AngleAxis(angle, axis) * baseRot;

            time -= travTime;
            if(travTime < MIN_TRAV_TIME)
                time = 0f;
            FinishSequence();
            baseRot = rb.rotation;

            paused = moving && keys[lastKey].pauseTime > 0f;
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
        PosRot fro = keys[lastKey].transform.posRot;
        PosRot to = keys[nextKey].transform.posRot;
        float distance = (to.position - fro.position).magnitude;
        float travTime = GetNextTravTime();

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
            TriggerKeyLink(keys[nextKey]);
            lastKey = nextKey;

            rb.position = to.position;
            
            time -= travTime;
            if(travTime < MIN_TRAV_TIME)
                time = 0f;
            if(AtLastKeyInSequence())
                FinishSequence();

            paused = moving && keys[lastKey].pauseTime > 0f;
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
    private float GetXByTravTime(float time, float travTime, float distance, int accelKey, int decelKey) {
        float Ta = keys[accelKey].accelTime;
        float Td = keys[decelKey].decelTime;
        return GetX_TX(time, Ta, Td, travTime, distance);
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

    /// <summary>
    /// Returns travel time to be used on next section of the path.
    /// Note that this value encodes a speed if the corresponding flag is active.
    /// </summary>
    private float GetNextTravTime() {
        if(rotateInPlace)
            return forward ? keys[0].departTravelTime : keys[0].returnTravelTime;

        if(forward)
            return keys[lastKey].departTravelTime;
        else
            return keys[GetNextKey()].returnTravelTime;
    }

    /// <summary>
    /// Returns travel time to be used on next section of the path.
    /// Also returns travel time of the next section when using "useTravTimeAsSpd".
    /// the time parameter should always be constrained between 0 and this value.
    /// </summary>
    private float GetRealTravTime() {
        float travTime = GetNextTravTime();
        if(!rotateInPlace && useTravTimeAsSpd){
            float speed = travTime;
            int nextKey = GetNextKey();
            PosRot fro = keys[lastKey].transform.posRot;
            PosRot to = keys[nextKey].transform.posRot;
            float distance = (to.position - fro.position).magnitude;
            GetXBySpeed(time, speed, distance, lastKey, nextKey, out travTime);
        }
        return travTime;
    }

    private void TriggerKeyLink(UFLevelStructure.Keyframe key) {
        /*
        if(key.triggerID > 0)
            UFTrigger.Activate(key.triggerID);
            */
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

    private void FinishSequence() {
        switch(type) {
        case MovingGroup.MovementType.Lift:
        case MovingGroup.MovementType.OneWay:
        forward = !forward;
        moving = false;
        break;

        case MovingGroup.MovementType.PingPongOnce:
        forward = !forward;
        if(!completedSequence)
            completedSequence = true;
        else {
            completedSequence = false;
            moving = false;
        }
        break;

        case MovingGroup.MovementType.PingPongInfinite:
        forward = !forward;
        break;

        case MovingGroup.MovementType.LoopOnce:
        moving = false;
        break;

        case MovingGroup.MovementType.LoopInfinite: break;

        default:
        moving = false;
        Debug.LogWarning("Encountered unkown movement type: " + type);
        break;
        }
    }

    public void Activate(bool positive) {
        moving = positive;
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

    public void ChangeRotationSpeed(float factor) {
        keys[0].departTravelTime /= factor;
        keys[0].returnTravelTime /= factor;
        time /= factor;
    }

    private void PlayClip(AudioClip clip, float volume) {
        if(clip == null)
            return;

        sound.clip = clip;
        sound.volume = volume;
        sound.Play();
    }

    public void ConvertToUdon(Transform[] keyTransforms) {
        UFMoverUdon udon = gameObject.AddComponent<UFMoverUdon>();

        //extract transforms to be moved by this mover
        List<Transform> contents = new List<Transform>();
        foreach(int link in links) {
            GameObject g = UFLevel.GetByID(link).objectRef;
            if(g != null)
                contents.Add(g.transform);
        }

        //make sure all contents have (kinematic) rigidbodies
        udon.content = new Rigidbody[contents.Count];
        for(int i = 0; i < contents.Count; i++)
        {
            Rigidbody rb = contents[i].GetComponent<Rigidbody>();
            if(rb == null)
                rb = contents[i].gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            udon.content[i] = rb;
        }

        //turn off all contained colliders if needed
        if(noPlayerCollide) {
            foreach(Rigidbody rb in udon.content) {
                Collider[] cols = rb.GetComponentsInChildren<Collider>();
                foreach(Collider c in cols) {
                    if(!c.isTrigger)
                        c.enabled = false;
                }
            }
        }
        
        udon.startKey = startKey;

        int n = keyTransforms.Length;
        udon.kf = new Transform[n];
        udon.kf_accelTime = new float[n];
        udon.kf_decelTime = new float[n];
        udon.kf_departTravelTime = new float[n];
        udon.kf_pauseTime = new float[n];
        udon.kf_returnTravelTime = new float[n];
        udon.kf_rotationAmount = new float[n];

        for(int i = 0; i < keyTransforms.Length; i++) {
            udon.kf[i] = keyTransforms[i];
            udon.kf_accelTime[i] = keys[i].accelTime;
            udon.kf_decelTime[i] = keys[i].decelTime;
            udon.kf_departTravelTime[i] = keys[i].departTravelTime;
            udon.kf_pauseTime[i] = keys[i].pauseTime;
            udon.kf_returnTravelTime[i] = keys[i].returnTravelTime;
            udon.kf_rotationAmount[i] = keys[i].rotationAmount;
        }

        udon.isDoor = isDoor;
        udon.startsBackwards = startsBackwards;
        udon.rotateInPlace = rotateInPlace;
        udon.useTravTimeAsSpd = useTravTimeAsSpd;
        udon.forceOrient = forceOrient;
        udon.noPlayerCollide = noPlayerCollide;

        udon.type = (int)type;

        udon.startClip = startClip;
        udon.loopClip = loopClip;
        udon.stopClip = stopClip;
        udon.closeClip = closeClip;
        udon.startVol = startVol;
        udon.loopVol = loopVol;
        udon.stopVol = stopVol;
        udon.closeVol = closeVol;

        UFUtils.MakeUdonBehaviour(udon);
    }
}
