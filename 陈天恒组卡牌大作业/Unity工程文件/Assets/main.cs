using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class main : MonoBehaviour
{
    public void Play()
    {
        SceneManager.LoadScene("AIScene", LoadSceneMode.Single);
    }

    public void AI()
    {
        SceneManager.LoadScene("2AI", LoadSceneMode.Single);
    }

    public void Player()
    {
        SceneManager.LoadScene("PvP", LoadSceneMode.Single);
    }

    public void Quit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
