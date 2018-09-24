using UnityEngine;
using System.Collections;
using System;

public class UFPlayerMovement : MonoBehaviour {

    private CharacterController cc;
    private Camera playerCamera;
    private UFPlayerMoveSounds moveSound;

    private MotionState motionState;
    public enum MotionState {
        ground, air, crouch, climb, swim
    }
    private bool crouching;

    private Vector3 velocity;
    private bool slippery;
    private float walkSlope;
    private bool hitGround;
    private float jumpTime;
    protected bool doubleJumped;

    private float rotationX, rotationY;
    private const int rotSmoothing = 3;
    private const float minY = -90f;
    private const float maxY = 90f;
    private float[] prevRotX, prevRotY;

    private Vector3 shiftVelocity;
    private Vector3 climbDir;
    private Transform platform;
    private Vector3 lastPlatformPosition;
    private UFLiquid liquid;
    private bool liquidNeedsReset;

    //movement constants
    public float walkSpeed = 8f; //movement speed in m/s
    public float airSpeed = 6f; //base horizontal movement speed when jumping
    public float swimSpeed = 3f; //target speed while swimming
    public float groundAccelTime = 0.2f; //time needed to achieve walkspeed
    public float iceAccelTime = 1.5f; //time needed to achieve walkspeed on ice
    public float verticalDrag = 0.5f; //aerial vertical drag force
    public float airSteeringMin = 1f; //push force the player can use to steer while flying fast
    public float airSteeringMax = 14.5f; //push force while flying slow
    public float climbSteering = 40f; //push force while climbing
    public float swimSteering = 8f; //push force while swimming
    public float jumpHeight = 1.25f; //height in m the player can jump
    public float minJumpSpeed = 1f; //additive jump speed when player is walking up a ramp
    public float stepOver = 0.2f; //Highest distance player can safely step over
    public float footing = 0.1f; //extra distance to make sure player holds on to the ground
    public float slideSlope = 65f; //Angle that seperates floors from walls
    public float crouchSpeed = 6f; //movement speed while crouching
    public float standingHeight = 1.85f, crouchingHeight = 1.15f; //cc height
    public float standingRadius = 0.6f, crouchingRadius = 0.55f; //cc radius
    public float antJmpGMulplr = 4f; //maximum gravity multiplier used to cut jump short
    public float sharpEdgeTrshold = 2.5f; //speed above which sharp edge correction activates
    public float moverHitForce = 1f; //factor by which mover collissions are multiplied
    public float liquidJumpMultiplier = 2f; //factor for vert speed when exiting liquids

    /// <summary>
    /// Unit vector pointing horizontally in the direction the player is turned towards
    /// </summary>
    Vector3 forward
    {
        get
        {
            Vector3 toReturn = this.transform.forward;
            toReturn = Vector3.ProjectOnPlane(toReturn, Vector3.up);
            return toReturn.normalized;
        }
    }

    public float speed { get { return velocity.magnitude; } }
    public Transform ragHip { get { return this.GetComponent<Ragdoll>().ragHip; } }
    public bool grounded { get {
            return hitGround 
                || motionState == MotionState.ground 
                || motionState == MotionState.crouch;
    } }

    private Vector3 horVel {
        get { return Vector3.ProjectOnPlane(velocity, Vector3.up); }
        set { velocity = new Vector3(value.x, velocity.y, value.z); }
    }
    private float horSpeed { get { return horVel.magnitude; } }
    private float vertVel {
        get { return velocity.y; }
        set { velocity = new Vector3(velocity.x, value, velocity.z); }
    }
    private float jumpSpeed { get { return Mathf.Sqrt(2 * jumpHeight * Physics.gravity.magnitude); } }
    private float climbDrag { get { return climbSteering / walkSpeed; } }
    private float swimDrag { get { return swimSteering / swimSpeed; } }
    private float accelTime { get { return slippery ? iceAccelTime : groundAccelTime; } }
    private float termSpeed { get { return Physics.gravity.magnitude / verticalDrag; } }

