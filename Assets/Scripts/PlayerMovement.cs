using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float maxSpeed;
    public float rotationSpeed;
    public float jumpSpeed;
    public float jumpButtonGracePeriod;
    public float idleTimeBeforeDrowsy = 5f; // Time before transitioning to drowsy animation

    public Transform mug; // Reference to the mug transform
    public Transform mugContainer; // Reference to the mug container transform
    private Rigidbody mugRigidbody;
    private bool isHoldingMug = true;

    [SerializeField]
    private Transform cameraTransform;

    private CharacterController characterController;
    private float ySpeed;
    private float originalStepOffset;
    private float? lastGroundedTime;
    private float? jumpButtonPressedTime;

    private bool isJumping;
    private bool isGrounded;

    
    private bool isDrowsy = false;
    private bool isSleeping = false;
    private bool isWakingUp = false;
    private bool isToasting = false;

    private Animator animator;
    private Animation faceAnimator;
    private float idleTimer;

    void Start()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        originalStepOffset = characterController.stepOffset;

        if (mug != null)
        {
            mugRigidbody = mug.GetComponent<Rigidbody>();
            mugRigidbody.isKinematic = true;
            mugRigidbody.useGravity = false;
        }
    }

    void Update()
    {
        // Prevent player movement and actions while sleeping or waking up
        if (isSleeping || isDrowsy || isWakingUp || isToasting)
        {
            
            if (isSleeping) {
                Debug.Log("is Sleeping");
            }
            if (isWakingUp) {
                Debug.Log("is Waking");
            }
            return;
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 movementDirection = new Vector3(horizontalInput, 0, verticalInput);
        float inputMagnitude = Mathf.Clamp01(movementDirection.magnitude);
        Debug.Log("Input Magnitude: " + (inputMagnitude > 0) + ", isMoving: " + animator.GetBool("isMoving"));
        // If the player is holding the shift key, reduce movement speed
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            inputMagnitude /= 2;
        }

        animator.SetFloat("InputMagnitude", inputMagnitude, 0.05f, Time.deltaTime);

        // Reset idle timer if the player is moving
        if (inputMagnitude > 0)
        {
            animator.SetBool("isMoving", true);
            idleTimer = 0;
            if (isDrowsy || isSleeping)
            {
                WakeUp();
            }
        }
        else
        {
            // Increment idle timer if the player is not moving
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleTimeBeforeDrowsy && !isDrowsy && !isWakingUp)
            {
                StartCoroutine(HandleDrowsyAndSleeping());
            }

            if (Input.GetButtonDown("Special") && !isDrowsy && !isWakingUp && characterController.isGrounded)
            {
                idleTimer = 0;
                StartCoroutine(PlayToastAnimation());
            }
        }

        float speed = inputMagnitude * maxSpeed;
        movementDirection = Quaternion.AngleAxis(cameraTransform.rotation.eulerAngles.y, Vector3.up) * movementDirection;
        movementDirection.Normalize();

        ySpeed += Physics.gravity.y * Time.deltaTime * 2;

        if (characterController.isGrounded)
        {
            lastGroundedTime = Time.time;
        }

        if (Input.GetButtonDown("Jump"))
        {
            idleTimer = 0;
            jumpButtonPressedTime = Time.time;
        }

        if (Time.time - lastGroundedTime <= jumpButtonGracePeriod)
        {
            characterController.stepOffset = originalStepOffset;
            ySpeed = -1f;
            animator.SetBool("isGrounded", true);

            animator.SetBool("isJumping", false);
            isJumping = false;
            animator.SetBool("isFalling", false);
            
            if(Time.time - jumpButtonPressedTime <= jumpButtonGracePeriod)
            {
                ySpeed = jumpSpeed;
                animator.SetBool("isJumping", true);
                isJumping = true;
                jumpButtonPressedTime = null;
                lastGroundedTime = null;
            }
        }
        else
        {
            characterController.stepOffset = 0;
            animator.SetBool("isGrounded", false);

            if ((isJumping && ySpeed < 0) || ySpeed < -1)
            {
                
                animator.SetBool("isFalling", true);
            }
        }

        Vector3 velocity = movementDirection * speed;
        velocity.y = ySpeed;

        characterController.Move(velocity * Time.deltaTime);

        if (movementDirection != Vector3.zero)
        {
            animator.SetBool("isMoving", true);
            Quaternion toRotation = Quaternion.LookRotation(movementDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            animator.SetBool("isMoving", false);
        }
    }

    private IEnumerator HandleDrowsyAndSleeping()
    {
        isDrowsy = true;
        animator.SetBool("isDrowsy", true);
        yield return new WaitForSeconds(6f);
        isDrowsy = false;
        isSleeping = true;
        animator.SetBool("isDrowsy", false);
        animator.SetBool("isSleeping", true);
        DropMug();

        while (isSleeping)
        {
            if (Input.anyKeyDown)
            {
                WakeUp();
            }
            yield return null;
        }
    }

    private void WakeUp()
    {
        if (!isSleeping) return;
        if (isWakingUp) return;

        isSleeping = false;
        isWakingUp = true;
        animator.SetBool("isSleeping", false);
        animator.SetBool("isWakingUp", true);
        StartCoroutine(HandleWakeUp());
    }

    private IEnumerator HandleWakeUp()
    {
        yield return new WaitForSeconds(6.7f);
        animator.SetBool("isWakingUp", false);
        PickUpMug();
        isWakingUp = false;
        idleTimer = 0; // Reset idle timer after waking up
    }

    private IEnumerator PlayToastAnimation()
    {

        animator.SetBool("isToast", true);
        isToasting = true;
        // Wait for the duration of the toast animation
        yield return new WaitForSeconds(1.5f);
        animator.SetBool("isToast", false);
        yield return new WaitForSeconds(0.5f);
        isToasting = false;
    }

    private void DropMug()
    {
        if (isHoldingMug && mug != null)
        {
            mug.parent = null; // Unparent the mug from mugContainer
            mugRigidbody.isKinematic = false; // Enable physics for the mug
            mugRigidbody.useGravity = true; // Enable gravity
            isHoldingMug = false;
        }
    }

    private void PickUpMug()
    {
        if (!isHoldingMug && mug != null)
        {
            mug.parent = mugContainer; // Reparent the mug to mugContainer
            mug.localPosition = Vector3.zero; // Reset local position to align with mugContainer
            mug.localRotation = Quaternion.identity; // Reset local rotation
            mugRigidbody.isKinematic = true; // Disable physics for the mug
            mugRigidbody.useGravity = false; // Disable gravity
            isHoldingMug = true;
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
