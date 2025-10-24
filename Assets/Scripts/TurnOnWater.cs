using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnOnWater : MonoBehaviour
{
    public GameObject water;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            water.SetActive(true);
        }
    }
}
