using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartComicTimer : MonoBehaviour
{
    public float timer;
    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            SceneManager.LoadScene(2);
        }
    }
}
