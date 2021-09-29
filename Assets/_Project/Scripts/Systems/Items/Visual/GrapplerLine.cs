using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplerLine : MonoBehaviour {

    public float waveFreq;
    public float waveAmplitude;
    public float waveEndFading;
    public float pointsPerUnit;
    public float animationSpeed;
    public float animationRetreatSpeed;

    public Vector3 targetWorldPosition;
    public LineRenderer line;

    public bool reachTarget = false;
    private float animationTime = 0f;
    private Vector3[] points;
    private int lastPointCount = 0;

    void Update () {
        if(Input.GetKeyDown(KeyCode.K)) {
            reachTarget = true;
        }
        if(reachTarget) {
            animationTime = Mathf.Clamp01(animationTime + Time.deltaTime * animationSpeed);
        } else {
            animationTime = Mathf.Clamp01(animationTime + -Time.deltaTime * animationRetreatSpeed);
        }
        
        if(animationTime == 1f) {
            reachTarget = false;
        }
        line.enabled = animationTime != 0f;

        Vector3 sPos = Vector3.zero;
        Vector3 fPos = Vector3.Lerp(Vector3.zero, transform.InverseTransformPoint(targetWorldPosition), animationTime * animationTime * animationTime);
        float dist = Vector3.Distance(sPos, fPos);
        int pointCount = Mathf.Max(1, Mathf.CeilToInt(dist * pointsPerUnit));
        Quaternion dirQuat = Quaternion.identity;
        if(sPos != fPos) {
            dirQuat = Quaternion.LookRotation((fPos - sPos).normalized, Vector3.up);
        }


        if(pointCount != lastPointCount) {
            points = new Vector3[pointCount];
            line.positionCount = pointCount;
            lastPointCount = pointCount;
        }

        points[0] = Vector3.zero;
        if(pointCount > 1) {
            for(int i = 0; i < pointCount; i++) {
                float t = (float)i / (pointCount - 1);
                float distT = dist - (t * dist);
                Vector3 centerPos = Vector3.Lerp(sPos, fPos, t);
                float startValue = Mathf.Clamp01(Mathf.InverseLerp(0f, waveEndFading, distT));
                float endValue = Mathf.Clamp01(Mathf.InverseLerp(dist, dist - waveEndFading, distT));
                float totalValue = (1f - (startValue * endValue));
                totalValue = totalValue * totalValue;
                totalValue = 1f - totalValue;
                totalValue *= 1f - animationTime;

                points[i] = centerPos + dirQuat * new Vector3(-Mathf.Cos(t * waveFreq) * waveAmplitude * totalValue, 0f, 0f);
            }
        }
        
        if(points != null)
            line.SetPositions(points);
    }
}
