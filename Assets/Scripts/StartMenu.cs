using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public GameObject comicCanvas;
    public AudioSource btnClickSound;
    public void StartBtn()
    {
        btnClickSound.Play();
        comicCanvas.SetActive(true);
        this.gameObject.SetActive(false);

    }
}
