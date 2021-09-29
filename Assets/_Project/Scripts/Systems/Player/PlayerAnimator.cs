using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.TubeRenderer;

public enum PlayerAnimationState {
    Default,
    RecoverDelta,
    RecoverJet,
    RecoverGrappler,
    HoldHammer,
    HoldThrowable
}

public class PlayerAnimator : MonoBehaviour {

    #region References and Parameters
    [Header("References")]
    public Transform originLeftArm;
    public Transform topLeftArm;
    public Transform originRightArm;
    public Transform topRightArm;
    public LineRenderer leftArmLine;
    public LineRenderer rightArmLine;
    public TubeRenderer leftArmTube;
    public TubeRenderer rightArmTube;
    public ClawAnimator leftClaw;
    public ClawAnimator rightClaw;

    [Header("Item References")]
    public Transform shieldHoldPoint;
    public Transform hammerHoldPoint;
    public Transform deltaHoldPointLeft;
    public Transform deltaHoldPointRight;
    public Transform deltaItem;
    
    [Header("Body Parameters")]
    public float bodyYOffset = 0.25f;

    [Header("Arm Visuals Parameters")]
    public float maxLength = 2f;
    public int resolution = 10;

    [Header("Anchoring System")]
    public float maxDistanceWihoutReanchor = 0.1f;
    public Vector3 anchorRaycastOriginOffset = Vector3.right;
    public Vector3 anchorRaycastDirection = Vector3.down;
    public float velocityInfluenceToRaycast = 1f;
    public float maxBalance = 1.3f;
    public float maxArmStrech = 2f;

    [Header("Old Anchoring System")]
    public float maxArmReach = 2f;
    public Vector3 maxArmReachDirection = Vector3.down;
    public float motionArcTop = 1f;
    public float motionArcSpeed = 1f;

    [Header("Float Animation")]
    public float anchorToFloatSpeed = 4f;
    public float floatSpeed = 2f;
    public float floatSecondOffset = 2f;
    public float floatSecondSpeed = 2f;
    public float minShakeMul = 0.5f;
    public float maxShakeMul = 2f;
    public float maxShakeVel = 20f;
    public float velocityYDragInfluenceOnY = 0.05f;
    public float velocityYDragInfluenceOnX = 0.025f;
    public float velocityXZDragInfluence = 0.05f;
    public float maxVelocityDeltaPerSecond = 200f;

    [Header("Jet Animation")]
    public float jetFloatSpeed = 2f;
    public float jetFloatSecondOffset = 2f;
    public float jetMinShakeMul = 0.5f;
    public float jetMaxShakeMul = 2f;
    public float jetMaxShakeVel = 20f;
    public float jetVelocityYDragInfluenceOnY = 0.05f;
    public float jetVelocityYDragInfluenceOnX = 0.025f;
    public float jetVelocityXZDragInfluence = 0.05f;
    public float jetMaxVelocityDeltaPerSecond = 200f;

    [Header("Body Spring")]
    public float invBodySpringMass = 1f;
    public float bodySpringK = 1f;
    public float bodySpringB = 1f;
    #endregion

    public PlayerAnimationState state;

    public bool isAnchor;
    public Vector3 velocity;

    // Locals
    private Vector3 invVelSmooth;
    private Vector3 invVel;
    private Vector3 planeVel;
    private Vector3 lastInvVel;

    private Vector3 currentLeftArmWorld;
    private Vector3 currentRightArmWorld;
    private Vector3 targetLeftArmWorld;
    private Vector3 targetRightArmWorld;
    private Vector3 fromLeftArmWorld;
    private Vector3 fromRightArmWorld;
    private Vector3 topLeftArmWorld;
    private Vector3 topRightArmWorld;
    private float leftTimer;
    private float rightTimer;
    private bool priorityLeft = false;

    private float anchorToFloatTimeLeft;
    private Vector3 anchorToFloatPosLeft;
    private float anchorToFloatTimeRight;
    private Vector3 anchorToFloatPosRight;
    private float floatingTime0;
    private float floatingTime1;
    private Vector3 targetVelocity;

    private Vector3 bodySpringPosition;
    private Vector3 bodySpringVelocity;

    private bool isGrounded;
    private bool wasGrounded;
    private Vector3 groundNormal = Vector3.up;
    private Quaternion alignToGround;
    private Quaternion invAlignToGround;
    private Vector3 lastRotAtRaycast;

