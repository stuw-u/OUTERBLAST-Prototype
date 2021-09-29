using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneCloser : MonoBehaviour {

    public void Close () {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}