    float wallLimit { get { return Mathf.Sin((90 - slideSlope) * Mathf.Deg2Rad); } }

    private void Awake() {
        //set up nearby connections
        cc = this.GetComponent<CharacterController>();
        playerCamera = this.GetComponentInChildren<Camera>();
        moveSound = this.GetComponentInChildren<UFPlayerMoveSounds>();
    }

    private void Start() {
        if(UFLevel.playerInfo != null)
            UFLevel.playerInfo.ApplyCameraSettings(playerCamera);
        SetRotSmoothing(rotSmoothing);
        UFLevel.playerInfo.ResetVision();
        GetComponent<UFPlayerWeapons>().SetLiquidVision(false, Color.clear);
        Spawn();
    }

    public virtual void Spawn() {
        UFLevelStructure.PosRot pr = default(UFLevelStructure.PosRot);
        if(UFLevel.playerInfo != null)
            pr = UFLevel.playerInfo.GetSpawn(UFPlayerInfo.PlayerClass.Free);

        this.gameObject.layer = UFLevel.playerInfo.playerLayer;

        this.transform.position = pr.position;
        this.transform.rotation = Quaternion.Euler(0f, pr.rotation.eulerAngles.y, 0f);
        playerCamera.transform.rotation = Quaternion.Euler(pr.rotation.x, 0f, 0f);

        GetComponent<UFPlayerWeapons>().Reset();
    }

    private void OnEnable() {
        if(!cc.enabled)
            return;

        bool onGround = CheckGround(0.1f);
        if(onGround) {
            cc.Move(0.01f * Vector3.up);
            cc.Move(0.11f * Vector3.down);
            motionState = MotionState.ground;
        }
        else
            motionState = MotionState.air;
    }

    private void Update() {
        MoveUpdate();

        //animation states:
        SetAnimation("Airborne", motionState == MotionState.air);
        SetAnimation("Crouch", motionState == MotionState.crouch);

        MouseUpdate();
        if(UFLevel.playerInfo != null)
            UFLevel.playerInfo.UpdateCamera(playerCamera);
    }

    private void MouseUpdate() {
        MouseRotate();
        UFUtils.SetFPSCursor(!IgnoreInput());

        transform.localEulerAngles = new Vector3(0, rotationX, 0);
        playerCamera.transform.localRotation = Quaternion.Euler(-rotationY, 0, 0);
    }

    public Vector2 MouseRotate() {
        float sensitivity = GetSensitivity();

        for(int i = 0; i < rotSmoothing - 1; i++) {
            prevRotX[i] = prevRotX[i + 1];
            prevRotY[i] = prevRotY[i + 1];
        }

        float xInput = 0f, yInput = 0f;
        if(!IgnoreInput()) {
            xInput = Input.GetAxis("Mouse X");
            yInput = Input.GetAxis("Mouse Y");
        }

        float newRotX = prevRotX[rotSmoothing - 1] + xInput * sensitivity;
        prevRotX[rotSmoothing - 1] = newRotX;
        float newRotY = prevRotY[rotSmoothing - 1] + yInput * sensitivity;
        prevRotY[rotSmoothing - 1] = Mathf.Clamp(newRotY, minY, maxY);

        rotationX = 0f;
        rotationY = 0f;
        for(int i = 0; i < rotSmoothing; i++) {
            rotationX += prevRotX[i];
            rotationY += prevRotY[i];
        }
        rotationX /= rotSmoothing;
        rotationY /= rotSmoothing;

        return new Vector2(rotationX, rotationY);
    }

    private void SetRotSmoothing(int rotSmoothing) {
        prevRotX = new float[rotSmoothing];
        prevRotY = new float[rotSmoothing];
        rotationX = transform.eulerAngles.y;
        for(int i = 0; i < rotSmoothing; i++)
            prevRotX[i] = rotationX;
    }


