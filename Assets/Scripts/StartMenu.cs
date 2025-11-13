using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public GameObject comicCanvas;
    public void StartBtn()
    {
        comicCanvas.SetActive(true);
        this.gameObject.SetActive(false);
    }
}
