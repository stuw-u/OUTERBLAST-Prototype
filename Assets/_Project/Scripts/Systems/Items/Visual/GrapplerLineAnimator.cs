using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplerLineAnimator : ItemVisualConfigListener {

    public GrapplerLine grapplerLine;

    public override void OnTriggerPosition (Vector3 position) {
        grapplerLine.targetWorldPosition = position;
        grapplerLine.reachTarget = true;
    }
}
