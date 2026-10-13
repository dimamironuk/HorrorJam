using UnityEngine;
using DG.Tweening;

public class DeskInteractiveItem : MonoBehaviour, IInteractable
{
    public enum InteractionType { SlideDrawer, RotateDoor }

    [Header("General Settings")]
    [SerializeField] private InteractionType type = InteractionType.SlideDrawer;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease easeType = Ease.OutQuad;

    [Header("Drawer Settings (Slide)")]
    [SerializeField] private Vector3 openLocalOffset = new Vector3(0f, 0f, 0.4f);

    [Header("Door Settings (Rotate)")]
    [SerializeField] private Vector3 openLocalRotationOffset = new Vector3(0f, -90f, 0f);

    private bool _isOpen = false;
    private Vector3 _closedLocalPos;
    private Vector3 _closedLocalRot;
    private Tween _activeTween;

    private void Awake()
    {
        _closedLocalPos = transform.localPosition;
        _closedLocalRot = transform.localEulerAngles;
    }

    public void Interact(PlayerController player)
    {
        _isOpen = !_isOpen;
        _activeTween?.Kill();

        if (type == InteractionType.SlideDrawer)
        {
            Vector3 targetPos = _isOpen ? _closedLocalPos + openLocalOffset : _closedLocalPos;
            _activeTween = transform.DOLocalMove(targetPos, animationDuration).SetEase(easeType);
        }
        else if (type == InteractionType.RotateDoor)
        {
            Vector3 targetRot = _isOpen ? _closedLocalRot + openLocalRotationOffset : _closedLocalRot;
            _activeTween = transform.DOLocalRotate(targetRot, animationDuration).SetEase(easeType);
        }
    }

    private void OnDestroy()
    {
        _activeTween?.Kill();
    }
}