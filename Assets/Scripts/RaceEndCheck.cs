using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceEndCheck : MonoBehaviour
{
    public bool reachEnd = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            reachEnd = true;
        }
    }
}
