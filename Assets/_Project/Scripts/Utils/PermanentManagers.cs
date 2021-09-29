using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class PermanentManagers : MonoBehaviour {

    public int forceType = -1;
    public GameObject[] managers;

    public static PermanentManagers inst;
    void Awake () {
        if(inst != null) {
            DestroyImmediate(gameObject, false);
            return;
        }

        foreach(GameObject manager in managers) {
            manager.SetActive(true);
        }

        inst = this;
        DontDestroyOnLoad(gameObject);
    }
}
