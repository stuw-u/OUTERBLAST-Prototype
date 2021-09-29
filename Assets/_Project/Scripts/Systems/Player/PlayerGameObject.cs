using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using Blast.ECS;
using Blast.ECS.DefaultComponents;
using Cinemachine;

[DefaultExecutionOrder(110)]
public class PlayerGameObject : MonoBehaviour {
    [Header("References")]
    public PlayerItemUserVisuals itemUserVisuals;
    public UserData userData;
    public int[] inventory;
    public bool isSelfControlled;
    public Transform headX;
    public Transform headY;
    public Transform headVisual;
    private Transform body;
    public PlayerAnimator playerAnimator;
    public Transform eyeAnchor;

    [Header("Render References")]
    public Renderer[] renderers;
    public Renderer[] inverseRenderers;
    public Renderer[] colorableRenderers;

    [Header("FOV Boost")]
    public float fovBoostSmooth = 0.1f;
    public float fovBoostSpeed = 20f;
    public float fovBoostMaxVel = 50f;
    public float fovBoostMaxAngleDelta = 30f;

    public Camera mainCamera;
    public CinemachineVirtualCamera virtualCamera;
    public CinemachineBrain brain;
    public CinemachineImpulseListener impulseListener;
    public Vector3 lastestSimPosition;

    private float axisTime;
    private Quaternion targetAxis;
    private Quaternion lastAxis;
    private Quaternion currentAxis;
    private float lastAxisUpdateTime;
    private float axisUpdateInterval = 1f;
    public ulong playerId;
    private bool isHidden = false;

    private void Start () {
        mainCamera = GameManager.inst.mainCamera;

        if(isSelfControlled) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if(PlayerIndicatorDisplay.inst != null)
                PlayerIndicatorDisplay.SetMainCamera(mainCamera);
        }
        body = transform;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetColor("_MainColor", userData.DisplayInfo.color);
        foreach(Renderer r in colorableRenderers) {
            r.SetPropertyBlock(mpb);
        }