    private void MoveUpdate() {

        //input
        bool allow = !IgnoreInput();
        bool right = allow && InputInterface.input.GetKey("right");
        bool left = allow && InputInterface.input.GetKey("left");
        bool forward = allow && InputInterface.input.GetKey("forward");
        bool backward = allow && InputInterface.input.GetKey("backward");
        bool jumpDown = allow && InputInterface.input.GetKeyDown("jump");
        bool jump = allow && InputInterface.input.GetKey("jump");
        bool crouch = allow && InputInterface.input.GetKey("crouch");

        hitGround = false;

        //horizontal motion
        float horizontal = (right ? 1f : 0f) - (left ? 1f : 0f);
        float vertical = (forward ? 1f : 0f) - (backward ? 1f : 0f);
        Vector3 movement = (new Vector3(horizontal , 0f, vertical)).normalized;
        movement = transform.rotation * movement;

        //move with platform
        if(this.platform != null) {
            Vector3 platformDelta = this.platform.position - this.lastPlatformPosition;
            cc.Move(platformDelta);
            if(this.platform != null)
                this.lastPlatformPosition = this.platform.position;
        }

        //liquid vision
        bool liqVision = false;
        if(liquid != null) {
            liqVision = playerCamera.transform.position.y < liquid.absoluteY;
            if(liqVision) {
                liquid.SetLiquidVision();
                GetComponent<UFPlayerWeapons>().SetLiquidVision(true, liquid.color);
                liquidNeedsReset = true;
            }
            liquid = null;
        }
        if(!liqVision && liquidNeedsReset) {
            liquidNeedsReset = false;
            UFLevel.playerInfo.ResetVision();
            GetComponent<UFPlayerWeapons>().SetLiquidVision(false, Color.clear);
        }

        //seperate motion updates
        switch(motionState) {
        case MotionState.ground:
        GroundMove(movement, jumpDown, crouch);
        break;
        case MotionState.air:
        AirMove(movement, jump, jumpDown);
        break;
        case MotionState.crouch:
        CrouchMove(movement, crouch);
        break;
        case MotionState.climb:
        ClimbMove(movement, jump, crouch);
        break;
        case MotionState.swim:
        SwimMove(movement, jump, crouch);
        break;
        default:
        Debug.LogError("Unexpected motion state: " + motionState);
        break;
        }

        //animation motion state
        float angle = Vector3.SignedAngle(this.forward, horVel, Vector3.up);
        SetAnimation("Moving", horSpeed > 0.01f);
        SetAnimation("MoveAngle", false, angle);

        //crouching
        if(crouch && !crouching)
            crouching = true;
        else if(!crouch && crouching)
            crouching = !CheckIfClearForStandingUp();
        SetCharacterHeight(crouching);

        //others
        shiftVelocity = Vector3.zero;
    }

    private void GroundMove(Vector3 movement, bool jumpDown, bool crouch) {
        float dv = Time.deltaTime * (walkSpeed / accelTime);
        movement *= walkSpeed;

        //limit speed when walking up steep inclines
        float slopeFactor = Mathf.Clamp01((slideSlope - walkSlope) / slideSlope);
        movement *= Mathf.Sqrt(slopeFactor);

        horVel = Vector3.MoveTowards(horVel, movement, dv);

        if(jumpDown) {
            Jump();
            AirMove(movement / walkSpeed, true, false);
        }
        else if(crouch) {
            motionState = MotionState.crouch;
            horVel *= crouchSpeed / walkSpeed;
            MoveCCWalking();
        }
        else {
            //simply walking
            MoveCCWalking();

            if(!hitGround)
                WalkOff();
        }
    }

    private void Jump() {
        jumpTime = Time.deltaTime;
        SetAnimation("Jump");
        moveSound.Jump();

        //vertical velocity
        if(jumpSpeed < vertVel + minJumpSpeed)
            vertVel += minJumpSpeed;
        else
            vertVel = jumpSpeed;

        //horizontal velocity
        horVel *= airSpeed / walkSpeed;

        motionState = MotionState.air;
    }