    private Vector3[] leftPos;
    private Vector3[] rightPos;

    [HideInInspector] public Transform holdPointLeft;
    private Transform lastHoldPointLeft;
    [HideInInspector] public Vector3 lastHoldPositionLeft;
    [HideInInspector] public Transform holdPointRight;
    private Transform lastHoldPointRight;
    [HideInInspector] public Vector3 lastHoldPositionRight;
    private float holdingValueLeft;
    private float unholdingTimerLeft;
    private float holdingValueRight;
    private float unholdingTimerRight;

    private void Start () {
        lastBodyOffsetPos = Vector3.up * bodyYOffset;
        leftPos = new Vector3[1 + resolution];
        rightPos = new Vector3[1 + resolution];
        leftArmTube.positions = leftPos;
        rightArmTube.positions = rightPos;
    }

    void Update () {

        // Grounding
        alignToGround = Quaternion.FromToRotation(transform.up, groundNormal);
        invAlignToGround = Quaternion.Inverse(alignToGround);
        
        // Computes smoothed velocity relative to itself for custom direction support &
        // Attenuates large changes in velocity for a smoother look
        targetVelocity = Vector3.MoveTowards(targetVelocity, velocity, maxVelocityDeltaPerSecond * Time.deltaTime);
        invVelSmooth = transform.InverseTransformVector(targetVelocity);
        planeVel = transform.TransformVector(new Vector3(invVelSmooth.x, 0f, invVelSmooth.z));
        
        // Animation interpolation
        BodySpringAnimation();



        if(isGrounded) {
            WalkAnimation();
        } else {
            FloatingAnimation();
            //JetAnimation();
        }
        HoldingItem();



        // Clamps the arm so they don't look too long if an error had occured
        if(isGrounded) {
            ClampLeftArm();
            ClampRightArm();
        }
        DrawArms(ClawOpenState.Closed, ClawOpenState.Closed);
        // Is opened : !isGrounded || rightTimer > 0f
    }

    private void FixedUpdate () {

        // Grounding
        int mask = 1 << 9;
        Vector3 castPosition = transform.parent.position + 0.55f * (Vector3)GravitySystem.GetGravityDirection(transform.position);
        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(castPosition, 0.49f + 0.2f, mask);
        if(Physics.Raycast(castPosition, Vector3.down, out RaycastHit hitInfo, 0.6f, mask))
            groundNormal = hitInfo.normal;
        else
            groundNormal = -transform.parent.up;

        // Computes velocity relative to itself for custom direction support
        lastInvVel = invVel;
        invVel = transform.InverseTransformVector(velocity);
        planeVel = transform.TransformVector(new Vector3(invVel.x, 0f, invVel.z));



        // Grounding events
        if(!wasGrounded && isGrounded)
            OnGround();
        else if(wasGrounded && !isGrounded)
            OnFloat();
        
        // Process animation logic
        if(isGrounded) {
            WalkLogic();
        }



        // Compares velocity change to apply some velocity to the spring
        if(lastInvVel.y < -4f && Mathf.Abs(invVel.y) < 0.7f) {
            bodySpringVelocity += (Vector3.down * Mathf.Abs(lastInvVel.y) * 0.5f);
        }
        
        // Gives a bouncy look to the body
        BodySpringLogic();
    }



    private void WalkAnimation () {
        if(leftTimer > 0f) {
            currentLeftArmWorld = GetBezierPoint(fromLeftArmWorld, topLeftArmWorld, targetLeftArmWorld, 1f - leftTimer);
            leftTimer = Mathf.Clamp01(leftTimer - Time.deltaTime * motionArcSpeed);
            if(leftTimer == 0f) {
                AudioManager.PlayEnvironmentSoundAt(targetLeftArmWorld, EnvironmentSound.Step);
                ExplosionManager.SpawnStepAt(targetLeftArmWorld);
            }
        } else {
            currentLeftArmWorld = targetLeftArmWorld;
        }
        if(rightTimer > 0f) {
            currentRightArmWorld = GetBezierPoint(fromRightArmWorld, topRightArmWorld, targetRightArmWorld, 1f - rightTimer);
            rightTimer = Mathf.Clamp01(rightTimer - Time.deltaTime * motionArcSpeed);
            if(rightTimer == 0f) {
                AudioManager.PlayEnvironmentSoundAt(targetRightArmWorld, EnvironmentSound.Step);
                ExplosionManager.SpawnStepAt(targetRightArmWorld);
            }
        } else {
            currentRightArmWorld = targetRightArmWorld;
        }
    }

