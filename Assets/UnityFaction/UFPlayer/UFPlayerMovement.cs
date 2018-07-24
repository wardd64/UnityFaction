using UnityEngine;
using System.Collections;
using System;

public class UFPlayerMovement : MonoBehaviour {

    public Animator characterAnim;
    public Transform heartPoint;

    private CharacterController cc;
    private Camera playerCamera;
    private UFPlayerMoveSounds moveSound;

    private UFPlayerInfo playerInfo;

    private MotionState motionState;
    public enum MotionState {
        ground, air, crouch, climb, swim
    }
    private bool crouching;

    private Vector3 velocity;
    private float walkSlope;
    private bool hitGround;
    private float jumpTime;

    private float rotationX, rotationY;
    private const int rotSmoothing = 3;
    private const float minY = -90f;
    private const float maxY = 90f;
    private float[] prevRotX, prevRotY;

    private Vector3 shiftVelocity;
    private Vector3 climbDir;
    private Transform platform;
    private Vector3 lastPlatformPosition;

    //movement constants
    private const float walkSpeed = 8f; //movement speed in m/s
    private const float airSpeed = 5.2f; //base horizontal movement speed when jumping
    private const float swimSpeed = 3f; //target speed while swimming
    private const float accelTime = 0.2f; //time needed to achieve walkspeed
    private const float verticalDrag = 0.5f; //aerial vertical drag force
    private const float airSteeringMin = 1f; //push force the player can use to steer flying fast
    private const float airSteeringMax = 14.5f; //push force while flying slow
    private const float climbSteering = 40f; //push force while climbing
    private const float swimSteering = 8f; //push force while swimming
    private const float jumpHeight = 1.4f; //height in m the player can jump
    private const float minJumpSpeed = 1f; //additive jump speed when player is walking up a ramp
    private const float stepOver = 0.2f; //Highest distance player can safely step over
    private const float footing = 0.1f; //extra distance to make sure player holds on to the ground
    private const float slideSlope = 65f; //Angle that seperates floors from walls
    private const float crouchSpeed = 6f; //movement speed while crouching
    private const float standingHeight = 1.85f, crouchingHeight = 1.15f; //cc height
    private const float standingRadius = 0.6f, crouchingRadius = 0.55f; //cc radius
    private const float antJmpGMulplr = 4f; //maximum gravity multiplier used to cut jump short
    private const float sharpEdgeTrshold = 2.5f; //speed above which sharp edge correction activates

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

    float wallLimit { get { return Mathf.Sin((90 - slideSlope) * Mathf.Deg2Rad); } }

    private void Awake() {
        //set up nearby connections
        cc = this.GetComponent<CharacterController>();
        playerCamera = this.GetComponentInChildren<Camera>();
        moveSound = this.GetComponentInChildren<UFPlayerMoveSounds>();
        playerInfo = FindObjectOfType<UFPlayerInfo>();
        
        
    }

    private void Start() {
        if(playerInfo != null)
            playerInfo.ApplyCameraSettings(playerCamera);
        SetRotSmoothing(rotSmoothing);
        Spawn();
    }

    public void Spawn() {
        UFLevelStructure.PosRot pr = default(UFLevelStructure.PosRot);
        if(playerInfo != null)
            pr = playerInfo.GetSpawn(UFPlayerInfo.PlayerClass.Free);

        this.transform.position = pr.position;
        this.transform.rotation = Quaternion.Euler(0f, pr.rotation.eulerAngles.y, 0f);
        playerCamera.transform.rotation = Quaternion.Euler(pr.rotation.x, 0f, 0f);
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
        //change active camera
        if(playerCamera == null || !playerCamera.isActiveAndEnabled)
            playerCamera = FindObjectOfType<Camera>();

        MoveUpdate();

        //animation states:
        //characterAnim.SetBool("Airborne", motionState == MotionState.air);
        //characterAnim.SetBool("Crouch", motionState == MotionState.crouch);

        MouseUpdate();
        if(playerInfo != null)
            playerInfo.UpdateCamera(playerCamera);
    }

    private void MouseUpdate() {
        //input
        //bool click = Input.GetMouseButtonDown(0);

        //TODO: FPS movement
        float sensitivity = 10f;

        for(int i = 0; i < rotSmoothing - 1; i++) {
            prevRotX[i] = prevRotX[i + 1];
            prevRotY[i] = prevRotY[i + 1];
        }

        float newRotX = prevRotX[rotSmoothing - 1] + Input.GetAxis("Mouse X") * sensitivity;
        prevRotX[rotSmoothing - 1] = newRotX;
        float newRotY = prevRotY[rotSmoothing - 1] + Input.GetAxis("Mouse Y") * sensitivity;
        prevRotY[rotSmoothing - 1] = Mathf.Clamp(newRotY, minY, maxY);

        rotationX = 0f;
        rotationY = 0f;
        for(int i = 0; i < rotSmoothing; i++) {
            rotationX += prevRotX[i];
            rotationY += prevRotY[i];
        }
        rotationX /= rotSmoothing;
        rotationY /= rotSmoothing;

        transform.localEulerAngles = new Vector3(0, rotationX, 0);
        playerCamera.transform.localRotation = Quaternion.Euler(-rotationY, 0, 0);
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
        bool right = InputInterface.input.GetKey("right");
        bool left = InputInterface.input.GetKey("left");
        bool forward = InputInterface.input.GetKey("forward");
        bool backward = InputInterface.input.GetKey("backward");
        bool jumpDown = InputInterface.input.GetKeyDown("jump");
        bool jump = InputInterface.input.GetKey("jump");
        bool crouch = InputInterface.input.GetKey("crouch");

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

        //seperate motion updates
        switch(motionState) {
        case MotionState.ground:
        GroundMove(movement, jumpDown, crouch);
        break;
        case MotionState.air:
        AirMove(movement, jump);
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
        //float angle = Vector3.SignedAngle(this.forward, horVel, Vector3.up);
        //characterAnim.SetBool("Moving", horSpeed > 0.01f);
        //characterAnim.SetFloat("MoveAngle", angle);

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
            AirMove(movement / walkSpeed, true);
        }
        else if(crouch) {
            motionState = MotionState.crouch;
            horVel *= crouchSpeed / walkSpeed;
            MoveCCWalking();
        }
        else {
            //simply walking
            MoveCCWalking();

            if(!hitGround) {
                //walked off of an object
                vertVel = 0f;
                motionState = MotionState.air;
            }
        }
    }