    private void DoubleJump() {
        jumpTime = Time.deltaTime;
        if(StabilizeDoubleJump())
            velocity = jumpSpeed * Vector3.up;
        else
            vertVel = jumpSpeed;
        SetAnimation("Jump");
        doubleJumped = true;
    }

    private void CrouchMove(Vector3 movement, bool crouch) {
        float dv = Time.deltaTime * (crouchSpeed / accelTime);
        movement *= crouchSpeed;

        horVel = Vector3.MoveTowards(horVel, movement, dv);

        MoveCCWalking();

        if(!hitGround)
            WalkOff();
        else if(!crouch) {
            //stand up
            bool canStandUp = CheckIfClearForStandingUp();
            if(canStandUp)
                motionState = MotionState.ground;
        }
    }

    private void WalkOff() {
        vertVel = 0f;
        motionState = MotionState.air;
    }

    private float GetGravMultiplier(float max, float time, float v0, float g) {
        float b = Mathf.Sqrt(g * (max * max - 1) / v0);
        return Mathf.Max(max - b * time, 1f);
    }

    private void AirMove(Vector3 movement, bool jump, bool jumpDown) {
        float dirSpeed = Vector3.Dot(velocity, movement.normalized);
        float spdFactor = Mathf.Clamp01(Mathf.Abs((dirSpeed - airSpeed) / airSpeed));
        float steering = airSteeringMax * spdFactor;
        if(steering > airSteeringMin && dirSpeed > airSpeed)
            steering = Mathf.Min(airSteeringMin, steering);
        Vector3 acceleration = movement * steering;
        Vector3 grav = Physics.gravity;

        //unphysically push player down when hes not holding the jump button
        if(AllowShortJump() && jumpTime > 0f) {
            jumpTime += Time.deltaTime;
            if(!jump)
                grav *= GetGravMultiplier(antJmpGMulplr, jumpTime, jumpSpeed, grav.magnitude);
        }

        //do stabilizing jump to make platforming easier
        if(jumpDown && AllowDoubleJump())
            DoubleJump();

        acceleration += grav;

        float vertFactor = Mathf.Clamp01((-vertVel - jumpSpeed) / (termSpeed - jumpSpeed));
        float vertDrag = verticalDrag * vertVel * vertFactor;
        Vector3 drag = Vector3.up * vertDrag;
        acceleration -= drag;

        MoveCCFlying(acceleration);
    }

    private void ClimbMove(Vector3 horMovement, bool up, bool down) {
        DriftMove(horMovement, up, down, climbDir, climbSteering, climbDrag);
    }

    private void SwimMove(Vector3 horMovement, bool up, bool down) {
        DriftMove(horMovement, up, down, Vector3.up, swimSteering, swimDrag);
    }

    private void DriftMove(Vector3 horMovement, bool up, bool down, Vector3 upDirection, float steering, float drag) {
        float vert = (up ? 1f : 0f) - (down ? 1f : 0f);
        Quaternion revRot = Quaternion.Euler(0f, -rotationX, 0f);
        Quaternion upRot = Quaternion.Euler(-rotationY, rotationX, 0f) * revRot;
        Vector3 movement = upRot * horMovement + vert * upDirection;

        if(movement != Vector3.zero)
            movement = movement.normalized;

        Vector3 acceleration = movement * steering;
        Vector3 dragV = drag * velocity;
        MoveCCFlying(acceleration - dragV);
        motionState = MotionState.air;
    }

    /// <summary>
    ///  Returns true if the player would collide with solid ground if he moves
    ///  the given amount of distance downwards
    /// </summary>
    private bool CheckGround(float distance) {
        RaycastHit[] hits;
        float kickHeight = 0.01f;

        Vector3 p1 = transform.position + (kickHeight + cc.radius) * Vector3.up;
        Vector3 p2 = transform.position + (cc.height - cc.radius) * Vector3.up;
        hits = Physics.CapsuleCastAll(p1, p2, cc.radius, -Vector3.down, distance + kickHeight);

        return CollidesWithTerrain(hits);
    }

