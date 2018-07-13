using UnityEngine;
using System.Collections;
using System;

public class UFPlayerMovement : MonoBehaviour {

    public Animator characterAnim;
    public Transform heartPoint;

    CharacterController cc;
    Camera playerCamera;
    UFPlayerMoveSounds moveSound;

    MotionState motionState;
    public enum MotionState {
        ground, air, crouch, climb
    }

    Vector3 velocity;
    bool hitGround;
    float jumpTime;

    float rotationX, rotationY;
    private const int rotSmoothing = 3;
    const float minY = -90f;
    const float maxY = 90f;
    float[] prevRotX, prevRotY;

    Vector3 shiftVelocity;
    Vector3 climbDir;

    //movement constants
    private const float walkSpeed = 8f; //movement speed in m/s
    private const float flySpeed = 6f; //target horizontal movement speed while airborne
    private const float climbSpeed = 8f; //target speed while climbing ladders or fences
    private const float accelTime = 0.2f; //time needed to achieve walkspeed
    private const float verticalDrag = 0.5f; //aerial vertical drag force
    private const float airSteering = 12f; //push force the player can use to steer while airborne
    private const float climbSteering = 40f; //Same as airsteering but for climbing
    private const float jumpHeight = 1.3f; //height in m the player can jump
    private const float minJumpSpeed = 1f; //additive jump speed when player is walking up a ramp
    private const float stepOver = 0.2f; //Highest distance player can safely step over
    private const float footing = 0.01f; //extra distance to make sure player holds on to the ground
    private const float slideSlope = 50f; //Angle that seperates floors and ceilings from walls
    private const float crouchSpeed = 6f; //movement speed while crouching
    private const float standingHeight = 1.9f, crouchingHeight = 1.15f; //cc height
    private const float standingRadius = 0.6f, crouchingRadius = 0.55f; //cc radius
    private const float antJmpGMulplr = 4f; //maximum gravity multiplier used to cut jump short

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
    private float horizontalDrag { get { return airSteering / jumpSpeed; } }
    private float climbDrag { get { return climbSteering / climbSpeed; } }

    float wallLimit { get { return Mathf.Sin((90 - slideSlope) * Mathf.Deg2Rad); } }

    private void Awake() {
        //set up nearby connections
        cc = this.GetComponent<CharacterController>();
        playerCamera = FindObjectOfType<Camera>();
        moveSound = this.GetComponentInChildren<UFPlayerMoveSounds>();
        SetRotSmoothing(rotSmoothing);
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
    }

    private void MouseUpdate() {
        //input
        bool click = Input.GetMouseButtonDown(0);

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
        bool right = InputInterface.GetKey("right");
        bool left = InputInterface.GetKey("left");
        bool forward = InputInterface.GetKey("forward");
        bool backward = InputInterface.GetKey("backward");
        bool jumpDown = InputInterface.GetKeyDown("jump");
        bool jump = InputInterface.GetKey("jump");
        bool crouch = InputInterface.GetKey("crouch");

        hitGround = false;

        //horizontal motion
        float horizontal = (right ? 1f : 0f) - (left ? 1f : 0f);
        float vertical = (forward ? 1f : 0f) - (backward ? 1f : 0f);
        Vector3 movement = (new Vector3(horizontal , 0f, vertical)).normalized;
        movement = transform.rotation * movement;

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
        default:
        Debug.LogError("Unexpected motion state: " + motionState);
        break;
        }

        //animation motion state
        float angle = Vector3.SignedAngle(this.forward, horVel, Vector3.up);
        //characterAnim.SetBool("Moving", horSpeed > 0.01f);
        //characterAnim.SetFloat("MoveAngle", angle);

        //character height
        SetCharacterHeight(crouch);