    public void HoldingItem () {
        if(holdPointLeft == null) {
            if(unholdingTimerLeft > 0) {
                unholdingTimerLeft = Mathf.Clamp01(unholdingTimerLeft - Time.deltaTime * 6f);
            } else {
                holdingValueLeft = Mathf.Clamp01(holdingValueLeft - Time.deltaTime * 8f);
            }
            if(lastHoldPointLeft != null) {
                lastHoldPositionLeft = lastHoldPointLeft.position;
            }
        } else {
            unholdingTimerLeft = 1f;
            lastHoldPointLeft = holdPointLeft;
            lastHoldPositionLeft = holdPointLeft.position;
            holdingValueLeft = Mathf.Clamp01(holdingValueLeft + Time.deltaTime * 8f);
        }
        currentRightArmWorld = Vector3.Lerp(currentRightArmWorld, lastHoldPositionLeft, holdingValueLeft);

        if(holdPointRight == null) {
            if(unholdingTimerRight > 0) {
                unholdingTimerRight = Mathf.Clamp01(unholdingTimerRight - Time.deltaTime * 6f);
            } else {
                holdingValueRight = Mathf.Clamp01(holdingValueRight - Time.deltaTime * 8f);
            }
            if(lastHoldPointRight != null) {
                lastHoldPositionRight = lastHoldPointRight.position;
            }
        } else {
            unholdingTimerRight = 1f;
            lastHoldPointRight = holdPointRight;
            lastHoldPositionRight = holdPointRight.position;
            holdingValueRight = Mathf.Clamp01(holdingValueRight + Time.deltaTime * 8f);
        }
        currentLeftArmWorld = Vector3.Lerp(currentLeftArmWorld, lastHoldPositionRight, holdingValueRight);
    }

    private void WalkLogic () {
        bool unbalanced = GetBalance() > maxBalance;
        bool tooStretched = GetStretch() > maxArmStrech;
        bool tooTwisted = Vector3.Angle(lastRotAtRaycast, transform.parent.forward) > 45;
        bool timerDone = leftTimer == 0f && rightTimer == 0f;

        if(timerDone && (unbalanced || tooStretched || tooTwisted)) {
            if(priorityLeft) {
                RaycastLeftArm(planeVel);
            } else {
                RaycastRightArm(planeVel);
            }
        }
    }

    private void FloatingAnimation () {

        // Calculate where to sample cosigns based on speed and velocity
        floatingTime0 += Time.deltaTime * floatSpeed * Mathf.Lerp(minShakeMul, maxShakeMul, Mathf.InverseLerp(0f, maxShakeVel, Mathf.Abs(invVelSmooth.y)));
        floatingTime1 += Time.deltaTime * floatSecondSpeed * Mathf.Lerp(minShakeMul, maxShakeMul, Mathf.InverseLerp(0f, maxShakeVel, Mathf.Abs(invVelSmooth.y)));
        
        // Sample offseted cosines for different movement and brings the -1,1 range to 0,1 to be used in lerp
        float floatLerp0 = Mathf.Cos(floatingTime0) * 0.5f + 0.5f;
        float floatLerp1 = Mathf.Cos(floatingTime0 + floatSecondOffset) * 0.5f + 0.5f;
        float floatLerp2 = Mathf.Cos(floatingTime1 + Mathf.PI) * 0.5f + 0.5f;
        float floatLerp3 = Mathf.Cos(floatingTime1 + Mathf.PI + floatSecondOffset) * 0.5f + 0.5f;

        // Calcuates "dragged out arm" offset
        float offsetY = -invVelSmooth.y * velocityYDragInfluenceOnY;
        float offsetX = invVelSmooth.y * velocityYDragInfluenceOnX;

        // Find the corresponding "world" point for the animated position
        if(!isAnchor) {
            currentLeftArmWorld = Vector3.Lerp(anchorToFloatPosLeft, transform.parent.TransformPoint(
                new Vector3(
                    Mathf.Lerp(1f, 1.5f, floatLerp2) - offsetX,
                    Mathf.Lerp(-1f, -1.5f, floatLerp0) + offsetY,
                    Mathf.Lerp(-0.2f, 0.2f, floatLerp1))) - planeVel * velocityXZDragInfluence, 1f-anchorToFloatTimeLeft);
            currentRightArmWorld = Vector3.Lerp(anchorToFloatPosRight, transform.parent.TransformPoint(
                new Vector3(
                    Mathf.Lerp(-1f, -1.5f, floatLerp3) + offsetX,
                    Mathf.Lerp(-1f, -1.5f, floatLerp1) + offsetY,
                    Mathf.Lerp(-0.2f, 0.2f, floatLerp0))) - planeVel * velocityXZDragInfluence, 1f-anchorToFloatTimeRight);
        } else {
            currentLeftArmWorld = Vector3.Lerp(anchorToFloatPosLeft, transform.parent.TransformPoint(
                new Vector3(0.5f, -1f, 0f)), 1f - anchorToFloatTimeLeft);
            currentRightArmWorld = Vector3.Lerp(anchorToFloatPosRight, transform.parent.TransformPoint(
                new Vector3(-0.5f, -1f, 0f)), 1f - anchorToFloatTimeRight);
        }

        anchorToFloatTimeLeft = Mathf.Max(0f, anchorToFloatTimeLeft - Time.deltaTime * anchorToFloatSpeed);
        anchorToFloatTimeRight = Mathf.Max(0f, anchorToFloatTimeRight - Time.deltaTime * anchorToFloatSpeed);
    }

