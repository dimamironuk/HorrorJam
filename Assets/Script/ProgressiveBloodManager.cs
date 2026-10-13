using System.Collections.Generic;
using UnityEngine;

public class ProgressiveBloodManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CrateDeliveryZone deliveryZone;
    [SerializeField] private Transform playerCamera;

    [Header("Blood Decals (Ordered)")]
    [SerializeField] private List<GameObject> bloodDecals = new List<GameObject>();

    [Header("Timing & Delay")]
    [SerializeField] private float delayAfterDelivery = 2.5f; 

    [Header("Spawn Conditions")]
    [SerializeField] private float minDistanceToSpawn = 2.0f;
    [SerializeField, Range(-1f, 0f)] private float viewAngleThreshold = -0.4f;

    private int _pendingDecalIndex = -1;
    private float _timer = 0f;

    private void OnEnable()
    {
        if (deliveryZone != null)
            deliveryZone.onCrateCountChanged.AddListener(OnCrateCountChanged);
    }

    private void OnDisable()
    {
        if (deliveryZone != null)
            deliveryZone.onCrateCountChanged.RemoveListener(OnCrateCountChanged);
    }

    private void OnCrateCountChanged(int currentCount, int requiredCount)
    {
        int targetIndex = currentCount - 1;
        if (targetIndex >= 0 && targetIndex < bloodDecals.Count)
        {
            _pendingDecalIndex = targetIndex;
            _timer = delayAfterDelivery; 
        }
    }

    private void Update()
    {
        if (_pendingDecalIndex < 0 || _pendingDecalIndex >= bloodDecals.Count || playerCamera == null)
            return;

        if (_timer > 0f)
        {
            _timer -= Time.deltaTime;
            return;
        }

        GameObject targetDecal = bloodDecals[_pendingDecalIndex];
        if (targetDecal == null || targetDecal.activeSelf)
        {
            _pendingDecalIndex++;
            return;
        }

        Vector3 dirToDecal = (targetDecal.transform.position - playerCamera.position).normalized;
        float dot = Vector3.Dot(playerCamera.forward, dirToDecal);
        float distance = Vector3.Distance(playerCamera.position, targetDecal.transform.position);

        if (dot < viewAngleThreshold && distance >= minDistanceToSpawn)
        {
            targetDecal.SetActive(true);
            _pendingDecalIndex++;
        }
    }
}