    private void SetCharacterHeight(bool crouching) {
        float targetHeight = crouching ? crouchingHeight : standingHeight;
        float targetRadius = crouching ? crouchingRadius : standingRadius;
        float heightDelta = standingHeight - crouchingHeight;
        float radiusDelta = standingRadius - crouchingRadius;

        float dt = 5f * Time.deltaTime;
        float oldHeight = cc.height;

        cc.height = Mathf.MoveTowards(cc.height, targetHeight, heightDelta * dt);
        cc.radius = Mathf.MoveTowards(cc.radius, targetRadius, radiusDelta * dt);
        cc.center = Vector3.up * (cc.height / 2f);

        float deltaHeight = cc.height - oldHeight;
        playerCamera.transform.position += deltaHeight * Vector3.up;
    }

    private bool CheckIfClearForStandingUp() {
        RaycastHit[] hits;
        RaycastHit hit = new RaycastHit();

        float dist = standingHeight - crouchingHeight - standingRadius + crouchingRadius;
        float headHeight = (cc.height / 2) - crouchingRadius;
        Vector3 headPos = this.transform.position + cc.center;
        headPos += this.transform.up * headHeight;

        hits = Physics.SphereCastAll(headPos, standingRadius, transform.up, dist);
        return !CollidesWithTerrain(hits, out hit);
    }

    /// <summary>
    /// Detects collisions with terrain or moveable objects.
    /// </summary>
    void OnControllerColliderHit(ControllerColliderHit hit) {
        float y = hit.normal.y;

        //set motions state according to what we hit...
        if(y > wallLimit) {
            //we hit the ground
            hitGround = true;
            moveSound.SetLastGroundObject(hit.collider);

            Vector3 gradient = -Vector3.ProjectOnPlane(hit.normal, Vector3.up);
            float x = Vector3.Dot(gradient, horVel.normalized);
            float atan = Mathf.Atan2(y, x);
            walkSlope = 90f - (Mathf.Rad2Deg * atan);

            SetPlatform(hit.transform);
            slippery = hit.collider.name.ToLower().Contains("icy");
            if(motionState == MotionState.air && vertVel < 1e-2f)
                Land(hit.collider);
        }
        else if(y > 0f) {
            //hit slope we should slide off of
            Vector3 alongVel = Vector3.ProjectOnPlane(velocity, hit.normal);
            Vector3 normalVel = Vector3.Project(velocity, hit.normal);
            float r = 1f - UFUtils.LerpExpFactor(.1f);
            velocity = alongVel + r * normalVel;
        }
        //otherwise we hit a ceiling or wall, cc can handle on its own
    }

    private void SetPlatform(Transform transform) {
        Rigidbody rb = transform.GetComponent<Rigidbody>();
        if(rb != null && rb.isKinematic) {
            this.platform = transform;
            this.lastPlatformPosition = platform.position;
        }
        else
            this.platform = null;
    }

    private void Land(Collider collider) {
        motionState = MotionState.ground;
        moveSound.Jump();
        jumpTime = 0f;
        doubleJumped = false;
    }