        itemUserVisuals.Setup(inventory, userData, isSelfControlled);
    }

    #region Set/Apply Data
    public void ApplyItemUser (ref ItemUserComponent itemUser) {
        itemUserVisuals.SetState(itemUser.lastHeldItem, itemUser.lastHeldItemUsed);

        if(itemUser.lastHeldItem >= 0 && SimulationManager.inst.playerSystem.itemManagers.TryGetValue(userData.ClientID, out LocalItemManager lim)) {
            BaseItem itemBase = AssetsManager.inst.itemAssets.items[lim.fixedInventory[itemUser.lastHeldItem].assetID];
            if(itemBase is IShatterableItemVisual) {
                itemUserVisuals.SetShatterState(itemUser.lastHeldItem, ((IShatterableItemVisual)itemBase).GetShatterValue(lim.fixedInventory[itemUser.lastHeldItem].data));
            }
        }
    }

    public void ApplyComponents (float3 pos, quaternion rot) {
        if(body == null) {
            return;
        }
        if(math.any(math.isnan(pos)) || math.any(math.isinf(pos))) {
            pos = float3.zero;
        }
        body.transform.position = pos;
        body.transform.rotation = rot;
    }

    public void ApplyAxis (float2 axis) {
        headX.localEulerAngles = Vector3.right * axis.y;
        headY.localEulerAngles = Vector3.up * axis.x;
        eyeAnchor.localRotation = Quaternion.Euler(axis.y, 0f, 0f);
    }

    public void ApplyAxis () {
        headX.localEulerAngles = Vector3.right * currentAxis.eulerAngles.x;
        headY.localEulerAngles = Vector3.up * currentAxis.eulerAngles.y;
        eyeAnchor.localEulerAngles = Vector3.right * currentAxis.eulerAngles.x;
    }

    public void SetInterpolatedAxis (float2 axis) {
        targetAxis = Quaternion.Euler(axis.y, axis.x, 0f);
        axisUpdateInterval = math.max(0.001f, Time.time - lastAxisUpdateTime);
        lastAxis = currentAxis;
        lastAxisUpdateTime = Time.time;
    }

    public void SetInterpolatedAxis (float2 axis, float localReceivingTime) {
        targetAxis = Quaternion.Euler(axis.y, axis.x, 0f);
        axisUpdateInterval = math.max(0.001f, localReceivingTime - lastAxisUpdateTime);
        lastAxis = currentAxis;
        lastAxisUpdateTime = Time.time;
        axisTime = 0f;
    }
    #endregion

    private const float bodyHideDistanceSqr = 2.5f;
    private float lastCamBodyDistance;
    private void Update () {

        if(isSelfControlled) {
            ApplyAxis(new float2(InputListener.GetMouseAxisX(), InputListener.GetMouseAxisY()));
            //brain.ManualUpdate();
        } else {
            axisTime += Time.deltaTime / axisUpdateInterval;
            currentAxis = Quaternion.Slerp(lastAxis, targetAxis, axisTime);
            ApplyAxis();
        }
    }
    
    private float fovBoostDelta = 0f;
    private void LateUpdate () {
        if(NetAssist.IsHeadlessServer) {
            return;
        }

        bool isGhost = false;
        if(SimulationManager.inst.simulationWorld.IsAlive()) {
            isGhost = SimulationManager.inst.playerSystem.playerEntities[playerId].Has<Ghost>();
        }
        if(PlayerIndicatorDisplay.inst != null)
            PlayerIndicatorDisplay.inst.SetHideIndicator(userData.ClientID, isGhost);
        
        float lerpBlend = 1f - math.pow(1f - fovBoostSmooth, Time.deltaTime * fovBoostSpeed);
        fovBoostDelta = math.lerp(fovBoostDelta, Mathf.InverseLerp(0f, fovBoostMaxVel, lastVelocity.magnitude) * fovBoostMaxAngleDelta, lerpBlend);

        if(isSelfControlled) {
            virtualCamera.m_Lens.FieldOfView = Blast.Settings.SettingsManager.settings.fieldOfView + fovBoostDelta;
            impulseListener.m_Gain = Blast.Settings.SettingsManager.settings.reduceCameraShake ? 0f : 1f;

            // Rendering Mode switch by cameraDistance
            float camBodyDistance = math.distancesq(mainCamera.transform.position, eyeAnchor.position);
            if(camBodyDistance < bodyHideDistanceSqr && lastCamBodyDistance > bodyHideDistanceSqr) {
                itemUserVisuals.SetRendering(true);
                foreach(Renderer r in renderers) {
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
                foreach(Renderer r in inverseRenderers) {
                    r.shadowCastingMode = ShadowCastingMode.On;
                }
            }
            if(camBodyDistance > bodyHideDistanceSqr && lastCamBodyDistance < bodyHideDistanceSqr) {
                itemUserVisuals.SetRendering(false);
                foreach(Renderer r in renderers) {
                    r.shadowCastingMode = ShadowCastingMode.On;
                }
                foreach(Renderer r in inverseRenderers) {
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
            lastCamBodyDistance = camBodyDistance;
        } else {
            if(isHidden != isGhost) {
                foreach(Renderer r in renderers) {
                    r.enabled = !isGhost;
                }
            }
            isHidden = isGhost;
        }
    }

    public bool lastAnchoredState;
    private Vector3 lastSimPosition;
    private Vector3 lastVelocity;
    private void FixedUpdate () {
        Vector3 velocity = (lastestSimPosition - lastSimPosition) / Time.fixedDeltaTime;
        playerAnimator.velocity = velocity;
        playerAnimator.isAnchor = lastAnchoredState;
        lastSimPosition = lastestSimPosition;
        lastVelocity = velocity;
    }
}