    private void Jump() {
        jumpTime = Time.deltaTime;
        //characterAnim.SetTrigger("Jump");
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

    private void CrouchMove(Vector3 movement, bool crouch) {
        float dv = Time.deltaTime * (crouchSpeed / accelTime);
        movement *= crouchSpeed;

        horVel = Vector3.MoveTowards(horVel, movement, dv);

        MoveCCWalking();

        if(!hitGround) {
            //walked off of an object
            vertVel = 0f;
            motionState = MotionState.air;
        }
        else if(!crouch) {
            //stand up
            bool canStandUp = CheckIfClearForStandingUp();
            if(canStandUp)
                motionState = MotionState.ground;
        }
    }

    private float GetGravMultiplier(float max, float time, float v0, float g) {
        float b = Mathf.Sqrt(g * (max * max - 1) / v0);
        return Mathf.Max(max - b * time, 1f);
    }

    private void AirMove(Vector3 movement, bool jump) {
        float velFactor = Vector3.Dot(velocity, movement.normalized) / jumpSpeed;
        float steering = Mathf.Lerp(airSteeringMax, airSteeringMin, velFactor);
        Vector3 acceleration = movement * steering;
        Vector3 grav = Physics.gravity;

        //unphysically push player down when hes not holding the jump button
        if(jumpTime > 0f) {
            jumpTime += Time.deltaTime;
            if(!jump)
                grav *= GetGravMultiplier(antJmpGMulplr, jumpTime, jumpSpeed, grav.magnitude);
        }

        acceleration += grav;

        float vertDrag = vertVel < 0 ? verticalDrag * vertVel : 0f;
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

        float dist = standingHeight - crouchingHeight + cc.skinWidth;
        float headHeight = (cc.height / 2) - standingRadius - cc.skinWidth;
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
        float hor = Vector3.Dot(horVel.normalized, hit.normal);

        //set motions state according to what we hit...
        if(y > wallLimit) {
            //we hit the ground
            hitGround = true;
            moveSound.SetLastGroundObject(hit.collider);
            walkSlope = Mathf.Rad2Deg*Mathf.Atan2(y, hor) - 90f;
            SetPlatform(hit.transform);
            if(motionState == MotionState.air && vertVel <= 0f)
                Land();
        }
        else if(y > 0f) {
            //hit slope we should slide off of
            velocity -= Vector3.Project(velocity, hit.normal);
        }

        //TODO provide option to use rb.GetPointVelocity(hit.point) to carry player in stead
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

    private void Land() {
        motionState = MotionState.ground;
        moveSound.Jump();
        jumpTime = 0f;
        vertVel = 0f;
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

        //Vector3 fro = transform.position;
        cc.Move((velocity + shiftVelocity) * dt);
        //velocity = ((transform.position - fro) / dt) - shiftVelocity;
        
        cc.Move(acceleration * dt * dt / 2f);
        velocity += acceleration * dt;
    }

    /// <summary>
    /// returns true if the given array contains any hits with solid terrain
    /// </summary>
    public static bool CollidesWithTerrain(RaycastHit[] hits, out RaycastHit actualHit) {
        foreach(RaycastHit hit in hits) {
            actualHit = hit;

            int layer = hit.collider.gameObject.layer;
            bool terrainLayer = layer == 0;

            //in ignored mask
            if(!terrainLayer)
                continue;

            //triggers
            if(hit.collider.isTrigger)
                continue;

            //characters (including player)
            if(hit.collider.GetComponent<CharacterController>())
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

    /// <summary>
    /// returns true if the given array contains any hits with solid terrain
    /// </summary>
    public static bool CollidesWithTerrain(RaycastHit[] hits) {
        RaycastHit hit;
        return CollidesWithTerrain(hits, out hit);
    }

    public void SetVelocity(Vector3 velocity) {
        this.velocity = velocity;
        bool onGround = motionState == MotionState.ground || motionState == MotionState.crouch;
        if(vertVel > 0f && onGround)
            motionState = MotionState.air;

    }

    public void ShiftVelocity(Vector3 velocity) {
        this.shiftVelocity += velocity;
    }

    public void ClimbState(Vector3 climbDirection) {
        motionState = MotionState.climb;
        this.climbDir = climbDirection;
        this.platform = null;
    }

    public void SwimState() {
        if(motionState == MotionState.climb)
            return;
        motionState = MotionState.swim;
        this.platform = null;
    }
}
