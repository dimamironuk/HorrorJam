using UnityEngine;
using UnityEngine.Events;

public class CrateDeliveryZone : MonoBehaviour
{
    [Header("Task Settings")]
    [SerializeField] private string targetTaskId = "deliver_crates_desk";
    [SerializeField] private int cratesForBlackout = 3;  
    [SerializeField] private int totalCratesForEnding = 9;

    [Header("Crate Settings")]
    [SerializeField] private string crateTag = "Pickupable";

    [Header("Events")]
    public UnityEvent<string> onTaskTriggered;
    public UnityEvent onBlackoutTriggered; 
    public UnityEvent onEndingTriggered;  
    public UnityEvent<int, int> onCrateCountChanged;

    private int _currentCrateCount = 0;
    private bool _blackoutDone = false;
    private bool _endingDone = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(crateTag))
        {
            _currentCrateCount++;

            onCrateCountChanged?.Invoke(_currentCrateCount, totalCratesForEnding);

            if (_currentCrateCount >= cratesForBlackout && !_blackoutDone)
            {
                _blackoutDone = true;
                onTaskTriggered?.Invoke(targetTaskId);
                onBlackoutTriggered?.Invoke();
            }

            if (_currentCrateCount >= totalCratesForEnding && !_endingDone)
            {
                _endingDone = true;
                onEndingTriggered?.Invoke();
            }
        }
    }
}