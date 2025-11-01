using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RaceTrigger : MonoBehaviour
{
    public GameObject raceAnt;
    public GameObject raceUi;
    bool raceTriggered = false;
    public RaceEndCheck raceEndCheck;

    float timer = 16f;

    private void Update()
    {
        if (raceTriggered && !raceEndCheck.reachEnd && timer > 0)
        {
            timer -= Time.deltaTime;

            raceUi.GetComponentInChildren<TMP_Text>().text = Mathf.CeilToInt(timer).ToString();

            
        }
        else if (timer <= 0f)
        {
            raceUi.GetComponentInChildren<TMP_Text>().text = "you lose";
            raceAnt.SetActive(false);
        }
        else if(raceEndCheck.reachEnd)
        {
            raceUi.GetComponentInChildren<TMP_Text>().text = "you win";
            raceAnt.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !raceTriggered )
        {
            raceAnt.SetActive(true);
            raceUi.SetActive(true);
            raceTriggered = true;
        }
    }
}
