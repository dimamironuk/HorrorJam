using System.Collections.Generic;
using UnityEngine;

public class BloodDecalController : MonoBehaviour
{
    [Header("List of Blood Decals")]
    [SerializeField] private List<GameObject> bloodDecals = new List<GameObject>();

    public void ShowAllBloodDecals()
    {
        foreach (GameObject decal in bloodDecals)
        {
            if (decal != null)
            {
                decal.SetActive(true);
            }
        }
    }
}