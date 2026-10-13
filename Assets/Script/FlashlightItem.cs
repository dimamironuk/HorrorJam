using UnityEngine;

public class FlashlightItem : MonoBehaviour, IInteractable
{
    public void Interact(PlayerController player)
    {
        player.UnlockFlashlight();
        gameObject.SetActive(false); 
    }
}