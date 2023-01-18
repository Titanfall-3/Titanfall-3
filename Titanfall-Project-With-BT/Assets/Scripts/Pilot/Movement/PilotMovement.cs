using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using UnityEngine.Animations.Rigging;
using Utilities;

public class PilotMovement : NetworkBehaviour
{
    [Header("Player Components")]
    CharacterController controller;

    public Animator animator;
    public Rig rig;

    public Transform groundCheck;

    public LayerMask groundMask;

    public GameObject body;
    public GameObject climbCheck;

    public TextMeshProUGUI velocityText;

    public InputValues wallInputDifference;
    public InputValues wallNormalDirection;
    public InputValues boostInputDifference;

    [Header("Basic Movement")]
    Vector3 move;
    Vector3 input;
    public Vector2 moveData;
    Vector3 Yvelocity;
    Vector3 forwardDirection;
    Vector3 jumpForward;

    float speed;

    public float runSpeed;
    public float sprintSpeed;
    public float crouchSpeed;
    public float airSpeedMultiplier;
    public float climbSpeed;
    float desiredSpeed;

    float gravity;
    public float normalGravity;
    public float wallRunGravity;

    public bool canMove = true;

    public bool shouldSprint;
    public bool isSprinting;
    public bool shouldCrouch;
    public bool isCrouching;
    public bool shouldJump;
    public bool isSliding;
    public bool isWallRunning;
    public bool isGrounded;
    public bool isMoving;

    public bool embarking;
    public Vector3 embarkPos;
    public Vector3 lookTarget;

    [Header("Jump")]
    public float jumpHeight;
    float jumpCooldown = 0f;
    int jumpCharges;
    float inAir;

    [Header("Crouch")]
    float startHeight;
    float crouchHeight = 0.5f;
    float slideTimer;
    bool canBoost = true;
    public float maxSlideTimer;
    Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    Vector3 standingCenter = new Vector3(0, 0, 0);

    [Header("Slide")]
    float friction;
    RaycastHit groundHit;
    public float slideSpeedIncrease;

    [Header("Wallrun")]
    bool onLeftWall;
    bool onRightWall;
    bool hasWallRun = false;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    Vector3 wallNormal;
    Vector3 lastWall;
    float wallDistance = 1f;

    [Header("Climbing")]
    bool isClimbing;
    bool hasClimbed;
    bool canClimb;
    private RaycastHit wallHit;
    float climbTimer;
    public float maxClimbTimer;

    [Header("WallJumping")]
    bool isWallJumping;
    float wallJumpTimer;
    public float maxWallJumpTimer;

    [Header("Lurch")]
    float lurchTimer;
    bool lurch;
    bool canLurch = true;
    bool groundBoost;

    [Header("TurnInAir")]
    Vector3 oldForward;
    Vector3 newForward;
    bool turn = true;

    [Header("Camera Effects")]
    public Camera playerCamera;
    float normalFov;
    public float specialFov;
    float slideFov;
    public float cameraChangeTime;
    public float wallRunTilt;
    public float tilt;
    
    void Start()
    {
        if (!HasInputAuthority)
        {
            LayerUtility.ReplaceLayerRecursively(transform,8, 12);
            LayerUtility.ReplaceLayerRecursively(transform,11, 8);
            velocityText.transform.parent.gameObject.SetActive(false);
        }
        
        controller = GetComponent<CharacterController>();
        startHeight = transform.localScale.y;
        normalFov = playerCamera.fieldOfView;
    }

    void IncreaseSpeed(float speedIncrease)
    {
        speed += speedIncrease;
    }

    void DecreaseSpeed(float speedDecrease)
    {
        speed -= speedDecrease * Time.deltaTime;
    }