    /// <summary>
    /// Moves player over the ground at his velocity.
    /// Any vertical component of the velocity is ignored.
    /// </summary>
    void MoveCCWalking() {
        float dt = Time.deltaTime;

        if(!cc.gameObject.activeInHierarchy || !cc.enabled || dt <= 0f)
            return;

        Vector3 fro = this.transform.position;

        Vector3 move = (velocity + shiftVelocity) * Time.deltaTime;
        move = Vector3.ProjectOnPlane(move, Vector3.up);
        Vector3 vertShift = Vector3.up * stepOver;

        //move horizontally
        cc.Move(move);

        //move further to catch downward slope
        float slopeCatch = move.magnitude * Mathf.Tan(Mathf.Deg2Rad * slideSlope) + footing;
        cc.Move(Vector3.down * slopeCatch);

        //check for sharp edge and keep player up if we find it
        float deltaY = this.transform.position.y - fro.y;
        Ray groundRay = new Ray(this.transform.position + vertShift, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(groundRay, 3 * vertShift.magnitude);
        bool sharpEdge = !UFUtils.collidesWithTerrain(hits);
        bool needsCorrection = deltaY < 0.001f && velocity.magnitude > sharpEdgeTrshold;
        if(sharpEdge && needsCorrection)
            cc.Move(deltaY * Vector3.down);

        //set velocity to correspond to the actual distance travel.
        //Vector3 realDist = this.transform.position - fro;
        //velocity = (realDist / dt) - shiftVelocity;
    }

    /**
	 * Move one update tick at velocity and the given acceleration.
	 */
    private void MoveCCFlying(Vector3 acceleration) {
        float dt = Time.deltaTime;

        if(!cc.gameObject.activeInHierarchy || !cc.enabled || dt <= 0f)
            return;

        Vector3 distance = (velocity + shiftVelocity) * dt;
        distance += acceleration * dt * dt / 2f;
        velocity += acceleration * dt;

        if(distance.y < 0f) {
            cc.Move(footing * Vector3.up);
            cc.Move(footing * Vector3.down);
        }

        cc.Move(distance);

        if(hitGround)
            vertVel = 0f;

    }

    /// <summary>
    /// returns true if the given array contains any hits with solid terrain
    /// </summary>
    public static bool CollidesWithTerrain(RaycastHit[] hits, out RaycastHit actualHit) {
        foreach(RaycastHit hit in hits) {
            actualHit = hit;

            //triggers
            if(hit.collider.isTrigger)
                continue;

            //part of the player
            if(hit.collider.GetComponentInParent<CharacterController>())
                continue;

            //non kinematic rigid bodies
            Rigidbody rb = hit.collider.transform.GetComponentInParent<Rigidbody>();
            if(rb != null && !rb.isKinematic)
                continue;

            return true;
        }
        actualHit = new RaycastHit();
        return false;
    }

    public void GetPushed(Vector3 delta, float speed) {
        transform.position += delta;
        Vector3 pushVel = delta.normalized * speed;
        SetVelocity(pushVel);
    }

    /// <summary>
    /// returns true if the given array contains any hits with solid terrain
    /// </summary>
    public static bool CollidesWithTerrain(RaycastHit[] hits) {
        RaycastHit hit;
        return CollidesWithTerrain(hits, out hit);
    }

    public void AddVelocity(Vector3 addVel) {
        SetVelocity(velocity + addVel);
    }

    public void SetVelocity(Vector3 velocity) {
        this.velocity = velocity;
        bool onGround = motionState == MotionState.ground || motionState == MotionState.crouch;
        if(vertVel > 0f && onGround) {
            motionState = MotionState.air;
            platform = null;
        }
    }

    public void JumpOutLiquid() {
        if(this.vertVel <= 0f)
            return;

        this.vertVel *= liquidJumpMultiplier;
    }

    public void ShiftVelocity(Vector3 velocity) {
        this.shiftVelocity += velocity;
    }

    public void ClimbState(Vector3 climbDirection) {
        motionState = MotionState.climb;
        this.climbDir = climbDirection;
        this.platform = null;
    }

    public void SwimState(UFLiquid liquid) {
        if(motionState == MotionState.climb)
            return;

        this.liquid = liquid;
        motionState = MotionState.swim;
        this.platform = null;
    }

    public void Reset() {
        velocity = Vector3.zero;
        platform = null;
    }

    public virtual void InButtonRange(KeyCode useKey) { }
    protected virtual void SetAnimation(string name, bool boolValue = false, float floatValue = 0f) { }
    protected virtual bool AllowShortJump() { return false; }
    protected virtual bool IgnoreInput() { return false; }
    protected virtual bool AllowDoubleJump() { return false; }
    protected virtual bool StabilizeDoubleJump() { return false; }
    protected virtual float GetSensitivity() { return 10f; }
}
