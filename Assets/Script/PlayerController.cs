using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Zenject;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [System.Serializable]
    public class Settings
    {
        public float WalkSpeed = 3.2f;
        public float SprintSpeed = 5.5f;
        public float CrouchSpeed = 1.6f;
        public float Acceleration = 10f;
        public float Gravity = -19.62f;
        public float JumpHeight = 1.0f;

        public float MaxStamina = 100f;
        public float StaminaDrainRate = 25f;
        public float StaminaRegenRate = 18f;
        public float StaminaRegenDelay = 1.2f;

        public KeyCode CrouchKey = KeyCode.LeftControl;
        public float CrouchHeight = 1.0f;
        public float StandingHeight = 2.0f;
        public float CrouchCamYOffset = -0.3f;
        public float CrouchTransitionSpeed = 10f;

        public float MouseSensitivity = 2.0f;
        public float MinPitch = -85f;
        public float MaxPitch = 85f;

        public float InteractDistance = 3.0f;
        public KeyCode InteractKey = KeyCode.E;
        public KeyCode FlashlightKey = KeyCode.F;

        public float BobFrequency = 2.2f;
        public float BobVerticalAmount = 0.06f;
        public float BobHorizontalAmount = 0.03f;
        public float BobTiltAmount = 1.2f;
        public float TiltAmount = 2.0f;
        public float TiltDuration = 0.2f;
        public float SprintFovOffset = 4.0f;
        public float FlashlightSwaySpeed = 8f;
    }

    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Light flashlight;
    [SerializeField] private Transform flashlightHolder;
    [SerializeField] private Transform holdPoint;

    [SerializeField] private Slider staminaSlider;
    [SerializeField] private CanvasGroup staminaCanvasGroup;

    private Settings _settings;
    private Vector3 _velocity;
    private Vector2 _inputVector;
    private float _cameraPitch;
    private float _currentSpeed;
    private bool _isSprinting;
    private bool _isCrouching;
    private bool _wasGrounded;
    private float _bobTimer;

    private float _currentStamina;
    private float _staminaRegenTimer;
    private float _currentTargetAlpha = -1f;
    private Tween _staminaFadeTween;

    private float _currentTilt;
    private float _bobRoll;

    private float _currentCrouchCamY;
    private Vector3 _defaultControllerCenter;

    private bool _hasFlashlight = false;
    private bool _canControl = true;
    private PickupableItem _currentlyHeldItem;

    private Tween _fovTween;
    private Tween _flashlightToggleTween;
    private float _defaultFov;
    private Vector3 _initialCamPos;

    public bool HasFlashlight => _hasFlashlight;
    public bool IsCrouching => _isCrouching;
    public float CurrentStamina => _currentStamina;
    public PickupableItem CurrentlyHeldItem => _currentlyHeldItem;

    [Inject]
    public void Construct(Settings settings)
    {
        _settings = settings;
    }

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (playerCamera == null && cameraHolder != null)
            playerCamera = cameraHolder.GetComponentInChildren<Camera>();

        if (playerCamera != null)
            _defaultFov = playerCamera.fieldOfView;

        if (cameraHolder != null)
            _initialCamPos = cameraHolder.localPosition;

        if (flashlight != null)
            flashlight.enabled = false;

        if (controller != null)
        {
            controller.height = _settings.StandingHeight;
            _defaultControllerCenter = controller.center;
        }

        if (_settings != null)
            _currentStamina = _settings.MaxStamina;

        if (staminaCanvasGroup != null)
        {
            staminaCanvasGroup.alpha = 0f;
            _currentTargetAlpha = 0f;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetControlActive(bool active)
    {
        _canControl = active;
        _inputVector = Vector2.zero;
        _currentSpeed = 0f;

        if (active)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void HoldItem(PickupableItem item)
    {
        if (_currentlyHeldItem != null) return;
        _currentlyHeldItem = item;
        _currentlyHeldItem.Pickup(holdPoint, controller);
    }

    public void DropCurrentItem(bool throwItem = false)
    {
        if (_currentlyHeldItem == null) return;
        Vector3 throwDir = throwItem ? playerCamera.transform.forward : Vector3.zero;
        _currentlyHeldItem.Drop(throwDir, controller);
        _currentlyHeldItem = null;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (_settings == null) return;
        if (!_canControl) return;

        HandleCrouch();
        HandleInteraction();
        HandleMouseLook();
        HandleMovement();
        HandleStamina();
        HandleHeadStepBob();
        HandleLandingImpact();
        HandleFlashlight();
    }

    private void HandleInteraction()
    {
        if (Input.GetKeyDown(_settings.InteractKey))
        {
            if (_currentlyHeldItem != null)
            {
                DropCurrentItem(throwItem: false);
                return;
            }

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int layerMask = ~LayerMask.GetMask("Player");

            if (Physics.Raycast(ray, out RaycastHit hit, _settings.InteractDistance, layerMask))
            {
                if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
                {
                    interactable.Interact(this);
                }
            }
        }

        if (Input.GetMouseButtonDown(0) && _currentlyHeldItem != null)
        {
            DropCurrentItem(throwItem: true);
        }
    }

    private void HandleStamina()
    {
        bool isMoving = _inputVector.sqrMagnitude > 0.01f;
        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) && _inputVector.y > 0.1f && !_isCrouching && isMoving;

        if (wantsToSprint && _currentStamina > 0f)
        {
            _isSprinting = true;
            _currentStamina -= _settings.StaminaDrainRate * Time.deltaTime;
            _staminaRegenTimer = 0f;

            if (_currentStamina <= 0f)
            {
                _currentStamina = 0f;
                _isSprinting = false;
            }
        }
        else
        {
            _isSprinting = false;

            if (_currentStamina < _settings.MaxStamina)
            {
                _staminaRegenTimer += Time.deltaTime;
                if (_staminaRegenTimer >= _settings.StaminaRegenDelay)
                {
                    _currentStamina += _settings.StaminaRegenRate * Time.deltaTime;
                    if (_currentStamina > _settings.MaxStamina)
                        _currentStamina = _settings.MaxStamina;
                }
            }
        }

        if (staminaSlider != null)
        {
            staminaSlider.value = _currentStamina / _settings.MaxStamina;
        }

        if (staminaCanvasGroup != null)
        {
            bool shouldBeVisible = _currentStamina < (_settings.MaxStamina - 0.01f) || _isSprinting;
            float targetAlpha = shouldBeVisible ? 1f : 0f;

            if (!Mathf.Approximately(_currentTargetAlpha, targetAlpha))
            {
                _currentTargetAlpha = targetAlpha;
                _staminaFadeTween?.Kill();
                _staminaFadeTween = staminaCanvasGroup.DOFade(targetAlpha, 0.4f).SetEase(Ease.InOutQuad);
            }
        }
    }

    private void HandleCrouch()
    {
        bool crouchInput = Input.GetKey(_settings.CrouchKey);

        if (crouchInput && !_isCrouching)
        {
            _isCrouching = true;
        }
        else if (!crouchInput && _isCrouching)
        {
            if (CanStandUp())
            {
                _isCrouching = false;
            }
        }

        float targetHeight = _isCrouching ? _settings.CrouchHeight : _settings.StandingHeight;
        Vector3 targetCenter = _isCrouching
            ? new Vector3(_defaultControllerCenter.x, _settings.CrouchHeight / 2f, _defaultControllerCenter.z)
            : _defaultControllerCenter;

        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * _settings.CrouchTransitionSpeed);
        controller.center = Vector3.Lerp(controller.center, targetCenter, Time.deltaTime * _settings.CrouchTransitionSpeed);

        float targetCamY = _isCrouching ? _settings.CrouchCamYOffset : 0f;
        _currentCrouchCamY = Mathf.Lerp(_currentCrouchCamY, targetCamY, Time.deltaTime * _settings.CrouchTransitionSpeed);
    }

    private bool CanStandUp()
    {
        Ray ray = new Ray(transform.position + controller.center, Vector3.up);
        float rayLength = (_settings.StandingHeight - controller.height) + 0.2f;
        return !Physics.SphereCast(ray, controller.radius, rayLength, ~LayerMask.GetMask("Player"));
    }

    public void UnlockFlashlight()
    {
        _hasFlashlight = true;
        if (flashlight != null) flashlight.enabled = true;
    }

    private void HandleFlashlight()
    {
        if (!_hasFlashlight || flashlight == null) return;

        if (Input.GetKeyDown(_settings.FlashlightKey))
        {
            flashlight.enabled = !flashlight.enabled;

            if (flashlightHolder != null)
            {
                _flashlightToggleTween?.Kill();
                _flashlightToggleTween = flashlightHolder.DOPunchRotation(new Vector3(-8f, 0f, 0f), 0.15f, 4, 0.3f);
            }
        }

        if (flashlightHolder != null)
        {
            flashlightHolder.rotation = Quaternion.Slerp(
                flashlightHolder.rotation,
                cameraHolder.rotation,
                Time.deltaTime * _settings.FlashlightSwaySpeed
            );
        }
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * _settings.MouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * _settings.MouseSensitivity;

        _cameraPitch -= mouseY;
        _cameraPitch = Mathf.Clamp(_cameraPitch, _settings.MinPitch, _settings.MaxPitch);

        transform.Rotate(Vector3.up * mouseX);

        float targetTilt = -Input.GetAxisRaw("Horizontal") * _settings.TiltAmount;
        _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, Time.deltaTime / Mathf.Max(_settings.TiltDuration, 0.01f));

        cameraHolder.localRotation = Quaternion.Euler(_cameraPitch, 0f, _currentTilt + _bobRoll);
    }

    private void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;
        if (isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }

        float targetInputX = Input.GetAxisRaw("Horizontal");
        float targetInputY = Input.GetAxisRaw("Vertical");

        _inputVector = Vector2.MoveTowards(_inputVector, new Vector2(targetInputX, targetInputY), _settings.Acceleration * Time.deltaTime);

        float targetSpeed = _settings.WalkSpeed;
        if (_isCrouching) targetSpeed = _settings.CrouchSpeed;
        else if (_isSprinting) targetSpeed = _settings.SprintSpeed;

        if (_inputVector.sqrMagnitude < 0.01f)
            targetSpeed = 0f;

        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _settings.Acceleration * Time.deltaTime);

        Vector3 moveDirection = (transform.right * _inputVector.x + transform.forward * _inputVector.y).normalized;
        controller.Move(moveDirection * (_currentSpeed * Time.deltaTime));

        if (Input.GetButtonDown("Jump") && isGrounded && !_isCrouching)
        {
            _velocity.y = Mathf.Sqrt(_settings.JumpHeight * -2f * _settings.Gravity);
        }

        _velocity.y += _settings.Gravity * Time.deltaTime;
        controller.Move(_velocity * Time.deltaTime);

        UpdateFov();
    }

    private void HandleHeadStepBob()
    {
        bool isMovingOnGround = controller.isGrounded && _inputVector.sqrMagnitude > 0.1f;
        Vector3 crouchOffsetPos = _initialCamPos + new Vector3(0f, _currentCrouchCamY, 0f);

        if (isMovingOnGround)
        {
            float speedMultiplier = _isSprinting ? 1.4f : (_isCrouching ? 0.7f : 1.0f);
            _bobTimer += Time.deltaTime * _settings.BobFrequency * speedMultiplier * Mathf.PI * 2f;

            float yOffset = Mathf.Abs(Mathf.Sin(_bobTimer)) * _settings.BobVerticalAmount;
            float xOffset = Mathf.Cos(_bobTimer * 0.5f) * _settings.BobHorizontalAmount;
            _bobRoll = Mathf.Sin(_bobTimer) * _settings.BobTiltAmount;

            cameraHolder.localPosition = crouchOffsetPos + new Vector3(xOffset, yOffset, 0f);
        }
        else
        {
            _bobTimer = 0f;
            _bobRoll = Mathf.Lerp(_bobRoll, 0f, Time.deltaTime * 6f);
            cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, crouchOffsetPos, Time.deltaTime * 6f);
        }
    }

    private void HandleLandingImpact()
    {
        bool isGrounded = controller.isGrounded;

        if (isGrounded && !_wasGrounded && _velocity.y < -4f && playerCamera != null)
        {
            playerCamera.transform.DOPunchPosition(Vector3.down * 0.15f, 0.3f, 8, 0.5f);
            playerCamera.transform.DOPunchRotation(new Vector3(4f, 0f, 2f), 0.3f, 6, 0.5f);
        }

        _wasGrounded = isGrounded;
    }

    private void UpdateFov()
    {
        if (playerCamera == null) return;

        float targetFov = _isSprinting ? _defaultFov + _settings.SprintFovOffset : _defaultFov;
        if (Mathf.Abs(playerCamera.fieldOfView - targetFov) > 0.01f && (_fovTween == null || !_fovTween.IsActive()))
        {
            _fovTween = playerCamera.DOFieldOfView(targetFov, 0.3f).SetEase(Ease.OutQuad);
        }
    }

    private void OnDestroy()
    {
        _fovTween?.Kill();
        _flashlightToggleTween?.Kill();
        _staminaFadeTween?.Kill();
    }
}