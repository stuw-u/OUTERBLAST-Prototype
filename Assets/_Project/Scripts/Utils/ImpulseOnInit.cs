using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class ImpulseOnInit : MonoBehaviour {

    public CinemachineImpulseSource impulseSource;

    void Start () {
        impulseSource.GenerateImpulse();
    }
}
