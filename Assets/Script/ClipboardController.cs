using UnityEngine;
using DG.Tweening;
using Zenject;

public class ClipboardController : MonoBehaviour
{
    [System.Serializable]
    public class Settings
    {
        public KeyCode PickupKey = KeyCode.E;
        public KeyCode ToggleKey = KeyCode.Tab;
        public float InteractDistance = 3f;
        public float AnimationDuration = 0.4f;
    }

    [SerializeField] private GameObject worldTableClipboard;
    [SerializeField] private Transform heldClipboardTransform;
    [SerializeField] private Vector3 inspectPosition = new Vector3(0f, -0.1f, 0.4f);
    [SerializeField] private Vector3 inspectRotation = new Vector3(10f, 0f, 0f);
    [SerializeField] private Vector3 hiddenPosition = new Vector3(0f, -0.8f, 0.3f);
    [SerializeField] private Vector3 hiddenRotation = new Vector3(45f, 0f, 0f);

    private TaskManager _taskManager;
    private Camera _playerCamera;
    private Settings _settings;

    private bool _hasClipboard;
    private bool _isInspecting;
    private Tween _moveTween;
    private Tween _rotateTween;

    [Inject]
    public void Construct(TaskManager taskManager, Camera playerCamera, Settings settings)
    {
        _taskManager = taskManager;
        _playerCamera = playerCamera;
        _settings = settings;
    }

    private void Start()
    {
        if (heldClipboardTransform != null)
        {
            heldClipboardTransform.localPosition = hiddenPosition;
            heldClipboardTransform.localEulerAngles = hiddenRotation;
            heldClipboardTransform.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (_settings == null) return;

        if (!_hasClipboard)
        {
            CheckPickup();
        }
        else if (Input.GetKeyDown(_settings.ToggleKey))
        {
            ToggleInspection();
        }
    }

    private void CheckPickup()
    {
        if (!Input.GetKeyDown(_settings.PickupKey)) return;

        Camera cam = _playerCamera != null ? _playerCamera : Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, _settings.InteractDistance))
        {
            if (hit.collider.gameObject == worldTableClipboard || hit.transform.IsChildOf(worldTableClipboard.transform))
            {
                PickupClipboard();
            }
        }
    }

    private void PickupClipboard()
    {
        _hasClipboard = true;

        if (worldTableClipboard != null)
            worldTableClipboard.SetActive(false);

        if (heldClipboardTransform != null)
            heldClipboardTransform.gameObject.SetActive(true);

        ToggleInspection();
    }

    private void ToggleInspection()
    {
        _isInspecting = !_isInspecting;

        _moveTween?.Kill();
        _rotateTween?.Kill();

        Vector3 targetPos = _isInspecting ? inspectPosition : hiddenPosition;
        Vector3 targetRot = _isInspecting ? inspectRotation : hiddenRotation;

        if (heldClipboardTransform != null)
        {
            _moveTween = heldClipboardTransform.DOLocalMove(targetPos, _settings.AnimationDuration)
                .SetEase(_isInspecting ? Ease.OutBack : Ease.InBack);

            _rotateTween = heldClipboardTransform.DOLocalRotate(targetRot, _settings.AnimationDuration)
                .SetEase(Ease.OutQuad);
        }
    }

    private void OnDestroy()
    {
        _moveTween?.Kill();
        _rotateTween?.Kill();
    }
}