    private void JetAnimation () {

        // Calculate where to sample cosigns based on speed and velocity
        floatingTime0 += Time.deltaTime * jetFloatSpeed * Mathf.Lerp(jetMinShakeMul, jetMaxShakeMul, Mathf.InverseLerp(0f, jetMaxShakeVel, Mathf.Abs(invVelSmooth.y)));
        floatingTime1 += Time.deltaTime * jetFloatSpeed * Mathf.Lerp(jetMinShakeMul, jetMaxShakeMul, Mathf.InverseLerp(0f, jetMaxShakeVel, Mathf.Abs(invVelSmooth.y)));

        // Sample offseted cosines for different movement and brings the -1,1 range to 0,1 to be used in lerp
        float floatLerp0 = Mathf.Cos(floatingTime0) * 0.5f + 0.5f;
        float floatLerp1 = Mathf.Cos(floatingTime0 + jetFloatSecondOffset) * 0.5f + 0.5f;
        float floatLerp2 = Mathf.Cos(floatingTime1) * 0.5f + 0.5f;
        float floatLerp3 = Mathf.Cos(floatingTime1 + jetFloatSecondOffset) * 0.5f + 0.5f;

        // Calcuates "dragged out arm" offset
        float offsetY = -invVelSmooth.y * jetVelocityYDragInfluenceOnY;
        float offsetX = invVelSmooth.y * jetVelocityYDragInfluenceOnX;

        // Find the corresponding "world" point for the animated position
        currentLeftArmWorld = transform.parent.TransformPoint(
            new Vector3(
                Mathf.Lerp(1f, 1.5f, floatLerp2) - offsetX,
                Mathf.Lerp(-1f, -1.5f, floatLerp0) + offsetY,
                Mathf.Lerp(-0.2f, 0.2f, floatLerp1))) - planeVel * jetVelocityXZDragInfluence;
        currentRightArmWorld = transform.parent.TransformPoint(
            new Vector3(
                Mathf.Lerp(-1f, -1.5f, floatLerp3) + offsetX,
                Mathf.Lerp(-1f, -1.5f, floatLerp1) + offsetY,
                Mathf.Lerp(-0.2f, 0.2f, floatLerp0))) - planeVel * jetVelocityXZDragInfluence;
    }

    private void DeltaAnimation () {
        currentLeftArmWorld = deltaHoldPointRight.transform.position;
        currentRightArmWorld = deltaHoldPointLeft.transform.position;
        deltaItem.localEulerAngles = new Vector3(Mathf.Clamp(-invVelSmooth.y, -90f, 90f), Mathf.Clamp(-invVelSmooth.x * 0.5f, -90f, 90f), Mathf.Clamp(-invVelSmooth.x, -90f, 90f));
    }


