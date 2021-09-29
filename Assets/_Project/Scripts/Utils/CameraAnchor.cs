using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAnchor : MonoBehaviour {

    public Transform anchor;
    public Vector3 anchorOffset;
    public Vector3 lockedOffset;
    public bool lockX;
    public bool lockY;
    public bool lockZ;

    void LateUpdate () {
        if(anchor == null) {
            return;
        }

        transform.position = new Vector3(
            lockX ? (lockedOffset.x) : (anchor.position.x + anchorOffset.x),
            lockY ? (lockedOffset.y) : (anchor.position.y + anchorOffset.y),
            lockZ ? (lockedOffset.z) : (anchor.position.z + anchorOffset.z)
        );
    }
}
