using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PickupableItem : MonoBehaviour, IInteractable
{
    [Header("Hold Settings")]
    [SerializeField] private Vector3 holdRotationOffset = Vector3.zero;
    [SerializeField] private float throwForce = 4f;
    [SerializeField] private LayerMask collisionLayers = ~0;
    [SerializeField] private float checkRadius = 0.15f;

    private Rigidbody _rb;
    private Collider _collider;
    private Transform _originalParent;
    private Transform _holdPoint;
    private Transform _playerCamera;
    private bool _isHeld = false;

    public bool IsHeld => _isHeld;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _originalParent = transform.parent;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    public void Interact(PlayerController player)
    {
        if (player.CurrentlyHeldItem != null) return;
        player.HoldItem(this);
    }

    public void Pickup(Transform holdPoint, Collider playerCollider)
    {
        _isHeld = true;
        _holdPoint = holdPoint;
        _playerCamera = holdPoint.parent != null ? holdPoint.parent : holdPoint;

        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;

        if (_collider != null)
        {
            _collider.enabled = false;
        }

        transform.SetParent(null);
    }

    private void LateUpdate()
    {
        if (!_isHeld || _holdPoint == null) return;

        Vector3 startPos = _playerCamera != null ? _playerCamera.position : _holdPoint.position;
        Vector3 targetPos = _holdPoint.position;
        Vector3 direction = targetPos - startPos;
        float distance = direction.magnitude;

        if (Physics.SphereCast(startPos, checkRadius, direction.normalized, out RaycastHit hit, distance, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point + hit.normal * checkRadius;
        }
        else
        {
            transform.position = targetPos;
        }

        transform.rotation = _holdPoint.rotation * Quaternion.Euler(holdRotationOffset);
    }

    public void Drop(Vector3 throwDirection, Collider playerCollider)
    {
        _isHeld = false;
        _holdPoint = null;
        transform.SetParent(_originalParent);
        _rb.isKinematic = false;

        if (_collider != null)
        {
            _collider.enabled = true;
        }

        if (throwDirection != Vector3.zero)
        {
            _rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
        }
    }
}