    private void RaycastLeftArm (Vector3 planeVel) {

        // Preparing ray
        Vector3 direction = /*alignToGround **/ transform.TransformDirection(
            new Vector3(anchorRaycastDirection.x, anchorRaycastDirection.y).normalized) + planeVel * velocityInfluenceToRaycast;
        Vector3 position = originLeftArm.TransformPoint(-anchorRaycastOriginOffset);

        Vector3 finalLeft = transform.position;

        if(Physics.Raycast(position, direction, out RaycastHit hitInfo, maxArmReach, 1 << 9)) {
            finalLeft = hitInfo.point;
            lastRotAtRaycast = transform.parent.forward;
        } else {
            finalLeft = originRightArm.TransformPoint(Vector3.down * 1.5f);
        }

        if((targetLeftArmWorld - finalLeft).sqrMagnitude < (maxDistanceWihoutReanchor * maxDistanceWihoutReanchor)) {
            return;
        }

        leftTimer = 1f;
        fromLeftArmWorld = targetLeftArmWorld;
        currentLeftArmWorld = fromLeftArmWorld;
        targetLeftArmWorld = finalLeft;
        topLeftArmWorld = (fromLeftArmWorld + targetLeftArmWorld) * 0.5f + transform.up * motionArcTop;

        priorityLeft = false;
    }

    private void RaycastRightArm (Vector3 planeVel) {
        
        // Preparing ray
        Vector3 direction = /*alignToGround * */transform.TransformDirection(
            new Vector3(-anchorRaycastDirection.x, anchorRaycastDirection.y).normalized) + planeVel * velocityInfluenceToRaycast;
        Vector3 position = originRightArm.TransformPoint(anchorRaycastOriginOffset);
        Vector3 finalRight = transform.position;

        if(Physics.Raycast(position, direction, out RaycastHit hitInfo, maxArmReach, 1 << 9)) {
            finalRight = hitInfo.point;
            lastRotAtRaycast = transform.parent.forward;
        } else {
            finalRight = originRightArm.TransformPoint(Vector3.down * 1.5f);
        }

        if((targetRightArmWorld - finalRight).sqrMagnitude < (maxDistanceWihoutReanchor * maxDistanceWihoutReanchor)) {
            return;
        }

        rightTimer = 1f;
        fromRightArmWorld = targetRightArmWorld;
        currentRightArmWorld = fromRightArmWorld;
        targetRightArmWorld = finalRight;
        topRightArmWorld = (fromRightArmWorld + targetRightArmWorld) * 0.5f + transform.up * motionArcTop;

        priorityLeft = true;
    }
    
    private void OnGround () {
        fromLeftArmWorld = currentLeftArmWorld;
        targetLeftArmWorld = currentLeftArmWorld;
        RaycastLeftArm(planeVel);
        topLeftArmWorld = Vector3.Lerp(fromLeftArmWorld, targetLeftArmWorld, 0.5f);

        fromRightArmWorld = currentRightArmWorld;
        targetRightArmWorld = currentRightArmWorld;
        RaycastRightArm(planeVel);
        topRightArmWorld = Vector3.Lerp(fromRightArmWorld, targetRightArmWorld, 0.5f);
    }

    private void OnFloat () {
        anchorToFloatTimeLeft = 1f;
        anchorToFloatPosLeft = currentLeftArmWorld;
        anchorToFloatTimeRight = 1f;
        anchorToFloatPosRight = currentRightArmWorld;
    }

    #region Get Control Values
    private float GetBalance () {
        Vector3 worldToLocalLeft = invAlignToGround * transform.InverseTransformPoint(targetLeftArmWorld);
        Vector3 worldToLocalRight = invAlignToGround * transform.InverseTransformPoint(targetRightArmWorld);

        return DistancePointToSegment(
            new Vector2(worldToLocalLeft.x, worldToLocalLeft.z),
            new Vector2(worldToLocalRight.x, worldToLocalRight.z),
            Vector2.zero);
    }

    private float GetStretch () {
        return Mathf.Max(
            Vector3.Distance(targetLeftArmWorld, originLeftArm.position),
            Vector3.Distance(targetRightArmWorld, originRightArm.position));
    }
    #endregion


