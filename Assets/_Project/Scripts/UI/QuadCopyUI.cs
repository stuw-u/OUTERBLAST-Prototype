using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuadCopyUI : MonoBehaviour
{
    public RectTransform source;
    Vector3[] corners = new Vector3[4];

    void Update () {
        transform.position = new Vector3(source.position.x, source.position.y, 2);
        
        source.GetWorldCorners(corners);
        float size = Mathf.Max((corners[3].x - corners[0].x), (corners[2].y - corners[0].y));
        transform.localScale = new Vector3(size, size, 1f);
    }
}
