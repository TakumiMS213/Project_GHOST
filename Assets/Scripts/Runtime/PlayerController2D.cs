using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TelegGhost.Runtime
{
    [DefaultExecutionOrder(-350)]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PlayerController2D : MonoBehaviour
    {
        private const float UpwardViewAngleDegrees = 35f;

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Gameplay";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string resetActionName = "Reset";
        [SerializeField] private string toggleViewActionName = "ToggleView";
        [SerializeField] private string lookUpActionName = "LookUp";
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float groundAcceleration = 55f;
        [SerializeField] private float airAcceleration = 28f;
        [SerializeField] private float jumpImpulse = 10.5f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.1f;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.16f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private Transform facingRoot;
        [SerializeField] private int initialViewHorizontalSign = 1;
        [SerializeField] private bool initialLookingUp;

        private Rigidbody2D cachedRigidbody;
        private InputActionMap gameplayMap;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction resetAction;
        private InputAction toggleViewAction;
        private InputAction lookUpAction;
        private float moveInput;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private int viewHorizontalSign = 1;
        private bool lookingUp;
        private bool inputEnabled = true;

        public event Action ResetRequested;

        public Vector2 FacingDirection => ComposeViewDirection(viewHorizontalSign, lookingUp);
        public int ViewHorizontalSign => viewHorizontalSign;
        public bool IsLookingUp => lookingUp;

        private void Awake()
        {
            cachedRigidbody = GetComponent<Rigidbody2D>();
            if (cachedRigidbody == null)
            {
                enabled = false;
                return;
            }

            if (inputActions == null)
            {
                enabled = false;
                return;
            }

            gameplayMap = inputActions.FindActionMap(actionMapName, false);
            moveAction = gameplayMap?.FindAction(moveActionName, false);
            jumpAction = gameplayMap?.FindAction(jumpActionName, false);
            resetAction = gameplayMap?.FindAction(resetActionName, false);
            toggleViewAction = gameplayMap?.FindAction(toggleViewActionName, false);
            lookUpAction = gameplayMap?.FindAction(lookUpActionName, false);
            ResetView();
        }

        private void OnEnable()
        {
            gameplayMap?.Enable();
        }

        private void OnDisable()
        {
            gameplayMap?.Disable();
        }

        private void Update()
        {
            if (!inputEnabled)
            {
                moveInput = 0f;
                return;
            }

            moveInput = moveAction?.ReadValue<float>() ?? 0f;
            if (toggleViewAction != null && toggleViewAction.WasPressedThisFrame())
            {
                SetViewHorizontal(-viewHorizontalSign);
            }

            lookingUp = lookUpAction != null && lookUpAction.IsPressed();

            if (jumpAction != null && jumpAction.WasPressedThisFrame())
            {
                jumpBufferTimer = jumpBufferTime;
            }

            if (resetAction != null && resetAction.WasPressedThisFrame())
            {
                ResetRequested?.Invoke();
            }

            jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (cachedRigidbody == null)
            {
                return;
            }

            bool grounded = IsGrounded();
            coyoteTimer = grounded ? coyoteTime : Mathf.Max(0f, coyoteTimer - Time.fixedDeltaTime);

            float acceleration = grounded ? groundAcceleration : airAcceleration;
            Vector2 velocity = cachedRigidbody.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, moveInput * moveSpeed, acceleration * Time.fixedDeltaTime);

            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                velocity.y = jumpImpulse;
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }

            cachedRigidbody.linearVelocity = velocity;
        }

        public void Teleport(Vector2 position)
        {
            if (cachedRigidbody == null)
            {
                return;
            }

            cachedRigidbody.position = position;
            cachedRigidbody.linearVelocity = Vector2.zero;
            Vector3 worldPosition = transform.position;
            worldPosition.x = position.x;
            worldPosition.y = position.y;
            transform.position = worldPosition;
        }

        public void SetInputEnabled(bool shouldEnable)
        {
            inputEnabled = shouldEnable;
            if (!shouldEnable)
            {
                moveInput = 0f;
                lookingUp = false;
            }
        }

        public void ConfigureInitialView(int horizontalSign, bool lookUp)
        {
            initialViewHorizontalSign = horizontalSign >= 0 ? 1 : -1;
            initialLookingUp = lookUp;
            ResetView();
        }

        public void ResetView()
        {
            lookingUp = initialLookingUp;
            SetViewHorizontal(initialViewHorizontalSign);
        }

        public static Vector2 ComposeViewDirection(int horizontalSign, bool lookUp)
        {
            float x = horizontalSign >= 0 ? 1f : -1f;
            if (!lookUp)
            {
                return new Vector2(x, 0f);
            }

            float radians = UpwardViewAngleDegrees * Mathf.Deg2Rad;
            return new Vector2(x * Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private void SetViewHorizontal(int sign)
        {
            viewHorizontalSign = sign >= 0 ? 1 : -1;
            if (facingRoot == null)
            {
                return;
            }

            Vector3 scale = facingRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * viewHorizontalSign;
            facingRoot.localScale = scale;
        }

        private bool IsGrounded()
        {
            return groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask) != null;
        }
    }
}