    // Self enclosed stuff that shouldn't messed with
    #region Spring
    Vector3 lastBodyOffsetPos;
    Vector3 bodyOffsetPos;
    private void BodySpringLogic () {

        // Calculate spring force (using distance from bodyPosition to 0,0,0)
        float xAbs = bodySpringPosition.magnitude;
        Vector3 springForce = bodySpringK * xAbs * ((-bodySpringPosition).normalized / xAbs * xAbs) - bodySpringB * bodySpringVelocity;

        // Add no velocity to the spring if the distance happened to be 0
        if(!float.IsNaN(springForce.x) && !float.IsNaN(springForce.y) && !float.IsNaN(springForce.z)) {
            bodySpringVelocity += springForce * invBodySpringMass * Time.deltaTime;
        }

        // Moves saved position and localPosition
        bodySpringPosition += bodySpringVelocity * Time.deltaTime;
        Vector3 clampedLocalPosition = new Vector3(
            Mathf.Clamp(bodySpringPosition.x, -0.5f, 0.5f),
            Mathf.Clamp(bodySpringPosition.y, -0.5f - bodyYOffset, 0.75f - bodyYOffset),
            Mathf.Clamp(bodySpringPosition.z, -0.5f, 0.5f));
        lastBodyOffsetPos = bodyOffsetPos;
        bodyOffsetPos = clampedLocalPosition + Vector3.up * bodyYOffset;

        // Simulate ground hits
        if(bodySpringPosition.y < clampedLocalPosition.y) {
            bodySpringPosition.y = clampedLocalPosition.y;
            bodySpringVelocity.y *= -0.5f;
        }
    }

    private void BodySpringAnimation () {
        transform.localPosition = Vector3.Lerp(lastBodyOffsetPos, bodyOffsetPos, (Time.time - Time.fixedTime) / Time.fixedDeltaTime);
    }
    #endregion

    #region Drawing Arms and Utils
    private void ClampLeftArm () {
        Vector3 leftDiff = currentLeftArmWorld - originLeftArm.position;
        float leftDistance = Mathf.Min(maxLength, leftDiff.magnitude);
        Vector3 leftNormal = leftDiff.normalized;
        currentLeftArmWorld = originLeftArm.position + leftNormal * leftDistance;
    }

    private void ClampRightArm () {
        Vector3 rightDiff = currentRightArmWorld - originRightArm.position;
        float rightDistance = Mathf.Min(maxLength, rightDiff.magnitude);
        Vector3 rightNormal = rightDiff.normalized;
        currentRightArmWorld = originRightArm.position + rightNormal * rightDistance;
    }

    private void DrawArms (ClawOpenState leftOpenState, ClawOpenState rightOpenState) {
        leftPos[0] = originLeftArm.position + transform.TransformVector(Vector3.left) * 0.01f;
        Vector3 oldPos = Vector3.zero;
        Vector3 newPos = Vector3.zero;
        Vector3 lastDir = Vector3.zero;
        for(int i = 0; i < resolution; i++) {
            oldPos = newPos;
            newPos = GetBezierPoint(originLeftArm.position, topLeftArm.position, currentLeftArmWorld, (float)i / (resolution - 1));
            leftPos[i + 1] = newPos;
            lastDir = (leftPos[i + 1] - leftPos[i]).normalized;
        }
        leftPos[resolution] -= lastDir * 0.05f;
        leftClaw.transform.SetPositionAndRotation(currentLeftArmWorld, Quaternion.LookRotation((newPos - oldPos).normalized, Vector3.up));
        leftClaw.clawOpenState = leftOpenState;

        
        rightPos[0] = originRightArm.position + transform.TransformVector(Vector3.right) * 0.01f;
        for(int i = 0; i < resolution; i++) {
            oldPos = newPos;
            newPos = GetBezierPoint(originRightArm.position, topRightArm.position, currentRightArmWorld, (float)i / (resolution - 1));
            rightPos[i + 1] = newPos;
            lastDir = (rightPos[i + 1] - rightPos[i]).normalized;
        }
        rightPos[resolution] -= lastDir * 0.05f;
        rightClaw.transform.SetPositionAndRotation(currentRightArmWorld, Quaternion.LookRotation((newPos - oldPos).normalized, Vector3.up));
        rightClaw.clawOpenState = rightOpenState;
    }

    public float DistancePointToSegment (Vector3 p0, Vector3 p1, Vector3 p) {
        Vector3 v = p1 - p0;
        Vector3 w = p - p0;

        float c1 = Vector3.Dot(w, v);
        if(c1 <= 0)
            return Vector3.Distance(p, p0);

        float c2 = Vector3.Dot(v, v);
        if(c2 <= c1)
            return Vector3.Distance(p, p1);

        float b = c1 / c2;
        Vector3 Pb = p0 + b * v;
        return Vector3.Distance(p, Pb);
    }

    public static Vector3 GetBezierPoint (Vector3 p0, Vector3 p1, Vector3 p2, float t) {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return
            oneMinusT * oneMinusT * p0 +
            2f * oneMinusT * t * p1 +
            t * t * p2;
    }
    #endregion
}
