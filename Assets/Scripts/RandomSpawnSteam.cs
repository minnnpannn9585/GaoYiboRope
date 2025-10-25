using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomSpawnSteam : MonoBehaviour
{
    [HideInInspector]
    public GameObject[] steams;
    
    public float[] timers;

    private void Start()
    {
        for (int i = 0; i < steams.Length; i++)
        {
            steams[i] = transform.GetChild(i).gameObject;
        }
    }


    void Update()
    {
        for (int i = 0; i < steams.Length; i++)
        {
            timers[i] -= Time.deltaTime;
            if (timers[i] <= 0)
            {
                steams[i].SetActive(!steams[i].activeInHierarchy);
                timers[i] = Random.Range(2.4f, 3.9f);
            }
        }
    }
}