    void Update()
    {
        if (!HasStateAuthority)
            return;

        if (transform == null)
            return;

        if (embarking)
        {
            MoveToEmbarkPoint(embarkPos);
            EmbarkLookDirection(lookTarget);
        }

        if (canMove == false)
            return;

        velocityText.text = speed.ToString();
        SecondChanceJump();
        HandleInput();

        if (isSliding)
        {
            SlideMovement();
        }
        else if (isGrounded)
        {
            GroundedMovement();
        }
        else if (isWallRunning)
        {
            WallRunMovement();
        }
        else if (isClimbing)
        {
            ClimbMovement();
        }
        else if (!isGrounded)
        {
            AirMovement();
        }

        controller.Move(move * Time.deltaTime);
        ApplyGravity();
        CameraEffects();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (transform == null)
            return;
        
        if (canMove == false)
            return;
        
        if (shouldJump && jumpCharges > 0)
        {
            Invoke("Jump", jumpCooldown);
        }
    }

    void FixedUpdate()
    {
        
        if (!HasStateAuthority)
            return;

        if (transform == null)
            return;
        
        if (canMove == false)
            return;
        
        HandleInput();
        
        CheckGround();
        CheckMoving();
        CheckWallRun();
        CheckClimbing();
        CheckTurnInAir();
        CheckGroundBoost();
        CheckLurch();
        HandleAnimations();

        if (!isMoving)
        {
            speed = 0f;
        }
        else if (isGrounded && isSliding)
        {
            DecreaseSpeed(friction);
        }
        else if (isGrounded || isWallRunning)
        {
            speed = Mathf.SmoothStep(speed, desiredSpeed, 9f * Time.deltaTime);
        }
    }


    #region Input

    void HandleInput()
    {
        input = new Vector3(moveData.x, 0f, moveData.y);

        animator.SetFloat("moveX", input.x, 0.1f, Time.deltaTime);
        animator.SetFloat("moveZ", input.z, 0.1f, Time.deltaTime);

        input = transform.TransformDirection(input);
        input = Vector3.ClampMagnitude(input, 1f);

        if (shouldCrouch && !isCrouching)
        {
            Crouch();
        }
        else if (!shouldCrouch && isCrouching)
        {
            ExitCrouch();
        }

        if (Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            Application.Quit();
        }

        if (shouldSprint && isGrounded && !isCrouching && !isSliding && Vector3.Dot(transform.forward, input) > 0.5)
        {
            isSprinting = true;
        }
        else if (!shouldSprint)
        {
            isSprinting = false;
        }
    }

    #endregion