        //shift velocity
        shiftVelocity = Vector3.zero;

    }

    private void GroundMove(Vector3 movement, bool jumpDown, bool crouch) {
        float dv = Time.deltaTime * (walkSpeed / accelTime);
        movement *= walkSpeed;

        horVel = Vector3.MoveTowards(horVel, movement, dv);

        if(jumpDown) {
            //starting a jump
            jumpTime = Time.deltaTime;
            //characterAnim.SetTrigger("Jump");
            moveSound.Jump();

            float oldSpd = this.velocity.y;
            if(jumpSpeed < oldSpd + minJumpSpeed)
                vertVel += minJumpSpeed;
            else
                vertVel = jumpSpeed;

            motionState = MotionState.air;
            AirMove(movement / walkSpeed, true);
        }
        else if(crouch) {
            motionState = MotionState.crouch;
            horVel /= 2;
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
        Vector3 acceleration = movement * airSteering;
        Vector3 grav = Physics.gravity;

        //unphysically push player down when hes not holding the jump button
        if(jumpTime > 0f) {
            jumpTime += Time.deltaTime;
            if(!jump)
                grav *= GetGravMultiplier(antJmpGMulplr, jumpTime, jumpSpeed, grav.magnitude);
        }

        acceleration += grav;

        Vector3 horDrag = horVel * horizontalDrag;
        float vertDrag = vertVel < 0 ? verticalDrag * vertVel : 0f;
        Vector3 drag = horDrag + Vector3.up * vertDrag;
        acceleration -= drag;

        MoveCCFlying(acceleration);
    }

    private void ClimbMove(Vector3 horMovement, bool up, bool down) {
        float vert = (up ? 1f : 0f) - (down ? 1f : 0f);
        Quaternion revRot = Quaternion.Euler(0f, -rotationX, 0f);
        Quaternion upRot = Quaternion.Euler(-rotationY, rotationX, 0f) * revRot;
        Vector3 movement = upRot * horMovement + vert * climbDir;

        if(movement != Vector3.zero)
            movement = movement.normalized;

        Vector3 acceleration = movement * climbSteering;
        Vector3 drag = climbDrag * velocity;
        MoveCCFlying(acceleration - drag);
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

        cc.height = Mathf.MoveTowards(cc.height, targetHeight, heightDelta * dt);
        cc.radius = Mathf.MoveTowards(cc.radius, targetRadius, radiusDelta * dt);
        cc.center = Vector3.up * (cc.height / 2f);
    }

    private bool CheckIfClearForStandingUp() {
        RaycastHit[] hits;
        RaycastHit hit = new RaycastHit();

        float dist = standingHeight - crouchingHeight + cc.skinWidth;
        float headHeight = (cc.height / 2) - cc.radius - cc.skinWidth;
        Vector3 headPos = this.transform.position + cc.center;
        headPos += this.transform.up * headHeight;

        hits = Physics.SphereCastAll(headPos, cc.radius, transform.up, dist);
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
            if(motionState == MotionState.air)
                Land();
        }
        else if(y > 0f) {
            //hit slope we should slide off of
            velocity -= Vector3.Project(velocity, hit.normal);
        }
    }

    private void Land() {
        motionState = MotionState.ground;
        moveSound.Jump();
        jumpTime = 0f;
    }

    /// <summary>
    /// Moves player over the ground at his velocity.
    /// Any vertical component of the velocity is ignored.
    /// </summary>
    void MoveCCWalking() {
        float dt = Time.deltaTime;

        if(!cc.gameObject.activeInHierarchy || !cc.enabled || dt <= 0f)
            return;

        //Vector3 fro = this.transform.position;

        Vector3 move = (velocity + shiftVelocity) * Time.deltaTime;
        move = Vector3.ProjectOnPlane(move, Vector3.up);
        Vector3 vertShift = Vector3.up * stepOver;

        //walk sequence:

        //pull in collider
        cc.height -= stepOver;
        Vector3 center = cc.center;
        cc.center = center - (vertShift / 2f);
        cc.Move(vertShift);

        //move horizontally
        cc.Move(move);

        //move back down to the ground
        cc.Move(-vertShift);

        //move further to catch downward slope
        float slopeCatch = move.magnitude * Mathf.Tan(Mathf.Deg2Rad * slideSlope) + footing;
        cc.Move(Vector3.down * slopeCatch);

        //extend collider
        cc.center = center;
        cc.height += stepOver;

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
    }
}
