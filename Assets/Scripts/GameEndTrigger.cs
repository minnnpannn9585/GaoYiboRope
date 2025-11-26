using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEndTrigger : MonoBehaviour
{
    

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            GetComponent<AudioSource>().Play();

            Invoke("LoadScene", 1.5f);
        }
    }

    void LoadScene()
    {
        SceneManager.LoadScene(2);
    }
}