    #region Groundmovement

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundMask);
        if (isGrounded)
        {
            inAir = 0.2f;
            hasWallRun = false;
            hasClimbed = false;
            climbTimer = maxClimbTimer;
            jumpCooldown = 0f;
        }
        else if (!isGrounded)
        {
            jumpCooldown = Mathf.Lerp(0, jumpCooldown, 0.3f);
        }
        else if (isGrounded && Yvelocity.y == 0)
        {
            jumpCharges = 1;
        }
    }

    void CheckMoving()
    {
        if (input == new Vector3(0f, 0f, 0f) && isGrounded && !isSliding)
        {
            isMoving = false;
        }
        else
        {
            isMoving = true;
        }

        if (isSprinting && Vector3.Dot(transform.forward, input) < 0.5)
        {
            isSprinting = false;
        }

        if (isCrouching)
        {
            isSprinting = false;
        }
    }

    void GroundedMovement()
    {
        desiredSpeed = isSprinting ? sprintSpeed : isCrouching ? crouchSpeed : runSpeed;

        if (input.x != 0)
        {
            move.x += input.x * speed;
        }
        else
        {
            move.x = 0;
        }
        if (input.z != 0)
        {
            move.z += input.z * speed;
        }
        else
        {
            move.z = 0;
        }

        move = Vector3.ClampMagnitude(move, speed);
    }
    
    #endregion

    #region Airmovement

    void Jump()
    {
        if (isWallRunning)
        {
            ExitWallRun();
            jumpForward = transform.forward;
        }
        else if (isGrounded)
        {
            StartCoroutine(LurchTimer());
            jumpForward = transform.forward;
        }
        else
        {
            jumpCharges -= 1;
            if (Vector3.Dot(transform.forward, jumpForward) > boostInputDifference.value)
            {
                IncreaseSpeed(1.2f);
            }
        }

        hasClimbed = false;
        climbTimer = maxClimbTimer;
        jumpCooldown = 0.3f;
        float currentJumpHeight = isCrouching ? jumpHeight * 0.8f : jumpHeight;
        Yvelocity.y = Mathf.Sqrt(currentJumpHeight * -2f * normalGravity);
    }

    void SecondChanceJump()
    {
        if (!isGrounded && !isWallRunning)
            inAir -= 1f * Time.deltaTime;
        if (shouldJump && inAir > 0)
            jumpCharges += 1;
    }

    void CheckLurch()
    {
        if (!isGrounded && !isWallJumping && (Vector3.Dot(-transform.right, input.normalized) > 0.4 || Vector3.Dot(transform.right, input.normalized) > 0.4) && canLurch == true)
        {
            speed -= 1f;
            forwardDirection = input;
            lurch = true;
            lurchTimer = 0.3f;
            canLurch = false;
        }
    }

    IEnumerator LurchTimer()
    {
        canLurch = true;
        yield return new WaitForSeconds(0.3f);
        canLurch = false;
    }

    void AirMovement()
    {
        if (isGrounded)
        {
            move.x += input.x * 0.005f;
            move.z += input.z * 0.005f;
        }
        else
        {
            move.x += input.x * airSpeedMultiplier;
            move.z += input.z * airSpeedMultiplier;
        }
        if (isWallJumping)
        {
            move += forwardDirection * airSpeedMultiplier;
            wallJumpTimer -= 1f * Time.deltaTime;
            if (wallJumpTimer <= 0)
            {
                isWallJumping = false;
            }
        }
        if (lurch)
        {
            move += forwardDirection;
            lurchTimer -= 1f * Time.deltaTime;
            if (lurchTimer <= 0)
            {
                lurch = false;
            }
        }

        move = Vector3.ClampMagnitude(move, speed);
    }

    void CheckTurnInAir()
    {
        if (turn == true && (!isGrounded || isSliding))
        {
            StartCoroutine(TurningDecreasesSpeed());
            turn = false;
        }
    }

    IEnumerator TurningDecreasesSpeed()
    {
        oldForward = transform.forward;
        oldForward = transform.TransformDirection(oldForward);

        yield return new WaitForSeconds(0.1f);

        newForward = transform.forward;
        newForward = transform.TransformDirection(newForward);

        float difference = Vector3.Dot(newForward, oldForward);
        float inputDifference = Vector3.Dot(input, oldForward);

        if (difference < 0.98 && inputDifference < 0.7)
        {
            speed -= ((speed * 0.1f) * (1f - difference));
        }
        if (difference < 0.45 && inputDifference < 0.9)
        {
            speed -= ((speed * 0.3f) * (1f - difference));
        }
        if (speed < 0)
            speed = 0;
        turn = true;
    }

    void ApplyGravity()
    {
        gravity = isWallRunning ? wallRunGravity : isClimbing ? 0f : normalGravity;
        if (!isGrounded)
            Yvelocity.y += gravity * Time.deltaTime;
        controller.Move(Yvelocity * Time.deltaTime);
    }
    
    #endregion
    
    #region Wallrunning 

    void CheckWallRun()
    {
        onRightWall = Physics.Raycast(transform.position, transform.right, out rightWallHit, wallDistance, groundMask);
        onLeftWall = Physics.Raycast(transform.position, -transform.right, out leftWallHit, wallDistance, groundMask);

        if ((onRightWall && Vector3.Dot(transform.right, input.normalized) > wallInputDifference.value || onLeftWall && Vector3.Dot(-transform.right, input.normalized) > wallInputDifference.value) && !isWallRunning && !isGrounded && !isSliding)
        {
            TestWallRun();
        }

        if (!onRightWall && !onLeftWall && isWallRunning)
        {
            ExitWallRun();
        }
    }

    void TestWallRun()
    {
        wallNormal = onRightWall ? rightWallHit.normal : leftWallHit.normal;

        if (Vector3.Dot(wallNormal.normalized, Vector3.up) < -wallNormalDirection.value)
        {
            return;
        }

        if (hasWallRun)
        {
            float wallAngle = Vector3.Angle(wallNormal, lastWall);
            if (wallAngle > 15)
            {
                WallRun();
            }
        }
        else
        {
            hasWallRun = true;
            WallRun();
        }
    }

    void WallRun()
    {
        if (speed < 8)
        {
            desiredSpeed = 10;
        }
        else if (speed < 13)
        {
            desiredSpeed = speed + 2;
        }
        else
        {
            desiredSpeed = speed;
        }

        wallDistance = 2f;
        isWallRunning = true;
        jumpCharges = 1;
        Yvelocity = new Vector3(0f, 0f, 0f);

        forwardDirection = Vector3.Cross(wallNormal, Vector3.up);

        if (Vector3.Dot(forwardDirection, transform.forward) < 0)
        {
            forwardDirection = -forwardDirection;
        }
    }

    void WallRunMovement()
    {

        wallNormal = onRightWall ? rightWallHit.normal : leftWallHit.normal;

        forwardDirection = Vector3.Cross(wallNormal, Vector3.up);

        if (Vector3.Dot(forwardDirection, transform.forward) < 0)
        {
            forwardDirection = -forwardDirection;
        }

        if (Vector3.Dot(forwardDirection.normalized, input.normalized) > 0)
        {
            move += forwardDirection;
        }
        else if (Vector3.Dot(forwardDirection.normalized, input.normalized) < 0)
        {
            move.x = 0;
            move.z = 0;
            ExitWallRun();
        }

        if (Physics.Raycast(transform.position, transform.forward, 1f, groundMask))
        {
            ExitWallRun();
        }

        move.x += input.x * airSpeedMultiplier;
        move = Vector3.ClampMagnitude(move, speed);
    }

    void ExitWallRun()
    {
        isWallRunning = false;
        lastWall = wallNormal;
        forwardDirection = wallNormal;
        inAir = 0.2f;
        isWallJumping = true;
        wallJumpTimer = maxWallJumpTimer;
        wallDistance = 1f;
    }

    #endregion

    #region Crouching/Slideing

    void Crouch()
    {
        controller.height = crouchHeight;
        controller.center = crouchingCenter;
        groundCheck.position += new Vector3(0f, 1f, 0f);
        body.transform.position += new Vector3(0f, 1f, 0f);
        if (speed > 8 || !isGrounded)
        {
            isSliding = true;
            isSprinting = false;
            if (isGrounded && canBoost)
            {
                IncreaseSpeed(slideSpeedIncrease);
                slideFov = 95f;
                StartCoroutine(SlideBoostCooldown());
            }
            else
            {
                slideFov = 92f;
            }
            slideTimer = maxSlideTimer;
        }
        isCrouching = true;
    }

    IEnumerator SlideBoostCooldown()
    {
        canBoost = false;
        yield return new WaitForSeconds(2f);
        canBoost = true;
    }

    void SlideMovement()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out groundHit, 2.2f, groundMask))
        {
            Vector3 normal = groundHit.normal.normalized;

            float difference = ((Vector3.Dot(normal, transform.forward) * -1) + 1);

            friction = 7f * difference;
        }

        move.x += input.x * airSpeedMultiplier;
        move.z += input.z * airSpeedMultiplier;
        move = Vector3.ClampMagnitude(move, speed);

        slideTimer -= 1f * Time.deltaTime;
        if (slideTimer <= 0)
        {
            isSliding = false;
        }
    }

    void CheckGroundBoost()
    {
        if (isGrounded && isSliding && (Vector3.Dot(-transform.right, input.normalized) > boostInputDifference.value || Vector3.Dot(transform.right, input.normalized) > boostInputDifference.value) && groundBoost == true)
        {
            speed += 0.65f;
            groundBoost = false;
            StartCoroutine(BoostCooldown());
        }
    }

    IEnumerator BoostCooldown()
    {
        yield return new WaitForSeconds(3f);
        groundBoost = true;
    }

    void ExitCrouch()
    {
        controller.height = (startHeight * 2);
        controller.center = standingCenter;
        groundCheck.position += new Vector3(0f, -1f, 0f);
        body.transform.position += new Vector3(0f, -1f, 0f);
        isCrouching = false;
        isSliding = false;
    }

    #endregion

    #region Climbing

    void CheckClimbing()
    {
        canClimb = Physics.Raycast(transform.position, transform.forward, out wallHit, 1f, groundMask);
        if (Vector3.Dot(wallHit.normal.normalized, Vector3.up) < -wallNormalDirection.value || Vector3.Dot(wallHit.normal.normalized, Vector3.up) > wallNormalDirection.value)
        {
            return;
        }

        float wallAngle = Vector3.Angle(-wallHit.normal, transform.forward);
        if (wallAngle < 15 && canClimb && !hasClimbed && Vector3.Dot(transform.forward, input.normalized) > wallInputDifference.value)
        {
            isClimbing = true;
        }
        else
        {
            isClimbing = false;
        }
    }

    void ClimbMovement()
    {
        forwardDirection = Vector3.up;
        move.x += input.x * airSpeedMultiplier;
        move.z += input.z * airSpeedMultiplier;

        Yvelocity += forwardDirection;
        speed = climbSpeed;

        move = Vector3.ClampMagnitude(move, speed);
        Yvelocity = Vector3.ClampMagnitude(Yvelocity, speed);

        if (Vector3.Dot(transform.forward, input.normalized) < wallInputDifference.value)
        {
            hasClimbed = true;
            Yvelocity.y = 1f;
            isClimbing = false;
        }

        climbTimer -= 1f * Time.deltaTime;
        if (climbTimer < 0)
        {
            if (Physics.Raycast(transform.position, transform.forward, out wallHit, 1f, groundMask) && Physics.Raycast(climbCheck.transform.position, transform.forward, 4f, groundMask) == false)
            {
                climbTimer += 0.3f;
                speed += 3f;
            }
            else
            {
                hasClimbed = true;
                isClimbing = false;
            }
        }
    }
    
    #endregion

    #region Camera Effects

    void CameraEffects()
    {
        float fov = isWallRunning && !isGrounded ? specialFov : isSliding ? slideFov : normalFov;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, cameraChangeTime * Time.deltaTime);

        if (isWallRunning)
        {
            if (onRightWall && !isGrounded)
            {
                tilt = Mathf.Lerp(tilt, wallRunTilt, cameraChangeTime * Time.deltaTime);
            }
            else if (onLeftWall && !isGrounded)
            {
                tilt = Mathf.Lerp(tilt, -wallRunTilt, cameraChangeTime * Time.deltaTime);
            }
        }
        else
        {
            tilt = Mathf.Lerp(tilt, 0f, cameraChangeTime * Time.deltaTime);
        }
    }

    void HandleAnimations()
    {
        animator.SetBool("isRunning", isGrounded && !isCrouching && !isSprinting && !isSliding);
        animator.SetBool("isCrouching", isCrouching);
        animator.SetBool("isSprinting", isSprinting);
        animator.SetBool("isSliding", isSliding);

        animator.SetBool("inAir", !isGrounded);
        animator.SetBool("rightWall", isWallRunning && onRightWall);
        animator.SetBool("leftWall", isWallRunning && onLeftWall);
        animator.SetBool("isClimbing", isClimbing);

        if (isSprinting || isClimbing)
            rig.weight = 0;
        else
            rig.weight = 1;
    }

    public void EmbarkLookDirection(Vector3 lookTarget)
    {
        Vector3 targetPosition = new Vector3(lookTarget.x, this.transform.position.y, lookTarget.z);
        transform.LookAt(targetPosition);
    }

    void MoveToEmbarkPoint(Vector3 target)
    {
        var offset = target - transform.position;
        //Get the difference.
        if (offset.magnitude > .1f)
        {
            //If we're further away than .1 unit, move towards the target.
            //The minimum allowable tolerance varies with the speed of the object and the framerate. 
            // 2 * tolerance must be >= moveSpeed / framerate or the object will jump right over the stop.
            offset = offset.normalized * 15f;
            //normalize it and account for movement speed.
            controller.Move(offset * Time.deltaTime);
            //actually move the character.
        }
    }
    
    #endregion
}
